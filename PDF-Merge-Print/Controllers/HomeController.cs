using Microsoft.AspNetCore.Mvc;
using PDF_Merge_Print.Services;
using System.Text;

namespace PDF_Merge_Print.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPdfMergeService _mergeService;
        private readonly IPdfPrintService _printService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IPdfMergeService mergeService,
            IPdfPrintService printService,
            IWebHostEnvironment environment,
            ILogger<HomeController> logger)
        {
            _mergeService = mergeService;
            _printService = printService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Landing page — shows the upload control and action buttons.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Merges the uploaded PDFs (or the default sample PDFs when nothing
        /// is uploaded) and returns the merged document as a download.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)] // 100 MB safety limit for uploads
        public IActionResult MergeAndDownload(List<IFormFile> files)
        {
            var sourceStreams = new List<Stream>();

            try
            {
                // Validate that only PDF files are accepted.
                var validationError = ValidatePdfFiles(files);
                if (validationError != null)
                {
                    TempData["Error"] = validationError;
                    return RedirectToAction("Index");
                }

                // Use uploaded files, or fall back to the default sample PDFs.
                var defaultPaths = GetDefaultSamplePaths();
                bool useDefaults = files == null || files.Count == 0;

                if (useDefaults)
                {
                    _logger.LogInformation("No files uploaded — using default sample PDFs.");
                    foreach (var path in defaultPaths)
                        sourceStreams.Add(System.IO.File.OpenRead(path));
                }
                else
                {
                    foreach (var file in files!)
                    {
                        // Copy each uploaded PDF into a memory stream so the
                        // request stream does not need to stay alive.
                        var memoryStream = new MemoryStream();
                        file.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        sourceStreams.Add(memoryStream);
                    }
                }

                // Merge everything into a single Merged.pdf.
                using var mergedStream = _mergeService.Merge(sourceStreams);

                var mergedBytes = mergedStream.ToArray();
                return File(mergedBytes, "application/pdf", "Merged.pdf");
            }
            catch (InvalidOperationException ex)
            {
                // Invalid PDF content raised by the merge service.
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while merging PDFs.");
                TempData["Error"] = "An unexpected error occurred while merging the documents.";
                return RedirectToAction("Index");
            }
            finally
            {
                // Dispose everything we opened/copied.
                foreach (var stream in sourceStreams)
                    stream.Dispose();
            }
        }

        /// <summary>
        /// Merges the PDFs and sends every merged page to the printer:
        /// merged PDF -> PdfToImageConverter -> Bitmaps -> PrintDocument -> printer.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public IActionResult MergeAndPrint(List<IFormFile> files, string? printerName)
        {
            var sourceStreams = new List<Stream>();

            try
            {
                var validationError = ValidatePdfFiles(files);
                if (validationError != null)
                {
                    TempData["Error"] = validationError;
                    return RedirectToAction("Index");
                }

                bool useDefaults = files == null || files.Count == 0;

                if (useDefaults)
                {
                    _logger.LogInformation("No files uploaded — using default sample PDFs.");
                    foreach (var path in GetDefaultSamplePaths())
                        sourceStreams.Add(System.IO.File.OpenRead(path));
                }
                else
                {
                    foreach (var file in files!)
                    {
                        var memoryStream = new MemoryStream();
                        file.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        sourceStreams.Add(memoryStream);
                    }
                }

                // Merge first.
                using var mergedStream = _mergeService.Merge(sourceStreams);

                // Then convert + print (PDF -> images -> PrintDocument -> printer).
                // PrintPdf returns the number of pages sent, eliminating the need to re-convert.
                int pageCount = _printService.PrintPdf(mergedStream, printerName);

                TempData["Success"] =
                    $"The merged document ({pageCount} pages) was sent to the printer.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while merging/printing PDFs.");
                TempData["Error"] = "An unexpected error occurred while printing the merged document.";
                return RedirectToAction("Index");
            }
            finally
            {
                foreach (var stream in sourceStreams)
                    stream.Dispose();
            }
        }

        /// <summary>
        /// Ensures only PDF files (by extension and content signature) are accepted.
        /// Returns an error message when validation fails, otherwise null.
        /// </summary>
        private static string? ValidatePdfFiles(List<IFormFile>? files)
        {
            if (files == null || files.Count == 0)
                return null; // Nothing uploaded is valid — defaults kick in.

            foreach (var file in files)
            {
                if (file.Length == 0)
                    return $"The file '{file.FileName}' is empty.";

                if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    return $"The file '{file.FileName}' is not a PDF. Only .pdf files can be uploaded.";

                // Verify the "%PDF-" magic bytes in addition to the extension.
                using var stream = file.OpenReadStream();
                var header = new byte[5];
                if (stream.Read(header, 0, header.Length) < header.Length ||
                    Encoding.ASCII.GetString(header) != "%PDF-")
                    return $"The file '{file.FileName}' is not a valid PDF document.";
            }

            return null;
        }

        /// <summary>
        /// Returns the full paths of the default sample PDFs in wwwroot/SamplePDFs.
        /// </summary>
        private List<string> GetDefaultSamplePaths()
        {
            var folder = Path.Combine(_environment.WebRootPath, "SamplePDFs");

            if (!Directory.Exists(folder))
                throw new InvalidOperationException(
                    "No PDFs were uploaded and the default sample folder (wwwroot/SamplePDFs) does not exist.");

            var paths = Directory.GetFiles(folder, "*.pdf")
                                 .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            if (paths.Count == 0)
                throw new InvalidOperationException(
                    "No PDFs were uploaded and no sample PDFs exist in wwwroot/SamplePDFs.");

            return paths;
        }
    }
}
