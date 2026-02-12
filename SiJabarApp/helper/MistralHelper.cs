using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SiJabarApp.helper
{
    public static class MistralHelper
    {
        private static readonly HttpClient client = new HttpClient();
        private static string _apiKey = "iWEbEkPNiwBcb7c6KTFxbs2Mz5hbznNA"; // Ganti dengan API Key

        // 1. Fungsi Embedding (Teks -> Angka) dengan Retry Logic (Rate Limit)
        public static async Task<float[]> GetEmbedding(string text)
        {
            int maxRetries = 3;
            int delayMs = 1000;

            for (int i = 0; i < maxRetries; i++)
            {
                var payload = new
                {
                    model = "mistral-embed",
                    input = new[] { text }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                if (!client.DefaultRequestHeaders.Contains("Authorization"))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var res = await client.PostAsync("https://api.mistral.ai/v1/embeddings", content);

                if (res.IsSuccessStatusCode)
                {
                    var responseString = await res.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseString);
                    return result.data[0].embedding.ToObject<float[]>();
                }
                
                if ((int)res.StatusCode == 429) // Too Many Requests
                {
                    if (i == maxRetries - 1) throw new System.Exception("Mistral API Rate Limit tercapai setelah beberapa kali mencoba. Silakan tunggu beberapa saat.");
                    
                    System.Diagnostics.Debug.WriteLine($"Rate Limit hit. Retrying in {delayMs}ms... (Attempt {i+1})");
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                    continue;
                }

                string errorBody = await res.Content.ReadAsStringAsync();
                throw new System.Exception($"Mistral API Error ({res.StatusCode}): {errorBody}");
            }

            return null; // Should not happen
        }

        // 2. Fungsi Chat Completion (Tanya Jawab) - Single Turn (Backward Compatible)
        public static async Task<string> GetChatResponse(string systemPrompt, string userMessage)
        {
            var messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "role", "system" }, { "content", systemPrompt } },
                new Dictionary<string, string> { { "role", "user" }, { "content", userMessage } }
            };
            return await GetChatResponse(messages);
        }

        // 3. Fungsi Chat Completion Multi-Turn (Menerima Seluruh Riwayat Percakapan)
        public static async Task<string> GetChatResponse(List<Dictionary<string, string>> messages)
        {
            var payload = new
            {
                model = "mistral-tiny", // atau mistral-small
                messages = messages,
                temperature = 0.3
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("https://api.mistral.ai/v1/chat/completions", content);

            if (!res.IsSuccessStatusCode) return "Maaf, terjadi kesalahan pada AI.";

            dynamic result = JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
            return result.choices[0].message.content;
        }
    }
}