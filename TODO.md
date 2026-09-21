# PDFjet — the plan to Oct 21

Target: **2026-10-21**, the date v9.0.0 was planned for. v9.0.0 was released
early, on 2026-09-16 at e957f841, and v9.0.1 on 2026-09-18 at 72c41923, so
what Oct 21 carries is **v9.0.3**: not new features but a foundation to build
on after it. Today is Sep 20, so 31 days are left, with one fix release in
the middle. This file is the working list; tick items off as they land on
master.

What is done is in CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## The three dates

- **Oct 1** — v9.0.2, a fix release from master on the v9.0.1 API. What is in
  it is under `## Unreleased` in CHANGELOG.md: the PDF/UA examples and the
  table tagging, kinsoku, the font line gap, the WinAnsi characters and
  kerning of the core fonts, the fallback font rule, the CMYK JPEG marker,
  and the font stream, PNG, BMP and JPEG fixes that fuzzing found.
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

1. ⬜ **B** Fuzz the parsers of untrusted input with Go's fuzzing, and fix each
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
     12, 12.0, 1200e-2, +12, with a space, a comma or nothing before them —
     and the two must draw the same picture; 9.1 M and 23.8 M runs clean after
     the fixes. Found path data written over more than one line read as one
     number, where Go's XML parser leaves the line feed, and a number written
     with an exponent read as two; and, in the other three ports, a throw or a
     trap on path data that starts with a number, on a first command that
     needs a current point, on a `<path>` with no `d`, and, in Swift, on an
     argument that is not a number. Fixed in the four ports with unit tests;
     of the 482 inputs of the Go corpus, every one the ports read draws the
     same content in all four, and none crashes. What they still differ on is
     which malformed XML their parsers accept.
   - ⬜ OTF and TTF, the `Font` loaders.
   - ⬜ The decompressor.
   - ⬜ `PDF.read`: the xref, the object streams and the encryption.

2. ⬜ **B** Finish the class-by-class review, as on Sep 17: one class end to
   end in the four ports, each finding proved by running it, fixed in the four
   ports with a test, and the example pages it touches rendered before and
   after. By exposure, and in this order: `Page` and `TextLine`; `Image`,
   `PNGImage`, `JPGImage`, `SVG` and `SVGImage`; `Font` and its loaders `OTF`,
   `OpenTypeFont`, `FontStream1` and `FontStream2`, which no test names; the
   reader, `PDF.read`, the merge and split, `PDFobj` and `Decryptor`, which no
   test names; `TextFrame`, `BigTable`, `CompositeTextLine` and `Bidi`; the
   barcodes, the charts, `Form`, `Container` and `Stamp`.

3. ⬜ **B** PDF/UA as it is claimed: check the 41 PDF/UA examples with PAC or
   by the Matterhorn Protocol, not only veraPDF, which cannot see what a
   paragraph stands for. Tagging a table as a table, which this goal waited
   on, landed on Sep 19 (b09231fd).

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
     gap and kerning of every font PDFjet ships and a few popular others.
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

### Sep 20–26: the decoders, `Page` and `TextLine`

- ⬜ **B** Goal 1: JPEG and SVG are done (Sep 20); fuzz OTF and TTF, then the
      decompressor, fixing each failure in the four ports as the first five
      targets were fixed.
- ⬜ **B** Goal 2: review `Image`, `PNGImage`, `JPGImage`, `SVG` and
      `SVGImage`, the classes the same week's fuzzing reads; then `Page` and
      `TextLine`, the two with the widest exposure. One of `JPGImage` for that
      review, which the fuzzing did not cover because image/jpeg rejects it
      too: the length of the frame header, which PDFjet does not check against
      the number of components, as libjpeg does.
- ⬜ Record what is fixed under `## Unreleased` in CHANGELOG.md as it lands.

### Sep 27–Oct 1: release v9.0.2

- ⬜ **B** Goal 2: review `Font` and its loaders `OTF`, `OpenTypeFont`,
      `FontStream1` and `FontStream2`, which no test names.
- ⬜ **B** The release checks: `check-examples.sh` clean, the public API that
      of v9.0.1 in the four ports, the JDK 8 build, the benchmarks recorded
      against 9.0.1 with Example_43's time, the docs, the packages and the
      site rebuilt, the CHANGELOG entry dated.
- ⬜ **B** Tag v9.0.2 on Oct 1 and make the GitHub release.

### Oct 2–8: the reader

- ⬜ **B** Goal 2: review `PDF.read`, the merge and the split, `PDFobj` and
      `Decryptor`, which no test names.
- ⬜ **B** Goal 1: fuzz `PDF.read` — the xref, the object streams and the
      encryption — the last and largest target, next to its review.
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

## Known and accepted (document, do not fix)

- Urdu is drawn in Naskh; Nastaliq fonts need GSUB and are not supported.
- Only Arabic and Persian letters are shaped.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- MuPDF puts spaces inside vowelled Arabic and Hebrew words drawn from `.otf`
  fonts; Poppler extracts them whole.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
- SVG arc flags written without a separator (`0 01`) are not parsed.
- A UTF-8 encoded surrogate (ED A0 80) reads as one U+FFFD in Java and three in
  Swift, as Unicode recommends.
