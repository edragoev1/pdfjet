# PDFjet — the plan to Oct 21

Target: **2026-10-21**, the date v9.0.0 was planned for. v9.0.0 was released
early, on 2026-09-16 at e957f841, and v9.0.1 on 2026-09-18 at 72c41923, so
what Oct 21 carries is **v9.0.3**: not new features but a foundation to build
on after it. Today is Sep 21, so 30 days are left, with one fix release in
the middle. This file is the working list; tick items off as they land on
master.

The work runs ahead of the calendar below. Goal 1 is closed, and of goal 2
the image classes, `Page`, `TextLine` and the whole reader are reviewed --
the reader eleven days before the Oct 2-8 week the plan gave it, and the
`Font` loaders, which the Sep 27-Oct 1 week had. What is left before the
Oct 1 release is the release checks.

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

## The seven goals of v9.0.3

1. ✅ **B** Fuzz the parsers of untrusted input with Go's fuzzing, and fix each
   failure in the four ports, which share the logic. Any input either works or
   fails with a clean error: no hang, no index error, no runaway memory. The
   targets and their corpora stay in the repository, in `src/*_fuzz_test.go`
   and `src/testdata/fuzz`.
   - ✅ The `.stream` fonts (Sep 19): `FuzzFontStream` and
     `FuzzFontStreamMetrics`, 1.1 M and 0.9 M runs clean after the fixes.
     Found lengths from the file allocated before reading (4 GB for a 9-byte
     file), metrics and marks read past their end, no check of the units per
     em, the character range, the tables or the name, and `embedFontFile2`
     looping on a read error; and, in a shipped font, IBM Plex Sans JP mapping
     15 arrows past its advance widths. Fixed in Java, C# and Swift with unit
     tests; Swift also trapped on a name or license that is not UTF-8, a
     negative length and an `Int16` out of range. The 873 inputs of the Go
     corpus replayed in the four ports: no crash, and Java and C# accept and
     reject the same inputs as Go.
   - ✅ PNG (Sep 19), against Go's `image/png`, and BMP (Sep 19), against
     Pillow; both fixed in the four ports.
   - ✅ JPEG (Sep 20), against Go's `image/jpeg`: `FuzzJPGImage` fuzzes whole
     files, and every header image/jpeg reads gives PDFjet the same width,
     height, color components and Adobe marker; 62 M runs clean after the
     fixes. Found the markers with no parameter segment — a restart marker,
     TEM, a nested SOI and the 0xFF 0x00 of a stuffed 0xFF byte — read as if
     they had a length, which skipped over the frame header; and an APP14
     segment taken for Adobe's on its first five bytes, unmarked by an APP14
     of another kind after it, or missed when it follows the frame header,
     where libjpeg's `jpeg_read_header` reads to the scan. Fixed in the four
     ports with unit tests; the 678 inputs of the Go corpus replayed in the
     four ports read the same width, height, components and Adobe marker from
     every one. The review beside it found a JPEG of other than eight bits per
     color component embedded as if it had eight, which is refused now.
   - ✅ SVG paths and `SVGImage` (Sep 20): `FuzzSVGImage` fuzzes whole
     documents, and `FuzzSVGPath` the path data, which it writes twice out of
     the same numbers, plainly and as the formats of the input give them —
     12, 12.0, 1200e-2, +12, with a space, a comma or nothing before them, and
     the flags of an arc with or without a separator after them — and the two
     must draw the same picture; 8.6 M and 26.3 M runs clean after the fixes.
     Found path data written over more than one line read as one number, where
     Go's XML parser leaves the line feed, a number written with an exponent
     read as two, and the flags of an elliptical arc written with no separator
     after them, as minifiers write them, taken for one number with what
     follows, which dropped the arc; and, in the other three ports, a throw or
     a trap on path data that starts with a number, on a first command that
     needs a current point, on a `<path>` with no `d`, and, in Swift, on an
     argument that is not a number. Replaying the corpus in the four ports
     found two more: an arc of a whole number of quarter turns drawn with one
     curve more in one port than in another, since they do not round `atan2`
     alike, and a number a PDF cannot hold left out of the content stream in
     Swift, where the other three write 0 and keep the operator's operands.
     Fixed in the four ports with unit tests; of the 2,897 documents of the
     replay — the corpus of both targets, and the 50 icons — every one the
     ports read draws the same content in all four, and none crashes. What
     they still differ on is which malformed XML their parsers accept.
   - ✅ OTF and TTF, the `Font` loaders (Sep 21): `FuzzOpenTypeFont` fuzzes a
     whole `.otf` or `.ttf` file, and `FuzzOpenTypeFontTables` each table
     PDFjet reads on its own, joined into a font whose directory is right, so
     that a change reaches a parser rather than moving the offsets of the
     other tables; 21 M and 8.5 M runs clean after the fixes. Found every
     read of a font unchecked, so that a truncated file, or a table of the
     directory pointing outside the file, read past its end; a character map
     that is not there followed; a glyph ID array of a negative size; a
     character map segment read outside that array; a name record read
     outside the font; a CFF table read past the end of the file to be
     embedded; units per em of 0 divided by; no advance widths indexed at -1;
     and a GPOS table that says it holds more lookups, subtables or coverage
     glyphs than any font does taking every byte of memory there is. Fixed in
     the four ports with unit tests; the mark lookups are read for the work a
     font needs, of which the 252 fonts PDFjet ships take at most 62,954 of
     the 1,048,576 allowed. Replaying the corpus in the four ports found two
     more: a font with no name, which Java failed on with a null pointer and
     which is refused in all four now, as a stream font with no name is, and
     a font with no `OS/2` or `post` table, which Swift trapped on and the
     other ports read as zeros. Of the 2,181 fonts of the replay the four
     ports read the same ones, and draw the same page from every one they
     read.
   - ✅ The decompressor (Sep 21): `FuzzDecompressor` runs every filter of a
     PDF stream -- Flate, LZW, ASCIIHex, ASCII85, RunLength and the predictor
     of a /DecodeParms -- on the same bytes, and `FuzzDeflateRoundTrip`
     checks that what PDFjet writes compressed reads back as the same bytes
     and that a prefix of it is its first bytes; 46.5 M and 12.9 M runs
     clean. The Go decoders, which carry the 256 MiB limit and the parameter
     guards from the review, took every input. Replaying the corpus in the
     four ports found one: Swift, whose decoder is a port of `puff.c` and not
     a library, decoded the symbol after the bytes a prefix asked for, so a
     stream cut short right after them failed there and read in the other
     three. Fixed with a unit test in the four ports. Of the 1,066 streams of
     the replay every port now decodes every one to the same bytes with every
     filter.
   - ✅ `PDF.read`: the xref, the object streams and the encryption (Sep 21).
     `FuzzPDFRead` fuzzes the bytes of a PDF and the password it is read with,
     and reads, pages and merges what it gets; its seeds are documents PDFjet
     writes, the three PDFs of `data/testPDFs` that other programs wrote, and
     one built for it with a cross-reference stream and an object stream,
     which PDFjet reads and does not write and no test covered; 95 M runs
     clean after the fixes. Found an object numbered higher than the file has
     bytes making one empty object of every number up to it, which took 7 GB
     for a file of 31 bytes; a /Length longer than the file allocated before
     its bytes were found not to be there; an object stream with no stream of
     its own, and one whose header is not a number, failing differently in
     each port; and `getValue`, `getObjectNumbers`, `getContentObject` and
     `getResourcesObject` reading past the end of the tokens on a dictionary
     that ends where a value belongs or a reference to an object that is not
     in the file. Fixed in the four ports with unit tests. Of the 1,257 PDFs
     of the replay the four ports read the same ones and draw the same pages
     from every one. Beside the review of the reader, later on Sep 21, the
     target was made to fail on a Go runtime error, which
     `ReadWithPassword` recovers from and returns like any other error, so
     that every index error of the reader was invisible to it; and to drive
     what a merge, a split and a stamp do with what was read. It found a
     hang in a second; 56 M runs are clean after the fixes. Of the 1,481
     inputs the corpus held then, none crashes or traps in any port, and all
     four read the same pages from the 1,076 whose password is valid UTF-8.

2. ⬜ **B** Finish the class-by-class review, as on Sep 17: one class end to
   end in the four ports, each finding proved by running it, fixed in the four
   ports with a test, and the example pages it touches rendered before and
   after. By exposure, and in this order: ✅ `Page` and `TextLine`; ✅ `Image`,
   `PNGImage`, `JPGImage`, `SVG` and `SVGImage`; ✅ `Font` and its loaders
   `OTF`, `OpenTypeFont`, `FontStream1` and `FontStream2`, which no test
   names;
   ✅ the reader, `PDF.read`, the merge and split, `PDFobj` and `Decryptor`,
   which no test names; ✅ `TextFrame`, `BigTable`, `CompositeTextLine` and
   `Bidi`; the barcodes, the charts, `Form`, `Container` and `Stamp`.

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
   is left for this goal:
   - ✅ A paragraph of a `TextColumn` or a `TextFrame` is one structure
     element of the type `Paragraph.setStructureType` gives it (Sep 21). Each
     was drawn word by word and every word was an element of its own, so a
     reader read each word as a paragraph: Example_10 had 240 of them for 7
     paragraphs and Example_03 had 48 for 26. The titles of Example_03 and
     Example_10 are headings now. A `Chart` needed nothing: it is one Figure
     with an alternate description, and its title belongs inside it.
   - ✅ The contents of Example_22 are tagged as a list (Sep 21), which no
     example did: an `L` of an `LI` for each chapter, each with the `Lbl` of
     its number and the `LBody` of its title and the link to it.
     `Page.beginStructElement` and `endStructElement` group what is drawn
     between them, which nothing could do before, and `LBody` was missing
     from `StructElem` in all four ports. The numbered paragraphs of
     Example_03 are two lists of their own: their numbers were drawn in a
     second pass, after all the text, and would have read after it, so
     `Paragraph.setListLabel` gives a paragraph the label of its item and a
     `TextFrame` draws it where the item begins. The example draws what it
     drew, to the point.
   - ✅ A tagged `BigTable` held every structure element until the document
     was written, so the 29 MB data file of Example_43 took 778 MB of heap
     (Sep 21). The elements of a page are written with the page now, and a
     cell holds its text rather than a paragraph under it: 86 MB, against 13
     MB for the same document untagged, and the file went from 384 MB to
     249 MB. What is left is one cross-reference entry per object, which
     every object of a PDF has.
   - ⬜ PAC itself, or the Matterhorn conditions that need eyes on a page:
     the reading order of each page, whether a colour alone carries meaning,
     and whether each Alt says what its figure shows.

4. ⬜ **B** The manual viewer pass, open since 9.0.0: Acrobat Reader on
   Windows opens Example_30 with `hello` and `world`, shows print allowed and
   copy denied, opens the Cyrillic and the 200 byte password files, and opens
   the PDF/UA and PDF/A examples without warnings. The same files in Preview,
   Chrome (pdf.js), Firefox and Edge — not only veraPDF and MuPDF.

5. ⬜ **B** Test the output against independent references and real files,
   which finds the silently wrong output that fuzzing does not:
   - Round-trip text: the text of every example in every port, extracted with
     MuPDF and pdftotext, equals the strings the example drew. It would have
     caught the core font spaces, .notdef glyphs and a wrong fallback font.
     `.github/scripts/check-example-pdfs.py` extracts the text spans already.
   - Images against Pillow: a corpus of PNG, JPEG and BMP files, real ones
     from GIMP, Photoshop, Paint and phone cameras as well as made ones,
     decoded to the pixels Pillow decodes them to.
   - Fonts against fontTools: the widths, character map, ascent, descent, line
     gap and kerning of every font PDFjet ships and a few popular others. The
     252 fonts of the repository are done, but by hand, beside the review of
     the loaders on Sep 21: what is left is the kerning, the other fonts and
     making it a check that runs.
   - The reader against real PDFs: the pdf.js and veraPDF test corpora read,
     merged and split, with the page counts, page sizes and text MuPDF finds.

6. ⬜ **B** Freeze the API and the behavior. After 9.0.2, fixes only; the
   public API of v9.0.1 in the four ports, checked as for 9.0.1 before each
   tag.

7. ⬜ **B** Guard against regressions: benchmarks recorded at 9.0.2 and 9.0.3
   against 9.0.1, Example_43's printed time among them, and the JDK 8 build,
   the packages and the docs made from the tag.

## The calendar

Four and a half weeks. The reviews and the fuzzing run first because they
change code; the checks that must hold at the tag run after the freeze.

Where it stands on Sep 21: the Sep 20-26 week is done, the review of the
`Font` loaders that the Sep 27-Oct 1 week had, two of the three blockers of
the Oct 2-8 week -- the review and the fuzzing of the reader -- and the fonts
of goal 5, which the Oct 9-14 week had. That buys more than a week. It goes
to the work that has to be done by hand and cannot be hurried at the end --
goal 4, the viewer pass, and goal 3, the Matterhorn conditions that need eyes
on a page -- and to the rest of goal 5, the references, which is the first
thing to give if anything does.

### Sep 20–26: the decoders, `Page`, `TextLine` and the reader

- ✅ **B** Goal 1 is done: the eight targets of the `.stream` fonts, PNG,
      BMP, JPEG, SVG, OTF and TTF, the decompressor and `PDF.read` are in
      `src/*_fuzz_test.go`, and every failure they found is fixed in the four
      ports with a test.
- ✅ Goal 2: `Image`, `PNGImage`, `JPGImage`, `SVG` and `SVGImage` are
      reviewed (Sep 21). Four findings fixed in the four ports with tests: the
      length of a JPEG frame header against the number of components, as
      libjpeg checks it; the compression and the filter method of a PNG IHDR,
      as libpng checks them; the width and the height of an `<svg>` element,
      which each port read differently and none read with a unit, and a
      viewBox that is not four numbers; and an image tagged as a paragraph
      rather than a Figure in a PDF/UA document. The `tRNS` chunk of a
      grayscale or truecolor PNG is a feature, below.
- ✅ Goal 2: `Page` and `TextLine` are reviewed (Sep 21). The public API, the
      literals each port writes into a content stream, and the fields the
      graphics state saves and restores are the same in the four ports, and
      the nine `Page` methods no example or test used were read and run. Two
      findings, fixed in the four ports with tests: the underline and the
      strikeout of a tagged text line were structure elements that repeat the
      text, and `transform` left the height of the page divided by its
      vertical scale after the state was restored.
- ✅ Goal 2: the reader is reviewed (Sep 21), which the calendar had in the
      Oct 2-8 week: `PDF.read`, the merge, the split, `PDFobj` and
      `Decryptor`, end to end in the four ports. The public API of `PDFobj`
      is the same in all four. Seven findings, fixed in the four ports with
      tests, all in what a PDF that was read is used for rather than in
      reading it: the `PDFobj` methods that add a font, an image, a content
      stream or a graphics state to a page indexed its dictionary and named
      its objects unchecked, which threw in Java, C# and Go and trapped in
      Swift, and `addResource(coreFont)` never ended on a resources object
      whose `/Font` names its own page, taking every byte of memory for a
      file of 1,264 bytes; a `/Length` that is not a number read past the
      tokens in Java, C# and Go, where Swift already said what was wrong;
      `getPageSize` read the fourth and fifth token after a `/MediaBox`
      whatever they were, and took the last two numbers of the box for the
      size, which is 9 points out on a page whose box does not start at
      `0 0`; the `/XObject` of a page that names an object the file does not
      have threw in Java and C#, where Go and Swift checked it; and the
      generation number of an object with none was read in Java and C#. An
      eighth finding, which is not about a broken PDF: a page inherits
      `/Resources`, `/MediaBox`, `/CropBox` and `/Rotate` from the page tree,
      and a page of another program's PDF often carries none of them, as the
      form of Example_50 does, so `getPageSize` read every such page as
      letter size whatever its size was and `getResourcesObject` found
      nothing; `getPageObjects` fills them in now, which needs no new method
      and so leaves the API of v9.0.1 as it is. The reader examples -- 20,
      37, 41 and 51 -- write the same bytes as before in the Java and Go
      ports, and Example_50's page is written with the `/MediaBox` it
      inherits, in all four ports alike.
- ✅ Goal 1: `FuzzPDFRead` fails on a Go runtime error (Sep 21), which it read
      as an ordinary error before, since `ReadWithPassword` recovers from a
      panic and returns it: every index error of the reader was invisible to
      the fuzzer. It drives the merge, the split and the stamp as well: the
      page size, the resources, `MergePages`, `AddObjects`,
      `AddResourceObjects` and the `PDFobj` methods that add to a page. It
      found the hang above in a second, and 56 M runs are clean after the
      fixes. The 1,481 inputs the corpus held then replayed in the four
      ports: none crashes or traps, and of the 1,076 whose password is valid
      UTF-8 -- the rest have no Swift equivalent, as a Swift string holds no
      other bytes -- all four read the same objects, the same pages, the same
      page size and the same content, and merge and split the same ones.
- ✅ Record what is fixed under `## Unreleased` in CHANGELOG.md as it lands
      (Sep 21). Everything on master since v9.0.1 is in it: the reviews of
      the image classes, `Page`, `TextLine` and the reader, every fix the
      eight fuzz targets found, the tagging work, the core font, fallback
      font and line gap work, the booklet, the examples that changed, and a
      `### Removed` section for the files that went.
- ✅ Goal 2: `Font` and its loaders `OTF`, `OpenTypeFont`, `FontStream1` and
      `FontStream2` are reviewed (Sep 21), which the Sep 27-Oct 1 week had.
      The public API of `Font` is the same 26 members in the four ports. One
      finding, fixed in the four ports with tests: a PDF embedded the font
      file, the descriptor, the CID font dictionary and the ToUnicode map of
      the first of two fonts of one name for both, and PDFjet ships twelve
      pairs of fonts that carry one name -- the whole Noto Sans SC and TC
      fonts and the subsets of them beside them -- of which 3,821 of the
      3,917 characters both hold have a different glyph in the subset. A
      document that drew with both drew the text of the second in the glyphs
      of the first: 中文字 came out as Ι㈜♡ in pdftotext and in MuPDF. The
      objects are shared now when a checksum of the font program matches as
      well as the name, so a font read twice, or read from a `.otf` and from
      the `.stream` of it, is still embedded once, as Example_28 draws it.
      The 56 example PDFs are the same as before in the four ports.
- ✅ Goal 2: `TextFrame`, `BigTable`, `CompositeTextLine` and `Bidi` are
      reviewed (Sep 21), which the Oct 9-14 week had. Two findings in
      `BigTable`, fixed in the four ports with tests: every row of a table was
      measured with the header font, so a body font wider than the header font
      ran over the column on its right -- a cell of "wwww" in Helvetica 14 is
      40 points wide and was given a column of 27 -- and `setTextAlignment`
      wrote into the alignments of the columns whatever the column was, which
      threw before `setTableData` made them. The table of Example_43 is
      narrower for the first and holds the same 2,546 pages; nothing else in
      the 56 examples changed. The other three classes hold: a `TextFrame`
      drawn through frames of every height, width and alignment loses no word,
      draws none twice and keeps every row inside the frame, except the one
      character a row takes when the frame is narrower than it;
      `CompositeTextLine.addFormula` reads the 20 formulas of the review as
      its documentation says; and `Bidi` gives the same answer in the four
      ports for all 4,024 strings of a corpus of Arabic, Hebrew, Latin,
      digits, brackets and marks, and loses no letter and reorders no run of
      Latin or digits over 20,000 random strings.
- ✅ Goal 2, in part: the barcodes are reviewed (Sep 21), which the Oct 9-14
      week has. The four ports draw the same modules for every symbol, and
      what they draw was read back with a decoder written from the
      specifications: the 30 UPC-A, EAN-13, Code 39 and Code 128 cases give
      back the text and the check digits they were made from, and of 60 QR
      codes, from version 4 to 40 at every level, 55 give back their string
      -- the finder patterns, the timing patterns, the format information and
      its BCH check, the free modules against the codeword counts, the
      Reed-Solomon of every block and the payload -- and the other 5 are
      refused as too long, which is the documented limit. Data Matrix and
      PDF417 are the same in the four ports. No finding.
- ✅ Goal 2, in part: `Container` is reviewed (Sep 21). One finding, fixed in
      the four ports with tests: it moved the corners of the annotations it
      holds by its own location on every drawing, so a container drawn on a
      second page left its annotations that far from what it drew. Every one
      of the 28 drawables is drawn twice in the tests of the four ports now,
      and only a `TextFrame` and a `Table` draw something else the second
      time, which is what makes them flow from page to page. The axis ranges
      of `Chart` were checked over 200,000 random ranges: each one holds its
      data, has grid lines and is finite. What is left of this block is
      reading `BarChart`, `DonutChart`, `Form` and `Stamp` end to end.
- ✅ Goal 5, in part: the 252 fonts PDFjet ships read against fontTools
      (Sep 21), which the Oct 9-14 week has. Every one gives the same name,
      units per em, ascent, descent, line gap, cap height, underline position
      and thickness, bounding box, first and last character, number of
      advance widths, digest of every advance width and digest of the whole
      character map. The 252 `.stream` files carry what their `.otf` or
      `.ttf` holds, the marks of the GPOS table among it. What is left of
      that item is the kerning, a few fonts PDFjet does not ship, and making
      it a check of the repository.

### Sep 27–Oct 1: release v9.0.2

- ✅ **B** Goal 2: review `Font` and its loaders `OTF`, `OpenTypeFont`,
      `FontStream1` and `FontStream2`, which no test names. Done on Sep 21,
      above.
- ⬜ **B** The release checks: `check-examples.sh` clean, the public API that
      of v9.0.1 in the four ports, the JDK 8 build, the benchmarks recorded
      against 9.0.1 with Example_43's time, the docs, the packages and the
      site rebuilt, the CHANGELOG entry dated.
- ⬜ **B** Tag v9.0.2 on Oct 1 and make the GitHub release.

### Oct 2–8: the reader

- ✅ **B** Goal 2: review `PDF.read`, the merge and the split, `PDFobj` and
      `Decryptor`, which no test names. Done on Sep 21, above.
- ✅ **B** Goal 1: fuzz `PDF.read` — the xref, the object streams and the
      encryption — the last and largest target, next to its review. Done on
      Sep 21, above; the target reaches the merge, the split and the stamp
      now, and a Go runtime error fails it.
- ⬜ **B** Goal 5: the reader against the pdf.js and veraPDF corpora, which
      the same work needs a corpus for anyway.

### Oct 9–14: the rest of the review, and the references

- ⬜ **B** Goal 2: review `TextFrame`, `BigTable`, `CompositeTextLine` and
      `Bidi`; then the barcodes, the charts, `Form`, `Container` and `Stamp`.
      This closes goal 2.
- ⬜ **B** Goal 5: round-trip text, images against Pillow, fonts against
      fontTools.
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
      still that of v9.0.1.
- ⬜ Rebuild the site, and date the `## v9.0.3` entry of CHANGELOG.md.
- Keep Oct 19 and 20 empty: they are the buffer for what the checks find.

### Oct 21: release v9.0.3

- ⬜ **B** Tag v9.0.3 and make the GitHub release.

## If the time runs short

In this order, and none of it moves the date:

- Goal 5 is the first to give: the reader against real PDFs is worth most and
  is already in the Oct 2–8 week; the images, fonts and round-trip text can
  land in 9.0.4.
- The last block of goal 2 — the barcodes, the charts, `Form`, `Container`
  and `Stamp` — is the least exposed and the easiest to carry over.
- The features below do not start before Oct 21 in any case.
- Goals 3, 4, 6 and 7 do not give: they are what "rock solid" is claimed on.

## After v9.0.3 — features

New features, the first work on the foundation of 9.0.3, from the review of
`Table` and `Cell` (Sep 18): what clients look for and do not find. None of
this is started before Oct 21.

- ⬜ True row spans: `Cell.setRowSpan`, like `setColSpan`, where Example_38
      fakes them today by turning cell borders off. A spanned cell draws its
      text, background and borders once over the rows it covers, and page
      breaks keep it whole. The table tagging gives it a `RowSpan`
      attribute. Redo Example_38 with it, and its text, which explains the
      borders left out.
- ⬜ Alternating row colors and simple row styles: `Table.setAlternateRowColor`
      (zebra striping, as `BigTable.setShadingColor`), and a style for the
      header rows, the body and a total row, rather than coloring every cell.
- ⬜ Repeating footer or total rows on every page ("carried forward"
      subtotals), and keeping a row with the next one across a page break.
- ⬜ Column widths from the table width: percentages, or fit a width and
      share it by the content, next to `autoAdjustColumnWidths`.
- ⬜ The transparent color of a grayscale or truecolor PNG: a `tRNS` chunk on
      color type 0 or 2 names one sample value that is transparent, which
      PDFjet ignores, so those pixels are drawn opaque where a browser and
      libpng leave them clear. A palette image already gets its alpha from
      `tRNS`. PDF writes it as `/Mask` with the range of each component, in
      the bit depth the image is embedded at. Found in the review of Sep 21;
      `TestPNGImageTruecolorTransparencyIsIgnored` is the test that records
      today's behavior in the four ports.

## Known and accepted (document, do not fix)

- Only Arabic and Persian letters are shaped.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- MuPDF puts spaces inside vowelled Arabic and Hebrew words drawn from `.otf`
  fonts; Poppler extracts them whole.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
- The table of Example_43 is wider than the page it is drawn on: its nine
  columns are as wide as their widest field, which comes to 795 points of a
  792 point landscape page, so the last column is cut off at the right edge.
  A table as wide as the page needs the column widths that `Table` is to get
  after 9.0.3, below, or fewer columns in the example.
- A character above the BMP, from U+10000 up, is not drawn: the character map
  of a font is read into 65,536 entries. 144 of the fonts PDFjet ships have
  glyphs up there, the bold italic alphabet of IBM Plex Math and the CJK
  ideographs of extension B among them.
- The characters a font draws are those of the range its OS/2 table gives,
  `usFirstCharIndex` to `usLastCharIndex`; a font whose range is narrower
  than its character map draws a space for the rest. None of the 252 fonts
  PDFjet ships is narrower.
- `/ItalicAngle 0` is written for every embedded font, though the loader
  reads the angle of the `post` table. A `.stream` file has no field for it,
  and a `.otf` and the `.stream` of it share one font descriptor, so writing
  the angle needs the field first. Nothing draws differently for it: the
  glyphs of an italic font are italic, and the font is always embedded.
