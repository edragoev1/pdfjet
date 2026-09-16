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

- ⬜ **B** Rows from memory in a `BigTable`, not only from a file. Rows from a
      query or a list of objects have to be written to a temporary file today.
      Take them from a callback or an iterator that yields one row of fields
      at a time, asked for twice, as `setTableData` and `complete` read the
      file twice (measure, then draw). The file API stays, written in terms of
      the new one. Nothing may collect the rows: memory stays flat.
- ⬜ **B** Choosing and ordering the columns of a `BigTable`.
      `setNumberOfColumns(9)` draws the first nine fields of every line; take
      the indexes to draw, in the order they are drawn, and keep
      `setNumberOfColumns` as the first N of them.
- ⬜ **B** The knobs of a `BigTable`: setters for the shading colour, the rule
      colour and the padding, and a way to turn the "Page i of N" footer off
      or give it a text and a font of its own.
- ⬜ Example_43 stays byte-identical in the four ports, and its printed time
      does not regress; tests for the new API in the four ports.

## Week 2 (Sep 24–30): `Table` data and delimited files

- ⬜ **B** Decide whether the rows of a `Table` may arrive incrementally, as
      `BigTable` reads its file. Today the caller builds the whole grid
      (`setTableData(List<List<Cell>>)`); at Example_43's size `Table` takes
      5,038 ms and a 1 GB heap against `BigTable`'s 1,778 ms and 32 MB
      (`benchmarks/results/2026-09-16-542b3dbf-table.log`). If yes, the method
      lands in 9.0.0, so there are never two ways of giving a table its data
      arriving in different releases.
- ⬜ Decide whether a quoted field may hold a line break (read on until the
      quotes balance) or document it as unsupported. Today it may not, in the
      four ports.
- ⬜ S `Table` writes 25.2 MB where iText's writes 21.6, because `Cell` sets
      the brush and the pen for every cell (2,027 `rg` and `RG` on the sample's
      first page against iText's 675). Skip the operators when the colour has
      not changed.

## Week 3 (Oct 1–7): cleanups and documentation

- ⬜ **B** CHANGELOG and README for everything weeks 1 and 2 add or change;
      record the new benchmark numbers in `benchmarks/README.md`.
- ⬜ S Remove C# `PNGImage.WriteInt`, a private method nothing calls since
      7c15442d, in a commit of its own.
- ⬜ S License headers, in a commit with nothing else in it: 45 library files
      carry theirs after the `package`, `using` and `import` lines (15 each in
      Java, C# and Go: the 14 core font classes, plus `Bidi` in Java and C#
      and `arc.go` in Go), and 105 have none (25 Java, 26 C#, 27 Go, 27
      Swift), as do the 51 Go examples.

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
