using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MongoDB.Driver;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using SiJabarApp.helper;
using SiJabarApp.model;
using System.Globalization;

namespace SiJabarApp
{
    public partial class FormInput : Form
    {
        // --- VARIABEL GLOBAL ---
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;

        private WebView2 webViewInput;

        // --- VARIABEL STATE (USER & EDIT) ---
        private string _userId;
        private string _userRole;     // Menyimpan Role User
        private string _sampahId;     // ID Data (Null jika baru)
        private bool _isEditMode;     // Penanda apakah sedang Edit

        // --- DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // ====================================================================
        // 1. KONSTRUKTOR TUNGGAL (MENERIMA 3 PARAMETER)
        // ====================================================================
        public FormInput(string userId, string role, string sampahId = null)
        {
            InitializeComponent();
            ConnectDB();

            // Simpan State
            this._userId = userId;
            this._userRole = role;
            this._sampahId = sampahId;
            this._isEditMode = !string.IsNullOrEmpty(sampahId);

            // Setup UI Dasar
            SetupUI();
            InitDataMaster(); // Isi ComboBox Wilayah
            InitMapWebView(); // Siapkan Peta

            // --- LOGIKA ROLE (KUNCI AKSES) ---
            AturAksesRole();

            // Jika Edit Mode, Load Datanya
            if (_isEditMode)
            {
                if (lblHeaderTitle != null) lblHeaderTitle.Text = "EDIT DATA SAMPAH";
                if (btnSimpan != null) btnSimpan.Text = "UPDATE DATA";
                LoadDataForEdit();
            }
            else
            {
                if (lblHeaderTitle != null) lblHeaderTitle.Text = "INPUT DATA BARU";
                if (dtpTanggal != null) dtpTanggal.Value = DateTime.Now;
                if (dtpJadwal != null) dtpJadwal.Value = DateTime.Now.AddDays(1);
            }
        }

        // ====================================================================
        // 2. SETUP & KONEKSI
        // ====================================================================
        private void ConnectDB()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var db = client.GetDatabase("SiJabarDB");
                collectionSampah = db.GetCollection<SampahModel>("Sampah");
                collectionMaster = db.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex) { MessageBox.Show("Error DB: " + ex.Message); }
        }

        private void SetupUI()
        {
            if (comboWilayah != null)
            {
                comboWilayah.DropDownStyle = ComboBoxStyle.DropDown; // Bisa ngetik
                comboWilayah.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                comboWilayah.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            // Pastikan item Status/Jenis ada
            if (comboStatus != null && comboStatus.Items.Count == 0)
                comboStatus.Items.AddRange(new object[] { "Masuk", "Dipilah", "Daur Ulang", "Selesai" });

            if (comboJenis != null && comboJenis.Items.Count == 0)
                comboJenis.Items.AddRange(new object[] { "Organik", "Anorganik", "B3", "Campuran" });
        }

        // --- PENTING: LOGIKA PEMBATASAN ROLE ---
        private void AturAksesRole()
        {
            if (_userRole == "Masyarakat")
            {
                // Masyarakat TIDAK BOLEH ubah Status (harus 'Masuk')
                if (comboStatus != null) comboStatus.Enabled = false;
                if (!_isEditMode && comboStatus != null) comboStatus.SelectedItem = "Masuk";

                // Masyarakat TIDAK BOLEH set Jadwal Angkut
                if (dtpJadwal != null) dtpJadwal.Enabled = false;
            }
            else if (_userRole == "Petugas")
            {
                // Petugas BOLEH ubah segalanya
                if (comboStatus != null) comboStatus.Enabled = true;
                if (dtpJadwal != null) dtpJadwal.Enabled = true;
                if (numBerat != null) numBerat.Enabled = true;
            }
        }

        // Load Data Master TPS ke ComboBox
        private void InitDataMaster()
        {
            try
            {
                var listLokasi = collectionMaster.Find(_ => true).ToList();
                listLokasi.Insert(0, new MasterLokasiModel { NamaTPS = "- Pilih / Ketik Baru -", Latitude = 0, Longitude = 0 });

                if (comboWilayah != null)
                {
                    comboWilayah.DataSource = listLokasi;
                    comboWilayah.DisplayMember = "NamaTPS";
                    comboWilayah.ValueMember = null;
                    comboWilayah.SelectedIndexChanged += ComboWilayah_SelectedIndexChanged;
                }
            }
            catch { }
        }

        // ====================================================================
        // 3. LOGIKA MAP & KOORDINAT
        // ====================================================================
        private void ComboWilayah_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboWilayah.SelectedItem is MasterLokasiModel lokasi)
            {
                if (lokasi.Latitude != 0 && lokasi.Longitude != 0)
                {
                    UpdateCoordinates(lokasi.Latitude, lokasi.Longitude);
                    // Pindah Peta
                    if (webViewInput != null && webViewInput.CoreWebView2 != null)
                    {
                        string sLat = lokasi.Latitude.ToString(CultureInfo.InvariantCulture);
                        string sLon = lokasi.Longitude.ToString(CultureInfo.InvariantCulture);
                        webViewInput.ExecuteScriptAsync($"setLocation({sLat}, {sLon})");
                    }
                }
            }
        }

        private async void InitMapWebView()
        {
            webViewInput = new WebView2();
            webViewInput.Dock = DockStyle.Fill;

            if (this.Controls.Find("panelMapInput", true).Length > 0)
                this.Controls.Find("panelMapInput", true)[0].Controls.Add(webViewInput);
            else
                this.Controls.Add(webViewInput);

            await webViewInput.EnsureCoreWebView2Async();

            string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "map.html");
            if (File.Exists(htmlPath))
            {
                webViewInput.Source = new Uri(htmlPath);
                webViewInput.NavigationCompleted += WebViewInput_NavigationCompleted;
                webViewInput.WebMessageReceived += WebViewInput_WebMessageReceived;
            }
        }

        private async void WebViewInput_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                await webViewInput.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                await webViewInput.ExecuteScriptAsync("setInputMode(true)"); // Mode Input Nyala

                // Jika Edit, Load Marker Lama
                if (_isEditMode && txtLatitude != null && !string.IsNullOrEmpty(txtLatitude.Text))
                {
                    string lat = txtLatitude.Text;
                    string lon = txtLongitude.Text;
                    await webViewInput.ExecuteScriptAsync($"setLocation({lat}, {lon})");
                }
            }
        }

        private void WebViewInput_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string jsonString = e.TryGetWebMessageAsString();
                var data = JObject.Parse(jsonString);
                double lat = double.Parse(data["lat"].ToString());
                double lon = double.Parse(data["lng"].ToString());
                UpdateCoordinates(lat, lon);
            }
            catch { }
        }

        private void UpdateCoordinates(double lat, double lon)
        {
            if (txtLatitude != null) txtLatitude.Text = lat.ToString(CultureInfo.InvariantCulture);
            if (txtLongitude != null) txtLongitude.Text = lon.ToString(CultureInfo.InvariantCulture);
        }

        // ====================================================================
        // 4. LOGIKA CRUD (SIMPAN / EDIT)
        // ====================================================================
        private void btnSimpan_Click(object sender, EventArgs e)
        {
            string namaWilayah = comboWilayah.Text;

            if (string.IsNullOrEmpty(namaWilayah) || string.IsNullOrEmpty(comboJenis.Text) || namaWilayah.StartsWith("- Pilih"))
            {
                MessageBox.Show("Wilayah dan Jenis Sampah wajib diisi!");
                return;
            }

            double lat = 0, lon = 0;
            double.TryParse(txtLatitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            double.TryParse(txtLongitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lon);

            if (lat == 0 && lon == 0)
            {
                MessageBox.Show("Lokasi belum ditentukan! Klik Peta atau Pilih Wilayah.");
                return;
            }

            // --- BUILD OBJECT SAMPAH ---
            var sampah = new SampahModel
            {
                UserId = this._userId, // Gunakan UserId dari session
                Wilayah = namaWilayah,
                Jenis = comboJenis.Text,
                Berat = (double)numBerat.Value,

                // LOGIKA STATUS: Jika Masyarakat -> Paksa 'Masuk'
                Status = (_userRole == "Masyarakat") ? "Masuk" : comboStatus.Text,

                Latitude = lat,
                Longitude = lon,
                Tanggal = dtpTanggal.Value,
                JadwalAngkut = dtpJadwal.Value,
                Keterangan = txtKeterangan.Text
            };

            try
            {
                // SIMPAN / UPDATE
                if (!_isEditMode) // BARU
                {
                    collectionSampah.InsertOne(sampah);
                }
                else // EDIT
                {
                    sampah.Id = _sampahId;
                    collectionSampah.ReplaceOne(Builders<SampahModel>.Filter.Eq(x => x.Id, _sampahId), sampah);
                }

                // AUTO-SAVE MASTER LOKASI (Fitur Pintar)
                var cekMaster = collectionMaster.Find(x => x.NamaTPS.ToLower() == namaWilayah.ToLower()).FirstOrDefault();
                if (cekMaster == null)
                {
                    if (MessageBox.Show($"Simpan '{namaWilayah}' ke Master Lokasi?", "Lokasi Baru", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        collectionMaster.InsertOne(new MasterLokasiModel { NamaTPS = namaWilayah, Latitude = lat, Longitude = lon });
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Gagal: " + ex.Message); }
        }

        private void LoadDataForEdit()
        {
            var data = collectionSampah.Find(x => x.Id == _sampahId).FirstOrDefault();
            if (data != null)
            {
                comboWilayah.Text = data.Wilayah;
                comboJenis.Text = data.Jenis;
                numBerat.Value = (decimal)data.Berat;
                comboStatus.Text = data.Status;

                UpdateCoordinates(data.Latitude, data.Longitude);

                dtpTanggal.Value = data.Tanggal;
                dtpJadwal.Value = (data.JadwalAngkut == DateTime.MinValue) ? DateTime.Now : data.JadwalAngkut;
                txtKeterangan.Text = data.Keterangan;

                // Pastikan UserId tidak berubah saat edit admin/petugas
                // this._userId = data.UserId; (Opsional, tergantung kebijakan)
            }
        }

        // ====================================================================
        // 5. WINDOW CONTROLS
        // ====================================================================
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (txtLatitude != null) txtLatitude.Text = "0";
            if (txtLongitude != null) txtLongitude.Text = "0";
            if (comboWilayah != null) comboWilayah.Text = "";
            if (txtKeterangan != null) txtKeterangan.Text = "";
            if (webViewInput != null) webViewInput.ExecuteScriptAsync("removeInputMarker()");
        }

        private void btnBatal_Click(object sender, EventArgs e) => this.Close();
        private void btnClose_Click(object sender, EventArgs e) => this.Close();
        private void panelHeader_MouseDown(object sender, MouseEventArgs e) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); }
    }
}