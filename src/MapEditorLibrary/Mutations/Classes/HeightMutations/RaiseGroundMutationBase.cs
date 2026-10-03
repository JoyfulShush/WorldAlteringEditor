using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

public abstract class RaiseGroundMutationBase : AlterElevationMutationBase
{
    protected RaiseGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
    {
    }

    /// <summary>
    /// Whether steep ramps may be created.
    /// </summary>
    protected abstract bool AllowSteep { get; }

    protected void RaiseGround()
    {
        Clear();

        var targetCell = Map.GetTile(OriginCell);

        if (targetCell == null || targetCell.Level >= Constants.MaxMapHeightLevel || !IsCellMorphable(targetCell))
            return;

        int targetCellHeight = targetCell.Level;

        // A 2x2 brush creates a small hill if possible, and otherwise acts as 1x1.
        if (BrushSize.Width == 2 && BrushSize.Height == 2)
        {
            if (CanCreateSmallHill(targetCellHeight))
            {
                CreateSmallHill(OriginCell);
                return;
            }
        }

        // Raising a flat cell always affects more than 1 cell,
        // so a 1-wide brush only raises ramps.
        if (BrushSize.Width == 1 || BrushSize.Height == 1)
        {
            if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                return;
        }

        // The brush covers the whole hill including its ramps, so the flat top is 2 cells smaller.
        int xSize = Math.Max(0, BrushSize.Width - 2);
        int ySize = Math.Max(0, BrushSize.Height - 2);

        int beginY = OriginCell.Y - (ySize - 1) / 2;
        int endY = OriginCell.Y + ySize / 2;
        int beginX = OriginCell.X - (xSize - 1) / 2;
        int endX = OriginCell.X + xSize / 2;

        // Only raise ground on the same level as the target cell.
        var targetedCells = new List<Point2D>();
        for (int y = beginY; y <= endY; y++)
        {
            for (int x = beginX; x <= endX; x++)
            {
                var cellCoords = new Point2D(x, y);
                var cell = Map.GetTile(cellCoords);
                if (cell == null || cell.Level >= Constants.MaxMapHeightLevel)
                    continue;

                if (cell.Level != targetCellHeight)
                    continue;

                if (!IsCellMorphable(cell))
                    continue;

                targetedCells.Add(cellCoords);
            }
        }

        if (targetedCells.Count == 0)
            return;

        SmoothFlat(targetedCells, targetCellHeight + 1, HeightFloodMode.Up, AllowSteep);
    }

    private bool CanCreateSmallHill(int height)
    {
        bool canCreateSmallHill = true;

        BrushSize.DoForBrushSize(offset =>
        {
            if (!canCreateSmallHill)
                return;

            var otherCell = Map.GetTile(OriginCell + offset);
            if (otherCell == null)
            {
                canCreateSmallHill = false;
                return;
            }

            var subTile = Map.TheaterInstance.GetTile(otherCell.TileIndex).GetSubTile(otherCell.SubTileIndex);
            if (!IsCellMorphable(otherCell) || otherCell.Level != height || subTile.TmpImage.RampType != RampType.None)
                canCreateSmallHill = false;
        });

        return canCreateSmallHill;
    }

    private void CreateSmallHill(Point2D originCell)
    {
        // Raise the corner shared by all four cells, turning each of them into a corner ramp.
        var field = new CornerHeightField(Map, originCell.X, originCell.Y, originCell.X + 1, originCell.Y + 1);
        field.Build();

        var sharedCorner = new Point2D(originCell.X + 1, originCell.Y + 1);
        if (!field.TrySeedAdjustCorner(sharedCorner, 1))
            return;

        RunSmoothing(field, HeightFloodMode.Up, AllowSteep);
    }
}
