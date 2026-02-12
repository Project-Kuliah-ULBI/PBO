using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace SiJabarApp.helper
{
    public class PdfIngestionHelper
    {
        private SupabaseHelper _supaHelper;

        public PdfIngestionHelper()
        {
            _supaHelper = new SupabaseHelper();
        }

        public async Task ProcessPdf(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    int pageCount = pdfDoc.GetNumberOfPages();
                    for (int i = 1; i <= pageCount; i++)
                    {
                        // Extract text from page
                        var strategy = new LocationTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);

                        if (string.IsNullOrWhiteSpace(pageText)) continue;

                        // Cleaning \r\n
                        pageText = pageText.Replace("\r", "").Replace("\n", " ").Trim();

                        // Chunking by Page (For simplicity, or split to smaller chunks if page is huge)
                        // If page text is very large (> 2000 chars), we might want to split it.
                        // For now, let's treat each page as a "document".
                        
                        string infoText = $"(PDF Source: {Path.GetFileName(filePath)}, Page {i}): {pageText}";

                        // Generate Embedding
                        float[] vector = await MistralHelper.GetEmbedding(infoText);
                        
                        if (vector != null)
                        {
                            await _supaHelper.InsertDocumentAsync(infoText, "system_pdf", vector);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal mengekstrak PDF: {ex.Message}");
            }
        }
    }
}
