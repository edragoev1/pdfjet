# PDFjet

Fast, dependency-free PDF generation for .NET, and the same API in Java, Go
and Swift.

- **Accessible and archival PDFs in one setting:** PDF/UA-1 and PDF/A-1a,
  1b, 2a, 2b, 3a and 3b.
- **Text in Latin, Greek, Cyrillic, Chinese, Japanese, Korean, Arabic,
  Persian, Hebrew and Thai.** (Chinese, Japanese and Korean fonts:
  `PDFjet.Fonts.CJK`.) TrueType and OpenType fonts (.ttf and .otf),
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

> **For Chinese, Japanese or Korean, also add the package
> [PDFjet.Fonts.CJK](https://www.nuget.org/packages/PDFjet.Fonts.CJK):**
> `dotnet add package PDFjet.Fonts.CJK`, the same version as PDFjet. Without
> it, `IBMPlexSansJP`, `IBMPlexSansKR`, `IBMPlexSansSC` and `IBMPlexSansTC`
> throw an error that names that package.

The core fonts, such as Helvetica, need no files. For text in other scripts,
the `PDFjet.dll` of this package carries IBM Plex Sans and Serif, Cascadia
Mono, IBM Plex Sans Arabic, Hebrew and Thai, and IBM Plex Math, all under the
SIL Open Font License, with their licenses in the package: their constants,
such as `IBMPlexSans.Regular`, work out of the box, with nothing else to
download. The Chinese, Japanese and Korean fonts, IBM Plex Sans JP, KR, SC
and TC, about 40 MB, are in the package `PDFjet.Fonts.CJK`, which PDFjet
finds on its own when your project has it. Only the glyphs a document draws
are embedded in it. Any other .ttf or .otf font is read by its path.

The fonts make its `PDFjet.dll` larger, about 22 MB, and the package about
30 MB, as it has one for .NET 8 and one for .NET Framework 4.8. Want a much
smaller DLL? The `PDFjet.dll` of the
[releases on GitHub](https://github.com/edragoev1/pdfjet/releases) is under
1 MB: it reads the same fonts from the folder `resources/fonts`, which its
package has as files; keep that folder next to your program, or only the
fonts you use.

## More

- [The repository](https://github.com/edragoev1/pdfjet), with 50 examples
- [The documentation](https://edragoev1.github.io/pdfjet/)
- [The changelog](https://github.com/edragoev1/pdfjet/blob/main/CHANGELOG.md)
