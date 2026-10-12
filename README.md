# Merge and Print PDF Documents in ASP.NET Core Using Syncfusion .NET PDF Library

## Overview

This sample demonstrates how to build an ASP.NET Core web application that enables users to:

- Upload multiple PDF documents.
- Merge the uploaded PDF files into a single PDF document using the [Syncfusion .NET PDF Library](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-library).
- Download the merged PDF document.
- Print the merged PDF document using the [Syncfusion PDF-to-Image converter](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-to-image-converter) without using a PDF Viewer component.
- Support both portrait and landscape PDF documents.

## Syncfusion Products Used

This sample uses the following Syncfusion products:

| Product | Purpose | Documentation |
|---------|---------|---------------|
| [Syncfusion .NET PDF Library (Syncfusion.Pdf.Net.Core)](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-library) | Merging multiple PDF documents into a single PDF | [Merge PDF documents in C#](https://help.syncfusion.com/file-formats/pdf/working-with-multipage-documents#merge-pdf-documents) |
| [Syncfusion PDF-to-Image Converter (Syncfusion.PdfToImageConverter.Net.Core)](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-to-image-converter) | Converting merged PDF pages to images for printing | [Convert PDF to image in C#](https://help.syncfusion.com/document-processing/pdf/converting-pdf-to-image/convert-pdf-to-image-using-c-sharp) |

Useful resources:

- [Syncfusion .NET PDF Library – Product Overview](https://www.syncfusion.com/document-processing/pdf-framework/net)
- [Syncfusion NuGet Packages (nuget.org)](https://www.nuget.org/packages?q=Syncfusion.Pdf)
- [How to generate a Syncfusion license key](https://help.syncfusion.com/common/essential-studio/licensing/how-to-generate-license-key)
- [Syncfusion File Formats Blog](https://www.syncfusion.com/blogs/category/pdf)

## Features

- Multiple PDF upload support.
- PDF merging using Syncfusion .NET PDF Library.
- Download merged PDF.
- Print merged PDF without a PDF Viewer.
- Automatic fallback to sample PDFs.
- ASP.NET Core MVC implementation.

## Workflow

```text
PDF1 (Portrait)
        \
PDF2 (Landscape)
          \
PDF3 (Portrait)
            |
            v
       Merge PDFs
            |
            v
       Merged.pdf
            |
            v
    PdfToImageConverter
            |
            v
      Bitmap Images
            |
            v
      PrintDocument
            |
            v
          Printer
```

## Prerequisites

- Visual Studio 2022 or later
- .NET SDK (target framework: net10.0)
- Windows Operating System (required for printing support)
- Valid Syncfusion License Key

## Required NuGet Packages

```powershell
Install-Package Syncfusion.Pdf.Net.Core
Install-Package Syncfusion.PdfToImageConverter.Net.Core
```

## Project Structure

```text
PDF-Merge-Print
│
├── Controllers
│   └── HomeController.cs
├── Models
├── Services
│   ├── PdfMergeService.cs
│   └── PdfPrintService.cs
├── Views
│   └── Home
│       └── Index.cshtml
├── wwwroot
│   └── SamplePDFs
│       ├── PDF1.pdf
│       ├── PDF2.pdf
│       └── PDF3.pdf
├── Program.cs
└── PDF-Merge-Print.csproj
```

## Syncfusion License Registration

```csharp
using Syncfusion.Licensing;

SyncfusionLicenseProvider.RegisterLicense("YOUR LICENSE KEY");
```

## Integration Steps

### Step 1: Create ASP.NET Core MVC Project

```powershell
dotnet new mvc -n PDF-Merge-Print
```

### Step 2: Install Required Packages

```powershell
Install-Package Syncfusion.Pdf.Net.Core
Install-Package Syncfusion.PdfToImageConverter.Net.Core
```

### Step 3: Add Sample PDFs

Store sample PDFs under:

```text
wwwroot/SamplePDFs/
```

### Step 4: Register Services

```csharp
builder.Services.AddScoped<PdfMergeService>();
builder.Services.AddScoped<PdfPrintService>();
```

## Merge PDF Documents

```csharp
PdfDocumentBase.Merge(destinationDocument, sourceStreams);
```

The sample merges all uploaded PDFs into a single PDF document and creates `Merged.pdf`.

## Download Merged PDF

```csharp
public IActionResult MergeAndDownload()
{
    MemoryStream mergedDocument = _pdfMergeService.Merge(files);

    return File(
        mergedDocument.ToArray(),
        "application/pdf",
        "Merged.pdf");
}
```

## Convert PDF to Images

```csharp
PdfToImageConverter converter = new PdfToImageConverter(pdfDocument);

Image image = converter.Convert(pageIndex);
```

## Print Merged PDF

```csharp
PrintDocument printDocument = new PrintDocument();

printDocument.PrintPage += PrintPage;
printDocument.Print();
```

## User Interface

```text
------------------------------------------------
 Merge and Print PDF Documents
------------------------------------------------

Select PDF Files

[ Merge and Download ]
[ Merge and Print ]

If no PDFs are uploaded,
sample PDFs from wwwroot/SamplePDFs will be used.
------------------------------------------------
```

## Running the Sample

### Visual Studio

1. Open the solution.
2. Restore NuGet packages.
3. Register the Syncfusion license key.
4. Build and run the application.

### .NET CLI

```powershell
dotnet restore
dotnet build
dotnet run
```

## Test Scenarios

### Upload and Download

1. Upload multiple PDF files.
2. Click Merge and Download.
3. Verify the downloaded PDF.

### Upload and Print

1. Upload multiple PDF files.
2. Click Merge and Print.
3. Verify the printed output.

### No Upload Scenario

If no files are uploaded, PDFs from `wwwroot/SamplePDFs` are automatically merged and processed.

## Error Handling

```csharp
if (!file.FileName.EndsWith(".pdf"))
{
    throw new Exception("Only PDF files are supported.");
}
```

The sample validates:

- Invalid PDF files
- Empty uploads
- Corrupted PDFs
- Merge failures
- Printing failures

## Supported Platforms

- ASP.NET Core
- .NET 10
- Windows 10/11
- Windows Server

## Conclusion

This sample provides a complete workflow for uploading, merging, downloading, and printing PDF documents in ASP.NET Core using the [Syncfusion .NET PDF Library](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-library). Printing is achieved through the [Syncfusion PDF-to-Image Converter](https://www.syncfusion.com/document-processing/pdf-framework/net/pdf-to-image-converter) and `PrintDocument` without relying on a PDF Viewer component.
