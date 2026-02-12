using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using MongoDB.Driver;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using SiJabarApp.helper;
using SiJabarApp.model;
using System.Globalization;
using System.Collections.Generic;

namespace SiJabarApp
{
    public partial class FormMasterLokasi : Form
    {
        // --- VARIABEL GLOBAL ---
        private IMongoCollection<MasterLokasiModel> collectionMaster;
        private WebView2 webView;
        private string _idTerpilih = null;

        // --- UI COMPONENTS ---
        private DataGridView gridLokasi;
        private TextBox txtNama, txtLat, txtLon, txtKeterangan;
        private Button btnSimpan, btnHapus, btnReset, btnClose;
        private Label lblTitle;
        private Panel panelMap;

        // --- DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        public FormMasterLokasi()
        {
            InitializeComponent();
            ConnectDB();
            SetupUI();
            InitMapWebView();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1200, 750); // Increased size
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(0); // Remove "Green Line" border
            this.BackColor = Color.White; // Set to white or header color
        }

        private void ConnectDB()
        {
            try
            {
                // var client = new MongoClient("mongodb://localhost:27017");
                var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                var db = client.GetDatabase("SiJabarDB");
                collectionMaster = db.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex) { MessageBox.Show("Error DB: " + ex.Message); }
        }

        private void SetupUI()
        {
            // Main Container
            Panel pMain = new Panel { Dock = DockStyle.Fill, BackColor = StyleHelper.BackgroundColor };
            this.Controls.Add(pMain);

            // 1. HEADER (Black Background)
            Panel pHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Black }; 
            pHeader.Padding = new Padding(20, 0, 20, 0);
            pHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0x112, 0xf012, 0); };
            
            lblTitle = new Label { 
                Text = "KELOLA MASTER LOKASI", 
                Font = new Font("Segoe UI", 14, FontStyle.Bold), 
                ForeColor = Color.White, 
                AutoSize = true, 
                Location = new Point(20, 15) 
            };
            pHeader.Controls.Add(lblTitle);

            btnClose = new Button { Text = "×", FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Size = new Size(40, 40), Dock = DockStyle.Right };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            pHeader.Controls.Add(btnClose);
            
            pMain.Controls.Add(pHeader);

            // 2. CONTENT CONTAINER
            Panel pContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            pMain.Controls.Add(pContent);
            
            // CRITICAL: Ensure Content fills the space *below* Header
            pContent.BringToFront(); 

            // 3. MAIN SPLIT (LEFT: FORM+GRID, RIGHT: MAP)
            SplitContainer splitMain = new SplitContainer { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Vertical, 
                SplitterWidth = 10,
                IsSplitterFixed = true // Keep layout stable
            };
            pContent.Controls.Add(splitMain);

            // Calculate Splitter Distance (50% of content width, approx 1160 / 2 = 580)
            splitMain.SplitterDistance = (1200 - 40) / 2; 

            // --- LEFT PANEL (NESTED SPLIT: INPUTS VS GRID) ---
            SplitContainer splitLeft = new SplitContainer {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 450, // Slightly more space for input form
                SplitterWidth = 10,
                IsSplitterFixed = true // Keep layout stable
            };
            splitMain.Panel1.Controls.Add(splitLeft);
            splitMain.Panel1.Padding = new Padding(0, 0, 10, 0);

            // === TOP LEFT: INPUTS ===
            Panel pInputs = splitLeft.Panel1;
            pInputs.BackColor = StyleHelper.BackgroundColor;
            
            // INCREASED PADDING TO PREVENT ANY HEADER OVERLAP
            int y = 30; 
            int inputWidth = 500; // Wider inputs

            // Nama Lokasi
            pInputs.Controls.Add(CreateLabel("Nama TPA atau TPS", y));
            y += 25;
            txtNama = CreateInput(y, inputWidth);
            pInputs.Controls.Add(txtNama);
            y += 55;

            // Latitude
            pInputs.Controls.Add(CreateLabel("Latitude", y));
            y += 25;
            txtLat = CreateInput(y, inputWidth);
            pInputs.Controls.Add(txtLat);
            y += 55;

            // Longitude
            pInputs.Controls.Add(CreateLabel("Longitude", y));
            y += 25;
            txtLon = CreateInput(y, inputWidth);
            pInputs.Controls.Add(txtLon);
            y += 55;

            // Keterangan
            pInputs.Controls.Add(CreateLabel("Keterangan", y));
            y += 25;
            txtKeterangan = CreateInput(y, inputWidth);
            txtKeterangan.Multiline = true;
            txtKeterangan.Height = 70;
            pInputs.Controls.Add(txtKeterangan);
            y += 90;

            // Buttons
            FlowLayoutPanel pBtn = new FlowLayoutPanel { 
                Location = new Point(0, y), 
                Size = new Size(inputWidth, 50), 
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            
            btnSimpan = new Button { Text = "SIMPAN", Size = new Size(110, 40) };
            StyleHelper.StyleButton(btnSimpan, StyleHelper.PrimaryColor, Color.White);
            btnSimpan.Click += BtnSimpan_Click;
            pBtn.Controls.Add(btnSimpan);

            btnHapus = new Button { Text = "HAPUS", Size = new Size(110, 40), Visible = false };
            StyleHelper.StyleButton(btnHapus, StyleHelper.DangerColor, Color.White);
            btnHapus.Click += BtnHapus_Click;
            pBtn.Controls.Add(btnHapus);

            btnReset = new Button { Text = "RESET", Size = new Size(100, 40) };
            StyleHelper.StyleSecondaryButton(btnReset);
            btnReset.Click += (s, e) => ResetForm();
            pBtn.Controls.Add(btnReset);

            pInputs.Controls.Add(pBtn);

            // === BOTTOM LEFT: GRID ===
            Panel pGrid = splitLeft.Panel2;
            pGrid.Padding = new Padding(0, 10, 0, 0); // Spacing top

            Label lblList = new Label { 
                Text = "Daftar Lokasi:", 
                Font = StyleHelper.FontBold, 
                ForeColor = StyleHelper.SecondaryColor,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            pGrid.Controls.Add(lblList);
            
            // Spacer
            pGrid.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });

            gridLokasi = new DataGridView { 
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // Cleaner selection
                BackgroundColor = Color.White, // Cleaner look
                BorderStyle = BorderStyle.None
            };
            StyleHelper.StyleGridView(gridLokasi);
            // OVERRIDE FOR FULL GREEN HEADER
            gridLokasi.ColumnHeadersDefaultCellStyle.BackColor = StyleHelper.PrimaryColor; // Full Green
            gridLokasi.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            gridLokasi.EnableHeadersVisualStyles = false;

            Control.CheckForIllegalCrossThreadCalls = false;
            gridLokasi.SelectionChanged += GridLokasi_SelectionChanged;
            pGrid.Controls.Add(gridLokasi);

            // === RIGHT PANEL: MAP ===
            panelMap = splitMain.Panel2;
            panelMap.BackColor = Color.White;
            panelMap.Padding = new Padding(2);
        }

        private Label CreateLabel(string text, int y) => new Label { 
            Text = text, 
            Location = new Point(0, y), 
            AutoSize = true, 
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.DimGray
        };
        
        private TextBox CreateInput(int y, int width)
        {
            TextBox t = new TextBox { Location = new Point(0, y), Width = width, Font = new Font("Segoe UI", 11) };
            StyleHelper.StyleInput(t);
            return t;
        }

        // ====================================================================
        // LOGIKA MAP WEBVIEW2
        // ====================================================================
        private async void InitMapWebView()
        {
            webView = new WebView2 { Dock = DockStyle.Fill };
            panelMap.Controls.Add(webView);
            await webView.EnsureCoreWebView2Async();

            string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "map.html");
            if (File.Exists(htmlPath))
            {
                // Revert to file-based loading but construct URI properly
                // This gives correct origin/context for map tiles to load
                var fileUri = new Uri(htmlPath).AbsoluteUri + "?v=" + DateTime.Now.Ticks;
                webView.Source = new Uri(fileUri);
                
                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.WebMessageReceived += WebView_WebMessageReceived;
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                try {
                    await webView.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                    await webView.ExecuteScriptAsync("setInputMode(true)");
                    LoadDataCallback(); // Call marker loading after map is ready
                } catch { }
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string jsonString = e.TryGetWebMessageAsString();
                var data = JObject.Parse(jsonString);
                double lat = double.Parse(data["lat"].ToString(), CultureInfo.InvariantCulture);
                double lon = double.Parse(data["lng"].ToString(), CultureInfo.InvariantCulture);
                
                txtLat.Text = lat.ToString(CultureInfo.InvariantCulture);
                txtLon.Text = lon.ToString(CultureInfo.InvariantCulture);
            }
            catch { }
        }

        // ====================================================================
        // CRUD LOGIC
        // ====================================================================
        private void LoadData()
        {
            LoadDataCallback(); // Also call initially if map is ready? No, rely on callback.
        }

        private async void LoadDataCallback()
        {
            try
            {
                // Detach event to prevent auto-select during data load
                gridLokasi.SelectionChanged -= GridLokasi_SelectionChanged;

                var list = collectionMaster.Find(_ => true).ToList();
                
                gridLokasi.Rows.Clear();
                
                // Only add columns if they don't exist yet
                if (gridLokasi.Columns.Count == 0)
                {
                    gridLokasi.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Visible = false });
                    gridLokasi.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNama", HeaderText = "Nama Lokasi", FillWeight = 40 });
                    gridLokasi.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLat", HeaderText = "Lat", FillWeight = 30 });
                    gridLokasi.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLon", HeaderText = "Lon", FillWeight = 30 });
                    gridLokasi.Columns.Add(new DataGridViewTextBoxColumn { Name = "colKet", HeaderText = "Ket", Visible = false });
                }

                // Clear existing markers on the map before re-adding
                if (webView != null && webView.CoreWebView2 != null)
                {
                    try { await webView.ExecuteScriptAsync("clearMarkers()"); } catch { }
                }
                
                foreach (var item in list)
                {
                    gridLokasi.Rows.Add(item.Id, item.NamaTPS, item.Latitude, item.Longitude, item.Keterangan);
                    
                    // Add Marker to Map
                    if (webView != null && webView.CoreWebView2 != null)
                    {
                         try {
                             string desc = item.Keterangan ?? "-";
                             string safeTitle = item.NamaTPS.Replace("'", "\\'"); 
                             string safeDesc = desc.Replace("'", "\\'");
    
                             string script = $"addMarker({item.Latitude.ToString(CultureInfo.InvariantCulture)}, {item.Longitude.ToString(CultureInfo.InvariantCulture)}, '{safeTitle}', '{safeDesc}', 'blue', 1000)";
                             await webView.ExecuteScriptAsync(script);
                         } catch { }
                    }
                }

                // Clear selection so no row is highlighted
                gridLokasi.ClearSelection();

                // Re-attach event handler
                gridLokasi.SelectionChanged += GridLokasi_SelectionChanged;

                // Reset form to "SIMPAN" (Add) mode
                ResetForm();
            }
            catch (Exception ex) { MessageBox.Show("Error Load: " + ex.Message); }
        }

        private void BtnSimpan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtNama.Text)) { MessageBox.Show("Nama wajib diisi!"); return; }

            double lat = 0, lon = 0;
            // Normalize: replace comma with dot to handle Indonesian locale
            string latText = txtLat.Text.Replace(',', '.');
            string lonText = txtLon.Text.Replace(',', '.');
            double.TryParse(latText, NumberStyles.Any, CultureInfo.InvariantCulture, out lat);
            double.TryParse(lonText, NumberStyles.Any, CultureInfo.InvariantCulture, out lon);

            // Validate coordinates are not zero (would not appear on maps)
            if (lat == 0 || lon == 0)
            {
                MessageBox.Show("Koordinat tidak valid! Klik peta untuk menentukan lokasi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lokasi = new MasterLokasiModel
            {
                NamaTPS = txtNama.Text,
                Latitude = lat,
                Longitude = lon,
                Keterangan = txtKeterangan.Text
            };

            if (_idTerpilih == null)
            {
                collectionMaster.InsertOne(lokasi);
                MessageBox.Show($"Lokasi berhasil ditambah!\nLat: {lat.ToString(CultureInfo.InvariantCulture)}\nLon: {lon.ToString(CultureInfo.InvariantCulture)}");
            }
            else
            {
                lokasi.Id = _idTerpilih;
                collectionMaster.ReplaceOne(Builders<MasterLokasiModel>.Filter.Eq(x => x.Id, _idTerpilih), lokasi);
                MessageBox.Show($"Lokasi berhasil diupdate!\nLat: {lat.ToString(CultureInfo.InvariantCulture)}\nLon: {lon.ToString(CultureInfo.InvariantCulture)}");
            }
            ResetForm();
            LoadData();
        }

        private void BtnHapus_Click(object sender, EventArgs e)
        {
            if (_idTerpilih != null && MessageBox.Show("Hapus lokasi ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                collectionMaster.DeleteOne(Builders<MasterLokasiModel>.Filter.Eq(x => x.Id, _idTerpilih));
                MessageBox.Show("Lokasi dihapus!");
                ResetForm();
                LoadData();
            }
        }

        private void GridLokasi_SelectionChanged(object sender, EventArgs e)
        {
            if (gridLokasi.SelectedRows.Count > 0)
            {
                var row = gridLokasi.SelectedRows[0];
                _idTerpilih = row.Cells["colId"].Value?.ToString();
                
                // Parse coordinates safely, handling nulls and culture
                double lat = 0, lon = 0;
                if (row.Cells["colLat"].Value != null) 
                    double.TryParse(row.Cells["colLat"].Value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out lat);
                
                if (row.Cells["colLon"].Value != null)
                    double.TryParse(row.Cells["colLon"].Value.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out lon);

                // Update UI (Local Format - e.g. Comma for ID)
                txtNama.Text = row.Cells["colNama"].Value?.ToString() ?? "";
                txtLat.Text = lat.ToString(CultureInfo.InvariantCulture); 
                txtLon.Text = lon.ToString(CultureInfo.InvariantCulture);
                
                // Safe Access
                if (row.Cells["colKet"].Value != null)
                    txtKeterangan.Text = row.Cells["colKet"].Value.ToString();
                else
                    txtKeterangan.Text = "";

                btnSimpan.Text = "UPDATE";
                btnSimpan.BackColor = Color.FromArgb(39, 174, 96); // Green for Update
                btnHapus.Visible = true;

                if (webView != null && webView.CoreWebView2 != null)
                {
                    // Send to JS (Must be Dot Format)
                    string script = $"setLocation({lat.ToString(CultureInfo.InvariantCulture)}, {lon.ToString(CultureInfo.InvariantCulture)})";
                    webView.ExecuteScriptAsync(script);
                }
            }
        }

        private void ResetForm()
        {
            _idTerpilih = null;
            txtNama.Text = "";
            txtLat.Text = "";
            txtLon.Text = "";
            txtKeterangan.Text = "";
            btnSimpan.Text = "SIMPAN";
            btnHapus.Visible = false;
            if (webView != null) webView.ExecuteScriptAsync("removeInputMarker()");
        }
    }
}
