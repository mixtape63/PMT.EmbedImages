using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PMT.EmbedImages
{
    public class Commands
    {
        [CommandMethod("PMT_PING", CommandFlags.Session)]
        public void Ping()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.Editor.WriteMessage("\nPMT_PING OK\n");
        }

        // Runner/скрипт передаёт путь out.dwg следующей строкой
        [CommandMethod("PMT_EMBED_ONE", CommandFlags.Session)]
        public void EmbedOne()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var p = ed.GetString("\nOutput DWG path: ");
            if (p.Status != PromptStatus.OK) return;

            string outPath = (p.StringResult ?? "").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(outPath))
            {
                ed.WriteMessage("\nPMT_EMBED_ONE: empty outPath.\n");
                return;
            }

            EmbedLogic.Run(doc, outPath);
        }
    }
}
