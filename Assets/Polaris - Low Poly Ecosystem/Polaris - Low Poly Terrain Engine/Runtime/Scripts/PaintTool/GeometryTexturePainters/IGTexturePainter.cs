using System.Collections.Generic;

namespace Pinwheel.Griffin.PaintTool
{
    public interface IGTexturePainter
    {
        string HistoryPrefix { get; }
        string Instruction { get; }
        void Paint(GStylizedTerrain terrain, GTexturePainterArgs args);
        List<GTerrainResourceFlag> GetResourceFlagForHistory(GTexturePainterArgs args);
    }
}
