# PDFjet v9.0.0 — release plan

Target: **2026-10-21**. This file is the working list for the release; tick
items off as they land on master.

v9.0.0 is a major release: the API changed since v8.7.0 (Go `Drawable` and
`[2]float32`, the `/v9` Go module path, `PageSize`, typed constants, the
`UserAccess` values, and the renames of the API audit). What is done is in the
`## v9.0.0` entry of CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## Done before this plan (Sep 11–16)

- ✅ Breaking changes, encryption and PDF reading, right to left text, the
      two API audits, merge and split, untrusted input limits, unit tests in
      the four ports, quoted fields in delimited data (4ee4e7cb, 3ebd321b),
      Swift dynamic Huffman compression (300d67ab).
- ✅ CHANGELOG `## v9.0.0` entry, version bump to 9.0.0, regenerated
      references on GitHub Pages, Windows workflow green (5c0ca7e1).

## Week 1 (Sep 17–23): `BigTable` API

These add public API, so they must land in 9.0.0 rather than a minor release.

- ✅ **B** Rows from memory in a `BigTable`, not only from a file. Rows from a
      query or a list of objects have to be written to a temporary file today.
      Take them from a callback or an iterator that yields one row of fields
      at a time, asked for twice, as `setTableData` and `complete` read the
      file twice (measure, then draw). The file API stays, written in terms of
      the new one. Nothing may collect the rows: memory stays flat.
      Done (Sep 16): `setTableData(header, rows)` with the header as its own
      argument (the user's choice), taking an `Iterable<String[]>` in Java
      (a `Closeable` iterator is closed), an `IEnumerable<string[]>` in C#, a
      `Sequence` of `[String]` in Swift and `SetTableRows(header,
      iter.Seq[[]string])` in Go. The file form reads the header from the
      first line with enough fields and passes the lines after it as the
      rows. A short header throws in Java and C# and is recorded on the PDF in
      Go and Swift. Three tests per port: memory and file draw the same last
      page, the rows are gone through twice, a short header is refused.
      Example_43 is identical in the four ports, and its time is unchanged
      when both builds run from the same directory: Java 2,090 ms, C# about
      2,430 (`setTableData` maybe 20 ms slower), Go 990, Swift 3,100.
- ✅ **B** Choosing and ordering the columns of a `BigTable`.
      `setNumberOfColumns(9)` draws the first nine fields of every line; take
      the indexes to draw, in the order they are drawn, and keep
      `setNumberOfColumns` as the first N of them.
      Done (Sep 16): `setColumns(int...)` in Java, `SetColumns(params int[])`
      in C#, `SetColumns(...int)` in Go and `setColumns([Int])` in Swift, as
      `merge` takes its page numbers. The indexes pick the header fields too;
      `setTextAlignment` counts drawn columns; a row, or a file's header line,
      needs a field for the largest index; a negative index is refused.
      Two more tests per port: file and memory draw the same last page with
      the columns `2, 0`, in that order, skipping a row without a third field,
      and a negative index is refused. Example_43 is identical in the four
      ports and its time unchanged, run from the same directory.
- ✅ **B** The knobs of a `BigTable`: setters for the shading colour, the rule
      colour and the padding, and a way to turn the "Page i of N" footer off
      or give it a text and a font of its own.
      Done (Sep 16): `setShadingColor` and `setBorderColor`, each with an
      `int` and a `float[]` form (`RGB` suffix in Go), where
      `Color.transparent` or null leaves the shading or the lines out;
      `setPadding`, which refuses a negative value and can follow
      `setTableData`, as the widths are kept without it; and
      `setFooter(text, font)`, a template with `{page}` and `{pages}` (the
      user's choice over a callback), where a null or empty text leaves the
      footer out and a null font is the header font. Three more tests per
      port. C# first looked 2.5% slower on Example_43, but the old
      `BigTable.cs` built in the repository was just as slow as the new
      one, so the gap came from the build folder; Swift built both ways in
      the repository is within 25 ms.
- ✅ Example_43 stays byte-identical in the four ports, and its printed time
      does not regress; tests for the new API in the four ports. Checked
      after each of the three items above.

## Week 2 (Sep 24–30): `Table` data and delimited files

- ✅ **B** One content field in `Cell`. `image`, `barcode`, `textBlock` and
      `textColumn` are four fields that are meant to exclude each other, but
      each setter clears only the cell text, and `drawOn` and `getHeight` try
      them in a fixed order, so after `setImage` then `setTextBlock` the text
      block is drawn whichever came last. Hold one `Drawable`, so the last
      setter wins and any drawable whose location is its top left corner can
      go in a cell (a QR code, an SVG image, a chart, a table). The cell
      measures its content with `drawOn(null)`, which not every `Drawable`
      supports (`Image.drawOn(null)` fails on `page.pdf`), so the drawables
      measure without drawing first, in the four ports. `getImage`,
      `getBarcode`, `getTextBlock` and `getTextColumn` stay, returning the
      content when it is of that type; `Table`'s column and row setters that
      reach into `cell.textBlock` check the type. `point` (a marker drawn
      beside the content) and `compositeTextLine` (drawn with the text) stay
      fields. Memory is not the reason: about 16 bytes a cell in Java, 1% of
      the `Table` benchmark peak, and more in Swift unless `Drawable` is
      class-bound. Also remove the stray "The text box drawn in this cell."
      comment in Java `Cell`.
      Step 1 done (Sep 16): `drawOn(null)` measures in every `Drawable` in
      the four ports, checked by a test that draws 29 drawables and compares
      the measured corner with the drawn one and what is drawn after
      measuring with what is drawn without. `Table` and `TextColumn` return
      their right edge (the user's choice), and Example_10 subtracts the
      column width. The 56 Java example PDFs have the same page content.
      Step 2 done (Sep 16): `Cell` holds one `Drawable` in the four ports,
      with `setDrawable` and `getDrawable`; the typed setters call
      `setDrawable`, the typed getters cast, `getHeight` follows the order
      `drawOn` draws in (non-empty text first), content is measured with
      `drawOn(null)` at 0, 0 and aligned as images were, and `Table` uses
      `getTextBlock`. Swift `Drawable` is `AnyObject`. The unused Go and C#
      `drawOnPageAtLocation` copies of `Barcode.drawOn` are gone. Four tests
      per port with a recording drawable. Example_08 changed on purpose: its
      barcode row is 1.9 points taller, as the measured corner includes the
      descent of the text under the barcode, which `Barcode.getHeight` leaves
      out; every other Java example has the same page content.
      `Barcode.getHeight` fixed after it (Sep 16): it returns the corner of
      `drawOn(null)` less the top, in the four ports, checked against the
      rendered ink of the four barcode types in three directions with and
      without text; the barcode tests expect it for all 24 rows.
- ✅ Decide whether a quoted field may hold a line break (read on until the
      quotes balance) or document it as unsupported. Today it may not, in the
      four ports.
      Decided (Sep 16, the user's choice of four ideas): it may, and a table
      draws the line break as a space. The record reader of each port reads on
      from a line that ends inside a quoted field, scanning only the new lines
      for the closing quote, and replaces the line breaks of such a record
      with spaces; a quote that is never closed fails at the end of the file
      or after 10,000 lines, a count that is the same in every port. `BigTable`
      looks at every field for line breaks only in rows from memory, so
      Example_43 is identical and as fast in the four ports: a first version
      that looked at every field cost Go 6% and Swift 3%.
- ✅ **B** `Table` writes 25.2 MB where iText's writes 21.6, because `Cell` sets
      the brush and the pen for every cell (2,027 `rg` and `RG` on the sample's
      first page against iText's 675). Skip the operators when the colour has
      not changed.
      Done (Sep 16), made a blocker by the user, in `Page` in the four ports.
      Colours alone took the benchmark to 24.54 MB, as iText's content is not
      shorter but compresses better; with the pen width and the font also
      written only when they change it was 24.10 MB, and with `fillRect` as
      one `re` 21.18 MB, which the user chose. What the content has set is
      saved and restored with q and Q, and a CMYK colour clears it; `re`
      writes the width and height as differences of the rounded corners, so
      the edges stay where the path put them, and a rectangle beyond 100,000
      points is still a path. The example PDFs render the same in Poppler; in
      MuPDF the `re` rectangles of Example_13, 15, 38, 39 and 40 differ by a
      few levels at the edges. Five `Page` tests per port. `check-examples.sh`
      is clean. Found on the way: compiling `BigTableBench.java` by
      hand against a directory of library classes let javac compile the
      library sources of the repository into the output too, where they
      shadow the build being measured; `benchmarks/run.sh` compiles against
      the jar and is not affected. The recorded benchmark numbers are stale
      now (Week 3 item).

## Week 3 (Oct 1–7): cleanups and documentation

- ✅ **B** CHANGELOG and README for everything weeks 1 and 2 add or change;
      record the new benchmark numbers in `benchmarks/README.md`.
      Benchmarks rerun at d2f5d4cb (Sep 16): the Example_43 table, the four
      ports and `Table` are recorded in the three READMEs and
      `jet-vs-box.html`. Left: the text document of `benchmarks/run.sh text`,
      to be rerun on a freshly rebooted, idle machine, as iText and PDFBox came
      out 6 to 22% slower than before on the busy one. After that run
      `BigTable` began filling its rows with one `re` (11.7 MB and 1 to 2%
      faster), so rerun `benchmarks/run.sh table` with it.
      Done (Sep 16): both rerun at 7dbfee5d after a reboot, one after the
      other, and recorded in `benchmarks/README.md` and `jet-vs-box.html`.
      iText and PDFBox came within 2% of 4ee4e7cb on the text document, where
      PDFjet now takes 54 ms against 59 and writes 533,287 bytes; `BigTable`
      takes 1,599 ms against 1,671, allocates 674 MB against 778 and is within
      1.3% of the same drawing on `Page`, whose pages still render the same.
- ✅ S Remove C# `PNGImage.WriteInt`, a private method nothing calls since
      7c15442d, in a commit of its own. Done (Sep 16).
- ✅ S License headers, in a commit with nothing else in it: 45 library files
      carry theirs after the `package`, `using` and `import` lines (15 each in
      Java, C# and Go: the 14 core font classes, plus `Bidi` in Java and C#
      and `arc.go` in Go), and 105 have none (25 Java, 26 C#, 27 Go, 27
      Swift), as do the 51 Go examples.
      Done (Sep 16) in the commit before this one. The count was off: the 14
      core font classes of each port had no header (what looked like one is
      the Adobe notice they hold as data), `Bidi` and `arc.go` had theirs
      late, and C# `Bidi.cs` had it as a doc comment that DocFX showed as the
      summary of `Bidi`: 164 library files in all, and the 51 examples of
      every port. `bmpimage.go` keeps its contributor's copyright. Javadoc,
      DocFX, doc2go and DocC build without warnings, and their output is the
      same apart from the `Bidi` summary and line numbers.

## Week 4 (Oct 8–14): release checks

- ⬜ **B** `check-examples.sh` clean, the four test suites green (Java on JDK 21
      and 8), `go vet ./src/...` clean, Swift builds with
      `-warnings-as-errors`, Build, Documentation and Windows workflows green.
- ⬜ **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, opens the
      Cyrillic and the 200 byte password files, and opens the PDF/UA and PDF/A
      examples without warnings. Same files in Preview, Chrome and Firefox.
- ⬜ **B** Regenerate the docs (`generate-documentation.sh`) and check the four
      references and the examples pages on GitHub Pages after the push.

## Week 5 (Oct 15–21): freeze and release

- ⬜ **B** Code freeze on Oct 15: fixes only, each with its check.
- ⬜ **B** Tag `v9.0.0` on Oct 21 and build the Java and .NET archives
      (`package-java.sh`, `package-dotnet.sh`).
- ⬜ **B** From a scratch module: `go list -m github.com/edragoev1/pdfjet/v9@v9.0.0`
      and `go get github.com/edragoev1/pdfjet/v9@v9.0.0` build and write a PDF.
- ⬜ **B** A scratch Swift package resolves PDFjet from the tag and writes a PDF.

## Known and accepted for 9.0.0 (document, do not fix)

- Urdu is drawn in Naskh; Nastaliq fonts need GSUB and are not supported.
- Only Arabic, Persian and Urdu letters are shaped; Pashto, Sindhi and Kurdish
  letters are drawn unjoined.
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
- Swift `FlateEncode` does no lazy matching; its files are about 1.18x the size
  of Java's for Example_43.
