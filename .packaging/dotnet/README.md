# PDFjet for .NET

PDFjet creates PDF documents: text in any script, tables, charts, barcodes and
images, accessible and archival when you need it to be, with no dependencies.
This package has the library ready to use, built for .NET 8 and later.

## What is in this package

| Path | What it is |
|---|---|
| `PDFjet.dll` | The library. Reference it from your project. |
| `docs/dotnet/index.html` | The API reference. |
| `examples/` | 51 example projects. |
| `examples-dotnet.html` | What each example shows, with links to its source and its PDF. |
| `Example_*.pdf` | The PDFs the examples create. |
| `build-dotnet.sh`, `build-dotnet.cmd` | Build and run all the examples. |
| `run-dotnet.sh`, `run-dotnet.cmd` | Build and run one example. |
| `fonts/` | The fonts, ready to embed, with their licenses. |
| `data/`, `images/`, `PngSuite/` | The files the examples read. |
| `CHANGELOG.md` | What changed in each release. |
| `LICENSE`, `THIRD-PARTIES.TXT` | The license of PDFjet and of the work it includes. |

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

Run it with `dotnet run` in a folder that has the `fonts` directory of this
package, as described below.

## Fonts

The bundled fonts, such as `IBMPlexSans.Regular`, are paths relative to the
working directory, like `fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream`. Run
your program in a folder that has the `fonts` directory, or copy the fonts you
use next to it and keep their paths. A font can also be read from any `.otf` or
`.ttf` file by its path. The 14 core PDF fonts, such as `CoreFont.HELVETICA`,
need no files.

The `.stream` files are compressed ahead of time, so they are embedded fast.
A `.stream` file has no GPOS table, so the marks of Thai, Hebrew and Arabic
text are placed on their letters only in a font read from the `.otf` or `.ttf`
file, as Example_27 does.

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
