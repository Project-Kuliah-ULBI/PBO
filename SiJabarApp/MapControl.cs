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
                var client = new MongoClient("mongodb://localhost:27017");
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
            comboFilterStatus.Location = new Point(120, 17);
            comboFilterStatus.SelectedIndexChanged += async (s, e) => {
                await LoadAllMarkers();
            };
            panelTop.Controls.Add(comboFilterStatus);

            // TOMBOL REFRESH
            btnRefresh = new Button();
            btnRefresh.Text = "Refresh Peta";
            btnRefresh.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            btnRefresh.Size = new Size(120, 32);
            btnRefresh.Location = new Point(320, 16);
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
                    webViewMap.Source = new Uri(htmlPath);
                    webViewMap.NavigationCompleted += WebViewMap_NavigationCompleted;
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
                // Init Peta Bandung
                await webViewMap.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                
                // Init Legend
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
        public async Task LoadAllMarkers()
        {
            if (!isMapReady || collectionSampah == null) return;

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
                        double latOffset = item.Latitude + 0.00015;
                        string lat = latOffset.ToString(CultureInfo.InvariantCulture);
                        string lon = item.Longitude.ToString(CultureInfo.InvariantCulture);
                        string judul = CleanText(item.Wilayah);
                        string tgl = item.Tanggal.ToString("dd MMM");
                        string desc = CleanText($"{item.Jenis} ({item.Berat} Kg)<br>Status: {item.Status}<br>Tgl: {tgl}");

                        string color = "#e74c3c"; // Merah
                        if (item.Status == "Selesai") color = "#2ecc71"; // Hijau
                        else if (item.Status == "Dipilah" || item.Status == "Daur Ulang") color = "#f1c40f"; // Kuning

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
