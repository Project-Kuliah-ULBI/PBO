using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SiJabarApp.helper;
using System.Threading.Tasks;

namespace SiJabarApp
{
    public partial class FileIOControl : UserControl
    {
        // Events to communicate with MainForm
        public event EventHandler OnExportPdfRequested;
        public event EventHandler OnDataImported;

        private Panel panelDropZone;
        private Label lblDropInfo;
        private Button btnBrowse;
        private Button btnExport;
        private OpenFileDialog openFileDialog;

        // Visual Elements (Promoted to Fields)
        private Label lblTitle;
        private GroupBox grpExport;
        private GroupBox grpImport;

        public FileIOControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;
            this.Padding = new Padding(30);

            // 1. TITLE
            lblTitle = new Label();
            lblTitle.Text = "Import & Export Data";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(44, 62, 80);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 20);
            this.Controls.Add(lblTitle);

            // 2. EXPORT SECTION
            grpExport = CreateGroupBox("Export Data", 80);
            grpExport.Size = new Size(740, 200); 
            grpExport.Visible = false; // Default Hidden

            Label lblExportInfo = new Label();
            lblExportInfo.Text = "Ekspor data tabel sampah saat ini ke format PDF laporan.\nPastikan data tabel sudah sesuai sebelum melakukan ekspor.";
            lblExportInfo.Font = new Font("Segoe UI", 10);
            lblExportInfo.Location = new Point(20, 30);
            lblExportInfo.AutoSize = true;
            grpExport.Controls.Add(lblExportInfo);

            btnExport = new Button();
            btnExport.Text = "Export PDF";
            btnExport.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnExport.Size = new Size(150, 45);
            btnExport.Location = new Point(20, 80);
            btnExport.BackColor = Color.FromArgb(231, 76, 60); // Red
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += (s, e) => OnExportPdfRequested?.Invoke(this, EventArgs.Empty);
            grpExport.Controls.Add(btnExport);

            this.Controls.Add(grpExport);

            // 3. IMPORT SECTION
            grpImport = CreateGroupBox("Import CSV (Open Data Jabar)", 80); // Same Y position
            grpImport.Size = new Size(740, 320);
            grpImport.Visible = false; // Default Hidden

            // DROP ZONE PANEL
            panelDropZone = new Panel();
            panelDropZone.Location = new Point(20, 30);
            panelDropZone.Size = new Size(grpImport.Width - 40, 180);
            panelDropZone.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelDropZone.BackColor = Color.FromArgb(248, 249, 250);
            panelDropZone.BorderStyle = BorderStyle.FixedSingle;
            panelDropZone.AllowDrop = true;
            
            // Events
            panelDropZone.DragEnter += PanelDropZone_DragEnter;
            panelDropZone.DragLeave += PanelDropZone_DragLeave;
            panelDropZone.DragDrop += PanelDropZone_DragDrop;

            // Labels inside Drop Zone
            lblDropInfo = new Label();
            lblDropInfo.Text = "Drag & Drop file CSV di sini\nAtau klik tombol di bawah";
            lblDropInfo.TextAlign = ContentAlignment.MiddleCenter;
            lblDropInfo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblDropInfo.ForeColor = Color.Gray;
            lblDropInfo.AutoSize = false;
            lblDropInfo.Dock = DockStyle.Fill;
            lblDropInfo.Click += (s, e) => OpenCsvDialog();
            panelDropZone.Controls.Add(lblDropInfo);

            grpImport.Controls.Add(panelDropZone);
            this.Controls.Add(grpImport);

            // Browse Button (Manual)
            btnBrowse = new Button();
            btnBrowse.Text = "Pilih File...";
            btnBrowse.Location = new Point(20, 220);
            btnBrowse.Size = new Size(120, 35);
            btnBrowse.Font = new Font("Segoe UI", 10);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.BackColor = Color.FromArgb(46, 204, 113);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Click += (s, e) => OpenCsvDialog();
            grpImport.Controls.Add(btnBrowse);

            // AI Knowledge Info Label
            Label lblAiInfo = new Label();
            lblAiInfo.Text = "ℹ️ Note: Data CSV yang diimpor akan diproses menjadi Knowledge AI (RAG).";
            lblAiInfo.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblAiInfo.ForeColor = Color.DimGray;
            lblAiInfo.AutoSize = true;
            lblAiInfo.Location = new Point(20, 270);
            grpImport.Controls.Add(lblAiInfo);
        }

        public void ShowImportMode()
        {
            lblTitle.Text = "Import Data CSV";
            grpImport.Visible = true;
            grpExport.Visible = false;
        }

        public void ShowExportMode()
        {
            lblTitle.Text = "Export Laporan PDF";
            grpImport.Visible = false;
            grpExport.Visible = true;
        }

        private GroupBox CreateGroupBox(string title, int yPos)
        {
            GroupBox grp = new GroupBox();
            grp.Text = title;
            grp.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            grp.ForeColor = Color.FromArgb(44, 62, 80);
            grp.Location = new Point(30, yPos);
            grp.Size = new Size(740, 280);
            grp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            return grp;
        }

        private void PanelDropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                panelDropZone.BackColor = Color.FromArgb(230, 240, 255); // Highlight Blue
            }
        }

        private void PanelDropZone_DragLeave(object sender, EventArgs e)
        {
            panelDropZone.BackColor = Color.FromArgb(248, 249, 250); // Reset
        }

        private async void PanelDropZone_DragDrop(object sender, DragEventArgs e)
        {
            panelDropZone.BackColor = Color.FromArgb(248, 249, 250);
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await ProcessFile(files[0]);
            }
        }

        private void OpenCsvDialog()
        {
            openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Pilih File CSV Open Data Jabar"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               _ = ProcessFile(openFileDialog.FileName);
            }
        }

        private async Task ProcessFile(string filePath)
        {
            if (Path.GetExtension(filePath).ToLower() != ".csv")
            {
                MessageBox.Show("Harap pilih file berformat .CSV!", "Format Salah", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblDropInfo.Text = "Sedang memproses data... Mohon tunggu.";
            this.Enabled = false;

            try
            {
                var ingestion = new CsvIngestionHelper();
                await ingestion.ProcessOpenDataCsv(filePath);
                
                MessageBox.Show("Data CSV berhasil diproses dan disimpan ke Knowledge Base!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnDataImported?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memproses file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblDropInfo.Text = "Drag & Drop file CSV di sini\nAtau klik tombol di bawah";
                this.Enabled = true;
            }
        }
    }
}
