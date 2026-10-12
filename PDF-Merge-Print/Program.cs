using PDF_Merge_Print.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Dependency registration
// ---------------------------------------------------------------------------
// Merge service — merges uploaded/default PDFs via Syncfusion PDF Library.
builder.Services.AddScoped<IPdfMergeService, PdfMergeService>();

// Print service — converts merged PDF pages to images via PdfToImageConverter
// and sends them to a printer through System.Drawing.Printing.PrintDocument.
builder.Services.AddScoped<IPdfPrintService, PdfPrintService>();

// MVC controllers.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Generate default sample PDFs (wwwroot/SamplePDFs/PDF1-3.pdf) on first run
// so the "merge with no uploads" workflow always has documents to use.
PDF_Merge_Print.Services.SamplePdfInitializer.GenerateIfNeeded(app.Environment, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serves wwwroot (including SamplePDFs).
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
