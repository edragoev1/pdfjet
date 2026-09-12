# PDF library for Java, C#, Swift and Go developers

This high performance library have no dependencies on external packages and should be usable on the widest variety of plaforms supported by the Java, C#, Swift and Go languages.


```
To build the Java version and compile and run all examples:

./build-java.sh


To build the C# version using .NET and compile and run all examples:

./build-dotnet.sh


To build the Go version and compile and run all examples:

./build-go.sh

## To use the Go library:
```bash
go get github.com/edragoev1/pdfjet/v9@latest

The module path carries the major version, as Go requires from v2 on, so
import the packages as:

import (
    pdfjet "github.com/edragoev1/pdfjet/v9/src"
    "github.com/edragoev1/pdfjet/v9/src/letter"
)


To build the Swift version and compile and run all examples:

./build-swift.sh


To compile and run specific Java example use the following command:

./run-java.sh 01


To compile and run specific C# example use one of the following:

./run-dotnet.sh 01


To compile and run specific Go example:

./run-go.sh 01


To compile and run specific Swift example:

./run-swift.sh 01

Make sure you install these first:
sudo apt install libc6-dev
sudo apt install gcc
```

## Builds

The `Build` GitHub Actions workflow runs `build-java.sh`, `build-dotnet.sh`,
`build-go.sh` and `build-swift.sh` on every push to `master` and on pull
requests, one job per port. A job fails if the library or an example does not
compile or compiles with a warning, `go vet` reports a problem in the Go port,
an example exits with an error, or an example does not create its PDF file. A
second job then checks the PDFs with `.github/scripts/check-example-pdfs.py`:
each example must render the same, with its text in the same fonts, sizes,
colors and positions and the same drawing instructions on every page, in the
C#, Go and Swift ports as in Java, and
each example that declares PDF/A or PDF/UA compliance must pass
[veraPDF](https://verapdf.org/) in all four ports.

To run the same checks locally before pushing, run `./check-examples.sh`. It
builds the four ports one after another in the repository folder and then
checks their PDFs, so it needs the four toolchains, Python 3 and veraPDF.

The `Windows` workflow runs the Windows scripts, `build-java.cmd`,
`build-dotnet.cmd`, `build-go.cmd` and `build-swift.cmd`, on a Windows runner
and checks that every example creates its PDF file. It only runs when started
from the Actions tab.

## Documentation

The API references and the example pages are published at
<https://edragoev1.github.io/pdfjet/>. The `Documentation` GitHub Actions
workflow rebuilds and publishes the site on every push to `master`, so the
generated HTML is not kept in git. The workflow also runs the Java examples and
publishes the PDF files they create, which the example pages link to. This
needs the repository's Pages source
(Settings > Pages) set to GitHub Actions.

To build them locally, `./generate-documentation.sh` runs Javadoc for the Java
port into `docs/java`, and [DocFX](https://dotnet.github.io/docfx/) for the C#
port into `docs/_net`. DocFX reads the XML doc comments (`/// <summary>`) in
`net/pdfjet`; its configuration is in `docfx/`. Install DocFX once with
`dotnet tool install -g docfx`. The Go reference is built with
[doc2go](https://abhinav.github.io/doc2go/) from the doc comments in `src` into
`docs/go`, leaving out the example programs in `src/examples`. The script runs
doc2go v0.12.2 with `go run`, so it needs no install. The Swift reference is built
with [DocC](https://www.swift.org/documentation/docc/), which comes with the
Swift toolchain, from the doc comments in `Sources/PDFjet` into `docs/swift`.
Its pages expect to be served from `/pdfjet/swift/`, so they do not work when
opened straight from disk.

## Java compatibility

The Java library and examples compile and run on Java 8 and later. With JDK 9
or newer, the Java build scripts pass `javac --release 8`, so `PDFjet.jar` and
the examples run on Java 8 whichever JDK builds them, and any API newer than
Java 8 is rejected. Java 8's `javac` has no `--release` option, so the scripts
leave it out there; that `javac` builds Java 8 class files anyway.

## Right to left text

`Bidi.reorderVisually` prepares a line of Hebrew, Arabic, Persian or Urdu text
for drawing. It puts the line in visual order, with left to right text such as
Latin words and numbers nested in it one level deep, and replaces the Arabic,
Persian and Urdu letters with their joined forms, including the lam-alef
ligature. `TextBlock.setRightToLeft(true)` wraps right to left text at the
width of the text block and prepares each line that way. Example_27 shows both.

The letters are shaped by replacing them with the presentation forms that fonts
map to Unicode. PDFjet does not use a font's OpenType substitution table (GSUB),
and uses only the mark positioning of its positioning table (GPOS), so:

- Urdu is drawn in the Naskh style of the Arabic fonts, not in Nastaliq, the
  style Urdu is usually printed in. Nastaliq fonts, such as Noto Nastaliq
  Urdu, depend on the OpenType tables, so they are not expected to work.
- Diacritics are positioned on their letters only in a font read from a `.otf`
  or `.ttf` file, as described in [Marks](#marks). In a font read from a
  `.otf.stream` or `.ttf.stream` file they are drawn where the font puts them
  by default: IBM Plex Sans Arabic draws every mark above its letter, and
  Hebrew niqqud falls between letters.
- The marks on a lam-alef ligature are put on its lam.
- A font's localized forms, such as the Urdu shapes of some digits, and its
  optional ligatures and kerning are not used.
- Only the letters of Arabic, Persian and Urdu are shaped. Letters used only by
  other languages written in Arabic script, such as Pashto, Sindhi or Kurdish,
  are drawn unjoined.
- Each string is laid out as a right to left line. The explicit embedding,
  override and isolate controls are left out, and text that is already shaped
  into presentation forms is not supported.

Right to left text is drawn in visual order, and PDF viewers put it back in
logical order when the text is copied or extracted. The joined letters map back
to the letters they stand for, so the words come out as typed, but:

- A bracket in right to left text is drawn with the glyph of its mirror image,
  and the viewers reverse the line without mirroring the brackets back. So
  `Bidi.reorderVisually` puts a right-to-left mark (U+200F) before each bracket
  it mirrors, and PDFjet draws the bracket in a marked content span that has
  the bracket that was typed as its ActualText, and does not draw the mark.
  Brackets around left to right text, as in `مرحبا (hello) عالم`, still come
  out the wrong way round. MuPDF can move a bracket at either end of a line to
  the other end, and Poppler puts a space before a closing bracket after a word
  whose last letter has a mark, as in `(كَتَبَ )`.
- The zero width non-joiner and joiner are not drawn, so they are not in the
  copied text: `می‌خواهم` comes out as `میخواهم`.
- Poppler separates an Arabic comma or a period from the word before it, as in
  `مادر ،`, and MuPDF moves numbers, such as `۱۴۰۳`, next to a word beside
  them.

Marking the lines with their text in logical order as ActualText does not help:
Poppler and MuPDF reverse that text as well.

## Text without spaces between words

`TextBlock` wraps text at its spaces. Thai, Lao, Khmer and Burmese text has no
spaces between its words, so put a zero width space (U+200B) between the words
where a line may break. It is not drawn. A word too wide for a line by itself
is broken between its characters, so text without zero width spaces still fits
in the text block, but its lines can break inside words. In right to left text
the letters on either side of such a break are not joined. The Thai text of
Example_27, `data/languages/thai.txt`, has a zero width space between its words.

## Marks

A mark, like a Hebrew or Arabic vowel mark or a Thai tone mark, is moved to
where the GPOS table of the font puts it: on its letter or ligature, or on the
mark it attaches to, like a Thai tone mark above an upper vowel or an Arabic
fatha above a shadda. The marks of a letter are stacked in the order HarfBuzz
puts them in, so the stacking does not depend on the order they were typed in.
Only a font read from a `.otf` or `.ttf` file has the table: a `.otf.stream` or
`.ttf.stream` file does not, so its marks are drawn where the font puts them by
default, and a mark on another mark is drawn on top of it. Example_27 reads its
Thai font from `fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf` for this
reason.

Text extraction tools take a mark that is moved up or down for text off the
line, and break the word at it. Each word with a moved mark is drawn in a
marked content span that has the text of the word as its ActualText, so Poppler
extracts the word whole. MuPDF 1.27 extracts the letters and marks in order,
but puts spaces inside some of these words.

## Encryption

`Encryption` encrypts a PDF with 256-bit AES, revision 6 of the standard
security handler of ISO 32000-2, with a user password, an owner password and
the permissions granted to the user. Example_30 shows it in all four ports. The
Java, C# and Go ports use the ciphers and hash functions of their platforms.
Swift has no cryptography in its standard library on Linux, and this library has
no dependencies on external packages, so the Swift port has its own AES, SHA-2,
MD5 and RC4 in `Sources/PDFjet/Cryptography.swift`, which its `Decryptor` uses
as well. The four ports write the same encryption dictionary, with random salts
in the password hashes.

A password is used as typed, in UTF-8, and at most 127 bytes of it are used,
which is where PDF readers cut it too. (The Poppler command line tools, such as
`pdftotext -upw`, take passwords of at most 32 characters; MuPDF and qpdf take
the full length.) ISO 32000-2 also asks for the SASLprep
normalization of the password, which none of the ports applies: Go has no
Unicode normalization without an external package, and the ports are kept the
same. Type passwords in precomposed (NFC) form, as keyboards produce them. A
password that is not set is empty, so a PDF whose user password is not set opens
without a prompt, with the permissions applied.

`PDF.read` also reads encrypted PDFs, made by PDFjet or by other producers:
RC4 and AES-128 (revisions 2 to 4 of the standard security handler) and AES-256
(revisions 5 and 6). A PDF that opens without a password is decrypted by the
plain `read`. For one that needs a password, pass its user or owner password to
`read(inputStream, password)` in Java and C#, `ReadWithPassword(buf, password)`
in Go, or `read(from:password:)` in Swift. A wrong password raises an error
that says so, and so does a missing one.

## Port differences

Public setters return the object they were called on, so calls can be chained.
The `Drawable` interface declares `drawOn` and `setLocation` in all four ports.
Java, C# and Swift let each class return its own type from `setLocation`. Go
does not: a Go type only satisfies an interface with an exact signature match,
so in Go the `SetLocation` of a `Drawable` type returns `Drawable`, and in a
chain of setter calls it goes last, right before `DrawOn`:

```go
image.ScaleBy(0.5).SetLocation(50.0, 50.0).DrawOn(page)
```

`DrawOn` returns the x and y coordinates of the bottom right corner of the
component, as a `float[]` in Java and C#, a `[Float]` in Swift and a
`[2]float32` in Go.
