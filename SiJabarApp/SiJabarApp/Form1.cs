using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
// Library Tambahan
using Newtonsoft.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using DotNetEnv;

namespace SiJabarApp
{
    public partial class Form1 : Form
    {
        // --- KONFIGURASI API & DB ---
        private static readonly HttpClient client = new HttpClient();
        private IMongoCollection<ChatLog> _chatCollection;
        private string _mistralApiKey;
        private string _selectedModel = "mistral-tiny";

        // --- DLL IMPORT (Drag Window) ---
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // --- KOMPONEN UI ---
        private Panel headerPanel;
        private FlowLayoutPanel chatFlowPanel;
        private Panel inputPanel;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnClose;
        private ComboBox cmbModel;

        // --- WARNA TEMA ---
        private Color primaryColor = Color.FromArgb(40, 180, 99);
        private Color secondaryColor = Color.FromArgb(124, 252, 0);
        private Color userBubbleColor = Color.FromArgb(220, 248, 198);
        private Color botBubbleColor = Color.FromArgb(240, 240, 240);
        private Color textUserColor = Color.Black;
        private Color textBotColor = Color.Black;

        public Form1()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            // 1. SET UKURAN LANDSCAPE
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Text = "SiJabar Chatbot";

            SetupBackend();
            SetupUI(); // <--- Layout diperbaiki di sini
            LoadChatHistory();

            this.MouseDown += Form_MouseDown;
        }

        private void SetupBackend()
        {
            try
            {
                Env.Load();
                _mistralApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
                string mongoConn = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://localhost:27017";
                string dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "SiJabarDB";
                string collName = Environment.GetEnvironmentVariable("COLLECTION_NAME") ?? "ChatHistory";

                var mongoClient = new MongoClient(mongoConn);
                var database = mongoClient.GetDatabase(dbName);
                _chatCollection = database.GetCollection<ChatLog>(collName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat konfigurasi: " + ex.Message);
            }
        }

        private async void LoadChatHistory()
        {
            try
            {
                var history = await _chatCollection.Find(_ => true).ToListAsync();
                if (history.Count == 0)
                {
                    AddMessageToUI("Halo! Selamat datang di SiJabar Bot (Landscape Mode). 🌿", false);
                }
                else
                {
                    foreach (var log in history)
                    {
                        AddMessageToUI(log.Message, log.Role == "user");
                    }
                    ScrollToBottom();
                }
            }
            catch
            {
                AddMessageToUI("Halo! (Offline Mode - DB Error)", false);
            }
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // === BAGIAN PERBAIKAN LAYOUT ===
        private void SetupUI()
        {
            // PENTING: Urutan Add ke Controls menentukan Docking Priority.
            // Kita ingin Input dan Header mengambil tempat duluan.

            // 1. INPUT PANEL (Dock Bottom) -> Prioritas 1
            inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20)
            };
            this.Controls.Add(inputPanel);

            // 2. HEADER PANEL (Dock Top) -> Prioritas 2
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
            };
            headerPanel.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(headerPanel.ClientRectangle, primaryColor, secondaryColor, 0F))
                {
                    e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
                }
            };
            headerPanel.MouseDown += Form_MouseDown;
            this.Controls.Add(headerPanel);

            // 3. CHAT FLOW PANEL (Dock Fill) -> Prioritas Terakhir (Mengisi Sisa)
            // Karena ditambahkan terakhir, dia akan otomatis mengisi ruang ANTARA Header dan Input.
            chatFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(20, 20, 0, 20) // Margin agar teks tidak mepet
            };

            // Event Resize untuk Anti Scroll Horizontal
            chatFlowPanel.Resize += (s, e) =>
            {
                foreach (Control c in chatFlowPanel.Controls)
                {
                    // Lebar baris = Lebar Panel - Scrollbar - Sedikit Margin
                    c.Width = chatFlowPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10;
                }
            };
            this.Controls.Add(chatFlowPanel);

            // --- Z-ORDER FIX ---
            // Memastikan Header dan Input secara visual ada di atas (Top Layer)
            // Chat Panel di bawah (Bottom Layer) agar scrollbar nya tidak menutupi header
            headerPanel.BringToFront();
            inputPanel.BringToFront();
            chatFlowPanel.SendToBack();


            // === ISI KONTEN HEADER ===
            Label lblTitle = new Label
            {
                Text = "SiJabar Bot 🤖",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 20),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblStatus = new Label
            {
                Text = "Online • Mistral AI",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                AutoSize = true,
                Location = new Point(34, 55),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblStatus);

            btnClose = new Button
            {
                Text = "×",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 50),
                Location = new Point(this.Width - 60, 20),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right // Agar nempel kanan saat resize
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { Application.Exit(); };
            headerPanel.Controls.Add(btnClose);

            cmbModel = new ComboBox();
            cmbModel.Items.AddRange(new object[] { "mistral-tiny", "mistral-small", "mistral-medium" });
            cmbModel.SelectedIndex = 0;
            cmbModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbModel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            cmbModel.Size = new Size(160, 35);
            cmbModel.Location = new Point(this.Width - 230, 30);
            cmbModel.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Agar nempel kanan
            cmbModel.SelectedIndexChanged += (s, e) => {
                _selectedModel = cmbModel.SelectedItem.ToString();
            };
            headerPanel.Controls.Add(cmbModel);


            // === ISI KONTEN INPUT ===
            btnSend = new Button
            {
                Text = "➤",
                Font = new Font("Segoe UI", 14),
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(60, 60),
                Location = new Point(this.Width - 85, 15),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right // Agar nempel kanan
            };
            btnSend.FlatAppearance.BorderSize = 0;
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, 60, 60);
            btnSend.Region = new Region(path);
            btnSend.Click += BtnSend_Click;
            inputPanel.Controls.Add(btnSend);

            Panel txtWrapper = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(this.Width - 120, 50),
                Padding = new Padding(10, 12, 10, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Melar otomatis
            };
            txtWrapper.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, txtWrapper.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid); };
            inputPanel.Controls.Add(txtWrapper);

            txtInput = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Width = txtWrapper.Width - 20,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Dock = DockStyle.Fill
            };
            txtInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnSend_Click(s, e); e.SuppressKeyPress = true; } };
            txtWrapper.Controls.Add(txtInput);
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            // 1. Simpan & Tampilkan User
            await SaveToMongo("user", msg);
            AddMessageToUI(msg, true);

            txtInput.Clear();
            txtInput.Enabled = false;

            // 2. API Request
            try
            {
                string botReply = await GetMistralResponseAsync(msg);
                await SaveToMongo("bot", botReply);
                AddMessageToUI(botReply, false);
            }
            catch (Exception ex)
            {
                AddMessageToUI("Error: " + ex.Message, false);
            }
            finally
            {
                txtInput.Enabled = true;
                txtInput.Focus();
            }
        }

        private async Task SaveToMongo(string role, string message)
        {
            if (_chatCollection == null) return;
            var log = new ChatLog
            {
                Role = role,
                Message = message,
                Timestamp = DateTime.Now,
                ModelUsed = _selectedModel
            };
            await _chatCollection.InsertOneAsync(log);
        }

        private async Task<string> GetMistralResponseAsync(string userMessage)
        {
            if (string.IsNullOrEmpty(_mistralApiKey)) return "API Key belum diset di .env!";

            var requestData = new
            {
                model = _selectedModel,
                messages = new[] { new { role = "user", content = userMessage } }
            };

            string jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            if (!client.DefaultRequestHeaders.Contains("Authorization"))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mistralApiKey}");

            var response = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", content);
            string responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                return jsonResponse.choices[0].message.content;
            }
            return $"Error API ({_selectedModel}): {response.StatusCode}";
        }

        private void AddMessageToUI(string message, bool isUser)
        {
            // Hitung lebar agar tidak scroll horizontal
            int scrollWidth = SystemInformation.VerticalScrollBarWidth;
            int availableWidth = chatFlowPanel.ClientSize.Width - scrollWidth - 10;

            Panel rowPanel = new Panel
            {
                Width = availableWidth,
                Padding = new Padding(0, 5, 0, 5),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Batasi lebar bubble maks 65% dari layar agar enak dibaca di Landscape
            int bubbleMaxWidth = (int)(availableWidth * 0.65);

            Label lblBubble = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 11),
                ForeColor = isUser ? textUserColor : textBotColor,
                BackColor = isUser ? userBubbleColor : botBubbleColor,
                Padding = new Padding(15),
                AutoSize = true,
                MaximumSize = new Size(bubbleMaxWidth, 0)
            };

            lblBubble.Paint += (s, e) =>
            {
                Label l = s as Label;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int radius = 18;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radius, radius, 180, 90);
                    path.AddArc(l.Width - radius, 0, radius, radius, 270, 90);
                    path.AddArc(l.Width - radius, l.Height - radius, radius, radius, 0, 90);
                    path.AddArc(0, l.Height - radius, radius, radius, 90, 90);
                    path.CloseFigure();
                    using (SolidBrush brush = new SolidBrush(l.BackColor)) e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, l.Text, l.Font, l.ClientRectangle, l.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordBreak);
            };

            rowPanel.Controls.Add(lblBubble);

            if (isUser) lblBubble.Location = new Point(rowPanel.Width - lblBubble.PreferredWidth - 5, 0);
            else lblBubble.Location = new Point(5, 0);

            chatFlowPanel.Controls.Add(rowPanel);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer();
            scrollTimer.Interval = 50; // Sedikit delay agar rendering selesai
            scrollTimer.Tick += (s, ev) =>
            {
                scrollTimer.Stop();
                // Scroll ke posisi paling bawah (Y Maksimal)
                chatFlowPanel.AutoScrollPosition = new Point(0, chatFlowPanel.VerticalScroll.Maximum + 100);
            };
            scrollTimer.Start();
        }
    }
}