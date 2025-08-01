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

    class ConnectedTileFilter
    {
        public ConnectedTileFilter() {}

        public ConnectedTileFilter(CliffType cliffTypeForTileSet, CliffTile lastPlacedCliffTile, List<byte> excludedConnectionMasks)
        {
            CliffTypeForTileSet = cliffTypeForTileSet;
            LastPlacedCliffTile = lastPlacedCliffTile;
            ExcludedConnectionMasks = excludedConnectionMasks;
        }

        public CliffType CliffTypeForTileSet;
        public CliffTile LastPlacedCliffTile;
        public List<byte> ExcludedConnectionMasks = [];
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
            map.TilePlaced += OnTilePlaced;
            map.UndoTilePlaced += UndoTilePlaced;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;            
            this.placeTerrainCursorAction = placeTerrainCursorAction;
            placeTerrainCursorAction.ActionExited += OnCursorActionExited;
            this.editorState = editorState;
        }

        public event EventHandler SelectedTileChanged;

        private PlaceTerrainCursorAction placeTerrainCursorAction;

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

        private PlacedTile _lastPlacedTile;
        private PlacedTile LastPlacedTile
        {
            get => _lastPlacedTile;
            set
            {
                _lastPlacedTile = value;
                placeTerrainCursorAction.LastPlacedTile = value;
            }
        }

        private PlacedTile _secondLastPlacedTile;
        public PlacedTile SecondLastPlacedTile
        {
            get => _secondLastPlacedTile;
            set
            {
                _secondLastPlacedTile = value;
                placeTerrainCursorAction.SecondLastPlacedTile = value;
            }
        }

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
            editorState.FilterTilesDisplayChanged += OnFilterTilesDisplayChanged;
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

            var connectedTileFilter = editorState.FilterTilesDisplay ? GetConnectedTileFilter(TileSet) : null;

            for (int i = 0; i < TileSet.TilesInSet; i++)
            {
                int tileIndex = TileSet.StartTileIndex + i;
                if (tileIndex > theaterGraphics.TileCount)
                    break;

                TileImage tileImageToPlace = theaterGraphics.GetTileGraphics(tileIndex);
                TileImage tileImageToDisplay = editorState.IsMarbleMadness ? theaterGraphics.GetMarbleMadnessTileGraphics(tileIndex) : tileImageToPlace;

                if (tileImageToDisplay == null)
                    break;

                // If the tile filter is active and has registered a cliff type for the current set,
                // then check if the current tile can attach to the last placed tile
                if (editorState.FilterTilesDisplay)
                {
                    if (LastPlacedTile != null && connectedTileFilter.CliffTypeForTileSet != null)
                    {
                        // If the cliff types are not equal, then we are not filtering based on last placed tile
                        // in this case, allow the tile to show in the display
                        // this usually occurs when navigating to different tile sets that don't have any shared tiles
                        bool matchingCliffTypes = AreBothCliffTypesEqualSet(tileImageToPlace, LastPlacedTile.TileImage);
                        if (matchingCliffTypes)
                        {
                            if (!CanTileMatchLastPlaced(tileImageToPlace, connectedTileFilter))
                                continue;
                        }
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

        public override void OnMouseScrolled(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            base.OnMouseScrolled(inputEventArgs);
            ViewY += Cursor.ScrollWheelValue * SCROLL_RATE;
        }

        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            SelectedTile = GetTileUnderCursor()?.TileImageToPlace;

            if (SelectedTile != null)
                inputEventArgs.Handled = true;

            base.OnMouseLeftDown(inputEventArgs);
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

        private void OnTilePlaced(object sender, PlaceTerrainTileEventArgs e)
        {
            var placedTile = e.Tile;

            if (LastPlacedTile == null)
            {
                LastPlacedTile = placedTile;
            }
            else
            {
                bool foundMatchingCliffTile = AreBothCliffTypesEqualSet(placedTile.TileImage, LastPlacedTile.TileImage);
                if (foundMatchingCliffTile)
                {
                    SecondLastPlacedTile = LastPlacedTile;
                } 
                else
                {
                    SecondLastPlacedTile = null;
                }

                LastPlacedTile = placedTile;
            }
            
            var cliffType = GetCliffTypeForTileSet(TileSet);
            if (editorState.FilterTilesDisplay && cliffType != null)
            {
                RefreshGraphics();
            }
        }

        private void UndoTilePlaced(object sender, UndoPlaceTerrainTileEventArgs e)
        {
            LastPlacedTile = e.CurrentTile;
            SecondLastPlacedTile = e.PreviousTile;

            var cliffType = GetCliffTypeForTileSet(TileSet);
            if (editorState.FilterTilesDisplay && cliffType != null)
            {
                RefreshGraphics();
            }
        }

        private void OnCursorActionExited(object sender, EventArgs e)
        {
            _selectedTile = null;

            LastPlacedTile = null;
            SecondLastPlacedTile = null;

            var cliffType = GetCliffTypeForTileSet(TileSet);
            if (editorState.FilterTilesDisplay && cliffType != null)
            {
                RefreshGraphics();
            }
        }

        private TileSet GetTileSet(int tileSetId)
        {
            return theaterGraphics.Theater.TileSets.Find(tileSet => tileSet.Index == tileSetId);
        }

        private string GetTileSetName(int tileSetId)
        {
            var relevanTileSet = GetTileSet(tileSetId);
            if (relevanTileSet == null)
                return null;

            return relevanTileSet.SetName;
        }

        private CliffTile GetCliffTile(PlacedTile placedTile)
        {
            if (placedTile == null) 
                return null;

            string tileSetName = GetTileSetName(placedTile.TileImage.TileSetId);

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
                        return cliffTile.IndicesInTileSet.Contains(placedTile.TileImage.TileIndexInTileSet) &&
                               cliffTile.TileSetName == GetTileSetName(placedTile.TileImage.TileSetId);
                    });
                }
            }

            return null;
        }

        private CliffType GetCliffTypeForTileSet(TileSet tileSet)
        {
            if (tileSet == null)
                return null;

            return map.EditorConfig.Cliffs.Find(cliffType =>
            {
                return cliffType.Tiles.Exists(cliffTile => cliffTile.TileSetName == tileSet.SetName);
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

        private bool AreBothCliffTypesEqualSet(TileImage firstTileImage, TileImage secondTileImage)
        {
            var firstTileSet = GetTileSet(firstTileImage.TileSetId);
            var secondTileSet = GetTileSet(secondTileImage.TileSetId);
            if (firstTileSet == null || secondTileSet == null)
                return false;

            var firstCliffType = GetCliffTypeForTileSet(firstTileSet);
            var secondCliffType = GetCliffTypeForTileSet(secondTileSet);
            if (firstCliffType == null || secondCliffType == null)
                return false;

            return firstCliffType.Equals(secondCliffType);
        }

        /// <summary>
        /// Determines whether the specified tile can be validly placed next to the last placed tile,
        /// based on cliff type compatibility and its connection points.
        /// Used to filter out tiles in the tile display.
        /// </summary>
        /// <param name="tileImageToPlace">The tile being considered for placement.</param>
        /// <param name="connectedTileFilter">Contains context information such as the cliff type set and connection constraints.</param>
        /// <returns>
        /// True if the tile matches all conditions to be placed next to the last placed tile; otherwise, false.
        /// </returns>
        private bool CanTileMatchLastPlaced(TileImage tileImageToPlace, ConnectedTileFilter connectedTileFilter)
        {            
            if (connectedTileFilter.CliffTypeForTileSet == null || connectedTileFilter.LastPlacedCliffTile == null)
                return false;

            var cliffTypeForTileSet = connectedTileFilter.CliffTypeForTileSet;
            var lastPlacedCliffTile = connectedTileFilter.LastPlacedCliffTile;            
            
            var tileToPlaceCliffTile = connectedTileFilter.CliffTypeForTileSet.Tiles.Find(tile =>
            {
                return tile.IndicesInTileSet.Contains(tileImageToPlace.TileIndexInTileSet) &&
                       tile.TileSetName == TileSet.SetName;
            });

            if (tileToPlaceCliffTile == null)
                return false;

            // When the last placed tile is an ending tile and it is connected, then we assume that the user has finished drawing the current flow
            // And would like to start a new flow. In this case, we will only show to the user all Ending tiles in this Cliff Tile.
            if (lastPlacedCliffTile.IsEnding &&
                connectedTileFilter.ExcludedConnectionMasks.Count > 0)
            {
                return tileToPlaceCliffTile.IsEnding;
            }

            // Loop through the connection points of the tile being considered
            // If a connection is considered excluded, then reject the tile immediately
            // check each connection point and its contents to determine whether the tile can fit into the last placed one
            foreach (var tileToPlaceCliffTileConnectionPoint in tileToPlaceCliffTile.ConnectionPoints)
            {
                var side = tileToPlaceCliffTileConnectionPoint.Side;
                var connectionMask = tileToPlaceCliffTileConnectionPoint.ConnectionMask;
                var directions = Helpers.GetDirectionsInMask(connectionMask);                

                bool foundExcludedDirection = false;
                foreach (var excludedConnectionMask in connectedTileFilter.ExcludedConnectionMasks)
                {
                    var excludedDirections = Helpers.GetDirectionsInMask(excludedConnectionMask);

                    foreach (var excludedDirection in excludedDirections)
                    {
                        foreach (var direction in directions)
                        {
                            if (excludedDirection.Equals(direction))
                                foundExcludedDirection = true;
                        }
                    }
                }

                if (foundExcludedDirection)
                    continue;                

                foreach (var lastPlacedTileConnectionPoint in lastPlacedCliffTile.ConnectionPoints)
                {
                    if (lastPlacedTileConnectionPoint.ForbiddenTiles != null && !lastPlacedTileConnectionPoint.IgnoreForbiddenTilesInTileDisplayFilter)
                    {
                        foreach (var forbiddenTile in lastPlacedTileConnectionPoint.ForbiddenTiles)
                        {
                            if (forbiddenTile == tileToPlaceCliffTile.Index)
                            {
                                // If this forbidden tile is referencing itself and it is allowed to repeat, skip this tile
                                if (forbiddenTile == lastPlacedCliffTile.Index && lastPlacedCliffTile.AllowRepeatingSelfInTileDisplayFilter)
                                    continue;

                                return false;
                            }
                        }
                    }

                    if (side == lastPlacedTileConnectionPoint.Side)
                    {
                        // Whenever we check for a tile, we want to get the opposite direction of the last placed tile to see if they match.
                        // For example, if the last placed tile has a connection point in the West direction, then it would
                        // match all tiles that have a connection point to the East, assuming they are placed correctly.

                        var lastPlacedReversedDirections = Helpers.GetDirectionsInMask(lastPlacedTileConnectionPoint.ReversedConnectionMask);                        
                        
                        foreach (var direction in directions)
                        {
                            foreach (var reversedDirection in lastPlacedReversedDirections)
                            {
                                if (direction == reversedDirection)
                                {
                                    bool hasRequiredTiles = false;
                                    bool foundRequiredTile = false;
                                    if (lastPlacedTileConnectionPoint.RequiredTiles != null && lastPlacedTileConnectionPoint.RequiredTiles.Length > 0)
                                    {
                                        hasRequiredTiles = true;
                                        foreach (var requiredTile in lastPlacedTileConnectionPoint.RequiredTiles)
                                        {
                                            if (requiredTile == tileToPlaceCliffTile.Index)
                                            {
                                                foundRequiredTile = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!hasRequiredTiles || (hasRequiredTiles && foundRequiredTile))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Constructs a <see cref="ConnectedTileFilter"/> based on the most recently placed tiles and the current set being considered.
        /// It tries to find and assign the cliff type for the set and any connection masks that should be excluded,
        /// based on conflicts between the last two placed tiles, if any.
        /// </summary>
        /// <returns>A <see cref="ConnectedTileFilter"/> containing cliff data and exclusion rules for the current set and recent tiles.</returns>
        private ConnectedTileFilter GetConnectedTileFilter(TileSet tileSet)
        {
            // Fetches the current cliff type for comparison. If this tile set is not part of any cliff type,
            // then we can quit early since we will not have any data to compare the last placed tiles with
            CliffType cliffTypeForTileSet = GetCliffTypeForTileSet(tileSet);

            if (cliffTypeForTileSet == null)
                return new ConnectedTileFilter();

            // If it exists, will be used to compare tiles with the last placed tile's connection points. 
            // If null, then it would mean the last placed tile isn't part of any cliff type set, and no filtering should be done.
            CliffTile lastPlacedCliffTile = null;

            List<byte> excludedConnectionMasks = [];

            lastPlacedCliffTile = GetCliffTile(LastPlacedTile);

            if (lastPlacedCliffTile == null)
                return new ConnectedTileFilter();

            // Comparison with the second last placed tiles, in order to filter out any connection points that are already in use
            // For a connection point to be in use, the last two placed tiles must be placed in adjacent positions
            // where their connection points would collide with each other when taking their directions into account.
            if (SecondLastPlacedTile != null)
            {
                var secondLastPlacedCliffTile = GetCliffTile(SecondLastPlacedTile);

                if (secondLastPlacedCliffTile != null)
                {
                    var lastPlacedTileOrigin = LastPlacedTile.Coords;
                    var secondLastPlacedTileOrigin = SecondLastPlacedTile.Coords;

                    var connectionCoordsLastPlaced = GetAllConnectionCoords(lastPlacedCliffTile, lastPlacedTileOrigin);
                    var connectionCoordsSecondLastPlaced = GetAllConnectionCoords(secondLastPlacedCliffTile, secondLastPlacedTileOrigin);

                    foreach (var lastPlacedConnCoords in connectionCoordsLastPlaced)
                    {
                        var directions = Helpers.GetDirectionsInMask(lastPlacedConnCoords.ConnectionMask);

                        foreach (var direction in directions)
                        {
                            var coordsWithOffset = lastPlacedConnCoords.Coords + Helpers.VisualDirectionToPoint(direction);

                            // Look for second-last tile connection points that are spatially adjacent to the current direction
                            var matchingCoords = connectionCoordsSecondLastPlaced.FindAll(connCoords => connCoords.Coords.Equals(coordsWithOffset));

                            foreach (var matchingCoord in matchingCoords)
                            {
                                var matchingCoordDirections = Helpers.GetDirectionsInMask(matchingCoord.ConnectionMask);

                                foreach (var matchingCoordDirection in matchingCoordDirections)
                                {
                                    if (matchingCoordDirection.Equals(direction))
                                        continue;

                                    if (Helpers.IsReverseDirection(matchingCoordDirection, direction))
                                        excludedConnectionMasks.Add(matchingCoord.ConnectionMask);
                                }
                            }
                        }
                    }
                }
            }

            return new ConnectedTileFilter(cliffTypeForTileSet, lastPlacedCliffTile, excludedConnectionMasks);
        }

        private void OnFilterTilesDisplayChanged(object sender, EventArgs e)
        {
            RefreshGraphics();
        }
    }
}
