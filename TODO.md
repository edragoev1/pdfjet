# PDFjet — the plan

The library work that is open, in the four ports (Java, C#, Go and Swift),
and the checks that hold before each release. What each release brought is in
CHANGELOG.md; the work that is done is in the history of this file.

The library takes fixes and what belongs to writing a correct, compact PDF;
new features go to PDFjet Pro.

## The numbers

Releases are odd: 9.1.5, 9.1.7, 9.1.9, then 9.2.1. PDFjet Pro is released
with the library, the same day, with the same number.

After each release, main is the next even number, for customer builds: after
9.1.9, main is 9.2.0. Faults found after a release are fixed there, with
their tests.

1. A customer build is never public: no tag of the even number (Go and
   SwiftPM take versions from tags), nothing uploaded to NuGet or Maven
   Central, no GitHub release. The producer string says the even number,
   so a PDF tells it was made by a customer build. README.md's install
   lines name the last release; CHANGELOG.md has "## v9.2.0 — customer
   builds, not released" over the changes.
2. The even number is released as the next odd one: its CHANGELOG section
   becomes "v9.2.1 — the date", the version moves to 9.2.1, and the release
   goes out as any release does; then main becomes 9.2.2.

## Checks before each release

- **PDFjet Pro, the same day, with the same number.** The library is tagged
  first, then PDFjet Pro: `./set-version.sh X` in pdfjet-pro, its tests
  against the release, CHANGELOG.md, the tag vX. The library's CHANGELOG
  says what changed in it, plainly.
- **The four ports agree.** `./check-examples.sh` builds and runs the
  examples and the unit tests of the four ports, compares the example PDFs
  with Java's, page by page and in every content stream, checks the PDF/UA and
  PDF/A files with veraPDF, opens the Java PDFs in PDFium, pdf.js and
  Poppler, and compares the snippets of the booklet in the four ports. The
  Build workflow runs the same checks on every push, but for the snippets,
  which it compiles and does not compare.
- **Swift where `Int` is 32 bits.** The Build workflow compiles the Swift
  library and its examples for 32-bit armv7 (`./check-swift-32bit.sh`, locally too), as
  the Swift Package Index builds every release for wasm32 and watchOS
  (`arm64_32`); it found 9.1.9's `0xFFFF_FFFF` there only after the tag.
- **The public API.** `./check-api.sh vX` (the last release) lists what
  was added and what was removed in each port: nothing is removed and no
  signature changes but those util/api-removed.txt lists, each with a line
  of CHANGELOG.md, and what is added is what CHANGELOG.md lists.
- **The manual viewer pass.** The PDFs in `pdf-files-to-test`, opened by
  hand in Acrobat Reader, and with NVDA where a file is tagged; see the README
  there for the files of each release. One question is open: Example_42's
  shapes have their opacity in an ExtGState and in the annotation's `/CA`,
  and ISO 32000 applies `/CA` to the appearance, so a strict viewer could
  draw them at 25% where MuPDF, PDFium, Poppler and pdf.js draw them at 50%
  (all four ignore `/CA` when the annotation has an appearance, tested 14
  November 2026). Only Acrobat is left to see; if it draws 25%, the fix is
  to drop `/CA`, not the ExtGState.
- **PAC.** The PDF/UA files in `pdf-files-to-test`, checked with PAC in
  Wine on Ubuntu; see the README there for which files go in.
- **Windows and macOS.** The Windows and macOS workflows, started by hand
  from the Actions tab on the commit to tag: Windows builds and runs the
  examples of the four ports, macOS the examples and the unit tests of Go.
  The Build workflow runs on Linux, with Swift on macOS too.
- **Benchmarks.** `benchmarks/run.sh` and the four ports' text and table
  benchmarks in `benchmarks/ports` and `benchmarks/table`, against the last
  release; the results are kept in their `results` folders.
- **Example_24.** The 2,546-page table in the four ports, against the last
  tag.

## Open

- **Left of the word dictionaries (done for 9.2.1 in the four ports, 22
  November 2026; CHANGELOG.md):**
  - CI: install hb-shape (`apt install libharfbuzz-bin`) in the Linux jobs
    of the four ports, so that HBShape's tests run there; they are skipped,
    or pass without testing, where it is not installed.
  - A Thai reader looks at the Thai UDHR's lines once, and at the letters
    that change their form in GSUB (ญ, ฐ with a lower vowel), which PDFjet
    does not read; and the same for Lao, Khmer and Burmese when a reader is
    found.

- **One image used by many documents.** A batch of 1,000 letters, each
  with a 386 by 395 RGBA PNG logo, spends nearly nine tenths of each letter
  on the logo (8.78 ms a letter, 1.00 without it, in Java): the PNG is
  decoded, its pixels and alpha split and both deflated again for every
  document. An image whose decoded and deflated data a second PDF can take
  as it is would make such a batch several times faster. To design,
  measured in the four ports first.

- **Korean line breaking by design.** isCJK, which chooses how TextBlock
  breaks a line, counts the Hangul jamo (U+1100 to U+11FF) but not the
  11,172 syllables (U+AC00 to U+D7A3), in the four ports. So Korean breaks
  at its spaces, as English does: keep-all, which Korean documents mostly
  use, but by chance. Its upper bound, U+9FD5, leaves out the newer CJK
  characters and the extensions (𨋢, U+282E2), and a line is CJK only when
  more than half of it is, so a long run of Chinese in a line mostly
  English is cut without the kinsoku rules, and a Latin word in a line
  mostly Chinese is broken between letters ("電郵或 W" / "hatsApp 交回").
  To do once a Korean reader has confirmed keep-all: Korean named on
  purpose, the Chinese ranges brought up to date, a test of each case, and
  the PDF the same for every text drawn well today.

- **Large documents.** Two parts:
  1. Smaller tagged documents. Each cell of a PDF/UA table costs 280 to 415
     bytes, its text and a structure element of its own, each an object
     written uncompressed: a price list of 3,000 rows of 6 columns is
     4.7 MB. First the structure elements written in fewer bytes, measured,
     with the xref table as it is.
  2. PDF/A and tagged documents at any size. A PDF/A-1 document past its
     limits is refused today; this writes one within them at any size: a
     balanced page tree, a parent tree of /Kids, and resources for each page
     rather than one dictionary of every image and font. Then: a tagged
     document keeps less of each page until complete, the pages keep less
     once written, and a bookmark tree of thousands of levels or siblings
     is built in linear time.

- **Object streams, an option off by default.** With
  `PDF.setObjectStreams(true)`, the objects that are not streams (structure
  elements, annotations, fonts' dictionaries) are written compressed in
  object streams, and the cross-reference table becomes a cross-reference
  stream. Off by default, so the readable xref table stays. Refused for
  PDF/A-1, which forbids both. An object stream is closed every 100 to 200
  objects, so one batch of small dictionaries is held at a time. Measured on
  the benchmark's documents and a tagged price list, before and after, the
  peak memory too; checked with qpdf --check and in Acrobat.

- **Text wrapped around images, in TextFrame.** The frame is told of the
  areas to keep clear on its page, `addExclusion(x, y, w, h)`, with a gap
  around them; each row takes the widest free run of the frame's width at
  its height, left or right of the image, never both. A row too narrow for
  a word (under about 3 em) is skipped. Rectangles only. TextFlow gets it
  later as an image item. Tests of the rows' starts and widths beside an
  image in the four ports, and an example. Core or Pro to be decided when
  it is scheduled.

- **How well AI assistants write PDFjet code.** About 10 everyday tasks
  (an invoice with a logo and a table, a 20-page report with page numbers,
  a letter in Bulgarian, a CSV file as a table, a form, a chart), written by
  a small model once with the language's llms guide and once without; a
  script compiles and runs every program and checks the PDF (it opens, the
  pages and text are there, PDF/UA passes). Gives the share that works the
  first time, and the traps to add to the guides.

- **The benchmarks again, on the .ttf files.** pdf-libs-benchmarks gave
  PDFjet the .otf files of IBM Plex Sans and Noto Sans and the other
  libraries the .ttf files. The same run with every library on the .ttf
  files, to show beside it, both on the tag of the release, the machine
  otherwise idle.

## Known and accepted (document, do not fix)

- Only Arabic and Persian letters are shaped by PDFjet itself. The Indic
  scripts, Sinhala, Khmer, Lao and Myanmar are drawn by `ComplexText`, with
  the platform's shaper (java.awt.font, Android, Core Text) or one an app
  passes (HarfBuzzSharp in .NET, go-text in Go): their glyphs drawn as
  outlines, the text as the actual text of tagged content. PDFjet's font
  code reads no GSUB.
- No Nastaliq Urdu.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- MuPDF puts spaces inside vowelled Arabic and Hebrew words drawn from `.otf`
  fonts; Poppler extracts them whole.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
- Full CommonMark is not planned: PDFjet's Markdown is the practical subset
  that documents use, as its Bidi covers what text needs without the whole of
  UAX #9.
- No GIF and no TIFF images: a GIF or a TIFF is a PNG in one command of
  any free converter, without loss. PDFjet reads PNG, JPEG, BMP and SVG,
  and Markdown for documents; not HTML.
- No text extraction: PDFjet makes PDFs.
- Any `.ttf` or `.otf` file can be loaded, and it is embedded as a subset.
