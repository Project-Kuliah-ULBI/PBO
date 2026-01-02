using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Driver;
using System.Collections.Generic;

namespace SiJabarApp
{
    public partial class Chatbot : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private IMongoCollection<ChatLog> _chatCollection;
        private string _apiKey = "iWEbEkPNiwBcb7c6KTFxbs2Mz5hbznNA";
        private string _selectedModel = "mistral-tiny";
        private string _currentUserId; // Menyimpan ID user yang login

        private Color colorUser = Color.FromArgb(220, 248, 198);
        private Color colorBot = Color.FromArgb(245, 245, 245);

        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private FlowLayoutPanel chatPanel;
        private TextBox txtInput;
        private ComboBox cmbModel;

        // PERBAIKAN: Tambahkan parameter userId di constructor
        public Chatbot(string userId)
        {
            this._currentUserId = userId;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(480, 600);
            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            InitDatabase();
            InitUI();

            this.Load += async (s, e) => await LoadHistory();
        }

        private void InitDatabase()
        {
            try
            {
                var mClient = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                _chatCollection = mClient.GetDatabase("SiJabarDB").GetCollection<ChatLog>("ChatHistory");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Koneksi Atlas Gagal: " + ex.Message);
            }
        }

        private void InitUI()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(39, 174, 96) };
            header.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); };

            Label title = new Label { Text = "SiJabar Assistant 🤖", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };

            // PERBAIKAN: Hanya hapus chat milik user yang login
            Button btnClear = new Button { Text = "🗑", FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Size = new Size(35, 35), Location = new Point(430, 15), Cursor = Cursors.Hand };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += async (s, e) => {
                if (_chatCollection != null)
                    await _chatCollection.DeleteManyAsync(c => c.UserId == _currentUserId);
                chatPanel.Controls.Clear();
            };

            cmbModel = new ComboBox
            {
                Location = new Point(285, 20),
                Width = 135,
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbModel.Items.AddRange(new object[] { "mistral-tiny", "mistral-small", "mistral-medium" });
            cmbModel.SelectedIndex = 0;
            cmbModel.SelectedIndexChanged += (s, e) => { _selectedModel = cmbModel.SelectedItem.ToString(); };

            header.Controls.AddRange(new Control[] { title, btnClear, cmbModel });
            this.Controls.Add(header);

            Panel pnlInput = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10) };
            txtInput = new TextBox { Width = 380, Font = new Font("Segoe UI", 11), BorderStyle = BorderStyle.FixedSingle, Location = new Point(15, 25) };
            txtInput.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) { SendAction(); e.SuppressKeyPress = true; }
            };

            Button btnSend = new Button { Text = "➤", BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(45, 45), Location = new Point(410, 15), Cursor = Cursors.Hand };
            btnSend.Click += (s, e) => SendAction();

            pnlInput.Controls.AddRange(new Control[] { txtInput, btnSend });
            this.Controls.Add(pnlInput);

            chatPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
            this.Controls.Add(chatPanel);
            chatPanel.BringToFront();
        }

        private async Task LoadHistory()
        {
            if (_chatCollection == null) return;
            // PERBAIKAN: Filter history berdasarkan UserId yang sedang aktif
            var logs = await _chatCollection.Find(c => c.UserId == _currentUserId)
                                           .SortBy(x => x.Timestamp)
                                           .ToListAsync();

            foreach (var log in logs) AddBubble(log.Message, log.Role == "user");
            ScrollBottom();
        }

        private async void SendAction()
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            txtInput.Clear();
            AddBubble(msg, true);
            await SaveMsg("user", msg);

            try
            {
                var response = await GetAIResponse(msg);
                AddBubble(response, false);
                await SaveMsg("bot", response);
            }
            catch { AddBubble("Maaf, koneksi terputus.", false); }
        }

        private async Task<string> GetAIResponse(string msg)
        {
            var payload = new { model = _selectedModel, messages = new[] { new { role = "user", content = msg } } };
            if (!client.DefaultRequestHeaders.Contains("Authorization"))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var res = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            dynamic data = JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
            return data.choices[0].message.content;
        }

        // PERBAIKAN: Simpan pesan beserta UserId pengirimnya
        private async Task SaveMsg(string role, string msg) =>
            await _chatCollection.InsertOneAsync(new ChatLog
            {
                UserId = _currentUserId,
                Role = role,
                Message = msg,
                Timestamp = DateTime.UtcNow
            });

        private void AddBubble(string text, bool isUser)
        {
            Panel row = new Panel { Width = chatPanel.ClientSize.Width - 30, AutoSize = true, Padding = new Padding(0, 5, 0, 5) };
            Label lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                BackColor = isUser ? colorUser : colorBot,
                Padding = new Padding(10),
                MaximumSize = new Size(300, 0),
                AutoSize = true
            };
            row.Controls.Add(lbl);
            lbl.Location = isUser ? new Point(row.Width - lbl.PreferredWidth - 10, 0) : new Point(10, 0);
            chatPanel.Controls.Add(row);
            ScrollBottom();
        }

        private void ScrollBottom()
        {
            this.BeginInvoke((MethodInvoker)delegate {
                chatPanel.AutoScrollPosition = new Point(0, chatPanel.VerticalScroll.Maximum);
                chatPanel.PerformLayout();
            });
        }
    }
}