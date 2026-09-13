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
      bottom right corner in all four ports. Commit 3fbafe4c. It missed Go
      `QRCode` and `PDF417`, which are not `Drawable` yet; see the API audit.
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
- ⬜ **B** Decide which renames from the API audit below go into 9.0.0; the
      rest move to a 10.0.0 list. Each is a breaking change, so the chosen
      ones land before the CHANGELOG entry is written.

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

## API audit fixes (Sep 14–Oct 1): bad names and drift between the ports

Found with `./audit-api.py --dump` and by reading each public class in the
four ports side by side; every item was checked in the code. The script
matches names and parameter counts only, so none of this is in its report.
Paths are Java unless a port is named. Bugs are **B**; renames are S, because
a rename breaks user code and must land in 9.0.0 or wait for 10.0.0, which
the Week 1 decision settles.

### Bugs found by comparing the ports

- ⬜ **B** Go gives every PDF the same `/ID` and XMP DocumentID:
      `djb.Salsa20()` hashes a fixed test vector and never reads the clock
      (`src/djb/salsa20.go:19`, `pdf.go:112`). Java, C# and Swift seed from
      the time.
- ⬜ **B** `PDF.setTitle`, `setAuthor`, `setSubject`, `setKeywords` and
      `setCreator` do nothing for `PDF_17`, the default compliance: they only
      feed the XMP stream, which is written for PDF/A and PDF/UA only, and the
      trailer has no `/Info` (`PDF.java:1171`, `:1220`). All four ports.
- ⬜ **B** Java `RadioButton.setLocation(double, double)` calls itself and
      throws `StackOverflowError` (`RadioButton.java:69`).
- ⬜ **B** Go `NewCircleAnnotation`, `NewSquareAnnotation`,
      `NewPolygonAnnotation` and `NewTextAnnotation` skip `NewBaseAnnotation`:
      the fill is black and the transparency 0, and `/CA 0` makes the
      annotation invisible (`circleannotation.go:10`). Go `Container.DrawOn`
      offsets only these four types, Java any `BaseAnnotation`.
- ⬜ **B** Go `QRCode` and `PDF417` are not `Drawable`, despite the Week 1
      item: `SetLocation` returns the concrete type and `DrawOn` returns
      `[]float32` (`qrcode.go:58,81`, `pdf417.go:132,240`). Go `DataMatrix`
      is right.
- ⬜ **B** Swift `OptionalContentGroup` keeps `visible`, `printable` and
      `exportable` as `Bool?` and tests `!= nil`, so `setVisible(false)`
      writes `/ON` (`OptionalContentGroup.swift:24,88`).
- ⬜ **B** C# `CompositeTextLine` is another algorithm: it lays the lines out
      at draw time and always calls `SetFontSize(fontSize)`, so a line with no
      font size draws at size 0; `GetMinMax` changes the font sizes and
      `GetWidth` adds up the line widths (`CompositeTextLine.cs:138,205-273`).
- ⬜ **B** Swift `Executive.PORTRAIT` and `LANDSCAPE` are `[Double]`, so
      `Page(pdf, Executive.PORTRAIT)` does not compile (`Executive.swift:16`).
- ⬜ **B** C# `Compressor.Deflate` returns no bytes for empty input, which is
      not a zlib stream, so an empty page gets a broken `/FlateDecode` stream
      (`Compressor.cs:18`).
- ⬜ **B** C# `SVG` parses path numbers with `float.Parse` in the current
      culture, so SVG images break on a German or French system
      (`SVG.cs:100`).
- ⬜ **B** 8-bit indexed PNG: Java, C# and Swift undo the row filters on the
      RGB bytes after the palette lookup, 3 bytes per pixel, instead of on the
      indexes; Go never undoes them (`PNGImage.java:342`, `pngimage.go:313`).
- ⬜ **B** `Chart`: Java and Swift start the automatic maximums at the
      smallest positive float (`Float.MIN_VALUE`, `leastNonzeroMagnitude`),
      so all-negative data gets a maximum of 0 (`Chart.java:40`); C# rounds
      axis limits the user set (`Chart.cs:501`); Go never uses the automatic
      colours (`chart.go:548`); Swift `drawOn` crashes on empty data and
      divides by zero on flat data.
- ⬜ **B** `CalendarMonth`: Swift takes the weekday of day 0, so a month that
      starts on a Sunday is a row low (`CalendarMonth.swift:39`); Go lays the
      calendar out differently and places the header with `x1` as the y
      (`calendarmonth.go:89`); the default location, cell size and circle
      pen width differ in all four ports.
- ⬜ **B** Go `Barcode` draws the UPC, EAN-13 and Code 39 text at the
      barcode's own `x1`, `y1` instead of the location it is drawn at, so the
      text of a barcode in a table cell is misplaced (`barcode.go:296,494`).
- ⬜ **B** Go `NewForm` leaves the label and value font sizes and the form
      width at 0 (`form.go:28`); Swift defaults to 8 and 10 where Java has 9
      and 9 (`Form.swift:17`).
- ⬜ **B** `BigTable`: Java splits lines with a regex and drops the empty
      fields at the end of a line (`BigTable.java:249,295`), as `Table` did;
      in all four ports `setLocation` adds to x, so a second call moves the
      table again, and `setLanguage` stores a field nothing reads.
- ⬜ **B** `TextColumn.setTextAlignment` has no effect: `drawOn` replaces it
      with each paragraph's alignment. `drawOn` also compares the column
      height with a y coordinate (`TextColumn.java:233,238`). All four ports.
- ⬜ **B** Wrapping drops text line settings: `Text` and `TextFrame` lose the
      line colour, structure type, text direction, alt description and the
      URI language, alt and actual text (`Text.java:231`,
      `TextFrame.java:410`); `TextColumn` loses the colour map and language
      (`TextColumn.java:270`). All four ports.
- ⬜ **B** `Table.drawOn(null)` renders every row and sets `rendered = -1`,
      so a later `drawOn(page)` draws only the header rows
      (`Table.java:583`). All four ports.
- ⬜ `PDF417.drawOn` overwrites `x1`, so a second draw shifts the symbol
      right (`PDF417.java:275`). All four ports.
- ⬜ `CheckBox` and `RadioButton` set a blue brush for a linked label, and the
      five-argument `drawString` resets it to black (`CheckBox.java:222`,
      `Page.java:464`). All four ports.
- ⬜ `TextBox` draws underline and strikeout in the border colour, not the
      text colour (`TextBox.java:937`). All four ports.
- ⬜ `TextBlock` measures with the fallback font and draws with the main font
      only (`Page.java:2664`); `setFallbackFontSize` resizes the shared
      `Font`. All four ports.
- ⬜ `Cell.setTextBlock` and `setTextColumn` keep the cell text, unlike
      `setTextBox`, and `drawOn` draws the text while `getHeight` measures
      the block (`Cell.java:274,294,860`); `getHeight` and column fitting
      ignore `setFontSize`. All four ports.
- ⬜ `Page.getPenWidth` is 0.5 on a new page but no `w` is written, so readers
      draw 1.0 (`Page.java:60`); the CMYK setters do not update `getPenColor`
      and `getBrushColor`. All four ports.
- ⬜ Java and Swift `FontStream1` read the font with one `read()` and no loop,
      so a short read corrupts it (`FontStream1.java:367`).
- ⬜ `SVGImage`: Go passes a `structureType` nothing sets to `AddBMC`, an
      empty tag in PDF/UA (`svgimage.go:326`); Java and C# never close the
      file (`SVGImage.java:53`); Swift scans for `" d="` and `" fill="`
      instead of parsing XML, losing single-quoted attributes and attributes
      after a newline (`SVGImage.swift:82`).
- ⬜ `Container`: Swift `add` never sets `parent`, so annotations in a nested
      container miss the offset (`Container.swift:139`); C# compares
      `GetType() == typeof(Container)`, which misses a subclass.
- ⬜ Swift `Bookmark` collapses only spaces in a title, not tabs and newlines
      (`Bookmark.swift:63`); `getDestKey` and `getTitle` crash on the root.

### Drift between the ports

- ⬜ Defaults: C# `CheckBox` check mark is blue, black elsewhere
      (`CheckBox.cs:22`); Go `FileAttachment` description ends "the attached
      attachment" (`fileattachment.go:27`); Java `new PDF()` has a null
      compliance, `PDF_17` elsewhere (`PDF.java:62`); a fill-only `Arc` gets a
      black hairline stroke (`B`) in Java, C# and Swift and is only filled
      (`f`) in Go (`Arc.java:22`, `arc.go:243`); Go `NewImage2` and
      `SVGImage` use empty alt and actual text, `" "` elsewhere; Java and C#
      `FileAttachment` write an empty `/T <>`, Go and Swift skip it.
- ⬜ Numbers written: `FastFloat` rounds `-1.125` to `-1.12` in Java and C#
      and `-1.13` in Go and Swift, and overflows `int` above 21.5 million in
      Java and C# (`FastFloat.java:11`); Swift `OpenTypeFont` scales
      `/FontBBox`, `/Ascent`, `/Descent` and `/CapHeight` to 1/1000 em where
      the others write font units (`OpenTypeFont.swift:116`); Swift
      `FontStream2` writes `/W` widths as `600.0`.
- ⬜ `Chart` axis labels use the locale in Java and C# and `.` in Go and
      Swift; Go ignores `SetMinimumFractionDigits` (`chart.go:402`); C#
      places the Y labels with the ascent at the chart font size. Go
      `DonutChart` computes percentages in float64 and rounds some slices
      differently (`donutchart.go:249`).
- ⬜ Text input: Java `TextBlock` splits `"\n"` into no lines, the others into
      one (`TextBlock.java:456`); Java and Go `Table` keep a BOM in the first
      header cell (`Table.java:71`); C# `Content.ofTextFile` also reads
      UTF-16 and UTF-32 BOMs; Go `Cell` measures an empty cell one line tall,
      the others 0 (`cell.go:299`).
- ⬜ Copies and live references: Swift `PDFobj.getDict()` returns a copy, so
      a read object cannot be edited (`PDFobj.swift:39`); Java and C# `Page`
      and `TextLine` colour getters return the internal array; Go
      `Page.GetContent` returns the live buffer; C# `Encryption.GetKey`
      returns the key, Java a clone (`Encryption.cs:150`).
- ⬜ Mutable constants: the page sizes and the `Token` byte arrays can be
      changed by callers in Java, C# and Go (`A4.PORTRAIT[0] = 100` changes
      every later page); Swift `let` is safe.
- ⬜ Errors. Go exits with `log.Fatal` where Java throws: `ReadWithPassword`
      on a wrong password (`pdf.go:1321`), bad numbers in `pdfobj.go`,
      `svg.go` (23 calls), `otf.go`, `font.go:280`, `NewEmbeddedFileAtPath`,
      an invalid Code 39 character. Go ignores errors from `Inflate`
      (`pdfobj.go:96`, `pngimage.go:81`) and `NewJPGImage` (`image.go:83`,
      then a nil pointer), and `AddObjects` without `/Pages` writes nothing.
      Swift prints and carries on: PNG errors (`PNGImage.swift:55`), QR data
      that does not fit (a truncated code, `QRCode.swift:308`), an invalid
      Code 39 character (skipped), an unknown barcode type (`[]`, which
      crashes `Cell`), `Stamp.drawText` without font or text, a bad SVG
      colour (transparent), a missing file in `Content.ofBinaryFile` (`[]`),
      `Form.drawOn(nil)`; `BigTable.complete` hides errors with `try?`;
      `Table.drawOn(pdf, pages, size)` crashes on `xy!` after a completed draw
      (`Table.swift:477`). Go and Swift crash on a truncated BMP where Java
      throws. C# Code 39 throws `KeyNotFoundException` before its own message.

### Types and signatures

- ⬜ Go constants are untyped ints in `compliance`, `alignment`, `capstyle`,
      `joinstyle`, `effect`, `mark` and `imagetype`; `type Compliance int`
      exists but `SetCompliance` takes `int`. Go `direction` has
      `BottomToTop` = 2, the others `BOTTOM_TO_TOP` = 1. Go `compress.Yes` is
      a `bool`.
- ⬜ Swift takes a `PathOperator` enum in `Page.drawPath`, `drawCircle`,
      `drawRectRoundCorners` and `Point.setPathOperator`, and an `ImageType`
      enum in `Image.init`; Java, C# and Go take any `String` and `int`, so a
      typo writes a broken content stream. `PDF.setPageLayout` and
      `setPageMode` take a `String` in all ports though `PageLayout` and
      `PageMode` exist.
- ⬜ `Point.setAlignment` takes an `int` in Java, an `Alignment` in C# and a
      `UInt32` in Swift; `Point.getTextColor` returns `int` in Java and C#,
      `[3]float32` in Go, `[Float]` in Swift. `TextBlock.setTextAlignment`
      takes an `Alignment`, `TextBox` and `Cell` an `Align` int.
- ⬜ Swift: `Dimension.getWidth`/`getHeight` return `Float?`;
      `PNGImage.getWidth` is `Int?` (Java `int`, Go `float32`); `Cell.init`,
      `Cell.setFont`, `TextParameters.setFont` and `setText` take optionals
      and force-unwrap; `DonutChart.init` requires fonts Java allows to be
      null; `Title.prefix` and `textLine` are optional; `Cell.setColSpan`
      takes `UInt32`; `PDF.addObjects`, `addResourceObjects` and four
      `PDFobj` methods are `inout` and write nothing back; `RadioButton`
      setters lack `@discardableResult`; `PDF417.init` throws an internal
      `EncodingError`; `Page.addWatermark`, `addHeader` and `addFooter` are
      `throws` and never throw.
- ⬜ Go colour setters: `BaseAnnotation.SetFillColor` takes `[3]float32` and
      `SetFillColorInt` an int, the reverse of every other type;
      `Arc.SetFillColorRGB` takes `r, g, b` (plus `SetFillColorRGBArray`),
      `Rect.SetFillColorRGB` the array; `Stamp` colours are `int`, the rest
      `int32`; `Form.SetLabelColor`/`SetValueColor` take `int32`, which Java,
      C# and Swift lack; only `Arc` has `Float64` variants. Go `Cell`,
      `TextBlock` and `TextBox` colour getters return black when unset, and a
      Go cell background cannot be removed.
- ⬜ Overloads: Go `Page.DrawString(font1, font2, text, x, y)` has no font
      size, and Go and Swift lack Java's `(font, fallback, size, str, x, y,
      color, colorMap)`; Java `drawString` takes a boxed `Integer` colour. Go
      has no `NewTextBox(font, text, width, height)` and no `Title` text line
      getter; Swift `Rect` and `TextBox` lack the `r, g, b` colour setters;
      only Java has a public `SVGImage()`, which leaves the path list null;
      Go `BigTable.SetTableData` returns only `error`; Go `PDF.Read` takes
      `[]byte`, not in the README.
- ⬜ Mutators that return void: `Rect.scaleBy`, `SVGImage.scaleBy`,
      `Image.rotateClockwise` (Swift returns the image), `Container.rotate`
      and `addBorder`, `Table.removeLineBetweenRows` and `rightAlignNumbers`,
      `TextColumn.addChineseParagraph`, `addJapaneseParagraph` and
      `removeLastParagraph`, `OptionalContentGroup.clear` and `drawOn` (not a
      `Drawable`), `BaseAnnotation.rotate`.
- ⬜ `Bidi.reorderVisually(str, from, to)` counts UTF-16 units in Java and C#,
      bytes in Go and scalars in Swift: document it in Port differences or
      use one unit.

### Names: one concept, several names

- ⬜ S Strokes. `Line`, `Path`, `Arc`, `Point` and `Rect` use
      `setStrokeColor`, `setStrokeWidth` and `setFillColor`; what is left:
      - dash pattern: `Line`/`Path.setPattern`,
        `Rect`/`Text`/`TextFrame.setBorderPattern`,
        `Chart.setHGridLinePattern`/`setVGridLinePattern`, against
        `setStrokeDashPattern` in `Page`, `Arc` and `Point`;
      - cap style: `Line.setCapStyle`, `setLineCapStyle` in `Path` and `Page`;
      - width: `Page.setPenWidth` and `setDefaultLineWidth` (the pen width),
        `Form.setLineWidth`, `TextBox.setLineWidth`/`getLineWidth` next to
        `setStrokeWidth` (no getter), `Chart.setChartBorderWidth`,
        `setInnerBorderWidth`, `setHGridLineWidth`;
      - colour: `Page.setPenColor`/`setBrushColor`, `CheckBox.setBoxColor`
        and `setCheckmark` (a colour), `Paragraph.setColor` (the text colour),
        `QRCode`/`DataMatrix.setColor`, `TextLine.setLineColor` (underline and
        strikeout);
      - borders: `TextBox.setStrokeColor`/`setStrokeWidth` against
        `TextBlock.setBorderColor`/`setBorderWidth`;
        `Table.setCellBordersColor`/`setCellBordersWidth`;
        `Rect.setCornerRadius` against `TextBlock.setBorderCornerRadius`.
- ⬜ S Rotation: `Arc.setRotateDegreesCW`/`CCW`; `Container` and `Stamp` have
      `rotate`, `setRotation` and `setRotationCounterClockwise` for one angle,
      plus `setRotationClockwise`; `Image.rotateClockwise` sets the angle;
      `Page.rotateBy` sets an absolute `/Rotate`; `BaseAnnotation.rotate` is a
      public `Container` helper.
- ⬜ S Scaling and moving: `Arc.setScaleFactor` multiplies the radii, so it
      is a `scaleBy`; `Container.setScaleFactor`/`setScaleFactorXY` set an
      absolute scale; `Rect.scaleBy` scales x and y only. `Line.setLocation`
      moves only the start point (`setStartPoint` does that too);
      `Arc.setCenterXY` duplicates `setLocation`; `Title.setOffset` adds to x
      on every call; `TextParameters.setTextLocation`;
      `CompositeTextLine.getPosition`, and `getMinMax` returns y values only.
- ⬜ S Alignment: `Align` (int codes with `JUSTIFY`, `TOP`, `BOTTOM`) and
      `Alignment` (enum with `LEFT`, `RIGHT`, `CENTER`) for one concept;
      `Cell.setVerTextAlignment` against `TextBox.setVerticalAlignment`;
      `Table.setTextAlignInColumn` against `setTextAlignment`;
      `Point.setAlignment`.
- ⬜ S Text boxes: `TextBox` and `TextBlock` name one setting differently:
      `setMargin`/`setTextPadding` (`Cell.setPadding`), `setSpacing` in
      points/`setLineSpacing` as a multiplier,
      `setTextColors`/`setKeywordHighlightColors` (`setColorMap` in `TextLine`
      and `Paragraph`), `setTextDirection(Direction)`/`setRightToLeft`;
      `TextLine.setTextDirection(int)` is a rotation in degrees.
      `setFillColor` and `setBackgroundColor` do the same in both.
      `getHeight` is the set height in `TextBlock`, the measured one in
      `TextBox`. `TextBox.setBorder(int)` only adds bits, so `Border.NONE`
      does nothing; `Cell` has `setBorder(int, boolean)`, `TextFrame`
      `setBorder(boolean)` with a blue default. `TextColumn.setParagraphSpacing`
      is a multiplier, `Text`/`TextFrame.setParagraphLeading` points.
- ⬜ S Barcodes: `PDF417.setModuleWidth`, `setModuleLength` in `Barcode`,
      `QRCode` and `DataMatrix`; `QRCode.getData` returns the modules;
      `ErrorCorrectLevel` for error correction level;
      `Barcode.LEFT_TO_RIGHT`, `TOP_TO_BOTTOM` and `BOTTOM_TO_TOP` duplicate
      `Direction`, and EAN-13 and UPC-A ignore the direction.

### Names: misleading, redundant or dead

- ⬜ S Duplicates: `Table.getCellAt`/`getCellAtRowColumn`,
      `getRow`/`getRowAtIndex`, `getColumn`/`getColumnAtIndex`;
      `Font.getHeight`/`getBodyHeight`; Java and Swift
      `Permissions.getAccess`/`getRawValue`;
      `TextColumn.addChineseParagraph`/`addJapaneseParagraph`;
      `Container.addBorder()` is `setBorderColor(Color.black)`, and
      `setBorderColor` adds another `Rect` on each call; `Cell.getBorder(int)`
      next to `getTopBorder` and the other sides.
- ⬜ S Misleading: `BaseAnnotation.setTransparency` writes `/CA`, an opacity;
      `FileAttachment.setDescription` writes `/Contents`, which
      `BaseAnnotation` calls `setContents`; `DonutChart.setR1AndR2`;
      `Bookmark.getDestKey` returns the name given to `Page.addDestination`;
      `Table.setColumnWidths()` fits the columns; `Image.flipUpsideDown` takes
      a flag; `Point.setDrawPath()` cannot be turned off; `Cell.setPoint` sets
      a marker, not a location; `Page.addBMC` writes `BDC`; `Page.drawArc`
      and `drawCircularArc` only append path segments;
      `FileAttachment.setIconPushPin` against `setIconPaperclip`;
      `Chart.setDrawXAxisLines` against `setHGridLineWidth` for the same
      lines; `Effect` means superscript and subscript; `ColorMap` (CSS names)
      against `TextLine.setColorMap` (word colours); `Color` constants are
      lower case, unlike every other constant class, and `oldgloryred` and
      `oldgloryblue` are not CSS names; `B5` is JIS B5 (516×729), not ISO;
      `Table.WITH_2_HEADER_ROWS` is the number 2; `Page.transform` documents
      9 values and reads 6 (`Page.java:2479`);
      `Permissions.setPermissions(flags, grant)` also revokes.
- ⬜ S Ignored or dead: the `Font(pdf, stream, Font.STREAM)` flag
      (`Font.java:303`); the `pdf` the `FileAttachment` constructor stores;
      `Slice.tooltip`; `BigTable.setLanguage`; `Image` has no `setLanguage`
      though `drawOn` reads the field; the `SVGImage.drawOn` link branch that
      no setter reaches; `TextLine.setURILanguage`, `setURIAltDescription` and
      `setURIActualText` have no getters; the public `BaseAnnotation()` makes
      an annotation with no subtype, which crashes Java and Swift `PDF` and
      writes `/Subtype /` in Go; the `Destination` constructors are public
      and nothing public takes a `Destination`.
- ⬜ S Public by accident: `Token` (mutable byte arrays), `Single` (one
      `space` constant; in C# it hides `System.Single`),
      `TextUtils.printDuration` (an examples helper; C# formats it in the
      current culture), the core font metrics classes (`Courier_Bold`, ...),
      the Java `PDFobj` protected fields and `setStream`, `setNumber` and
      `getLength`, the public no-argument constructors of the Java constant
      classes (`new A4()`, `new Color()`), and `Encryption.getKey`, which only
      Java needs public.

### Names in one port

- ⬜ S C#: `SVGImage.getWidth` and `getHeight` are lower case
      (`SVGImage.cs:213`); `Compliance.PDF_1_7` is `PDF_17` in the other
      ports (`PDF_1_7` reads better next to `PDF_UA_1`); `PathOperator`,
      `Token`, `UserAccess` and `Point.ControlPointC`/`V`/`Y` are PascalCase
      where the other C# constant classes copy Java's `UPPER_SNAKE`.
- ⬜ S Swift: `PathOperator`, `Token` and `Point.controlPointC`/`V`/`Y` are
      camelCase and `StructElem` is PascalCase (`Document`, `THead`) where the
      other Swift constant classes copy Java's `UPPER_SNAKE`.
- ⬜ S Go: `RadioButton.SelectButton` and `CheckBox.XMarkCheckBox`, whose
      suffixes no overload explains; `NewImage2`; `Page.GetPenColorRGB` and
      `GetBrushColorRGB` with no plain `GetPenColor` and `GetBrushColor`;
      `mark.UnCheck`; `tabloid.PORTRAIT` and `LANDSCAPE` where the other page
      size packages have `Portrait` and `Landscape`; the `djb` package, whose
      `Salsa20()` returns a document ID; the `Courier` … `ZapfDingbats`
      constants in `src/corefont.go`, numbered from 0 and named like the
      `corefont.Courier()` functions; `ValidBitsMask`.
- ⬜ S Go exports helpers that Week 4 meant to hide and the README does not
      list: `BitBuffer`, `RSBlock`, `Polynomial`, `TextCompact`, `L5ECC`,
      `Round` and the `src/round` package, `JPGImage`, `BMPImage`,
      `FontStream1`, `FontStream2`, `NewCoreFontForPDFobj`.
- ⬜ S Java: the sources in `com/pdfjet/fonts`, `qrcode`, `pdf417` and
      `datamatrix` declare `package com.pdfjet`, while `barcodes`,
      `corefonts` and `encryption` have their own packages.

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
      script's report now shows only those. Go still exports some helpers
      (`BitBuffer`, `RSBlock`, `JPGImage`, ...); see the API audit.
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
      `setImage` and `setBarcode` do; `setFont` sets only the font, as in
      `TextLine` and `TextBox`, and `Table.setFontInRow` and `setFontInColumn`
      set the font and the font size of the cells; Go and Swift use the cell font as the
      fallback font, as Java and C# do; the text width, the underline, the
      strikeout and the link of a cell are measured at the cell's font size
      with the fallback font; `Align.JUSTIFY` draws the text, image or
      barcode of a cell left aligned instead of throwing; a right aligned
      image or barcode keeps the right padding, not the left one; Swift
      `getBackgroundColor` returns an optional and `setBackgroundColor(nil)`
      removes the background; the unused Java `colspan` and Java, C# and
      Swift `strokeDashPattern` fields are removed; `setStrokeWidth` sets the
      width the borders are drawn with, and `setLineWidth` and `getLineWidth`,
      which did that before while `setStrokeWidth` did nothing, are removed.
      `TextBox`: `setLineWidth` sets the width the borders are drawn with, as
      `setStrokeWidth` does, and `getLineWidth` returns it; before, both used
      a field that nothing drew with; measuring a text box that grows to fit
      its text with `drawOn(null)`, as `Cell.getHeight` does, or drawing it
      twice no longer turns it into a text box of fixed height, which could
      cut its last line to "...".
      `Text` and `TextFrame` keep the vertical offset, including the offset of
      a superscript or subscript, the URI and the GoTo action of a text line
      when they wrap it, as `TextColumn` does; Java and Go
      `Content.ofTextFile`, and so `Util.readLines` and
      `Text.paragraphsFromFile`, drop a byte order mark at the start of the
      file, as C# and Swift do.
      `Line` and `Path` `setWidth` and `setColor` are renamed `setStrokeWidth`
      and `setStrokeColor`, as in `Arc` and `Point`; `Container.add`,
      `Path.add`, `OptionalContentGroup.add`, `DonutChart.addSlice`,
      `CompositeTextLine.addComponent` and `TextColumn.addParagraph` return
      the object they are called on; `Chart.toFloatArray` is no longer
      public, and `Encryption.getObjNumber` is internal in C#, Go and Swift;
      Java `PDF.newobj` and `endobj` are named `newObj` and `endObj`.
      `Text.readLines` moves to `Util.readLines`, which is public in Java and
      C# and new in Swift; in Go `pdfjet.ReadLines` is `util.ReadLines` in
      the new `util` package.
      `Path.setLocation` sets the offset of the path instead of adding to it,
      so a second call no longer moves the path further, and `Path.scaleBy`
      returns the path.
      `Line.setPointA` and `setPointB` are removed; they did what
      `setStartPoint` and `setEndPoint` do.
      `Paragraph.setAlignment` and `TextColumn.setAlignment` are renamed
      `setTextAlignment`, as in `Cell`, `TextBox` and `TextBlock`.
      Then: Data Matrix barcodes (Example_14), Swift encryption, random salts, `EncryptMetadata true`, right to
      left fixes, TODO cleanups, and the fixes and renames from the API audit.
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
  