namespace PMT.EmbedImages
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnBrowseFolder = new System.Windows.Forms.Button();
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panelFilesTop = new System.Windows.Forms.Panel();
            this.btnInvert = new System.Windows.Forms.Button();
            this.btnNone = new System.Windows.Forms.Button();
            this.btnAll = new System.Windows.Forms.Button();
            this.gridFiles = new System.Windows.Forms.DataGridView();
            this.colDo = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpSave = new System.Windows.Forms.GroupBox();
            this.panelSaveInner = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblExample = new System.Windows.Forms.Label();
            this.tbSuffix = new System.Windows.Forms.TextBox();
            this.lblSuffix = new System.Windows.Forms.Label();
            this.tbPrefix = new System.Windows.Forms.TextBox();
            this.lblPrefix = new System.Windows.Forms.Label();
            this.cbBackup = new System.Windows.Forms.CheckBox();
            this.rbNewFile = new System.Windows.Forms.RadioButton();
            this.rbOverwrite = new System.Windows.Forms.RadioButton();
            this.panelOutFolder = new System.Windows.Forms.Panel();
            this.btnBrowseOutFolder = new System.Windows.Forms.Button();
            this.tbOutFolder = new System.Windows.Forms.TextBox();
            this.cbSameFolder = new System.Windows.Forms.CheckBox();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panelFilesTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridFiles)).BeginInit();
            this.grpSave.SuspendLayout();
            this.panelSaveInner.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelOutFolder.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnBrowseFolder);
            this.panelTop.Controls.Add(this.tbFolder);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.panelTop.Size = new System.Drawing.Size(980, 40);
            this.panelTop.TabIndex = 0;
            // 
            // btnBrowseFolder
            // 
            this.btnBrowseFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseFolder.Location = new System.Drawing.Point(886, 7);
            this.btnBrowseFolder.Name = "btnBrowseFolder";
            this.btnBrowseFolder.Size = new System.Drawing.Size(86, 26);
            this.btnBrowseFolder.TabIndex = 1;
            this.btnBrowseFolder.Text = "Выбрать…";
            this.btnBrowseFolder.UseVisualStyleBackColor = true;
            this.btnBrowseFolder.Click += new System.EventHandler(this.btnBrowseFolder_Click);
            // 
            // tbFolder
            // 
            this.tbFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFolder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbFolder.Location = new System.Drawing.Point(8, 9);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.ReadOnly = true;
            this.tbFolder.Size = new System.Drawing.Size(872, 22);
            this.tbFolder.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 40);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.gridFiles);
            this.splitContainer1.Panel1.Controls.Add(this.panelFilesTop);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grpSave);
            this.splitContainer1.Size = new System.Drawing.Size(980, 560);
            this.splitContainer1.SplitterDistance = 520;
            this.splitContainer1.TabIndex = 1;
            // 
            // panelFilesTop
            // 
            this.panelFilesTop.Controls.Add(this.btnInvert);
            this.panelFilesTop.Controls.Add(this.btnNone);
            this.panelFilesTop.Controls.Add(this.btnAll);
            this.panelFilesTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilesTop.Location = new System.Drawing.Point(0, 0);
            this.panelFilesTop.Name = "panelFilesTop";
            this.panelFilesTop.Padding = new System.Windows.Forms.Padding(6);
            this.panelFilesTop.Size = new System.Drawing.Size(520, 38);
            this.panelFilesTop.TabIndex = 0;
            // 
            // btnInvert
            // 
            this.btnInvert.Location = new System.Drawing.Point(198, 7);
            this.btnInvert.Name = "btnInvert";
            this.btnInvert.Size = new System.Drawing.Size(100, 24);
            this.btnInvert.TabIndex = 2;
            this.btnInvert.Text = "Инверт.";
            this.btnInvert.UseVisualStyleBackColor = true;
            this.btnInvert.Click += new System.EventHandler(this.btnInvert_Click);
            // 
            // btnNone
            // 
            this.btnNone.Location = new System.Drawing.Point(104, 7);
            this.btnNone.Name = "btnNone";
            this.btnNone.Size = new System.Drawing.Size(90, 24);
            this.btnNone.TabIndex = 1;
            this.btnNone.Text = "Ни одного";
            this.btnNone.UseVisualStyleBackColor = true;
            this.btnNone.Click += new System.EventHandler(this.btnNone_Click);
            // 
            // btnAll
            // 
            this.btnAll.Location = new System.Drawing.Point(8, 7);
            this.btnAll.Name = "btnAll";
            this.btnAll.Size = new System.Drawing.Size(90, 24);
            this.btnAll.TabIndex = 0;
            this.btnAll.Text = "Все";
            this.btnAll.UseVisualStyleBackColor = true;
            this.btnAll.Click += new System.EventHandler(this.btnAll_Click);
            // 
            // gridFiles
            // 
            this.gridFiles.AllowUserToAddRows = false;
            this.gridFiles.AllowUserToDeleteRows = false;
            this.gridFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDo,
            this.colName});
            this.gridFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridFiles.Location = new System.Drawing.Point(0, 38);
            this.gridFiles.Name = "gridFiles";
            this.gridFiles.RowHeadersVisible = false;
            this.gridFiles.RowTemplate.Height = 24;
            this.gridFiles.Size = new System.Drawing.Size(520, 522);
            this.gridFiles.TabIndex = 1;
            this.gridFiles.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridFiles_CellValueChanged);
            this.gridFiles.CurrentCellDirtyStateChanged += new System.EventHandler(this.gridFiles_CurrentCellDirtyStateChanged);
            // 
            // colDo
            // 
            this.colDo.HeaderText = "";
            this.colDo.MinimumWidth = 6;
            this.colDo.Name = "colDo";
            this.colDo.Width = 35;
            // 
            // colName
            // 
            this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colName.HeaderText = "DWG";
            this.colName.MinimumWidth = 6;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            // 
            // grpSave
            // 
            this.grpSave.Controls.Add(this.panelSaveInner);
            this.grpSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSave.Location = new System.Drawing.Point(0, 0);
            this.grpSave.Name = "grpSave";
            this.grpSave.Padding = new System.Windows.Forms.Padding(10);
            this.grpSave.Size = new System.Drawing.Size(456, 560);
            this.grpSave.TabIndex = 0;
            this.grpSave.TabStop = false;
            this.grpSave.Text = "Сохранение";
            // 
            // panelSaveInner
            // 
            this.panelSaveInner.Controls.Add(this.panelBottom);
            this.panelSaveInner.Controls.Add(this.lblExample);
            this.panelSaveInner.Controls.Add(this.tbSuffix);
            this.panelSaveInner.Controls.Add(this.lblSuffix);
            this.panelSaveInner.Controls.Add(this.tbPrefix);
            this.panelSaveInner.Controls.Add(this.lblPrefix);
            this.panelSaveInner.Controls.Add(this.cbBackup);
            this.panelSaveInner.Controls.Add(this.rbNewFile);
            this.panelSaveInner.Controls.Add(this.rbOverwrite);
            this.panelSaveInner.Controls.Add(this.panelOutFolder);
            this.panelSaveInner.Controls.Add(this.cbSameFolder);
            this.panelSaveInner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSaveInner.Location = new System.Drawing.Point(10, 25);
            this.panelSaveInner.Name = "panelSaveInner";
            this.panelSaveInner.Padding = new System.Windows.Forms.Padding(6);
            this.panelSaveInner.Size = new System.Drawing.Size(436, 525);
            this.panelSaveInner.TabIndex = 0;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Controls.Add(this.btnStart);
            this.panelBottom.Controls.Add(this.lblStatus);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(6, 475);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(424, 44);
            this.panelBottom.TabIndex = 10;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(337, 9);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(84, 26);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.Location = new System.Drawing.Point(247, 9);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(84, 26);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Старт";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(6, 15);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(123, 13);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Выбрано файлов: 0 из 0";
            // 
            // lblExample
            // 
            this.lblExample.AutoSize = true;
            this.lblExample.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblExample.Location = new System.Drawing.Point(9, 270);
            this.lblExample.Name = "lblExample";
            this.lblExample.Size = new System.Drawing.Size(144, 13);
            this.lblExample.TabIndex = 9;
            this.lblExample.Text = "Пример: A101_embedded.dwg";
            // 
            // tbSuffix
            // 
            this.tbSuffix.Location = new System.Drawing.Point(80, 242);
            this.tbSuffix.Name = "tbSuffix";
            this.tbSuffix.Size = new System.Drawing.Size(200, 20);
            this.tbSuffix.TabIndex = 8;
            this.tbSuffix.TextChanged += new System.EventHandler(this.tbSuffix_TextChanged);
            // 
            // lblSuffix
            // 
            this.lblSuffix.AutoSize = true;
            this.lblSuffix.Location = new System.Drawing.Point(9, 245);
            this.lblSuffix.Name = "lblSuffix";
            this.lblSuffix.Size = new System.Drawing.Size(46, 13);
            this.lblSuffix.TabIndex = 7;
            this.lblSuffix.Text = "Суффикс";
            // 
            // tbPrefix
            // 
            this.tbPrefix.Location = new System.Drawing.Point(80, 216);
            this.tbPrefix.Name = "tbPrefix";
            this.tbPrefix.Size = new System.Drawing.Size(200, 20);
            this.tbPrefix.TabIndex = 6;
            this.tbPrefix.TextChanged += new System.EventHandler(this.tbPrefix_TextChanged);
            // 
            // lblPrefix
            // 
            this.lblPrefix.AutoSize = true;
            this.lblPrefix.Location = new System.Drawing.Point(9, 219);
            this.lblPrefix.Name = "lblPrefix";
            this.lblPrefix.Size = new System.Drawing.Size(46, 13);
            this.lblPrefix.TabIndex = 5;
            this.lblPrefix.Text = "Префикс";
            // 
            // cbBackup
            // 
            this.cbBackup.AutoSize = true;
            this.cbBackup.Location = new System.Drawing.Point(12, 169);
            this.cbBackup.Name = "cbBackup";
            this.cbBackup.Size = new System.Drawing.Size(230, 17);
            this.cbBackup.TabIndex = 4;
            this.cbBackup.Text = "Создавать backup в папку backup";
            this.cbBackup.UseVisualStyleBackColor = true;
            // 
            // rbNewFile
            // 
            this.rbNewFile.AutoSize = true;
            this.rbNewFile.Location = new System.Drawing.Point(12, 142);
            this.rbNewFile.Name = "rbNewFile";
            this.rbNewFile.Size = new System.Drawing.Size(157, 17);
            this.rbNewFile.TabIndex = 3;
            this.rbNewFile.TabStop = true;
            this.rbNewFile.Text = "Сохранить как новый файл";
            this.rbNewFile.UseVisualStyleBackColor = true;
            this.rbNewFile.CheckedChanged += new System.EventHandler(this.rbNewFile_CheckedChanged);
            // 
            // rbOverwrite
            // 
            this.rbOverwrite.AutoSize = true;
            this.rbOverwrite.Location = new System.Drawing.Point(12, 119);
            this.rbOverwrite.Name = "rbOverwrite";
            this.rbOverwrite.Size = new System.Drawing.Size(163, 17);
            this.rbOverwrite.TabIndex = 2;
            this.rbOverwrite.TabStop = true;
            this.rbOverwrite.Text = "Перезаписать исходные";
            this.rbOverwrite.UseVisualStyleBackColor = true;
            this.rbOverwrite.CheckedChanged += new System.EventHandler(this.rbOverwrite_CheckedChanged);
            // 
            // panelOutFolder
            // 
            this.panelOutFolder.Controls.Add(this.btnBrowseOutFolder);
            this.panelOutFolder.Controls.Add(this.tbOutFolder);
            this.panelOutFolder.Location = new System.Drawing.Point(12, 58);
            this.panelOutFolder.Name = "panelOutFolder";
            this.panelOutFolder.Size = new System.Drawing.Size(412, 32);
            this.panelOutFolder.TabIndex = 1;
            // 
            // btnBrowseOutFolder
            // 
            this.btnBrowseOutFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutFolder.Location = new System.Drawing.Point(326, 3);
            this.btnBrowseOutFolder.Name = "btnBrowseOutFolder";
            this.btnBrowseOutFolder.Size = new System.Drawing.Size(83, 26);
            this.btnBrowseOutFolder.TabIndex = 1;
            this.btnBrowseOutFolder.Text = "Выбрать…";
            this.btnBrowseOutFolder.UseVisualStyleBackColor = true;
            this.btnBrowseOutFolder.Click += new System.EventHandler(this.btnBrowseOutFolder_Click);
            // 
            // tbOutFolder
            // 
            this.tbOutFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbOutFolder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbOutFolder.Location = new System.Drawing.Point(0, 6);
            this.tbOutFolder.Name = "tbOutFolder";
            this.tbOutFolder.ReadOnly = true;
            this.tbOutFolder.Size = new System.Drawing.Size(320, 20);
            this.tbOutFolder.TabIndex = 0;
            // 
            // cbSameFolder
            // 
            this.cbSameFolder.AutoSize = true;
            this.cbSameFolder.Location = new System.Drawing.Point(12, 32);
            this.cbSameFolder.Name = "cbSameFolder";
            this.cbSameFolder.Size = new System.Drawing.Size(149, 17);
            this.cbSameFolder.TabIndex = 0;
            this.cbSameFolder.Text = "Сохранить в ту же папку";
            this.cbSameFolder.UseVisualStyleBackColor = true;
            this.cbSameFolder.CheckedChanged += new System.EventHandler(this.cbSameFolder_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 600);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panelTop);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PMT — Внедрение картинок в DWG";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panelFilesTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridFiles)).EndInit();
            this.grpSave.ResumeLayout(false);
            this.panelSaveInner.ResumeLayout(false);
            this.panelSaveInner.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.panelOutFolder.ResumeLayout(false);
            this.panelOutFolder.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnBrowseFolder;
        private System.Windows.Forms.TextBox tbFolder;

        private System.Windows.Forms.SplitContainer splitContainer1;

        private System.Windows.Forms.Panel panelFilesTop;
        private System.Windows.Forms.Button btnAll;
        private System.Windows.Forms.Button btnNone;
        private System.Windows.Forms.Button btnInvert;

        private System.Windows.Forms.DataGridView gridFiles;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colDo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;

        private System.Windows.Forms.GroupBox grpSave;
        private System.Windows.Forms.Panel panelSaveInner;

        private System.Windows.Forms.CheckBox cbSameFolder;
        private System.Windows.Forms.Panel panelOutFolder;
        private System.Windows.Forms.TextBox tbOutFolder;
        private System.Windows.Forms.Button btnBrowseOutFolder;

        private System.Windows.Forms.RadioButton rbOverwrite;
        private System.Windows.Forms.RadioButton rbNewFile;

        private System.Windows.Forms.CheckBox cbBackup;

        private System.Windows.Forms.Label lblPrefix;
        private System.Windows.Forms.TextBox tbPrefix;
        private System.Windows.Forms.Label lblSuffix;
        private System.Windows.Forms.TextBox tbSuffix;
        private System.Windows.Forms.Label lblExample;

        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnClose;
    }
}
