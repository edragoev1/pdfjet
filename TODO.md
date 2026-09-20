# PDFjet v9.0.0 — release plan

Target: **2026-10-21**. Released early, on **2026-09-16**, at e957f841. This
file is the working list for the release; tick items off as they land on
master.

v9.0.0 is a major release: the API changed since v8.7.0 (Go `Drawable` and
`[2]float32`, the `/v9` Go module path, `PageSize`, typed constants, the
`UserAccess` values, and the renames of the API audit). What is done is in the
`## v9.0.0` entry of CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## v9.0.2 — planned for about Oct 1

A fix release from master, on the v9.0.1 API. What is in it so far is under
`## Unreleased` in CHANGELOG.md: the PDF/UA examples, kinsoku, the font line
gap, the WinAnsi characters and kerning of the core fonts, the fallback font
rule, the CMYK JPEG marker and the BMP fixes.

- ⬜ **B** The first wave of the class reviews of v9.0.3 (below): `Page`,
      `TextLine`, and the image decoders `Image`, `PNGImage`, `JPGImage`.
- ⬜ **B** The release checks of 9.0.1: `check-examples.sh` clean, the public
      API that of v9.0.1 in the four ports, the JDK 8 build, benchmarks
      recorded against 9.0.1 (Example_43's time), docs, packages and the
      site rebuilt, the CHANGELOG entry dated, the tag and the GitHub release.

## v9.0.3 — Oct 21: a rock solid foundation

The goal is not new features but a foundation to build on after Oct 21.
The bugs found from Sep 17 to 18 were almost all in code nobody had read end
to end, and almost all were silently wrong output that every test and example
check passed: 23 in the reviews of `Cell`, `Table`, `TextBlock`, `TextColumn`
and `PDF`; then 32-bit BMPs in the wrong colors, ’ and € drawn as spaces in
the core fonts, every CMYK JPEG inverted, a fallback font that stuck, and the
CJK line gaps. Nothing is fuzzed. So, in order:

- ⬜ **B** 1. Finish the class-by-class review, as on Sep 17: one class end
      to end in the four ports, each finding proved by running it, fixed in
      the four ports with a test, and the example pages it touches rendered
      before and after. By exposure: `Page` and `TextLine`; `Image`,
      `PNGImage`, `JPGImage`, `SVG` and `SVGImage`; `Font` and its loaders
      `OTF`, `OpenTypeFont`, `FontStream1` and `FontStream2`, which no test
      names; the reader, `PDF.read`, the merge and split, `PDFobj` and
      `Decryptor`, which no test names; `TextFrame`, `BigTable`,
      `CompositeTextLine` and `Bidi`; the barcodes, the charts, `Form`,
      `Container` and `Stamp`.
- ⬜ **B** 2. Fuzz the parsers of untrusted input with Go's fuzzing, and fix
      each failure in the four ports, which share the logic: `PDF.read` (xref,
      object streams, encryption), PNG, JPEG, BMP, SVG paths, OTF and TTF,
      the `.stream` fonts and the decompressor. Any input either works or
      fails with a clean error: no hang, no index error, no runaway memory.
      Keep the fuzz targets and their corpora in the repository.
      Stream fonts in Go done (Sep 19): `FuzzFontStream` (the whole file) and
      `FuzzFontStreamMetrics` (the metrics and marks, compressed by the
      target), in `src/fontstream_fuzz_test.go`, 1.1 M and 0.9 M runs clean
      after the fixes. Found: lengths from the file allocated before reading
      (4 GB for a 9-byte file), metrics and marks read past their end, no
      check of the units per em, the character range, the tables or the
      name, and `embedFontFile2` looping on a read error. And one in shipped
      fonts: IBM Plex Sans JP maps 15 arrows (↺ …) past its advance widths,
      so "↺" and a combining mark threw in `Page.markOffsets` (Java
      ArrayIndexOutOfBounds, Swift traps). Fixed in Java, C# and Swift the
      same way (Sep 19), with unit tests; Swift also trapped on a name or
      license that is not UTF-8, a negative length and an `Int16` out of
      range. A mark block of 0 bytes is no marks in the four ports (Go did
      so by accident). The 873 inputs of the Go corpus, replayed in the four
      ports: no crash in any; Java and C# accept and reject the same inputs
      as Go.
- ⬜ **B** 3. PDF/UA as it is claimed: tag a table as a table (below), and
      check the 41 PDF/UA examples with PAC or by the Matterhorn Protocol,
      not only veraPDF, which cannot see what a paragraph stands for.
- ⬜ **B** 4. The manual viewer pass, open since 9.0.0 (Week 4 below):
      Acrobat Reader, Chrome (pdf.js), Firefox, Edge and Preview, not only
      veraPDF and MuPDF.
- ⬜ **B** 5. Freeze the API and the behavior. After 9.0.2 fixes only; the
      public API of v9.0.1 in the four ports, checked as for 9.0.1 before each
      tag; the code freeze of Oct 15 stands.
- ⬜ **B** 6. Guard against regressions: benchmarks recorded at 9.0.2 and
      9.0.3 against 9.0.1, Example_43's printed time among them, and the JDK
      8 build, the packages and the docs made from the tag.
- ⬜ **B** 7. Test the output against independent references and real files,
      which finds the silently wrong output that fuzzing does not:
      - Round-trip text: the text of every example in every port, extracted
        with MuPDF and pdftotext, equals the strings the example drew. It
        would have caught the core font spaces, .notdef glyphs and a wrong
        fallback font. `check-example-pdfs.py` extracts the text spans
        already.
      - Images against Pillow: a corpus of PNG, JPEG and BMP files, real ones
        from GIMP, Photoshop, Paint and phone cameras as well as made ones,
        decoded to the pixels Pillow decodes them to.
      - Fonts against fontTools: the widths, character map, ascent, descent,
        line gap and kerning of every font PDFjet ships and a few popular
        others.
      - The reader against real PDFs: the pdf.js and veraPDF test corpora
        read, merged and split, with the page counts, page sizes and text
        MuPDF finds.

## After v9.0.3 — features

New features, the first work on the foundation of 9.0.3, from the review of
`Table` and `Cell` (Sep 18): what clients look for and do not find.

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

## Week 1 (Sep 17–23): `BigTable` API

These add public API, so they must land in 9.0.0 rather than a minor release.

## Week 2 (Sep 24–30): `Table` data and delimited files

## Week 3 (Oct 1–7): cleanups and documentation

## Week 4 (Oct 8–14): release checks

- ⬜ **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, opens the
      Cyrillic and the 200 byte password files, and opens the PDF/UA and PDF/A
      examples without warnings. Same files in Preview, Chrome and Firefox.

## Week 5 (Oct 15–21): freeze and release

- ⬜ **B** Code freeze on Oct 15: fixes only, each with its check.
      Not held: the user released on Sep 16, so fixes from here go in 9.0.x.

## Known and accepted for 9.0.0 (document, do not fix)

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
