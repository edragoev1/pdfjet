# PDFjet — the plan

The library work that is open, in the four ports (Java, C#, Go and Swift),
and the checks that hold before each release. What each release brought is in
CHANGELOG.md.

## v9.0.7

The first public release since 9.0.3. v9.0.5 was tagged on 12 October 2026
as an internal release, not published (the owner: "We will label v9.0.5
internal release and focus on v9.0.7"); nothing more is backported to it,
and 9.0.7 carries all of it: subsets of every embedded font, TrueType and
CFF, fonts added to an existing PDF included; the `.ttf` and `.otf` fonts
read directly, with no `.stream` files; `ImageSize`; the EXIF orientation of
JPEGs; the public XML parser; `Table.setCellPadding`; `addCJKParagraph` with
a language; the fixes of the code review of 11 October 2026 (#1 to #38),
passwords prepared with SASLprep among them; and, new in 9.0.7, fonts that
forbid embedding refused and the stroke width of an Arc. Only a major issue
holds it; anything else goes into 9.0.9.

Left before the tag: the items for 9.0.7 below, the manual viewer pass
(below) on the files of the new code, then the release.

PDFjet Pro is released with it, its first release, on the same day, with
the same number (the owner, 11 October 2026: the two move together), the
Swiss QR-bill in it (merged on 12 October 2026). Right after the library's
tag: in pdfjet-pro, `./set-version.sh 9.0.7`, the tests against the release
(`PDFJET=published`), CHANGELOG.md, the tag v9.0.7 and push; then
pdfjet-pro made public, on the owner's go-ahead. Its CI builds against the
library's main branch until then, and on a tag against the library's tag of
the same number.

And pdfjet-server to the release: its go.mod requires a snapshot of the
library of 9 October (v9.0.4-0.20261009203557-ed6b1c9bcec8), which its CI
and the Forms that is deployed build with, so they have none of the fixes
since, the reader's limits among them, until it requires v9.0.7:
`go get github.com/edragoev1/pdfjet/v9@v9.0.7 && go mod tidy` in
pdfjet-server, its tests, commit and push. Locally its go.work uses the
library beside it, which has them already.

PDFjet Forms takes the number too (the owner, 11 October 2026): Forms is
9.0.7 when it is built on the library's 9.0.7, and tags pdfjet-server and
pdfjet-client v9.0.7 with the deploy that moves it there. Deploys of Forms
alone between the library's releases keep its number, told apart by their
commit and date. The layout's format version (1) and the look's rules
(rules 1) are not these numbers, and never move with them.

## Checks before each release

- **PDFjet Pro, the same day, with the same number.** The library is tagged
  first, then PDFjet Pro: `./set-version.sh X` in pdfjet-pro, its tests
  against the release, CHANGELOG.md, the tag vX. The library's CHANGELOG
  says what changed in it, plainly, a small release as small: never a change
  made only so that it seems to change.

- **The four ports agree.** `./check-examples.sh` builds and runs the
  examples and the unit tests of the four ports, compares the example PDFs
  with Java's, page by page and in every content stream, checks the PDF/UA and
  PDF/A files with veraPDF, opens the Java PDFs in PDFium, pdf.js and
  Poppler, and compares the snippets of the booklet in the four ports. The
  Build workflow runs the same checks on every push.
- **The public API.** `./check-api.sh v9.0.3` (the last release) lists what
  was added and what was removed in each port: nothing is removed and no
  signature changes in a 9.0.x release, and what is added is what CHANGELOG.md
  lists.
- **The manual viewer pass.** The PDFs in `viewer-files-to-test`, opened by
  hand in Acrobat Reader, and with NVDA where a file is tagged; see the README
  there for the files of each release. One question is open from the pass of
  9.0.3: Example_06's shapes have their opacity in an ExtGState and in the
  annotation's `/CA`, and ISO 32000 applies `/CA` to the appearance, so a
  strict viewer could draw them at 25% where MuPDF, PDFium, Poppler and pdf.js
  draw them at 50%.
- **PAC.** The PDF/UA files in `pdf-ua-files-to-test`, checked with PAC on
  Windows; see the README there for which files go in.
- **Benchmarks.** `benchmarks/run.sh` and the four ports' text and table
  benchmarks in `benchmarks/ports` and `benchmarks/table`, against the last
  release; the results are kept in their `results` folders.

## Open

- **v9.0.7: fonts and writing, faster and leaner, in the four ports.** The
  output byte for byte the same: every example and test PDF of the four
  ports written before and after, and compared. Done in 9.0.5 (the owner,
  12 October 2026): the ToUnicode map's character of each glyph found once
  when the font is read, the character-to-glyph table in two levels, and
  the glyphs drawn a set of bits; a page in three Plex fonts 25% faster in
  Go and 12 to 20% in the others, with half the memory or less, the 56
  examples of each port byte for byte the same. Left:
  - Java first, then the other ports as they need it: the GPOS mark tables
    (mark to base, mark to mark) read when a mark is first drawn, where
    every font load reads every anchor pair into a map: 83% of the 7 ms of a
    page in Noto Sans, and half of what a page in Plex allocates.
  - `Font.checksumOf` (19% of a page in Plex, in Java) and the format 4 cmap
    (17%), looked at once the above is done. The checksum still folds the
    65,536 glyphs, as the subset's tag is made of it: a faster one changes
    the tags, and so the bytes of every PDF.
  - Not to be done (the owner, 10 October 2026): a lower compression level
    for the font programs, which makes the files larger and changes their
    bytes; a Deflater for each thread in Java; a pool of FlateEncode's
    buffers in Swift, which gained 2% and nothing on top of reading a font
    once.

- **Smaller tagged documents, with object streams.** Each cell of a table of
  PDF/UA costs 280 to 415 bytes, its text and a structure element of its own,
  each an object written uncompressed: a price list of 3,000 rows of 6
  columns is 4.7 MB. PDF 1.5's object streams, the small objects compressed
  together a batch of 100 to 200 at a time so that memory stays flat, and a
  cross-reference stream would shrink them a third or more. An option, on by
  default, off for PDF/A-1, which forbids object streams. Measured first, the
  time and the size of a long PDF/UA table, before and after.
- **BigTable.setColumnWidthsInPercent, in the four ports.** The width of each
  column drawn, as a percent of the table's width, as Table's
  setColumnWidthsInPercent sets them; BigTable sizes its columns itself today.
  For any BigTable whose columns want widths of their own. Counted in the
  order setColumns draws the columns; refused when the percents are not one
  for each column. A test in each port, the widths compared across the ports.
- **For 9.0.7: the code review of 11 October 2026.** Five areas: the
  writer and encryption, text and fonts, barcodes, tables and charts, the
  Java, C# and Swift ports against Go, and the parsers of untrusted input.
  Its numbers are kept in the items below. Each fix in the four ports, with
  a test.
- **For 9.0.7: what is left of the review.** ActualText for a document
  that draws two characters of one glyph, which extract as the first drawn
  (#8; the ToUnicode map gives each glyph the character drawn with it, in
  9.0.5). The rest of the 38 is fixed in 9.0.5.
- **For 9.0.7: CI.** The output of the four ports compared on more than the
  examples' inputs: the float paths of DonutChart and Arc were found apart by
  a random run against Go (#25), which CI does not run. The fuzzers given
  inputs large enough to reach the 256 MB decode limit (#29 to #32), pages
  timed in FuzzPDFRead (#35), FuzzMarkdown's linear check repeating the
  body without the header (#33), and FuzzMarkup's timing made steady under
  load. (Done: net48 built, check-api.sh run against the last tag, and the
  Java and C# tests run in a Turkish locale, in build.yml.)
- **Longer fuzzing of the font subsetters.** Go's fuzzers ran 5 minutes each
  on every target before 9.0.5 with no failure; the TrueType and CFF
  subsetters, which subset a real font for each input, ran 27,000 and 15,000
  inputs. A run of a night on those two.

## Known and accepted (document, do not fix)

- Only Arabic and Persian letters are shaped.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- MuPDF puts spaces inside vowelled Arabic and Hebrew words drawn from `.otf`
  fonts; Poppler extracts them whole.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
- A font draws only the characters up to U+FFFF that are inside the range
  its OS/2 table gives. Any other character counts as missing, like one the
  font lacks. This costs the glyphs above U+FFFF that 144 of the shipped fonts
  have, such as IBM Plex Math's bold italic letters and the CJK Extension B
  ideographs; no shipped font has an OS/2 range narrower than its glyphs.
- Full CommonMark is not planned: PDFjet's Markdown is the practical subset
  that documents use, as its Bidi covers what text needs without the whole of
  UAX #9.
- No GIF and no TIFF images: a GIF or a TIFF is a PNG in one command of
  any free converter, without loss, and a reader of either is code to write,
  fuzz and keep in four ports for what a developer fixes in seconds. PDFjet
  reads PNG, JPEG, BMP and SVG, and Markdown for documents; not HTML.
- No more fonts are bundled. Any `.ttf` or `.otf` file can be loaded, and it
  is embedded as a subset.
