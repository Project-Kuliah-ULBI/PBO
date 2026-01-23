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
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SiJabarApp
{
    public partial class Chatbot : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private IMongoCollection<ChatLog> _chatCollection;
        private IMongoCollection<SampahModel> _sampahCollection;
        private string _apiKey = "iWEbEkPNiwBcb7c6KTFxbs2Mz5hbznNA";
        private string _selectedModel = "mistral-tiny";
        private string _currentUserId;

        private Color colorUser = Color.FromArgb(220, 248, 198);
        private Color colorBot = Color.FromArgb(245, 245, 245);

        // Daftar kata kunci yang harus difilter
        private readonly HashSet<string> _forbiddenKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "python", "c#", "java", "javascript", "php", "html", "css", "sql", "database",
            "mongodb", "api", "endpoint", "function", "class", "method", "code", "script",
            "programming", "developer", "debug", "error handling", "exception", "namespace",
            "import", "include", "using", "namespace", "public", "private", "protected",
            "static", "void", "return", "if", "else", "for", "while", "foreach", "switch",
            "case", "break", "continue", "try", "catch", "finally", "throw", "new", "this",
            "base", "override", "virtual", "abstract", "interface", "implements", "extends",
            "package", "module", "library", "framework", "sdk", "ide", "compiler", "interpreter"
        };

        // Daftar kata kunci yang aman dan relevan
        private readonly HashSet<string> _allowedTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sampah", "limbah", "daur ulang", "pengelolaan", "lingkungan", "organik",
            "anorganik", "berbahaya", "residu", "penjemputan", "jadwal", "bank sampah",
            "pemilahan", "pengurangan", "pengomposan", "daur", "ulang", "reuse", "reduce",
            "recycle", "ecobrick", "kompos", "TPS", "TPA", "kebersihan", "kelestarian",
            "lingkungan hidup", "sustainable", "ekosistem", "polusi", "kontaminasi",
            "pengolahan", "pembuangan", "volume", "berat", "jenis", "kategori", "wilayah",
            "lokasi", "harga", "nilai", "manfaat", "dampak", "solusi", "tips", "edukasi",
            "kesadaran", "partisipasi", "komunitas", "program", "kebijakan", "aturan",
            "peraturan", "undang-undang", "pemerintah", "kota", "kabupaten", "desa",
            "kecamatan", "kelurahan", "rt", "rw", "tetangga", "warga", "masyarakat"
        };

        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private FlowLayoutPanel chatPanel;
        private TextBox txtInput;

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
                // Pastikan IP Address Anda sudah di-whitelist di MongoDB Atlas
                var mClient = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                var database = mClient.GetDatabase("SiJabarDB");
                _chatCollection = database.GetCollection<ChatLog>("ChatHistory");
                _sampahCollection = database.GetCollection<SampahModel>("Sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi Database Gagal: " + ex.Message);
            }
        }

        private void InitUI()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(39, 174, 96) };
            header.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); };

            Label title = new Label { Text = "SiJabar Waste Assistant ♻️", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };

            Button btnClear = new Button { Text = "🗑", FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Size = new Size(35, 35), Location = new Point(430, 15), Cursor = Cursors.Hand };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += async (s, e) => {
                if (_chatCollection != null)
                    await _chatCollection.DeleteManyAsync(c => c.UserId == _currentUserId);
                chatPanel.Controls.Clear();
            };

            header.Controls.AddRange(new Control[] { title, btnClear });
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
            try
            {
                if (_chatCollection == null) return;
                var logs = await _chatCollection.Find(c => c.UserId == _currentUserId)
                                               .SortBy(x => x.Timestamp)
                                               .ToListAsync();

                foreach (var log in logs) AddBubble(log.Message, log.Role == "user");
                ScrollBottom();
            }
            catch { /* Ignore loading error */ }
        }

        private async void SendAction()
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            txtInput.Clear();
            AddBubble(msg, true);

            try
            {
                await SaveMsg("user", msg);
                var response = await GetAIResponse(msg);

                // Filter dan validasi response sebelum ditampilkan
                response = FilterAndValidateResponse(response, msg);

                AddBubble(response, false);
                await SaveMsg("bot", response);
            }
            catch (Exception ex)
            {
                // Berikan respon default yang aman jika terjadi error
                string safeResponse = "Maaf, saya sedang mengalami gangguan koneksi. Silakan coba lagi nanti atau ajukan pertanyaan lain tentang pengelolaan sampah.";
                AddBubble(safeResponse, false);
                await SaveMsg("bot", safeResponse);
            }
        }

        private async Task<string> GetAIResponse(string msg)
        {
            // 1. Ambil 5 data sampah terbaru user agar prompt tidak terlalu panjang
            var userTrashData = await _sampahCollection.Find(s => s.UserId == _currentUserId)
                                                      .SortByDescending(s => s.Id)
                                                      .Limit(5)
                                                      .ToListAsync();

            StringBuilder dataContext = new StringBuilder("DATA USER DI SIJABAR:\n");
            if (userTrashData.Any())
            {
                foreach (var item in userTrashData)
                {
                    dataContext.AppendLine($"- Wilayah: {item.Wilayah}, Jenis: {item.Jenis}, Berat: {item.Berat}kg, Status: {item.Status}");
                }
            }
            else { dataContext.AppendLine("(Belum ada data input)."); }

            // 2. System Prompt yang SANGAT KETAT dengan multiple layer protection
            string systemPrompt =
                "PERAN: Anda adalah 'SiJabar Assistant', pakar pengelolaan sampah profesional.\n" +
                "TUGAS UTAMA: Memberikan informasi edukasi sampah, jadwal penjemputan, dan data aplikasi user.\n\n" +

                "ATURAN MUTLAK YANG TIDAK BOLEH DILANGGAR:\n" +
                "1. DILARANG KERAS memberikan kode pemrograman dalam BAHASA APA PUN (Python, C#, Java, dll).\n" +
                "2. JANGAN PERNAH menggunakan format markdown, backticks (```), atau blok kode.\n" +
                "3. TIDAK BOLEH membahas topik di luar pengelolaan sampah dan lingkungan.\n" +
                "4. Jika ditanya tentang coding, programming, atau hal teknis non-sampah, jawab: 'Maaf, saya adalah asisten khusus pengelolaan sampah. Saya hanya bisa membantu pertanyaan tentang sampah, daur ulang, jadwal penjemputan, dan lingkungan.'\n" +
                "5. JANGAN PERNAH menyebutkan teknologi backend, database, API, atau detail implementasi aplikasi.\n" +
                "6. FOKUS pada solusi praktis, edukasi, dan informasi yang berguna untuk pengguna.\n" +
                "7. Gunakan bahasa yang sederhana, mudah dipahami, dan berempati.\n\n" +

                "KONTEKS YANG WAJIB DIINGAT:\n" +
                "- Nama aplikasi: SiJabar (Sistem Informasi Jabar)\n" +
                "- Fungsi utama: Manajemen sampah rumah tangga\n" +
                "- Pengguna adalah warga biasa, bukan developer\n" +
                "- JADWAL PENJEMPUTAN TETAP: Senin & Kamis (Sampah Organik), Rabu (Sampah Anorganik)\n" +
                "- Bank Sampah: Setiap Sabtu di Balai Desa\n\n" +

                "DATA USER SAAT INI:\n" + dataContext.ToString() + "\n" +

                "CONTOH PERTANYAAN YANG RELEVAN:\n" +
                "- 'Kapan jadwal penjemputan sampah organik?'\n" +
                "- 'Bagaimana cara memilah sampah di rumah?'\n" +
                "- 'Berapa berat sampah saya bulan ini?'\n" +
                "- 'Apa manfaat daur ulang plastik?'\n" +
                "- 'Di mana lokasi bank sampah terdekat?'\n\n" +

                "CONTOH JAWABAN YANG BAIK:\n" +
                "'Untuk wilayah Anda, jadwal penjemputan sampah organik adalah hari Senin dan Kamis pagi. Pastikan sampah sudah dipilah dan ditaruh di depan rumah sebelum jam 7 pagi.'\n\n" +

                "INGAT: JAWABAN HARUS SINGKAT, INFORMATIF, DAN LANGSUNG KE POKOK MASALAH.";

            var payload = new
            {
                model = _selectedModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = msg }
                },
                temperature = 0.1, // Lebih rendah untuk mengurangi kreativitas yang tidak perlu
                max_tokens = 500,   // Batasi panjang respon
                top_p = 0.9         // Filter respon yang tidak relevan
            };

            if (!client.DefaultRequestHeaders.Contains("Authorization"))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", content);

            if (!res.IsSuccessStatusCode)
            {
                var errContent = await res.Content.ReadAsStringAsync();
                throw new Exception($"API Error ({res.StatusCode}): {errContent.Substring(0, Math.Min(errContent.Length, 200))}");
            }

            dynamic data = JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
            return data.choices[0].message.content;
        }

        private string FilterAndValidateResponse(string response, string userQuery)
        {
            if (string.IsNullOrWhiteSpace(response))
                return "Maaf, saya tidak bisa memberikan jawaban untuk pertanyaan Anda. Silakan ajukan pertanyaan lain tentang pengelolaan sampah.";

            // 1. Hapus format markdown dan kode
            response = Regex.Replace(response, @"```[\s\S]*?```", "", RegexOptions.Multiline);
            response = Regex.Replace(response, @"`[^`]+`", "", RegexOptions.Multiline);
            response = Regex.Replace(response, @"\*\*|\*|__|_", "", RegexOptions.Multiline);
            response = Regex.Replace(response, @"\<code\>[\s\S]*?\</code\>", "", RegexOptions.Multiline);
            response = Regex.Replace(response, @"\<pre\>[\s\S]*?\</pre\>", "", RegexOptions.Multiline);

            // 2. Periksa kata kunci terlarang
            foreach (var keyword in _forbiddenKeywords)
            {
                if (response.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return GenerateSafeFallbackResponse(userQuery);
                }
            }

            // 3. Periksa apakah respon mengandung kata kunci yang relevan
            bool isRelevant = false;
            foreach (var keyword in _allowedTopics)
            {
                if (response.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    userQuery.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isRelevant = true;
                    break;
                }
            }

            // 4. Jika tidak relevan, berikan fallback
            if (!isRelevant && !IsGeneralGreeting(userQuery))
            {
                return GenerateSafeFallbackResponse(userQuery);
            }

            // 5. Batasi panjang respon
            if (response.Length > 1000)
            {
                response = response.Substring(0, 997) + "...";
            }

            // 6. Bersihkan dari karakter aneh
            response = Regex.Replace(response, @"[^\w\s\p{P}\p{L}\p{N}.,!?;:()""'-]", " ", RegexOptions.Multiline);
            response = Regex.Replace(response, @"\s+", " ").Trim();

            return response;
        }

        private bool IsGeneralGreeting(string query)
        {
            string[] greetings = { "hai", "halo", "hello", "hi", "pagi", "siang", "sore", "malam", "assalamualaikum" };
            string lowerQuery = query.ToLower().Trim();

            return greetings.Any(g => lowerQuery.StartsWith(g) || lowerQuery.EndsWith(g) || lowerQuery.Contains($" {g} "));
        }

        private string GenerateSafeFallbackResponse(string userQuery)
        {
            string lowerQuery = userQuery.ToLower();

            if (lowerQuery.Contains("coding") || lowerQuery.Contains("program") || lowerQuery.Contains("code") ||
                lowerQuery.Contains("developer") || lowerQuery.Contains("script") || lowerQuery.Contains("bahasa pemrograman"))
            {
                return "Maaf, saya adalah asisten khusus pengelolaan sampah. Saya hanya bisa membantu pertanyaan tentang sampah, daur ulang, jadwal penjemputan, dan lingkungan.";
            }
            else if (lowerQuery.Contains("jadwal") || lowerQuery.Contains("kapan") || lowerQuery.Contains("penjemputan"))
            {
                return "Jadwal penjemputan sampah di SiJabar adalah:\n- Senin & Kamis: Sampah Organik\n- Rabu: Sampah Anorganik\n- Sabtu: Bank Sampah di Balai Desa";
            }
            else if (lowerQuery.Contains("cara") || lowerQuery.Contains("bagaimana") || lowerQuery.Contains("tips"))
            {
                return "Beberapa tips pengelolaan sampah:\n1. Pisahkan sampah organik dan anorganik\n2. Bersihkan sampah sebelum didaur ulang\n3. Komposkan sisa makanan di rumah\n4. Simpan sampah plastik secara rapi untuk bank sampah";
            }
            else
            {
                return "Saya hanya bisa membantu pertanyaan tentang pengelolaan sampah, daur ulang, jadwal penjemputan, dan lingkungan. Silakan ajukan pertanyaan dalam topik tersebut.";
            }
        }

        private async Task SaveMsg(string role, string msg)
        {
            if (_chatCollection == null) return;
            await _chatCollection.InsertOneAsync(new ChatLog
            {
                UserId = _currentUserId,
                Role = role,
                Message = msg,
                Timestamp = DateTime.UtcNow
            });
        }

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
                AutoSize = true,
                TextAlign = isUser ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
            };
            row.Controls.Add(lbl);
            lbl.Location = isUser ? new Point(row.Width - lbl.PreferredWidth - 10, 0) : new Point(10, 0);
            chatPanel.Controls.Add(row);
            ScrollBottom();
        }

        private void ScrollBottom()
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    chatPanel.AutoScrollPosition = new Point(0, chatPanel.VerticalScroll.Maximum);
                    chatPanel.PerformLayout();
                });
            }
        }
    }
}