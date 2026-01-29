using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using Microsoft.Win32.SafeHandles;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using WinFormsApp = System.Windows.Forms.Application;

namespace PMT.EmbedImages
{
    public partial class MainForm : Form
    {
        private BatchRunner _runner;

        public MainForm()
        {
            InitializeComponent();

            cbSameFolder.Checked = true;
            rbNewFile.Checked = true;
            cbBackup.Checked = true;
            tbSuffix.Text = "_embedded";

            UpdateUiState();
            UpdateSelectedCount();
            UpdateExample();

            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_runner != null && _runner.IsRunning)
            {
                MessageBox.Show(this, "Идёт обработка. Дождитесь завершения.", "PMT",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }

            try { _runner?.DisposeSafe(); } catch { }
        }

        // =========================
        // WinAPI long-path helpers
        // =========================
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFileAttributesW(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private static string ToLongPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (path.StartsWith(@"\\?\")) return path;

            if (path.StartsWith(@"\\"))
                return @"\\?\UNC\" + path.Substring(2);

            return @"\\?\" + path;
        }

        internal static bool FileExistsLong(string path)
        {
            try
            {
                string p = ToLongPath(path);
                uint attr = GetFileAttributesW(p);
                if (attr == INVALID_FILE_ATTRIBUTES) return false;
                if ((attr & FILE_ATTRIBUTE_DIRECTORY) != 0) return false;
                return true;
            }
            catch { return false; }
        }

        private static Stream OpenReadLong(string path)
        {
            string p = ToLongPath(path);

            var handle = CreateFileW(
                p,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (handle.IsInvalid)
                throw new IOException("Cannot open file (CreateFileW): " + path);

            return new FileStream(handle, FileAccess.Read);
        }

        internal static Bitmap LoadBitmapLongPath(string path)
        {
            using (var s = OpenReadLong(path))
            using (var img = System.Drawing.Image.FromStream(s))
            {
                return new Bitmap(img); // clone
            }
        }

        // =========================
        // Safe scheduling
        // =========================
        internal static void RunInNextIdle(Action action)
        {
            EventHandler h = null;
            h = (s, e) =>
            {
                AcadApp.Idle -= h;
                try { action(); } catch { }
            };
            AcadApp.Idle += h;
        }

        internal static void RunWhenQuiescent(Document doc, Action action)
        {
            EventHandler h = null;
            h = (s, e) =>
            {
                try
                {
                    if (doc == null)
                    {
                        AcadApp.Idle -= h;
                        return;
                    }

                    if (!doc.Editor.IsQuiescent)
                        return;

                    AcadApp.Idle -= h;

                    AcadApp.DocumentManager.ExecuteInApplicationContext(_ =>
                    {
                        try { action(); } catch { }
                    }, null);
                }
                catch
                {
                    try { AcadApp.Idle -= h; } catch { }
                }
            };
            AcadApp.Idle += h;
        }

        internal static void SafeDbWrite(Document doc, Action body)
        {
            using (doc.LockDocument())
            {
                body();
            }
        }

        internal static void SetSysVarSafe(string name, object value)
        {
            try { AcadApp.SetSystemVariable(name, value); } catch { }
        }

        // =========================
        // UI handlers
        // =========================
        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Выберите папку с DWG";
                dlg.ShowNewFolderButton = false;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                tbFolder.Text = dlg.SelectedPath;
                gridFiles.Rows.Clear();

                var files = Directory.GetFiles(dlg.SelectedPath, "*.dwg").OrderBy(x => x);
                foreach (var f in files)
                {
                    int idx = gridFiles.Rows.Add(true, Path.GetFileName(f));
                    gridFiles.Rows[idx].Tag = f;
                }

                UpdateSelectedCount();
                UpdateExample();
            }
        }

        private void btnAll_Click(object sender, EventArgs e) => SetAllRowsChecked(true);
        private void btnNone_Click(object sender, EventArgs e) => SetAllRowsChecked(false);

        private void btnInvert_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                bool cur = (row.Cells[colDo.Index].Value as bool?) ?? true;
                row.Cells[colDo.Index].Value = !cur;
            }
            UpdateSelectedCount();
        }

        private void SetAllRowsChecked(bool value)
        {
            foreach (DataGridViewRow row in gridFiles.Rows)
                row.Cells[colDo.Index].Value = value;

            UpdateSelectedCount();
        }

        private void gridFiles_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridFiles.IsCurrentCellDirty)
                gridFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void gridFiles_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == colDo.Index) UpdateSelectedCount();
        }

        private void cbSameFolder_CheckedChanged(object sender, EventArgs e) => UpdateUiState();

        private void rbOverwrite_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUiState();
            UpdateExample();
        }

        private void rbNewFile_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUiState();
            UpdateExample();
        }

        private void tbPrefix_TextChanged(object sender, EventArgs e) => UpdateExample();
        private void tbSuffix_TextChanged(object sender, EventArgs e) => UpdateExample();

        private void btnBrowseOutFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Выберите папку для сохранения";
                dlg.ShowNewFolderButton = true;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                tbOutFolder.Text = dlg.SelectedPath;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (_runner != null && _runner.IsRunning)
            {
                MessageBox.Show(this, "Идёт обработка. Дождитесь завершения.", "PMT",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Close();
        }

        private void UpdateUiState()
        {
            if (rbOverwrite.Checked)
            {
                cbSameFolder.Checked = true;
                cbSameFolder.Enabled = false;
            }
            else
            {
                cbSameFolder.Enabled = true;
            }

            panelOutFolder.Enabled = !cbSameFolder.Checked;
            cbBackup.Enabled = rbOverwrite.Checked;

            bool newFile = rbNewFile.Checked;
            tbPrefix.Enabled = newFile;
            tbSuffix.Enabled = newFile;
            lblPrefix.Enabled = newFile;
            lblSuffix.Enabled = newFile;
            lblExample.Enabled = newFile;
        }

        private void UpdateSelectedCount()
        {
            int total = gridFiles.Rows.Count;
            int sel = 0;

            foreach (DataGridViewRow row in gridFiles.Rows)
            {
                bool v = (row.Cells[colDo.Index].Value as bool?) ?? false;
                if (v) sel++;
            }

            lblStatus.Text = $"Выбрано файлов: {sel} из {total}";
            btnStart.Enabled = sel > 0 && (_runner == null || !_runner.IsRunning);
        }

        private void UpdateExample()
        {
            if (!rbNewFile.Checked)
            {
                lblExample.Text = "Пример: (перезапись исходного файла)";
                return;
            }

            string prefix = tbPrefix.Text ?? "";
            string suffix = tbSuffix.Text ?? "";
            lblExample.Text = $"Пример: {prefix}A101{suffix}.dwg";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_runner != null && _runner.IsRunning)
                return;

            var selected = gridFiles.Rows.Cast<DataGridViewRow>()
                .Where(r => ((r.Cells[colDo.Index].Value as bool?) ?? false))
                .Select(r => r.Tag as string)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Не выбрано ни одного DWG.", "PMT",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string logFolder = (rbOverwrite.Checked || cbSameFolder.Checked) ? tbFolder.Text : tbOutFolder.Text;
            if (string.IsNullOrWhiteSpace(logFolder) || !Directory.Exists(logFolder))
            {
                MessageBox.Show(this, "Не выбрана папка для сохранения/логов.", "PMT",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var settings = new RunSettings
            {
                Overwrite = rbOverwrite.Checked,
                Backup = cbBackup.Checked,
                SameFolder = cbSameFolder.Checked,
                OutFolder = tbOutFolder.Text ?? "",
                Prefix = tbPrefix.Text ?? "",
                Suffix = tbSuffix.Text ?? "",
                LogFolder = logFolder
            };

            btnStart.Enabled = false;
            btnClose.Enabled = false;

            _runner = new BatchRunner(
                selected,
                settings,
                setStatus: s => { lblStatus.Text = s; WinFormsApp.DoEvents(); },
                onFinished: () =>
                {
                    btnClose.Enabled = true;
                    _runner = null;
                    UpdateSelectedCount();
                    MessageBox.Show(this, "Готово.", "PMT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

            _runner.Start();
        }

        // =========================
        // Data models
        // =========================
        private class RunSettings
        {
            public bool Overwrite;
            public bool Backup;
            public bool SameFolder;
            public string OutFolder;
            public string Prefix;
            public string Suffix;
            public string LogFolder;
        }

        private class WorkItem
        {
            public string LayoutName;
            public ObjectId ImageIdToErase; // if direct layout RasterImage
            public string FileName;
            public string ImagePath;
            public Point3d TargetMin;
            public Point3d TargetMax;
        }

        private class BlockImageDef
        {
            public string BlockName;
            public ObjectId RasterImageId; // erase later from block definition
            public string FileName;
            public string ImagePath;
            public Point3d LocalMin;
            public Point3d LocalMax;
        }

        // =========================
        // BatchRunner
        // =========================
        private class BatchRunner
        {
            private readonly List<string> _dwgs;
            private readonly RunSettings _s;
            private readonly Action<string> _status;
            private readonly Action _onFinished;

            private int _i = -1;
            public bool IsRunning { get; private set; }

            private StreamWriter _report;
            private StreamWriter _errors;
            private StreamWriter _missing;

            // sysvars restore
            private object _origFILEDIA;
            private object _origCMDDIA;
            private bool _captured;

            private bool _openingNext;

            public BatchRunner(List<string> dwgs, RunSettings s, Action<string> setStatus, Action onFinished)
            {
                _dwgs = dwgs;
                _s = s;
                _status = setStatus;
                _onFinished = onFinished;
            }

            public void Start()
            {
                IsRunning = true;
                OpenLogs();
                CaptureSysvarsOnce();

                MainForm.SetSysVarSafe("FILEDIA", 0);
                MainForm.SetSysVarSafe("CMDDIA", 0);
                MainForm.SetSysVarSafe("OLEFRAME", 0);
                MainForm.SetSysVarSafe("OLEHIDE", 0);
                MainForm.SetSysVarSafe("OLEQUALITY", 2);

                _openingNext = false;
                NextDwg();
            }

            public void DisposeSafe()
            {
                try { RestoreSysvars(); } catch { }
                try { CloseLogs(); } catch { }
                IsRunning = false;
            }

            private void CaptureSysvarsOnce()
            {
                if (_captured) return;

                try { _origFILEDIA = AcadApp.GetSystemVariable("FILEDIA"); } catch { _origFILEDIA = null; }
                try { _origCMDDIA = AcadApp.GetSystemVariable("CMDDIA"); } catch { _origCMDDIA = null; }

                _captured = true;
            }

            private void RestoreSysvars()
            {
                if (!_captured) return;

                try { if (_origFILEDIA != null) AcadApp.SetSystemVariable("FILEDIA", _origFILEDIA); } catch { }
                try { if (_origCMDDIA != null) AcadApp.SetSystemVariable("CMDDIA", _origCMDDIA); } catch { }
            }

            private void NextDwg()
            {
                if (_openingNext) return;
                _openingNext = true;

                MainForm.RunInNextIdle(() =>
                {
                    AcadApp.DocumentManager.ExecuteInApplicationContext(_ =>
                    {
                        _openingNext = false;

                        _i++;
                        if (_i >= _dwgs.Count)
                        {
                            RestoreSysvars();
                            CloseLogs();
                            IsRunning = false;
                            _onFinished?.Invoke();
                            return;
                        }

                        string dwg = _dwgs[_i];
                        if (string.IsNullOrWhiteSpace(dwg) || !MainForm.FileExistsLong(dwg))
                        {
                            WriteReport(dwg, "", "ERROR", "DWG not found");
                            NextDwg();
                            return;
                        }

                        _status($"Открываю: {Path.GetFileName(dwg)} ({_i + 1}/{_dwgs.Count})");

                        Document doc = null;
                        try
                        {
                            doc = AcadApp.DocumentManager.Open(dwg, false);
                            AcadApp.DocumentManager.MdiActiveDocument = doc;
                        }
                        catch (System.Exception ex)
                        {
                            WriteReport(dwg, "", "ERROR", "Open failed: " + ex.Message);
                            NextDwg();
                            return;
                        }

                        List<WorkItem> workItems;
                        HashSet<ObjectId> blockImageIdsToDelete;

                        try
                        {
                            BuildPlan(doc, dwg, out workItems, out blockImageIdsToDelete);
                        }
                        catch (System.Exception ex)
                        {
                            WriteReport(dwg, "", "ERROR", "BuildPlan failed: " + ex.Message);
                            TryCloseDiscard(doc);
                            NextDwg();
                            return;
                        }

                        var processor = new DwgProcessor(
                            doc,
                            dwg,
                            workItems,
                            blockImageIdsToDelete,
                            _status,
                            line => _errors.WriteLine(line),
                            onDone: () =>
                            {
                                MainForm.RunWhenQuiescent(doc, () =>
                                {
                                    try
                                    {
                                        SaveAndClose(doc, dwg);
                                        WriteReport(dwg, GetTargetPath(dwg), "OK", "Done");
                                    }
                                    catch (System.Exception ex2)
                                    {
                                        WriteReport(dwg, GetTargetPath(dwg), "ERROR", "Save failed: " + ex2.Message);
                                        TryCloseDiscard(doc);
                                    }

                                    NextDwg();
                                });
                            });

                        processor.Start();
                    }, null);
                });
            }

            private void OpenLogs()
            {
                Directory.CreateDirectory(_s.LogFolder);

                string reportCsv = Path.Combine(_s.LogFolder, "PMT_EmbedImages_Report.csv");
                string errorsTxt = Path.Combine(_s.LogFolder, "PMT_EmbedImages_Errors.txt");
                string missingCsv = Path.Combine(_s.LogFolder, "PMT_EmbedImages_MissingImages.csv");

                bool needHeader = !File.Exists(reportCsv);
                bool needMissingHeader = !File.Exists(missingCsv);

                _report = new StreamWriter(reportCsv, true, Encoding.UTF8) { AutoFlush = true };
                _errors = new StreamWriter(errorsTxt, true, Encoding.UTF8) { AutoFlush = true };
                _missing = new StreamWriter(missingCsv, true, Encoding.UTF8) { AutoFlush = true };

                if (needHeader)
                    _report.WriteLine("Timestamp;SourcePath;TargetPath;Mode;BackupPath;Status;Message");

                if (needMissingHeader)
                    _missing.WriteLine("Timestamp;DWG;Where;FileName;ExpectedPath");
            }

            private void CloseLogs()
            {
                try { _report?.Dispose(); } catch { }
                try { _errors?.Dispose(); } catch { }
                try { _missing?.Dispose(); } catch { }
            }

            private void WriteReport(string src, string dst, string status, string msg)
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string mode = _s.Overwrite ? "Overwrite" : "NewFile";
                string backup = (_s.Overwrite && _s.Backup) ? "backup" : "";
                _report.WriteLine($"{ts};{src};{dst};{mode};{backup};{status};{msg}");
            }

            private string GetTargetPath(string src)
            {
                if (_s.Overwrite) return src;

                string srcFolder = Path.GetDirectoryName(src) ?? "";
                string targetFolder = _s.SameFolder ? srcFolder : _s.OutFolder;

                string baseName = Path.GetFileNameWithoutExtension(src);
                return Path.Combine(targetFolder, _s.Prefix + baseName + _s.Suffix + ".dwg");
            }

            private void SaveAndClose(Document doc, string src)
            {
                if (_s.Overwrite && _s.Backup)
                    MakeBackup(src);

                if (_s.Overwrite)
                {
                    string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dwg");
                    doc.Database.SaveAs(tmp, DwgVersion.Current);
                    doc.CloseAndDiscard();
                    File.Copy(tmp, src, true);
                    TryDelete(tmp);
                }
                else
                {
                    string target = GetTargetPath(src);
                    Directory.CreateDirectory(Path.GetDirectoryName(target) ?? "");
                    doc.Database.SaveAs(target, DwgVersion.Current);
                    doc.CloseAndDiscard();
                }
            }

            // ---------- PLAN ----------
            private void BuildPlan(Document doc, string dwgPath, out List<WorkItem> workItems, out HashSet<ObjectId> blockImageIdsToDelete)
            {
                workItems = new List<WorkItem>();
                blockImageIdsToDelete = new HashSet<ObjectId>();

                string folder = Path.GetDirectoryName(dwgPath) ?? "";
                var blockImageDefs = new List<BlockImageDef>();

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layoutDict = (DBDictionary)tr.GetObject(doc.Database.LayoutDictionaryId, OpenMode.ForRead);

                    // A) direct images on layouts
                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                        if (layout.ModelType) continue;

                        var ps = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                        foreach (ObjectId id in ps)
                        {
                            if (id.ObjectClass != RXClass.GetClass(typeof(RasterImage)))
                                continue;

                            var img = (RasterImage)tr.GetObject(id, OpenMode.ForRead);
                            string fileName = GetImageFileName(tr, img);
                            if (string.IsNullOrWhiteSpace(fileName)) continue;

                            string imgPath = Path.Combine(folder, fileName);
                            if (!MainForm.FileExistsLong(imgPath))
                            {
                                WriteMissing(dwgPath, "LAYOUT:" + layout.LayoutName, fileName, imgPath);
                                continue;
                            }

                            Extents3d ext;
                            try { ext = img.GeometricExtents; }
                            catch { continue; }

                            workItems.Add(new WorkItem
                            {
                                LayoutName = layout.LayoutName,
                                ImageIdToErase = id,
                                FileName = fileName,
                                ImagePath = imgPath,
                                TargetMin = ext.MinPoint,
                                TargetMax = ext.MaxPoint
                            });
                        }
                    }

                    // B) images in block definitions
                    var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                        if (btr.IsLayout) continue;
                        if (btr.IsFromExternalReference) continue;
                        if (btr.IsDependent) continue;
                        if (btr.IsAnonymous) continue;

                        string blockName = btr.Name;

                        foreach (ObjectId entId in btr)
                        {
                            if (entId.ObjectClass != RXClass.GetClass(typeof(RasterImage)))
                                continue;

                            var img = (RasterImage)tr.GetObject(entId, OpenMode.ForRead);
                            string fileName = GetImageFileName(tr, img);
                            if (string.IsNullOrWhiteSpace(fileName)) continue;

                            string imgPath = Path.Combine(folder, fileName);
                            if (!MainForm.FileExistsLong(imgPath))
                            {
                                WriteMissing(dwgPath, "BLOCKDEF:" + blockName, fileName, imgPath);
                                continue;
                            }

                            Extents3d ext;
                            try { ext = img.GeometricExtents; }
                            catch { continue; }

                            blockImageDefs.Add(new BlockImageDef
                            {
                                BlockName = blockName,
                                RasterImageId = entId,
                                FileName = fileName,
                                ImagePath = imgPath,
                                LocalMin = ext.MinPoint,
                                LocalMax = ext.MaxPoint
                            });
                        }
                    }

                    // C) lift block images onto layouts for each blockref in paperspace
                    if (blockImageDefs.Count > 0)
                    {
                        var imagesByBlock = blockImageDefs
                            .GroupBy(x => x.BlockName, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                        RXClass brClass = RXClass.GetClass(typeof(BlockReference));

                        foreach (DBDictionaryEntry entry in layoutDict)
                        {
                            var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                            if (layout.ModelType) continue;

                            var ps = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                            foreach (ObjectId entId in ps)
                            {
                                if (!entId.ObjectClass.IsDerivedFrom(brClass))
                                    continue;

                                var br = (BlockReference)tr.GetObject(entId, OpenMode.ForRead);
                                string bname = br.Name;

                                if (!imagesByBlock.TryGetValue(bname, out var list))
                                    continue;

                                Matrix3d m = br.BlockTransform;

                                foreach (var bi in list)
                                {
                                    TransformExtentsToMinMax(m, bi.LocalMin, bi.LocalMax, out var wMin, out var wMax);

                                    workItems.Add(new WorkItem
                                    {
                                        LayoutName = layout.LayoutName,
                                        ImageIdToErase = ObjectId.Null,
                                        FileName = bi.FileName,
                                        ImagePath = bi.ImagePath,
                                        TargetMin = wMin,
                                        TargetMax = wMax
                                    });

                                    blockImageIdsToDelete.Add(bi.RasterImageId);
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                workItems = workItems
                    .OrderBy(x => x.LayoutName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
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

            private void WriteMissing(string dwg, string where, string fileName, string expectedPath)
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _missing.WriteLine($"{ts};{dwg};{where};{fileName};{expectedPath}");
            }

            private static void TryCloseDiscard(Document doc)
            {
                try { doc?.CloseAndDiscard(); } catch { }
            }

            private static void TryDelete(string path)
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }

            private void MakeBackup(string src)
            {
                string srcDir = Path.GetDirectoryName(src) ?? "";
                string backupDir = Path.Combine(srcDir, "backup");
                Directory.CreateDirectory(backupDir);

                string name = Path.GetFileName(src);
                string candidate = Path.Combine(backupDir, name);

                if (!File.Exists(candidate))
                {
                    File.Copy(src, candidate, false);
                    return;
                }

                string baseName = Path.GetFileNameWithoutExtension(src);
                string ext = Path.GetExtension(src);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string stamped = Path.Combine(backupDir, baseName + "_" + stamp + ext);

                File.Copy(src, stamped, false);
            }
        }

        // =========================
        // DwgProcessor: COM OLE insert from file + erase raster
        // =========================
        private class DwgProcessor
        {
            private readonly Document _doc;
            private readonly Database _db;
            private readonly string _dwgPath;

            private readonly List<WorkItem> _items;
            private readonly HashSet<ObjectId> _blockImageIdsToDelete;

            private readonly Action<string> _status;
            private readonly Action<string> _err;
            private readonly Action _onDone;

            private int _idx = -1;
            private bool _busy;

            public DwgProcessor(Document doc, string dwgPath,
                List<WorkItem> items,
                HashSet<ObjectId> blockImageIdsToDelete,
                Action<string> status,
                Action<string> err,
                Action onDone)
            {
                _doc = doc;
                _db = doc.Database;
                _dwgPath = dwgPath;

                _items = items ?? new List<WorkItem>();
                _blockImageIdsToDelete = blockImageIdsToDelete ?? new HashSet<ObjectId>();

                _status = status;
                _err = err;
                _onDone = onDone;
            }

            public void Start()
            {
                _idx = -1;
                _busy = false;
                NextItem();
            }

            private void NextItem()
            {
                if (_busy) return;
                _busy = true;

                _idx++;
                if (_idx >= _items.Count)
                {
                    _busy = false;

                    // delete block-def raster images after all OLE insertions
                    MainForm.RunWhenQuiescent(_doc, () =>
                    {
                        DeleteBlockDefImagesSafe();
                        _onDone?.Invoke();
                    });
                    return;
                }

                var item = _items[_idx];

                _status($"OLE insert: {Path.GetFileName(_dwgPath)} | {item.LayoutName} | {item.FileName} ({_idx + 1}/{_items.Count})");

                if (string.IsNullOrWhiteSpace(item.ImagePath) || !MainForm.FileExistsLong(item.ImagePath))
                {
                    LogErr(item, "Image file not found next to DWG: " + item.ImagePath);
                    _busy = false;
                    MainForm.RunInNextIdle(() => NextItem());
                    return;
                }

                MainForm.RunWhenQuiescent(_doc, () =>
                {
                    try
                    {
                        // 1) switch active layout via COM (no commands)
                        object acadDoc = null;
                        try
                        {
                            // COM AutoCAD application object
                            object acadAppCom = AcadApp.AcadApplication;
                            // active COM document
                            acadDoc = acadAppCom.GetType().InvokeMember("ActiveDocument",
                                BindingFlags.GetProperty, null, acadAppCom, null);
                        }
                        catch
                        {
                            acadDoc = null;
                        }

                        if (acadDoc == null)
                        {
                            LogErr(item, "COM: cannot get ActiveDocument");
                            _busy = false;
                            MainForm.RunInNextIdle(() => NextItem());
                            return;
                        }

                        if (!SetActiveLayoutCom(acadDoc, item.LayoutName))
                        {
                            LogErr(item, "COM: cannot set ActiveLayout=" + item.LayoutName);
                        }

                        // 2) insert OLE in current PaperSpace via COM
                        double w = item.TargetMax.X - item.TargetMin.X;
                        double h = item.TargetMax.Y - item.TargetMin.Y;
                        if (w <= 1e-9 || h <= 1e-9)
                            throw new System.Exception("Target bbox invalid");

                        var insPt = new double[] { item.TargetMin.X, item.TargetMin.Y, 0.0 };

                        bool ok = AddOleFromFileCom(acadDoc, insPt, w, h, item.ImagePath);
                        if (!ok)
                        {
                            LogErr(item, "COM: AddOLEObject failed (no matching signature)");
                        }

                        // 3) erase original RasterImage on layout (if any)
                        if (!item.ImageIdToErase.IsNull)
                        {
                            MainForm.SafeDbWrite(_doc, () =>
                            {
                                using (var tr = _db.TransactionManager.StartTransaction())
                                {
                                    var oldImg = tr.GetObject(item.ImageIdToErase, OpenMode.ForWrite) as RasterImage;
                                    if (oldImg != null && !oldImg.IsErased)
                                    {
                                        LayerTableRecord ltr = null;
                                        bool wasLocked = false;

                                        try
                                        {
                                            ltr = (LayerTableRecord)tr.GetObject(oldImg.LayerId, OpenMode.ForWrite);
                                            wasLocked = ltr.IsLocked;
                                            if (wasLocked) ltr.IsLocked = false;
                                        }
                                        catch { }

                                        try { oldImg.Erase(true); } catch { }

                                        try { if (ltr != null && wasLocked) ltr.IsLocked = true; } catch { }
                                    }

                                    tr.Commit();
                                }
                            });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LogErr(item, "STEP ERROR: " + ex.Message);
                    }

                    _busy = false;
                    MainForm.RunInNextIdle(() => NextItem());
                });
            }

            private void DeleteBlockDefImagesSafe()
            {
                if (_blockImageIdsToDelete == null || _blockImageIdsToDelete.Count == 0)
                    return;

                try
                {
                    MainForm.SafeDbWrite(_doc, () =>
                    {
                        using (var tr = _db.TransactionManager.StartTransaction())
                        {
                            foreach (var rid in _blockImageIdsToDelete)
                            {
                                try
                                {
                                    var img = tr.GetObject(rid, OpenMode.ForWrite) as RasterImage;
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

                                    try { img.Erase(true); } catch { }

                                    try { if (ltr != null && wasLocked) ltr.IsLocked = true; } catch { }
                                }
                                catch { }
                            }

                            tr.Commit();
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    _err($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {_dwgPath} | BLOCKDEF | (n/a) | Delete block images failed: {ex.Message}");
                }
            }

            private void LogErr(WorkItem item, string msg)
            {
                _err($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {_dwgPath} | {item.LayoutName} | {item.FileName} | {msg}");
            }

            // -------- COM helpers (late bound) --------

            private static bool SetActiveLayoutCom(object acadDoc, string layoutName)
            {
                try
                {
                    // acadDoc.Layouts.Item(layoutName) -> layoutObj
                    object layouts = acadDoc.GetType().InvokeMember("Layouts", BindingFlags.GetProperty, null, acadDoc, null);
                    object layoutObj = layouts.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, layouts, new object[] { layoutName });
                    acadDoc.GetType().InvokeMember("ActiveLayout", BindingFlags.SetProperty, null, acadDoc, new object[] { layoutObj });
                    return true;
                }
                catch { return false; }
            }

            private static bool AddOleFromFileCom(object acadDoc, double[] insPt, double width, double height, string filePath)
            {
                try
                {
                    object paperSpace = acadDoc.GetType().InvokeMember("PaperSpace", BindingFlags.GetProperty, null, acadDoc, null);

                    // пробуем несколько наиболее распространённых сигнатур AddOLEObject
                    // 1) (InsertionPoint, Width, Height, FileName)
                    if (TryInvoke(paperSpace, "AddOLEObject", new object[] { insPt, width, height, filePath })) return true;

                    // 2) (InsertionPoint, Width, Height, ClassName, FileName)
                    if (TryInvoke(paperSpace, "AddOLEObject", new object[] { insPt, width, height, "Paint.Picture", filePath })) return true;

                    // 3) (InsertionPoint, Width, Height, ClassName, FileName, Link)
                    if (TryInvoke(paperSpace, "AddOLEObject", new object[] { insPt, width, height, "Paint.Picture", filePath, false })) return true;

                    // 4) (InsertionPoint, Width, Height, FileName, Link)
                    if (TryInvoke(paperSpace, "AddOLEObject", new object[] { insPt, width, height, filePath, false })) return true;

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            private static bool TryInvoke(object target, string method, object[] args)
            {
                try
                {
                    target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        // =========================
        // Geometry: transform block-local extents to layout bbox
        // =========================
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
    }
}
