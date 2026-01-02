using FontAwesome.Sharp;                  // Library Icon
using iText.IO.Font;
using iText.Kernel.Font;
// --- LIBRARY PDF (ITEXT7) ---
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using MongoDB.Bson;                       // Wajib untuk ObjectId
using MongoDB.Bson.Serialization.Attributes; // Wajib untuk Model
using MongoDB.Driver;                     // Driver Database
using System;
using System.Drawing;
using System.Runtime.InteropServices;     // Untuk Drag Window
using System.Windows.Forms;

namespace SiJabarApp
{
    // ==========================================
    // 1. CLASS FORM UTAMA
    // ==========================================
    public partial class MainForm : Form
    {
        // --- VARIABEL GLOBAL ---
        private IMongoCollection<SampahModel> collection;

        // --- SETUP DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        public MainForm()
        {
            InitializeComponent();
            ConnectToMongoDB();
            SetupStyling();
            LoadData();
        }

        // --- 1. KONEKSI DATABASE ---
        private void ConnectToMongoDB()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("SiJabarDB");
                collection = database.GetCollection<SampahModel>("Sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal koneksi ke Database: " + ex.Message);
            }
        }

        // --- 2. STYLING TABEL ---
        private void SetupStyling()
        {
            // Matikan style bawaan
            gridSampah.EnableHeadersVisualStyles = false;

            // Header
            gridSampah.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridSampah.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gridSampah.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            gridSampah.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
            gridSampah.ColumnHeadersDefaultCellStyle.Font = new Font("Century Gothic", 10, FontStyle.Bold);
            gridSampah.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridSampah.ColumnHeadersHeight = 50;

            // Isi Data
            gridSampah.DefaultCellStyle.Font = new Font("Century Gothic", 10);
            gridSampah.DefaultCellStyle.BackColor = Color.White;
            gridSampah.DefaultCellStyle.ForeColor = Color.Black;
            gridSampah.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
            gridSampah.DefaultCellStyle.SelectionForeColor = Color.Black;
            gridSampah.RowTemplate.Height = 45;

            // Aktifkan Sorting
            foreach (DataGridViewColumn col in gridSampah.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            // Sembunyikan ID
            if (gridSampah.Columns["colId"] != null)
                gridSampah.Columns["colId"].Visible = false;
        }

        // --- 3. READ DATA ---
        private void LoadData()
        {
            try
            {
                var dataList = collection.Find(_ => true).ToList();
                gridSampah.Rows.Clear();

                foreach (var item in dataList)
                {
                    gridSampah.Rows.Add(item.Id, item.Wilayah, item.Jenis, item.Berat, item.Status);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        // ==========================================================
        // EVENTS BUTTON
        // ==========================================================

        private void btnAdd_Click(object sender, EventArgs e)
        {
            FormInput frm = new FormInput();
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                string idTerpilih = gridSampah.SelectedRows[0].Cells[0].Value.ToString();
                FormInput frm = new FormInput(idTerpilih);
                if (frm.ShowDialog() == DialogResult.OK) LoadData();
            }
            else
            {
                MessageBox.Show("Pilih baris data yang ingin diedit!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                var konfirmasi = MessageBox.Show("Yakin ingin menghapus data ini?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (konfirmasi == DialogResult.Yes)
                {
                    try
                    {
                        string idTerpilih = gridSampah.SelectedRows[0].Cells[0].Value.ToString();
                        var filter = Builders<SampahModel>.Filter.Eq(x => x.Id, idTerpilih);
                        collection.DeleteOne(filter);
                        LoadData();
                        MessageBox.Show("Data berhasil dihapus.");
                    }
                    catch (Exception ex) { MessageBox.Show("Gagal menghapus: " + ex.Message); }
                }
            }
            else
            {
                MessageBox.Show("Pilih baris data yang ingin dihapus!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            // 1. Cek Data Kosong
            if (gridSampah.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data untuk diekspor!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Pilih Lokasi Simpan
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = "Laporan Data Sampah SiJabar.pdf";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 3. Setup Font Manual (AMBIL DARI WINDOWS) -> SOLUSI ANTI ERROR
                    string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    PdfFont fontArial = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

                    // 4. Mulai Membuat PDF
                    using (PdfWriter writer = new PdfWriter(sfd.FileName))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document doc = new Document(pdf))
                    {
                        // SET FONT GLOBAL
                        doc.SetFont(fontArial);

                        // A. JUDUL
                        Paragraph judul = new Paragraph("LAPORAN DATA SAMPAH JAWA BARAT")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(18);
                        doc.Add(judul);

                        // B. SUB-JUDUL
                        Paragraph subJudul = new Paragraph("Dicetak pada: " + DateTime.Now.ToString("dd MMMM yyyy HH:mm"))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(10)
                            .SetMarginBottom(20);
                        doc.Add(subJudul);

                        // C. TABEL (4 Kolom)
                        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2 }));
                        table.SetWidth(UnitValue.CreatePercentValue(100));

                        // D. HEADER TABEL
                        string[] headers = { "Wilayah", "Jenis Sampah", "Berat (Kg)", "Status" };
                        foreach (string h in headers)
                        {
                            Cell cell = new Cell().Add(new Paragraph(new Text(h))); 
                            cell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
                            cell.SetTextAlignment(TextAlignment.CENTER);
                            table.AddHeaderCell(cell);
                        }

                        // E. ISI DATA (SEMUA RATA TENGAH)
                        foreach (DataGridViewRow row in gridSampah.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string wilayah = row.Cells[1].Value?.ToString() ?? "-";
                            string jenis = row.Cells[2].Value?.ToString() ?? "-";
                            string berat = row.Cells[3].Value?.ToString() ?? "0";
                            string status = row.Cells[4].Value?.ToString() ?? "-";

                            // Masukkan data dengan .SetTextAlignment(TextAlignment.CENTER)
                            table.AddCell(new Paragraph(wilayah).SetTextAlignment(TextAlignment.CENTER));
                            table.AddCell(new Paragraph(jenis).SetTextAlignment(TextAlignment.CENTER));
                            table.AddCell(new Paragraph(berat).SetTextAlignment(TextAlignment.CENTER));
                            table.AddCell(new Paragraph(status).SetTextAlignment(TextAlignment.CENTER));
                        }

                        doc.Add(table);
                    }

                    MessageBox.Show("PDF Berhasil Disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("File PDF sedang terbuka! Tutup file tersebut lalu coba lagi.", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Detail: " + ex.ToString(), "Gagal Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // --- WINDOW CONTROLS ---
        private void btnClose_Click(object sender, EventArgs e) => Application.Exit();
        private void btnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;
        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
                btnMaximize.IconChar = IconChar.WindowRestore;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                btnMaximize.IconChar = IconChar.Square;
            }
        }
        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        // --- NAVIGASI ---
        private void btnDataSampah_Click(object sender, EventArgs e) => LoadData();
        private void btnChatbot_Click(object sender, EventArgs e) => MessageBox.Show("Fitur Chatbot akan segera hadir!");
        private void btnLogout_Click(object sender, EventArgs e)
        {
            var jawab = MessageBox.Show("Apakah Anda yakin ingin keluar?", "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (jawab == DialogResult.Yes) Application.Exit();
        }
    }

    // ==========================================
    // 2. MODEL DATA
    // ==========================================
    public class SampahModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Wilayah { get; set; }
        public string Jenis { get; set; }
        public double Berat { get; set; }
        public string Status { get; set; }
    }
}