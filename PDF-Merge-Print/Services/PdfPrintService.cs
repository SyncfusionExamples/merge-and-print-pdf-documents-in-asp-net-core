using System.Drawing;
using System.Drawing.Printing;
using Syncfusion.PdfToImageConverter;

namespace PDF_Merge_Print.Services
{
    /// <summary>
    /// Service responsible for printing a PDF document:
    /// 1. Converts every PDF page into a bitmap image using Syncfusion's
    ///    PdfToImageConverter (https://support.syncfusion.com/kb/article/17292).
    /// 2. Sends the rendered images to a printer through
    ///    System.Drawing.Printing.PrintDocument, preserving each page's
    ///    original orientation and aspect ratio.
    /// 
    /// This service is designed for Windows environments (on-premises or Azure App Service
    /// Windows instances). It requires System.Drawing.Printing support which may not be
    /// available in containerized/Linux environments.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class PdfPrintService : IPdfPrintService
    {
        private readonly ILogger<PdfPrintService> _logger;
        private readonly string? _environmentInfo;

        public PdfPrintService(ILogger<PdfPrintService> logger)
        {
            _logger = logger;
            _environmentInfo = DetectEnvironment();
            if (!string.IsNullOrEmpty(_environmentInfo))
                _logger.LogInformation("Detected environment: {Environment}", _environmentInfo);
        }

        /// <summary>
        /// Detects the current runtime environment for diagnostic logging.
        /// </summary>
        private static string? DetectEnvironment()
        {
            try
            {
                if (Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") != null)
                    return "Azure App Service";
                if (Environment.GetEnvironmentVariable("CONTAINER_NAME") != null)
                    return "Docker Container";
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts every page of the merged PDF into a bitmap image.
        /// </summary>
        /// <param name="mergedPdfStream">Stream containing the merged PDF.</param>
        /// <returns>Ordered list of bitmaps — one per PDF page. Caller disposes them after printing.</returns>
        public List<Bitmap> ConvertPdfToImages(Stream mergedPdfStream)
        {
            mergedPdfStream.Position = 0;

            var images = new List<Bitmap>();

            // PdfToImageConverter renders each page of the PDF as an image
            // (see Syncfusion KB article 17292).
            var converter = new PdfToImageConverter();
            converter.Load(mergedPdfStream);

            // Convert ALL pages (first page .. last page) into image streams.
            // false, false -> keep default page size / no rotation flags
            // (see KB example: Convert(startIndex, endIndex, false, false)).
            Stream[] pageImageStreams =
                converter.Convert(0, converter.PageCount - 1, false, false);

            // Each converted page is returned as an encoded image stream;
            // decode it into a standalone Bitmap for printing.
            foreach (var pageStream in pageImageStreams)
            {
                using (pageStream)
                    images.Add(new Bitmap(pageStream));
            }

            _logger.LogInformation("Converted {PageCount} PDF pages into bitmap images.", images.Count);
            return images;
        }

        /// <summary>
        /// Prints the given page images using System.Drawing.Printing.PrintDocument.
        /// Each page is scaled to fit the printable area while maintaining its
        /// aspect ratio, and portrait/landscape orientation is chosen per page
        /// automatically based on the page dimensions.
        /// </summary>
        /// <param name="images">Bitmaps representing the PDF pages, in print order.</param>
        /// <param name="printerName">
        /// Target printer name. Pass null/empty to use the system default printer.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when no pages are provided.</exception>
        /// <exception cref="InvalidOperationException">Thrown when printer setup fails or print job cannot be sent.</exception>
        public void PrintPages(IReadOnlyList<Bitmap> images, string? printerName = null)
        {
            if (images == null || images.Count == 0)
                throw new ArgumentException("No pages provided to print.", nameof(images));

            int currentPageIndex = 0; // Shared state across PrintPage events.
            PrintDocument? printDocument = null;

            try
            {
                printDocument = new PrintDocument
                {
                    DocumentName = "Merged.pdf"
                };

                // Allow the caller to pick a specific printer; otherwise the
                // default printer is used automatically.
                if (!string.IsNullOrWhiteSpace(printerName))
                {
                    printDocument.PrinterSettings.PrinterName = printerName;
                }
                else
                {
                    _logger.LogInformation("No printer specified; using system default printer.");
                }

                // Validate printer availability before attempting to print
                if (!printDocument.PrinterSettings.IsValid)
                {
                    var errorMsg = string.IsNullOrWhiteSpace(printerName)
                        ? "No valid default printer is available."
                        : $"Printer '{printerName}' is not available or not valid.";

                    _logger.LogError("{Message} Environment: {Environment}", errorMsg, _environmentInfo ?? "Not detected");
                    throw new InvalidOperationException(
                        "Printing failed. Please make sure a printer is installed and accessible. " +
                        "Note: This service requires Windows OS with printer support (Azure App Service Windows or on-premises).",
                        new InvalidPrinterException(printDocument.PrinterSettings));
                }

                printDocument.PrintPage += (sender, e) =>
                {
                    try
                    {
                        var image = images[currentPageIndex];

                        // Choose orientation automatically per page so both portrait
                        // and landscape pages keep their original orientation.
                        bool isLandscape = image.Width > image.Height;
                        e.PageSettings.Landscape = isLandscape;

                        using var graphics = e.Graphics;

                        // Printable area of the current page in 1/100 inch units.
                        float printableWidth = e.MarginBounds.Width;
                        float printableHeight = e.MarginBounds.Height;

                        // Compute a scale factor that fits the entire image inside the
                        // printable area while maintaining the original aspect ratio.
                        float scale = Math.Min(printableWidth / image.Width, printableHeight / image.Height);
                        int drawWidth = (int)(image.Width * scale);
                        int drawHeight = (int)(image.Height * scale);

                        // Center the image on the page.
                        int x = e.MarginBounds.Left + (int)((printableWidth - drawWidth) / 2);
                        int y = e.MarginBounds.Top + (int)((printableHeight - drawHeight) / 2);

                        // Draw high-quality scaled image onto the printer's surface.
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawImage(image, x, y, drawWidth, drawHeight);

                        // Move to the next page; HasMorePages fires another PrintPage event.
                        currentPageIndex++;
                        e.HasMorePages = currentPageIndex < images.Count;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error rendering page {PageIndex} for printing.", currentPageIndex);
                        e.HasMorePages = false;
                        e.Cancel = true;
                        throw;
                    }
                };

                printDocument.Print();
                _logger.LogInformation("Successfully sent {PageCount} pages to printer '{Printer}'.",
                    images.Count, printDocument.PrinterSettings.PrinterName);
            }
            catch (InvalidPrinterException ex)
            {
                var errorMsg = "No valid printer is available.";
                _logger.LogError(ex, "{Message} Environment: {Environment}", errorMsg, _environmentInfo ?? "host");
                throw new InvalidOperationException(
                    "Printing failed. Please make sure a printer is installed and accessible. " +
                    "If running on Azure App Service, ensure you are using a Windows instance, not Linux or containers.",
                    ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error during printing. Printer may be offline or inaccessible.");
                throw new InvalidOperationException(
                    "Print failed due to I/O error. Printer may be offline or inaccessible.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied to printer. Check printer permissions.");
                throw new InvalidOperationException(
                    "Access denied to printer. Check printer permissions and app service identity.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during printing. Environment: {Environment}", _environmentInfo ?? "Unknown");
                throw new InvalidOperationException(
                    "An unexpected error occurred during printing. " +
                    "This service requires Windows OS with System.Drawing.Printing support.",
                    ex);
            }
            finally
            {
                printDocument?.Dispose();
            }
        }

        /// <summary>
        /// Convenience helper: converts a PDF to images and then prints them.
        /// Returns the number of pages printed for caller reference (e.g., UI feedback).
        /// </summary>
        /// <returns>The number of pages sent to the printer.</returns>
        public int PrintPdf(Stream mergedPdfStream, string? printerName = null)
        {
            var images = ConvertPdfToImages(mergedPdfStream);
            try
            {
                PrintPages(images, printerName);
                return images.Count;
            }
            finally
            {
                // Dispose bitmaps regardless of print outcome.
                foreach (var image in images)
                    image.Dispose();
            }
        }
    }

    /// <summary>
    /// Contract for PDF printing operations.
    /// </summary>
    public interface IPdfPrintService
    {
        /// <summary>
        /// Converts all pages of a PDF stream into bitmap images.
        /// </summary>
        List<Bitmap> ConvertPdfToImages(Stream mergedPdfStream);

        /// <summary>
        /// Prints bitmap images to the specified printer.
        /// </summary>
        void PrintPages(IReadOnlyList<Bitmap> images, string? printerName = null);

        /// <summary>
        /// Converts and prints a PDF in one operation.
        /// </summary>
        /// <returns>Number of pages sent to the printer.</returns>
        int PrintPdf(Stream mergedPdfStream, string? printerName = null);
    }
}
