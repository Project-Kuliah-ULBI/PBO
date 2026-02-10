using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

// --- LIBRARY DATABASE ---
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// --- LIBRARY UI & PDF ---
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;

using SiJabarApp.helper;
using SiJabarApp.model;

namespace SiJabarApp
{
    public partial class MainForm : Form
    {
        // --- VARIABEL GLOBAL ---
        private IMongoCollection<SampahModel> collection;

        // --- VARIABEL SESI USER & ROLE ---
        private string activeUserId;
        private string activeUserName;
        public string activeRole;

        private Chatbot chatbotPopup;

        // --- SETUP DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // =============================================================
        // 1. KONSTRUKTOR KOSONG (WAJIB ADA UNTUK DESIGNER)
        // =============================================================
        public MainForm()
        {
            InitializeComponent();
            // Inisialisasi default agar designer tidak error
            this.activeRole = "Masyarakat";
            ConnectToMongoDB();
            SetupStyling();
        }

        // =============================================================
        // 2. KONSTRUKTOR UTAMA (DIPANGGIL SAAT LOGIN)
        // =============================================================
        public MainForm(string userId, string userName, string userRole)
        {
            InitializeComponent();

            // 1. Simpan Data Sesi
            this.activeUserId = userId;
            this.activeUserName = userName;
            this.activeRole = userRole;

            // 2. Setup Awal
            ConnectToMongoDB();
            SetupStyling();

            // 3. ATUR HAK AKSES 
            ApplyRolePermissions();

            // 4. Load Data
            LoadData();

            // 5. Set Label User
            if (lblUserLogin != null)
            {
                lblUserLogin.Text = $"Halo, {activeUserName} ({activeRole})";
            }
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

        // --- 2. LOGIKA HAK AKSES (ROLE PERMISSIONS) ---
        private void ApplyRolePermissions()
        {
            // PENTING: Cek null agar Designer tidak crash
            if (btnAdd == null) return;

            // Default: Semua nyala dulu
            btnAdd.Visible = true;
            btnEdit.Visible = true;
            btnDelete.Visible = true;

            if (activeRole == "Masyarakat")
            {
                // Masyarakat: Hanya Lapor, Tidak boleh Edit/Hapus
                btnEdit.Visible = false;
                btnDelete.Visible = false;
            }
            if (activeRole != "Admin")
            {
                btnImportCSV.Visible = false;
            }
        }

        // --- 3. STYLING TABEL ---
        private void SetupStyling()
        {
            if (gridSampah == null) return;

            gridSampah.EnableHeadersVisualStyles = false;
            gridSampah.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridSampah.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gridSampah.ColumnHeadersDefaultCellStyle.Font = new Font("Century Gothic", 9, FontStyle.Bold);
            gridSampah.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridSampah.ColumnHeadersHeight = 50;

            gridSampah.DefaultCellStyle.Font = new Font("Century Gothic", 9);
            gridSampah.RowTemplate.Height = 45;
            gridSampah.AllowUserToAddRows = false;

            // DEFINISI KOLOM
            gridSampah.Columns.Clear();
            gridSampah.Columns.Add("colId", "ID");
            gridSampah.Columns["colId"].Visible = false;

            gridSampah.Columns.Add("colWilayah", "Wilayah");
            gridSampah.Columns.Add("colJenis", "Jenis");
            gridSampah.Columns.Add("colBerat", "Berat (Kg)");
            gridSampah.Columns.Add("colStatus", "Status");
            gridSampah.Columns.Add("colTanggal", "Tgl Lapor");
            gridSampah.Columns.Add("colJadwal", "Jadwal Angkut");
            gridSampah.Columns.Add("colKet", "Keterangan");

            gridSampah.Columns["colWilayah"].Width = 150;
            gridSampah.Columns["colKet"].Width = 200;
        }

        // --- 4. LOAD DATA (SESUAI ROLE) ---
        private void LoadData()
        {
            if (collection == null || gridSampah == null) return;

            try
            {
                List<SampahModel> dataList;

                // LOGIKA FILTER DATA
                if (activeRole == "Admin" || activeRole == "Petugas")
                {
                    // Admin & Petugas melihat SEMUA DATA
                    dataList = collection.Find(_ => true).ToList();
                }
                else
                {
                    // Masyarakat hanya melihat DATA MILIK SENDIRI
                    var filter = Builders<SampahModel>.Filter.Eq(x => x.UserId, activeUserId);
                    dataList = collection.Find(filter).ToList();
                }

                gridSampah.Rows.Clear();
                foreach (var item in dataList)
                {
                    string tglLapor = item.Tanggal.ToString("dd MMM yyyy");
                    string tglJadwal = (item.JadwalAngkut == DateTime.MinValue) ? "-" : item.JadwalAngkut.ToString("dd MMM yyyy");

                    gridSampah.Rows.Add(
                        item.Id, item.Wilayah, item.Jenis, item.Berat, item.Status,
                        tglLapor, tglJadwal, item.Keterangan
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        // ==========================================================
        // EVENTS BUTTON (CRUD)
        // ==========================================================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Kirim UserId dan Role ke FormInput (Constructor 2 Parameter)
            FormInput frm = new FormInput(activeUserId, activeRole);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                string idTerpilih = gridSampah.SelectedRows[0].Cells[0].Value.ToString();
                // Kirim UserId, Role, dan ID ke FormInput (Constructor 3 Parameter)
                FormInput frm = new FormInput(activeUserId, activeRole, idTerpilih);
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
                        collection.DeleteOne(Builders<SampahModel>.Filter.Eq(x => x.Id, idTerpilih));
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

        // ==========================================================
        // TOMBOL BUKA MAP
        // ==========================================================

        // METHOD INI UNTUK MENGATASI ERROR DESIGNER (CS0103)
        private void btnBukaMap_Click_1(object sender, EventArgs e)
        {
            btnBukaMap_Click(sender, e);
        }

        private void btnBukaMap_Click(object sender, EventArgs e)
        {
            FormMap mapForm = new FormMap(activeUserId);
            mapForm.Show();
        }

        // ==========================================================
        // FITUR PDF
        // ==========================================================
        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            if (gridSampah.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = $"Laporan Sampah - {activeUserName}.pdf";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    PdfFont fontArial = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

                    using (PdfWriter writer = new PdfWriter(sfd.FileName))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document doc = new Document(pdf))
                    {
                        doc.SetFont(fontArial);
                        pdf.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4.Rotate());

                        doc.Add(new Paragraph("LAPORAN DATA SAMPAH JAWA BARAT").SetTextAlignment(TextAlignment.CENTER).SetFontSize(18));
                        doc.Add(new Paragraph($"User: {activeUserName} ({activeRole}) | {DateTime.Now}").SetTextAlignment(TextAlignment.CENTER).SetFontSize(10).SetMarginBottom(20));

                        float[] colWidths = { 2, 2, 1, 2, 2, 2, 3 };
                        Table table = new Table(UnitValue.CreatePercentArray(colWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                        string[] headers = { "Wilayah", "Jenis", "Berat", "Status", "Tgl Lapor", "Jadwal", "Keterangan" };
                        foreach (string h in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(h)).SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER).SetFontSize(9));

                        foreach (DataGridViewRow row in gridSampah.Rows)
                        {
                            if (row.IsNewRow) continue;
                            string GetVal(int idx) => row.Cells[idx].Value?.ToString() ?? "-";

                            table.AddCell(new Paragraph(GetVal(1)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(2)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(3)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(4)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(5)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(6)).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal(7)).SetFontSize(9));
                        }
                        doc.Add(table);
                    }
                    MessageBox.Show("PDF Berhasil Disimpan!");
                }
                catch (Exception ex) { MessageBox.Show("Error Export: " + ex.Message); }
            }
        }

        // WINDOW CONTROL & UTILS
        private void btnClose_Click(object sender, EventArgs e) => Application.Exit();
        private void btnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;
        private void btnMaximize_Click(object sender, EventArgs e) =>
            this.WindowState = (this.WindowState == FormWindowState.Normal) ? FormWindowState.Maximized : FormWindowState.Normal;

        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void btnDataSampah_Click(object sender, EventArgs e) => LoadData();

        // --- CHATBOT ---
        private void btnChatbot_Click(object sender, EventArgs e)
        {
            if (chatbotPopup == null || chatbotPopup.IsDisposed)
            {
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

        private void btnChart_Click(object sender, EventArgs e)
        {
            // Membuka form chart sebagai dialog box
            FormChart frm = new FormChart();
            frm.ShowDialog();
        }

        private async void btnImportCSV_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CSV Files (*.csv)|*.csv";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    btnImportCSV.Enabled = false;
                    btnImportCSV.Text = "Processing...";

                    var ingestionHelper = new SiJabarApp.helper.CsvIngestionHelper();
                    await ingestionHelper.ProcessOpenDataCsv(ofd.FileName);

                    MessageBox.Show("Data CSV berhasil diproses dan dimasukkan ke database RAG!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal mengimpor data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                    btnImportCSV.Enabled = true;
                    btnImportCSV.Text = "Import RAG Data";
                }
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            FormDashboard frm = new FormDashboard();
            frm.ShowDialog();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Yakin ingin keluar?", "Log Out", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.Hide();
                FormAuth login = new FormAuth();
                login.ShowDialog();
                this.Close();
            }
        }
    }
}