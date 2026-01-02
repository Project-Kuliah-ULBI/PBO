using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using MongoDB.Driver;

namespace SiJabarApp
{
    public partial class Chatbot : Form
    {
        // ===== KONFIGURASI API & DATABASE =====
        private static readonly HttpClient client = new HttpClient();
        private IMongoCollection<ChatLog> _chatCollection;
        private string _mistralApiKey;
        private string _selectedModel = "mistral-tiny";

        // ===== DRAG WINDOW DLL =====
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // ===== KOMPONEN UI =====
        private Panel headerPanel;
        private FlowLayoutPanel chatFlowPanel;
        private Panel inputPanel;
        private TextBox txtInput;
        private Button btnSend;
        private Button btnClose;
        private ComboBox cmbModel;

        // ===== WARNA TEMA =====
        private Color primaryColor = Color.FromArgb(40, 180, 99);
        private Color secondaryColor = Color.FromArgb(124, 252, 0);
        private Color userBubbleColor = Color.FromArgb(220, 248, 198);
        private Color botBubbleColor = Color.FromArgb(240, 240, 240);

        public Chatbot()
        {
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Text = "SiJabar Chatbot";

            SetupBackend();
            SetupUI();
            LoadChatHistory();

            MouseDown += Form_MouseDown;
        }

        // ===== BACKEND (TANPA ENV) =====
        private void SetupBackend()
        {
            try
            {
                _mistralApiKey = ConfigurationManager.AppSettings["MISTRAL_API_KEY"];
                string mongoConn = ConfigurationManager.AppSettings["mongodb://127.0.0.1:27017"];
                string dbName = ConfigurationManager.AppSettings["SiJabarDB"];
                string collName = ConfigurationManager.AppSettings["ChatHistory"];

                if (string.IsNullOrEmpty(_mistralApiKey))
                    MessageBox.Show("MISTRAL_API_KEY belum diatur di App.config!");

                var mongoClient = new MongoClient(mongoConn);
                var database = mongoClient.GetDatabase(dbName);
                _chatCollection = database.GetCollection<ChatLog>(collName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat konfigurasi: " + ex.Message);
            }
        }

        // ===== LOAD HISTORY =====
        private async void LoadChatHistory()
        {
            try
            {
                var history = await _chatCollection.Find(_ => true).ToListAsync();
                if (history.Count == 0)
                {
                    AddMessageToUI("Halo! Selamat datang di SiJabar Bot 🤖", false);
                }
                else
                {
                    foreach (var chat in history)
                        AddMessageToUI(chat.Message, chat.Role == "user");
                }
                ScrollToBottom();
            }
            catch
            {
                AddMessageToUI("Mode Offline - Database tidak tersedia", false);
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

        // ===== UI =====
        private void SetupUI()
        {
            inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20)
            };
            Controls.Add(inputPanel);

            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90
            };
            headerPanel.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush =
                    new LinearGradientBrush(
                        headerPanel.ClientRectangle,
                        primaryColor,
                        secondaryColor,
                        LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
                }
            };

            headerPanel.MouseDown += Form_MouseDown;
            Controls.Add(headerPanel);

            chatFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20)
            };
            Controls.Add(chatFlowPanel);

            // ===== HEADER =====
            Label lblTitle = new Label
            {
                Text = "SiJabar Bot 🤖",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 25),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblTitle);

            btnClose = new Button
            {
                Text = "×",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 50),
                Location = new Point(Width - 60, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();
            headerPanel.Controls.Add(btnClose);

            cmbModel = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Size = new Size(160, 35),
                Location = new Point(Width - 230, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbModel.Items.AddRange(new[] { "mistral-tiny", "mistral-small", "mistral-medium" });
            cmbModel.SelectedIndex = 0;
            cmbModel.SelectedIndexChanged += (s, e) => _selectedModel = cmbModel.Text;
            headerPanel.Controls.Add(cmbModel);

            // ===== INPUT =====
            btnSend = new Button
            {
                Text = "➤",
                Font = new Font("Segoe UI", 14),
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(60, 60),
                Location = new Point(Width - 85, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;
            inputPanel.Controls.Add(btnSend);

            txtInput = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                Width = Width - 140,
                Location = new Point(20, 25),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnSend_Click(s, e);
                    e.SuppressKeyPress = true;
                }
            };
            inputPanel.Controls.Add(txtInput);
        }

        // ===== SEND =====
        private async void BtnSend_Click(object sender, EventArgs e)
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            txtInput.Clear();
            txtInput.Enabled = false;

            await SaveToMongo("user", msg);
            AddMessageToUI(msg, true);

            try
            {
                string reply = await GetMistralResponseAsync(msg);
                await SaveToMongo("bot", reply);
                AddMessageToUI(reply, false);
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

        // ===== API =====
        private async Task<string> GetMistralResponseAsync(string message)
        {
            var data = new
            {
                model = _selectedModel,
                messages = new[] { new { role = "user", content = message } }
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!client.DefaultRequestHeaders.Contains("Authorization"))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mistralApiKey}");

            var response = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"API Error: {response.StatusCode}";

            dynamic res = JsonConvert.DeserializeObject(result);
            return res.choices[0].message.content;
        }

        // ===== DATABASE =====
        private async Task SaveToMongo(string role, string message)
        {
            if (_chatCollection == null) return;

            await _chatCollection.InsertOneAsync(new ChatLog
            {
                Role = role,
                Message = message,
                Timestamp = DateTime.Now,
                ModelUsed = _selectedModel
            });
        }

        // ===== UI MESSAGE =====
        private void AddMessageToUI(string message, bool isUser)
        {
            Label lbl = new Label
            {
                Text = message,
                AutoSize = true,
                MaximumSize = new Size(chatFlowPanel.Width * 65 / 100, 0),
                BackColor = isUser ? userBubbleColor : botBubbleColor,
                Padding = new Padding(10),
                Font = new Font("Segoe UI", 11)
            };

            Panel row = new Panel
            {
                Width = chatFlowPanel.Width - 25,
                AutoSize = true
            };

            lbl.Location = isUser
                ? new Point(row.Width - lbl.PreferredWidth - 10, 5)
                : new Point(10, 5);

            row.Controls.Add(lbl);
            chatFlowPanel.Controls.Add(row);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            chatFlowPanel.VerticalScroll.Value = chatFlowPanel.VerticalScroll.Maximum;
        }
    }
}
