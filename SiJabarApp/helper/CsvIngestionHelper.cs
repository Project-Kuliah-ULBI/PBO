using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
                if (lines.Count < 2) return;

                char delimiter = ';';
                if (lines[0].Count(c => c == ',') > lines[0].Count(c => c == ';'))
                {
                    delimiter = ',';
                }

                var headers = lines[0].Split(delimiter).Select(h => h.Replace("\"", "").Trim()).ToArray();
                int countSuccess = 0;

                for (int i = 1; i < lines.Count; i++)
                {
                    var cols = lines[i].Split(delimiter);
                    if (cols.Length < 1) continue;

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
                        string fullText = "CSV Data: " + string.Join(", ", facts);

                        float[] vector = await MistralHelper.GetEmbedding(fullText);
                        if (vector != null)
                        {
                            await _supaHelper.InsertDocumentAsync(fullText, "system_pdf", vector);
                            countSuccess++;
                        }

                        await Task.Delay(500);
                    }
                }

                if (countSuccess == 0)
                {
                    throw new Exception("No valid data found in the CSV file.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"CSV processing failed: {ex.Message}");
            }
        }
    }
}
