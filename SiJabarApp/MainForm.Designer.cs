namespace SiJabarApp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panelSidebar = new Panel();
            btnBukaMap = new FontAwesome.Sharp.IconButton();
            btnLogout = new FontAwesome.Sharp.IconButton();
            btnChatbot = new FontAwesome.Sharp.IconButton();
            btnExportPDF = new FontAwesome.Sharp.IconButton();
            btnDataSampah = new FontAwesome.Sharp.IconButton();
            panel1 = new Panel();
            label1 = new Label();
            iconPictureBox1 = new FontAwesome.Sharp.IconPictureBox();
            panelHeader = new Panel();
            btnMinimize = new FontAwesome.Sharp.IconButton();
            btnMaximize = new FontAwesome.Sharp.IconButton();
            btnClose = new FontAwesome.Sharp.IconButton();
            lblTitle = new Label();
            panelContent = new Panel();
            panel3 = new Panel();
            gridSampah = new DataGridView();
            colId = new DataGridViewTextBoxColumn();
            colWilayah = new DataGridViewTextBoxColumn();
            colJenis = new DataGridViewTextBoxColumn();
            colBerat = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            panel4 = new Panel();
            panel2 = new Panel();
            lblUserLogin = new Label();
            btnDelete = new FontAwesome.Sharp.IconButton();
            btnEdit = new FontAwesome.Sharp.IconButton();
            btnAdd = new FontAwesome.Sharp.IconButton();
            panelSidebar.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)iconPictureBox1).BeginInit();
            panelHeader.SuspendLayout();
            panelContent.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridSampah).BeginInit();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.FromArgb(33, 37, 41);
            panelSidebar.Controls.Add(btnBukaMap);
            panelSidebar.Controls.Add(btnLogout);
            panelSidebar.Controls.Add(btnChatbot);
            panelSidebar.Controls.Add(btnExportPDF);
            panelSidebar.Controls.Add(btnDataSampah);
            panelSidebar.Controls.Add(panel1);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Margin = new Padding(4, 3, 4, 3);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Size = new Size(260, 720);
            panelSidebar.TabIndex = 0;
            // 
            // btnBukaMap
            // 
            btnBukaMap.Dock = DockStyle.Top;
            btnBukaMap.FlatAppearance.BorderSize = 0;
            btnBukaMap.FlatStyle = FlatStyle.Flat;
            btnBukaMap.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnBukaMap.ForeColor = Color.White;
            btnBukaMap.IconChar = FontAwesome.Sharp.IconChar.Map;
            btnBukaMap.IconColor = Color.White;
            btnBukaMap.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnBukaMap.IconSize = 24;
            btnBukaMap.ImageAlign = ContentAlignment.MiddleLeft;
            btnBukaMap.Location = new Point(0, 245);
            btnBukaMap.Name = "btnBukaMap";
            btnBukaMap.Padding = new Padding(25, 0, 0, 0);
            btnBukaMap.Size = new Size(260, 55);
            btnBukaMap.TabIndex = 5;
            btnBukaMap.Text = "Maps";
            btnBukaMap.TextAlign = ContentAlignment.MiddleLeft;
            btnBukaMap.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBukaMap.UseVisualStyleBackColor = true;
            btnBukaMap.Click += btnBukaMap_Click;
            // 
            // btnLogout
            // 
            btnLogout.BackColor = Color.FromArgb(33, 37, 41);
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseDownBackColor = Color.Maroon;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.Maroon;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLogout.ForeColor = Color.White;
            btnLogout.IconChar = FontAwesome.Sharp.IconChar.SignOut;
            btnLogout.IconColor = Color.White;
            btnLogout.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnLogout.IconSize = 24;
            btnLogout.ImageAlign = ContentAlignment.MiddleLeft;
            btnLogout.Location = new Point(0, 665);
            btnLogout.Name = "btnLogout";
            btnLogout.Padding = new Padding(25, 0, 0, 0);
            btnLogout.Size = new Size(260, 55);
            btnLogout.TabIndex = 4;
            btnLogout.Text = "Keluar";
            btnLogout.TextAlign = ContentAlignment.MiddleLeft;
            btnLogout.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;
            // 
            // btnChatbot
            // 
            btnChatbot.Dock = DockStyle.Top;
            btnChatbot.FlatAppearance.BorderSize = 0;
            btnChatbot.FlatStyle = FlatStyle.Flat;
            btnChatbot.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnChatbot.ForeColor = Color.White;
            btnChatbot.IconChar = FontAwesome.Sharp.IconChar.Robot;
            btnChatbot.IconColor = Color.White;
            btnChatbot.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnChatbot.IconSize = 24;
            btnChatbot.ImageAlign = ContentAlignment.MiddleLeft;
            btnChatbot.Location = new Point(0, 190);
            btnChatbot.Name = "btnChatbot";
            btnChatbot.Padding = new Padding(25, 0, 0, 0);
            btnChatbot.Size = new Size(260, 55);
            btnChatbot.TabIndex = 3;
            btnChatbot.Text = "Chat Assistant";
            btnChatbot.TextAlign = ContentAlignment.MiddleLeft;
            btnChatbot.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnChatbot.UseVisualStyleBackColor = true;
            btnChatbot.Click += btnChatbot_Click;
            // 
            // btnExportPDF
            // 
            btnExportPDF.Dock = DockStyle.Top;
            btnExportPDF.FlatAppearance.BorderSize = 0;
            btnExportPDF.FlatStyle = FlatStyle.Flat;
            btnExportPDF.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnExportPDF.ForeColor = Color.White;
            btnExportPDF.IconChar = FontAwesome.Sharp.IconChar.FilePdf;
            btnExportPDF.IconColor = Color.White;
            btnExportPDF.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnExportPDF.IconSize = 24;
            btnExportPDF.ImageAlign = ContentAlignment.MiddleLeft;
            btnExportPDF.Location = new Point(0, 135);
            btnExportPDF.Name = "btnExportPDF";
            btnExportPDF.Padding = new Padding(25, 0, 0, 0);
            btnExportPDF.Size = new Size(260, 55);
            btnExportPDF.TabIndex = 2;
            btnExportPDF.Text = "Export PDF";
            btnExportPDF.TextAlign = ContentAlignment.MiddleLeft;
            btnExportPDF.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnExportPDF.UseVisualStyleBackColor = true;
            btnExportPDF.Click += btnExportPDF_Click;
            // 
            // btnDataSampah
            // 
            btnDataSampah.Dock = DockStyle.Top;
            btnDataSampah.FlatAppearance.BorderSize = 0;
            btnDataSampah.FlatStyle = FlatStyle.Flat;
            btnDataSampah.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnDataSampah.ForeColor = Color.White;
            btnDataSampah.IconChar = FontAwesome.Sharp.IconChar.Table;
            btnDataSampah.IconColor = Color.White;
            btnDataSampah.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnDataSampah.IconSize = 24;
            btnDataSampah.ImageAlign = ContentAlignment.MiddleLeft;
            btnDataSampah.Location = new Point(0, 80);
            btnDataSampah.Name = "btnDataSampah";
            btnDataSampah.Padding = new Padding(25, 0, 0, 0);
            btnDataSampah.Size = new Size(260, 55);
            btnDataSampah.TabIndex = 1;
            btnDataSampah.Text = "Data Sampah";
            btnDataSampah.TextAlign = ContentAlignment.MiddleLeft;
            btnDataSampah.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnDataSampah.UseVisualStyleBackColor = true;
            btnDataSampah.Click += btnDataSampah_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(label1);
            panel1.Controls.Add(iconPictureBox1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(260, 80);
            panel1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(66, 20);
            label1.Name = "label1";
            label1.Size = new Size(166, 41);
            label1.TabIndex = 1;
            label1.Text = "SI - JABAR";
            // 
            // iconPictureBox1
            // 
            iconPictureBox1.BackColor = Color.FromArgb(33, 37, 41);
            iconPictureBox1.ForeColor = Color.LawnGreen;
            iconPictureBox1.IconChar = FontAwesome.Sharp.IconChar.Recycle;
            iconPictureBox1.IconColor = Color.LawnGreen;
            iconPictureBox1.IconFont = FontAwesome.Sharp.IconFont.Auto;
            iconPictureBox1.IconSize = 47;
            iconPictureBox1.Location = new Point(12, 20);
            iconPictureBox1.Name = "iconPictureBox1";
            iconPictureBox1.Size = new Size(48, 47);
            iconPictureBox1.TabIndex = 0;
            iconPictureBox1.TabStop = false;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(btnMinimize);
            panelHeader.Controls.Add(btnMaximize);
            panelHeader.Controls.Add(btnClose);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(260, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1020, 80);
            panelHeader.TabIndex = 1;
            // 
            // btnMinimize
            // 
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnMinimize.IconChar = FontAwesome.Sharp.IconChar.WindowMinimize;
            btnMinimize.IconColor = Color.Black;
            btnMinimize.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnMinimize.IconSize = 20;
            btnMinimize.Location = new Point(873, 0);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(45, 40);
            btnMinimize.TabIndex = 3;
            btnMinimize.UseVisualStyleBackColor = true;
            btnMinimize.Click += btnMinimize_Click;
            // 
            // btnMaximize
            // 
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnMaximize.IconChar = FontAwesome.Sharp.IconChar.WindowMaximize;
            btnMaximize.IconColor = Color.Black;
            btnMaximize.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnMaximize.IconSize = 20;
            btnMaximize.Location = new Point(924, 3);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(45, 40);
            btnMaximize.TabIndex = 2;
            btnMaximize.UseVisualStyleBackColor = true;
            btnMaximize.Click += btnMaximize_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnClose.IconChar = FontAwesome.Sharp.IconChar.Close;
            btnClose.IconColor = Color.Black;
            btnClose.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnClose.IconSize = 20;
            btnClose.Location = new Point(975, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(45, 40);
            btnClose.TabIndex = 1;
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.Black;
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(1020, 80);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "DASHBOARD MONITORING SAMPAH";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.MouseDown += panelHeader_MouseDown;
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.FromArgb(243, 244, 246);
            panelContent.Controls.Add(panel3);
            panelContent.Controls.Add(panel2);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(260, 80);
            panelContent.Name = "panelContent";
            panelContent.Padding = new Padding(30);
            panelContent.Size = new Size(1020, 640);
            panelContent.TabIndex = 2;
            // 
            // panel3
            // 
            panel3.BackColor = Color.White;
            panel3.Controls.Add(gridSampah);
            panel3.Controls.Add(panel4);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(30, 90);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(20);
            panel3.Size = new Size(960, 520);
            panel3.TabIndex = 1;
            // 
            // gridSampah
            // 
            gridSampah.AllowUserToAddRows = false;
            gridSampah.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridSampah.BackgroundColor = Color.White;
            gridSampah.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.White;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            gridSampah.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            gridSampah.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridSampah.Columns.AddRange(new DataGridViewColumn[] { colId, colWilayah, colJenis, colBerat, colStatus });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            gridSampah.DefaultCellStyle = dataGridViewCellStyle2;
            gridSampah.Dock = DockStyle.Fill;
            gridSampah.EnableHeadersVisualStyles = false;
            gridSampah.Location = new Point(20, 25);
            gridSampah.Name = "gridSampah";
            gridSampah.RowHeadersVisible = false;
            gridSampah.RowHeadersWidth = 51;
            gridSampah.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSampah.Size = new Size(920, 475);
            gridSampah.TabIndex = 1;
            // 
            // colId
            // 
            colId.HeaderText = "ID";
            colId.MinimumWidth = 6;
            colId.Name = "colId";
            colId.Visible = false;
            // 
            // colWilayah
            // 
            colWilayah.HeaderText = "Wilayah";
            colWilayah.MinimumWidth = 6;
            colWilayah.Name = "colWilayah";
            // 
            // colJenis
            // 
            colJenis.HeaderText = "Jenis";
            colJenis.MinimumWidth = 6;
            colJenis.Name = "colJenis";
            // 
            // colBerat
            // 
            colBerat.HeaderText = "Berat (Kg)";
            colBerat.MinimumWidth = 6;
            colBerat.Name = "colBerat";
            // 
            // colStatus
            // 
            colStatus.HeaderText = "Status";
            colStatus.MinimumWidth = 6;
            colStatus.Name = "colStatus";
            // 
            // panel4
            // 
            panel4.Dock = DockStyle.Top;
            panel4.Location = new Point(20, 20);
            panel4.Name = "panel4";
            panel4.Size = new Size(920, 5);
            panel4.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Transparent;
            panel2.Controls.Add(lblUserLogin);
            panel2.Controls.Add(btnDelete);
            panel2.Controls.Add(btnEdit);
            panel2.Controls.Add(btnAdd);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(30, 30);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(0, 0, 0, 10);
            panel2.Size = new Size(960, 60);
            panel2.TabIndex = 0;
            // 
            // lblUserLogin
            // 
            lblUserLogin.AutoSize = true;
            lblUserLogin.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUserLogin.Location = new Point(0, 9);
            lblUserLogin.Name = "lblUserLogin";
            lblUserLogin.Size = new Size(184, 31);
            lblUserLogin.TabIndex = 3;
            lblUserLogin.Text = "Selamat Datang";
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelete.BackColor = Color.FromArgb(220, 53, 69);
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnDelete.ForeColor = Color.White;
            btnDelete.IconChar = FontAwesome.Sharp.IconChar.Trash;
            btnDelete.IconColor = Color.White;
            btnDelete.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnDelete.IconSize = 18;
            btnDelete.Location = new Point(859, 7);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 40);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Hapus";
            btnDelete.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnEdit
            // 
            btnEdit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEdit.BackColor = Color.FromArgb(255, 193, 7);
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.FlatStyle = FlatStyle.Flat;
            btnEdit.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnEdit.ForeColor = Color.White;
            btnEdit.IconChar = FontAwesome.Sharp.IconChar.Pen;
            btnEdit.IconColor = Color.White;
            btnEdit.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnEdit.IconSize = 18;
            btnEdit.Location = new Point(744, 7);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(100, 40);
            btnEdit.TabIndex = 1;
            btnEdit.Text = "Edit";
            btnEdit.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnEdit.UseVisualStyleBackColor = false;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.BackColor = Color.FromArgb(46, 204, 113);
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAdd.ForeColor = Color.White;
            btnAdd.IconChar = FontAwesome.Sharp.IconChar.Add;
            btnAdd.IconColor = Color.White;
            btnAdd.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnAdd.IconSize = 18;
            btnAdd.Location = new Point(629, 7);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(100, 40);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Tambah";
            btnAdd.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAdd.UseVisualStyleBackColor = false;
            btnAdd.Click += btnAdd_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 720);
            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelSidebar);
            Font = new Font("Century Gothic", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SI - JABAR Dashboard Admin";
            panelSidebar.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)iconPictureBox1).EndInit();
            panelHeader.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridSampah).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelSidebar;
        private Panel panelHeader;
        private Panel panelContent;
        private Panel panel1;
        private Label label1;
        private FontAwesome.Sharp.IconPictureBox iconPictureBox1;
        private FontAwesome.Sharp.IconButton btnDataSampah;
        private FontAwesome.Sharp.IconButton btnChatbot;
        private FontAwesome.Sharp.IconButton btnExportPDF;
        private Label lblTitle;
        private FontAwesome.Sharp.IconButton btnClose;
        private FontAwesome.Sharp.IconButton btnMinimize;
        private FontAwesome.Sharp.IconButton btnMaximize;
        private Panel panel2;
        private FontAwesome.Sharp.IconButton btnAdd;
        private FontAwesome.Sharp.IconButton btnEdit;
        private FontAwesome.Sharp.IconButton btnDelete;
        private Panel panel3;
        private DataGridView gridSampah;
        private Panel panel4;
        private DataGridViewTextBoxColumn colId;
        private DataGridViewTextBoxColumn colWilayah;
        private DataGridViewTextBoxColumn colJenis;
        private DataGridViewTextBoxColumn colBerat;
        private DataGridViewTextBoxColumn colStatus;
        private FontAwesome.Sharp.IconButton btnLogout;
        private Label lblUserLogin;
        private FontAwesome.Sharp.IconButton btnBukaMap;
    }
}