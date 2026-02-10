using System;
using System.IO;
using System.Drawing; // Wajib untuk UI/Grafis
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization; // Untuk format angka titik/koma
using System.Threading.Tasks; // Untuk Task.Delay
using System.Runtime.InteropServices; // Wajib untuk Drag Window

// --- LIBRARY LUAR ---
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MongoDB.Driver;
using SiJabarApp.helper;

namespace SiJabarApp
{
    public partial class FormMap : Form
    {
        // --- KOMPONEN UI ---
        private WebView2 webViewMap;
        private ComboBox comboFilterStatus;
        private Button btnRefresh;

        // --- DATABASE ---
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;

        // --- VARIABEL ---
        private string activeUserId;
        private bool isMapReady = false;

        // --- LOGIKA DRAG WINDOW (DLL IMPORT) ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // Event saat panel header diklik untuk geser window
        private void PanelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        public FormMap(string userId)
        {
            InitializeComponent();
            this.activeUserId = userId;

            // 1. SETUP WINDOW (BORDERLESS & MODERN)
            this.Text = "Peta Sebaran Sampah & Lokasi TPS";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            this.FormBorderStyle = FormBorderStyle.None; // Hilangkan Border Bawaan
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea; // Agar taskbar tidak tertutup saat max

            ConnectDB();
            InitUI(); // Membangun Tampilan Modern
        }

        private void ConnectDB()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("SiJabarDB");
                collectionSampah = database.GetCollection<SampahModel>("Sampah");
                collectionMaster = database.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex) { MessageBox.Show("Error Koneksi DB: " + ex.Message); }
        }

        // =================================================================
        // MEMBANGUN UI (MODERN DASHBOARD STYLE)
        // =================================================================
        private void InitUI()
        {
            // 1. PANEL ATAS (HEADER) - BISA DIGESER
            Panel panelTop = new Panel();
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 80; // Tinggi lega
            panelTop.BackColor = Color.White;
            panelTop.Padding = new Padding(30, 0, 30, 0);

            // Aktifkan Drag Window
            panelTop.MouseDown += PanelHeader_MouseDown;

            this.Controls.Add(panelTop);

            // Garis Separator Tipis
            Panel separator = new Panel();
            separator.Dock = DockStyle.Bottom;
            separator.Height = 1;
            separator.BackColor = Color.FromArgb(230, 230, 230); // Abu muda
            panelTop.Controls.Add(separator);

            // =============================================================
            // 2. WINDOW CONTROLS (CLOSE, MAX, MIN)
            // =============================================================

            // Helper function bikin tombol header
            Button CreateHeaderBtn(string text, int xRightOffset, Color hoverColor, Color hoverText)
            {
                Button btn = new Button();
                btn.Text = text;
                btn.Font = new Font("Segoe UI Symbol", 10, FontStyle.Regular);
                btn.Size = new Size(45, 30);
                btn.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Nempel Kanan
                btn.Location = new Point(this.Width - xRightOffset, 0); // Tempel di atas
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.DimGray;
                btn.Cursor = Cursors.Hand;

                btn.MouseEnter += (s, e) => { btn.BackColor = hoverColor; btn.ForeColor = hoverText; };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.DimGray; };
                return btn;
            }

            // Tombol Close (X) - Offset 45px
            Button btnClose = CreateHeaderBtn("✕", 45, Color.Red, Color.White);
            btnClose.Click += (s, e) => this.Close();
            panelTop.Controls.Add(btnClose);

            // Tombol Maximize (Kotak) - Offset 90px
            Button btnMax = CreateHeaderBtn("⬜", 90, Color.WhiteSmoke, Color.Black);
            btnMax.Click += (s, e) => {
                this.WindowState = (this.WindowState == FormWindowState.Normal) ? FormWindowState.Maximized : FormWindowState.Normal;
            };
            panelTop.Controls.Add(btnMax);

            // Tombol Minimize (Garis) - Offset 135px
            Button btnMin = CreateHeaderBtn("—", 135, Color.WhiteSmoke, Color.Black);
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            panelTop.Controls.Add(btnMin);


            // =============================================================
            // 3. FILTER & REFRESH CONTROLS
            // =============================================================

            // LABEL FILTER
            Label lblFilter = new Label();
            lblFilter.Text = "Pilih Status:";
            lblFilter.AutoSize = true;
            lblFilter.Location = new Point(30, 32);
            lblFilter.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblFilter.ForeColor = Color.FromArgb(80, 80, 80);
            lblFilter.MouseDown += PanelHeader_MouseDown; // Label juga bisa buat geser
            panelTop.Controls.Add(lblFilter);

            // COMBOBOX (CUSTOM DRAW - ANTI BIRU)
            comboFilterStatus = new ComboBox();
            comboFilterStatus.Items.AddRange(new object[] { "Semua Status", "Masuk", "Dipilah", "Daur Ulang", "Selesai" });
            comboFilterStatus.SelectedIndex = 0;

            // Setting Tampilan
            comboFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboFilterStatus.DrawMode = DrawMode.OwnerDrawFixed; // Custom Draw
            comboFilterStatus.Font = new Font("Segoe UI", 11);
            comboFilterStatus.Width = 200;
            comboFilterStatus.Location = new Point(140, 28);
            comboFilterStatus.BackColor = Color.White;

            // Event Menggambar Item
            comboFilterStatus.DrawItem += (sender, e) =>
            {
                if (e.Index < 0) return;
                ComboBox combo = sender as ComboBox;

                // Jika dipilih, warnai abu muda (bukan biru)
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), e.Bounds);
                else
                    e.Graphics.FillRectangle(new SolidBrush(combo.BackColor), e.Bounds);

                // Gambar Teks
                string text = combo.Items[e.Index].ToString();
                float y = e.Bounds.Y + (e.Bounds.Height - e.Graphics.MeasureString(text, combo.Font).Height) / 2;
                e.Graphics.DrawString(text, combo.Font, new SolidBrush(Color.FromArgb(64, 64, 64)), e.Bounds.X + 5, y);
            };

            comboFilterStatus.SelectedIndexChanged += async (s, e) => {
                this.ActiveControl = null; // Hilangkan fokus
                await LoadAllMarkers();
            };
            panelTop.Controls.Add(comboFilterStatus);

            // TOMBOL REFRESH (MODERN FLAT BLUE)
            btnRefresh = new Button();
            btnRefresh.Text = "Muat Ulang";
            btnRefresh.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            btnRefresh.Size = new Size(130, 36);
            btnRefresh.Location = new Point(360, 27);

            btnRefresh.BackColor = Color.FromArgb(33, 150, 243); // Biru Material
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Cursor = Cursors.Hand;

            btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = Color.FromArgb(25, 118, 210); // Hover Darker
            btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = Color.FromArgb(33, 150, 243); // Normal

            btnRefresh.Click += async (s, e) => {
                btnRefresh.Text = "Loading...";
                btnRefresh.Enabled = false;
                await LoadAllMarkers();
                btnRefresh.Text = "Muat Ulang";
                btnRefresh.Enabled = true;
            };
            panelTop.Controls.Add(btnRefresh);

            // 4. WEBVIEW PETA
            webViewMap = new WebView2();
            webViewMap.Dock = DockStyle.Fill;
            this.Controls.Add(webViewMap);

            webViewMap.BringToFront();
            panelTop.SendToBack();

            InitMapWebView();
        }

        // =================================================================
        // LOGIKA PETA (WEBVIEW)
        // =================================================================
        private async void InitMapWebView()
        {
            await webViewMap.EnsureCoreWebView2Async();

            string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "map.html");
            if (File.Exists(htmlPath))
            {
                webViewMap.Source = new Uri(htmlPath);
                webViewMap.NavigationCompleted += WebViewMap_NavigationCompleted;
            }
            else
            {
                MessageBox.Show("File map.html tidak ditemukan!");
            }
        }

        private async void WebViewMap_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                isMapReady = true;

                // 1. Init Peta Bandung
                await webViewMap.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");

                // 2. Inject Legenda (Keterangan Warna)
                string legendScript = @"
                    var legend = L.control({position: 'bottomleft'});
                    legend.onAdd = function (map) {
                        var div = L.DomUtil.create('div', 'legend');
                        div.innerHTML = '<h4>Keterangan</h4>' +
                            '<div><span style=""background:#3498db""></span> TPS Resmi</div>' +
                            '<div><span style=""background:#e74c3c""></span> Laporan Baru</div>' +
                            '<div><span style=""background:#f1c40f""></span> Proses</div>' +
                            '<div><span style=""background:#2ecc71""></span> Selesai</div>';
                        return div;
                    };
                    legend.addTo(map);
                ";
                await webViewMap.ExecuteScriptAsync(legendScript);

                // 3. Load Data
                await Task.Delay(800);
                await LoadAllMarkers();
            }
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "-";
            return input.Replace("'", "\\'").Replace("\"", "").Replace("\n", " ").Trim();
        }

        // =================================================================
        // LOGIKA MARKER (LOAD DATA)
        // =================================================================
        private async Task LoadAllMarkers()
        {
            if (!isMapReady) return;

            try
            {
                await webViewMap.ExecuteScriptAsync("clearMarkers()");

                string selectedFilter = comboFilterStatus.SelectedItem?.ToString() ?? "Semua Status";

                // 1. LOAD MARKER MASTER TPS (BIRU)
                var listMaster = collectionMaster.Find(_ => true).ToList();
                foreach (var tps in listMaster)
                {
                    if (tps.Latitude != 0 && tps.Longitude != 0)
                    {
                        string lat = tps.Latitude.ToString(CultureInfo.InvariantCulture);
                        string lon = tps.Longitude.ToString(CultureInfo.InvariantCulture);
                        string judul = CleanText(tps.NamaTPS);

                        // Z-INDEX = 0 (Belakang)
                        string script = $"addMarker({lat}, {lon}, '{judul}', 'Lokasi TPS Resmi', '#3498db', 0)";
                        await webViewMap.ExecuteScriptAsync(script);
                    }
                }

                // 2. LOAD MARKER LAPORAN SAMPAH
                var builder = Builders<SampahModel>.Filter;
                var filter = builder.Empty;

                if (selectedFilter != "Semua Status")
                {
                    filter = builder.Eq(x => x.Status, selectedFilter);
                }

                var listSampah = collectionSampah.Find(filter).ToList();

                foreach (var item in listSampah)
                {
                    if (item.Latitude != 0 && item.Longitude != 0)
                    {
                        // OFFSET: Geser sedikit ke Utara (+0.00015)
                        double latOffset = item.Latitude + 0.00015;

                        string lat = latOffset.ToString(CultureInfo.InvariantCulture);
                        string lon = item.Longitude.ToString(CultureInfo.InvariantCulture);

                        string judul = CleanText(item.Wilayah);
                        string tgl = item.Tanggal.ToString("dd MMM");
                        string desc = CleanText($"{item.Jenis} ({item.Berat} Kg)<br>Status: {item.Status}<br>Tgl: {tgl}");

                        string color = "#e74c3c"; // Merah
                        if (item.Status == "Selesai") color = "#2ecc71"; // Hijau
                        else if (item.Status == "Dipilah" || item.Status == "Daur Ulang") color = "#f1c40f"; // Kuning

                        // Z-INDEX = 1000 (Depan)
                        string script = $"addMarker({lat}, {lon}, '{judul}', '{desc}', '{color}', 1000)";
                        await webViewMap.ExecuteScriptAsync(script);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat marker: " + ex.Message);
            }
        }
    }
}