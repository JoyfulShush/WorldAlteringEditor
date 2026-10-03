using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

public abstract class LowerGroundMutationBase : AlterElevationMutationBase
{
    public LowerGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
    {
    }

    /// <summary>
    /// Whether steep ramps may be created.
    /// </summary>
    protected abstract bool AllowSteep { get; }

    protected void LowerGround()
    {
        Clear();

        var targetCell = Map.GetTile(OriginCell);

        if (targetCell == null || targetCell.Level < 1 || !IsCellMorphable(targetCell))
            return;

        int targetCellHeight = targetCell.Level;

        // Lowering a flat cell always affects more than 1 cell,
        // so a 1-wide brush only lowers ramps.
        if (BrushSize.Width == 1 || BrushSize.Height == 1)
        {
            if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                return;
        }

        // The brush covers the whole crater including its ramps, so the flat bottom is 2 cells smaller.
        int xSize = Math.Max(0, BrushSize.Width - 2);
        int ySize = Math.Max(0, BrushSize.Height - 2);

        int beginY = OriginCell.Y - (ySize - 1) / 2;
        int endY = OriginCell.Y + ySize / 2;
        int beginX = OriginCell.X - (xSize - 1) / 2;
        int endX = OriginCell.X + xSize / 2;

        // Only lower ground on the same level as the target cell.
        var targetedCells = new List<Point2D>();
        for (int y = beginY; y <= endY; y++)
        {
            for (int x = beginX; x <= endX; x++)
            {
                var cellCoords = new Point2D(x, y);
                var cell = Map.GetTile(cellCoords);
                if (cell == null || cell.Level < 1)
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

        var changedCells = SmoothFlat(targetedCells, targetCellHeight - 1, HeightFloodMode.Down, AllowSteep);

        if (changedCells != null && AutoLATEnabled)
            ApplyAutoLAT(changedCells);
    }

    private void ApplyAutoLAT(List<MapTile> changedCells)
    {
        if (changedCells.Count == 0)
            return;

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (var cell in changedCells)
        {
            if (cell.X < minX) minX = cell.X;
            if (cell.Y < minY) minY = cell.Y;
            if (cell.X > maxX) maxX = cell.X;
            if (cell.Y > maxY) maxY = cell.Y;
        }

        ApplyGenericAutoLAT(minX - 1, minY - 1, maxX + 1, maxY + 1, AddCellToUndoData);
    }
}
