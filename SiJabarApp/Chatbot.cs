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
                //var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                var client = new MongoClient("mongodb://localhost:27017");
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
                    AddBubble(log.Message, log.Role == "user");

                ScrollBottom();
            }
            catch { }
        }

        // ---------------------------------------------------------
        // LOGIKA UTAMA: SEND ACTION (RAG)
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
                    contextSb.AppendLine("Tidak ada data spesifik ditemukan di database. Jawab berdasarkan pengetahuan umum tentang pengelolaan sampah.");
                }

                // 5. Susun System Prompt
                string systemPrompt =
                    "PERAN: Anda adalah 'SiJabar Assistant', asisten cerdas aplikasi pengelolaan sampah Jawa Barat.\n" +
                    "TUGAS: Jawab pertanyaan user dengan ramah, singkat, dan informatif.\n" +
                    "ATURAN:\n" +
                    "- Gunakan DATA PENDUKUNG di bawah ini sebagai sumber kebenaran utama.\n" +
                    "- Jika data pendukung menjawab pertanyaan, gunakan informasi tersebut.\n" +
                    "- Jika tidak ada data pendukung, berikan saran umum tentang sampah.\n" +
                    "- JANGAN berikan kode program/coding.\n\n" +
                    contextSb.ToString();

                // 6. Minta Jawaban ke AI (Generation)
                string aiResponse = await MistralHelper.GetChatResponse(systemPrompt, msg);

                // 7. Tampilkan Jawaban AI
                AddBubble(aiResponse, false);

                // Simpan log bot ke MongoDB
                await SaveLogToMongo("bot", aiResponse);
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
                    Timestamp = DateTime.UtcNow
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
        }
    }
}