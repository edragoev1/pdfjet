# PDFjet for Java

PDFjet creates PDF documents: text in any script, tables, charts, barcodes and
images, accessible and archival when you need it to be, with no dependencies.
This package has the library ready to use, built for Java 8 and later.

## What is in this package

| Path | What it is |
|---|---|
| `PDFjet.jar` | The library. Put it on the class path. |
| `docs/java/index.html` | The API reference. |
| `examples/` | 50 example programs. |
| `examples-java.html` | What each example shows, with links to its source and its PDF. |
| `Example_*.pdf` | The PDFs the examples create. |
| `build-java.sh`, `build-java.cmd` | Compile and run all the examples. |
| `run-java.sh`, `run-java.cmd` | Compile and run one example. |
| `resources/fonts/` | The fonts `PDFjet.jar` carries, as files, each family with its license. |
| `resources/data/`, `resources/images/`, `tests/data/PngSuite/` | The files the examples read; `resources/data/README.md` says where the data comes from. |
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

PDFjet carries IBM Plex Sans and Serif and Cascadia Mono, from Light or Thin
to Bold, upright and italic; IBM Plex Sans Arabic, Hebrew and Thai; IBM Plex
Math; and IBM Plex Sans JP, KR, SC and TC, for Chinese, Japanese and Korean.
They are in `resources/fonts` of this package, with their licenses, the CJK
ones in `resources/fonts/cjk`. Their constants, such as
`IBMPlexSans.Regular`, are paths like
`fonts/IBMPlexSans/IBMPlexSans-Regular.ttf`, which the `Font` constructor
reads from the file of that path if there is one, else from the folder
`resources/fonts` of the working directory, else from the classpath. The
`PDFjet.jar` of this package is small and does not carry them: keep
`resources/fonts` next to your program, or only the fonts you use. The jar
of Maven Central, `com.pdfjet:pdfjet`, carries them but the CJK ones, which
are the jar `com.pdfjet:pdfjet-fonts-cjk`, added next to it. A
font can also be read from any `.otf` or `.ttf` file by its path; the fonts
PDFjet came with before 9.1.7, Noto Sans among them, are at
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
./build-java.sh
./run-java.sh 01
```

On Windows, use `build-java.cmd` and `run-java.cmd 01`. The scripts compile the
examples against `PDFjet.jar` into `out/`, and each example writes its PDF to
this folder and reads its data from `resources/data`, by paths relative to
this folder.

## Documentation

Open `docs/java/index.html` for the API reference. The full guide, including
which class to use for text, right to left text, encryption, merging and
splitting documents, is at <https://github.com/edragoev1/pdfjet>, and the
references of all the ports are at <https://edragoev1.github.io/pdfjet/>.
