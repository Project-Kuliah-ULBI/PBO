using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
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
            // Baca semua baris file
            var lines = File.ReadAllLines(filePath);

            // ---------------------------------------------------------
            // BAGIAN 1: AMBIL METADATA (DEFINISI)
            // ---------------------------------------------------------
            // Mencari baris yang mengandung kata "Definisi"
            string definisiLine = lines.FirstOrDefault(l => l.StartsWith("\"Definisi\""));
            if (!string.IsNullOrEmpty(definisiLine))
            {
                // Format di CSV: "Definisi";":  [K01524] Kegiatan yang sistematis..."
                var parts = definisiLine.Split(new[] { "\";\"" }, StringSplitOptions.None);

                if (parts.Length > 1)
                {
                    string defContent = parts[1].Replace("\"", "").Replace(":", "").Trim();
                    string fullText = $"Informasi Regulasi Jabar: Definisi Pengelolaan Sampah adalah {defContent}";

                    // Embed & Simpan ke Supabase
                    float[] vector = await MistralHelper.GetEmbedding(fullText);
                    if (vector != null)
                    {
                        await _supaHelper.InsertDocumentAsync(fullText, "system", vector);
                        System.Diagnostics.Debug.WriteLine("Metadata Definisi tersimpan.");
                    }
                }
            }

            // ---------------------------------------------------------
            // BAGIAN 2: AMBIL DATA TABEL (STATISTIK)
            // ---------------------------------------------------------
            // Cari baris header tabel. Di file Anda header dimulai dengan "No";"Kode Wilayah"
            int startRow = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"Kode Wilayah\"") && lines[i].Contains("\"Wilayah\""))
                {
                    startRow = i + 1; // Data dimulai persis setelah header
                    break;
                }
            }

            if (startRow != -1)
            {
                for (int i = startRow; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Pecah CSV berdasarkan titik koma (;)
                    var cols = line.Split(';');

                    // Pastikan jumlah kolom cukup (minimal 5 kolom: No, Kode, Wilayah, Tahun, Volume)
                    if (cols.Length >= 5)
                    {
                        // Bersihkan tanda kutip "
                        string wilayah = cols[2].Replace("\"", "").Trim();
                        string tahun = cols[3].Replace("\"", "").Trim();
                        string volume = cols[4].Replace("\"", "").Trim();

                        // Buat kalimat narasi fakta
                        string statText = $"Statistik Open Data Jabar: Pada tahun {tahun}, volume sampah yang dikelola di {wilayah} tercatat sebanyak {volume} Ton/Tahun.";

                        // Embed & Simpan
                        float[] vec = await MistralHelper.GetEmbedding(statText);
                        if (vec != null)
                        {
                            await _supaHelper.InsertDocumentAsync(statText, "system", vec);
                            System.Diagnostics.Debug.WriteLine($"Data {wilayah} {tahun} tersimpan.");
                        }
                    }
                }
            }
        }
    }
}