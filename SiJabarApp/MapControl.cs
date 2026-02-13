using System;
using System.IO;
using System.Drawing; 
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization; 
using System.Threading.Tasks; 
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MongoDB.Driver;
using SiJabarApp.helper;
using SiJabarApp.model;
using Newtonsoft.Json.Linq;

namespace SiJabarApp
{
    public partial class MapControl : UserControl
    {
        // --- KOMPONEN UI ---
        private WebView2 webViewMap;
        private ComboBox comboFilterStatus;
        private Button btnRefresh;
        private Panel panelTop;

        // --- DATABASE ---
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;
        private bool isMapReady = false;

        private string _userRole;
        public string UserRole 
        { 
            get => _userRole; 
            set 
            { 
                _userRole = value; 
                if (btnKelolaMarker != null && btnUpdateLokasiUser != null)
                {
                    bool isAdmin = (_userRole == "Admin");
                    btnKelolaMarker.Visible = isAdmin;
                    
                    // JIKA BUKAN ADMIN, GESER TOMBOL UPDATE LOKASI KE KIRI (POSISI KELOLA MARKER)
                    if (isAdmin) {
                        btnUpdateLokasiUser.Location = new Point(620, 16);
                    } else {
                        btnUpdateLokasiUser.Location = new Point(480, 16);
                    }
                }
            }
        }
        private Button btnKelolaMarker;
        private Button btnUpdateLokasiUser;
        private bool isPickMode = false;
        private string _activeUserId; // Need to set this from MainForm
        public string ActiveUserId { get => _activeUserId; set => _activeUserId = value; }

        public MapControl()
        {
            InitializeComponent();
            ConnectDB();
            InitUI(); 
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;
        }

        private void ConnectDB()
        {
            try
            {
                // var client = new MongoClient("mongodb://localhost:27017");
                var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                var database = client.GetDatabase("SiJabarDB");
                collectionSampah = database.GetCollection<SampahModel>("Sampah");
                collectionMaster = database.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex) { MessageBox.Show("Error Koneksi DB: " + ex.Message); }
        }

        // =================================================================
        // MEMBANGUN UI (MODERN STYLE)
        // =================================================================
        private void InitUI()
        {
            // 1. PANEL ATAS (FILTER & REFRESH)
            panelTop = new Panel();
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 60; 
            panelTop.BackColor = Color.White;
            panelTop.Padding = new Padding(20, 10, 20, 10);
            this.Controls.Add(panelTop);

            // LABEL FILTER
            Label lblFilter = new Label();
            lblFilter.Text = "Filter Status:";
            lblFilter.AutoSize = true;
            lblFilter.Location = new Point(20, 20);
            lblFilter.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblFilter.ForeColor = Color.FromArgb(80, 80, 80);
            panelTop.Controls.Add(lblFilter);

            // COMBOBOX
            comboFilterStatus = new ComboBox();
            comboFilterStatus.Items.AddRange(new object[] { "Semua Status", "Masuk", "Dipilah", "Daur Ulang", "Selesai" });
            comboFilterStatus.SelectedIndex = 0;
            comboFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboFilterStatus.Font = new Font("Segoe UI", 10);
            comboFilterStatus.Width = 180;
            comboFilterStatus.Location = new Point(140, 17); // Geser kanan (prev: 120)
            comboFilterStatus.SelectedIndexChanged += async (s, e) => {
                await LoadAllMarkers();
            };
            panelTop.Controls.Add(comboFilterStatus);

            // TOMBOL REFRESH
            btnRefresh = new Button();
            btnRefresh.Text = "Refresh Peta";
            btnRefresh.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            btnRefresh.Size = new Size(120, 32);
            btnRefresh.Location = new Point(340, 16); // Geser kanan (prev: 320)
            btnRefresh.BackColor = Color.FromArgb(33, 150, 243); 
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += async (s, e) => {
                btnRefresh.Text = "Loading...";
                btnRefresh.Enabled = false;
                await LoadAllMarkers();
                btnRefresh.Text = "Refresh Peta";
                btnRefresh.Enabled = true;
            };
            panelTop.Controls.Add(btnRefresh);

            // TOMBOL KELOLA MARKER (ADMIN ONLY)
            btnKelolaMarker = new Button();
            btnKelolaMarker.Text = "Kelola Marker";
            btnKelolaMarker.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            btnKelolaMarker.Size = new Size(120, 32);
            btnKelolaMarker.Location = new Point(480, 16); // Geser kanan setelah Refresh
            btnKelolaMarker.BackColor = Color.FromArgb(46, 204, 113); // Green
            btnKelolaMarker.ForeColor = Color.White;
            btnKelolaMarker.FlatStyle = FlatStyle.Flat;
            btnKelolaMarker.FlatAppearance.BorderSize = 0;
            btnKelolaMarker.Cursor = Cursors.Hand;
            btnKelolaMarker.Visible = false; // Default Hidden
            btnKelolaMarker.Click += async (s, e) => {
                FormMasterLokasi frm = new FormMasterLokasi();
                frm.ShowDialog();
                // Refresh map after form closes — await to ensure markers fully loaded
                await LoadAllMarkers();
            };
            panelTop.Controls.Add(btnKelolaMarker);

            // TOMBOL UPDATE LOKASI USER (ALL ROLES)
            btnUpdateLokasiUser = new Button();
            btnUpdateLokasiUser.Text = "Update Lokasi Saya";
            btnUpdateLokasiUser.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            btnUpdateLokasiUser.Size = new Size(150, 32);
            btnUpdateLokasiUser.Location = new Point(620, 16); // Geser setelah Kelola Marker
            btnUpdateLokasiUser.BackColor = Color.FromArgb(142, 68, 173); // Amethyst Purple
            btnUpdateLokasiUser.ForeColor = Color.White;
            btnUpdateLokasiUser.FlatStyle = FlatStyle.Flat;
            btnUpdateLokasiUser.FlatAppearance.BorderSize = 0;
            btnUpdateLokasiUser.Cursor = Cursors.Hand;
            btnUpdateLokasiUser.Click += async (s, e) => {
                if (!isPickMode)
                {
                    isPickMode = true;
                    btnUpdateLokasiUser.Text = "Pilih di Peta...";
                    btnUpdateLokasiUser.BackColor = Color.FromArgb(192, 57, 43); // Red-ish
                    await webViewMap.ExecuteScriptAsync("setInputMode(true)");
                    
                    // OTOMATIS TRIGGER GPS SAAT KLIK
                    await webViewMap.ExecuteScriptAsync("locateUser()");
                }
                else
                {
                    CancelPickMode();
                }
            };
            panelTop.Controls.Add(btnUpdateLokasiUser);

            // 2. WEBVIEW PETA
            webViewMap = new WebView2();
            webViewMap.Dock = DockStyle.Fill;
            this.Controls.Add(webViewMap);
            webViewMap.BringToFront();

            InitMapWebView();
        }

        private async void InitMapWebView()
        {
            try
            {
                await webViewMap.EnsureCoreWebView2Async();
                string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "map.html");
                if (File.Exists(htmlPath))
                {
                    var fileUri = new Uri(htmlPath).AbsoluteUri + "?v=" + DateTime.Now.Ticks;
                    webViewMap.Source = new Uri(fileUri);
                    webViewMap.NavigationCompleted += WebViewMap_NavigationCompleted;
                    webViewMap.WebMessageReceived += WebViewMap_WebMessageReceived; // HANDLE PICK MESSAGE
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal inisialisasi Map: " + ex.Message);
            }
        }

        private async void WebViewMap_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                isMapReady = true;
                // Init Peta Bandung - View Only
                await webViewMap.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                await webViewMap.ExecuteScriptAsync("setInputMode(false)"); // Map view = view-only
                
                // Init Legend
                string legendScript = @"
                    var legend = L.control({position: 'bottomleft'});
                    legend.onAdd = function (map) {
                        var div = L.DomUtil.create('div', 'legend');
                        div.innerHTML = '<h4>Keterangan</h4>' +
                            '<div><span style=""background:#3498db""></span> TPS Resmi</div>' +
                            '<div><span style=""background:#e74c3c""></span> Laporan Baru</div>' +
                            '<div><span style=""background:#f1c40f""></span> Proses</div>' +
                            '<div><span style=""background:#2ecc71""></span> Selesai</div>' +
                            '<hr>' +
                            '<div><span style=""background:#2c3e50""></span> User (Admin)</div>' +
                            '<div><span style=""background:#d35400""></span> User (Petugas)</div>' +
                            '<div><span style=""background:#8e44ad""></span> User (Masyarakat)</div>';
                        return div;
                    };
                    legend.addTo(map);
                ";
                await webViewMap.ExecuteScriptAsync(legendScript);

                await Task.Delay(800);
                await LoadAllMarkers();
            }
        }

        private async void WebViewMap_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string jsonString = e.TryGetWebMessageAsString();
                Newtonsoft.Json.Linq.JObject data = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
                
                string msgType = data["type"]?.ToString() ?? "manual";
                double lat = (double)data["lat"];
                double lon = (double)data["lng"];

                // SECURITY: Only process manual clicks if PickMode is actually active
                if (msgType == "gps" || (msgType == "click" && isPickMode))
                {
                    var mongo = new MongoHelper();
                    bool success = mongo.UpdateUserLocation(_activeUserId, lat, lon);

                    if (success)
                    {
                        if (msgType == "gps")
                        {
                            MessageBox.Show("Lokasi GPS berhasil diperbarui!", "GPS Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Lokasi manual berhasil diperbarui!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        
                        await LoadAllMarkers();
                        if (isPickMode) CancelPickMode();
                    }
                    else
                    {
                        MessageBox.Show("Gagal memperbarui lokasi di database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error WebMessage: " + ex.Message);
            }
        }

        private async void CancelPickMode()
        {
            isPickMode = false;
            btnUpdateLokasiUser.Text = "Update Lokasi Saya";
            btnUpdateLokasiUser.BackColor = Color.FromArgb(142, 68, 173);
            await webViewMap.ExecuteScriptAsync("setInputMode(false)");
            await webViewMap.ExecuteScriptAsync("removeInputMarker()");
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "-";
            return input.Replace("'", "\\'").Replace("\"", "").Replace("\n", " ").Trim();
        }

        // =================================================================
        // LOGIKA MARKER (LOAD DATA)
        // =================================================================
        public async Task LoadAllMarkers()
        {
            if (!isMapReady || collectionSampah == null || collectionMaster == null) return;

            try
            {
                // Verify JavaScript map object is actually ready
                string mapCheck = await webViewMap.ExecuteScriptAsync("isMapReady()");
                if (mapCheck != "true")
                {
                    await Task.Delay(500);
                    mapCheck = await webViewMap.ExecuteScriptAsync("isMapReady()");
                    if (mapCheck != "true") return;
                }

                await webViewMap.ExecuteScriptAsync("clearMarkers()");

                string selectedFilter = comboFilterStatus.SelectedItem?.ToString() ?? "Semua Status";

                // 1. LOAD MARKER MASTER TPS (BIRU)
                var listMaster = collectionMaster.Find(_ => true).ToList();
                foreach (var tps in listMaster)
                {
                    double lat = tps.Latitude;
                    double lon = tps.Longitude;
                    if (lat == 0 || lon == 0) continue;

                    // AUTO-REPAIR: Fix corrupted coordinates (decimal point lost due to locale bug)
                    if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
                    {
                        bool repaired = RepairCoordinate(ref lat, ref lon);
                        if (repaired)
                        {
                            try {
                                var update = Builders<MasterLokasiModel>.Update
                                    .Set(x => x.Latitude, lat)
                                    .Set(x => x.Longitude, lon);
                                collectionMaster.UpdateOne(
                                    Builders<MasterLokasiModel>.Filter.Eq(x => x.Id, tps.Id), update);
                            } catch { }
                        }
                        else continue; // Can't repair, skip this record
                    }

                    string latStr = lat.ToString(CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(CultureInfo.InvariantCulture);
                    string judul = CleanText(tps.NamaTPS);
                    string script = $"addMarker({latStr}, {lonStr}, '{judul}', 'Lokasi TPS Resmi', '#3498db', 0)";
                    await webViewMap.ExecuteScriptAsync(script);
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
                    double lat = item.Latitude;
                    double lon = item.Longitude;
                    if (lat == 0 || lon == 0) continue;

                    // AUTO-REPAIR for Sampah records too
                    if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
                    {
                        bool repaired = RepairCoordinate(ref lat, ref lon);
                        if (repaired)
                        {
                            try {
                                var update = Builders<SampahModel>.Update
                                    .Set(x => x.Latitude, lat)
                                    .Set(x => x.Longitude, lon);
                                collectionSampah.UpdateOne(
                                    Builders<SampahModel>.Filter.Eq(x => x.Id, item.Id), update);
                            } catch { }
                        }
                        else continue;
                    }

                    double latOffset = lat + 0.00015;
                    string latStr = latOffset.ToString(CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(CultureInfo.InvariantCulture);
                    string judul = CleanText(item.Wilayah);
                    string tgl = item.Tanggal.ToString("dd MMM");
                    string desc = CleanText($"{item.Jenis} ({item.Berat} Kg)<br>Status: {item.Status}<br>Tgl: {tgl}");

                    string color = "#e74c3c";
                    if (item.Status == "Selesai") color = "#2ecc71";
                    else if (item.Status == "Dipilah" || item.Status == "Daur Ulang") color = "#f1c40f";

                    string script = $"addMarker({latStr}, {lonStr}, '{judul}', '{desc}', '{color}', 1000)";
                    await webViewMap.ExecuteScriptAsync(script);
                }

                // 3. LOAD MARKER USER (ROLE-BASED)
                var mongoHelper = new MongoHelper();
                var listUsers = mongoHelper.GetAllUsers();
                foreach (var user in listUsers)
                {
                    double lat = user.Latitude;
                    double lon = user.Longitude;

                    // REPAIR COORDINATES IF NEEDED
                    if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
                    {
                        if (!RepairCoordinate(ref lat, ref lon)) continue;
                    }

                    string color = "gray"; 
                    string role = user.Role;
                    if (role == "Admin") color = "admin";
                    else if (role == "Petugas") color = "petugas";
                    else if (role == "Masyarakat") color = "masyarakat";

                    string judul = CleanText(user.Fullname);
                    string desc = $"Role: {role}";
                    string script = $"addMarker({lat.ToString(CultureInfo.InvariantCulture)}, {lon.ToString(CultureInfo.InvariantCulture)}, '{judul}', '{desc}', '{color}', 500)";
                    await webViewMap.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Gagal memuat marker: " + ex.Message);
            }
        }

        // Auto-repair coordinates corrupted by locale bug (decimal point stripped)
        // Example: -6.905... was saved as -69053... (dot treated as thousands separator)
        private bool RepairCoordinate(ref double lat, ref double lon)
        {
            bool latOk = Math.Abs(lat) <= 90;
            bool lonOk = Math.Abs(lon) <= 180;

            if (!latOk)
            {
                // Try dividing by powers of 10 until we get valid Indonesia latitude (-11 to 6)
                for (int p = 1; p <= 16; p++)
                {
                    double tryLat = lat / Math.Pow(10, p);
                    if (tryLat >= -11 && tryLat <= 6) { lat = tryLat; latOk = true; break; }
                }
            }

            if (!lonOk)
            {
                // Try dividing by powers of 10 until we get valid Indonesia longitude (95 to 141)
                for (int p = 1; p <= 16; p++)
                {
                    double tryLon = lon / Math.Pow(10, p);
                    if (tryLon >= 95 && tryLon <= 141) { lon = tryLon; lonOk = true; break; }
                }
            }

            return latOk && lonOk;
        }
    }
}
