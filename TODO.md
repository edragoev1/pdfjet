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
that was to finish it. v9.0.2 was released on Sep 28, three days before the
Oct 1 planned for it, after a code review of the four ports.

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
  commercial product (`.commercial`), built on the 9.0.3 library. The same number says which
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

3. ✅ **B** PDF/UA as it is claimed, by the Matterhorn Protocol, not only
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
   - ✅ Done (Sep 29): PAC itself, or the Matterhorn conditions that need eyes on a page:
     the reading order of each page, whether a colour alone carries meaning,
     and whether each Alt says what its figure shows.
     Only files that embed all their fonts go to PAC: one that does not fails
     Matterhorn 31-001 for that alone (see `pdf-ua-files-to-test/README.md`).
     On Sep 28 that left out Example_04, 05, 44 and 50.
     On Sep 29 PAC found five files with Quality issues: Example_21's and
     Example_54's are fixed, Example_06's, on its annotations, are left, as
     no structure of its `Annot` elements clears them (see
     `pdf-ua-files-to-test/README.md`), and Example_42's and 45's, an email
     address in a form field that PAC takes for a link, are left too. The
     PDF/UA testing was closed that day.

4. ⬜ **B** The manual viewer pass, open since 9.0.0: Acrobat Reader on
   Windows opens Example_30 with `hello` and `world`, shows print allowed and
   copy denied, opens the Cyrillic and the 200 byte password files, and opens
   the PDF/UA and PDF/A examples without warnings. The same files in the
   engines of the viewers most people use, not only veraPDF and MuPDF:
   - PDFium: Chrome, and Google Drive and Files by Google on Android;
   - Adobe: Acrobat Reader, and Edge, which draws PDFs with Adobe's engine
     since 2023;
   - Apple PDFKit: Preview, and Safari, Files and Mail on an iPhone or iPad;
   - PDF.js: Firefox, and the many web apps that embed it;
   - Foxit: Foxit PDF Reader.

   With MuPDF (SumatraPDF) and Poppler (Okular, Evince, pdftoppm), which
   `check-examples.sh` already renders with, these six engines draw the PDFs
   of nearly everyone; WPS Office and PDF-XChange have engines of their own,
   and are left out. The ranking is an estimate of use, not measured shares.

   The five engines we focus on (Sep 29), with MuPDF (SumatraPDF) as the
   reference the others are compared against:

   | Viewer | Engine | Covers | How it is checked |
   |---|---|---|---|
   | Chrome | PDFium | Chrome, Brave, Opera, Vivaldi, Android | automatically, in CI and `check-examples.sh` |
   | Firefox | PDF.js | Firefox | automatically (PDF.js in Node, in CI and `check-examples.sh`), and in Firefox by hand now and then |
   | Safari | PDFKit | Preview, Safari, iPhone and iPad | automatically, in CI on macOS |
   | Acrobat Reader | Adobe PDF Library | Acrobat, Edge, Adobe Document Cloud | by hand, on Windows |
   | Evince / Okular | Poppler | Linux desktop viewers, CUPS printing, command-line tools | automatically, in CI and `check-examples.sh` |

   - ✅ Automated on Sep 29: the viewers job of the Build workflow opens the
     Java examples, Example_30 with its user and its owner password, and a
     PDF with a Cyrillic and one with a 200 byte password
     (`.github/scripts/encrypted-pdfs`), in PDFium (pypdfium2), pdf.js
     (pdfjs-dist in Node) and Poppler (pdftoppm and pdftotext) on Linux and
     in PDFKit on macOS, with
     `.github/scripts/check-viewers.py`: every page opens, renders and gives
     its text; none is blank or looks different from MuPDF's render, in
     blocks of 8 pixels; and it has every character MuPDF extracts. The
     renders are uploaded, a contact sheet of each PDF beside MuPDF's, to
     look at. `check-examples.sh` runs the PDFium, pdf.js and Poppler
     checks. All four pass; PDFKit first ran on Sep 29, its largest block
     difference 2.8%, on Example_13.
   - ✅ What they found: the annotations of Example_06 and the file
     attachment of Example_30 have no appearance stream (`/AP`), which PDF
     2.0 and PDF/A ask for, so each viewer draws them its own way. PDFium,
     so Chrome, draws neither file attachment icon nor the polygon, and
     draws the note icon above its place; the `/Rect` of the file
     attachments and the note has its y values the wrong way round
     ([70 617 94 593]). Fixed on Sep 29, in the four ports: every
     annotation but a link has an appearance, in every document and not in
     PDF/A alone, the shapes in their fill color and opacity, a file as its
     push pin or paperclip icon and a note as a speech bubble, drawn with
     paths in a white box with a black frame; and every `/Rect`, links too,
     is from its lower left corner to its upper right one. PDFium draws
     Example_06 as MuPDF does, and its exception in `check-viewers.py` is
     gone; PDFKit, which drew 16.7% of the blocks of its first page
     otherwise, passes it too.
     Also: PDFium does not cut a password at 127 bytes, as ISO 32000-2 asks
     of a viewer, so Chrome opens a PDF with a longer password only with its
     first 127 bytes; not ours to fix, and said in the docs of `Passwords`
     in the four ports (Sep 29). Poppler cuts a password at 127 bytes, but its
     pdftoppm and pdftotext keep only the first 32 bytes of the one they are
     given, so the check opens the 200 byte password file in them with its
     owner password; the Cyrillic password works.
   - ⬜ Still manual: Acrobat Reader, and Edge with Adobe's engine; Foxit;
     Preview itself, which the PDFKit check stands in for but does not
     click through; PAC; NVDA with Acrobat Reader, and VoiceOver; and what
     is interactive: typing the passwords, the permissions each viewer
     shows (print allowed, copy denied), the attachments, the links and the
     form fields. The CJK examples whose fonts are not embedded, Example_04
     and 44, are drawn with the fonts of each machine, so only the manual
     pass shows them as users see them.
   What the pass is for, besides finding faults: evidence for what we say
   when we sell PDF/A and PDF/UA (Sep 28).
   - PDF/A: every font and colour profile is in the file, and nothing
     outside it is needed, so a PDF looks the same in every viewer, in print
     and in 20 years. The pass shows it in each engine, as screenshots to
     keep.
   - PDF/UA: most viewers ignore the tags. A screen reader gets the
     structure mainly through Acrobat and Reader, partly through Edge's
     Adobe engine and Apple's VoiceOver. So the claim is compliance, not
     that every viewer reads a PDF aloud: the law asks for accessible
     documents (the ADA Title II rule in the US, Section 508, the European
     Accessibility Act in force since June 2025), most tools write PDFs that
     fail PDF/UA, and every PDF/UA example of ours passes veraPDF, in the
     four ports, and PAC. The pass reads one tagged example with NVDA and
     Acrobat Reader on Windows and with VoiceOver in Preview, to say which
     of them do what.
   - The wording to sell with: every PDF passes PDF/A and PDF/UA
     validation, so it looks the same in every viewer and in print, and it
     meets the accessibility law. Not "works in every screen reader", which
     is not ours to promise.

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

   And one on Sep 28, for PAC, which fails a figure with no bounding box:
   - `Page.setFigureBoundingBox`, for a figure drawn with `addBDC` and
     `addEMC` around other drawing; the figures of the library set it
     themselves.

   And more that day, for PAC and for the review of the four ports:
   - `Point.setAltDescription` and `getAltDescription`, the description of
     a linked point of a chart or a cell, a figure in its Link;
   - `Table.drawOn(pdf, first, pages, pageSize)`, in Go `DrawOnPagesFrom`,
     and `BigTable.setFirstPage(page, y)`, a table that starts under a
     heading on a page of its own;
   - `DataMatrix.setAltDescription` and `PDF417.setAltDescription`, as
     `Barcode` and `QRCode` have it.

   And one more that day, in Go only, where the constructor of the other
   ports that throws has no error to return:
   - `ReadTableFromFile`, `NewTableFromFile` that returns the error of a file
     that cannot be read, or has a quoted field that is not closed, rather
     than panic with it.

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
- ✅ **B** The release checks: `check-examples.sh` clean, the public API that
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
      v9.0.1. Run again on Sep 28, after the code review of the four ports:
      `./check-api.sh` with nothing of v9.0.1 gone and the additions of that
      day added to goal 6, and the benchmarks in
      `benchmarks/results/2026-09-28-74f21e90.log` and
      `benchmarks/table/results/2026-09-28-74f21e90.log`: Example_43 takes
      1.67 s with `BigTable` and 3.62 s with `Table`, as on Sep 24, and the
      text is as fast; the 50,000-row table is as fast in the four ports,
      and Go's PDF was 4% larger with its pages at compression level 5,
      which is now for the samples of images only.
      Left for the tag: these again on the tagged tree, the docs, the
      packages, the booklet and the site rebuilt, and the CHANGELOG entry
      dated.
- ✅ **B** Tag v9.0.2 on Oct 1 and make the GitHub release. Done three days
  early, on Sep 28: the checks of Sep 28 above, `check-examples.sh` clean at
  the tag, the API references regenerated into `docs/` for the site, and the
  release on GitHub with a summary of the CHANGELOG entry. The packages of
  `.packaging/` are the tag's: checked on Sep 30, their CHANGELOG, examples
  and Producer are those of the tagged commit. Left: the site, rebuilt from
  the tag (on the owner's go-ahead).

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
- ✅ **B** Goal 3: PAC or Matterhorn over the 41 PDF/UA examples, and the
      fixes it asks for — the last day a tagging fix can land before the
      freeze. Done on Sep 29.

### Oct 15: code freeze

- ⬜ **B** Goal 6: fixes only from here, each with its check. Whatever is
      still open moves to 9.0.4 or the AGPL major rather than into the tag.

### Oct 15–20: the checks that must hold at the tag

- ⬜ **B** Since the checks of Sep 30 (6bd05dfe), the library changed: on Oct
      1, `/Length 256` in the encryption dictionary, in the four ports
      (fb43c9b6), as Acrobat could not decrypt PDFjet's AES-256 PDFs without
      it. Run the checks of the tag again on the frozen master, and build
      the viewer files again: `viewer-files-to-test/Example_30.pdf` of Sep
      30 has not the fix. Checked in Acrobat Reader on Oct 1: Example_30
      with the passwords hello and world, and without an open password.
- ⬜ **B** Goal 4: the manual viewer pass, on the files built from the frozen
      master, in Acrobat Reader, Preview, Chrome, Firefox and Edge. The
      engines of Chrome, Firefox, Preview and the Linux viewers are checked
      by the Build workflow since Sep 29; see goal 4 for what is left.
- ⬜ **B** Goal 7: benchmarks at 9.0.3 against 9.0.2 and 9.0.1, Example_43's
      printed time among them; the JDK 8 build; the packages and the docs made
      from the tag.
- ⬜ **B** `check-examples.sh` clean in the four ports, and the public API
      still that of goal 6.
- ⬜ **B** `go test ./...` of pdfjet-server passes against the library to be
      tagged, as its `go.work`, which uses `../pdfjet`, builds it. It reads images back
      out of the PDFs PDFjet writes, which `check-examples.sh` does not: the
      PNG pass-through of Sep 28 (99486dea) broke five of its tests and went
      into v9.0.2 unseen, worked around the same day in pdfjet-server
      (dee3909).
- ⬜ Rebuild the site, and date the `## v9.0.3` entry of CHANGELOG.md.
- Keep Oct 19 and 20 empty: they are the buffer for what the checks find.

A trial run of these checks on Sep 29, at 7aa67a7a: `check-examples.sh`
clean in the four ports, the viewers and the booklet; `./check-api.sh
v9.0.2` with nothing gone, changed or added in any port; `go test ./...`
of pdfjet-server passing against master; and the benchmarks, in
`benchmarks/results/2026-09-29-e784a127.log` and
`benchmarks/table/results/2026-09-29-e784a127.log`, as on Sep 28 within the
spread of the runs: Example_43 takes 1.65 s with `BigTable` and 3.69 s
with `Table`, the text is as fast, and the 50,000-row table is as fast in
the four ports, Go's PDF 4% smaller.
Again on Sep 30, at 6bd05dfe, after the 9.0.3 Producer: `check-examples.sh`
clean in the four ports, the viewers and the booklet, in 10 minutes;
`./check-api.sh v9.0.2` with nothing gone, changed or added in any port;
and `go test ./...` of pdfjet-server passing. The files of the viewer pass
were built from that tree into `viewer-files-to-test`, for a dry run.

### Oct 21: release v9.0.3

- ⬜ **B** Go users cannot `go get` the library: proxy.golang.org lists v9.0.0,
      v9.0.1 and v9.0.2 but serves none of them (`.info` answers "not found:
      fetch timed out"), and sum.golang.org has no checksum for any, so `go
      get github.com/edragoev1/pdfjet/v9@v9.0.2` fails its checksum check
      with the default settings, for every user since v9.0.0, and so does a
      module that requires pdfjet-commercial. Found by the rehearsal of Sep 28.
      The cause, found on Sep 28: the proxy fetches only the tag, shallow
      (about 470 MB), then `git archive`s the whole tree at it, a zip of 487
      MiB, most of it `fonts/` (604 MiB uncompressed), before it drops the
      nested modules; the
      work passes the proxy's limit of about 56 s a request, and the failure
      is cached for 30 minutes. The history is not the problem, and the raw
      archive is 13 MiB under Go's 500 MiB cap, so `GOPROXY=direct` breaks too
      once the tree grows. The module zip is 4 MB. Until it is solved, users
      need `GONOSUMDB=github.com/edragoev1/pdfjet` (or `GOPRIVATE`), and
      `GOPROXY=direct` clones the whole repository. To decide: ask the Go team
      (golang/go issues), shrink what the proxy clones, or give the Go module a
      small repository of its own, which changes its import path unless a
      vanity path is set up first. Check the proxy again after each tag:
      `curl https://proxy.golang.org/github.com/edragoev1/pdfjet/v9/@v/v9.0.3.info`.
      Done on Sep 29: `fonts/` and `data/` moved to the public repositories
      edragoev1/pdfjet-fonts and edragoev1/pdfjet-data (`fonts/go.mod` and
      `data/go.mod` are gone; `images/` keeps its `go.mod`), with no history
      rewritten, and came back at the same paths as folders that git ignores
      and `get-fonts-and-data.sh` (and `.cmd`) fetches at the commits
      `fonts-and-data.txt` pins, the pinned commit alone; the build, run and
      test scripts run it. They were submodules for a day, but SwiftPM checks
      out the submodules of a package it depends on, so a Swift user
      downloaded about 1.3 GB instead of 0.8 GB; a fetched folder keeps them
      out of both. The zip of the tree is 8 MB and the module zip, made with
      golang.org/x/mod/zip from the tree, 4 MB (12 MB, 1520 files,
      uncompressed). The packaging scripts, `check-api.sh`, the workflows, the
      README and the booklet follow; a fresh `git clone` passes `test-go.sh`,
      which fetches the folders. Left: tag v9.0.3
      and check that the proxy answers 200 for its `.info` and `.zip`, and that
      sum.golang.org has its checksum. go.mod retracts v9.0.0 to v9.0.2
      (Sep 29, the owner's decision), so `go get @latest` skips the versions
      the proxy cannot serve once v9.0.3 is out.
      Checked again on Sep 29, evening: the proxy still times out on v9.0.0
      to v9.0.2, and on the untagged master too, whose pseudo-version needs
      the whole history, about 900 MB. A tag needs only its commit: fetched
      alone, v9.0.2 is 471 MB (15 s from here) and master 7.7 MB (1 s), so a
      tag on master should pass. Tagging early for the Go users was weighed
      and left for Oct 21 (the owner's decision): a tag is for good once the
      proxy has it. The tree says 9.0.3 since Sep 30: the Producer strings
      of the four ports, the ReviewWriter tests that check them and the
      DataMatrix text of Example_14 in the four. Left for the tag: the
      `exact:` pin of `.commercial/Package.swift`, and its go.mod.
- ⬜ **B** Tag v9.0.3 and make the GitHub release.
- ⬜ **B** Release v9.0.3 of the commercial product, built on the tag of the
      library, the same day: its steps are in TODO.md of its own repository.

### After the tag: the package registries (1 October 2026)

None changes the library's code; each is packaging around the tag. Free,
and the names are free: no `PDFjet` on NuGet, no `com.pdfjet` on Maven
Central (checked Oct 1). GitHub counts no downloads of a repository, only
of the files attached to a release, and its traffic shows 14 days and keeps
no history; the registries count, and are where developers look.

- ⬜ **Go**, at the tag: nothing to register. Fetch
      `https://proxy.golang.org/github.com/edragoev1/pdfjet/v9/@v/v9.0.3.info`
      once, as above, and pkg.go.dev lists the module within minutes, with
      its docs, its license and who imports it; no download counts.
- ⬜ **NuGet**, first, as C# is the largest audience of the library: an
      account at nuget.org and an API key; `PDFjet.csproj` given its package
      metadata (PackageId `PDFjet`, the version, the license expression
      `MIT`, the README, an icon); `dotnet pack` and `dotnet nuget push`.
      About an hour; its download counts are public. Ask NuGet to reserve
      the prefix `PDFjet.*`, so that only we publish packages of that name.
- ⬜ **Swift Package Index**: the repository's URL added by its form or a
      pull request; it shows the docs and the platforms, no downloads.
- ⬜ **Maven Central**, after the others: an account at central.sonatype.com;
      the namespace `com.pdfjet` proved by a TXT record in pdfjet.com's DNS at
      IONOS, which can take a day to be seen; a GPG key, published to a
      keyserver and kept for good with the other secrets. Each release is a
      bundle of the jar, a sources jar, a javadoc jar and a POM (name,
      description, URL, license, developers, SCM), each signed and with its
      checksums, uploaded by the Portal's API. The Java port builds with
      scripts, not Maven or Gradle, so a script makes the bundle; javadoc
      that fails its checks is the likeliest work. Half a day to a day the
      first time, one script after.

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

- ✅ **A long word takes minutes to break across lines.** `appendBrokenWordLines`
  in textblock.go measured the whole rest of the word for every line it
  broke it over, and copied the word up to each break to measure it, so the
  time grew with the square of the word: a word of 20,000 characters in a
  full-width block took 1.6 s, 100,000 took 39 s, and 2 million in a narrow
  column had not finished after 5 minutes. Normal text was fast, 2 MB of
  words in 0.4 s. Found by a review of pdfjet-server on 25 September 2026,
  and fixed the same day in the four ports: each line is found by measuring
  the word from the line's start to each character break until one does not
  fit, and the rest of the word is measured only when the breaks run out, so
  a word is measured about once a line's worth of characters a line; the word
  of 100,000 characters breaks in 0.02 s. The lines are the same as before,
  checked in Go against the old code on Latin, combining marks, Thai and
  Arabic, and in the four ports by the height of the block. A right-to-left
  word was still reordered and shaped up to each break, 27 s for 16,000
  Arabic letters and about 20 minutes for 100,000; it is reordered and shaped
  once, whole, and each line is a part of it, measured as the sum of the
  widths of its characters, so the letter a line ends on keeps the form it
  has in the word. pdfjet-server keeps its limit of 1000 characters a word
  (`checkText`, layout.go).

- ✅ **Reading a PDF decodes every stream in full, with no total.** `Read` in
  pdf.go inflated every Flate stream as it read, capped at 256 MB each by
  `decompressor.MaxDecodedLength` and not at all in total: a PDF of 8 MB, 40
  streams of 203 KB that each inflate to 200 MB, took 32 GB to read. Found by
  the same review, and fixed the same day in the four ports, in two parts.
  A stream that is not a cross-reference or an object stream is decoded when
  its data is first asked for, `GetData`, not when the PDF is read, so that
  reading a PDF of large images takes the memory of none of them; a stream
  that cannot be decoded has no data, as before. And all the streams of one
  PDF may decode to 256 MiB together (`maxDecodedTotal`, a budget the objects
  of one `Read` share): an object stream or a cross-reference stream past it
  is the error "the streams of the PDF decode to more than N bytes together",
  as the PDF cannot be read without its objects, and any other stream past
  it has no data. pdfjet-server keeps its checks before reading, the name of
  the layout in the PDF and 20 MB at most.

- ✅ **An image is decoded whole to give its size.** `NewImage` decodes the
  pixels of a PNG or a JPEG to know its width and height, 256 MB for a PNG of
  16,000 by 16,000, which pdfjet-server did three or four times a request.
  Found by the same review. Measured on Sep 28, after the IDAT of a PNG is
  embedded as it is: a JPEG was never decoded, its header is read to the
  frame header, 0.2 ms for 36 megapixels; an opaque PNG is inflated once, to
  check the filter type of each row and that the data is the rows and nothing
  more, 100 ms for 36 megapixels of RGB; a PNG with alpha is decoded, as its
  alpha is a soft mask of its own, 670 ms. That is what embedding the image
  takes, so the library is not changed, and a size-only reader is new API,
  a maybe for v9.1. Fixed in pdfjet-server the same day: a request reads
  each image once for its size and its fingerprint, and once more to draw it,
  two reads where it made three, or four for a form (`imageReads`,
  xobject.go); a form with a PNG of 36 megapixels takes 0.36 s in place of
  0.61 s, and 1.75 s in place of 3.25 s with alpha. It still refuses an image
  of more than 40 megapixels from its header (`checkPixels`, layout.go).

- ✅ **`PDFobj.GetData` of an image leaves the PNG predictor in place, and an
  /Indexed image in its indexes.** `applyDecodeParms` in pdfobj.go skips a
  stream of `/Subtype /Image`, "as they are copied with their stream, and
  their data is not used", so the data of an image with `/DecodeParms
  <</Predictor 15 ...>>` is its rows each with the filter type and the
  filter of its PNG row, and of an /Indexed image its packed indexes. Since
  99486dea embeds the IDAT data of an opaque PNG as it is, and a palette PNG
  as /Indexed on RGB, that is the data of every such image PDFjet writes:
  pdfjet-server's fingerprint of an image, and the PNG it makes again from
  it, read 32 bytes more for a 32 by 32 gray image, and the fingerprints of
  forms signed before no longer matched. Found Sep 28, by five tests of
  pdfjet-server. Worked around the same day in pdfjet-server, which undoes
  the predictor and expands the palette to RGB of eight bits itself
  (`imageSamples`, xobject.go), and gets the fingerprints of before for
  every PNG of PngSuite. The predictor is fixed, the same day, in the four
  ports, with a test in each: `GetData` of an image undoes it as it does for
  any other stream, the PNG predictors and the TIFF one. It was skipped to
  save the time of undoing it on read, when every stream was decoded as the
  PDF was read; a stream is decoded now when its data is asked for, and
  nothing in the library asks for the data of an image: `Image` from a
  `PDFobj`, `merge`, `addObjects` and `addResourceObjects` copy its stream
  and its `/DecodeParms` as they are. pdfjet-server no longer undoes the
  predictor. The samples of an /Indexed image in its colors are not what
  the stream is, so they are not `GetData`'s to give: that is new API, and
  waits for v9.1, below.

- ✅ **`Color.transparent`, -1, was drawn white by most color setters;
  fixed for v9.0.3, in the four ports, on Oct 5, 2026, on the owner's word.**
  A color is an int, 0xRRGGBB, and the setters kept its low 24 bits, so -1,
  the value of `Color.transparent`, was 0xFFFFFF: `Line.SetStrokeColor(-1)`
  drew a white line. Of the 36 int-color setters, 13 left the color as it
  was for it, as their comments said (TextLine, TextBlock, Cell, BigTable,
  Table's alternate row, Rect's border, TextFrame); the other 23 drew it
  white: Line, Path, Arc, Point and Series strokes, Rect, Arc, Point, Stamp
  and annotation fills, Page's pen and brush, Paragraph's text, Form's
  label and value, Markup's links, CheckBox, Container and Table cell
  borders, and the grid lines of Chart and BarChart. Each of the 23 now
  leaves the color as it was, says so in its comment, and a test in each
  port sets it red, then transparent, and finds it red (22 of the 23 failed
  before; Paragraph's passed, as TextLine already ignored it). Any other
  value keeps its low 24 bits, as documented: 0xFFRRGGBB, a negative int
  in Java and C#, draws as before. No caller in the library passed -1 to
  them; pdfjet-server tests for transparent before each call. Found by the
  review of Sep 30, 2026.

- ⬜ **The four ports read SVG with four XML parsers, which may take
  different files; for v9.1, in the four.** Go reads it with encoding/xml,
  Java with StAX, C# with XmlReader (its DTD ignored) and Swift with a
  reader of its own, so a file one port draws another may refuse. What Go
  refuses, proved by pdfjet-client's `check-svg.sh` (89 cases in its
  `tests/svg/cases.json`), where the editor follows it: XML of version 1.1;
  an encoding declared other than UTF-8, "ISO-8859-1" and the like, as the
  decoder has no CharsetReader; a processing instruction with no target; a
  name of an element or an attribute that starts with a digit, or has a
  character a name may not; bytes that are not UTF-8, but in comments and
  processing instructions, which it reads past; an entity XML does not
  define, `&nbsp;`; and the entities of a DTD, as older files of Illustrator
  have them, `<!ENTITY ns_svg "http://www.w3.org/2000/svg">`. To do, in
  the four, with a test:
  - **Decided on Sep 30, 2026: one parser for SVG and for the invoices.**
    The XML parser of ZUGFeRD and Factur-X (`XMLParser` of the invoices,
    written alike in the four ports, hardened and fuzzed: no DOCTYPE, the
    five entities of XML and the numeric ones alone, a depth bounded without
    recursion, 20 MB at most) moves into the MIT core, and SVGImage reads
    with it in the four, in place of encoding/xml, StAX, XmlReader and
    Swift's own reader; the invoices then use it from the core. Released
    under MIT, as it is a plain XML parser: what is paid for is the
    invoices, not it.
  - The cases of `tests/svg/cases.json` read by each port's tests, and what
    each takes and refuses compared, as the fixtures of all four; with them
    the fuzz tests of the parser, and of SVGImage through it.
  - Its behavior written down once, the refusals of the list above among
    it, in words alike in the four.
  - To weigh after, as real files have them: an encoding declared of
    Latin-1 or Windows-1252, and the internal entities of older Illustrator
    files, which a parser that refuses a DOCTYPE refuses; if taken, text
    entities alone, no external ones, and their expansion bounded, so that
    no file of nested entities runs the reader out of memory.
  - pdfjet-server's `svg_test.go` and pdfjet-client's `SVGImage.ts`, which
    follows Go, then follow the parser, and `check-svg.sh` says where they
    differ.
  pdfjet-server draws with the Go port, and the editor refuses what it
  refuses, so nothing is to work around meanwhile. Found by the review of
  Sep 30, 2026.

- ⬜ Maybe: **check once that positions are the same on arm64.** Go's
  compiler fuses a float32 multiply and add into one instruction on arm64
  (FMADDS, FMSUBS), rounding once where amd64 rounds twice, so a position
  could differ in its last bit, about one part in ten million, and a
  comparison exactly at a boundary, as of a line that just fits, come out
  the other way. Seen in the arm64 assembly of `TextBlock.layout` and
  `Cell.GetHeight`; no difference seen on a page. Not a fix: run the
  examples' comparison on arm64 once (a GitHub arm64 runner); only if a
  PDF differs, an explicit `float32(...)` round each product, which the Go
  spec says keeps it from being fused. pdfjet-server runs on Lambda's
  arm64 and checks its own the same way (its TODO.md). Found by the review
  of PDFjet Forms, 5 October 2026.

- ✅ **A JPEG cut short is taken.** `NewImage` read a JPEG's header for its
  size and embedded its bytes as they were, so a file cut short in its
  upload was drawn as far as it went, or as noise, and veraPDF, which does
  not decode the image, passed the PDF. Fixed for v9.0.3 on 5 October 2026,
  in the four ports, with a test in each: the end-of-image marker must
  follow the header of the scan (a thumbnail's own before the scan does not
  count, and data after the end is left), else "Error: The JPEG is cut
  short: its image data has no end." Found by the review of PDFjet Forms.

- ⬜ **A cell's wrapped lines are read as paragraphs of their own** (the
  second part, the break, fixed for v9.0.3 on 5 October 2026, below).
  `wrapCellText` (table.go) makes each line of a cell that wraps a P of its
  own in the structure tree, so a screen reader reads one cell as several
  paragraphs; and a word wider than its column is broken where it reaches
  the edge, even where a space follows soon after
  ("Superc / alifragi / listic e / xpialid / ocious"). veraPDF and PAC do
  not flag it. The cell one P, its lines the content of it, and a line
  broken at a space where there is one, in the four ports, with a test.
  The second, seen by everyone, was fixed for v9.0.3 on 5 October 2026, in
  the four ports and pdfjet-client's SVGTable.ts, before PDFjet Forms
  launches, whose look of a form is kept after: a word wider than its
  column starts a line of its own. Left, for v9.1: the cell one P. Found by
  the review of PDFjet Forms, 5 October 2026.

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
- ⬜ Maybe: the EXIF orientation of a JPEG, in the four ports. A photo
  taken with a phone is stored as the sensor saw it, with a tag that says
  how to turn it, and is drawn sideways or upside down. The orientation is
  read from the APP1 segment, and the image is drawn turned or mirrored with
  its matrix, its width and height swapped for a quarter turn; the samples
  are not changed. A medium feature, with test images of the eight
  orientations; found by the code review of Sep 28.
- ✅ Done on 5 October 2026, ahead of the one XML parser, on the owner's
  word: `viewbox` read when there is no `viewBox`, in the four ports and
  SVGImage.ts, a test in each, and two cases of check-svg.sh. As it was:
  An SVG's `viewbox` in lower case, in the four ports and pdfjet-client's
  SVGImage.ts at once, as check-svg.sh compares them. SVGImage reads only
  `viewBox`, as XML has attribute names case-sensitive, so a file that writes
  `viewbox` is drawn with no viewBox at all, silently: the size of its width
  and height, its paths neither scaled nor moved. Such files are common, as
  an HTML page's parser takes `viewbox` for `viewBox` in an inline svg, and
  files saved from web pages keep it. Found in Example_33's europe.svg, whose
  `viewbox="0 0 1000 684"` was ignored and whose map stood off the middle of
  the page; the file was fixed instead, with `viewBox` (98d13f70, 4 October
  2026). The fix: read `viewbox` when there is no `viewBox`, as the HTML
  parser does, and `viewBox` first when a file has both; a test of each, with
  a viewBox that scales and moves the paths. Small. Done in the same pass as
  the one XML parser for SVG, above, after the switch: the parser keeps
  `viewbox` and `viewBox` apart, as XML's names are case-sensitive, so the
  fix is in SVGImage, and `check-svg.sh` checks both at once.
- ⬜ Maybe: smaller tagged tables, with object streams, in the four ports. A
  cell of a table of PDF/UA costs 280 to 415 bytes in the PDF, its text and a
  structure element of its own, each an object written uncompressed: a price
  list of 3,000 rows of 6 columns is 4.7 MB, past the 4 MB PDFjet Forms can
  send, with no image in it (measured by pdfjet-server on 5 October 2026).
  PDF 1.5's object streams, the structure elements compressed together, and
  a cross-reference stream would shrink them several times. Found by the
  review of PDFjet Forms; pdfjet-server meanwhile
  refuses such a table before it is drawn, and says to make it shorter.
- ⬜ Maybe: a size-only reader of an image, in the four ports: the width and
  the height a PNG, a JPEG or a BMP is drawn at, read from its header, the
  IHDR and pHYs chunks, the SOF and JFIF segments, without embedding it, for
  a layout that places images before it draws them. New API, so after the
  freeze of 9.0.2; pdfjet-server has no need of it, as it embeds each image
  once for its fingerprint anyway. Apart from it, the check of the rows of an
  opaque PNG could inflate them a piece at a time, not whole: in Go the
  buffer grows to about twice the rows, 250 MB for 108 MB of them, which
  the image does not need, as its IDAT data is embedded as it is.
- ⬜ The decoded samples of an image read from a PDF, in the four ports:
  the colors of an /Indexed image, as `GetData` gives its indexes, the
  stream being its indexes. pdfjet-server expands the palette to RGB of
  eight bits itself (`indexedToRGB`, xobject.go), for the fingerprint of an
  image and the PNG it makes again from it. New API, so after the freeze of
  9.0.2; see the item on `PDFobj.GetData` above, whose predictor part was
  fixed on Sep 28.
- ⬜ `Table.SetCellPadding(side, vertical float32)`, in the four ports: the
  padding of every cell of the table, at its sides and over and under its
  text, as `SetCellBorders`, `SetCellBorderColor` and `SetCellBorderWidth`
  set the borders of every cell. Today it is set cell by cell, with
  `Cell.SetPadding` or one side at a time, as pdfjet-server's newTable does
  for PDFjet Forms, 4 points at the sides and 3 over and under, since a
  table whose cells have borders looked cramped at the default (5 October
  2026). Opt-in: NewCell's 2 points on every side stay the default, as
  changing it would make every row of every customer's table taller and
  move its page breaks. The rows added under a wrapped cell keep their own
  padding as now: none over them, NewCell's under them. A test in each port,
  the rows' heights compared across the ports. Small.
- ⬜ Maybe: a faster Deflate for Swift, which has its own, written in
  Swift. After the review of Sep 28 it is Swift's main cost for a PNG that
  is decoded and compressed again, one with transparency: about 500 ms for
  a photo of 3000 by 3993 pixels. Its time goes to the work per byte, the
  hash, the tokens and the Huffman codes, not to the search for matches, so
  its one setting, the chain of 32, barely changes it; a faster one needs a
  design of its own, as zlib's levels 1 to 3 have. A large job, worth it
  if a customer draws such images from Swift.
- ⬜ Maybe: OEM12x20 in pdfjet-fonts. The 12 by 20 pixel font of code page
  437, drawn from scratch in the late 90s and MIT-licensed
  (github.com/edragoev1/OEM12x20), converted to OTF: each pixel a square, with
  FontForge, and a map from CP437 to Unicode, so that the text of a PDF can be
  extracted and read aloud. With one example: a report in the style of DOS,
  with tables drawn in box-drawing characters. No new library API: it is used
  as any bundled font is. Small value: JetBrains Mono and IBM Plex Mono cover
  monospaced text, and 256 characters are few; it suits legacy text reports
  and text-mode art made PDFs that look like the original.
- ⬜ Maybe: Andika in pdfjet-fonts, its four styles, with their .stream files
  (SIL, https://software.sil.org/andika/, SIL Open Font License, as IBM Plex
  and Noto). A font designed for legibility: I, l and 1 clearly apart, b, d,
  p and q not mirrors of one another, single-storey a and g, open shapes and
  generous spacing, and Latin, Cyrillic, Greek and phonetic letters. Pleasant
  to read for anyone, so a candidate for a default reading font, of Forms and
  of the booklet, not only an option. Where it would add value to PDFjet:
  1. The accessibility story. PDFjet sells PDF/UA, and a font designed for
     legibility is the natural companion. Tagging makes a document readable
     by a screen reader; Andika makes it readable by people who read it
     themselves. "Accessible structure and an accessible font" is a stronger
     pitch than PDF/UA alone.
  2. PDFjet Forms. Forms for public services, schools and healthcare are
     exactly where clear letters matter. It could be a font choice in the
     editor, something like "Clear (Andika)".
  3. Education and children's material: worksheets and reading material,
     where the single-storey a and g matter.

  With one example, a reading worksheet or a form tagged for PDF/UA, which
  passes the viewer checks as every example does. No new library API. Its
  size does not matter: pdfjet-fonts is fetched apart from the library, and a
  document embeds what it uses.

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
