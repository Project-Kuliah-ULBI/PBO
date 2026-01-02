using System;
using System.Drawing;
using System.Windows.Forms;
using MongoDB.Bson;                       // Wajib untuk ObjectId
using MongoDB.Bson.Serialization.Attributes; // Wajib untuk Model
using MongoDB.Driver;                     // Driver Database
using FontAwesome.Sharp;                  // Library Icon
using System.Runtime.InteropServices;     // Untuk Drag Window

// --- LIBRARY PDF (ITEXT7) ---
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;

namespace SiJabarApp
{
    // ==========================================
    // 1. CLASS FORM UTAMA
    // ==========================================
    public partial class MainForm : Form
    {
        // --- VARIABEL GLOBAL ---
        private IMongoCollection<SampahModel> collection;

        // --- VARIABEL SESI USER (PENTING) ---
        private string activeUserId;
        private string activeUserName;
        public string currentUserId; // Set nilai ini saat login berhasil

        private Chatbot chatbotPopup;

        // --- SETUP DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // --- KONSTRUKTOR (Terima ID dan Nama User) ---
        public MainForm(string userId, string userName)
        {
            InitializeComponent();

            // 1. Simpan Data Sesi
            this.activeUserId = userId;
            this.activeUserName = userName;

            // 2. Setup Awal
            ConnectToMongoDB();
            SetupStyling();
            LoadData();

            // --- BAGIAN BARU: SET LABEL USER ---
            // Kode ini akan error jika kamu belum membuat label "lblUserLogin" di Designer
            // Pastikan (Name) label di properties sudah diubah menjadi: lblUserLogin
            if (lblUserLogin != null)
            {
                lblUserLogin.Text = $"Halo, {activeUserName}!";
            }
            // -----------------------------------
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
            gridSampah.EnableHeadersVisualStyles = false;

            // Header
            gridSampah.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridSampah.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gridSampah.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            gridSampah.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
            gridSampah.ColumnHeadersDefaultCellStyle.Font = new Font("Century Gothic", 10, FontStyle.Bold);
            gridSampah.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridSampah.ColumnHeadersHeight = 50;

            // Rows
            gridSampah.DefaultCellStyle.Font = new Font("Century Gothic", 10);
            gridSampah.DefaultCellStyle.BackColor = Color.White;
            gridSampah.DefaultCellStyle.ForeColor = Color.Black;
            gridSampah.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
            gridSampah.DefaultCellStyle.SelectionForeColor = Color.Black;
            gridSampah.RowTemplate.Height = 45;

            // Sorting
            foreach (DataGridViewColumn col in gridSampah.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            // Hide ID & UserId
            if (gridSampah.Columns["colId"] != null) gridSampah.Columns["colId"].Visible = false;
        }

        // --- 3. READ DATA (FILTER BY USER ID) ---
        private void LoadData()
        {
            try
            {
                // HANYA AMBIL DATA MILIK USER YANG LOGIN
                var filter = Builders<SampahModel>.Filter.Eq(x => x.UserId, activeUserId);
                var dataList = collection.Find(filter).ToList();

                gridSampah.Rows.Clear();

                foreach (var item in dataList)
                {
                    // Pastikan urutan kolom di GridView Designer sesuai:
                    // [0] ID, [1] Wilayah, [2] Jenis, [3] Berat, [4] Status
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
            // Kirim ID User saat menambah data baru
            FormInput frm = new FormInput(activeUserId);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                string idTerpilih = gridSampah.SelectedRows[0].Cells[0].Value.ToString();
                // Mode Edit: Kirim ID Data, flag true
                FormInput frm = new FormInput(idTerpilih, true);
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

        // --- FITUR EXPORT PDF ---
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (gridSampah.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data untuk diekspor!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = $"Laporan Sampah - {activeUserName}.pdf"; // Nama file pakai nama user

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    PdfFont fontArial = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

                    using (PdfWriter writer = new PdfWriter(sfd.FileName))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document doc = new Document(pdf))
                    {
                        doc.SetFont(fontArial);

                        // HEADER
                        Paragraph judul = new Paragraph("LAPORAN DATA SAMPAH JAWA BARAT")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(18);
                        doc.Add(judul);

                        Paragraph subJudul = new Paragraph($"Oleh User: {activeUserName}\nDicetak pada: " + DateTime.Now.ToString("dd MMMM yyyy HH:mm"))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(10)
                            .SetMarginBottom(20);
                        doc.Add(subJudul);

                        // TABEL
                        Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2 }));
                        table.SetWidth(UnitValue.CreatePercentValue(100));

                        string[] headers = { "Wilayah", "Jenis Sampah", "Berat (Kg)", "Status" };
                        foreach (string h in headers)
                        {
                            Cell cell = new Cell().Add(new Paragraph(new Text(h))); // Header Bold Manual di Text object jika mau
                            cell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
                            cell.SetTextAlignment(TextAlignment.CENTER);
                            table.AddHeaderCell(cell);
                        }

                        // ISI DATA
                        foreach (DataGridViewRow row in gridSampah.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string wilayah = row.Cells[1].Value?.ToString() ?? "-";
                            string jenis = row.Cells[2].Value?.ToString() ?? "-";
                            string berat = row.Cells[3].Value?.ToString() ?? "0";
                            string status = row.Cells[4].Value?.ToString() ?? "-";

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
        private void btnChatbot_Click(object sender, EventArgs e)
        {
            if (chatbotPopup == null || chatbotPopup.IsDisposed)
            {
                // PERBAIKAN: Kirim activeUserId ke constructor Chatbot
                chatbotPopup = new Chatbot(activeUserId);

                int x = this.Location.X + this.Width - chatbotPopup.Width - 20;
                int y = this.Location.Y + this.Height - chatbotPopup.Height - 20;
                chatbotPopup.StartPosition = FormStartPosition.Manual;
                chatbotPopup.Location = new Point(x, y);
                chatbotPopup.Owner = this;
                chatbotPopup.Show();
            }
            else
            {
                if (chatbotPopup.Visible) chatbotPopup.Hide();
                else { chatbotPopup.Show(); chatbotPopup.BringToFront(); }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var jawab = MessageBox.Show($"Sampai jumpa, {activeUserName}. Yakin ingin keluar?", "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (jawab == DialogResult.Yes)
            {
                // Restart Aplikasi agar kembali ke Login bersih
                Application.Restart();
            }
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

        public string UserId { get; set; }

        public string Wilayah { get; set; }
        public string Jenis { get; set; }
        public double Berat { get; set; }
        public string Status { get; set; }
    }
}