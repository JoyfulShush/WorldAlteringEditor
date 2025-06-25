using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI
{
    class ConnectionCoords(Point2D coords, byte connectionMask)
    {
        public Point2D Coords = coords;        
        public byte ConnectionMask = connectionMask;
    }

    public class PlacedTile(TileImage tileImage, Point2D coords)
    {
        public Point2D Coords = coords;
        public TileImage TileImage = tileImage;
    }

    class TileDisplayTile
    {
        public TileDisplayTile(Point location, Point offset, Point size, TileImage tileImageToDisplay, TileImage tileImageToPlace)
        {
            Location = location;
            Offset = offset;
            Size = size;
            TileImageToDisplay = tileImageToDisplay;
            TileImageToPlace = tileImageToPlace;
        }

        public Point Location { get; set; }
        public Point Offset { get; set; }
        public Point Size { get; set; }
        public TileImage TileImageToDisplay { get; set; }
        public TileImage TileImageToPlace { get; set; }
    }

    public class TileDisplay : XNAPanel
    {
        private const int TILE_PADDING = 3;
        private const int SCROLL_RATE = 10;
        private const int KEYBOARD_SCROLL_RATE = 400;

        public TileDisplay(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            PlaceTerrainCursorAction placeTerrainCursorAction, EditorState editorState) : base(windowManager)
        {
            this.theaterGraphics = theaterGraphics;
            this.map = map;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;            
            placeTerrainCursorAction.ActionExited += OnCursorActionExited;
            placeTerrainCursorAction.TerrainTilePlaced += OnTilePlaced;
            this.editorState = editorState;
        }

        public event EventHandler SelectedTileChanged;

        private TileImage _selectedTile;
        public TileImage SelectedTile
        {
            get => _selectedTile;
            set
            {
                if (_selectedTile != value)
                {
                    _selectedTile = value;
                    SelectedTileChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private PlacedTile lastPlacedTile;
        private PlacedTile secondLastPlacedTile;

        public TileSet TileSet { get; private set; }

        private List<TileDisplayTile> tilesInView = new List<TileDisplayTile>();

        private double _viewY = 0;
        private double ViewY
        {
            get => _viewY;
            set
            {
                if (value > 0)
                    _viewY = 0;
                else
                    _viewY = value;
            }
        }

        private readonly EditorState editorState;

        private Effect palettedDrawEffect;

        public override void Initialize()
        {
            base.Initialize();

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            palettedDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedDrawNoDepth");

            KeyboardCommands.Instance.NextTile.Action = NextTile;
            KeyboardCommands.Instance.PreviousTile.Action = PreviousTile;
            editorState.MarbleMadnessChanged += OnMarbleMadnessChanged;
        }

        /// <summary>
        /// Handles the "Next Tile" keyboard command.
        /// </summary>
        private void NextTile()
        {
            if (!AppliesToSelfAndAllParents(c => c.Enabled))
                return;

            int selectedTileIndex = tilesInView.FindIndex(t => t.TileImageToPlace == SelectedTile);

            if (SelectedTile == null || selectedTileIndex < 0)
            {
                // If no tile from the current tileset is selected, then select the first tile

                if (tilesInView.Count > 0)
                    SelectedTile = tilesInView[0].TileImageToPlace;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedTileIndex += 5;
            else
                selectedTileIndex++;

            // Don't cross bounds
            if (selectedTileIndex >= tilesInView.Count)
                selectedTileIndex = tilesInView.Count - 1;

            SelectedTile = tilesInView[selectedTileIndex].TileImageToPlace;
        }

        /// <summary>
        /// Handles the "Previous Tile" keyboard command.
        /// </summary>
        private void PreviousTile()
        {
            if (!AppliesToSelfAndAllParents(c => c.Enabled))
                return;

            int selectedTileIndex = tilesInView.FindIndex(t => t.TileImageToPlace == SelectedTile);

            if (SelectedTile == null || selectedTileIndex < 0)
            {
                // If no tile from the current tileset is selected, then select the last tile

                if (tilesInView.Count > 0)
                    SelectedTile = tilesInView[tilesInView.Count - 1].TileImageToPlace;

                return;
            }

            if (Keyboard.IsAltHeldDown())
                selectedTileIndex -= 5;
            else
                selectedTileIndex--;

            // Don't cross bounds
            if (selectedTileIndex < 0)
                selectedTileIndex = 0;

            SelectedTile = tilesInView[selectedTileIndex].TileImageToPlace;
        }

        protected override void OnClientRectangleUpdated()
        {
            base.OnClientRectangleUpdated();

            RefreshGraphics();
        }

        public void SetTileSet(TileSet tileSet)
        {
            ViewY = 0;
            this.TileSet = tileSet;
            RefreshGraphics();
        }

        private void RefreshGraphics()
        {
            ViewY = 0;
            tilesInView.Clear();

            if (TileSet == null)
                return;

            var tilesOnCurrentLine = new List<TileDisplayTile>();
            int usableWidth = Width - (Constants.UIEmptySideSpace * 2);
            int y = Constants.UIEmptyTopSpace;
            int x = Constants.UIEmptySideSpace;
            int currentLineHeight = 0;

            CliffType cliffTypeForTileSet = null;
            CliffTile lastPlacedCliffTile = null;
            List<byte> excludedConnectionMasks = [];            
            if (lastPlacedTile != null && lastPlacedTile.TileImage.TileSetId == TileSet.Index)
            {
                lastPlacedCliffTile = GetCliffTile(lastPlacedTile);
                if (lastPlacedCliffTile != null)
                {
                   cliffTypeForTileSet = GetCliffTypeForTileSet();
                }

                if (secondLastPlacedTile != null && lastPlacedCliffTile != null)                
                {                    
                    var secondLastPlacedCliffTile = GetCliffTile(secondLastPlacedTile);

                    if (secondLastPlacedCliffTile != null)
                    {
                        var lastPlacedTileOrigin = lastPlacedTile.Coords;
                        var secondLastPlacedTileOrigin = secondLastPlacedTile.Coords;

                        var connectionCoordsLastPlaced = GetAllConnectionCoords(lastPlacedCliffTile, lastPlacedTileOrigin);
                        var connectionCoordsSecondLastPlaced = GetAllConnectionCoords(secondLastPlacedCliffTile, secondLastPlacedTileOrigin);

                        foreach (var lastPlacedConnCoords in connectionCoordsLastPlaced)
                        {
                            var directions = Helpers.GetDirectionsInMask(lastPlacedConnCoords.ConnectionMask);

                            foreach (var direction in directions)
                            {
                                var coordsWithOffset = lastPlacedConnCoords.Coords + Helpers.VisualDirectionToPoint(direction);

                                var matchingCoords = connectionCoordsSecondLastPlaced.FindAll(connCoords => connCoords.Coords.Equals(coordsWithOffset));
                                foreach (var matchingCoord in matchingCoords)
                                {
                                    var matchingCoordDirections = Helpers.GetDirectionsInMask(matchingCoord.ConnectionMask);
                                    foreach (var matchingCoordDirection in matchingCoordDirections)
                                    {
                                        if (matchingCoordDirection.Equals(direction))
                                            continue;

                                        excludedConnectionMasks.Add(matchingCoord.ConnectionMask);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < TileSet.TilesInSet; i++)
            {
                int tileIndex = TileSet.StartTileIndex + i;
                if (tileIndex > theaterGraphics.TileCount)
                    break;

                TileImage tileImageToPlace = theaterGraphics.GetTileGraphics(tileIndex);
                TileImage tileImageToDisplay = editorState.IsMarbleMadness ? theaterGraphics.GetMarbleMadnessTileGraphics(tileIndex) : tileImageToPlace;

                if (tileImageToDisplay == null)
                    break;

                if (cliffTypeForTileSet != null)
                {
                    var relevantCliffTile = cliffTypeForTileSet.Tiles.Find(tile =>
                    {
                        return tile.IndicesInTileSet.Contains(tileImageToPlace.TileIndexInTileSet);
                    });

                    if (relevantCliffTile != null)
                    {
                        bool foundMatchingConnectionMask = false;
                        foreach (var relevantCliffTileConnectionPoint in relevantCliffTile.ConnectionPoints)
                        {
                            var side = relevantCliffTileConnectionPoint.Side;
                            var connectionMask = relevantCliffTileConnectionPoint.ConnectionMask;

                            if (excludedConnectionMasks.Contains(connectionMask))
                                continue;

                            foreach (var lastPlacedTileConnectionPoint in lastPlacedCliffTile.ConnectionPoints)
                            {
                                if (connectionMask == lastPlacedTileConnectionPoint.ConnectionMask && side == lastPlacedTileConnectionPoint.Side)
                                {   
                                    foundMatchingConnectionMask = true;
                                }

                                if (foundMatchingConnectionMask)
                                    break;
                            }

                            if (foundMatchingConnectionMask)
                                break;
                        }

                        if (!foundMatchingConnectionMask)
                            continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                int width = tileImageToDisplay.GetWidth(out int minX);
                int height = tileImageToDisplay.GetHeight();
                int yOffset = tileImageToDisplay.GetYOffset();

                if (x + width > usableWidth)
                {
                    // Start a new line of tile graphics

                    x = Constants.UIEmptySideSpace;
                    y += currentLineHeight + TILE_PADDING;
                    CenterLine(tilesOnCurrentLine, currentLineHeight);
                    currentLineHeight = 0;
                    tilesOnCurrentLine.Clear();
                }

                if (minX > 0)
                    minX = 0;

                var tileDisplayTile = new TileDisplayTile(new Point(x, y), new Point(-minX, yOffset), new Point(width, height), tileImageToDisplay, tileImageToPlace);
                tilesInView.Add(tileDisplayTile);

                if (height > currentLineHeight)
                    currentLineHeight = height;
                x += width + TILE_PADDING;
                tilesOnCurrentLine.Add(tileDisplayTile);
            }

            CenterLine(tilesOnCurrentLine, currentLineHeight);
        }

        /// <summary>
        /// Centers all tiles vertically relative to each other.
        /// </summary>
        private void CenterLine(List<TileDisplayTile> line, int lineHeight)
        {
            foreach (var tile in line)
            {
                tile.Location = new Point(tile.Location.X, tile.Location.Y + (lineHeight - tile.TileImageToDisplay.GetHeight()) / 2);
            }
        }

        public override void OnMouseScrolled()
        {
            base.OnMouseScrolled();
            ViewY += Cursor.ScrollWheelValue * SCROLL_RATE;
        }

        public override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            SelectedTile = GetTileUnderCursor()?.TileImageToPlace;
        }

        private TileDisplayTile GetTileUnderCursor()
        {
            if (!IsActive)
                return null;

            Point cursorPoint = GetCursorPoint();

            foreach (var tile in tilesInView)
            {
                var rectangle = new Rectangle(tile.Location.X, tile.Location.Y + (int)ViewY, tile.Size.X, tile.Size.Y);
                if (rectangle.Contains(cursorPoint))
                    return tile;
            }

            return null;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsActive)
            {
                if (Keyboard.IsKeyHeldDown(Keys.Up))
                {
                    ViewY += KEYBOARD_SCROLL_RATE * gameTime.ElapsedGameTime.TotalSeconds;
                }
                else if (Keyboard.IsKeyHeldDown(Keys.Down))
                {
                    ViewY -= KEYBOARD_SCROLL_RATE * gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
        }

        private void SetTileRenderSettings()
        {
            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, palettedDrawEffect));
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            foreach (var tile in tilesInView)
            {
                var rectangle = new Rectangle(tile.Location.X, tile.Location.Y + (int)ViewY, tile.Size.X, tile.Size.Y);
                FillRectangle(rectangle, Color.Black);
            }

            SetTileRenderSettings();

            foreach (var tile in tilesInView)
            {
                var rectangle = new Rectangle(tile.Location.X, tile.Location.Y + (int)ViewY, tile.Size.X, tile.Size.Y);

                if (tile.TileImageToDisplay.TMPImages.Length == 0)
                    continue;

                bool paletteTextureSet = false;

                foreach (MGTMPImage image in tile.TileImageToDisplay.TMPImages)
                {
                    if (image == null || image.TmpImage == null)
                        continue;

                    if (!paletteTextureSet)
                    {
                        palettedDrawEffect.Parameters["PaletteTexture"].SetValue(image.GetPaletteTexture());

                        palettedDrawEffect.Parameters["Lighting"].SetValue(map.Lighting.MapColorFromPreviewMode(editorState.LightingPreviewState).ToXNAVector4());

                        paletteTextureSet = true;
                    }

                    int subTileHeightOffset = image.TmpImage.Height * Constants.CellHeight;

                    DrawTexture(image.Texture, new Rectangle(tile.Location.X + image.TmpImage.X + tile.Offset.X,
                        (int)ViewY + tile.Location.Y + image.TmpImage.Y + tile.Offset.Y - subTileHeightOffset,
                        Constants.CellSizeX, Constants.CellSizeY), Color.White);

                    if (image.ExtraTexture != null)
                    {
                        DrawTexture(image.ExtraTexture, new Rectangle(tile.Location.X + image.TmpImage.XExtra + tile.Offset.X,
                            (int)ViewY + tile.Location.Y + image.TmpImage.YExtra + tile.Offset.Y - subTileHeightOffset,
                            image.ExtraTexture.Width, image.ExtraTexture.Height), Color.White);
                    }
                }

                if (tile.TileImageToPlace == SelectedTile)
                {
                    // We can't draw a red rectangle while the shader is active
                    Renderer.PopSettings();
                    DrawRectangle(rectangle, Color.Red, 2);
                    SetTileRenderSettings();
                }
            }

            Renderer.PopSettings();

            DrawChildren(gameTime);
            DrawPanelBorders();
        }

        public void OnMarbleMadnessChanged(object sender, EventArgs e) => RefreshGraphics();

        public override void Kill()
        {
            editorState.MarbleMadnessChanged -= OnMarbleMadnessChanged;

            base.Kill();
        }

        private void OnTilePlaced(object sender, PlaceTerrainTileCursorActionEventArgs e)
        {
            var placedTile = e.Tile;

            if (lastPlacedTile == null)
            {
                lastPlacedTile = placedTile;
            }
            else
            {
                if (lastPlacedTile.TileImage.TileSetId != placedTile.TileImage.TileSetId)
                {
                    secondLastPlacedTile = null;
                } 
                else
                {
                    secondLastPlacedTile = lastPlacedTile;
                }

                lastPlacedTile = placedTile;
            }

            RefreshGraphics();
        }

        private void OnCursorActionExited(object sender, EventArgs e)
        {
            _selectedTile = null;

            lastPlacedTile = null;
            secondLastPlacedTile = null;
            RefreshGraphics();
        }

        private CliffTile GetCliffTile(PlacedTile mapTile)
        {
            var tileSetName = theaterGraphics.Theater.TileSets.Find(tileSet => tileSet.Index == mapTile.TileImage.TileSetId)?.SetName;

            if (!string.IsNullOrEmpty(tileSetName))
            {
                var cliffTileSet = map.EditorConfig.Cliffs.Find(cliffType =>
                {
                    return cliffType.Tiles.Exists(tile => tile.TileSetName.Contains(tileSetName));
                });

                if (cliffTileSet != null)
                {
                    return cliffTileSet.Tiles.Find(cliffTile =>
                    {
                        return cliffTile.IndicesInTileSet.Contains(mapTile.TileImage.TileIndexInTileSet);
                    });
                }
            }

            return null;
        }

        private CliffType GetCliffTypeForTileSet()
        {
            return map.EditorConfig.Cliffs.Find(cliffType =>
            {
                return cliffType.Tiles.Exists(cliffTile => cliffTile.TileSetName == TileSet.SetName);
            });
        }

        private List<ConnectionCoords> GetAllConnectionCoords(CliffTile cliffTile, Point2D originCoords) 
        {
            List<ConnectionCoords> coords = [];

            foreach (var connectionPoint in cliffTile.ConnectionPoints)
            {
                var cpOffset = connectionPoint.CoordinateOffset;
                var connectionCoord = originCoords + cpOffset;

                coords.Add(new ConnectionCoords(connectionCoord, connectionPoint.ConnectionMask));                
            }

            return coords;
        }
    }
}
