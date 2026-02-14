using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using MongoDB.Driver;
using SiJabarApp.helper;
using SiJabarApp.model;

namespace SiJabarApp
{
    public partial class Chatbot : Form
    {
        private IMongoCollection<ChatLog> _chatCollection;
        private string _currentUserId;
        private List<Dictionary<string, string>> _chatHistory = new List<Dictionary<string, string>>();
        private const int MAX_HISTORY = 10;

        public Chatbot(string userId)
        {
            InitializeComponent();
            InitCustomUI();

            this._currentUserId = userId;
            this.Load += async (s, e) => await LoadHistory();

            InitMongoHistory();
        }

        private void InitMongoHistory()
        {
            try
            {
                var client = new MongoClient(MongoHelper.ConnectionString);
                var db = client.GetDatabase(MongoHelper.DatabaseName);
                _chatCollection = db.GetCollection<ChatLog>("ChatHistory");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to connect to Mongo History: " + ex.Message);
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
                    _chatHistory.Add(new Dictionary<string, string>
                    {
                        { "role", log.Role == "user" ? "user" : "assistant" },
                        { "content", log.Message }
                    });
                }

                if (_chatHistory.Count > MAX_HISTORY)
                    _chatHistory = _chatHistory.GetRange(_chatHistory.Count - MAX_HISTORY, MAX_HISTORY);

                ScrollBottom();
            }
            catch { }
        }

        private async void SendAction()
        {
            string msg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            AddBubble(msg, true);
            txtInput.Clear();

            await SaveLogToMongo("user", msg);
            _chatHistory.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", msg }
            });

            try
            {
                float[] queryVector = await MistralHelper.GetEmbedding(msg);

                var supaHelper = new SupabaseHelper();
                var relevantDocs = await supaHelper.SearchSimilarAsync(queryVector, _currentUserId);

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
                    contextSb.AppendLine("TIDAK ADA DATA RELEVAN ditemukan di database. Kamu WAJIB menolak menjawab dengan sopan.");
                }

                string systemPrompt =
                    "PERAN: Kamu adalah 'Asisten SiJabar', asisten cerdas untuk aplikasi pengelolaan sampah Jawa Barat.\n" +
                    "TUGAS: Jawab pertanyaan pengguna HANYA berdasarkan DATA PENDUKUNG yang disediakan di bawah.\n" +
                    "ATURAN KETAT:\n" +
                    "- Kamu HANYA menjawab berdasarkan DATA PENDUKUNG (FAKTA DARI DATABASE) di bawah.\n" +
                    "- JANGAN PERNAH mengarang, menebak, atau menjawab berdasarkan pengetahuan umum di luar data yang disediakan.\n" +
                    "- Jika DATA PENDUKUNG berisi informasi yang relevan, jawab dengan ramah, singkat, dan informatif berdasarkan data tersebut.\n" +
                    "- Jika TIDAK ADA DATA PENDUKUNG atau data tidak relevan dengan pertanyaan, jawab: 'Maaf, saya tidak memiliki data yang cukup untuk menjawab pertanyaan tersebut.'\n" +
                    "- Kamu MEMILIKI riwayat percakapan. Gunakan untuk menjawab pertanyaan lanjutan.\n" +
                    "- JANGAN memberikan kode sumber atau cuplikan pemrograman apa pun.\n\n" +
                    contextSb.ToString();

                var messages = new List<Dictionary<string, string>>();
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", systemPrompt }
                });

                int startIdx = Math.Max(0, _chatHistory.Count - MAX_HISTORY);
                for (int i = startIdx; i < _chatHistory.Count; i++)
                {
                    messages.Add(_chatHistory[i]);
                }

                string aiResponse = await MistralHelper.GetChatResponse(messages);

                AddBubble(aiResponse, false);
                await SaveLogToMongo("bot", aiResponse);

                _chatHistory.Add(new Dictionary<string, string>
                {
                    { "role", "assistant" },
                    { "content", aiResponse }
                });

                if (_chatHistory.Count > MAX_HISTORY)
                    _chatHistory = _chatHistory.GetRange(_chatHistory.Count - MAX_HISTORY, MAX_HISTORY);
            }
            catch (Exception ex)
            {
                AddBubble("Maaf, terjadi masalah koneksi dengan AI: " + ex.Message, false);
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
            _chatHistory.Clear();
        }
    }
}
