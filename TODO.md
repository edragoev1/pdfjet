# PDFjet — the plan to Oct 21

Target: **2026-10-21**, the date v9.0.0 was planned for. v9.0.0 was released
early, on 2026-09-16 at e957f841, and v9.0.1 on 2026-09-18 at 72c41923, so
what Oct 21 carries is **v9.0.3**: a foundation to build on after it, and
one feature, `Cell.setRowSpan`, which the work was far enough ahead to fit in
(Sep 21). Today is Sep 21, so 30 days are left, with one fix release in
the middle. This file is the working list; tick items off as they land on
master.

The work runs ahead of the calendar below. Goals 1 and 2 are closed: every
class of the library is read end to end, three weeks before the Oct 9-14 week
that was to finish it. What is left before the Oct 1 release is the release
checks.

What is done is in CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## The three dates

- **Oct 1** — v9.0.2, a fix release from master on the v9.0.1 API. What is in
  it is under `## Unreleased` in CHANGELOG.md: the PDF/UA examples and the
  table tagging, kinsoku, the font line gap, the WinAnsi characters and
  kerning of the core fonts, the fallback font rule, the CMYK JPEG marker,
  the font stream, PNG, BMP, JPEG, SVG, OpenType, decompressor and `PDF.read`
  fixes that fuzzing found, and the reviews of the image classes, `Page`,
  `TextLine` and the reader.
- **Oct 15** — code freeze: fixes only, each with its check.
- **Oct 21** — v9.0.3.

## Why 9.0.3 is hardening and not features

The bugs found from Sep 17 to 19 were almost all in code nobody had read end
to end, and almost all were silently wrong output that every test and example
check passed: 23 in the reviews of `Cell`, `Table`, `TextBlock`, `TextColumn`
and `PDF`; then 32-bit BMPs in the wrong colors, ’ and € drawn as spaces in
the core fonts, every CMYK JPEG inverted, a fallback font that stuck, the CJK
line gaps, and what the first three fuzz targets turned up. Eight kinds of
untrusted input are read from a file, and five of them are fuzzed. So the
work to Oct 21 is the seven goals below, in this order.

Five features are the exceptions, and all are in v9.0.2. The first is
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
shared from the width of the table. The fifth, the same day and the last,
is a `TextFrame` that flows onto as many pages as its text needs. The other
members master adds to the API of v9.0.1 came with the PDF/UA work and the
fixes; goal 6 lists all 25.

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
   public API is that of v9.0.1 and the 25 members master adds to it, the
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

   Nothing is removed and no signature changes. Swift's `Alignment` has an
   `init(rawValue:)` since its cases are the numbers a `Cell` packs, and Go's
   `internal/utf8text.Decode` is not importable, so neither is API. Any other
   difference fails the check. The API diff lists each port's public
   declarations at the tag and at v9.0.1: `javap -public`, `go doc -all`,
   reflection over the C# assembly, and the Swift symbol graph.

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

### Sep 27–Oct 1: release v9.0.2

- ✅ **B** Goal 2: the review of `Font` and its loaders. Done on Sep 21.
- ⬜ **B** The release checks: `check-examples.sh` clean, the public API that
      of goal 6 in the four ports, the JDK 8 build, the benchmarks recorded
      against 9.0.1 with Example_43's time, the docs, the packages and the
      site rebuilt, the CHANGELOG entry dated. 9.0.2 is cut from master, so it
      carries the 25 members of goal 6 and the fixes under `## Unreleased`.
      Done on Sep 22, to be run again at the tag: the API diff, the JDK 8
      build, and the benchmarks, in `benchmarks/results/2026-09-22-*.log` and
      `benchmarks/table/results/2026-09-22-010c7c21.log`. They found `Table`
      measuring every row on every page and Java's `BigTable` reading its
      file a byte at a time, both since v9.0.1 and both fixed. Example_43 is
      as fast as at v9.0.1 with `BigTable` (1.66 s) and 8% slower with
      `Table` (3.76 s); `Table` in the four ports is 5 to 22% slower than on
      Sep 18, spread over the tagging, the fallback font of each character and
      the row spans, with no one place that costs it.
- ⬜ **B** Tag v9.0.2 on Oct 1 and make the GitHub release.

### Oct 2–8: the reader

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

## Markdown to PDF, on the branch markdown

- ⬜ A practical subset of Markdown to PDF, in the four ports, built on the
  branch `markdown` with the checks master has: headings, paragraphs with
  the inline markup of `Markup`, bullet and numbered lists that nest, block
  quotes, fenced code blocks, thematic breaks, GitHub tables and images,
  flowed down the pages and tagged for PDF/UA. It is merged into v9.0.3 on
  **Oct 8** if it is solid by then, with its tests, fuzz targets and
  examples; otherwise master stays as it is and it ships in v9.1. It is
  called Markdown, not CommonMark: CommonMark's 652 examples are the goal
  of v9.1 below.

## v9.1 — features, after v9.0.3

- ⬜ Markdown to PDF, in the four ports: a CommonMark parser written from
  the spec, with the GitHub tables, strikethrough and task lists, and a
  renderer that draws the document with PDFjet's components, headings,
  paragraphs of mixed styles, lists, code blocks and tables, tagged for
  PDF/UA, which few Markdown to PDF tools are.
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
  - About a week for the first port to pass the spec, a few days for each
    of the others, and the renderer after.

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
