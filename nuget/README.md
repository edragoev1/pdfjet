# PDFjet

Fast, dependency-free PDF generation for .NET, and the same API in Java, Go
and Swift.

- **Accessible and archival PDFs in one setting:** PDF/UA-1 and PDF/A-1a,
  1b, 2a, 2b, 3a and 3b.
- **Text in Latin, Greek, Cyrillic, Chinese, Japanese, Korean, Arabic,
  Persian, Hebrew and Thai.** TrueType and OpenType fonts (.ttf and .otf),
  embedded as subsets of the glyphs a document draws.
- **Layout:** tables that break across pages and repeat their headers,
  BigTable for tables of any length, text blocks, charts, Markdown to PDF.
- **Barcodes:** EAN-13, UPC-A, Code 39, Code 128, QR, Data Matrix and
  PDF417.
- **Images:** PNG, JPEG and BMP, and SVG drawn as vector graphics.
- **Existing PDFs:** read, merged and split; AES-256 encryption with
  passwords and permissions.
- **Fast:** a 500-page document with an embedded font in under 100 ms.
- **Hardened:** everything that reads untrusted input, PDFs, images and
  fonts, is fuzzed.

For .NET 8 and later, and .NET Framework 4.8.

## Hello, World

```csharp
using System.IO;
using PDFjet.NET;

var pdf = new PDF(new BufferedStream(new FileStream("hello.pdf", FileMode.Create)));
var font = new Font(pdf, CoreFont.HELVETICA).SetSize(18f);
var page = new Page(pdf, Letter.PORTRAIT);
new TextLine(font, "Hello, World!").SetLocation(50f, 100f).DrawOn(page);
pdf.Complete();
```

The core fonts, such as Helvetica, need no files. For text in other scripts,
`PDFjet.dll` carries IBM Plex Sans and Serif, Source Serif 4, Cascadia Mono,
IBM Plex Sans Arabic, Hebrew, Thai, JP, KR, SC and TC, and IBM Plex Math, all
under the SIL Open Font License: their constants, such as
`IBMPlexSans.Regular`, need no files. Any other .ttf or .otf font is read by
its path; Noto Sans and the other fonts PDFjet came with before 9.1.7 are at
[pdfjet-fonts](https://github.com/edragoev1/pdfjet-fonts).

## More

- [The repository](https://github.com/edragoev1/pdfjet), with 50 examples
- [The documentation](https://edragoev1.github.io/pdfjet/)
- [The changelog](https://github.com/edragoev1/pdfjet/blob/main/CHANGELOG.md)
