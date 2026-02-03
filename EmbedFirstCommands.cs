using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PMT.EmbedImages
{
    public class EmbedFirstCommands
    {
        [CommandMethod("PMT_EMBED_FIRST", CommandFlags.Session)]
        public void EmbedFirst()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var p = ed.GetString("\nOutput DWG path: ");
            if (p.Status != PromptStatus.OK) return;

            string outPath = (p.StringResult ?? "").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(outPath))
            {
                ed.WriteMessage("\nPMT_EMBED_FIRST: empty outPath.\n");
                return;
            }

            EmbedFirstLogic.RunFirst(doc, outPath);
        }
    }
}
