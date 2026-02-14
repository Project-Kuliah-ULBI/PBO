using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
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
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;
        private WebView2 webViewInput;

        private string _userId;
        private string _userRole;
        private string _sampahId;
        private bool _isEditMode;

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        public FormInput(string userId, string role, string sampahId = null)
        {
            InitializeComponent();
            ConnectDB();

            this._userId = userId;
            this._userRole = role;
            this._sampahId = sampahId;
            this._isEditMode = !string.IsNullOrEmpty(sampahId);

            SetupUI();
            InitDataMaster();
            InitMapWebView();
            ApplyRolePermissions();

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

        private void ConnectDB()
        {
            try
            {
                var client = new MongoClient(MongoHelper.ConnectionString);
                var db = client.GetDatabase(MongoHelper.DatabaseName);
                collectionSampah = db.GetCollection<SampahModel>("Sampah");
                collectionMaster = db.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex) { MessageBox.Show("Database connection error: " + ex.Message); }
        }

        private void SetupUI()
        {
            this.BackColor = StyleHelper.BackgroundColor;

            if (comboWilayah != null)
            {
                comboWilayah.DropDownStyle = ComboBoxStyle.DropDown;
                comboWilayah.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                comboWilayah.AutoCompleteSource = AutoCompleteSource.ListItems;
                StyleHelper.StyleInput(comboWilayah);
            }

            if (comboStatus != null)
            {
                comboStatus.Items.Clear();
                comboStatus.Items.AddRange(new object[] { "Masuk", "Dipilah", "Daur Ulang", "Selesai" });
                StyleHelper.StyleInput(comboStatus);
            }

            if (comboJenis != null)
            {
                comboJenis.Items.Clear();
                comboJenis.Items.AddRange(new object[] { "Organik", "Anorganik", "B3", "Campuran" });
                StyleHelper.StyleInput(comboJenis);
            }

            if (txtLatitude != null) StyleHelper.StyleInput(txtLatitude);
            if (txtLongitude != null) StyleHelper.StyleInput(txtLongitude);
            if (txtKeterangan != null) StyleHelper.StyleInput(txtKeterangan);
            if (numBerat != null) StyleHelper.StyleInput(numBerat);

            if (btnSimpan != null) StyleHelper.StyleButton(btnSimpan, StyleHelper.PrimaryColor, Color.White);
            if (btnReset != null) StyleHelper.StyleSecondaryButton(btnReset);
            if (btnBatal != null) StyleHelper.StyleButton(btnBatal, StyleHelper.DangerColor, Color.White);
            
            Control pnlHeader = this.Controls.Find("panelHeader", true).Length > 0 ? this.Controls.Find("panelHeader", true)[0] : null;
            if (pnlHeader != null) pnlHeader.BackColor = Color.White;
        }

        private void ApplyRolePermissions()
        {
            if (_userRole == "Masyarakat")
            {
                if (comboStatus != null) comboStatus.Enabled = false;
                if (!_isEditMode && comboStatus != null) comboStatus.SelectedItem = "Masuk";
                if (dtpJadwal != null) dtpJadwal.Enabled = false;
            }
            else if (_userRole == "Petugas")
            {
                if (comboStatus != null) comboStatus.Enabled = true;
                if (dtpJadwal != null) dtpJadwal.Enabled = true;
                if (numBerat != null) numBerat.Enabled = true;
            }
        }

        private void InitDataMaster()
        {
            try
            {
                var listLokasi = collectionMaster.Find(_ => true).ToList();
                listLokasi.Insert(0, new MasterLokasiModel { NamaTPS = "- Select or Type New -", Latitude = 0, Longitude = 0 });

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

        private void ComboWilayah_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboWilayah.SelectedItem is MasterLokasiModel lokasi)
            {
                if (lokasi.Latitude != 0 && lokasi.Longitude != 0)
                {
                    UpdateCoordinates(lokasi.Latitude, lokasi.Longitude);
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

            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SiJabarApp", "WebView2", "FormInput");
                    var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    await webViewInput.EnsureCoreWebView2Async(env);

                    string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");
                    if (File.Exists(htmlPath))
                    {
                        webViewInput.Source = new Uri(htmlPath);
                        webViewInput.NavigationCompleted += WebViewInput_NavigationCompleted;
                        webViewInput.WebMessageReceived += WebViewInput_WebMessageReceived;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                        await Task.Delay(1000);
                    else
                        MessageBox.Show("Failed to initialize Map: " + ex.Message);
                }
            }
        }

        private async void WebViewInput_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                await webViewInput.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                await webViewInput.ExecuteScriptAsync("setInputMode(true)");

                try
                {
                    if (collectionMaster != null)
                    {
                        var listMaster = collectionMaster.Find(_ => true).ToList();
                        foreach (var tps in listMaster)
                        {
                            if (tps.Latitude != 0 && tps.Longitude != 0)
                            {
                                string lat = tps.Latitude.ToString(CultureInfo.InvariantCulture);
                                string lon = tps.Longitude.ToString(CultureInfo.InvariantCulture);
                                string judul = tps.NamaTPS.Replace("'", "\\'");
                                string script = $"addMarker({lat}, {lon}, '{judul}', 'Official TPS Location', '#3498db', 0)";
                                await webViewInput.ExecuteScriptAsync(script);
                            }
                        }
                    }
                }
                catch { }

                if (_isEditMode && txtLatitude != null && !string.IsNullOrEmpty(txtLatitude.Text))
                {
                    try
                    {
                        double lat = double.Parse(txtLatitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture);
                        double lon = double.Parse(txtLongitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture);
                        string script = $"setLocation({lat.ToString(CultureInfo.InvariantCulture)}, {lon.ToString(CultureInfo.InvariantCulture)})";
                        await webViewInput.ExecuteScriptAsync(script);
                    }
                    catch { }
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

        private async void btnSimpan_Click(object sender, EventArgs e)
        {
            string namaWilayah = comboWilayah.Text;

            if (string.IsNullOrEmpty(namaWilayah) || string.IsNullOrEmpty(comboJenis.Text) || namaWilayah.StartsWith("- Select"))
            {
                MessageBox.Show("Field yang wajib diisi masih kosong!");
                return;
            }

            double lat = 0, lon = 0;
            double.TryParse(txtLatitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            double.TryParse(txtLongitude.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out lon);

            if (lat == 0 && lon == 0)
            {
                MessageBox.Show("Lokasi belum diatur!");
                return;
            }

            string statusValue = comboStatus.Text;
            string jenisValue = comboJenis.Text;

            var sampah = new SampahModel
            {
                UserId = this._userId,
                Wilayah = namaWilayah,
                Jenis = jenisValue,
                Berat = (double)numBerat.Value,
                Status = (_userRole == "Masyarakat") ? "Masuk" : statusValue,
                Latitude = lat,
                Longitude = lon,
                Tanggal = dtpTanggal.Value,
                JadwalAngkut = dtpJadwal.Value,
                Keterangan = txtKeterangan.Text
            };

            try
            {
                if (!_isEditMode)
                {
                    collectionSampah.InsertOne(sampah);
                }
                else
                {
                    sampah.Id = _sampahId;
                    collectionSampah.ReplaceOne(Builders<SampahModel>.Filter.Eq(x => x.Id, _sampahId), sampah);
                }

                // AI Sync
                try
                {
                    string tgl = sampah.Tanggal.ToString("dd MMMM yyyy");
                    string ragText = $"Report: In {sampah.Wilayah}, " +
                        $"{sampah.Jenis} waste weighs {sampah.Berat} Kg " +
                        $"reported on {tgl}. Status: {sampah.Status}. " +
                        (!string.IsNullOrEmpty(sampah.Keterangan) ? $"Info: {sampah.Keterangan}." : "");

                    float[] vector = await MistralHelper.GetEmbedding(ragText);
                    if (vector != null)
                    {
                        var supaHelper = new SupabaseHelper();
                        await supaHelper.InsertDocumentAsync(ragText, this._userId, vector);
                        MessageBox.Show("Data berhasil disinkronkan dengan AI Knowledge Base.", "Berhasil");
                    }
                }
                catch (Exception exRag)
                {
                    System.Diagnostics.Debug.WriteLine("RAG Sync Error: " + exRag.Message);
                }

                // Auto-save Master Location
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
            catch (Exception ex) { MessageBox.Show("Terjadi kesalahan: " + ex.Message); }
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
            }
        }

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
