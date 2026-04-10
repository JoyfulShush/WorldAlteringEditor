using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    struct OriginalCellTerrainData
    {
        public Point2D CellCoords;
        public int TileIndex;
        public byte SubTileIndex;
        public byte HeightLevel;
        public PlacedTile CurrentTile;
        public PlacedTile PreviousTile;

        public OriginalCellTerrainData(Point2D cellCoords, int tileIndex, byte subTileIndex, byte heightLevel, PlacedTile currentTile, PlacedTile previousTile)
        {
            CellCoords = cellCoords;
            TileIndex = tileIndex;
            SubTileIndex = subTileIndex;
            HeightLevel = heightLevel;
            CurrentTile = currentTile;
            PreviousTile = previousTile;
        }
    }
}
