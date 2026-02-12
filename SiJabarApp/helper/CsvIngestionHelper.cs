using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using SiJabarApp.model;

namespace SiJabarApp.helper
{
    public class CsvIngestionHelper
    {
        private SupabaseHelper _supaHelper;

        public CsvIngestionHelper()
        {
            _supaHelper = new SupabaseHelper();
        }

        public async Task ProcessOpenDataCsv(string filePath)
        {
            try 
            {
                var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                if (lines.Count < 2) return; // Butuh minimal header + 1 data

                // 1. Deteksi Delimiter (Cek baris pertama mana yang punya lebih banyak pemisah)
                char delimiter = ';';
                if (lines[0].Count(c => c == ',') > lines[0].Count(c => c == ';'))
                {
                    delimiter = ',';
                }

                // 2. Ambil Header (Baris pertama yang tidak kosong)
                var headers = lines[0].Split(delimiter).Select(h => h.Replace("\"", "").Trim()).ToArray();
                
                int countSuccess = 0;

                // 3. Iterasi Data (Mulai dari baris ke-2)
                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = lines[i].Split(delimiter);
                    if (cols.Length < 1) continue;

                    // Buat Narasi Fakta Universal: "Header1: Value1, Header2: Value2, ..."
                    var facts = new List<string>();
                    for (int j = 0; j < Math.Min(headers.Length, cols.Length); j++)
                    {
                        string val = cols[j].Replace("\"", "").Trim();
                        if (!string.IsNullOrEmpty(val))
                        {
                            facts.Add($"{headers[j]}: {val}");
                        }
                    }

                    if (facts.Count > 0)
                    {
                        string fullText = "Informasi Data CSV: " + string.Join(", ", facts);

                        // Embed & Simpan ke Knowledge Base RAG
                        float[] vector = await MistralHelper.GetEmbedding(fullText);
                        if (vector != null)
                        {
                            await _supaHelper.InsertDocumentAsync(fullText, "system_pdf", vector);
                            countSuccess++;
                        }

                        // Tambahkan Jeda agar tidak terkena Rate Limit API AI
                        await Task.Delay(500);
                    }
                }

                if (countSuccess == 0)
                {
                    throw new Exception("Tidak ada data valid yang ditemukan dalam file CSV tersebut.");
                }
                
                System.Diagnostics.Debug.WriteLine($"Selesai Ingest CSV Universal. Total: {countSuccess} baris.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal memproses CSV: {ex.Message}");
            }
        }
    }
}