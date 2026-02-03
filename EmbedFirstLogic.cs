using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PMT.EmbedImages
{
    // v14:
    // - RU point: "X;Y" when decimal separator is comma (prevents "Invalid 2D point" prompt).
    // - Save via Database.SaveAs API after paste/transform completes (avoids SAVEAS command locale/prompts).
    // - Thread-safe: no background timers; uses Application.Idle state machine.
    internal static class EmbedFirstLogic
    {
        private const int POST_PASTE_DELAY_MS = 1200;
        private const int WATCHDOG_MS = 180000;

        private enum Stage { None=0, PasteSent=1, WaitAfterPaste=2, Done=3 }

        private class PendingJob
        {
            public Document Doc;
            public Database Db;
            public Editor Ed;

            public string DwgPath;
            public string OutPath;

            public string FileName;
            public string ResolvedPath;

            public Point3d TargetMin;
            public Point3d TargetMax;

            public long MaxHandleBefore;

            public DateTime StartedAtUtc;
            public DateTime DueAtUtc;

            public Stage CurStage;

            public StreamWriter WLog;
            public StreamWriter WErr;
            public StreamWriter WMiss;

            public CommandEventHandler WillStart;
            public CommandEventHandler DidEnd;

            public EventHandler IdleHandler;
        }

        private static PendingJob _job;

        private static readonly Type TEmbedLogic = Type.GetType("PMT.EmbedImages.EmbedLogic, PMT.EmbedImages");
        private static readonly MethodInfo MBuildOcc = TEmbedLogic?.GetMethod("BuildOccurrences_Layout1Only", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo MLoadBmp   = TEmbedLogic?.GetMethod("LoadBitmapNoLock", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo MTs        = TEmbedLogic?.GetMethod("Ts", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo MLog       = TEmbedLogic?.GetMethod("Log", BindingFlags.NonPublic | BindingFlags.Static);

        public static void RunFirst(Document doc, string outPath)
        {
            if (_job != null)
            {
                try { doc.Editor.WriteMessage("\nPMT_EMBED_FIRST: job already running.\n"); } catch { }
                return;
            }

            var ed = doc.Editor;
            var db = doc.Database;

            string dwgPath = db.Filename ?? "";
            string dwgFolder = Path.GetDirectoryName(dwgPath) ?? "";

            string logDir = Path.GetDirectoryName(outPath) ?? Path.GetTempPath();
            Directory.CreateDirectory(logDir);

            string logMain = Path.Combine(logDir, "PMT_EMBED_FIRST_log.csv");
            string logErr  = Path.Combine(logDir, "PMT_EMBED_FIRST_errors.txt");
            string logMiss = Path.Combine(logDir, "PMT_EMBED_FIRST_missing.csv");

            var wLog = new StreamWriter(logMain, true, Encoding.UTF8) { AutoFlush = true };
            var wErr = new StreamWriter(logErr,  true, Encoding.UTF8) { AutoFlush = true };
            var wMiss= new StreamWriter(logMiss, true, Encoding.UTF8) { AutoFlush = true };

            if (new FileInfo(logMain).Length == 0)
                wLog.WriteLine("Timestamp;Stage;DWG;Layout;FileName;Message");

            try
            {
                SetSysVarSafe("FILEDIA", 0);
                SetSysVarSafe("CMDDIA", 0);
                SetSysVarSafe("OLEHIDE", 0);
                SetSysVarSafe("OLEFRAME", 2);

                Log(wLog, "START", dwgPath, "Layout1", "", "Begin");

                IList occAll;
                using (doc.LockDocument())
                {
                    occAll = BuildOccurrences(doc, dwgFolder, wMiss, wErr);
                }
                Log(wLog, "SCAN_DONE", dwgPath, "Layout1", "", $"Occurrences={occAll.Count}");

                if (occAll.Count == 0)
                {
                    Log(wLog, "END", dwgPath, "Layout1", "", "Done (no images)");
                    wLog.Dispose(); wErr.Dispose(); wMiss.Dispose();
                    return;
                }

                object occ = occAll[0];
                string fileName = GetStringField(occ, "FileName");
                string resolved = GetStringField(occ, "ResolvedPath");
                Point3d targetMin = GetPoint3dField(occ, "TargetMin");
                Point3d targetMax = GetPoint3dField(occ, "TargetMax");

                double tgtW = targetMax.X - targetMin.X;
                double tgtH = targetMax.Y - targetMin.Y;

                Log(wLog, "PICK_FIRST", dwgPath, "Layout1", fileName, resolved);
                Log(wLog, "TARGET_XYWH", dwgPath, "Layout1", fileName,
                    string.Format(CultureInfo.InvariantCulture, "x={0} y={1} w={2} h={3}", targetMin.X, targetMin.Y, tgtW, tgtH));

                using (doc.LockDocument())
                {
                    try { ed.SwitchToPaperSpace(); } catch { }
                }

                using (var bmp = LoadBitmap(resolved))
                {
                    Clipboard.SetImage(bmp);
                }

                long maxHandleBefore;
                using (doc.LockDocument())
                {
                    maxHandleBefore = GetMaxHandleInPaperSpace(db);
                }

                _job = new PendingJob
                {
                    Doc = doc,
                    Db = db,
                    Ed = ed,
                    DwgPath = dwgPath,
                    OutPath = outPath,
                    FileName = fileName,
                    ResolvedPath = resolved,
                    TargetMin = targetMin,
                    TargetMax = targetMax,
                    MaxHandleBefore = maxHandleBefore,
                    WLog = wLog,
                    WErr = wErr,
                    WMiss = wMiss,
                    StartedAtUtc = DateTime.UtcNow,
                    CurStage = Stage.PasteSent
                };

                _job.WillStart = (s, e) =>
                {
                    try
                    {
                        var n = (e?.GlobalCommandName ?? "").ToUpperInvariant();
                        if (n.Contains("PASTECLIP"))
                            Log(_job.WLog, "PASTE_STATUS", _job.DwgPath, "Layout1", _job.FileName, "start=True");
                    }
                    catch { }
                };

                _job.DidEnd = (s, e) =>
                {
                    try
                    {
                        var n = (e?.GlobalCommandName ?? "").ToUpperInvariant();
                        if (n.Contains("PASTECLIP"))
                        {
                            Log(_job.WLog, "PASTE_STATUS", _job.DwgPath, "Layout1", _job.FileName, "end=True");
                            _job.CurStage = Stage.WaitAfterPaste;
                            _job.DueAtUtc = DateTime.UtcNow.AddMilliseconds(POST_PASTE_DELAY_MS);
                        }
                    }
                    catch { }
                };

                _job.IdleHandler = (s, e) => OnIdle();

                doc.CommandWillStart += _job.WillStart;
                doc.CommandEnded += _job.DidEnd;
                doc.CommandCancelled += _job.DidEnd;
                doc.CommandFailed += _job.DidEnd;

                AcadApp.Idle += _job.IdleHandler;

                string pt = FormatPointForCmd(targetMin);
                Log(wLog, "PASTE_POINT", dwgPath, "Layout1", fileName, pt);

                Log(wLog, "CMD_PASTECLIP", dwgPath, "Layout1", fileName, "");
                string cmdPaste = "_.PASTECLIP\n" + pt + "\n";
                doc.SendStringToExecute(cmdPaste, true, false, false);
            }
            catch (System.Exception ex)
            {
                try { wErr.WriteLine($"{Ts()} | {dwgPath} | Layout1 | (n/a) | FATAL: {ex.Message}"); } catch { }
                CleanupJob();
            }
        }

        private static void OnIdle()
        {
            var j = _job;
            if (j == null) return;

            if ((DateTime.UtcNow - j.StartedAtUtc).TotalMilliseconds > WATCHDOG_MS)
            {
                try { j.WErr.WriteLine($"{Ts()} | {j.DwgPath} | Layout1 | {j.FileName} | watchdog timeout"); } catch { }
                CleanupJob();
                return;
            }

            if (j.CurStage == Stage.WaitAfterPaste && DateTime.UtcNow >= j.DueAtUtc)
            {
                try
                {
                    using (j.Doc.LockDocument())
                    {
                        SetSysVarSafe("OLEHIDE", 0);
                        SetSysVarSafe("OLEFRAME", 2);

                        var insertedId = FindInsertedEntityByHandle(j.Db, j.MaxHandleBefore, out string insertedType);
                        if (insertedId.IsNull)
                        {
                            j.WErr.WriteLine($"{Ts()} | {j.DwgPath} | Layout1 | {j.FileName} | Inserted entity not found after paste");
                        }
                        else
                        {
                            Log(j.WLog, "PASTE_ENTITY", j.DwgPath, "Layout1", j.FileName, insertedType);

                            if (TryGetEntityExtentsWithRetry(j.Db, insertedId, out var curMin, out var curMax, attempts: 120, sleepMs: 50))
                            {
                                double curW = curMax.X - curMin.X;
                                double curH = curMax.Y - curMin.Y;

                                double tgtW = j.TargetMax.X - j.TargetMin.X;
                                double tgtH = j.TargetMax.Y - j.TargetMin.Y;

                                Log(j.WLog, "CUR_XYWH", j.DwgPath, "Layout1", j.FileName,
                                    string.Format(CultureInfo.InvariantCulture, "x={0} y={1} w={2} h={3}", curMin.X, curMin.Y, curW, curH));

                                TransformEntity(j.Db, insertedId, Matrix3d.Displacement(j.TargetMin - curMin));

                                if (TryGetEntityExtentsWithRetry(j.Db, insertedId, out curMin, out curMax, attempts: 120, sleepMs: 50))
                                {
                                    curW = curMax.X - curMin.X;
                                    curH = curMax.Y - curMin.Y;

                                    if (curW > 1e-9 && curH > 1e-9 && tgtW > 1e-9 && tgtH > 1e-9)
                                    {
                                        double sx = tgtW / curW;
                                        double sy = tgtH / curH;
                                        double scale = Math.Sqrt(sx * sy);

                                        Log(j.WLog, "SET_WH", j.DwgPath, "Layout1", j.FileName,
                                            string.Format(CultureInfo.InvariantCulture, "sx={0} sy={1} use={2}", sx, sy, scale));

                                        TransformEntity(j.Db, insertedId, Matrix3d.Scaling(scale, j.TargetMin));
                                    }
                                }

                                if (TryGetEntityExtentsWithRetry(j.Db, insertedId, out curMin, out curMax, attempts: 120, sleepMs: 50))
                                {
                                    TransformEntity(j.Db, insertedId, Matrix3d.Displacement(j.TargetMin - curMin));
                                }
                            }

                            try { j.Ed.Command("_.REGENALL"); } catch { }
                        }

                        // API SaveAs (no command prompts, locale-safe)
                        try
                        {
                            if (File.Exists(j.OutPath))
                            {
                                File.Delete(j.OutPath);
                                Log(j.WLog, "SAVE_PREDELETE", j.DwgPath, "Layout1", j.FileName, j.OutPath);
                            }
                        }
                        catch (System.Exception exDel)
                        {
                            try { j.WErr.WriteLine($"{Ts()} | {j.DwgPath} | Layout1 | {j.FileName} | delete outPath failed: {exDel.Message}"); } catch { }
                        }

                        try { Directory.CreateDirectory(Path.GetDirectoryName(j.OutPath) ?? Path.GetTempPath()); } catch { }

                        Log(j.WLog, "SAVE_API_BEGIN", j.DwgPath, "Layout1", j.FileName, j.OutPath);
                        j.Db.SaveAs(j.OutPath, DwgVersion.Current);
                        Log(j.WLog, "SAVE_API_END", j.DwgPath, "Layout1", j.FileName, j.OutPath);

                        try { Clipboard.Clear(); } catch { }
                    }

                    Log(j.WLog, "END", j.DwgPath, "Layout1", j.FileName, "Done");
                    j.CurStage = Stage.Done;
                    CleanupJob();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception exAcad)
                {
                    try { j.WErr.WriteLine($"{Ts()} | {j.DwgPath} | Layout1 | (n/a) | FATAL: {exAcad.ErrorStatus}"); } catch { }
                    CleanupJob();
                }
                catch (System.Exception ex)
                {
                    try { j.WErr.WriteLine($"{Ts()} | {j.DwgPath} | Layout1 | (n/a) | FATAL: {ex.Message}"); } catch { }
                    CleanupJob();
                }
            }
        }

        private static void CleanupJob()
        {
            var j = _job;
            _job = null;
            if (j == null) return;

            try
            {
                try { j.Doc.CommandWillStart -= j.WillStart; } catch { }
                try { j.Doc.CommandEnded -= j.DidEnd; } catch { }
                try { j.Doc.CommandCancelled -= j.DidEnd; } catch { }
                try { j.Doc.CommandFailed -= j.DidEnd; } catch { }
            }
            catch { }

            try { if (j.IdleHandler != null) AcadApp.Idle -= j.IdleHandler; } catch { }

            try { j.WLog?.Dispose(); } catch { }
            try { j.WErr?.Dispose(); } catch { }
            try { j.WMiss?.Dispose(); } catch { }
        }

        private static void SetSysVarSafe(string name, object value)
        {
            try { AcadApp.SetSystemVariable(name, value); } catch { }
        }

        private static string Ts()
        {
            if (MTs != null) return (string)MTs.Invoke(null, null);
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private static void Log(StreamWriter w, string stage, string dwg, string layout, string fileName, string message)
        {
            if (MLog != null)
            {
                MLog.Invoke(null, new object[] { w, stage, dwg, layout, fileName, message });
                return;
            }
            w.WriteLine($"{Ts()};{stage};{dwg};{layout};{fileName};{message}");
        }

        private static IList BuildOccurrences(Document doc, string dwgFolder, StreamWriter wMiss, StreamWriter wErr)
        {
            if (MBuildOcc == null) throw new InvalidOperationException("EmbedLogic.BuildOccurrences_Layout1Only not found (reflection).");
            return (IList)MBuildOcc.Invoke(null, new object[] { doc, dwgFolder, wMiss, wErr });
        }

        private static System.Drawing.Bitmap LoadBitmap(string path)
        {
            if (MLoadBmp != null) return (System.Drawing.Bitmap)MLoadBmp.Invoke(null, new object[] { path });
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var img = System.Drawing.Image.FromStream(fs))
            {
                return new System.Drawing.Bitmap(img);
            }
        }

        private static string GetStringField(object obj, string fieldName)
        {
            var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return "";
            return (f.GetValue(obj) as string) ?? "";
        }

        private static Point3d GetPoint3dField(object obj, string fieldName)
        {
            var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return Point3d.Origin;
            var v = f.GetValue(obj);
            return v is Point3d p ? p : Point3d.Origin;
        }

        // RU locale: decimal separator is ',' so AutoCAD expects point input "X;Y"
        private static string FormatPointForCmd(Point3d p)
        {
            var nfi = CultureInfo.CurrentCulture.NumberFormat;
            bool decComma = nfi.NumberDecimalSeparator == ",";
            string sep = decComma ? ";" : ",";

            string x = p.X.ToString("0.###############", CultureInfo.CurrentCulture).Replace(nfi.NumberGroupSeparator, "");
            string y = p.Y.ToString("0.###############", CultureInfo.CurrentCulture).Replace(nfi.NumberGroupSeparator, "");
            return x + sep + y;
        }

        private static long GetMaxHandleInPaperSpace(Database db)
        {
            long best = -1;
            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ps = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead);

                    foreach (ObjectId id in ps)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;
                        try
                        {
                            long h = Convert.ToInt64(ent.Handle.ToString(), 16);
                            if (h > best) best = h;
                        }
                        catch { }
                    }
                    tr.Commit();
                }
            }
            catch { }
            return best;
        }

        private static ObjectId FindInsertedEntityByHandle(Database db, long minHandleExclusive, out string typeName)
        {
            typeName = "";
            ObjectId bestOle = ObjectId.Null;
            long bestOleH = -1;

            ObjectId bestAny = ObjectId.Null;
            long bestAnyH = -1;

            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ps = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead);

                    foreach (ObjectId id in ps)
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        long h = -1;
                        try { h = Convert.ToInt64(ent.Handle.ToString(), 16); } catch { }
                        if (h <= minHandleExclusive) continue;

                        if (ent is Ole2Frame && h > bestOleH) { bestOleH = h; bestOle = id; }
                        if (h > bestAnyH) { bestAnyH = h; bestAny = id; }
                    }
                    tr.Commit();
                }
            }
            catch { }

            if (!bestOle.IsNull) { typeName = "Ole2Frame(handle)"; return bestOle; }
            if (!bestAny.IsNull) { typeName = "Entity(handle)"; return bestAny; }
            return ObjectId.Null;
        }

        private static bool TryGetEntityExtentsWithRetry(Database db, ObjectId entId, out Point3d min, out Point3d max, int attempts, int sleepMs)
        {
            min = Point3d.Origin;
            max = Point3d.Origin;

            for (int i = 0; i < attempts; i++)
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
                if (sleepMs > 0) System.Threading.Thread.Sleep(sleepMs);
            }

            return false;
        }

        private static bool TransformEntity(Database db, ObjectId id, Matrix3d m)
        {
            try
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent == null) { tr.Commit(); return false; }
                    ent.TransformBy(m);
                    tr.Commit();
                    return true;
                }
            }
            catch { return false; }
        }
    }
}
