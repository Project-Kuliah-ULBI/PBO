using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using MongoDB.Driver;
using SiJabarApp.helper; // PENTING: Gunakan namespace helper
using SiJabarApp.model;  // PENTING: Gunakan namespace model

namespace SiJabarApp
{
    public partial class Chatbot : Form
    {
        // Variabel Database Dashboard (MongoDB) - Masih dipakai untuk clear history
        private IMongoCollection<ChatLog> _chatCollection;
        private string _currentUserId;

        // --- RIWAYAT PERCAKAPAN IN-MEMORY (MULTI-TURN CONTEXT) ---
        private List<Dictionary<string, string>> _chatHistory = new List<Dictionary<string, string>>();
        private const int MAX_HISTORY = 10; // Maksimal 10 pesan terakhir yang dikirim ke AI

        public Chatbot(string userId)
        {
            // Panggil method dari Designer
            InitializeComponent();
            InitCustomUI(); // Setup UI tambahan (Header, Rounded, dll)

            this._currentUserId = userId;
            this.Load += async (s, e) => await LoadHistory();

            // Setup Database MongoDB (Hanya untuk History Chat Log, bukan RAG)
            InitMongoHistory();
        }

        private void InitMongoHistory()
        {
            try
            {
                var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                //var client = new MongoClient("mongodb://localhost:27017");
                var db = client.GetDatabase("SiJabarDB");
                _chatCollection = db.GetCollection<ChatLog>("ChatHistory");
            }
            catch (Exception ex)
            {
                // Silent fail untuk chat history, tidak fatal
                System.Diagnostics.Debug.WriteLine("Gagal koneksi Mongo History: " + ex.Message);
            }
        }

        private async Task LoadHistory()
        {
            if (_chatCollection == null) return;
            try
            {
                var logs = await _chatCollection.Find(c => c.UserId == _currentUserId)
                                               .SortBy(x => x.Timestamp)
                                               .ToListAsync();

                foreach (var log in logs)
                {
                    AddBubble(log.Message, log.Role == "user");

                    // Muat juga ke _chatHistory agar AI ingat percakapan sebelumnya
                    _chatHistory.Add(new Dictionary<string, string>
                    {
                        { "role", log.Role == "user" ? "user" : "assistant" },
                        { "content", log.Message }
                    });
                }

                // Batasi hanya simpan pesan terakhir
                if (_chatHistory.Count > MAX_HISTORY)
                    _chatHistory = _chatHistory.GetRange(_chatHistory.Count - MAX_HISTORY, MAX_HISTORY);

                ScrollBottom();
            }
            catch { }
        }

        // ---------------------------------------------------------
        // LOGIKA UTAMA: SEND ACTION (RAG + MULTI-TURN CONTEXT)
        // ---------------------------------------------------------
        private async void SendAction()
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            // 1. Tampilkan Chat User
            AddBubble(msg, true);
            txtInput.Clear();

            // Simpan log user ke MongoDB (History)
            await SaveLogToMongo("user", msg);

            // Tambahkan ke riwayat in-memory
            _chatHistory.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", msg }
            });

            try
            {
                // 2. Generate Vector Pertanyaan (Mistral API)
                float[] queryVector = await MistralHelper.GetEmbedding(msg);

                // 3. Cari Data Relevan di Supabase (RAG Retrieval)
                var supaHelper = new SupabaseHelper();
                var relevantDocs = await supaHelper.SearchSimilarAsync(queryVector, _currentUserId);

                // 4. Susun Context
                StringBuilder contextSb = new StringBuilder();
                if (relevantDocs.Count > 0)
                {
                    contextSb.AppendLine("DATA PENDUKUNG (FAKTA DARI DATABASE):");
                    foreach (var doc in relevantDocs)
                    {
                        contextSb.AppendLine("- " + doc);
                    }
                }
                else
                {
                    contextSb.AppendLine("TIDAK ADA DATA RELEVAN ditemukan di database. Anda WAJIB menolak menjawab dengan sopan.");
                }

                // 5. Susun System Prompt
                string systemPrompt =
                    "PERAN: Anda adalah 'SiJabar Assistant', asisten cerdas aplikasi pengelolaan sampah Jawa Barat.\n" +
                    "TUGAS: Jawab pertanyaan user HANYA berdasarkan DATA PENDUKUNG yang disertakan di bawah.\n" +
                    "ATURAN KETAT:\n" +
                    "- Anda HANYA boleh menjawab berdasarkan DATA PENDUKUNG (FAKTA DARI DATABASE) di bawah ini.\n" +
                    "- JANGAN PERNAH mengarang, mengira-ngira, atau menjawab berdasarkan pengetahuan umum di luar data yang tersedia.\n" +
                    "- Jika DATA PENDUKUNG berisi informasi yang relevan, jawab dengan ramah, singkat, dan informatif berdasarkan data tersebut.\n" +
                    "- Jika TIDAK ADA DATA PENDUKUNG atau data tidak relevan dengan pertanyaan, jawab: 'Maaf, saya belum memiliki data untuk menjawab pertanyaan tersebut.'\n" +
                    "- Anda MEMILIKI riwayat percakapan. Gunakan riwayat tersebut untuk menjawab pertanyaan lanjutan.\n" +
                    "- JANGAN berikan kode program/coding.\n\n" +
                    contextSb.ToString();

                // 6. BANGUN MESSAGES ARRAY (MULTI-TURN)
                var messages = new List<Dictionary<string, string>>();

                // System prompt selalu di awal
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", systemPrompt }
                });

                // Ambil riwayat percakapan terakhir (batasi agar tidak melebihi token limit)
                int startIdx = Math.Max(0, _chatHistory.Count - MAX_HISTORY);
                for (int i = startIdx; i < _chatHistory.Count; i++)
                {
                    messages.Add(_chatHistory[i]);
                }

                // 7. Minta Jawaban ke AI (Generation) dengan FULL CONTEXT
                string aiResponse = await MistralHelper.GetChatResponse(messages);

                // 8. Tampilkan Jawaban AI
                AddBubble(aiResponse, false);

                // Simpan log bot ke MongoDB
                await SaveLogToMongo("bot", aiResponse);

                // Tambahkan jawaban AI ke riwayat in-memory
                _chatHistory.Add(new Dictionary<string, string>
                {
                    { "role", "assistant" },
                    { "content", aiResponse }
                });

                // Batasi ukuran riwayat
                if (_chatHistory.Count > MAX_HISTORY)
                    _chatHistory = _chatHistory.GetRange(_chatHistory.Count - MAX_HISTORY, MAX_HISTORY);
            }
            catch (Exception ex)
            {
                AddBubble("Maaf, terjadi gangguan koneksi ke AI: " + ex.Message, false);
            }
        }

        private async Task SaveLogToMongo(string role, string msg)
        {
            if (_chatCollection != null)
            {
                await _chatCollection.InsertOneAsync(new ChatLog
                {
                    UserId = _currentUserId,
                    Role = role,
                    Message = msg,
                    Timestamp = DateTime.Now
                });
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (_chatCollection != null)
            {
                _chatCollection.DeleteManyAsync(c => c.UserId == _currentUserId);
            }
            chatPanel.Controls.Clear();
            _chatHistory.Clear(); // Reset context AI
        }
    }
}