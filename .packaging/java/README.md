# PDFjet for Java

PDFjet creates PDF documents: text in any script, tables, charts, barcodes and
images, accessible and archival when you need it to be, with no dependencies.
This package has the library ready to use, built for Java 8 and later.

## What is in this package

| Path | What it is |
|---|---|
| `PDFjet.jar` | The library. Put it on the class path. |
| `docs/java/index.html` | The API reference. |
| `examples/` | 57 example programs. |
| `examples-java.html` | What each example shows, with links to its source and its PDF. |
| `Example_*.pdf` | The PDFs the examples create. |
| `build-java.sh`, `build-java.cmd` | Compile and run all the examples. |
| `run-java.sh`, `run-java.cmd` | Compile and run one example. |
| `fonts/` | The fonts, ready to embed, with their licenses. |
| `data/`, `images/`, `PngSuite/` | The files the examples read. |
| `CHANGELOG.md` | What changed in each release. |
| `LICENSE`, `THIRD-PARTIES.TXT` | The license of PDFjet and of the work it includes. |

## Quick start

Save this as `Hello.java` in this folder:

```java
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import java.io.*;

public class Hello {
    public static void main(String[] args) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("hello.pdf")));
        Font font = new Font(pdf, IBMPlexSans.Regular).setSize(18f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Hello, World! Γειά σου, κόσμε! Здравей, свят!").setLocation(50f, 100f).drawOn(page);
        pdf.complete();
    }
}
```

Compile and run it with `PDFjet.jar` on the class path:

```bash
javac -encoding utf-8 -cp PDFjet.jar Hello.java
java -cp PDFjet.jar:. Hello
```

On Windows, separate the class path entries with `;` instead of `:`.

## Fonts

The bundled fonts, such as `IBMPlexSans.Regular`, are paths relative to the
working directory, like `fonts/IBMPlexSans/IBMPlexSans-Regular.ttf`. Run
your program in a folder that has the `fonts` directory, or copy the fonts you
use next to it and keep their paths. A font can also be read from any `.otf` or
`.ttf` file by its path. The 14 core PDF fonts, such as `CoreFont.HELVETICA`,
need no files.

Every font is embedded as a subset, only the glyphs a document draws: a page
of English embeds about 25 KB of Noto Sans, a page of Chinese about 93 KB of
Noto Sans SC's 6.5 MB.

A PDF that embeds a font carries a copy of it, so its license has to allow
that. The bundled fonts are all under the SIL Open Font License, which lets
any document embed them; the fonts of fonts.google.com are under licenses
that allow it too. Use another font only when its license says so: the fonts
of Windows and macOS, such as Georgia or Tahoma, are licensed for that
computer, not for copying to a server to make PDFs there.

## Examples

Run all the examples, or one by its number, from this folder:

```bash
./build-java.sh
./run-java.sh 01
```

On Windows, use `build-java.cmd` and `run-java.cmd 01`. The scripts compile the
examples against `PDFjet.jar` into `out/`, and each example writes its PDF to
this folder.

## Documentation

Open `docs/java/index.html` for the API reference. The full guide, including
which class to use for text, right to left text, encryption, merging and
splitting documents, is at <https://github.com/edragoev1/pdfjet>, and the
references of all the ports are at <https://edragoev1.github.io/pdfjet/>.
