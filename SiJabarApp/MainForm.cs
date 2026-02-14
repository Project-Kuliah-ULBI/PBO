using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using MongoDB.Driver;
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
        private IMongoCollection<SampahModel> collection;
        private IMongoCollection<MasterLokasiModel> collectionMaster;

        private string activeUserId;
        private string activeUserName;
        public string activeRole;

        private Chatbot chatbotPopup;

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        public MainForm()
        {
            InitializeComponent();
            this.activeRole = "Masyarakat";
            ConnectToMongoDB();
            SetupStyling();
            
            if(btnFloatingChat != null) btnFloatingChat.BringToFront();
        }

        public MainForm(string userId, string userName, string userRole)
        {
            InitializeComponent();

            this.activeUserId = userId;
            this.activeUserName = userName;
            this.activeRole = userRole;

            ConnectToMongoDB();
            SetupStyling();
            
            ApplyRolePermissions();
            SetupDashboardControl(); 
            ShowDashboard();

            if (lblUserLogin != null)
            {
                lblUserLogin.Text = $"Selamat Datang, {activeUserName} ({activeRole})";
            }
            
            if(btnFloatingChat != null) btnFloatingChat.BringToFront();
        }

        private DashboardControl dashboardControl;
        private MapControl mapControl;
        private ChartControl chartControl;
        private FileIOControl fileIOControl;

        private void SetupDashboardControl()
        {
            dashboardControl = new DashboardControl();
            dashboardControl.UserRole = this.activeRole;
            dashboardControl.Dock = DockStyle.Fill;
            
            mapControl = new MapControl();
            mapControl.UserRole = this.activeRole;
            mapControl.ActiveUserId = this.activeUserId;
            mapControl.Dock = DockStyle.Fill;
            mapControl.Visible = false;

            chartControl = new ChartControl();
            chartControl.Dock = DockStyle.Fill;
            chartControl.Visible = false;
            
            fileIOControl = new FileIOControl();
            fileIOControl.Dock = DockStyle.Fill;
            fileIOControl.Visible = false;
            fileIOControl.OnExportPdfRequested += (s, e) => PerformPdfExport();
            fileIOControl.OnDataImported += (s, e) => MessageBox.Show("Data berhasil diimpor.");
            
            if(panelContent != null)
            {
                panelContent.Controls.Add(dashboardControl);
                panelContent.Controls.Add(mapControl);
                panelContent.Controls.Add(chartControl);
                panelContent.Controls.Add(fileIOControl);
            }
        }

        private void SwitchView(Control viewToShow)
        {
            if (dashboardControl != null) dashboardControl.Visible = false;
            if (mapControl != null) mapControl.Visible = false;
            if (chartControl != null) chartControl.Visible = false;
            if (fileIOControl != null) fileIOControl.Visible = false;
            if (panel3 != null) panel3.Visible = false;
            if (panel2 != null) panel2.Visible = false;

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
            if(lblTitle != null) lblTitle.Text = "IMPOR & EKSPOR";
        }
        
        private void btnImportPDF_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowImportPage();
            if(lblTitle != null) lblTitle.Text = "IMPORT PDF (RAG)";
        }

        private void ShowDataSampah()
        {
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
            await mapControl.LoadAllMarkers();
        }

        private void ShowChart()
        {
            SwitchView(chartControl);
            if(chartControl != null) chartControl.LoadDataToChart();
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

        private void ConnectToMongoDB()
        {
            try
            {
                var client = new MongoClient(MongoHelper.ConnectionString);
                var database = client.GetDatabase(MongoHelper.DatabaseName);
                collection = database.GetCollection<SampahModel>("Sampah");
                collectionMaster = database.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi database gagal: " + ex.Message);
            }
        }

        private void ApplyRolePermissions()
        {
            if (btnAdd == null) return;

            btnAdd.Visible = true;
            btnEdit.Visible = true;
            btnDelete.Visible = true;
            btnSyncRAG.Visible = (activeRole == "Admin");

            if (activeRole == "Masyarakat")
            {
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                if(btnDataSampah != null) btnDataSampah.Visible = false;
            }
            
            if (activeRole != "Admin")
            {
                btnImportCSV.Visible = false;
            }
            
            if (activeRole == "Masyarakat" && btnExportPDF != null)
            {
                btnExportPDF.Visible = false;
            }

            if (activeRole == "Petugas")
            {
                 btnDelete.Visible = false;
                 if (btnEdit != null && btnAdd != null)
                 {
                     Point locDelete = btnDelete.Location;
                     Point locEdit = btnEdit.Location;
                     btnEdit.Location = locDelete;
                     btnAdd.Location = locEdit;
                 }
            }
        }

        private void SetupStyling()
        {
            if (gridSampah != null)
            {
                StyleHelper.StyleGridView(gridSampah);

                gridSampah.Columns.Clear();
                gridSampah.Columns.Add("colId", "ID");
                gridSampah.Columns["colId"].Visible = false;

                gridSampah.Columns.Add("colWilayah", "Wilayah");
                gridSampah.Columns.Add("colJenis", "Jenis");
                gridSampah.Columns.Add("colBerat", "Berat (Kg)");
                gridSampah.Columns.Add("colStatus", "Status");
                gridSampah.Columns.Add("colTanggal", "Tanggal");
                gridSampah.Columns.Add("colJadwal", "Jadwal");
                gridSampah.Columns.Add("colKet", "Keterangan");

                gridSampah.Columns["colWilayah"].Width = 150;
                gridSampah.Columns["colKet"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                gridSampah.EnableHeadersVisualStyles = false;
                gridSampah.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(16, 185, 129);
                gridSampah.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                gridSampah.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                gridSampah.ColumnHeadersHeight = 40;
                
                gridSampah.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                gridSampah.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
                gridSampah.DefaultCellStyle.SelectionForeColor = Color.Black;
                gridSampah.RowTemplate.Height = 35;
                gridSampah.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
                
                gridSampah.BorderStyle = BorderStyle.None;
                gridSampah.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                gridSampah.GridColor = Color.FromArgb(230, 230, 230);
            }

            this.BackColor = StyleHelper.BackgroundColor;
            ApplyModernStyles();
            if(btnDashboard != null) ActivateButton(btnDashboard);
        }

        private void ActivateButton(object btnSender)
        {
            if (btnSender == null) return;

            DisableButton(btnDashboard);
            DisableButton(btnDataSampah);
            DisableButton(btnExportPDF);
            DisableButton(btnBukaMap);
            DisableButton(btnChart);
            DisableButton(btnImportCSV);

            if (btnSender is FontAwesome.Sharp.IconButton currentBtn)
            {
                currentBtn.BackColor = Color.FromArgb(45, 50, 56);
                currentBtn.ForeColor = Color.White;
                currentBtn.IconColor = Color.White;
            }
        }

        private void DisableButton(FontAwesome.Sharp.IconButton btn)
        {
            if (btn == null) return;
            btn.BackColor = Color.FromArgb(33, 37, 41);
            btn.ForeColor = Color.White;
            btn.IconColor = Color.White;
        }

        private void ApplyModernStyles()
        {
            Control pnlHeader = this.Controls.Find("panelHeader", true).Length > 0 ? this.Controls.Find("panelHeader", true)[0] : null;
            if (pnlHeader != null) pnlHeader.BackColor = Color.White;

            if(btnAdd != null) StyleHelper.StyleButton(btnAdd, StyleHelper.PrimaryColor, Color.White);
            if(btnEdit != null) StyleHelper.StyleButton(btnEdit, StyleHelper.WarningColor, Color.White);
            if(btnDelete != null) StyleHelper.StyleButton(btnDelete, StyleHelper.DangerColor, Color.White);
            
            if(btnLogout != null) 
            {
                btnLogout.BackColor = Color.FromArgb(220, 53, 69);
                btnLogout.ForeColor = Color.White;
            }
        }

        private void LoadData()
        {
            if (collection == null || gridSampah == null) return;

            try
            {
                List<SampahModel> dataList;

                if (activeRole == "Admin" || activeRole == "Petugas")
                {
                    dataList = collection.Find(_ => true).ToList();
                }
                else
                {
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            FormInput frm = new FormInput(activeUserId, activeRole);
            if (frm.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                string idTerpilih = gridSampah.SelectedRows[0].Cells[0].Value.ToString();
                FormInput frm = new FormInput(activeUserId, activeRole, idTerpilih);
                if (frm.ShowDialog() == DialogResult.OK) LoadData();
            }
            else
            {
                MessageBox.Show("Silakan pilih data yang ingin diubah.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (gridSampah.SelectedRows.Count > 0)
            {
                var konfirmasi = MessageBox.Show("Apakah Anda yakin ingin menghapus data ini?", "Konfirmasi Hapus", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

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
                MessageBox.Show("Silakan pilih data yang ingin dihapus.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnSyncRAG_Click(object sender, EventArgs e)
        {
            if (activeRole != "Admin") return;

            var confirm = MessageBox.Show("Anda ingin menyinkronkan semua data ke AI Knowledge Base?\nProses ini mungkin memakan waktu.", 
                                        "Konfirmasi Sinkronisasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (confirm != DialogResult.Yes) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                btnSyncRAG.Enabled = false;
                btnSyncRAG.Text = "Menyinkronkan...";

                var supaHelper = new SupabaseHelper();
                int countSampah = 0;
                int countMaster = 0;

                var listSampah = collection.Find(_ => true).ToList();
                foreach (var item in listSampah)
                {
                    string tgl = item.Tanggal.ToString("dd MMMM yyyy");
                    string content = $"Waste Report: Region {item.Wilayah}, Type {item.Jenis}, Weight {item.Berat}kg. Status: {item.Status}. Date: {tgl}. Info: {item.Keterangan ?? "-"}";
                    
                    float[] vector = await MistralHelper.GetEmbedding(content);
                    if (vector != null)
                    {
                        await supaHelper.InsertDocumentAsync(content, "system_admin", vector);
                        countSampah++;
                    }
                }

                var listMaster = collectionMaster.Find(_ => true).ToList();
                foreach (var loc in listMaster)
                {
                    string content = $"TPS/TPA Location: {loc.NamaTPS}, Coordinates: {loc.Latitude}, {loc.Longitude}. Info: {loc.Keterangan ?? "-"}";
                    
                    float[] vector = await MistralHelper.GetEmbedding(content);
                    if (vector != null)
                    {
                        await supaHelper.InsertDocumentAsync(content, "system_admin", vector);
                        countMaster++;
                    }
                }

                MessageBox.Show($"Sinkronisasi Selesai!\n- {countSampah} Laporan Sampah\n- {countMaster} Lokasi TPS", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Sinkronisasi: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnSyncRAG.Enabled = true;
                btnSyncRAG.Text = "Sinkron AI";
            }
        }


        private void btnBukaMap_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowMap();
        }

        private void btnClose_Click(object sender, EventArgs e) => Application.Exit();
        private void btnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;
        private void btnMaximize_Click(object sender, EventArgs e) =>
            this.WindowState = (this.WindowState == FormWindowState.Normal) ? FormWindowState.Maximized : FormWindowState.Normal;

        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void btnFloatingChat_Click(object sender, EventArgs e)
        {
            if (chatbotPopup == null || chatbotPopup.IsDisposed)
            {
                chatbotPopup = new Chatbot(activeUserId);
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
            if(lblTitle != null) lblTitle.Text = "IMPOR DATA";
        }

        private void ShowExportPage()
        {
            SwitchView(fileIOControl);
            fileIOControl.ShowExportMode();
            if(lblTitle != null) lblTitle.Text = "EKSPOR PDF";
        }

        private void btnImportCSV_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowImportPage();
        }

        private void btnExportPDF_Click(object sender, EventArgs e)
        {
            ActivateButton(sender);
            ShowExportPage();
        }

        private void PerformPdfExport()
        {
            if (gridSampah == null || gridSampah.Rows.Count == 0)
            {
                MessageBox.Show("Tidak ada data untuk diekspor.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                        doc.Add(new Paragraph("LAPORAN PENGELOLAAN SAMPAH JAWA BARAT").SetTextAlignment(TextAlignment.CENTER).SetFontSize(18));
                        doc.Add(new Paragraph($"Username: {activeUserName} ({activeRole}) | {DateTime.Now:dd MMMM yyyy HH:mm}").SetTextAlignment(TextAlignment.CENTER).SetFontSize(10).SetMarginBottom(20));

                        float[] colWidths = { 2, 2, 1, 2, 2, 2, 3 };
                        Table table = new Table(UnitValue.CreatePercentArray(colWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                        string[] headers = { "Wilayah", "Jenis", "Berat", "Status", "Tanggal Lapor", "Jadwal", "Keterangan" };
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

                    MessageBox.Show("PDF berhasil diekspor.", "Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    try {
                         var p = new System.Diagnostics.Process();
                         p.StartInfo = new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true };
                         p.Start();
                    } catch { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Ekspor: " + ex.Message);
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Apakah Anda yakin ingin keluar?", "Logout", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                this.Hide();
                FormAuth login = new FormAuth();
                login.ShowDialog();
                this.Close();
            }
        }
    }
}
