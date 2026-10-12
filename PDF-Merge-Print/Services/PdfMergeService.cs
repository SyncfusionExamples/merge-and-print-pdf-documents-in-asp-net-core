using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace PDF_Merge_Print.Services
{
    /// <summary>
    /// Service responsible for merging multiple PDF documents into a single PDF
    /// using the Syncfusion .NET PDF Library.
    /// </summary>
    public class PdfMergeService : IPdfMergeService
    {
        private readonly ILogger<PdfMergeService> _logger;

        public PdfMergeService(ILogger<PdfMergeService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Merges the supplied PDF streams (in order) into a single PDF document
        /// and returns the merged document as a MemoryStream.
        /// </summary>
        /// <param name="sources">
        /// Ordered collection of PDF file streams. The caller owns these streams;
        /// they are not disposed here, so the caller can reuse or dispose them later.
        /// </param>
        /// <returns>A MemoryStream positioned at 0 containing the merged PDF.</returns>
        /// <exception cref="ArgumentException">Thrown when no valid source is supplied.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a source is not a valid PDF.</exception>
        public MemoryStream Merge(IEnumerable<Stream> sources)
        {
            // Materialize and validate the source list before doing any work.
            var sourceList = sources?.ToList() ?? throw new ArgumentNullException(nameof(sources));

            if (sourceList.Count == 0)
                throw new ArgumentException("At least one PDF document is required to merge.", nameof(sources));

            // Create the destination document. PdfDocumentBase.Merge appends
            // every source document (including pages, bookmarks, forms and
            // maintenance of original page size/orientation) into this document.
            using var destinationDocument = new PdfDocument();

            foreach (var stream in sourceList)
            {
                stream.Position = 0; // Always rewind before loading a stream as a document.

                try
                {
                    // Load the source document to validate it is a readable PDF.
                    // (Corrupt or non-PDF files throw here.)
                    using var sourceDocument = new PdfLoadedDocument(stream);

                    // Merge the loaded source document into the destination document.
                    PdfDocumentBase.Merge(destinationDocument, sourceDocument);
                }
                catch (Exception ex) when (ex is not ArgumentException)
                {
                    _logger.LogError(ex, "Failed to load/merge one of the source PDF documents.");
                    throw new InvalidOperationException(
                        "One of the files is not a valid PDF document. Please check the uploaded files.", ex);
                }
            }

            // Save the merged document to an in-memory stream that outlives
            // the destination document (Save closes the document but the
            // MemoryStream itself can still be read).
            var outputStream = new MemoryStream();
            destinationDocument.Save(outputStream);
            outputStream.Position = 0;

            _logger.LogInformation("Merged {Count} PDF documents. Output size: {Size} bytes.",
                sourceList.Count, outputStream.Length);

            return outputStream;
        }
    }

    /// <summary>
    /// Contract for merging PDF documents.
    /// </summary>
    public interface IPdfMergeService
    {
        MemoryStream Merge(IEnumerable<Stream> sources);
    }
}
