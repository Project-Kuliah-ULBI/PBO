namespace SiJabarApp
{
    partial class FormInput
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormInput));
            panelHeader = new Panel();
            btnClose = new FontAwesome.Sharp.IconButton();
            lblHeaderTitle = new Label();
            label2 = new Label();
            comboWilayah = new ComboBox();
            label3 = new Label();
            comboJenis = new ComboBox();
            label4 = new Label();
            numBerat = new NumericUpDown();
            label5 = new Label();
            comboStatus = new ComboBox();
            btnSimpan = new FontAwesome.Sharp.IconButton();
            btnBatal = new FontAwesome.Sharp.IconButton();
            panelMapInput = new Panel();
            txtLatitude = new TextBox();
            txtLongitude = new TextBox();
            btnReset = new FontAwesome.Sharp.IconButton();
            label1 = new Label();
            label6 = new Label();
            label7 = new Label();
            txtKeterangan = new TextBox();
            dtpTanggal = new DateTimePicker();
            dtpJadwal = new DateTimePicker();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBerat).BeginInit();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(46, 204, 113);
            panelHeader.Controls.Add(btnClose);
            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1137, 60);
            panelHeader.TabIndex = 0;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.BackColor = Color.FromArgb(33, 37, 41);
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnClose.IconChar = FontAwesome.Sharp.IconChar.Close;
            btnClose.IconColor = Color.White;
            btnClose.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnClose.IconSize = 20;
            btnClose.Location = new Point(1092, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(45, 40);
            btnClose.TabIndex = 2;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnClose_Click;
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.BackColor = Color.FromArgb(33, 37, 41);
            lblHeaderTitle.Dock = DockStyle.Fill;
            lblHeaderTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(0, 0);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Padding = new Padding(0, 0, 0, 10);
            lblHeaderTitle.Size = new Size(1137, 60);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "INPUT DATA SAMPAH";
            lblHeaderTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblHeaderTitle.MouseDown += panelHeader_MouseDown;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.Black;
            label2.Location = new Point(30, 80);
            label2.Name = "label2";
            label2.Size = new Size(133, 23);
            label2.TabIndex = 1;
            label2.Text = "Wilayah / Lokasi";
            // 
            // comboWilayah
            // 
            comboWilayah.FormattingEnabled = true;
            comboWilayah.Items.AddRange(new object[] { "Kabupaten Bandung", "Kabupaten Bandung Barat", "Kabupaten Bekasi", "Kabupaten Bogor", "Kabupaten Ciamis", "Kabupaten Cianjur", "Kabupaten Cirebon", "Kabupaten Garut", "Kabupaten Indramayu", "Kabupaten Karawang", "Kabupaten Kuningan", "Kabupaten Majalengka", "Kabupaten Pangandaran", "Kabupaten Purwakarta", "Kabupaten Subang", "Kabupaten Sukabumi", "Kabupaten Sumedang", "Kabupaten Tasikmalaya", "Kota Bandung", "Kota Banjar", "Kota Bekasi", "Kota Bogor", "Kota Cimahi", "Kota Cirebon", "Kota Depok", "Kota Sukabumi", "Kota Tasikmalaya" });
            comboWilayah.Location = new Point(30, 108);
            comboWilayah.Name = "comboWilayah";
            comboWilayah.Size = new Size(390, 31);
            comboWilayah.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(30, 144);
            label3.Name = "label3";
            label3.Size = new Size(113, 23);
            label3.TabIndex = 3;
            label3.Text = "Jenis Sampah";
            // 
            // comboJenis
            // 
            comboJenis.DropDownStyle = ComboBoxStyle.DropDownList;
            comboJenis.FormattingEnabled = true;
            comboJenis.Items.AddRange(new object[] { "Organik", "Anorganik", "B3 (Bahan Berbahaya)", "Residu" });
            comboJenis.Location = new Point(30, 172);
            comboJenis.Name = "comboJenis";
            comboJenis.Size = new Size(390, 31);
            comboJenis.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(30, 208);
            label4.Name = "label4";
            label4.Size = new Size(152, 23);
            label4.TabIndex = 5;
            label4.Text = "Berat Sampah (Kg)";
            // 
            // numBerat
            // 
            numBerat.DecimalPlaces = 2;
            numBerat.Location = new Point(30, 236);
            numBerat.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numBerat.Name = "numBerat";
            numBerat.Size = new Size(390, 30);
            numBerat.TabIndex = 6;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(30, 271);
            label5.Name = "label5";
            label5.Size = new Size(151, 23);
            label5.TabIndex = 7;
            label5.Text = "Status Pengolahan";
            // 
            // comboStatus
            // 
            comboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboStatus.FormattingEnabled = true;
            comboStatus.Items.AddRange(new object[] { "Masuk", "Dipilah", "Daur Ulang", "Selesai", "Diangkut ke TPA" });
            comboStatus.Location = new Point(30, 299);
            comboStatus.Name = "comboStatus";
            comboStatus.Size = new Size(390, 31);
            comboStatus.TabIndex = 8;
            // 
            // btnSimpan
            // 
            btnSimpan.BackColor = Color.FromArgb(46, 204, 113);
            btnSimpan.FlatAppearance.BorderSize = 0;
            btnSimpan.FlatStyle = FlatStyle.Flat;
            btnSimpan.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSimpan.ForeColor = Color.White;
            btnSimpan.IconChar = FontAwesome.Sharp.IconChar.Save;
            btnSimpan.IconColor = Color.White;
            btnSimpan.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnSimpan.IconSize = 22;
            btnSimpan.Location = new Point(29, 578);
            btnSimpan.Name = "btnSimpan";
            btnSimpan.Size = new Size(105, 45);
            btnSimpan.TabIndex = 9;
            btnSimpan.Text = "SIMPAN";
            btnSimpan.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnSimpan.UseVisualStyleBackColor = false;
            btnSimpan.Click += btnSimpan_Click;
            // 
            // btnBatal
            // 
            btnBatal.BackColor = Color.FromArgb(220, 53, 69);
            btnBatal.FlatAppearance.BorderSize = 0;
            btnBatal.FlatStyle = FlatStyle.Flat;
            btnBatal.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBatal.ForeColor = Color.White;
            btnBatal.IconChar = FontAwesome.Sharp.IconChar.Cancel;
            btnBatal.IconColor = Color.White;
            btnBatal.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnBatal.IconSize = 22;
            btnBatal.Location = new Point(171, 578);
            btnBatal.Name = "btnBatal";
            btnBatal.Size = new Size(105, 45);
            btnBatal.TabIndex = 10;
            btnBatal.Text = "BATAL";
            btnBatal.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBatal.UseVisualStyleBackColor = false;
            btnBatal.Click += btnBatal_Click;
            // 
            // panelMapInput
            // 
            panelMapInput.Location = new Point(454, 80);
            panelMapInput.Name = "panelMapInput";
            panelMapInput.Size = new Size(655, 543);
            panelMapInput.TabIndex = 11;
            // 
            // txtLatitude
            // 
            txtLatitude.Location = new Point(74, 542);
            txtLatitude.Name = "txtLatitude";
            txtLatitude.Size = new Size(125, 30);
            txtLatitude.TabIndex = 12;
            txtLatitude.Visible = false;
            // 
            // txtLongitude
            // 
            txtLongitude.Location = new Point(229, 542);
            txtLongitude.Name = "txtLongitude";
            txtLongitude.Size = new Size(125, 30);
            txtLongitude.TabIndex = 13;
            txtLongitude.Visible = false;
            // 
            // btnReset
            // 
            btnReset.BackColor = Color.FromArgb(192, 64, 0);
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnReset.ForeColor = Color.White;
            btnReset.IconChar = FontAwesome.Sharp.IconChar.Refresh;
            btnReset.IconColor = Color.White;
            btnReset.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnReset.IconSize = 22;
            btnReset.Location = new Point(313, 578);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(105, 45);
            btnReset.TabIndex = 14;
            btnReset.Text = "RESET";
            btnReset.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnReset.UseVisualStyleBackColor = false;
            btnReset.Click += btnReset_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(29, 335);
            label1.Name = "label1";
            label1.Size = new Size(69, 23);
            label1.TabIndex = 15;
            label1.Text = "Tanggal";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(29, 461);
            label6.Name = "label6";
            label6.Size = new Size(98, 23);
            label6.TabIndex = 16;
            label6.Text = "Keterangan";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(29, 398);
            label7.Name = "label7";
            label7.Size = new Size(175, 23);
            label7.TabIndex = 17;
            label7.Text = "Jadwal Pengangkutan";
            // 
            // txtKeterangan
            // 
            txtKeterangan.Location = new Point(30, 489);
            txtKeterangan.Name = "txtKeterangan";
            txtKeterangan.Size = new Size(390, 30);
            txtKeterangan.TabIndex = 18;
            // 
            // dtpTanggal
            // 
            dtpTanggal.Format = DateTimePickerFormat.Short;
            dtpTanggal.Location = new Point(30, 363);
            dtpTanggal.Name = "dtpTanggal";
            dtpTanggal.Size = new Size(250, 30);
            dtpTanggal.TabIndex = 19;
            // 
            // dtpJadwal
            // 
            dtpJadwal.Format = DateTimePickerFormat.Short;
            dtpJadwal.Location = new Point(30, 426);
            dtpJadwal.Name = "dtpJadwal";
            dtpJadwal.Size = new Size(250, 30);
            dtpJadwal.TabIndex = 20;
            // 
            // FormInput
            // 
            AutoScaleDimensions = new SizeF(9F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1137, 645);
            Controls.Add(dtpJadwal);
            Controls.Add(dtpTanggal);
            Controls.Add(txtKeterangan);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label1);
            Controls.Add(btnReset);
            Controls.Add(txtLongitude);
            Controls.Add(txtLatitude);
            Controls.Add(panelMapInput);
            Controls.Add(btnBatal);
            Controls.Add(btnSimpan);
            Controls.Add(comboStatus);
            Controls.Add(label5);
            Controls.Add(numBerat);
            Controls.Add(label4);
            Controls.Add(comboJenis);
            Controls.Add(label3);
            Controls.Add(comboWilayah);
            Controls.Add(label2);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormInput";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FormInput";
            panelHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numBerat).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelHeader;
        private Label lblHeaderTitle;
        private FontAwesome.Sharp.IconButton btnClose;
        private Label label2;
        private ComboBox comboWilayah;
        private Label label3;
        private ComboBox comboJenis;
        private Label label4;
        private NumericUpDown numBerat;
        private Label label5;
        private ComboBox comboStatus;
        private FontAwesome.Sharp.IconButton btnSimpan;
        private FontAwesome.Sharp.IconButton btnBatal;
        private Panel panelMapInput;
        private TextBox txtLatitude;
        private TextBox txtLongitude;
        private FontAwesome.Sharp.IconButton btnReset;
        private Label label1;
        private Label label6;
        private Label label7;
        private TextBox txtKeterangan;
        private DateTimePicker dtpTanggal;
        private DateTimePicker dtpJadwal;
    }
}