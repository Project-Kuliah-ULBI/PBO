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
            
            // Ensure Floating Button is on Top
            if(btnFloatingChat != null) btnFloatingChat.BringToFront();
            if(btnFloatingChat != null) btnFloatingChat.BringToFront();
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
            
            // 4. Init Views
            SetupDashboardControl(); 

            // 5. Default View: Dashboard
            ShowDashboard();

            // 6. Set Label User
            if (lblUserLogin != null)
            {
                lblUserLogin.Text = $"Halo, {activeUserName} ({activeRole})";
            }
            
            if(btnFloatingChat != null) btnFloatingChat.BringToFront();
        }

        // --- CONTROL DASHBOARD & VIEWS ---
        private DashboardControl dashboardControl;
        private MapControl mapControl;
        private ChartControl chartControl;
        private FileIOControl fileIOControl; // NEW

        // ... existing code ...

        private void SetupDashboardControl()
        {
            // 1. Init Dashboard
            dashboardControl = new DashboardControl();
            dashboardControl.UserRole = this.activeRole; // Pass Role
            dashboardControl.Dock = DockStyle.Fill;
            
            // 2. Init Map
            mapControl = new MapControl();
            mapControl.UserRole = this.activeRole; // Pass Role to MapControl
            mapControl.ActiveUserId = this.activeUserId; // Pass ActiveUserId to MapControl
            mapControl.Dock = DockStyle.Fill;
            mapControl.Visible = false;

            // 3. Init Chart
            chartControl = new ChartControl();
            chartControl.Dock = DockStyle.Fill;
            chartControl.Visible = false;
            
            // 4. Init FileIO (Import/Export)
            fileIOControl = new FileIOControl();
            fileIOControl.Dock = DockStyle.Fill;
            fileIOControl.Visible = false;
            fileIOControl.OnExportPdfRequested += (s, e) => PerformPdfExport(); // Handle Export Event
            fileIOControl.OnDataImported += (s, e) => MessageBox.Show("Data baru telah ditambahkan.");
            
            // Masukkan ke Panel Content
            if(panelContent != null)
            {
                panelContent.Controls.Add(dashboardControl);
                panelContent.Controls.Add(mapControl);
                panelContent.Controls.Add(chartControl);
                panelContent.Controls.Add(fileIOControl);
            }
        }

        // --- NAVIGATION METHODS (SWITCH VIEW) ---
        private void SwitchView(Control viewToShow)
        {
            // Sembunyikan semua view dulu
            if (dashboardControl != null) dashboardControl.Visible = false;
            if (mapControl != null) mapControl.Visible = false;
            if (chartControl != null) chartControl.Visible = false;
            if (fileIOControl != null) fileIOControl.Visible = false;
            if (panel3 != null) panel3.Visible = false; // Panel Data Sampah
            if (panel2 != null) panel2.Visible = false; // Panel Header Data Sampah

            // Tampilkan View yang diminta
            if (viewToShow != null)
            {
                viewToShow.Visible = true;
                viewToShow.BringToFront();
            }
        }

        private void ShowDashboard()
        {
            SwitchView(dashboardControl);
            dashboardControl.ReloadData();
            if(lblTitle != null) lblTitle.Text = "DASHBOARD";
        }
        
        private void ShowFileIO()
        {
            SwitchView(fileIOControl);
            if(lblTitle != null) lblTitle.Text = "IMPORT & EXPORT";
        }
        
        private void btnImportPDF_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowImportPage();
            if(lblTitle != null) lblTitle.Text = "IMPORT PDF (RAG)";
        }

        private void ShowDataSampah()
        {
            // Data Sampah masih pakai Panel3 & Panel2 (Legacy)
            if (dashboardControl != null) dashboardControl.Visible = false;
            if (mapControl != null) mapControl.Visible = false;
            if (chartControl != null) chartControl.Visible = false;
            if (fileIOControl != null) fileIOControl.Visible = false;

            if (panel3 != null) panel3.Visible = true;
            if (panel2 != null) panel2.Visible = true;
            
            if(lblTitle != null) lblTitle.Text = "DATA SAMPAH";
            LoadData(); 
        }

        private async void ShowMap()
        {
            SwitchView(mapControl);
            if(lblTitle != null) lblTitle.Text = "PETA SEBARAN";
            // Refresh markers every time map is shown (picks up newly added TPS/TPA)
            await mapControl.LoadAllMarkers();
        }

        private void ShowChart()
        {
            SwitchView(chartControl);
            if(chartControl != null) chartControl.LoadDataToChart(); // Refresh Chart Data
            if(lblTitle != null) lblTitle.Text = "STATISTIK SAMPAH";
        }

        private void btnDataSampah_Click(object sender, EventArgs e) 
        {
            ActivateButton(sender);
            ShowDataSampah();
        }

        private void btnDashboard_Click(object sender, EventArgs e) 
        {
             ActivateButton(sender);
             ShowDashboard();
        }

        // ... existing code ...


        // --- 1. KONEKSI DATABASE ---
        private void ConnectToMongoDB()
        {
            try
            {
                // var client = new MongoClient("mongodb://localhost:27017");
                var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
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
                
                // NEW: Hide Data Sampah Sidebar for Masyarakat
                if(btnDataSampah != null) btnDataSampah.Visible = false;
            }
            if (activeRole != "Admin")
            {
                btnImportCSV.Visible = false;
            }
            
            // NEW: Hide Export PDF for Masyarakat
            if (activeRole == "Masyarakat" && btnExportPDF != null)
            {
                btnExportPDF.Visible = false;
            }

            // NEW: Petugas (Admin - Delete)
            if (activeRole == "Petugas")
            {
                 // 1. Hide Delete Button
                 btnDelete.Visible = false;
                 
                 // 2. Shift Add & Edit to Right (Fill Gap)
                 // Assumes layout: [Add] [Edit] [Delete]
                 if (btnEdit != null && btnAdd != null)
                 {
                     Point locDelete = btnDelete.Location;
                     Point locEdit = btnEdit.Location;
                     
                     btnEdit.Location = locDelete;
                     btnAdd.Location = locEdit;
                 }
            }

        }

        // --- 3. STYLING TABEL & UI MODERN ---
        private void SetupStyling()
        {
            // 1. Setup DataGridView
            if (gridSampah != null)
            {
                StyleHelper.StyleGridView(gridSampah);

                // DEFINISI KOLOM
                gridSampah.Columns.Clear();
                gridSampah.Columns.Add("colId", "ID");
                gridSampah.Columns["colId"].Visible = false;

                gridSampah.Columns.Add("colWilayah", "Wilayah");
                gridSampah.Columns.Add("colJenis", "Jenis");
                gridSampah.Columns.Add("colBerat", "Berat (Kg)");
                gridSampah.Columns.Add("colStatus", "Status");
                gridSampah.Columns.Add("colTanggal", "Tanggal");
                gridSampah.Columns.Add("colJadwal", "Jadwal Kirim");
                gridSampah.Columns.Add("colKet", "Keterangan");

                gridSampah.Columns["colWilayah"].Width = 150;
                gridSampah.Columns["colKet"].Width = 200;
                gridSampah.Columns["colKet"].Width = 200;
                gridSampah.Columns["colKet"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                // --- ADVANCED STYLING ---
                gridSampah.EnableHeadersVisualStyles = false; // Wajib agar warna header berubah
                gridSampah.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(16, 185, 129); // Emerald Green
                gridSampah.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                gridSampah.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                gridSampah.ColumnHeadersHeight = 40;
                
                gridSampah.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                gridSampah.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
                gridSampah.DefaultCellStyle.SelectionForeColor = Color.Black;
                gridSampah.RowTemplate.Height = 35; // Lebih lega
                gridSampah.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                
                gridSampah.BorderStyle = BorderStyle.None;
                gridSampah.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                gridSampah.GridColor = Color.FromArgb(230, 230, 230);
            }

            // 2. Setup Form & Buttons
            this.BackColor = StyleHelper.BackgroundColor;
            ApplyModernStyles();
            if(btnDashboard != null) ActivateButton(btnDashboard); // Default Active
        }

        // --- NEW: SIDEBAR ACTIVE STATE HELPER ---
        private void ActivateButton(object btnSender)
        {
            if (btnSender == null) return;

            // 1. Reset Semua Tombol ke Default (Dark)
            DisableButton(btnDashboard);
            DisableButton(btnDataSampah);
            DisableButton(btnExportPDF);
            DisableButton(btnBukaMap);
            DisableButton(btnChart);
            DisableButton(btnImportCSV);

            // 2. Highlight Tombol Aktif
            if (btnSender is FontAwesome.Sharp.IconButton currentBtn)
            {
                currentBtn.BackColor = Color.FromArgb(45, 50, 56); // Lighter Dark
                currentBtn.ForeColor = Color.White;
                currentBtn.IconColor = Color.White;
                // Optional: Add left border visual if desired
            }
        }

        private void DisableButton(FontAwesome.Sharp.IconButton btn)
        {
            if (btn == null) return;
            btn.BackColor = Color.FromArgb(33, 37, 41); // Default Sidebar Color
            btn.ForeColor = Color.White;
            btn.IconColor = Color.White;
        }

        private void ApplyModernStyles()
        {
            // Header Panel
            Control pnlHeader = this.Controls.Find("panelHeader", true).Length > 0 ? this.Controls.Find("panelHeader", true)[0] : null;
            if (pnlHeader != null) pnlHeader.BackColor = Color.White;

            // Style Tombol CRUD
            if(btnAdd != null) StyleHelper.StyleButton(btnAdd, StyleHelper.PrimaryColor, Color.White);
            if(btnEdit != null) 
            {
                StyleHelper.StyleButton(btnEdit, StyleHelper.WarningColor, Color.White); // Force Yellow
                btnEdit.BackColor = StyleHelper.WarningColor; // Double assurance
            }
            if(btnDelete != null) StyleHelper.StyleButton(btnDelete, StyleHelper.DangerColor, Color.White);
            
            // Logout Button (Red) - Already set in Designer but enforce here
            if(btnLogout != null) 
            {
                btnLogout.BackColor = Color.FromArgb(220, 53, 69);
                btnLogout.ForeColor = Color.White;
            }
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
            ActivateButton(sender);
            ShowMap();
        }

        // ==========================================================
        // FITUR PDF
        // ==========================================================


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



        // --- CHATBOT ---
        // --- CHATBOT (FLOATING) ---
        private void btnFloatingChat_Click(object sender, EventArgs e)
        {
            if (chatbotPopup == null || chatbotPopup.IsDisposed)
            {
                chatbotPopup = new Chatbot(activeUserId);
                
                // Position above the floating button (Bottom Right)
                int x = this.Location.X + this.Width - chatbotPopup.Width - 80;
                int y = this.Location.Y + this.Height - chatbotPopup.Height - 80;

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
            ActivateButton(sender);
            ShowChart();
        }

        private void ShowImportPage()
        {
            SwitchView(fileIOControl);
            fileIOControl.ShowImportMode();
            if(lblTitle != null) lblTitle.Text = "IMPORT DATA";
        }

        private void ShowExportPage()
        {
            SwitchView(fileIOControl);
            fileIOControl.ShowExportMode();
            if(lblTitle != null) lblTitle.Text = "EXPORT PDF";
        }

        private async void btnImportCSV_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowImportPage();
        }

        private async void btnExportPDF_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowExportPage();
        }

        private void PerformPdfExport()
        {
            if (gridSampah == null || gridSampah.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data untuk diekspor!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PDF Files|*.pdf";
            sfd.FileName = $"Laporan Sampah - {activeUserName} - {DateTime.Now:yyyyMMdd}.pdf";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    PdfFont fontText = File.Exists(fontPath) 
                        ? PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H)
                        : PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                    using (PdfWriter writer = new PdfWriter(sfd.FileName))
                    using (PdfDocument pdf = new PdfDocument(writer))
                    using (Document doc = new Document(pdf))
                    {
                        doc.SetFont(fontText);
                        pdf.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4.Rotate());

                        doc.Add(new Paragraph("LAPORAN DATA SAMPAH JAWA BARAT").SetTextAlignment(TextAlignment.CENTER).SetFontSize(18));
                        doc.Add(new Paragraph($"User: {activeUserName} ({activeRole}) | {DateTime.Now:dd MMMM yyyy HH:mm}").SetTextAlignment(TextAlignment.CENTER).SetFontSize(10).SetMarginBottom(20));

                        float[] colWidths = { 2, 2, 1, 2, 2, 2, 3 };
                        Table table = new Table(UnitValue.CreatePercentArray(colWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                        string[] headers = { "Wilayah", "Jenis", "Berat", "Status", "Tgl Lapor", "Jadwal", "Keterangan" };
                        foreach (string h in headers)
                        {
                            Cell cell = new Cell().Add(new Paragraph(h));
                            cell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
                            cell.SetTextAlignment(TextAlignment.CENTER);
                            cell.SetFontSize(9);
                            table.AddHeaderCell(cell);
                        }

                        foreach (DataGridViewRow row in gridSampah.Rows)
                        {
                            if (row.IsNewRow) continue;
                            string GetVal(string colName) => row.Cells[colName].Value?.ToString() ?? "-";

                            table.AddCell(new Paragraph(GetVal("colWilayah")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colJenis")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colBerat")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colStatus")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colTanggal")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colJadwal")).SetFontSize(9));
                            table.AddCell(new Paragraph(GetVal("colKet")).SetFontSize(9));
                        }

                        doc.Add(table);
                    }

                    MessageBox.Show("PDF Berhasil Diekspor!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    try {
                         var p = new System.Diagnostics.Process();
                         p.StartInfo = new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true };
                         p.Start();
                    } catch { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Export PDF: " + ex.Message);
                }
            }
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