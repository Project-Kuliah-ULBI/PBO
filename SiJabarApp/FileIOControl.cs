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
        public event EventHandler OnExportPdfRequested;
        public event EventHandler OnDataImported;

        private Panel panelDropZone;
        private Label lblDropInfo;
        private Button btnBrowse;
        private Button btnExport;
        private OpenFileDialog openFileDialog;

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

            lblTitle = new Label();
            lblTitle.Text = "Impor & Ekspor Data";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(44, 62, 80);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 20);
            this.Controls.Add(lblTitle);

            grpExport = CreateGroupBox("Ekspor Data", 80);
            grpExport.Size = new Size(740, 200); 
            grpExport.Visible = false;

            Label lblExportInfo = new Label();
            lblExportInfo.Text = "Ekspor data tabel sampah saat ini ke format laporan PDF.\nPastikan data tabel sudah benar sebelum mengekspor.";
            lblExportInfo.Font = new Font("Segoe UI", 10);
            lblExportInfo.Location = new Point(20, 30);
            lblExportInfo.AutoSize = true;
            grpExport.Controls.Add(lblExportInfo);

            btnExport = new Button();
            btnExport.Text = "Ekspor PDF";
            btnExport.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnExport.Size = new Size(150, 45);
            btnExport.Location = new Point(20, 80);
            btnExport.BackColor = Color.FromArgb(231, 76, 60);
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += (s, e) => OnExportPdfRequested?.Invoke(this, EventArgs.Empty);
            grpExport.Controls.Add(btnExport);

            this.Controls.Add(grpExport);

            grpImport = CreateGroupBox("Impor CSV & PDF", 80);
            grpImport.Size = new Size(740, 320);
            grpImport.Visible = false;

            panelDropZone = new Panel();
            panelDropZone.Location = new Point(20, 30);
            panelDropZone.Size = new Size(grpImport.Width - 40, 180);
            panelDropZone.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelDropZone.BackColor = Color.FromArgb(248, 249, 250);
            panelDropZone.BorderStyle = BorderStyle.FixedSingle;
            panelDropZone.AllowDrop = true;
            
            panelDropZone.DragEnter += PanelDropZone_DragEnter;
            panelDropZone.DragLeave += PanelDropZone_DragLeave;
            panelDropZone.DragDrop += PanelDropZone_DragDrop;

            lblDropInfo = new Label();
            lblDropInfo.Text = "Drag & Drop CSV/PDF file here\nOr click the button below";
            lblDropInfo.TextAlign = ContentAlignment.MiddleCenter;
            lblDropInfo.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblDropInfo.ForeColor = Color.Gray;
            lblDropInfo.AutoSize = false;
            lblDropInfo.Dock = DockStyle.Fill;
            lblDropInfo.Click += (s, e) => OpenFileDialog();
            panelDropZone.Controls.Add(lblDropInfo);

            grpImport.Controls.Add(panelDropZone);
            this.Controls.Add(grpImport);

            btnBrowse = new Button();
            btnBrowse.Text = "Pilih File...";
            btnBrowse.Location = new Point(20, 220);
            btnBrowse.Size = new Size(120, 35);
            btnBrowse.Font = new Font("Segoe UI", 10);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.BackColor = Color.FromArgb(46, 204, 113);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Click += (s, e) => OpenFileDialog();
            grpImport.Controls.Add(btnBrowse);

            Label lblAiInfo = new Label();
            lblAiInfo.Text = "ℹ️ Catatan: Data CSV/PDF yang diimpor akan diproses ke AI Knowledge (RAG).";
            lblAiInfo.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblAiInfo.ForeColor = Color.DimGray;
            lblAiInfo.AutoSize = true;
            lblAiInfo.Location = new Point(20, 270);
            grpImport.Controls.Add(lblAiInfo);
        }

        public void ShowImportMode()
        {
            lblTitle.Text = "Impor Data CSV/PDF";
            grpImport.Visible = true;
            grpExport.Visible = false;
        }

        public void ShowExportMode()
        {
            lblTitle.Text = "Ekspor Laporan PDF";
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
                panelDropZone.BackColor = Color.FromArgb(230, 240, 255);
            }
        }

        private void PanelDropZone_DragLeave(object sender, EventArgs e)
        {
            panelDropZone.BackColor = Color.FromArgb(248, 249, 250);
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

        private void OpenFileDialog()
        {
            openFileDialog = new OpenFileDialog
            {
                Filter = "Data Files (*.csv;*.pdf)|*.csv;*.pdf|CSV Files (*.csv)|*.csv|PDF Files (*.pdf)|*.pdf",
                Title = "Pilih File CSV/PDF untuk Basis Pengetahuan AI"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               _ = ProcessFile(openFileDialog.FileName);
            }
        }

        private async Task ProcessFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".csv" && ext != ".pdf")
            {
                MessageBox.Show("Silakan pilih file .CSV atau .PDF!", "Format Tidak Valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblDropInfo.Text = "Memproses data... Harap tunggu.";
            this.Enabled = false;

            try
            {
                if (ext == ".csv")
                {
                    var ingestion = new CsvIngestionHelper();
                    await ingestion.ProcessOpenDataCsv(filePath);
                    MessageBox.Show("Data CSV diproses dan disimpan ke Basis Pengetahuan!", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (ext == ".pdf")
                {
                    var ingestion = new PdfIngestionHelper();
                    await ingestion.ProcessPdf(filePath);
                    MessageBox.Show("File PDF diproses dan disimpan ke Basis Pengetahuan!", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                OnDataImported?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memproses file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lblDropInfo.Text = "Tarik & Lepas file CSV/PDF di sini\nAtau klik tombol di bawah";
                this.Enabled = true;
            }
        }
    }
}
