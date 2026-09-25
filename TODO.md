# PDFjet — the plan to Oct 21

Target: **2026-10-21**, the date v9.0.0 was planned for. v9.0.0 was released
early, on 2026-09-16 at e957f841, and v9.0.1 on 2026-09-18 at 72c41923, so
what Oct 21 carries is **v9.0.3**: a foundation to build on after it, and
two features the work was far enough ahead to fit in: `Cell.setRowSpan`
(Sep 21) and Markdown to PDF, merged from its branch on Sep 22. Today is
Sep 22, so 29 days are left, with one fix release in the middle. This file
is the working list; tick items off as they land on master.

The work runs ahead of the calendar below. Goals 1 and 2 are closed: every
class of the library is read end to end, three weeks before the Oct 9-14 week
that was to finish it. What is left before the Oct 1 release is the release
checks.

What is done is in CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## The three dates

- **Oct 1** — v9.0.2 of the MIT library only, the `pdfjet` repository, cut
  from master: the API of v9.0.1 and the members goal 6 lists, Markdown to
  PDF and `PDF_A_3A_UA_1` among them. The commercial product is not released
  with it. What is in it is under `## Unreleased` in CHANGELOG.md: the PDF/UA examples and the
  table tagging, kinsoku, the font line gap, the WinAnsi characters and
  kerning of the core fonts, the fallback font rule, the CMYK JPEG marker,
  the font stream, PNG, BMP, JPEG, SVG, OpenType, decompressor and `PDF.read`
  fixes that fuzzing found, and the reviews of the image classes, `Page`,
  `TextLine` and the reader.
- **Oct 15** — code freeze: fixes only, each with its check.
- **Oct 21** — v9.0.3 of the MIT library and, the same day, v9.0.3 of the
  commercial product (`.commercial`: electronic invoices and digital
  signatures), built on the 9.0.3 library. The same number says which
  library each release of the commercial product is built on.

## Why 9.0.3 is hardening and not features

The bugs found from Sep 17 to 19 were almost all in code nobody had read end
to end, and almost all were silently wrong output that every test and example
check passed: 23 in the reviews of `Cell`, `Table`, `TextBlock`, `TextColumn`
and `PDF`; then 32-bit BMPs in the wrong colors, ’ and € drawn as spaces in
the core fonts, every CMYK JPEG inverted, a fallback font that stuck, the CJK
line gaps, and what the first three fuzz targets turned up. Ten kinds of
untrusted input are read, from a file or from the text a document is
written from, and all ten are fuzzed. So the work to Oct 21 is the seven
goals below, in this order.

Six features are the exceptions. Five are in v9.0.2. The first is
`Cell.setRowSpan` (Sep 21): goals 1 and 2 closed three weeks early, the
feature is what clients ask `Table` for and could not have, and it landed
with tests in the four ports and an example that renders alike in all of
them. The second is the page breaks of `Table` (Sep 22): the wrapped lines
of a row are kept together, which changes one page break of Example_34, and
`keepRowWithNext`, `setNumberOfFooterRows`, `setPageSum`, `setRunningSum`
and `setBroughtForwardSum` are new, each with tests in the four ports. The
third and the fourth, the same day, are the rest of what "After v9.0.3 --
features" had, from the review of `Table` and `Cell` of Sep 18: striped
rows with styles for the header and the footer rows, and column widths
shared from the width of the table. The fifth, the same day,
is a `TextFrame` that flows onto as many pages as its text needs.

The seventh, on Sep 24, is `Compliance.PDF_A_3A_UA_1`, PDF/A-3a and
PDF/UA-1 in one document, which PDFjet Forms writes every PDF as: archival,
accessible, and able to carry the layout it was made from. It is a new
member of an enum and a few lines in the four ports, since the tagging of
PDF/UA and the metadata of PDF/A were both there, and it is checked by
veraPDF with both profiles, in check-examples.sh by Example_55. With it, the
same day, `setAltDescription` of `Barcode` and `QRCode`, so that a barcode
of such a document is read aloud rather than skipped, and the content of a
figure is the figure, marked no further.

The sixth is Markdown to PDF, which is in v9.0.2 with the others, since
9.0.2 is cut from master: it was written on a branch of its own with the
checks master has, and merged on Sep 22 once it had them all. It is the one feature of the release that is
not a small addition to something that was already there, which is why it
was kept off master until it was done. The other members master adds to the
API of v9.0.1 came with the PDF/UA work and the fixes; goal 6 lists them.

## The seven goals of v9.0.3

1. ✅ **B** Fuzz the parsers of untrusted input with Go's fuzzing, and fix each
   failure in the four ports, which share the logic. Any input either works or
   fails with a clean error: no hang, no index error, no runaway memory. The
   targets and their corpora stay in the repository, in `src/*_fuzz_test.go`
   and `src/testdata/fuzz`.
   - ✅ Done (Sep 19–21): the eight targets -- the `.stream` fonts, PNG, BMP,
     JPEG, SVG, OTF and TTF, the decompressor and `PDF.read` -- each replayed
     in the four ports, and every failure they found fixed in the four ports
     with a test. What was fixed is in CHANGELOG.md.
   - ✅ Two more since (Sep 22): `Markup` and `Markdown`, the readers of the
     text a document is written from. They found the quadratic joining of
     `TextFrame`, and a `Markdown` that looped forever on a surrogate pair in
     a code block 31 quotes deep. Ten targets in all.

2. ✅ **B** Finish the class-by-class review, as on Sep 17: one class end to
   end in the four ports, each finding proved by running it, fixed in the four
   ports with a test, and the example pages it touches rendered before and
   after. By exposure, and in this order: ✅ `Page` and `TextLine`; ✅ `Image`,
   `PNGImage`, `JPGImage`, `SVG` and `SVGImage`; ✅ `Font` and its loaders
   `OTF`, `OpenTypeFont`, `FontStream1` and `FontStream2`, which no test
   names;
   ✅ the reader, `PDF.read`, the merge and split, `PDFobj` and `Decryptor`,
   which no test names; ✅ `TextFrame`, `BigTable`, `CompositeTextLine` and
   `Bidi`; ✅ the barcodes, the charts, `Form`, `Container` and `Stamp`.

3. ⬜ **B** PDF/UA as it is claimed, by the Matterhorn Protocol, not only
   veraPDF, which cannot see what a paragraph stands for. PAC runs on Windows
   only, so the checks it would make by hand are in
   `.github/scripts/check-pdfua-tags.py`, which reads the structure tree of
   every tagged example: the heading levels, the descriptions of the figures
   and the links, the nesting of the lists, and the header cells and the
   widths of the rows of the tables. It runs in `check-examples.sh` and in the
   Build workflow. There are 39 PDF/UA examples, not 41: Example_30 is
   encrypted and Example_43 has its `setCompliance` commented out and stays
   that way: a tagged table of 2000+ pages is an object for every cell, which
   is 249 MB rather than 11.8 MB, and the example says so. What the first run
   found is fixed (Sep 21): not one heading was tagged as a heading in any of
   them, though 23 draw a visible title, so every title was a paragraph. What
   came after it, and what is left for this goal:
   - ✅ Done (Sep 21): a paragraph of a `TextColumn` or a `TextFrame` is one
     structure element, the contents of Example_22 and the numbered
     paragraphs of Example_03 are lists, and a tagged `BigTable` writes its
     elements with each page.
   - ⬜ PAC itself, or the Matterhorn conditions that need eyes on a page:
     the reading order of each page, whether a colour alone carries meaning,
     and whether each Alt says what its figure shows.

4. ⬜ **B** The manual viewer pass, open since 9.0.0: Acrobat Reader on
   Windows opens Example_30 with `hello` and `world`, shows print allowed and
   copy denied, opens the Cyrillic and the 200 byte password files, and opens
   the PDF/UA and PDF/A examples without warnings. The same files in Preview,
   Chrome (pdf.js), Firefox and Edge — not only veraPDF and MuPDF.

5. ✅ **B** Test the output against independent references and real files:
   the text of every example, the images against Pillow, the fonts against
   fontTools, and the reader against the pdf.js and veraPDF corpora. Done on
   Sep 21, three weeks early: all four are checks the Build workflow runs,
   and every bug they found is fixed in the four ports with tests.

6. ⬜ **B** Freeze the API and the behavior. After 9.0.2, fixes only; the
   public API is that of v9.0.1 and the members master adds to it, the
   same in the four ports, checked as for 9.0.1 before each tag:
   - `Cell.setRowSpan` and `Cell.getRowSpan`, the row spans (Sep 21);
   - `Chart`, `BarChart` and `DonutChart` `setAltDescription`,
     `Page.beginStructElement` and `endStructElement`, `Paragraph.setListLabel`
     and `setStructureType`, and `StructElem.LBODY`, for PDF/UA (Sep 21);
   - `Font.getLineGap` and `CalendarMonth.setFirstDayOfWeek`;
   - `Table.keepRowWithNext`, `setNumberOfFooterRows`, `setPageSum`,
     `setRunningSum` and `setBroughtForwardSum`, the page breaks of a table
     (Sep 22);
   - `Table.setAlternateRowColor`, of a 0xRRGGBB value and of red, green
     and blue (in Go `SetAlternateRowColorRGB`), `setHeaderRowStyle`,
     `setFooterRowStyle`, `setWidth`, `setColumnWidthsInPercent` and
     `fitToWidth`, the row styles and the column widths (Sep 22);
   - `TextFrame.drawOn(pdf, pages, pageSize)`, in Go `DrawOnPages`, a frame
     that flows onto as many pages as its text needs (Sep 22).

   Three types and twenty-one members came after those, all on Sep 22:
   - `Markup`, with its constructor of five fonts, `paragraph`, `paragraphs`,
     `setLinkColor` and `setLinkUnderline`, and `Paragraph.addJoined`;
   - `Markdown`, with its constructor of five fonts, `setHeadingFont`,
     `setMargins`, `setImageDirectory` and `drawOn`, and the structure types
     `StructElem.BLOCKQUOTE` and `StructElem.CODE`;
   - `Relationship` and its five values, `PDF.addAssociatedFile`,
     `PDF.addMetadata`, and the constructor of `EmbeddedFile` that takes the
     media type, the relationship and the description, which are the files a
     document of PDF/A-3 carries.

   One type and four members came on Sep 23, the GS1 barcodes:
   - `GS1` and `GS1.digitalLink(domain, data)`, in Go `GS1DigitalLink`, the
     GS1 Digital Link of GS1 data, for an ordinary QR code;
   - `Barcode.GS1_128` and `Barcode.ITF_14`;
   - `DataMatrix.fromGS1(str)` and `fromGS1(str, shape)`, in Go
     `NewGS1DataMatrix` and `NewGS1DataMatrixWithShape`, in Swift
     `init(gs1:_:)`, GS1 DataMatrix from GS1 data written as people read it.

   `MarkdownParser` is not public in any port, so the Markdown a document is
   written from is read one way only, through `Markdown`.

   One more value and three members came on Sep 24, decided that day to go
   into 9.0.2, which is cut from master, rather than to wait for 9.0.3:
   - `Compliance.PDF_A_3A_UA_1`, PDF/A-3a and PDF/UA-1 in one document, after
     `PDF_A_3B`, so that no other value changes; and `PDF_A_3A` deprecated
     for it (`@Deprecated`, `[Obsolete]`, `@available(*, deprecated)` and a
     `Deprecated:` comment in Go), still written as before;
   - `Barcode.setAltDescription` and `QRCode.setAltDescription`, a barcode
     read aloud as one figure;
   - `TextBlock.setStructureType`, as `TextLine` has it, for a heading.

   With them, two changes of behavior, which the CHANGELOG has under
   Changed: the A levels of PDF/A tag their content, as level A asks, and
   refuse a figure with no description as PDF/UA does; and what is drawn
   inside a figure is the figure, marked no further.

   Nothing is removed and no signature changes. Some differences are not
   members to list: Swift's `Alignment` has an `init(rawValue:)`, and with it
   `Hashable`, since its cases are the numbers a `Cell` packs; the
   constructors of Swift's `DataMatrix` and `EmbeddedFile` that became
   convenience ones keep their signatures; Go's `Compliance` has a `String`
   method (Sep 22), as a Go enumeration does; and Go's
   `internal/utf8text.Decode` is not importable. Java has no internal access
   across packages, so what the packages of the library share and is not API
   is in `com.pdfjet.internal`, which is left out of the Javadoc and of the
   API diff: the GS1 parser that `Barcode` and `DataMatrix` share, as Go's
   `internal/gs1` (Sep 24, before any release had it public).
   Any other difference fails the check. `./check-api.sh` lists each port's
   public declarations at the tag and at v9.0.1, of `javap -public`,
   `util/goapi` with Go's own parser, reflection over the C# assembly and the
   Swift symbol graph, and prints what is gone, changed and added; it fails
   if anything is gone. On Sep 24 nothing was, and everything added is
   listed here.

7. ⬜ **B** Guard against regressions: benchmarks recorded at 9.0.2 and 9.0.3
   against 9.0.1, Example_43's printed time among them, and the JDK 8 build,
   the packages and the docs made from the tag.

## The calendar

Four and a half weeks. The reviews and the fuzzing run first because they
change code; the checks that must hold at the tag run after the freeze.

Where it stands on Sep 21: the Sep 20-26 week is done, and with it the
review of the `Font` loaders that the Sep 27-Oct 1 week had, two of the three
blockers of the Oct 2-8 week -- the review and the fuzzing of the reader --
and, of the Oct 9-14 week, the rest of goal 2 and the fonts of goal 5. Goal 2
is closed. That buys about two weeks. They go to the work that has to be done
by hand and cannot be hurried at the end -- goal 4, the viewer pass, and goal
3, the Matterhorn conditions that need eyes on a page. Goal 5 closed the same
day.

### Sep 20–26: the decoders, `Page`, `TextLine` and the reader

- ✅ **B** Goal 1: the eight fuzz targets, in `src/*_fuzz_test.go` (Sep 19–21).
- ✅ Goal 2: the reviews of the image classes, `Page` and `TextLine`, the
      reader, `Font` and its loaders, `TextFrame`, `BigTable`,
      `CompositeTextLine` and `Bidi`, the barcodes, `Container`, the charts,
      `Form` and `Stamp` (Sep 21). Goal 2 is done.
- ✅ Goal 1: `FuzzPDFRead` fails on a Go runtime error, and drives the merge,
      the split and the stamp (Sep 21).
- ✅ What is fixed is recorded under `## Unreleased` in CHANGELOG.md as it
      lands (Sep 21).
- ✅ Goal 5, in part: the 252 fonts PDFjet ships against fontTools (Sep 21).
- ✅ True row spans, `Cell.setRowSpan` (Sep 21), and beside them the bottom
      border of a cell whose text wraps, a `BigTable` wider than its page, the
      small hexadecimal digits of Swift's strings, and a PNG, JPEG and BMP
      drawn at the size they ask for.
- ✅ Example_17 is an example rather than a test case, and a line chart
      (Sep 21).
- ✅ `Markup`, the inline markup of Markdown in a `Paragraph`, with
      `Paragraph.addJoined` beside it and Example_53 (Sep 22).
- ✅ Markdown to PDF merged from its branch, with Example_54 (Sep 22).
- ✅ The files a document carries, `PDF.addAssociatedFile` and
      `PDF.addMetadata`, which PDF/A-3 asks for and an electronic invoice is
      made of (Sep 22). See the invoices section below.

### Sep 27–Oct 1: release v9.0.2

- ✅ **B** Goal 2: the review of `Font` and its loaders. Done on Sep 21.
- ⬜ **B** The release checks: `check-examples.sh` clean, the public API that
      of goal 6 in the four ports, the JDK 8 build, the benchmarks recorded
      against 9.0.1 with Example_43's time, the docs, the packages and the
      site rebuilt, the CHANGELOG entry dated. 9.0.2 is cut from master, so it
      carries the members of goal 6, those of Sep 24 among them, and the
      fixes under `## Unreleased`.
      Done on Sep 22, to be run again at the tag: the API diff, the JDK 8
      build, and the benchmarks, in `benchmarks/results/2026-09-22-*.log` and
      `benchmarks/table/results/2026-09-22-010c7c21.log`. They found `Table`
      measuring every row on every page and Java's `BigTable` reading its
      file a byte at a time, both since v9.0.1 and both fixed. Example_43 is
      as fast as at v9.0.1 with `BigTable` (1.66 s) and 8% slower with
      `Table` (3.76 s); `Table` in the four ports is 5 to 22% slower than on
      Sep 18, spread over the tagging, the fallback font of each character and
      the row spans, with no one place that costs it.
      Run again on Sep 24, after the PDF/UA and PDF/A work of that day:
      `check-examples.sh` clean in the four ports and with JDK 8,
      `./check-api.sh` with nothing of v9.0.1 gone and every addition in goal
      6, and the benchmarks in `benchmarks/results/2026-09-24-f583352f.log`
      and `benchmarks/table/results/2026-09-24-f583352f.log`: Example_43
      takes 1.65 s with `BigTable` (1.67 s at v9.0.1) and 3.61 s with `Table`
      (3.49 s, 3.5% slower, from 8% on Sep 22), and the text is as fast as at
      v9.0.1. Left for the tag: these again on the tagged tree, the docs, the
      packages, the booklet and the site rebuilt, and the CHANGELOG entry
      dated.
- ⬜ **B** Tag v9.0.2 on Oct 1 and make the GitHub release.

### Oct 2–8: the reader

- ✅ The Oct 8 decision on the `markdown` branch, made early: merged on
      Sep 22. See the Markdown section below.
- ✅ **B** Goal 2: the review of the reader. Done on Sep 21.
- ✅ **B** Goal 1: fuzz `PDF.read`. Done on Sep 21.
- ✅ **B** Goal 5: the reader against the pdf.js and veraPDF corpora, and the
      PDFs pdf.js links to, in the four ports (Sep 21). Every port reads the
      4,300-odd files with no failure, and the ones that differ by design are
      in `tests/corpus/known-differences.txt`.

### Oct 9–14: the rest of the review, and the references

- ✅ **B** Goal 2: the rest of the review. Done on Sep 21.
- ✅ **B** Goal 5: round-trip text, images against Pillow and fonts against
      fontTools, three checks the Build workflow runs (Sep 21).
- ⬜ **B** Goal 3: PAC or Matterhorn over the 41 PDF/UA examples, and the
      fixes it asks for — the last day a tagging fix can land before the
      freeze.

### Oct 15: code freeze

- ⬜ **B** Goal 6: fixes only from here, each with its check. Whatever is
      still open moves to 9.0.4 or the AGPL major rather than into the tag.

### Oct 15–20: the checks that must hold at the tag

- ⬜ **B** Goal 4: the manual viewer pass, on the files built from the frozen
      master, in Acrobat Reader, Preview, Chrome, Firefox and Edge.
- ⬜ **B** Goal 7: benchmarks at 9.0.3 against 9.0.2 and 9.0.1, Example_43's
      printed time among them; the JDK 8 build; the packages and the docs made
      from the tag.
- ⬜ **B** `check-examples.sh` clean in the four ports, and the public API
      still that of goal 6.
- ⬜ Rebuild the site, and date the `## v9.0.3` entry of CHANGELOG.md.
- Keep Oct 19 and 20 empty: they are the buffer for what the checks find.

### Oct 21: release v9.0.3

- ⬜ **B** Tag v9.0.3 and make the GitHub release.
- ⬜ **B** Release v9.0.3 of the commercial product, built on the tag of the
      library: `.commercial/go.mod` requires `github.com/edragoev1/pdfjet/v9
      v9.0.3` rather than v9.0.0, with no `replace` of the local checkout in
      what is shipped, and the Java, C# and Swift packages are built against
      the 9.0.3 library. The commercial code needs 9.0.2 at least, for
      `Compliance.PDF_A_3A_UA_1`, and uses nothing master does not have.

## Markdown to PDF — merged

- ✅ A practical subset of Markdown to PDF, in the four ports: headings,
  paragraphs with the inline markup of `Markup`, bullet and numbered lists
  that nest, block quotes, fenced and indented code, thematic breaks, GitHub
  tables and images, flowed down the pages and tagged for PDF/UA. Written on
  the branch `markdown` and merged into master on **Sep 22**, sixteen days
  before the Oct 8 date it was given, with its tests, its fuzz target,
  Example_54 and a booklet snippet in each port. The branch is deleted.
  - It is called Markdown, not CommonMark: CommonMark's 652 examples are the
    goal of v9.1 below.
  - The merge holds the checks that hold at the tag: the unit tests of the
    four ports, and `check-examples.sh` over the 54 examples and the 59
    snippet PDFs.

## Found by pdfjet-server, to fix later

The SaaS (pdfjet-server, pdfjet-client) builds on the MIT core without
changing it, to keep the four ports in sync; what its testing finds wrong in
the core is listed here, and pdfjet-server works around it until it is fixed.
Each was proved in the Go port; the other three have the same code, so each is
to check and fix in the four, with a test.

- ✅ **A glyph two characters share is copied as one of them.** Source Serif 4
  draws the Greek small letter mu (U+03BC) and the micro sign (U+00B5) with the
  same glyph, and the ToUnicode map of the embedded font gives that glyph one of
  them, the micro sign: Greek text drawn in it, "δικαιώματα", is right on the
  page, but is copied, searched and read aloud with µ in place of μ. The map
  of a glyph should be the character it was drawn for, as an ActualText span
  or a glyph of its own in the map would give it. Found by comparing the PDFs
  of Latin, Greek and Cyrillic text in the four families of the editor, on 23
  September 2026; IBM Plex Sans, JetBrains Mono and Noto Sans read back as
  drawn. Fixed the same day in the four ports: the micro sign, and the ohm,
  kelvin and angstrom signs, are among the characters text seldom has, so a
  glyph they share with a letter is copied as the letter.

- ✅ **Code 128 cuts a long text short without a word.** `drawCode128` stops
  at 48 codewords and draws the rest of the barcode, so the barcode holds
  another value than its text: 60 sevens draw exactly as wide as 48. It
  should refuse the text, or say so, as `NewQRCode` does for a text too long.
  pdfjet-server refuses such a text before it is drawn (`code128Codewords`
  in document.go). Found Sep 23; fixed Sep 23 in the four ports: the
  constructor refuses it.
- ✅ **Code 128 panics on a character above U+00FF.** `drawCode128` appends
  codeword 256, "This will generate an exception", and indexes the table
  with it: "A€" is `index out of range [256] with length 107`. It should
  refuse the text with a clear error. pdfjet-server refuses it first. Found
  Sep 23; fixed Sep 23 in the four ports: the constructor refuses it.
- ✅ **A barcode is drawn in whatever pen color the page has.** `Barcode`
  sets the pen width of each bar but never its color, and does not save and
  restore the graphics state, so after `Page.SetPenColor`, which an ellipse or
  a path drawn with the methods of `Page` leaves set, the bars come out in
  that color. It should draw in a color of its own, black unless set, inside
  q and Q. pdfjet-server sets the pen to black before each barcode. Found
  Sep 23; fixed Sep 23 in the four ports: the bars are black, in q and Q.

- ✅ **The guard bars of EAN-13 and UPC-A are 8 points longer, not 5
  modules.** The GS1 General Specifications have the guard bars extend 5X
  below the other bars, so the extension follows the module length;
  `drawEGuard` and `drawMGuard` are drawn `h+8`, 8 points whatever the
  module, which is about 10.7 modules at the default of 0.75, 8 at 1 and 4
  at 2. Barcodes scan either way; they look unlike the standard's. Found Sep
  23, while porting the drawing to pdfjet-client, whose preview draws them
  as PDFjet does until it changes. Fixed Sep 24 in the four ports, and in
  pdfjet-client's preview.
- ✅ **UPC-A does not extend the bars of its first and last digits.** In a
  UPC-A the bars of the number system digit and of the check digit are as
  long as the guard bars, which is why the two digits are printed outside
  the bars; `drawCodeUPC` draws every digit at `h`. Found Sep 23, with the
  item above. Fixed Sep 24 in the four ports, and in pdfjet-client's preview.

- ✅ **The lines of a table's cells are drawn in whatever pen color the page
  has.** A cell's border color is transparent until `SetBorderColor` or the
  table's `SetCellBorderColor` sets one, and `drawBorders` then sets no pen
  color at all, so after `Page.SetPenColor` the lines of the table come out
  in that color, as the bars of a barcode did (fixed Sep 23). A cell should
  draw its lines black unless a color is set, in a graphics state of its
  own. pdfjet-server sets them black with `SetCellBorderColor`. Found Sep 23,
  while adding tables to the editor. Fixed Sep 24 in the four ports: a cell
  with no color set draws its lines black. Not in a graphics state of its
  own: the pen color is written only when it changes, so black costs one
  operator a page, where q and Q would cost two for every cell. pdfjet-server
  no longer sets it.

- ✅ **A wrapped cell line can keep a trailing space, which moves right and
  centered text.** When a word wider than the column follows other text and
  its first character does not fit, `wrapCellText` pushes the line with the
  space after it; the rows below are wrapped again and lose theirs, but the
  first line keeps it, and the space counts in the width of the text. A
  table of columns 30% right, 30% center and 40% at a width of 100, each
  cell "abcd Wxyzwxyzw" in IBM Plex Sans at 10: the first line is "abcd ",
  drawn 2.36 points too far left in the right column and 1.19 in the center
  one. Found Sep 23, porting Table to pdfjet-client, whose check has it as a
  case. Fixed Sep 24 in the four ports, and in pdfjet-client's preview.
- ✅ **Not a fault: the lines of a wrapped cell are evenly spaced.** It was
  noted that the first row of a wrapped cell keeps its top and bottom padding
  and each row added below it drops only its top padding, so the rows are 17
  and 15 points tall in IBM Plex Sans at 10. The lines of text are 15 points
  apart all the way down, which is what matters: the text of a row starts
  under its top padding, and the extra 2 points of the first row are the top
  padding above the first line alone. Measured in the Go port on Sep 24.
- ✅ **Column percentages that are all 0 leave the columns 75 points wide.**
  `applyColumnPercents` returns when their total is not above 0, so
  `SetWidth` is ignored and each column keeps the width of a new cell: two
  columns at 0% and a width of 300 make a table 150 wide. It should share
  the width equally, as for columns with no percentage. Found Sep 23, with
  the items above. Fixed Sep 24 in the four ports, and in pdfjet-client's
  preview.
- ✅ **A corner radius over half the rect is drawn as crossed loops.**
  `Rect` draws each side from the radius in from one corner to the radius in
  from the next, and does not cap the radius, so a radius over half the
  width or the height turns those sides back on themselves, and the curves
  of the corners cross: a rect 20 high with a radius of 30 has loops at its
  ends. It should cap the radius at half the shorter side, as SVG caps rx
  and ry, which is what a pill shape is drawn with. pdfjet-server caps it
  before it draws (`cornerRadius` in document.go). Found Sep 24, writing the
  help of the corner radius of a box in the editor. Fixed Sep 24 in the four
  ports; pdfjet-server no longer caps it.
- ✅ **The header row has a line under it without borders.**
  `drawHeaderRows` sets the bottom border of the last header row whatever
  `SetCellBorders` says, so a table without lines still has one under its
  header. Decided Sep 24 that it is a fault: the line is drawn only under the
  header cells that have borders. Fixed in the four ports, and in
  pdfjet-client's preview. Found Sep 23.

- ✅ **No level is PDF/A and PDF/UA at once.** A document is one or the
  other, `PDF_UA_1` or a PDF/A level, where the two are made to go together,
  and PDFjet Forms writes documents that are to be archived and read aloud.
  Added Sep 24: `Compliance.PDF_A_3A_UA_1` in the four ports.
- ✅ **The A levels of PDF/A tag no content.** `PDF_A_1A`, `PDF_A_2A` and
  `PDF_A_3A` write a structure tree, but the marked content of the text and
  the images is written only for PDF/UA (`isUA` in the four ports), so a
  document of an A level has no tagged content, which the A of PDF/A asks
  for, and the booklet says the A levels are tagged as PDF/UA is. veraPDF's
  PDF/A profiles pass them all the same. `PDF_A_3A_UA_1` is tagged. To
  decide for v9.0.3: tag the A levels as PDF/UA is, which asks each figure
  of them for a description, as PDF/UA does, and so refuses documents that
  are written today; or tag them without that rule; or say in the booklet
  that they are not tagged. Found Sep 24, adding `PDF_A_3A_UA_1`. Decided and
  fixed the same day: the A levels are tagged as PDF/UA is, rules and all,
  in the four ports, which may refuse a document that is written today, as
  CHANGELOG.md says; and `PDF_A_3A` is deprecated, since `PDF_A_3A_UA_1` is
  the same document that says it is PDF/UA-1 too. The PDF/UA examples, of
  PDF/A-1a, 2a and 3a, pass veraPDF's profiles of those levels but for what
  each of PDF/A forbids, such as transparency in PDF/A-1.

- **A long word takes minutes to break across lines.** `appendBrokenWordLines`
  in textblock.go measures the whole rest of the word for every line it
  breaks it over, so the time grows with the square of the word: a word of
  20,000 characters in a full-width block takes 1.6 s, 100,000 take 39 s, and
  2 million in a narrow column had not finished after 5 minutes. Normal text
  is fast, 2 MB of words in 0.4 s. Found by a review of pdfjet-server on 25
  September 2026. To fix: accumulate the advance of each glyph and break where
  the sum passes the width, measuring each character once. pdfjet-server
  refuses a word of more than 1000 characters until then (`checkText`,
  layout.go).

- **Reading a PDF decodes every stream in full, with no total.** `Read` in
  pdf.go inflates every Flate stream as it reads, capped at 256 MB each by
  `decompressor.MaxDecodedLength` and not at all in total: a PDF of 8 MB, 40
  streams of 203 KB that each inflate to 200 MB, took 32 GB to read. Found by
  the same review. To fix: a budget of decoded bytes for the whole `Read`,
  such as 16 times the size of the file, or streams decoded only when they
  are asked for. pdfjet-server refuses a PDF that does not name
  pdfjet-layout.json before reading it, and takes at most 20 MB, until then.

- **An image is decoded whole to give its size.** `NewImage` decodes the
  pixels of a PNG or a JPEG to know its width and height, 256 MB for a PNG of
  16,000 by 16,000, which pdfjet-server did three or four times a request.
  Found by the same review. To fix: read the size from the IHDR of a PNG or
  the SOF of a JPEG first, as `image.DecodeConfig` does, and let a caller ask
  for the size alone. pdfjet-server refuses an image of more than 40 megapixels
  from its header, before anything decodes it, until then (`checkPixels`,
  layout.go).

## v9.1 — features, after v9.0.3

- ⬜ CommonMark itself, in the four ports, where v9.0.3 has the practical
  subset above: a parser written from the spec, with the GitHub tables,
  strikethrough and task lists. The renderer is written and tagged for
  PDF/UA already, so what is left is the parser and the 652 examples of the
  spec.
  - The parser is about 2,500 to 3,000 lines a port, 1,200 to 1,500 of them
    for the blocks and as many for the inlines; the GitHub extensions add
    800 to 1,200, the renderer 800 to 1,500, and the table of the 2,125
    named entities is data, generated once for the four ports.
  - The check is the 652 examples of the CommonMark spec, from its JSON
    file, run in every port, as the examples are compared across the ports.
  - The hard parts: the emphasis algorithm, with its flanking rules and
    the rule of 3; lazy continuation lines in block quotes and lists; tight
    and loose lists; the seven kinds of HTML block; and link labels matched
    with Unicode case folding.
  - About a week for the first port to pass the spec, and a few days for
    each of the others; the renderer is done.
- ⬜ Order-X, in the commercial product (`.commercial`), in the four ports:
  the order of FNFE-MPE and FeRD, a PDF/A-3 that carries its XML as
  Factur-X does, in the profiles BASIC, COMFORT and EXTENDED. No law asks
  for electronic orders, as the laws of Germany and France ask for
  invoices, so it is for the buyers who want the whole of their purchasing
  electronic; worth doing first for a customer who asks.
  - The XML is not that of an invoice: it is the Cross Industry Order of
    UN/CEFACT's Supply Chain Reference Data Model, with a model of its own
    (requested quantities and delivery dates, and the order, the change of
    an order and the response to one) and no rules of EN 16931. What is
    shared is the embedding, the XMP metadata, the XML writer and parser,
    and the parties, addresses, amounts and tax categories.
  - New: the model, the writer and the reader of the order, checked against
    the XSD schemas and the rules of each profile and the sample orders
    Order-X publishes, with tests and an example in every port.
  - A third to a half of the 6,300 lines of the invoice code of the Java
    port, in each port; 3 to 5 days, most of it the checking.
- ⬜ Deliver-X, the delivery note of FeRD, in the commercial product: still
  in development on Sep 24, so written once it is published, after Order-X,
  whose model it shares most of.

## Electronic invoices (in the commercial repository, `.commercial`)

Written and checked in the four ports: the model of EN 16931, the writer and
the reader of the Cross Industry Invoice, the metadata, and `Facturx`, which
makes a document of PDF/A-3 an invoice of Factur-X and ZUGFeRD and reads one
back out of a document someone else wrote. The MIT library carries the files
(`PDF.addAssociatedFile`) and the metadata (`PDF.addMetadata`).

Checked against the 38 sample invoices the standard publishes, and against
veraPDF and the validator of Mustangproject, which reads the XML against the
schema and the rules of EN 16931 and of XRechnung.

What is left, in the order it is worth doing:

- ✅ The examples in C#, Go and Swift, beside the Java one in
  `examples/invoice` (Sep 22). Of `PDF_A_3A_UA_1` since Sep 24: veraPDF
  passes them as PDF/A-3a and PDF/UA-1, and `Facturx` takes that level.
- ✅ A page on the site, as `digital-signatures.html` is, saying what an
  electronic invoice is, what the law asks for in Germany, France and Italy,
  and what the library does about it: `.commercial/e-invoicing.html`, written
  Sep 24 with the dates checked against the ministries' own pages, which it
  cites. It names PDFjet Pro, so it goes on pdfjet.com when Pro launches.
- ✅ Discounts and charges on a line (BG-27 and BG-28), Sep 24, in the four
  ports: `NewLineAllowance` and `NewLineCharge`, with `SetBasis` for the
  percentage and the base, added to a line, whose amount is its price times
  its quantity less its discounts and with its charges. Written in the
  settlement of the line, at its tax, and read back; the schemas of BASIC,
  EN 16931 and EXTENDED and the validator of Mustangproject pass them, and
  the invoice examples of the four ports have one. A discount or a charge
  with no reason and no reason code is refused, on the line (BR-42, BR-44)
  and on the invoice (BR-33, BR-38).
- ✅ The fields that the model had no room for, Sep 24, in the four ports:
  the party that is paid (`SetPayee`, BG-10, BASIC WL and above), the party
  that pays (`SetPayer`, EXTENDED), the invoices an invoice refers to
  (`AddPrecedingInvoice`, BG-3, such as the one a credit note corrects), the
  documents that back it up, linked or attached (`AddSupportingDocument`,
  BG-24, EN 16931 and above), and the line of the order of a line
  (`SetOrderReference`, BT-132), with the order too in EXTENDED, so that one
  invoice covers several orders. Written where the schema has them, read
  back, and checked (BT-59, BT-25, BT-122, BT-125); Mustangproject passes
  BASIC WL, BASIC, EN 16931 and EXTENDED, and the four ports write the same
  XML.
- Order-X and Deliver-X: in v9.1.0 of the commercial product, decided on
  Sep 24; see "v9.1 — features".

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
