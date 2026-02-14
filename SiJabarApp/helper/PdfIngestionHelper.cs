using System;
using System.IO;
using System.Threading.Tasks;
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
                        var strategy = new LocationTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);

                        if (string.IsNullOrWhiteSpace(pageText)) continue;

                        pageText = pageText.Replace("\r", "").Replace("\n", " ").Trim();
                        string infoText = $"(PDF: {Path.GetFileName(filePath)}, Page {i}): {pageText}";

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
                throw new Exception($"PDF extraction failed: {ex.Message}");
            }
        }
    }
}
