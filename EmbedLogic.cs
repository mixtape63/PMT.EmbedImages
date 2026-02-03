using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;


using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using ThreadingTimer = System.Threading.Timer;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PMT.EmbedImages
{
    internal static class EmbedLogic
    {
        private class Occurrence
        {
            public string FileName;
            public string ResolvedPath;
            public Point3d TargetMin;
            public Point3d TargetMax;
            public ObjectId RasterIdToErase;
        }

        public static void Run(Document doc, string outPath)
        {
            var ed = doc.Editor;
            var db = doc.Database;

            string dwgPath = db.Filename ?? "";
            string dwgFolder = Path.GetDirectoryName(dwgPath) ?? "";

            string logDir = Path.GetDirectoryName(outPath) ?? Path.GetTempPath();
            Directory.CreateDirectory(logDir);

            string logMain = Path.Combine(logDir, "PMT_EMBED_ONE_log.csv");
            string logErr = Path.Combine(logDir, "PMT_EMBED_ONE_errors.txt");
            string logMiss = Path.Combine(logDir, "PMT_EMBED_ONE_missing.csv");

            using (var wLog = new StreamWriter(logMain, true, Encoding.UTF8) { AutoFlush = true })
            using (var wErr = new StreamWriter(logErr, true, Encoding.UTF8) { AutoFlush = true })
            using (var wMiss = new StreamWriter(logMiss, true, Encoding.UTF8) { AutoFlush = true })
            {
                if (new FileInfo(logMain).Length == 0)
                    wLog.WriteLine("Timestamp;Stage;DWG;Layout;FileName;Message");
                if (new FileInfo(logMiss).Length == 0)
                    wMiss.WriteLine("Timestamp;DWG;Layout;FileName;ExpectedPath");
                if (new FileInfo(logErr).Length == 0)
                    wErr.WriteLine("Timestamp | DWG | Layout | FileName | Error");

                try
                {
                    SetSysVarSafe("FILEDIA", 0);
                    SetSysVarSafe("CMDDIA", 0);
                    SetSysVarSafe("PICKFIRST", 1);
                    SetSysVarSafe("OLEFRAME", 0);
                    SetSysVarSafe("OLEHIDE", 0);
                    SetSysVarSafe("OLEQUALITY", 2);

                    Log(wLog, "START", dwgPath, "Layout1", "", "Begin");

                    // 1) Скан Layout1
                    var occAll = BuildOccurrences_Layout1Only(doc, dwgFolder, wMiss, wErr);
                    Log(wLog, "SCAN_DONE", dwgPath, "Layout1", "", $"Occurrences={occAll.Count}");

                    // Если нет картинок — просто SaveAs и всё
                    if (occAll.Count == 0)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? logDir);
                        db.SaveAs(outPath, DwgVersion.Current);
                        Log(wLog, "SAVE_OK", dwgPath, "Layout1", "", outPath);
                        try { doc.CloseAndDiscard(); } catch { }
                        Log(wLog, "END", dwgPath, "Layout1", "", "Done (no images)");
                        return;
                    }

                    // 2) Активировать Layout1 (ВАЖНО: без _Set)
                    //Log(wLog, "CMD_LAYOUT_SET", dwgPath, "Layout1", "", "Running -LAYOUT Set Layout1");
                    //SafeCommand(ed, "_.-LAYOUT", "Set", "Layout1");
                    //WaitLikeHuman();
                    Log(wLog, "CMD_PSPACE", dwgPath, "Layout1", "", "Switch to paperspace (API)");
                    ed.SwitchToPaperSpace();          // <-- вот это
                    WaitLikeHuman();
                    Log(wLog, "CMD_PSPACE_DONE", dwgPath, "Layout1", "", "OK");

                    // 3) Группировка по картинке
                    var groups = occAll
                        .GroupBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    bool allOk = true;

                    foreach (var g in groups)
                    {
                        string fileName = g.Key;
                        var list = g.ToList();

                        // Clipboard.SetImage один раз
                        try
                        {
                            Log(wLog, "CLIPBOARD_BEFORE", dwgPath, "Layout1", fileName, list[0].ResolvedPath);
                            using (var bmp = LoadBitmapNoLock(list[0].ResolvedPath))
                            {
                                Clipboard.SetImage(bmp);
                            }
                            Log(wLog, "CLIPBOARD_OK", dwgPath, "Layout1", fileName, "SetImage");
                        }
                        catch (System.Exception ex)
                        {
                            allOk = false;
                            wErr.WriteLine($"{Ts()} | {dwgPath} | Layout1 | {fileName} | CLIPBOARD ERROR: {ex.Message}");
                            continue;
                        }

                        WaitLikeHuman();

                        foreach (var o in list)
                        {
                            bool ok = InsertMoveScale_NoDrawOrder(doc, o, wLog, wErr);
                            if (!ok) allOk = false;
                        }

                        try { Clipboard.Clear(); } catch { }
                        WaitLikeHuman();
                    }

                    Log(wLog, "PASTE_DONE", dwgPath, "Layout1", "", $"AllOk={allOk}");

                    // 4) Удаляем RasterImage только если 100% успех
                    if (allOk)
                    {
                        var toErase = occAll.Select(x => x.RasterIdToErase)
                            .Where(id => !id.IsNull)
                            .Distinct()
                            .ToList();

                        int erased = EraseRasterImages(doc, toErase, wErr);
                        Log(wLog, "ERASE_DONE", dwgPath, "Layout1", "", $"Erased={erased}");
                    }
                    else
                    {
                        Log(wLog, "ERASE_SKIPPED", dwgPath, "Layout1", "", "Skipped erase because not all inserts succeeded");
                    }

                    // 5) SaveAs
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? logDir);
                    db.SaveAs(outPath, DwgVersion.Current);
                    Log(wLog, "SAVE_OK", dwgPath, "Layout1", "", outPath);

                    try { doc.CloseAndDiscard(); } catch { }
                    Log(wLog, "END", dwgPath, "Layout1", "", "Done");
                }
                catch (Autodesk.AutoCAD.Runtime.Exception exAcad)
                {
                    wErr.WriteLine($"{Ts()} | {dwgPath} | Layout1 | (n/a) | FATAL: {exAcad.ErrorStatus}");
                    try { doc.CloseAndDiscard(); } catch { }
                }
                catch (System.Exception ex)
                {
                    wErr.WriteLine($"{Ts()} | {dwgPath} | Layout1 | (n/a) | FATAL: {ex.Message}");
                    try { doc.CloseAndDiscard(); } catch { }
                }
            }
        }

        // -------- Layout1 scan --------
        private static List<Occurrence> BuildOccurrences_Layout1Only(Document doc, string dwgFolder, StreamWriter wMiss, StreamWriter wErr)
        {
            var db = doc.Database;
            var result = new List<Occurrence>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                if (!layoutDict.Contains("Layout1"))
                {
                    tr.Commit();
                    return result;
                }

                var layoutId = layoutDict.GetAt("Layout1");
                var layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);
                var ps = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                RXClass rasterClass = RXClass.GetClass(typeof(RasterImage));
                RXClass brClass = RXClass.GetClass(typeof(BlockReference));

                // A) RasterImage на листе
                foreach (ObjectId id in ps)
                {
                    if (id.ObjectClass != rasterClass) continue;

                    var img = (RasterImage)tr.GetObject(id, OpenMode.ForRead);
                    string fileName = GetImageFileName(tr, img);
                    if (string.IsNullOrWhiteSpace(fileName)) continue;

                    string resolved = Path.Combine(dwgFolder, fileName);
                    if (!File.Exists(resolved))
                    {
                        wMiss.WriteLine($"{Ts()};{db.Filename};Layout1;{fileName};{resolved}");
                        continue;
                    }

                    Extents3d ext;
                    try { ext = img.GeometricExtents; } catch { continue; }

                    result.Add(new Occurrence
                    {
                        FileName = fileName,
                        ResolvedPath = resolved,
                        TargetMin = ext.MinPoint,
                        TargetMax = ext.MaxPoint,
                        RasterIdToErase = id
                    });
                }

                // B) RasterImage в block definition, вставленном на лист
                foreach (ObjectId entId in ps)
                {
                    if (!entId.ObjectClass.IsDerivedFrom(brClass)) continue;

                    var br = (BlockReference)tr.GetObject(entId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);

                    foreach (ObjectId bid in btr)
                    {
                        if (bid.ObjectClass != rasterClass) continue;

                        var imgInBlock = (RasterImage)tr.GetObject(bid, OpenMode.ForRead);
                        string fileName = GetImageFileName(tr, imgInBlock);
                        if (string.IsNullOrWhiteSpace(fileName)) continue;

                        string resolved = Path.Combine(dwgFolder, fileName);
                        if (!File.Exists(resolved))
                        {
                            wMiss.WriteLine($"{Ts()};{db.Filename};Layout1;{fileName};{resolved}");
                            continue;
                        }

                        Extents3d extLocal;
                        try { extLocal = imgInBlock.GeometricExtents; } catch { continue; }

                        TransformExtentsToMinMax(br.BlockTransform, extLocal.MinPoint, extLocal.MaxPoint, out var wMin, out var wMax);

                        result.Add(new Occurrence
                        {
                            FileName = fileName,
                            ResolvedPath = resolved,
                            TargetMin = wMin,
                            TargetMax = wMax,
                            RasterIdToErase = bid
                        });
                    }
                }

                tr.Commit();
            }

            return result
                .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TargetMin.X)
                .ThenBy(x => x.TargetMin.Y)
                .ToList();
        }

        // -------- Insert "как руками" без DRAWORDER --------
        private static bool InsertMoveScale_NoDrawOrder(Document doc, Occurrence o, StreamWriter wLog, StreamWriter wErr)
        {
            var ed = doc.Editor;
            var db = doc.Database;
            string dwg = db.Filename ?? "";

            try
            {
                Log(wLog, "BEFORE_PASTE", dwg, "Layout1", o.FileName, "");

                // PASTECLIP
                Log(wLog, "CMD_PASTECLIP", dwg, "Layout1", o.FileName, "");
                SafeCommand(ed, "_.PASTECLIP", o.TargetMin);
                WaitLikeHuman();
                SafeCommand(ed, "_.REGEN");
                WaitLikeHuman();

                // last OLE
                ObjectId oleId = FindLastOle2Frame(ed, db);
                if (oleId.IsNull)
                {
                    wErr.WriteLine($"{Ts()} | {dwg} | Layout1 | {o.FileName} | PASTE: Ole2Frame not found");
                    return false;
                }

                // extents
                if (!TryGetEntityExtentsWithRetry(db, oleId, out var curMin, out var curMax))
                {
                    wErr.WriteLine($"{Ts()} | {dwg} | Layout1 | {o.FileName} | OLE extents timeout");
                    return false;
                }

                // MOVE last (ВАЖНО: "L" без подчёркивания)
                Log(wLog, "CMD_MOVE", dwg, "Layout1", o.FileName, "");
                SafeCommand(ed, "_.MOVE", "L", "", curMin, o.TargetMin);
                WaitLikeHuman();
                SafeCommand(ed, "_.REGEN");
                WaitLikeHuman();

                // extents after move
                if (!TryGetEntityExtentsWithRetry(db, oleId, out curMin, out curMax))
                {
                    wErr.WriteLine($"{Ts()} | {dwg} | Layout1 | {o.FileName} | OLE extents timeout after MOVE");
                    return false;
                }

                // SCALE last (ВАЖНО: "L" без подчёркивания)
                double curW = curMax.X - curMin.X;
                double curH = curMax.Y - curMin.Y;
                double tgtW = o.TargetMax.X - o.TargetMin.X;
                double tgtH = o.TargetMax.Y - o.TargetMin.Y;

                if (curW > 1e-9 && curH > 1e-9 && tgtW > 1e-9 && tgtH > 1e-9)
                {
                    double sx = tgtW / curW;
                    double sy = tgtH / curH;
                    double scale = Math.Min(sx, sy);

                    Log(wLog, "CMD_SCALE", dwg, "Layout1", o.FileName, scale.ToString("0.########", CultureInfo.InvariantCulture));
                    SafeCommand(ed, "_.SCALE", "L", "", o.TargetMin, scale);
                    WaitLikeHuman();
                    SafeCommand(ed, "_.REGEN");
                    WaitLikeHuman();

                    // move correction
                    if (TryGetEntityExtentsWithRetry(db, oleId, out curMin, out curMax))
                    {
                        Log(wLog, "CMD_MOVE_FIX", dwg, "Layout1", o.FileName, "");
                        SafeCommand(ed, "_.MOVE", "L", "", curMin, o.TargetMin);
                        WaitLikeHuman();
                    }
                }

                Log(wLog, "PASTE_OK", dwg, "Layout1", o.FileName, "OK");
                return true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception exAcad)
            {
                wErr.WriteLine($"{Ts()} | {dwg} | Layout1 | {o.FileName} | INSERT ERROR: {exAcad.ErrorStatus}");
                return false;
            }
            catch (System.Exception ex)
            {
                wErr.WriteLine($"{Ts()} | {dwg} | Layout1 | {o.FileName} | INSERT ERROR: {ex.Message}");
                return false;
            }
        }

        private static void SafeCommand(Editor ed, params object[] args)
        {
            ed.Command(args);
        }

        private static void WaitLikeHuman()
        {
            Thread.Sleep(250);
        }

        private static bool TryGetEntityExtentsWithRetry(Database db, ObjectId entId, out Point3d min, out Point3d max)
        {
            min = Point3d.Origin;
            max = Point3d.Origin;

            for (int i = 0; i < 20; i++)
            {
                try
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent != null)
                        {
                            var ext = ent.GeometricExtents;
                            min = ext.MinPoint;
                            max = ext.MaxPoint;
                            tr.Commit();

                            double w = max.X - min.X;
                            double h = max.Y - min.Y;
                            if (w > 1e-9 && h > 1e-9)
                                return true;
                        }
                        tr.Commit();
                    }
                }
                catch { }

                Thread.Sleep(200);
            }

            return false;
        }

        private static ObjectId FindLastOle2Frame(Editor ed, Database db)
        {
            try
            {
                var sel = ed.SelectLast();
                if (sel.Status != PromptStatus.OK || sel.Value == null || sel.Value.Count == 0)
                    return ObjectId.Null;

                ObjectId ole = ObjectId.Null;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var id in sel.Value.GetObjectIds())
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent is Ole2Frame)
                        {
                            ole = id;
                            break;
                        }
                    }
                    tr.Commit();
                }
                return ole;
            }
            catch { return ObjectId.Null; }
        }

        private static int EraseRasterImages(Document doc, List<ObjectId> ids, StreamWriter wErr)
        {
            if (ids == null || ids.Count == 0) return 0;

            int erased = 0;
            var db = doc.Database;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    try
                    {
                        if (id.IsNull) continue;

                        var img = tr.GetObject(id, OpenMode.ForWrite) as RasterImage;
                        if (img == null || img.IsErased) continue;

                        LayerTableRecord ltr = null;
                        bool wasLocked = false;

                        try
                        {
                            ltr = (LayerTableRecord)tr.GetObject(img.LayerId, OpenMode.ForWrite);
                            wasLocked = ltr.IsLocked;
                            if (wasLocked) ltr.IsLocked = false;
                        }
                        catch { }

                        try
                        {
                            img.Erase(true);
                            erased++;
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception exErase)
                        {
                            wErr.WriteLine($"{Ts()} | {db.Filename} | Layout1 | (n/a) | ERASE ERROR: {exErase.ErrorStatus}");
                        }

                        try { if (ltr != null && wasLocked) ltr.IsLocked = true; } catch { }
                    }
                    catch { }
                }

                tr.Commit();
            }

            return erased;
        }

        private static Bitmap LoadBitmapNoLock(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var img = System.Drawing.Image.FromStream(fs))
            {
                return new Bitmap(img);
            }
        }

        private static string GetImageFileName(Transaction tr, RasterImage img)
        {
            try
            {
                var defId = img.ImageDefId;
                if (defId.IsNull) return "";
                var def = (RasterImageDef)tr.GetObject(defId, OpenMode.ForRead);
                string src = (def.SourceFileName ?? "").Trim().Trim('"');
                return Path.GetFileName(src);
            }
            catch { return ""; }
        }

        private static void TransformExtentsToMinMax(Matrix3d m, Point3d localMin, Point3d localMax, out Point3d wMin, out Point3d wMax)
        {
            var p1 = new Point3d(localMin.X, localMin.Y, 0).TransformBy(m);
            var p2 = new Point3d(localMax.X, localMin.Y, 0).TransformBy(m);
            var p3 = new Point3d(localMax.X, localMax.Y, 0).TransformBy(m);
            var p4 = new Point3d(localMin.X, localMax.Y, 0).TransformBy(m);

            double minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            double minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            double maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            double maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            wMin = new Point3d(minX, minY, 0);
            wMax = new Point3d(maxX, maxY, 0);
        }

        private static void SetSysVarSafe(string name, object value)
        {
            try { AcadApp.SetSystemVariable(name, value); } catch { }
        }

        private static string Ts() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private static void Log(StreamWriter w, string stage, string dwg, string layout, string file, string msg)
        {
            w.WriteLine($"{Ts()};{stage};{dwg};{layout};{file};{msg}");
        }
    }
}
