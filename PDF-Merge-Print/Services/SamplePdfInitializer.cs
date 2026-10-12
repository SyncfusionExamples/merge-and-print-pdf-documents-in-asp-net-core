using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

// Syncfusion drawing types are used explicitly below (Syncfusion.Drawing.PointF/RectangleF)
// to avoid ambiguity with System.Drawing.

namespace PDF_Merge_Print.Services
{
    /// <summary>
    /// On first run, generates the default sample PDFs in wwwroot/SamplePDFs
    /// if the folder is empty:
    ///   PDF1.pdf - A4 Portrait
    ///   PDF2.pdf - A4 Landscape
    ///   PDF3.pdf - A4 Portrait
    /// This guarantees the "merge with no uploads" workflow always has content.
    /// </summary>
    public static class SamplePdfInitializer
    {
        public static void GenerateIfNeeded(IWebHostEnvironment environment, ILogger logger)
        {
            var folder = Path.Combine(environment.WebRootPath, "SamplePDFs");
            Directory.CreateDirectory(folder);

            // Only generate when there are no sample PDFs present.
            if (Directory.GetFiles(folder, "*.pdf").Length > 0)
                return;

            logger.LogInformation("Generating default sample PDFs in {Folder}...", folder);

            CreateSamplePdf(Path.Combine(folder, "PDF1.pdf"), "PDF1 - Portrait Sample", true);
            CreateSamplePdf(Path.Combine(folder, "PDF2.pdf"), "PDF2 - Landscape Sample", false);
            CreateSamplePdf(Path.Combine(folder, "PDF3.pdf"), "PDF3 - Portrait Sample", true);

            logger.LogInformation("Default sample PDFs generated.");
        }

        /// <summary>
        /// Creates a simple multi-page sample PDF with visible content.
        /// </summary>
        private static void CreateSamplePdf(string path, string title, bool portrait)
        {
            using var document = new PdfDocument();

            // A4 size - 595 x 842 points. Landscape swaps width/height.
            document.PageSettings.Size = PdfPageSize.A4;
            document.PageSettings.Orientation = portrait
                ? PdfPageOrientation.Portrait
                : PdfPageOrientation.Landscape;

            // Create 3 pages so the print/merge workflow has visible variety.
            for (int i = 1; i <= 3; i++)
            {
                var page = document.Pages.Add();
                var bounds = page.GetClientSize();

                var font = new PdfStandardFont(PdfFontFamily.Helvetica, 20);
                var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

                page.Graphics.DrawString(title, font, PdfBrushes.DarkBlue,
                    new Syncfusion.Drawing.PointF(bounds.Width / 4, 40));

                page.Graphics.DrawString(
                    "Page " + i + " - orientation: " + (portrait ? "Portrait" : "Landscape") + ".\n" +
                    "This page was generated automatically as a default sample.\n" +
                    "Merge it with other PDFs using the buttons on the home page.",
                    smallFont, PdfBrushes.Black,
                    new Syncfusion.Drawing.PointF(bounds.Width / 5, 100));

                // Draw a table-like grid so the printed output is easy to verify.
                var grid = new PdfGrid();
                grid.Style.CellPadding = new PdfPaddings(5, 5, 5, 5);
                grid.Columns.Add(3);
                var header = grid.Headers.Add(1);
                header[0].Cells[0].Value = "Column A";
                header[0].Cells[1].Value = "Column B";
                header[0].Cells[2].Value = "Column C";
                for (int row = 1; row <= 5; row++)
                {
                    var dataRow = grid.Rows.Add();
                    dataRow.Cells[0].Value = "Row " + row + " / 1";
                    dataRow.Cells[1].Value = "Row " + row + " / 2";
                    dataRow.Cells[2].Value = "Row " + row + " / 3";
                }
                grid.Draw(page.Graphics, new Syncfusion.Drawing.RectangleF(bounds.Width / 4, 180,
                    bounds.Width / 2, bounds.Height - 280));
            }

            using (var stream = File.Create(path))
            {
                document.Save(stream);
            }
        }
    }
}
