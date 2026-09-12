# PDFjet v9.0.0 — release plan

Target: **2026-10-11** (30 days from 2026-09-11). This file is the working list
for the release; tick items off as they land on master.

v9.0.0 is a major release because the API changed since v8.7.0: the Go `Drawable`
declares `SetLocation` and returns `Drawable` from it, `DrawOn` returns
`[2]float32` in Go, `Arc.drawOn` returns the corner of the arc, the `UserAccess`
values are halved, and `/P` is negative. Everything below that is not done is
either a blocker (must ship in 9.0.0) or a stretch goal (ships if ready).

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## Week 1 (Sep 11–17): decisions and breaking changes

- ✅ **B** Go `Drawable` declares `SetLocation`; `SetLocation` returns `Drawable`
      and goes last in a chain. Commit 95ea973d.
- ✅ **B** Go `DrawOn` returns `[2]float32` everywhere; `Arc.drawOn` returns the
      bottom right corner in all four ports. Commit 3fbafe4c.
- ✅ **B** Go module path for a major version: `go get github.com/edragoev1/pdfjet@v8.7.0`
      failed with *module path must match major version*, so nobody could
      fetch a tagged Go release. The module is now
      `github.com/edragoev1/pdfjet/v9` in `go.mod`, every import, doc2go and
      the README. Still to do after tagging: `go list -m
      github.com/edragoev1/pdfjet/v9@v9.0.0` from a scratch module.
- ✅ **B** `Table`, `TextBlock`, `SVGImage` and `DonutChart` implement
      `Drawable` in all four ports. `DonutChart.drawOn` returns the bottom
      right corner of the outer circle (it was `void` in Java, C# and Swift
      and returned `error` in Go), and Go `DonutChart.SetLocation` returns
      `Drawable`, so it goes last in a chain like the other Go drawables.
- ✅ **B** `Stamp.Rectangle()` and `Stamp.Draw()` were empty stubs in Go and C#,
      `rectangle()` and `draw()` in Java, `rectangle()` in Swift. Removed from
      all four ports; nothing used them.
- ✅ S `Permissions`/`UserAccess` stay int based in Java and Swift: Java has
      no flags enum and a Swift `OptionSet` is not an enum, so both keep the
      enum with the bit values of the standard, combined with `|` on
      `getValue()`. Documented in the README Port differences section.

## Week 2 (Sep 18–24): encryption and PDF reading

- ✅ **B** Password handling is the same in the four ports: UTF-8, at most 127
      bytes, no normalization (Go has none without a dependency; documented in
      the README). A 200 byte password opens with its first 127 bytes and not
      with 126 in MuPDF and qpdf; a Cyrillic password opens in Poppler, MuPDF
      and qpdf. Still to check: Acrobat.
- ✅ **B** `Passwords` default to `""` in all four ports; a PDF with no user
      password opens without a prompt, with the permissions applied.
- ✅ **B** `Perms` bytes 12–15 are random bytes in all four ports, as the
      standard says; Java, C# and Go wrote `----`.
- ✅ **B** `Decryptor` opened only PDFs with an empty user password. The PDF
      reading entry points take a password in all four ports (`read(stream,
      password)`, `ReadWithPassword` in Go, `read(from:password:)` in Swift),
      checked as user then owner password for revisions 2 to 6. Example_30
      and qpdf-made R2, R3, R4 and R6 files read back with either password in
      all four ports; a wrong or missing password raises a clear error.
- ✅ Swift AES speed: 26 ms per MiB in a release build, 1.2 s per MiB in a
      debug build (`swiftc -O` / `-Onone`, `Cryptography.aesEncryptCBC`).
      Example_30 with the 7 MB NotoSansTC-Regular.ttf takes 0.47 s in release
      and 11.6 s in debug, so the cipher stays as it is; no T-table.
- ✅ An encrypted PDF/UA file (Example_22 plus the Example_30 passwords)
      failed veraPDF ua1 in all four ports on ISO 14289-1 7.16: bit 10 of
      `/P` (extract for accessibility) must be set. `Encryption` now grants it
      when the compliance is PDF/UA, and the files pass with `--password` in
      all four ports. Noted in the README, with the veraPDF 1.30.2 hash loop
      bug that rejects about one in forty AES-256 files from any producer.

## Week 3 (Sep 25–Oct 1): right to left text, marks and code TODOs

Pick from the README limitations; the first three are the ones users hit.

- ✅ **B** Letters unjoined at a forced break inside a too wide right to left
      word (`TextBlock`): `Bidi.reorderVisually(str, from, to)` shapes the
      whole string and returns the part for a range of it, and `TextBlock`
      makes each line of a broken word, and the line the rest of the word
      starts, that way, so the joined forms survive the break in all four
      ports.
- ✅ **B** ZWNJ and ZWJ were dropped from copied text (`می‌خواهم` → `میخواهم`).
      Bidi keeps them, Page puts each in the ActualText of the glyph before
      it with a zero-width stand-in glyph (MuPDF needs a glyph per character),
      or draws the font's own zero-width glyph when it has one. Poppler and
      MuPDF copy them; rendering is unchanged.
- ✅ Brackets around left to right text nested in a right to left line
      (`مرحبا (hello) عالم`) copied the wrong way round. Per-bracket ActualText
      cannot fix it: Poppler keeps the bracket before the run with the run and
      moves the one after it, whatever the ActualText says, and MuPDF moves
      the first bracket to the right to left text. What both accept: the run,
      brackets included, in one span whose ActualText is its text between two
      left-to-right marks (U+200E), which end up in the copied text. `Bidi`
      marks the pairs that rule N0 resolves as right to left around left to
      right text only, and `Page` draws the run between the two marks as that
      span, in all four ports; Poppler copies `مرحبا (hello) عالم`, and MuPDF
      keeps `(hello)` together. An `abc (مرحبا) def` line is still laid out as
      a right to left line, as the README says.
- ✅ S MuPDF spaces inside words with moved marks: reproduced with MuPDF
      1.27.2 on vowelled Arabic and Hebrew drawn from the `.otf` fonts
      (`מִ יד`, `رَسُ ولُ`); Poppler extracts the words whole. A word is
      already drawn in one span (`appendWordWithMovedMarks`), so a span per
      word is not the fix: MuPDF adds the spaces from the positions of the
      moved mark glyphs, which sit over the letter after them in the drawn
      order. Left as a known MuPDF limitation in the README Marks section.
- ✅ `CheckBox` with a URI: the marker was stale. `Page.addAnnotation` makes
      a Link structure element of its own, so the link needs no BMC/EMC; a
      comment says so now.
- ✅ `CheckBox`: the link rectangle, the label baseline and the returned
      corner use the check box font size; nothing changes when it is the font's
      size, as in Example_26. `RadioButton` sets the size on the font itself, so
      its markers were stale.
- ✅ `Rect`: the no-op `setStructureType` and `setFillShape` are gone from
      Java and Go (the fill and border colours decide the painting, and a rect
      is an artifact). Java and Swift add the link annotation for
      `setURIAction`/`setGoToAction`, as C# and Go did. Found on the way: the
      `Cell` link rectangle was mirrored vertically in Java, C# and Swift (the y
      was flipped twice); fixed, and Go got the cell link it had commented out.
- ✅ `PDF417`: a quiet zone is blank page, so nothing is drawn; the class
      comment in the four ports says to leave at least two modules around the
      symbol, as ISO/IEC 15438 asks.
- ✅ SVG elliptical arcs: no port had them (the note that Java and Go did was
      wrong). All four convert `A`/`a` to cubic curves (SVG 1.1 F.6.5) and
      render a test file identically. Also found: the path tokenizer dropped the
      last number of a path that does not end with `Z`, in all four ports;
      fixed. Arc flags written without a separator (`0 01`) are still not
      parsed.
- ✅ PNG `gAMA`, `cHRM`, `sBIT`, `bKGD`: ignored in all four ports; the
      empty branches are gone and a comment says so.
- ✅ Internal TODOs: 49 markers, now none. `Cell` fields keep their names (they
      match `setWidth`/`setLineWidth`); `State` copies the colours on purpose
      (comment); Go `image.go` panics on a close error like the other files;
      Go `qrutil.go` documents the BCH polynomial and mask; Swift `BMPImage`
      and `OTF` throw where Java throws, Swift `Font` (CJK) and `TextColumn`
      use `fatalError` for invalid input like the rest of the port; the
      Swift `Bookmark`, `Cell`, `Chart`, `Table` and `TextColumn` markers were
      stale (the code already matched Java) and are dropped.

## Week 4 (Oct 2–11): parity audit, docs, release

- ✅ **B** Public API audit across the four ports: `audit-api.py` lists the
      public types and members per class in Java, C#, Go and Swift and diffs
      them. Its first run listed some 60 types and 400 members that were not in
      every port. Fixed: members missing from a port (Cell borders and
      paddings, TextBlock background and height, Page transform and rotateBy,
      PDF.setLanguage in C#, Rect.setLanguage, Font.getName, PDFobj reading
      members in Go and Swift, Permissions accessors in C#, ...), one-port
      names renamed to the Java name (Go GetBgColor, C# Size, Swift
      setAllBorders, ...), one-port duplicates removed (setXY, setBrushColor,
      SetTextIsArabic, ...), helpers that Java keeps package-private hidden
      in C#, Go and Swift (Page text state, QR and PDF417 tables, SVG, Util,
      ...), the PageLayout `RIGTH` typo, and the font constant classes, which
      named files that do not ship (C# and Swift JetBrainsMono) or missed
      shipped ones (NotoSans Black and Thin, SourceSerif4 Black). The rest are
      conventions, documented in the README Port differences section; the
      script's report now shows only those.
- ⬜ **B** `check-examples.sh` clean, `go vet` clean, Swift builds with
      warnings as errors, Windows workflow run from the Actions tab and green.
- ⬜ **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, and opens the
      PDF/UA and PDF/A examples without warnings. Same files in Preview,
      Chrome and Firefox.
- ⬜ **B** CHANGELOG `## v9.0.0` entry. Breaking changes first: Go `Drawable`
      and `[2]float32`, `Arc.drawOn`, `UserAccess` values (any code that used
      raw ints must change), negative `/P`, Go module path `/v9`, `Box`
      removed in favor of `Rect` (`setColor` becomes `setBorderColor`, or
      `setFillColor` with `setFillShape(true)`; `setLineWidth` and `setPattern`
      become `setBorderWidth` and `setBorderPattern`), `TextFrame`
      `setBorderColor` turning the border on (as `Text` does) and
      `drawOn(null)` measuring instead of throwing or exiting, and whatever week 1
      decides for `Table`/`TextBlock`/`SVGImage`/`DonutChart` and `Stamp`.
      `Table`: the Java file constructor throws `IOException` and keeps the
      empty fields at the end of a line; Swift `setTextColorInColumn` takes
      an `Int32` and `setTextAlignInColumn` no longer throws; a table no longer
      skips as many data rows as it has header rows on the first page and at
      every page break, and the lines of a wrapped header cell repeat on
      every page; `rightAlignNumbers` right-aligns the same texts in the four
      ports (ASCII digits with an optional sign and exponent); the column
      setters change a cell's `TextBox`, as in Java, not its `TextBlock`;
      Java and C# `Cell.setBorders(true)`, and so `Table.setCellBorders(true)`,
      turn the four borders on, as in Go and Swift; the `Table(f1, f2)`
      constructor, which ignored its fonts, is removed from Java, C# and Swift.
      `Cell`: `setTextBox` clears the cell text in the four ports, as
      `setImage` and `setBarcode` do; Java `setFont`, and so
      `Table.setFontInRow` and `setFontInColumn` in the four ports, set the
      font size to the size of the font; Go and Swift use the cell font as the
      fallback font, as Java and C# do; the text width, the underline, the
      strikeout and the link of a cell are measured at the cell's font size
      with the fallback font; `Align.JUSTIFY` draws the text, image or
      barcode of a cell left aligned instead of throwing; a right aligned
      image or barcode keeps the right padding, not the left one; Swift
      `getBackgroundColor` returns an optional and `setBackgroundColor(nil)`
      removes the background; the unused Java `colspan` and Java, C# and
      Swift `strokeDashPattern` fields are removed.
      `TextBox`: `setLineWidth` sets the width the borders are drawn with, as
      `setStrokeWidth` does, and `getLineWidth` returns it; before, both used
      a field that nothing drew with; measuring a text box that grows to fit
      its text with `drawOn(null)`, as `Cell.getHeight` does, or drawing it
      twice no longer turns it into a text box of fixed height, which could
      cut its last line to "...".
      `Text` and `TextFrame` keep the vertical offset, including the offset of
      a superscript or subscript, the URI and the GoTo action of a text line
      when they wrap it, as `TextColumn` does; Java and Go
      `Content.ofTextFile`, and so `Text.readLines` and
      `Text.paragraphsFromFile`, drop a byte order mark at the start of the
      file, as C# and Swift do.
      `Line` and `Path` `setWidth` and `setColor` are renamed `setStrokeWidth`
      and `setStrokeColor`, as in `Arc` and `Point`; `Container.add`,
      `Path.add`, `OptionalContentGroup.add`, `DonutChart.addSlice`,
      `CompositeTextLine.addComponent` and `TextColumn.addParagraph` return
      the object they are called on; `Chart.toFloatArray` is no longer
      public, and `Encryption.getObjNumber` is internal in C#, Go and Swift;
      Java `PDF.newobj` and `endobj` are named `newObj` and `endObj`.
      Then: Data Matrix barcodes (Example_14), Swift encryption, random salts, `EncryptMetadata true`, right to
      left fixes, TODO cleanups.
- ⬜ **B** Version bump: producer string `PDFjet v9.0.0` in `PDF.java`,
      `PDF.cs`, `pdf.go`, `PDF.swift`; `package-java.sh` and
      `package-dotnet.sh` archive names; README where 8.7.0 is mentioned.
- ⬜ **B** Regenerate the docs (`generate-documentation.sh`), check the four
      references on GitHub Pages after the push, and that the examples pages
      link the new Swift Example_30.
- ⬜ Release: tag `v9.0.0`, build the Java and .NET archives, confirm
      `go get github.com/edragoev1/pdfjet/v9@v9.0.0` works from a clean module,
      and confirm the Swift package resolves from the tag.

## Known and accepted for 9.0.0 (document, do not fix)

- Urdu is drawn in Naskh; Nastaliq fonts need GSUB and are not supported.
- Only Arabic, Persian and Urdu letters are shaped; Pashto, Sindhi and Kurdish
  letters are drawn unjoined.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
  