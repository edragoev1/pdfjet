# PDFjet for .NET

PDFjet creates PDF documents: text in any script, tables, charts, barcodes and
images, accessible and archival when you need it to be, with no dependencies.
This package has the library ready to use, built for .NET 8 and later, and
for .NET Framework 4.8.

## What is in this package

| Path | What it is |
|---|---|
| `PDFjet.dll` | The library, for .NET 8 and later. Reference it from your project. |
| `net48/` | The library for .NET Framework 4.8, `PDFjet.dll`, with the four DLLs of Microsoft it needs beside it. |
| `docs/dotnet/index.html` | The API reference. |
| `examples/` | 50 example projects. |
| `examples-dotnet.html` | What each example shows, with links to its source and its PDF. |
| `Example_*.pdf` | The PDFs the examples create. |
| `build-dotnet.sh`, `build-dotnet.cmd` | Build and run all the examples. |
| `run-dotnet.sh`, `run-dotnet.cmd` | Build and run one example. |
| `data/`, `images/`, `PngSuite/` | The files the examples read. |
| `CHANGELOG.md` | What changed in each release. |
| `LICENSE`, `THIRD-PARTIES.TXT` | The license of PDFjet and of the work it includes. |
| `*-license.txt`, `*-OFL.txt`, `*-LICENSE` | The licenses of the fonts `PDFjet.dll` carries, one for each family, such as `IBMPlexSans-license.txt`. |

## Quick start

Reference `PDFjet.dll` in the `.csproj` file of your project, with the path to
where you keep it:

```xml
<ItemGroup>
  <Reference Include="PDFjet">
    <HintPath>path/to/PDFjet.dll</HintPath>
  </Reference>
</ItemGroup>
```

Then write a PDF:

```csharp
using System.IO;
using PDFjet.NET;

public class Hello {
    public static void Main() {
        var pdf = new PDF(new BufferedStream(new FileStream("hello.pdf", FileMode.Create)));
        var font = new Font(pdf, IBMPlexSans.Regular).SetSize(18f);
        var page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Hello, World! Γειά σου, κόσμε! Здравей, свят!").SetLocation(50f, 100f).DrawOn(page);
        pdf.Complete();
    }
}
```

Run it with `dotnet run`: IBM Plex Sans is one of the fonts `PDFjet.dll`
carries, as described below.

## .NET Framework 4.8

For an application on .NET Framework 4.8, reference `net48/PDFjet.dll`
instead, and keep the four DLLs of Microsoft beside it, as your build copies
them to its output: `System.Memory.dll`, `System.Buffers.dll`,
`System.Numerics.Vectors.dll` and `System.Runtime.CompilerServices.Unsafe.dll`.
Or reference the NuGet package `System.Memory` 4.5.5, which brings the same.

Both are the same library, from the same sources, and both are strong-named
with the same identity:

    PDFjet, Version=9.0.0.0, Culture=neutral, PublicKeyToken=e66c1909913f295d

The assembly version stays 9.0.0.0 through 9.x, so that strong-named
assemblies that reference PDFjet.dll need no binding redirects for an update;
the file version is the release. In a multi-target NuGet package of your own,
put `net48/PDFjet.dll` (with its four DLLs) in `lib/net48/` and `PDFjet.dll` in
`lib/net8.0/`, which .NET 9 and .NET 10 use as well.

## Fonts

`PDFjet.dll` carries IBM Plex Sans and Serif, Source Serif 4 and Cascadia
Mono, from Light or Thin to Bold, upright and italic; IBM Plex Sans Arabic,
Hebrew and Thai; IBM Plex Math; and IBM Plex Sans JP, KR, SC and TC. Their
constants, such as `IBMPlexSans.Regular`, are paths like
`fonts/IBMPlexSans/IBMPlexSans-Regular.ttf`, which the `Font` constructor
reads from `PDFjet.dll` when there is no such file: they need no files. A font
can also be read from any `.otf` or `.ttf` file by its path; the fonts PDFjet
came with before 9.1.7, Noto Sans among them, are at
[pdfjet-fonts](https://github.com/edragoev1/pdfjet-fonts). The 14 core PDF
fonts, such as `CoreFont.HELVETICA`, need no files either.

Every font is embedded as a subset, only the glyphs a document draws: a page
of English embeds a few dozen KB of IBM Plex Sans, and a page of Chinese a
small part of IBM Plex Sans SC.

A PDF that embeds a font carries a copy of it, so its license has to allow
that. The fonts PDFjet carries are all under the SIL Open Font License, which lets
any document embed them; the fonts of fonts.google.com are under licenses
that allow it too. Use another font only when its license says so: the fonts
of Windows and macOS, such as Georgia or Tahoma, are licensed for that
computer, not for copying to a server to make PDFs there.

## Examples

Run all the examples, or one by its number, from this folder:

```bash
./build-dotnet.sh
./run-dotnet.sh 01
```

On Windows, use `build-dotnet.cmd` and `run-dotnet.cmd 01`. Each example
project references the `PDFjet.dll` of this folder, and each example writes its
PDF to this folder.

## Documentation

Open `docs/dotnet/index.html` for the API reference. The full guide, including
which class to use for text, right to left text, encryption, merging and
splitting documents, is at <https://github.com/edragoev1/pdfjet>, and the
references of all the ports are at <https://edragoev1.github.io/pdfjet/>.
