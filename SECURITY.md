# Security Policy

## Supported versions

The latest release is the one that gets security fixes. Older ones do not.

| Version   | Supported |
|-----------|-----------|
| Latest    | ✅ Yes    |
| < Latest  | ❌ No     |

## Reporting a vulnerability

Please report a security issue privately, not through a public GitHub issue.

1. **Email**: edragoev@protonmail.com
2. **GitHub**: the "Report a vulnerability" button on
   https://github.com/edragoev1/pdfjet

What helps most:

- what the vulnerability is, and what it lets someone do
- the steps to reproduce it, with the file or the code that shows it
- which versions and which of the four ports it affects
- a suggested fix, if you have one

### Response

- **First reply**: within 48 hours
- **Where it stands**: within 7 days
- **A fix or a way around it**: within 30 days, depending on what it takes

### Disclosure

The report is acknowledged, the fix is written, a release goes out, and then
the vulnerability is described publicly, with credit to whoever reported it.

## What PDFjet reads

PDFjet writes PDFs, and most of what it does is writing, from data its own
program holds. The inputs worth thinking about are the ones it **reads**,
because those can come from somewhere else:

| What | Read by |
|---|---|
| A PDF | `PDF.read`, and the merging and splitting that follow it |
| A font | `Font` and the `.ttf`, `.otf` and `.ttf.stream` files it reads |
| An image | `Image`, `PNGImage`, `JPGImage`, `BMPImage`, `SVGImage` |
| Text with markup | `Markup` and `Markdown` |

These are what the hardening below is about. A PDF, a font or an image from
someone else is untrusted input, and PDFjet is written on that footing.

### The limits it holds to

- **256 MB** is the most anything decompresses to, whether it is Flate, LZW
  or run length. A small stream cannot expand without bound. A BMP image
  larger than that is refused before it is decoded rather than after.
- **16 MB** is the most a font's metrics may take.
- **32 levels** is as deep as the blocks of a Markdown text may nest. Deeper
  than that is drawn as the text it is.
- **`Markdown` reads an image only from the directory `setImageDirectory`
  names**: not an absolute path, not a URL, not a path with `..` in it, and
  only a file that is there. A Markdown text from someone else cannot make it
  read another file.
- A misuse of the API, and an input PDFjet will not read, end in an exception
  of PDFjet's own in Java, C# and Swift, and in an error the document records
  in Go — not in a crash, a hang, or a demand for memory without end.

### What PDFjet does not do

- **It opens no network connection.** None of the four ports has any
  networking code in it at all: nothing is fetched, no matter what a document,
  a font or a Markdown text names.
- **It runs nothing that a PDF carries.** There is no JavaScript engine and no
  action is performed; a PDF that is read is data.
- **Merging copies what the source document holds**, which can include
  JavaScript, embedded files, actions and form fields. Merging a document from
  someone else carries those into yours; PDFjet does not strip them.
- **It does not sanitize the text you draw.** What a program passes to
  PDFjet's components is written as it stands, escaped so that it cannot break
  the syntax of the PDF, but it is not inspected for anything else.

### How that is checked

- **Fuzzing.** The Go port carries ten fuzz targets — the PDF reader, the
  decompressor, the font streams, OpenType fonts, PNG, JPEG, BMP and SVG
  images, and the `Markup` and `Markdown` readers. Any input must leave them
  without a runtime error, in under 10 seconds, and inside 256 MB. Several of
  the fixes in the changelog came from them.
- **Real PDFs.** `tests/corpus` reads the pdf.js and veraPDF test corpora with
  each of the four ports, merges and splits every file, and holds the result
  against MuPDF. A crash, a file that takes too long, or a document MuPDF then
  has to repair, is a failure.
- **The four ports against each other.** `check-examples.sh` builds every
  example in Java, C#, Go and Swift and compares the PDFs, so that a fix in
  one port is a fix in all four and a difference is caught rather than
  shipped.

## Using PDFjet safely

- Treat a PDF, a font or an image that came from someone else as untrusted:
  read it where you can bound what it costs, with a memory limit and a
  timeout, as the fuzzing does.
- Point `Markdown.setImageDirectory` at a directory that holds only what you
  are willing to have drawn.
- Remember what merging carries over, and decide whether you want it.
- Encrypt a document that needs it: PDFjet writes AES-256, security revision
  6, and encrypts the metadata along with everything else.
- Keep to the latest release.

## Acknowledgements

With thanks to those who have reported security issues in PDFjet:

## Contact

- **Email**: edragoev@protonmail.com
- **GitHub**: https://github.com/edragoev1/pdfjet

**Please do not report security vulnerabilities through public GitHub
issues.**
