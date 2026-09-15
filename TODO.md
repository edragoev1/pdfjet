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
      `QRCode` and `PDF417`, which implement `Drawable` since the API audit.
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
- ✅ S `Permissions`/`UserAccess` stayed int based in Java and Swift (Sep 13);
      the second audit made them typed on Sep 14, see "Second audit,
      permission types" below.
- ✅ **B** Decide which renames from the API audit below go into 9.0.0: all
      of them, and every other change the audit lists (decided Sep 13).
      They are breaking changes, so they land before the CHANGELOG entry.

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
Paths are Java unless a port is named. Everything here goes into 9.0.0, the
renames included (the Week 1 decision), so every item is a blocker.

### Bugs found by comparing the ports

- ✅ **B** Go gives every PDF the same `/ID` and XMP DocumentID:
      `djb.Salsa20()` hashes a fixed test vector and never reads the clock
      (`src/djb/salsa20.go:19`, `pdf.go:112`). Java, C# and Swift seed from
      the time.
      Fixed: Go hashes the time in milliseconds, as Java does; two runs of
      Example_01 get different IDs.
- ✅ **B** `PDF.setTitle`, `setAuthor`, `setSubject`, `setKeywords` and
      `setCreator` do nothing for `PDF_17`, the default compliance: they only
      feed the XMP stream, which is written for PDF/A and PDF/UA only, and the
      trailer has no `/Info` (`PDF.java:1171`, `:1220`). All four ports.
      Fixed: every PDF gets an `/Info` object with the properties that are
      set, `/Producer` and `/CreationDate` (from the XMP date), in UTF-16BE
      and encrypted with the document; pdfinfo shows them for a PDF 1.7 and
      an encrypted file, and the PDF/A and PDF/UA examples pass veraPDF.
- ✅ **B** Java `RadioButton.setLocation(double, double)` calls itself and
      throws `StackOverflowError` (`RadioButton.java:69`).
      Fixed: it casts to float, as C# does.
- ✅ **B** Go `NewCircleAnnotation`, `NewSquareAnnotation`,
      `NewPolygonAnnotation` and `NewTextAnnotation` skip `NewBaseAnnotation`:
      the fill is black and the transparency 0, and `/CA 0` makes the
      annotation invisible (`circleannotation.go:10`). Go `Container.DrawOn`
      offsets only these four types, Java any `BaseAnnotation`.
      Fixed: the four constructors start from `NewBaseAnnotation`, and
      `Container.DrawOn` offsets any type that embeds `BaseAnnotation`.
- ✅ **B** Go `QRCode` and `PDF417` are not `Drawable`, despite the Week 1
      item: `SetLocation` returns the concrete type and `DrawOn` returns
      `[]float32` (`qrcode.go:58,81`, `pdf417.go:132,240`). Go `DataMatrix`
      is right.
      Fixed: both follow `DataMatrix`; no caller had to change.
- ✅ **B** Swift `OptionalContentGroup` keeps `visible`, `printable` and
      `exportable` as `Bool?` and tests `!= nil`, so `setVisible(false)`
      writes `/ON` (`OptionalContentGroup.swift:24,88`).
      Fixed: plain `Bool`s, false by default, as in Java.
- ✅ **B** C# `CompositeTextLine` is another algorithm: it lays the lines out
      at draw time and always calls `SetFontSize(fontSize)`, so a line with no
      font size draws at size 0; `GetMinMax` changes the font sizes and
      `GetWidth` adds up the line widths (`CompositeTextLine.cs:138,205-273`).
      Fixed: Java's code ported as it is; a line with no font size keeps the
      sizes of its lines, and the width is Java's.
- ✅ **B** Swift `Executive.PORTRAIT` and `LANDSCAPE` are `[Double]`, so
      `Page(pdf, Executive.PORTRAIT)` does not compile (`Executive.swift:16`).
      Fixed: typed `[Float]`; the other page sizes already were.
- ✅ **B** C# `Compressor.Deflate` returns no bytes for empty input, which is
      not a zlib stream, so an empty page gets a broken `/FlateDecode` stream
      (`Compressor.cs:18`).
      Fixed: the early return is gone; an empty page gets the 8 byte zlib
      stream Java writes, and MuPDF no longer warns.
- ✅ **B** C# `SVG` parses path numbers with `float.Parse` in the current
      culture, so SVG images break on a German or French system
      (`SVG.cs:100`).
      Fixed: `SVG.cs` and the colours in `SVGImage.cs` parse in the
      invariant culture; de-DE and fr-FR give Java's output.
- ✅ **B** 8-bit indexed PNG: Java, C# and Swift undo the row filters on the
      RGB bytes after the palette lookup, 3 bytes per pixel, instead of on the
      indexes; Go never undoes them (`PNGImage.java:342`, `pngimage.go:313`).
      Fixed: one function reads 1, 2, 4 and 8 bit palette images, undoing
      the filters on the packed indexes (one byte per pixel) before the
      palette and tRNS lookup; 1, 2 and 4 bit grayscale images never undid
      their filters and do now. 468 PngSuite images re-encoded with every
      filter, with and without tRNS, match PIL in the four ports.
- ✅ **B** `Chart`: Java and Swift start the automatic maximums at the
      smallest positive float (`Float.MIN_VALUE`, `leastNonzeroMagnitude`),
      so all-negative data gets a maximum of 0 (`Chart.java:40`); C# rounds
      axis limits the user set (`Chart.cs:501`); Go never uses the automatic
      colours (`chart.go:548`); Swift `drawOn` crashes on empty data and
      divides by zero on flat data.
      Fixed: the maximums start at minus the largest float; C# rounds only
      the ranges it computes; Go colours a series with no stroke colour
      from the palette and keeps an explicit black; with no points
      `drawOn` draws nothing and an empty series is skipped; flat data gets
      a range of the value to the value plus 1 before rounding. Example_09,
      Example_39 and Example_40 are unchanged.
- ✅ **B** Bar charts were a two point path with a 20 point stroke on the XY
      `Chart`: the bars at the ends of the rounded axis stuck out of the plot
      (Example_39 covered its title and X axis labels), the bar text sat in the
      upper half of a horizontal bar, `setXYChart(false)` mapped x as a
      fraction of the chart width so Example_40 computed bar positions in
      page points, the month names were repeated in every bar with no legend,
      and integer data got `0.00` labels. Stroke widths were scaled by the
      plot to chart width ratio in XY mode only (`Chart.java:486`), and the
      titles were centered at the font size but drawn at `fontSize`.
      Fixed (Sep 14): new `BarChart` in the four ports (categories, grouped
      series, horizontal or vertical, legend, value labels, value axis that
      includes 0, tests) and Example_39 and Example_40 rewritten with it;
      `Chart` loses `setXYChart` and the stroke scaling, centers the titles
      at the drawn size, centers the path text across the stroke, and writes
      axis labels with the decimal places of the axis step.
- ✅ **B** `Text` duplicated `TextFrame`: a frame without a height draws the
      same paragraphs the same way (Example_03 drawn with either has the same
      content stream). Removed (Sep 14) in the four ports; Example_03, 41 and
      49 use `TextFrame` with `setBorders(true)`, and `Text.paragraphsFromFile`
      is `Paragraph.paragraphsFromFile` (Go `ParagraphsFromFile`).
- ✅ **B** Colours came in three forms, an `int`, three floats and an array,
      and each class offered a different subset per port (`Line` one form in
      Java, two in C#; `Point` two in Java, one in Go and Swift). Decided
      Sep 14: the `int` for the everyday case and the array for any colour,
      which is the internal representation, on every colour setter in every
      port; no three-float overloads. Fixed: the triples are removed and the
      array form is added where only the `int` was (`Line`, `Path`,
      `CheckBox`, `Container`, `Paragraph`, `Table`, Go `Point`, Swift `Rect`
      and `Page`).
- ✅ **B** `DonutChart` took an angle per slice, so Example_25 passed 90, 72
      and 108 to mean 25, 20 and 30 percent, and a constructor flag made a
      pie. Fixed (Sep 14): `Slice(value, color, label)` and the chart computes
      the angles and percentages; an inner radius of 0 is a pie; the `Slice`
      fields are hidden; Example_25 draws the same PDF; tests in the four ports.
- ✅ S `BarChart.setStacked`: the series stacked in each category, the value
      axis rounded to the sums of each category, the values above 0 up from 0
      and below 0 down, value labels inside the segments that have room.
      Example_40 draws the grouped and the stacked chart of the same data.
- ✅ **B** Constants that escaped the v9 typing: the `Point` shapes were int
      constants and `StructElem` string constants in Java, C# and Swift, Go's
      `shape` and `structtype` constants were untyped, and `Compress.YES` was
      a boolean with one caller. Fixed (Sep 14): `Shape` and `StructElem`
      enums in the four ports (Go `shape.Shape` and `structelem.StructElem`),
      `Point.setShape`, `TextLine.setStructureType` and `Page.addBDC` take
      them, the structure tree record is the internal `StructElement`, and
      `EmbeddedFile` takes a boolean; `Compress` is gone.
- ✅ **B** Java doubled 74 float setters with a `double` overload that only
      cast, C# 63, and the coverage was arbitrary (`Point` seven, `Cell` none);
      Swift and Go never had them. Fixed (Sep 14): the overloads are removed,
      the handful of methods that took only a `double` take a `float`, and the
      eight example call sites write `7f`, `1f`, `2.0f` and a `float` variable.
- ✅ **B** Permissions drift: C# had `Access` and `GetAccess`, `RawValue` and
      `GetRawValue` and `Can*` properties, Go had `Has`, `Add` and `Remove` on
      `UserAccess` and two constructors from an int, Java and Swift had
      `getValue` and `isSetIn`. Fixed (Sep 14): one getter, `getAccess`, and
      one query, `UserAccess.isSetIn` (Go `IsSetIn`, C# `HasFlag`), in every
      port; `getRawValue`, the C# properties, Go `Add`, `Remove` and
      `NewPermissionsFromUint32` are gone.
- ✅ **B** Go exported twenty types the other ports hide. Fixed (Sep 14): the
      helper packages are under `src/internal`, `embed` (unused) is gone, the
      annotation, PNG chunk, font table, OCG, encryption key, SVG and saved
      state types and the BMP and JPEG decoder methods are unexported, and
      `content.GetFromReader` is `GetFromStream`. The Go-only members that
      stay are the documented conventions: `corefont` fields and
      `Encryption` methods the `pdfjet` package needs across packages, the
      `AddCoreFontResource` family, `qrcode.ErrorCorrectionLevelL`, and the
      `Paragraph` and `Title` getters that wait for the public fields item.
- ✅ **B** Public fields: `Paragraph` exposed six coordinates, `Container` seven
      layout fields and `Title` its two `TextLine` objects in Java, C# and
      Swift. Fixed (Sep 14): the fields are private, `Paragraph` and `Title`
      have the getters Go had (`getX1`, `getTextX`, `getPrefix`, ...), and
      Example_03, 41 and 48 call them; the audit lists no Go-only getter.
- ✅ **B** Chart leftovers: `setFontSize` drew everything at 8 points whatever
      the fonts' sizes, `setAutoColors` had no clear meaning, and `slope` and
      `intercept` were statistics on a chart object. Fixed (Sep 14): `Chart`
      draws with the sizes of its fonts like `BarChart`, the palette colors a
      series without a color, and Example_09 computes its own trend line, in
      the four ports; Example_09 draws the same PDF.
- ✅ **B** The rename sweep (Sep 14): one `setRotation`, counterclockwise, on
      `Arc`, `Container`, `Stamp`, `Image` and `Page`, and `TextBox.setTextRotation`
      for `setTextDirection`; `setBorderDashPattern` for `setBorderPattern`;
      `Container.scaleBy`; `Page.drawString` down to three public forms, with
      Example_32 on `TextLine`; `Image` detects PNG, JPEG and BMP from the first
      bytes and `ImageType` is internal; the gap (points) and spacing (multiple)
      rule documented. Only Example_32 draws differently: `TextLine` sets the
      color before each line.
- ✅ **B** Java internals (Sep 14): `PDF.append`, `newObj`, `endObj` and
      `getObjNumber` package-private, with `Encryption` and the AES classes
      moved to `com.pdfjet` so they can be; `ColorMap` and `Util` internal in
      the four ports; `Barcode.drawOnPageAtLocation` removed; `TextUtils`
      removed, the examples print their duration themselves; the two font
      generators in `util/` with their scripts; `Util.readLines` is
      `Content.linesOfTextFile`. The audit report is 103 lines, from 261 at
      the start of the day.
- ✅ **B** `CalendarMonth`: Swift takes the weekday of day 0, so a month that
      starts on a Sunday is a row low (`CalendarMonth.swift:39`); Go lays the
      calendar out differently and places the header with `x1` as the y
      (`calendarmonth.go:89`); the default location, cell size and circle
      pen width differ in all four ports.
      Fixed: C#, Go and Swift lay the calendar out as Java does, with its
      defaults: location (0, 0), a cell width twice the widest day name,
      square cells, a 1.25 point circle; Swift takes the weekday of day 1;
      Java takes a `GregorianCalendar`, as a Thai default locale put the
      days in the wrong columns.
- ✅ **B** Go `Barcode` draws the UPC, EAN-13 and Code 39 text at the
      barcode's own `x1`, `y1` instead of the location it is drawn at, so the
      text of a barcode in a table cell is misplaced (`barcode.go:296,494`).
      Fixed: the text uses the location arguments, as in Java.
- ✅ **B** Go `NewForm` leaves the label and value font sizes and the form
      width at 0 (`form.go:28`); Swift defaults to 8 and 10 where Java has 9
      and 9 (`Form.swift:17`).
      Fixed: 9 and 9 points and a width of 500 in Go, 9 and 9 in Swift.
- ✅ **B** `BigTable`: Java splits lines with a regex and drops the empty
      fields at the end of a line (`BigTable.java:249,295`), as `Table` did;
      in all four ports `setLocation` adds to x, so a second call moves the
      table again, and `setLanguage` stores a field nothing reads.
      Fixed: Java splits at the delimiter with `indexOf` and keeps the empty
      fields; `setLocation` stores x and recomputes the vertical lines, also
      before `setTableData`. `setLanguage` is removed: BigTable writes no
      marked content for a language to go to.
- ✅ **B** `TextColumn.setTextAlignment` has no effect: `drawOn` replaces it
      with each paragraph's alignment. `drawOn` also compares the column
      height with a y coordinate (`TextColumn.java:233,238`). All four ports.
      Fixed: a paragraph remembers whether `setTextAlignment` was called, and
      the column's alignment applies to the paragraphs that did not set one;
      the returned corner reaches at least the location plus the height, in
      the direction the lines advance when the column is rotated.
- ✅ **B** Wrapping drops text line settings: `Text` and `TextFrame` lose the
      line colour, structure type, text direction, alt description and the
      URI language, alt and actual text (`Text.java:231`,
      `TextFrame.java:410`); `TextColumn` loses the colour map and language
      (`TextColumn.java:270`). All four ports.
      Fixed: `TextLine.copyWithText` copies every setting, and `Text`,
      `TextFrame` and `TextColumn` use it; an alt description is kept only
      if it was set, otherwise each piece's text is its own.
- ✅ **B** `Table.drawOn(null)` renders every row and sets `rendered = -1`,
      so a later `drawOn(page)` draws only the header rows
      (`Table.java:583`). All four ports.
      Fixed: measuring walks a local row index and only a page moves the
      next row to draw. It was worse than listed: the later `drawOn(page)`
      crashed on index -1, and so did measuring between two pages.
- ✅ `PDF417.drawOn` overwrites `x1`, so a second draw shifts the symbol
      right (`PDF417.java:275`). All four ports.
      Fixed: each row of codewords starts at a local `x0`.
- ✅ `CheckBox` and `RadioButton` set a blue brush for a linked label, and the
      five-argument `drawString` resets it to black (`CheckBox.java:222`,
      `Page.java:464`). All four ports.
      Fixed: the label goes to the `drawString` form that takes a colour;
      the linked labels of Example_26 are blue in the four ports.
- ✅ **B** `TextBox` draws underline and strikeout in the border colour, not the
      text colour (`TextBox.java:937`). All four ports.
      Fixed: the pen takes the text colour before the lines are drawn.
- ✅ **B** `TextBlock` measures with the fallback font and draws with the main font
      only (`Page.java:2664`); `setFallbackFontSize` resizes the shared
      `Font`. All four ports.
      Fixed: `Page.drawTextBlock` (internal) takes the fallback font and its
      size and switches fonts as `drawString` does; `setFallbackFontSize`
      stores a size in the text block, the font size by default, and a null
      fallback font works. Text blocks without a fallback font write the
      same content stream as before.
- ✅ `Cell.setTextBlock` and `setTextColumn` keep the cell text, unlike
      `setTextBox`, and `drawOn` draws the text while `getHeight` measures
      the block (`Cell.java:274,294,860`); `getHeight` and column fitting
      ignore `setFontSize`. All four ports.
      Fixed: both clear the text; `getHeight`, `Table.setColumnWidths()`
      and the wrapping of cell text, with its continuation cells, measure
      at the cell font size. Example_29 shows its text column instead of
      the word "World".
- ✅ `Page.getPenWidth` is 0.5 on a new page but no `w` is written, so readers
      draw 1.0 (`Page.java:60`); the CMYK setters do not update `getPenColor`
      and `getBrushColor`. All four ports.
      Fixed: no port skips writing `w`, `RG` or `rg` any more, so only the
      getters were wrong: a new page tracks a width of 1, the CMYK setters
      track the RGB the specification converts them to, and
      `restoreGraphicsState` restores the tracked state with `State`. No
      content stream changes.
- ✅ **B** Java and Swift `FontStream1` read the font with one `read()` and no loop,
      so a short read corrupts it (`FontStream1.java:367`).
      Fixed: both read fully and fail on a truncated stream; a stream that
      returns 3 bytes per read gives the same PDF as a normal one.
- ✅ **B** `SVGImage`: Go passes a `structureType` nothing sets to `AddBMC`, an
      empty tag in PDF/UA (`svgimage.go:326`); Java and C# never close the
      file (`SVGImage.java:53`); Swift scans for `" d="` and `" fill="`
      instead of parsing XML, losing single-quoted attributes and attributes
      after a newline (`SVGImage.swift:82`).
      Fixed: Go tags `/P` with a space as alt and actual text; Java and C#
      close the file; Swift tokenizes start tags like an XML parser
      (quotes, line breaks, attribute order, comments, character
      references), and the 71 SVG files in `images/` match Java.
- ✅ `Container`: Swift `add` never sets `parent`, so annotations in a nested
      container miss the offset (`Container.swift:139`); C# compares
      `GetType() == typeof(Container)`, which misses a subclass.
      Fixed: Swift sets `parent`, C# tests `element is Container`.
- ✅ Swift `Bookmark` collapses only spaces in a title, not tabs and newlines
      (`Bookmark.swift:63`); `getDestKey` and `getTitle` crash on the root.
      Fixed: Swift collapses what Java's `\s` matches, and both getters
      return `String?`, nil for the root.

### Drift between the ports

- ✅ **B** Defaults: C# `CheckBox` check mark is blue, black elsewhere
      (`CheckBox.cs:22`); Go `FileAttachment` description ends "the attached
      attachment" (`fileattachment.go:27`); Java `new PDF()` has a null
      compliance, `PDF_17` elsewhere (`PDF.java:62`); a fill-only `Arc` gets a
      black hairline stroke (`B`) in Java, C# and Swift and is only filled
      (`f`) in Go (`Arc.java:22`, `arc.go:243`); Go `NewImage2` and
      `SVGImage` use empty alt and actual text, `" "` elsewhere; Java and C#
      `FileAttachment` write an empty `/T <>`, Go and Swift skip it.
      Fixed: all four use Java's defaults, and Java's `new PDF()` is
      `PDF_17`. `Arc` has no stroke colour by default, so a fill-only arc is
      only filled in the four ports, as `Point` and `Rect` do; with no colour
      at all it still gets a black hairline. Java and C# also skip an empty
      `/Contents`. No example PDF changes.
- ✅ **B** Numbers written: `FastFloat` rounds `-1.125` to `-1.12` in Java and C#
      and `-1.13` in Go and Swift, and overflows `int` above 21.5 million in
      Java and C# (`FastFloat.java:11`); Swift `OpenTypeFont` scales
      `/FontBBox`, `/Ascent`, `/Descent` and `/CapHeight` to 1/1000 em where
      the others write font units (`OpenTypeFont.swift:116`); Swift
      `FontStream2` writes `/W` widths as `600.0`.
      Fixed: `FastFloat` widens the float to a double, rounds hundredths
      halves away from zero and writes whole numbers of 2^23 or more with all
      their digits; 100,064 float bit patterns give the same bytes in the
      four ports. Some numbers in 10 example PDFs change by 0.01, the same
      way in every port. The font descriptor metrics are in 1/1000 em in the
      four ports through `OpenTypeFont.toGlyphSpace`, Swift writes integer
      widths, and C# rounds half widths away from zero like the others.
- ✅ **B** `Chart` axis labels use the locale in Java and C# and `.` in Go and
      Swift; Go ignores `SetMinimumFractionDigits` (`chart.go:402`); C#
      places the Y labels with the ascent at the chart font size. Go
      `DonutChart` computes percentages in float64 and rounds some slices
      differently (`donutchart.go:249`).
      Fixed: the labels round the exact value half to even and use `.`, no
      grouping and no `-0` in the four ports, whatever the locale; Go honours
      `SetMinimumFractionDigits`; C# places the Y labels with the font's
      ascent; Go `DonutChart` computes percentages in float32.
- ✅ **B** Text input: Java `TextBlock` splits `"\n"` into no lines, the others into
      one (`TextBlock.java:456`); Java and Go `Table` keep a BOM in the first
      header cell (`Table.java:71`); C# `Content.ofTextFile` also reads
      UTF-16 and UTF-32 BOMs; Go `Cell` measures an empty cell one line tall,
      the others 0 (`cell.go:299`).
      Fixed: Java `TextBlock` gives one empty line; Java and Go `Table` drop a
      BOM from the first line; C# `Content.ofTextFile` and the `Table` file
      constructor read UTF-8 only and drop a BOM, and C# has
      `getFromStream(stream, bufferSize)`; a Go cell without text (a nil
      text in Java) is 0 tall, and wrapping skips it, while a `""` cell is
      one line tall, as in Java.
- ✅ **B** Copies and live references: Swift `PDFobj.getDict()` returns a copy, so
      a read object cannot be edited (`PDFobj.swift:39`); Java and C# `Page`
      and `TextLine` colour getters return the internal array; Go
      `Page.GetContent` returns the live buffer; C# `Encryption.GetKey`
      returns the key, Java a clone (`Encryption.cs:150`).
      Fixed: Swift `PDFobj.dict` is public and can be edited in place
      (`getDict()` still returns a copy); Java and C# `Page` and `TextLine`
      colour getters return copies and their array setters copy; Go
      `Page.GetContent` and C# `Encryption.getKey` return copies.
- ✅ **B** Mutable constants: the page sizes and the `Token` byte arrays could be
      changed by callers in Java, C# and Go (`A4.PORTRAIT[0] = 100` changed
      every later page). Fixed: `Token` is not public in any port (Go:
      `src/internal/token`); a page size is an immutable `PageSize` in the four
      ports (decided Sep 13), with a constructor and `getWidth`/`getHeight`,
      and Go's page sizes are functions (`a4.Portrait()`) returning
      `pagesize.PageSize`. Assigning to or indexing a constant no longer
      compiles, and all 200 example PDFs are unchanged.
- ✅ **B** Found while fixing the drift above: the Java and C# colour setters
      and getters of `Cell`, `TextBox`, `TextBlock`, `Point`, `Arc`, `Rect`,
      `Stamp`, `Text`, `TextFrame`, `Form` and `BaseAnnotation` kept the
      caller's array; `BigTable` read its file differently in each port; Java
      and C# `Content.ofTextFile` hid a missing file; Swift
      `BufferedOutputStream` printed write errors. All fixed, with the
      decisions of Sep 13: Swift `PDF.complete()` and `PDF.addObjects` throw,
      and Go `PDF.Read` and `ReadWithPassword` return an error. Left as it is:
      a UTF-8 encoded surrogate (ED A0 80) reads as one U+FFFD in Java and
      three in Swift, which is what Unicode recommends.
- ✅ **B** Errors. Go exits with `log.Fatal` where Java throws: `ReadWithPassword`
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
      C# `FontStream1` stops without an error on a truncated font stream,
      where Java and Swift throw.
      Fixed: Go panics, or returns the error where the function returns one,
      instead of `log.Fatal` or ignoring errors, and `PDF.Read` and
      `ReadWithPassword` return `([]*PDFobj, error)`; Swift throws where Java
      throws, or stops with `fatalError` for invalid input, instead of
      printing and carrying on; C# throws its own Code 39 message and on a
      truncated font stream; `addObjects` without a root `/Pages` fails
      clearly in the four ports (Swift throws); the four ports read top-down
      BMP images. Go `GetPageObjects` still dereferences nil without a
      `/Pages` root, as Java does.
- ✅ **B** Second audit drift (Sep 14, `api-suggestions.html`): empty document
      properties and annotation strings written by three ports; Go and Swift
      stopping the program in `drawPath`, `SetTextRenderingMode`,
      `AddObjects`, `Complete` and `NewPDFFile`; no reading-only Go `PDF`;
      C# and Swift warnings on stdout; Go `GetMinMaxY` slice, `AddObjects`
      pointer, Swift labels and `inout`, missing `@discardableResult`, C#
      colour setters, saved state order; Go `NewCell` only, four cell flag
      layouts, Swift `Table.getWidth`, `RadioButton.setFontSize` resizing the
      font, `Form` fields not copied, nil page handling; `getComponents`
      copies, unchecked core font number; stale Go comments and the
      Javadoc tags. Fixed in the four ports; Go keeps `AddBDC` with the
      language, `NewParagraph()` and an array `SetBorderColorRGB` by the
      documented convention.
- ✅ **B** Second audit, inconsistencies shared by the four ports (Sep 14,
      `api-suggestions.html`): alt text and actual text defaulting to a
      space in seven drawables and to null in the rest; `Color.transparent`
      making the text of a `TextBlock` or a `Cell` white; three `setFont`
      fallback rules; `Chart` drawing a hairline for a border width of 0 and
      lacking the subtitle, grid color and axis line settings of `BarChart`.
      Fixed in the four ports with unit tests: the texts default to none, the
      text setters ignore `Color.transparent`, the fallback font follows the
      font unless another was set, and `Chart` has the `BarChart` rules and
      setters. `TextColumn.getHeight`/`getSize` and the missing text getters
      stay for 10.0 with the `TextColumn` fold.
- ✅ **B** Second audit, a field and its setter disagree (Sep 14,
      `api-suggestions.html`): the fields are named after their setters in
      the four ports: `opacity` in `BaseAnnotation` and `Annotation`,
      `borderColor` and `borderWidth` in `Cell`, `borderColor` and
      `checkmarkColor` in `CheckBox`, `strokeWidth` in `Form`,
      `decorationColor` in `TextLine`, `hGridLineDashPattern` and
      `vGridLineDashPattern` in `Chart`, `paragraphGap` in `TextFrame`, and
      `superscriptFactor` and `subscriptFactor` in `CompositeTextLine`.
      Internal, so no caller changes.
- ✅ **B** `TextFrame.setParagraphGap` was the distance from baseline to
      baseline, so a gap smaller than the line overlapped the text. It is the
      space from the bottom of the text of a paragraph to the top of the text
      of the next, one empty line in the size of the next paragraph by default
      in both constructors, in the four ports with unit tests. Example_10 (`TextColumn`) does not change; the `TextColumn` and
      `TextFrame` paragraph spacings stay two until the 10.0 fold.
- ✅ Example_41 was a copy of Example_03 in the four ports (both moved from
      `Text` to `TextFrame` when `Text` went). Removed; Example_51, the merge
      example, is Example_41, and the scripts, workflows and example pages
      count 50 examples.
- ✅ **B** Second audit, one name with two meanings (Sep 14,
      `api-suggestions.html`): `DonutChart.setLocation` sets the top left
      corner of the outer circle, where it set the center, to match `drawOn`,
      which returns the bottom right corner; `Form.setFormWidth` is `setWidth`
      and `Path.setClosePath` is `setClosed`, with their fields, in the four
      ports. Example_25 and the unit tests place the chart at the same center.
- ✅ **B** Second audit, odd members (Sep 14, `api-suggestions.html`):
      `TextLine.getStringWidth(text)` measured any text with the font of the
      line, which is `Font.stringWidth`; removed from the four ports, with no
      callers. `getWidth` measures the text of the line.
      `TextLine.advance(leading)` moved the line down without saying so, and
      the location of a line could not be read; `getLocation` replaces it in
      the four ports, as in `TextBlock` and `CompositeTextLine`.
- ✅ **B** Second audit, permission types (Sep 14, `api-suggestions.html`):
      `Permissions` took the `UserAccess` values as an `int` in Java and Swift
      and as a typed value in C# and Go. Java `grant` and `revoke` take
      `UserAccess...` and `getAccess`/`setAccess` a `Set<UserAccess>`; the
      Swift `UserAccess` is an `OptionSet` combined with `|`, as in C# and Go;
      `isSetIn` takes the typed permissions. Example_30, the unit tests and
      the README port note follow.

### Types and signatures

- ✅ **B** Go constants are untyped ints in `compliance`, `alignment`, `capstyle`,
      `joinstyle`, `effect`, `mark` and `imagetype`; `type Compliance int`
      exists but `SetCompliance` takes `int`. Go `direction` has
      `BottomToTop` = 2, the others `BOTTOM_TO_TOP` = 1. Go `compress.Yes` is
      a `bool`.
      Fixed: the constants of `compliance`, `alignment`, `capstyle`,
      `joinstyle`, `effect`, `mark`, `imagetype`, `pathoperator`,
      `pagelayout` and `pagemode` have the type of their package, and the
      setters and getters take and return it; `direction` is in the order of
      the other ports; `compress.Compress` is an int type with `Yes` and `No`.
- ✅ **B** Swift takes a `PathOperator` enum in `Page.drawPath`, `drawCircle`,
      `drawRectRoundCorners` and `Point.setPathOperator`, and an `ImageType`
      enum in `Image.init`; Java, C# and Go take any `String` and `int`, so a
      typo writes a broken content stream. `PDF.setPageLayout` and
      `setPageMode` take a `String` in all ports though `PageLayout` and
      `PageMode` exist.
      Fixed: `PathOperator`, `ImageType`, `PageLayout` and `PageMode` are
      enums in Java, C# and Swift, with their names unchanged (C# keeps
      `PathOperator.Stroke`, see Names in one port), and typed constants in
      Go; `drawPath`, `drawCircle`, `drawRectRoundCorners`,
      `Stamp.drawPath`, `Point.setPathOperator`, the `Image` constructors,
      `setPageLayout` and `setPageMode` take them in the four ports.
- ✅ **B** `Point.setAlignment` takes an `int` in Java, an `Alignment` in C# and a
      `UInt32` in Swift; `Point.getTextColor` returns `int` in Java and C#,
      `[3]float32` in Go, `[Float]` in Swift. `TextBlock.setTextAlignment`
      takes an `Alignment`, `TextBox` and `Cell` an `Align` int.
      Fixed, as decided on Sep 13: `Align` is removed, and `Alignment` is one
      enum with `LEFT`, `RIGHT`, `CENTER`, `JUSTIFY`, `TOP` and `BOTTOM` that
      every alignment setter and getter takes or returns in the four ports,
      as Go's `alignment` package already had. `Cell` and `TextBox` keep the
      alignment in a field instead of two bits of `properties`, and `Table`
      copies it to the cells of a wrapped row. `Point.getTextColor` returns
      the red, green and blue components in the four ports, next to
      `setTextColor(float[])` in Java and C# and `SetTextColorRGB` in Go. The
      Java example PDFs are the same as before.
- ✅ **B** Swift: `Dimension.getWidth`/`getHeight` return `Float?`;
      `PNGImage.getWidth` is `Int?` (Java `int`, Go `float32`); `Cell.init`,
      `Cell.setFont`, `TextParameters.setFont` and `setText` take optionals
      and force-unwrap; `DonutChart.init` requires fonts Java allows to be
      null; `Title.prefix` and `textLine` are optional; `Cell.setColSpan`
      takes `UInt32`; `PDF.addObjects`, `addResourceObjects` and four
      `PDFobj` methods are `inout` and write nothing back; `RadioButton`
      setters lack `@discardableResult`; `PDF417.init` throws an internal
      `EncodingError`; `Page.addWatermark`, `addHeader` and `addFooter` are
      `throws` and never throw.
      Fixed: all of these. `PDFjetError` is public, with a description, and
      is what `PDF417.init` throws; `Cell.getColSpan` returns an `Int` too.
      The `PDFobj` methods that keep `inout` add objects to the list:
      `addResource` for a core font, `addContent`, `addPrefixContent` and
      `setGraphicsState`.
- ✅ **B** Go colour setters: `BaseAnnotation.SetFillColor` takes `[3]float32` and
      `SetFillColorInt` an int, the reverse of every other type;
      `Arc.SetFillColorRGB` takes `r, g, b` (plus `SetFillColorRGBArray`),
      `Rect.SetFillColorRGB` the array; `Stamp` colours are `int`, the rest
      `int32`; `Form.SetLabelColor`/`SetValueColor` take `int32`, which Java,
      C# and Swift lack; only `Arc` has `Float64` variants. Go `Cell`,
      `TextBlock` and `TextBox` colour getters return black when unset, and a
      Go cell background cannot be removed.
      Fixed: every Go type has `SetXxxColor(int32)` and
      `SetXxxColorRGB([3]float32)`; the `r, g, b` form of `Arc`,
      `SetFillColorRGBArray` and the `Float64` variants of `Arc` are removed,
      and `Stamp` takes `int32` and `[3]float32`. Java, C# and Swift `Form`
      take an `int` label and value colour too. Go `Cell.GetBackgroundColor`
      and `GetStrokeColor`, `TextBlock.GetBackgroundColor` and
      `TextBox.GetStrokeColor` return nil when the colour is not set, as Java
      returns null, and `Color.transparent` removes a cell background in the
      four ports, as it does for `TextBox` and `TextBlock`.
- ✅ **B** Overloads: Go `Page.DrawString(font1, font2, text, x, y)` has no font
      size, and Go and Swift lack Java's `(font, fallback, size, str, x, y,
      color, colorMap)`; Java `drawString` takes a boxed `Integer` colour. Go
      has no `NewTextBox(font, text, width, height)` and no `Title` text line
      getter; Swift `Rect` and `TextBox` lack the `r, g, b` colour setters;
      only Java has a public `SVGImage()`, which leaves the path list null;
      Go `BigTable.SetTableData` returns only `error`; Go `PDF.Read` takes
      `[]byte`, not in the README.
      Fixed: Go `DrawString(font, fallbackFont, fontSize, text, x, y)`, and
      `DrawStringUsingColor` for the `int32` colour; Java takes an `int`
      colour and Swift has the `Int32` form; Go `NewTextBoxWithSize` and
      `Title.GetTextLine`; Swift `Rect` and `TextBox` `r, g, b` setters; the
      Java `SVGImage()` is removed; Go `SetTableData` returns `(*BigTable,
      error)`; the README says that Go reads a `[]byte`.
- ✅ **B** Mutators that return void: `Rect.scaleBy`, `SVGImage.scaleBy`,
      `Image.rotateClockwise` (Swift returns the image), `Container.rotate`
      and `addBorder`, `Table.removeLineBetweenRows` and `rightAlignNumbers`,
      `TextColumn.addChineseParagraph`, `addJapaneseParagraph` and
      `removeLastParagraph`, `OptionalContentGroup.clear` and `drawOn` (not a
      `Drawable`), `BaseAnnotation.rotate`.
      Fixed: they return the object they are called on in the four ports, and
      `OptionalContentGroup.drawOn` returns the largest x and y of the
      corners of its drawables; it stays out of `Drawable`, since a group has
      no location of its own.
- ✅ **B** `Bidi.reorderVisually(str, from, to)` counts UTF-16 units in Java and C#,
      bytes in Go and scalars in Swift: document it in Port differences or
      use one unit.
      Fixed: documented in Port differences, as each language indexes its
      strings; the Java and C# comments say UTF-16 code units.

### Names: one concept, several names

- ✅ **B** Strokes. `Line`, `Path`, `Arc`, `Point` and `Rect` use
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
      Fixed, as decided on Sep 13: the outline of a box is a border and a
      line has a stroke. `Line`/`Path.setPattern` is `setStrokeDashPattern`
      and the `Chart` grid line patterns are `setHGridLineDashPattern` and
      `setVGridLineDashPattern`; `Line.setCapStyle` is `setLineCapStyle`;
      `Page` keeps pen and brush, and `setDefaultLineWidth` is
      `setDefaultPenWidth`; `Form.setLineWidth` is `setStrokeWidth`;
      `TextBox` and `Cell` have `setBorderColor`, `setBorderWidth` and
      `getBorderWidth`, and `TextBox.setLineWidth` is removed;
      `CheckBox.setBoxColor` and `setCheckmark` are `setBorderColor` and
      `setCheckmarkColor`; `Paragraph.setColor` is `setTextColor`,
      `QRCode`/`DataMatrix.setColor` `setModuleColor` and
      `TextLine.setLineColor` `setDecorationColor`;
      `Table.setCellBordersColor`/`Width` are `setCellBorderColor`/`Width`,
      and `TextBlock.setBorderCornerRadius` is `setCornerRadius`.
- ✅ **B** Rotation: `Arc.setRotateDegreesCW`/`CCW`; `Container` and `Stamp` have
      `rotate`, `setRotation` and `setRotationCounterClockwise` for one angle,
      plus `setRotationClockwise`; `Image.rotateClockwise` sets the angle;
      `Page.rotateBy` sets an absolute `/Rotate`; `BaseAnnotation.rotate` is a
      public `Container` helper.
      Fixed, as decided on Sep 13: an angle is set with `setRotation`,
      counter-clockwise, or `setRotationClockwise`.
      `Arc.setRotateDegreesCCW` and `CW` are `setRotation` and
      `setRotationClockwise`; `Container` and `Stamp` lose `rotate` and
      `setRotationCounterClockwise`; `Image.rotateClockwise` and
      `Page.rotateBy` are `setRotationClockwise`; `BaseAnnotation.rotate` is
      internal; and the text rotation in degrees of `Page`, `TextLine` and
      `Point`, `setTextDirection(int)`, is `setTextRotation`.
- ✅ **B** Scaling and moving: `Arc.setScaleFactor` multiplies the radii, so it
      is a `scaleBy`; `Container.setScaleFactor`/`setScaleFactorXY` set an
      absolute scale; `Rect.scaleBy` scales x and y only. `Line.setLocation`
      moves only the start point (`setStartPoint` does that too);
      `Arc.setCenterXY` duplicates `setLocation`; `Title.setOffset` adds to x
      on every call; `TextParameters.setTextLocation`;
      `CompositeTextLine.getPosition`, and `getMinMax` returns y values only.
      Fixed: `Arc.setScaleFactor` is `scaleBy`; `Rect.scaleBy` scales the
      width, the height and the corner radius, not the location;
      `Line.setLocation` moves the whole line; `Arc.setCenterXY` is removed
      and `setLocation` sets the center; `Title.setOffset` sets the offset of
      the text line from the prefix; `TextParameters.setTextLocation` is
      `setLocation`; `CompositeTextLine.getPosition` is `getLocation` and
      `getMinMax` `getMinMaxY`. `Container.setScaleFactor` stays, as it sets
      the scale.
- ✅ **B** Alignment: `Align` and `Alignment` are one `Alignment` enum since the
      types and signatures fixes; what is left:
      `Cell.setVerTextAlignment` against `TextBox.setVerticalAlignment`;
      `Table.setTextAlignInColumn` against `setTextAlignment`;
      `Point.setAlignment`.
      Fixed: `Cell.setVerTextAlignment` is `setVerticalAlignment` and
      `Table.setTextAlignInColumn` `setTextAlignmentInColumn`.
      `Point.setAlignment` stays, as a point has one alignment.
- ✅ **B** Text boxes: `TextBox` and `TextBlock` name one setting differently:
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
      Fixed, as decided on Sep 13, with a gap in points and a spacing as a
      multiplier: `TextBox.setMargin` and `TextBlock.setTextPadding` are
      `setPadding`; `TextBox.setSpacing` is `setLineGap` and
      `Text`/`TextFrame.setParagraphLeading` `setParagraphGap`;
      `setTextColors`, `setKeywordHighlightColors` and `setColorMap` are
      `setHighlightColors`; the `setFillColor` duplicates are removed;
      `TextBox.setBorder(int, boolean)` removes a border, as `Cell`'s does;
      `TextFrame.setBorder(boolean)` is `setBorders`, black by default; and
      `TextBlock.getHeight` returns the drawn height when the text is taller
      than the set height. `TextBox.setTextDirection` stays: it turns the
      text, where `TextBlock.setRightToLeft` sets the reading order. The
      Java example PDFs are the same as before.
- ✅ **B** Barcodes: `PDF417.setModuleWidth`, `setModuleLength` in `Barcode`,
      `QRCode` and `DataMatrix`; `QRCode.getData` returns the modules;
      `ErrorCorrectLevel` for error correction level;
      `Barcode.LEFT_TO_RIGHT`, `TOP_TO_BOTTOM` and `BOTTOM_TO_TOP` duplicate
      `Direction`, and EAN-13 and UPC-A ignore the direction.
      Fixed: `PDF417.setModuleWidth` is `setModuleLength`;
      `QRCode`/`DataMatrix.getData` are `getModules`; `ErrorCorrectLevel` is
      an `ErrorCorrectionLevel` enum (Go `qrcode.ErrorCorrectionLevel`, with
      `ErrorCorrectionLevelL` and so on); the `Barcode` direction constants
      are removed and `setDirection` takes a `Direction`; EAN-13 and UPC-A
      are drawn top to bottom a quarter turn clockwise and bottom to top a
      quarter turn counter-clockwise, as Code 39 is, so Example_11 draws its
      UPC-A and EAN-13 bottom to top, as it asks.
- ✅ S Code 128 draws nothing bottom to top in the four ports, and top to
      bottom its text reads up on the right, where Code 39's reads down on
      the left.
      Fixed: the four barcodes are drawn left to right and turned to their
      direction, so Code 128 is drawn bottom to top, and top to bottom its
      text reads down on the left, as Code 39's does. Code 39 no longer
      returns (0, 0) as its corner without a font, and its text is centered
      on the bars, not on the bars and the gap after the last character.
      `drawOn` returns the bottom right corner of the bars and the text of
      every barcode in every direction, with or without a font.
- ✅ **B** Table data: `Table.setData` against `BigTable.setTableData`.
      Fixed: `Table.setData` is `setTableData` in the four ports, and the
      six `Table` examples (09, 13, 15, 29, 34, 38), the unit tests and
      the README Go note use it. The rest of one table API is left for
      10.0 in api-suggestions.html.

### Names: misleading, redundant or dead

- ✅ **B** Duplicates: `Table.getCellAt`/`getCellAtRowColumn`,
      `getRow`/`getRowAtIndex`, `getColumn`/`getColumnAtIndex`;
      `Font.getHeight`/`getBodyHeight`; Java and Swift
      `Permissions.getAccess`/`getRawValue`;
      `TextColumn.addChineseParagraph`/`addJapaneseParagraph`;
      `Container.addBorder()` is `setBorderColor(Color.black)`, and
      `setBorderColor` adds another `Rect` on each call; `Cell.getBorder(int)`
      next to `getTopBorder` and the other sides.
      Fixed: `Table` keeps `getCellAt`, `getRow` and `getColumn`; `Font`
      keeps `getBodyHeight`; Java and Swift `Permissions` keep `getAccess`,
      and C# and Go keep `GetRawValue`, which returns the flags as an
      unsigned number where `GetAccess` returns `UserAccess`; `addCJKParagraph`
      replaces the Chinese and Japanese methods; `Container.addBorder` is
      removed and `setBorderColor` colors one border; `Cell` keeps `setBorder`
      and `getBorder` with the `Border` bits, as `TextBox` does.
- ✅ **B** Misleading: `BaseAnnotation.setTransparency` writes `/CA`, an opacity;
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
      Fixed: `BaseAnnotation.setOpacity`, `FileAttachment.setContents`,
      `setIconPushpin`, `DonutChart.setRadii`, `Bookmark.getDestinationName`,
      `Table.autoAdjustColumnWidths`, `Image.setFlipUpsideDown`, which
      returns the image and flips it in place instead of one image height
      lower, `Point.setDrawPath(boolean)`, `Cell.setMarker`, `Page.addBDC`,
      `Page.addArcToPath` and `addCircularArcToPath`,
      `Chart.setDrawHGridLines` and `setDrawVGridLines`, with a grid line
      width of 0 documented as the thinnest line, the `ScriptPosition` enum
      with `TextLine.setScriptPosition`, and `Permissions.grant` and `revoke`.
      `B5` is the ISO 216 B5 (499 by 709) and `JISB5` the Japanese B5 it was.
      The `WITH_n_HEADER_ROWS` constants are removed, and `Page.transform`
      documents the six values it reads. `TextLine.setColorMap` was already
      `setHighlightColors`, and Go `DrawStringUsingColorMap` is
      `DrawStringUsingHighlightColors`. The `Color` constants stay in lower
      case, as the CSS color keywords are and as C# SVG parsing looks them up;
      `Color` documents that, and the two Old Glory colors.
- ✅ **B** Ignored or dead: the `Font(pdf, stream, Font.STREAM)` flag
      (`Font.java:303`); the `pdf` the `FileAttachment` constructor stores;
      `Slice.tooltip`; `Image` has no `setLanguage`
      though `drawOn` reads the field; the `SVGImage.drawOn` link branch that
      no setter reaches; `TextLine.setURILanguage`, `setURIAltDescription` and
      `setURIActualText` have no getters; the public `BaseAnnotation()` makes
      an annotation with no subtype, which crashes Java and Swift `PDF` and
      writes `/Subtype /` in Go; the `Destination` constructors are public
      and nothing public takes a `Destination`.
      Fixed: `Font(pdf, stream)` tells a stream font from an OpenType or
      TrueType font by its first four bytes, as Go `NewFont` now does, so the
      flag constructors and `Font.STREAM` are removed and
      `Font(objects, stream)` takes no flag; `FileAttachment` no longer takes
      the `PDF`; `Slice.tooltip` is removed; `Image.setLanguage`;
      `SVGImage.setURIAction`, `setGoToAction`, `setAltDescription`,
      `setActualText` and `setLanguage`; `TextLine.getURILanguage`,
      `getURIAltDescription` and `getURIActualText`; `BaseAnnotation` is
      abstract with a protected constructor, Go `newBaseAnnotation`; the
      `Destination` constructors are internal, Go `newDestination`, and the
      Java and C# `double` ones are removed.
- ✅ **B** Public by accident: `Token` (mutable byte arrays), `Single` (one
      `space` constant; in C# it hides `System.Single`),
      `TextUtils.printDuration` (an examples helper; C# formats it in the
      current culture), the core font metrics classes (`Courier_Bold`, ...),
      the Java `PDFobj` protected fields and `setStream`, `setNumber` and
      `getLength`, the public no-argument constructors of the Java constant
      classes (`new A4()`, `new Color()`), and `Encryption.getKey`, which only
      Java needs public.
      Fixed: `Token` already was. `Single` is internal, in Go under
      `src/internal/single`. `TextUtils.printDuration` stays public, as the
      examples of every port use it, and C# formats it in the invariant
      culture. The core font metrics classes are package-private in
      `com.pdfjet` in Java and internal in C# and Swift, and Go's unused
      `Courier` to `ZapfDingbats` constants are removed; `corefont.Courier()`
      stays Go's way to choose a core font. The Java `PDFobj` members are
      package-private, the Java constant classes have private constructors,
      and `Encryption.getKey` is internal in C# and Swift and removed from Go,
      which never called it; Java keeps it public for the `com.pdfjet`
      package.
- ✅ **B** `Table.getRowsRendered` says "rendered" where the library says
      "drawn".
      Fixed: it is `getRowsDrawn` in the four ports, and the field behind
      it is `drawn`; the unit tests use it, and no example calls it.

### Names in one port

- ✅ **B** C#: `SVGImage.getWidth` and `getHeight` are lower case
      (`SVGImage.cs:213`); `Compliance.PDF_1_7` is `PDF_17` in the other
      ports (`PDF_1_7` reads better next to `PDF_UA_1`); `PathOperator`,
      `Token`, `UserAccess` and `Point.ControlPointC`/`V`/`Y` are PascalCase
      where the other C# constant classes copy Java's `UPPER_SNAKE`.
      Fixed: `SVGImage.GetWidth` and `GetHeight`; `Compliance.PDF_1_7` in the
      four ports; the C# `PathOperator` and `UserAccess` members and
      `Point.CONTROL_POINT_C`, `V` and `Y` are `UPPER_SNAKE`. `Token` is
      internal in every port since the public by accident fixes.
- ✅ **B** Swift: `PathOperator`, `Token` and `Point.controlPointC`/`V`/`Y` are
      camelCase and `StructElem` is PascalCase (`Document`, `THead`) where the
      other Swift constant classes copy Java's `UPPER_SNAKE`.
      Fixed: the `PathOperator` cases, `Point.CONTROL_POINT_C`, `V` and `Y`
      and the `StructElem` constants are `UPPER_SNAKE`; `Token` is internal.
- ✅ **B** Go: `RadioButton.SelectButton` and `CheckBox.XMarkCheckBox`, whose
      suffixes no overload explains; `NewImage2`; `Page.GetPenColorRGB` and
      `GetBrushColorRGB` with no plain `GetPenColor` and `GetBrushColor`;
      `mark.UnCheck`; `tabloid.PORTRAIT` and `LANDSCAPE` where the other page
      size packages have `Portrait` and `Landscape`; the `djb` package, whose
      `Salsa20()` returns a document ID; the `Courier` … `ZapfDingbats`
      constants in `src/corefont.go`, numbered from 0 and named like the
      `corefont.Courier()` functions; `ValidBitsMask`.
      Fixed: `RadioButton.Select`, as in Java; `NewImageForObjects`;
      `Page.GetPenColor` and `GetBrushColor`, which return the `[3]float32`
      color as `Point.GetStrokeColor` does; `mark.Uncheck`; the document ID
      comes from `DocumentID` in the internal `salsa20` package; and
      `validBitsMask` is unexported. `CheckBox.XMarkCheckBox` was already
      `Check(mark.X)`, `tabloid` already had `Portrait()` and `Landscape()`,
      and the core font constants went with the public by accident fixes.
- ✅ **B** Go exports helpers that Week 4 meant to hide and the README does not
      list: `BitBuffer`, `RSBlock`, `Polynomial`, `TextCompact`, `L5ECC`,
      `Round` and the `src/round` package, `JPGImage`, `BMPImage`,
      `FontStream1`, `FontStream2`, `NewCoreFontForPDFobj`.
      Fixed: they are unexported (`bitBuffer`, `qrRSBlock`, `qrPolynomial`,
      `textCompactWrapper`, `l5ECCWrapper`, `roundedRange`, `jpgImage`,
      `bmpImage`, `fontStream1`, `fontStream2`, `newCoreFontForPDFobj`), the
      unused `src/round` package is removed, and the README lists the helpers
      that stay exported.
- ✅ **B** Java: the sources in `com/pdfjet/fonts`, `qrcode`, `pdf417` and
      `datamatrix` declare `package com.pdfjet`, while `barcodes`,
      `corefonts` and `encryption` have their own packages.
      Fixed: they declare `com.pdfjet.fonts`, `com.pdfjet.qrcode`,
      `com.pdfjet.pdf417` and `com.pdfjet.datamatrix`, as the Go packages are
      named, and the examples import them. The core font metrics went into
      `com.pdfjet` with the public by accident fixes.

### Merge (Sep 14)

- ✅ **B** `PDF.merge(objects)` adds all the pages of a document that was read,
      with drawn pages before, between and after them, in the four ports:
      the objects the pages use are renumbered and written at once, the
      entries a page inherits are written on it, references to the page tree,
      the catalog and pages that are not merged become null, and the merged
      strings and streams are encrypted when the PDF is. Example_41; the
      examples go to 51. Split is next.
- ✅ **B** Split (Sep 14): `PDF.merge(objects, pageNumbers)` (Go `MergePages`,
      Swift `merge(objects, [pageNumbers])`) merges the listed pages of a
      document that was read, in the order listed, in the four ports; merging
      each part of a document into a PDF of its own splits it, and the objects
      that were read can be merged into any number of PDFs. A link to a page
      left out becomes null; a page the document does not have or a page
      listed twice is refused. Example_51 writes each page of `wirth.pdf` to
      a PDF of its own and all of its pages in reverse order to
      Example_51.pdf; the scripts, workflows and example pages count 51
      examples. Tests in each port.

### Broken PDFs from misuse (Sep 14)

- ✅ **B** A probe of 32 wrong Java programs and 6 Go programs, checked with
      poppler, mutool, veraPDF and a strict content-stream check, found 16
      that wrote a broken or invalid PDF without an error: NaN and infinite
      numbers, raw dash patterns, unpaired q/Q and BDC/EMC, fonts, images and
      pages of another PDF, uncompleted stamps, late encryption and
      compliance, no pages, control characters in the XMP metadata, page
      sizes outside 3 to 14,400, negative pen widths and numbers too large for
      a PDF. Go lost drawing on a written page silently and wrote a page
      added twice twice. Fixed in the four ports: the call fails (Java, C#) or
      records the mistake (Go, Swift), and `complete()` refuses to finish the
      document; MisuseTest in each port. The pie chart NaN and stamp text
      without `addFont` were fixed on the way.

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
- ✅ **B** Release blockers found by the code review of Sep 13:
      - `go get` could not fetch the module: the repository is 637 MiB, 596
        MiB of it fonts, and Go rejects a module over 500 MiB ("module source
        tree too large", `golang.org/x/mod/zip.CreateFromVCS` on HEAD).
        Fixed: `fonts/`, `data/` and `images/` have a `go.mod` of their own,
        which Go leaves out of the module; the module zip is the library and
        example source, and a scratch module fetches it from a local proxy,
        builds and writes a PDF. The README says the fonts are not in the
        module. Still to do after tagging: the `go list -m` check above.
      - Swift packages could not depend on PDFjet: `Package.swift` declared no
        products. Fixed: it declares the `PDFjet` library product, and a
        scratch package that depends on it by path builds and writes PDFs.
      - Documents made in the same millisecond got the same `/ID` and XMP
        DocumentID: Java, Go and Swift hashed the time in milliseconds with
        Salsa20, C# the ticks. 200 PDFs made in a loop in Java had 59 IDs.
        Fixed: the ID is 16 bytes from `SecureRandom`,
        `RandomNumberGenerator`, `crypto/rand` and
        `SystemRandomNumberGenerator`, and the Salsa20 classes are gone.
- ✅ Unit tests (Sep 13): the four ports had none. Each has a suite with the
      same cases and expected values: JUnit 5 in `tests/java`, xUnit in
      `tests/dotnet`, the `testing` package next to the Go sources and Swift
      Testing in `tests/swift`, run by `test-java.sh`, `test-dotnet.sh`,
      `test-go.sh` and `test-swift.sh`, by the Build workflow (the Java tests
      on JDK 21 and 8) and by `check-examples.sh`. PNG samples are checked
      against MuPDF and Pillow. `generate-documentation.sh` gives DocC the
      PDFjet symbol graph only, as the test target adds two test modules.
- ✅ Bugs the unit tests found, fixed in every port that had them:
      - Java `Decompressor.inflate` threw on an empty zlib stream, so
        `PDF.read` failed on any PDF with a blank page; the other ports read it.
      - SVG `fill="none"` without a stroke was filled black, and `none` on a
        path did not override the color of the svg element; four ports.
      - An open SVG path with a stroke was never stroked, which left an
        unpainted path in the content stream; four ports.
      - A grayscale PNG with alpha (color type 4) crashed with an index out of
        range in the true color decoder; four ports. 8-bit images are a
        DeviceGray image with a soft mask, and 16-bit ones fail with the
        message of 16-bit RGBA images.
      - An interlaced PNG logged a warning and then crashed; four ports. It
        fails with "Interlaced PNG images are not supported." and the OptiPNG
        command.
      - Swift `Puff` read past the end of a truncated zlib stream and
        trapped; it throws.
      - C# `BMPImage` looped forever, at full CPU, on a truncated BMP file:
        the read loop tested `read < 0`, copied from Java, where `Stream.Read`
        returns 0 at the end of a stream. It throws Java's message.
      - C# `Decompressor.Inflate` returned the bytes of a truncated Flate
        stream without an error, as `ZLibStream` does. It reads the
        `ZLibStream` with `Read`, which reads the input only until the stream
        ends (`CopyTo` reads all of it), from an input stream that remembers a
        read at its end, and throws when there was one; bytes after the end of
        a stream are still ignored.
      In the four ports every truncation of a stream fails with an error and a
      stream followed by other bytes decodes. Java example PDFs render the
      same before and after the fixes.
- ✅ **B** Untrusted input limits (Sep 13), in the four ports:
      - A stream, a font stream or the samples of an image may decode to at
        most 256 MiB; the Flate, LZW and RunLength decoders fail past it with
        "... data decodes to more than N bytes", so a few kilobytes of a
        decompression bomb no longer take gigabytes. ASCII85 is not capped,
        as it decodes to at most 4 times its input.
      - PNG: the IHDR chunk, the size, the bit depth for the color type and
        the palette are checked, and the decoded size (with the RGB and alpha
        of a palette image) against the limit, before any buffer is allocated;
        chunks are read in pieces, so a length that the file does not have
        fails at its end, and a stream that returns few bytes at a time is
        read correctly (Java read a chunk with one `read`); the rows are
        decoded up to the size of the image, data after them is ignored and
        missing data fails. A truecolor image with a suggested palette was
        decoded as a palette image; it is decoded as truecolor.
      - BMP: the size, the bit depth, the palette size and the decoded size
        are checked before any buffer is allocated.
      - The size checks reject a height above the limit before multiplying
        the width and the height, as a PNG row has a filter byte and a BMP
        pixel 3 bytes: with a width and a height near 2^31 the products
        overflowed a 64-bit integer in Java, C# and Go and could pass the
        check. Swift multiplies with overflow checks. Found by the Swift port.
      - Java and C# counted the bytes written in an `int`, so the offsets of
        a PDF past 2 GiB were wrong; they are 64-bit, and an offset past the
        10 digits of a cross-reference entry fails with an error.
      Java example PDFs render the same before and after.
- ✅ S Port differences the tests found and left as they are: Swift
      `BMPImage` needs an opened stream where `PNGImage` opens its own; Swift
      stops with `fatalError` on invalid Code 39, UPC-A and EAN-13 input and
      on QR data that does not fit, which a test cannot catch; Swift
      `PNGImage.getAlpha` returns no bytes where Java returns null; Go and
      Swift have no public hex helper like Java `Util.toHexString`. Done:
      Swift `BMPImage` opens its stream, `Barcode` and `QRCode` throw
      `PDFjetError`, `getAlpha` returns `nil`, with Swift tests mirroring the
      Java ones; the hex helper is internal in all four ports, so nothing to
      expose.
- ✅ **B** Intermittent Java test failure: `EncryptionTest.passwordsAreCutAt127Bytes`
      failed once in about fifteen runs with `incorrect header check` while
      reading back the encrypted PDF. Cause: after the `stream` keyword the
      reader skipped one line feed, meant for the LF of a CRLF, but the
      tokenizer had already consumed the LF that PDFjet writes, so a stream
      whose first data byte is 0x0A lost it. Plain streams start with the zlib
      byte 0x78; an encrypted stream starts with a random IV, so one in 256
      failed (16 of 4000 in a loop). Fixed (Sep 14) in the four readers: only
      the LF of a CRLF is skipped, with a test that reads a stream starting
      with a line feed; 0 failures in 4000 runs after the fix.
- ⬜ **B** `check-examples.sh` clean, `go vet` clean, Swift builds with
      warnings as errors, Windows workflow run from the Actions tab and green.
      Done on Sep 13: `check-examples.sh` is clean with the version bump,
      `go vet ./src/...` is clean, `build-swift.sh` builds with
      `-warnings-as-errors`, and the Build and Documentation workflows passed
      on 017d197b. Left: the Windows workflow last ran on Sep 11 (4c626820),
      before the API audit fixes; run it again from the Actions tab.
- ⬜ **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, and opens the
      PDF/UA and PDF/A examples without warnings. Same files in Preview,
      Chrome and Firefox.
- ✅ **B** CHANGELOG `## v9.0.0` entry. Breaking changes first: Go `Drawable`
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
      API audit fixes: Java `RadioButton.setLocation(double, double)` no
      longer overflows the stack; Go circle, square, polygon and text
      annotations have a gray fill and full opacity by default instead of
      being invisible, and a Go `Container` offsets any annotation; Swift
      `OptionalContentGroup.setVisible(false)`, `setPrintable(false)` and
      `setExportable(false)` write `/OFF`; Go `Form` defaults to 9 point
      label and value fonts and a width of 500, Swift to 9 point fonts; the
      label of a `CheckBox` or `RadioButton` with a URI is blue in the four
      ports; annotations in a nested `Container` get its offset in Swift, and
      in C# for a subclass of `Container`; Swift `Bookmark` collapses tabs
      and newlines in a title, and `getDestKey` and `getTitle` return nil for
      the root. Go `QRCode` and `PDF417` implement `Drawable`: `SetLocation`
      returns `Drawable` and `DrawOn` returns `[2]float32`; Go draws the text
      of a UPC-A, EAN-13 or Code 39 barcode in a table cell under the barcode;
      a `PDF417` drawn twice no longer moves to the right.
      `Chart`: all-negative data no longer gets an automatic maximum of 0
      in Java and Swift; C# no longer rounds the axis ranges set with
      `setXAxisMinMax` and `setYAxisMinMax`; Go gives a series with no
      stroke colour the automatic colours and keeps an explicit black; a
      chart with no points draws nothing instead of crashing, an empty
      series is skipped, and flat data is drawn on a range of the value to
      the value plus 1 instead of NaN labels or a crash. `CalendarMonth`
      defaults to the location (0, 0), a cell size from the day name font
      and a 1.25 point circle in the four ports, where C#, Go and Swift had
      (75, 75) and fixed sizes; Go lays it out as Java does; Swift draws a
      month that starts on a Sunday in the right row; Java uses the
      Gregorian calendar whatever the default locale.
      Every PDF gets an `/Info` dictionary with `/Producer`, `/CreationDate`
      and the properties set with `setTitle`, `setAuthor`, `setSubject`,
      `setKeywords` and `setCreator`, which did nothing in a PDF 1.7 file;
      Go PDFs get their own `/ID` and XMP DocumentID, where all had the
      same; Swift `Executive.PORTRAIT` and `LANDSCAPE` are `[Float]`, so
      `Page` takes them; `Page.getPenWidth` returns 1 on a new page, what
      readers draw with, `getPenColor` and `getBrushColor` return the RGB
      of a CMYK colour, and `restoreGraphicsState` restores what they
      return.
      `Table.drawOn(null)` measures a table without changing what a later
      `drawOn` draws, where that draw crashed; `Cell.setTextBlock` and
      `setTextColumn` clear the cell text, as `setTextBox` does, so the block
      or column is drawn; `Cell.getHeight`, `Table.setColumnWidths()` and the
      wrapping of cell text measure at the size set with `Cell.setFontSize`;
      Java `BigTable` splits lines at the delimiter literally and keeps the
      empty fields at the end of a line, as the other ports do;
      `BigTable.setLocation` sets the location instead of adding to x, and
      can be called before `setTableData`; `BigTable.setLanguage`, which did
      nothing, is removed.
      PNG images with row filters draw correctly: palette images at every
      bit depth and 1, 2 and 4 bit grayscale images, and 1, 2 and 4 bit
      palette images get the transparency of their tRNS chunk; C# reads
      SVG files in a culture with a decimal comma; `SVGImage(path)` closes
      the file in Java and C#; Go `SVGImage` writes a `/P` tag and space alt
      and actual text in PDF/UA files; Swift `SVGImage` reads attributes in
      single quotes, over several lines or in any order; C# empty pages get
      a valid compressed content stream; Java and Swift read `.ttf.stream`
      and `.otf.stream` fonts from streams that return fewer bytes than
      asked for, and fail on a truncated one.
      C# `CompositeTextLine` lays its lines out as Java does, so a composite
      line with no font size keeps the sizes of its lines instead of drawing
      them at size 0; `TextColumn.setTextAlignment` applies to the paragraphs
      that do not set their own alignment, and `TextColumn.drawOn` returns a
      corner at least as far as the location plus the height; `Text`,
      `TextFrame` and `TextColumn` keep every setting of a line they wrap:
      line colour, colour map, language, structure type, text direction, alt
      description and the language, alt and actual text of its link;
      `TextBox` draws underline and strikeout in the text colour, not the
      border colour; `TextBlock` draws the characters its font lacks in the
      fallback font, and `setFallbackFontSize` sets their size without
      resizing the shared `Font`.
      Drift fixes: the C# `CheckBox` check mark is black by default, as in
      the other ports; a filled `Arc` or `Ellipse` with no stroke colour is
      only filled, without a black hairline, in Java, C# and Swift; Java
      `new PDF()` reports `PDF_17` instead of null; Java and C# no longer
      write an empty title or description for a `FileAttachment`; the default
      description of a Go `FileAttachment` ends "the attached file."; Go
      `NewImage2` gives an image a space as alt and actual text, as
      `NewImage` does; `Token` is no longer public in any port, and Go's
      `token` package is internal; the C# page sizes are static properties
      that return a new array (source compatible, not binary compatible), so
      changing `A4.PORTRAIT[0]` no longer changes later pages.
      Java `TextBlock` draws a text of only line breaks as one empty line;
      Java and Go `Table` from a file drop a UTF-8 byte order mark from the
      first header cell; C# `Content.ofTextFile` and the `Table` file
      constructor read UTF-8 only, and C# has `Content.getFromStream(stream,
      bufferSize)`; Go tables no longer make the rows of wrapped cell text
      too tall when a cell without text has a larger font; Swift
      `PDFobj.dict` is public, so a read object's dictionary can be edited,
      as in the other ports; Java and C# `Page` and `TextLine` colour getters
      return copies, and their array setters copy the caller's array; Go
      `Page.GetContent` and C# `Encryption.GetKey` return copies.
      Errors: Swift `PNGImage`, `BMPImage` and `OTF` throw on invalid data
      instead of printing a message or crashing, and a top-down BMP reads;
      Swift stops with an error on QR data that does not fit, an invalid
      Code 39 character, an unknown barcode type, `Stamp.drawText` without
      a font or text and `Form.drawOn(nil)`; Go `Barcode.DrawOn` panics on an
      unknown barcode type instead of returning (0, 0); C# throws its own
      message for an invalid Code 39 character and throws on a truncated
      font stream; Swift `SVGImage` throws on an invalid colour instead of
      drawing it transparent, and its `init(stream:)` and
      `init?(fileAtPath:)` are `throws`; `Content.ofBinaryFile` reports a
      missing file in Java, C# and Swift, and so does `Content.ofTextFile`
      in Java and C#; Swift `BigTable.complete()`
      reports drawing errors and keeps lines that are not UTF-8; Swift
      `Table.drawOn(pdf, &pages, pageSize)` returns nil, not a crash, when
      the table is already drawn, and returns `[Float]?`; Swift
      `Content.ofTextFile` reads invalid UTF-8 as U+FFFD instead of
      throwing.
      The Java and C# colour setters of `Cell`, `TextBox`, `TextBlock`,
      `Point`, `Arc`, `Rect`, `Stamp`, `Text`, `TextFrame`, `Form` and
      `BaseAnnotation`, and C# `Line`, copy the caller's array, and their
      colour getters return copies; `BigTable` reads its data file as UTF-8
      and drops a byte order mark at its start in the four ports, and Swift
      `BigTable` draws a line that is not UTF-8 with U+FFFD instead of
      skipping it.
      Breaking: a page size is an immutable `PageSize` in the four ports
      (Java `final class`, C# `sealed class`, Swift `struct`, Go
      `pagesize.PageSize`) with `PageSize(width, height)`, `getWidth()` and
      `getHeight()`; `A3`, `A4`, `A5`, `B5`, `Executive`, `Legal`, `Letter` and
      `Tabloid` `PORTRAIT` and `LANDSCAPE` are `PageSize` values, and in Go
      the functions `Portrait()` and `Landscape()` (Tabloid's `PORTRAIT` and
      `LANDSCAPE` renamed to match); the `Page` constructors, the `BigTable`
      constructor and `Table.drawOn(pdf, pages, pageSize)` take a `PageSize`
      instead of a `float[]`, `[Float]` or `[2]float32`, and
      `PDFobj.getPageSize()` returns one; Go `NewPage`, `NewPageDetached`,
      `NewBigTable` and `Table.DrawOnPages` take one. `Token` is no longer
      public in Java, C# and Swift.
      Breaking: Swift `PDF.complete()` throws when the PDF cannot be
      written, as Java's does, where a failed write printed a message and
      `complete()` returned as if it had succeeded.
      Numbers in content streams are rounded to hundredths from the exact
      value of the float, halves away from zero, so negative halves and
      values above 21.5 million are written correctly and the same in every
      port; font descriptor bounding box, ascent, descent and cap height are
      in 1/1000 em, as the PDF specification requires; Swift writes the CID
      font widths as integers, and C# rounds half widths like the other
      ports; `Chart` axis labels do not depend on the locale (`.` as the
      decimal separator, no grouping, no `-0`), Go `Chart` honours
      `SetMinimumFractionDigits`, and C# places the Y axis labels like the
      other ports; Go `DonutChart` percentages match the other ports.
      Breaking: Go `PDF.Read` and `ReadWithPassword` return `([]*PDFobj,
      error)`, with an error for a wrong or missing password, a malformed PDF
      or a Flate stream that cannot be inflated; Swift `PDF.addObjects`
      throws. The Go port panics, or returns an error where the function
      returns one, where it exited the program with `log.Fatal`, and no
      longer ignores errors writing, compressing or encrypting a PDF or
      reading a JPEG, PNG, font stream or embedded file; Go `NewSVGImage`
      returns an error for a bad number in a path; `PDF.addObjects` raises a
      clear error in the four ports when the objects have no root `/Pages`;
      the four ports read top-down BMP images, and Go reports a truncated BMP
      or a bad palette index instead of crashing.
      Breaking, types and signatures: the Go constants of `compliance`,
      `alignment`, `capstyle`, `joinstyle`, `effect`, `mark`, `imagetype`,
      `pathoperator`, `pagelayout` and `pagemode` are typed, Go
      `direction.BottomToTop` is 1 and `TopToBottom` 2, and the
      `compress.Compress` type replaces a `bool`; `PathOperator`, `ImageType`,
      `PageLayout` and `PageMode` are enums in Java, C# and Swift; `Align` is
      removed and every alignment is an `Alignment`, which has `JUSTIFY`,
      `TOP` and `BOTTOM`; `Point.getTextColor` returns a color array in Java
      and C#. Swift: the `Dimension` and `PNGImage` sizes, `Title.prefix` and
      `textLine` are not optional, `Cell` and `TextParameters` take a
      non-optional font and text, `DonutChart` takes optional fonts,
      `Cell.setColSpan` and `getColSpan` use `Int`, `PDF.addObjects`,
      `addResourceObjects`, `PDFobj.getContentObject`, `getResourcesObject`
      and the image and font `addResource` take the objects without `&`,
      `PDFjetError` is public and `PDF417` throws it, and `addWatermark`,
      `addHeader` and `addFooter` do not throw. Go: `BaseAnnotation`
      `SetFillColor` takes an `int32` and `SetFillColorRGB` the array, the
      `Arc` RGB setters take an array and the `Float64` variants are gone,
      `Stamp` colors are `int32` and `[3]float32`, the `Cell`, `TextBlock` and
      `TextBox` color getters return nil when the color is not set,
      `DrawString` takes a font size and `SetTableData` returns the table.
      Java: `drawString` takes an `int` color and `SVGImage()` is removed.
      `Rect.scaleBy`, `SVGImage.scaleBy`, `Image.rotateClockwise`,
      `Container.rotate` and `addBorder`, `Table.removeLineBetweenRows` and
      `rightAlignNumbers`, the `TextColumn` paragraph methods,
      `OptionalContentGroup.clear` and `BaseAnnotation.rotate` return the
      object, and `OptionalContentGroup.drawOn` returns a corner.
      New: `Color.transparent` removes a cell background; Java, C# and Swift
      `Form` take `int` colors; Go `NewTextBoxWithSize`, `Title.GetTextLine`,
      `Page.DrawStringUsingColor` and `Point.SetTextColorRGB`; Swift `Rect` and
      `TextBox` `r, g, b` setters and `drawString` with an `Int32` color.
      Breaking, names: box outlines are borders (`TextBox`, `Cell` and
      `CheckBox.setBorderColor`, `setBorderWidth`,
      `Table.setCellBorderColor` and `setCellBorderWidth`,
      `TextBlock.setCornerRadius`) and lines have strokes
      (`Line`/`Path.setStrokeDashPattern`, `Line.setLineCapStyle`,
      `Form.setStrokeWidth`, the `Chart` grid line dash patterns);
      `Page.setDefaultPenWidth`; `CheckBox.setCheckmarkColor`,
      `Paragraph.setTextColor`, `QRCode`/`DataMatrix.setModuleColor` and
      `TextLine.setDecorationColor`. `setRotation` and `setRotationClockwise`
      replace `Arc.setRotateDegreesCW`/`CCW`, `Container` and `Stamp`
      `rotate` and `setRotationCounterClockwise`, `Image.rotateClockwise` and
      `Page.rotateBy`; `BaseAnnotation.rotate` is internal; `setTextRotation`
      replaces the `setTextDirection(int)` of `Page`, `TextLine` and `Point`.
      `Arc.scaleBy`, no `Arc.setCenterXY`, `TextParameters.setLocation`,
      `CompositeTextLine.getLocation` and `getMinMaxY`; `Rect.scaleBy` keeps
      the location, `Line.setLocation` moves the whole line and
      `Title.setOffset` sets the offset. `Cell.setVerticalAlignment` and
      `Table.setTextAlignmentInColumn`. `setPadding`, `setLineGap`,
      `setParagraphGap` and `setHighlightColors` in the text classes, no
      `setFillColor` in `TextBox` and `TextBlock`,
      `TextBox.setBorder(int, boolean)`, `TextFrame.setBorders` with a black
      border, and `TextBlock.getHeight` returns the drawn height.
      `PDF417.setModuleLength`, `QRCode`/`DataMatrix.getModules`, the
      `ErrorCorrectionLevel` enum, `Barcode.setDirection(Direction)`, and
      EAN-13 and UPC-A drawn in that direction; Code 128 is drawn bottom to
      top, and top to bottom its text is on the left, as Code 39's is.
      Breaking, misleading, redundant or dead names: `Table.getCellAtRowColumn`,
      `getRowAtIndex` and `getColumnAtIndex`, `Font.getHeight`, Java and Swift
      `Permissions.getRawValue`, `Container.addBorder` and the `Cell` side
      border methods are removed, and `TextColumn.addCJKParagraph` replaces
      the Chinese and Japanese methods. `setOpacity`,
      `FileAttachment.setContents`, `setIconPushpin`, `DonutChart.setRadii`,
      `Bookmark.getDestinationName`, `Table.autoAdjustColumnWidths`,
      `Image.setFlipUpsideDown`, `Point.setDrawPath(boolean)`,
      `Cell.setMarker`, `Page.addBDC`, `addArcToPath` and
      `addCircularArcToPath`, `Chart.setDrawHGridLines` and
      `setDrawVGridLines`, the `ScriptPosition` enum and
      `TextLine.setScriptPosition`, `Permissions.grant` and `revoke`, and Go
      `DrawStringUsingHighlightColors` replace the old names. `B5` is ISO B5
      and `JISB5` the size `B5` was; there are no `Table.WITH_n_HEADER_ROWS`
      constants, no `Font.STREAM` or stream font flag, and
      `FileAttachment(file)` and `Slice(angle, color, text)` take no `PDF` and
      no tooltip. `BaseAnnotation` is abstract, the `Destination`
      constructors, `Single`, the core font metrics classes, the Java
      `PDFobj` members and C# and Swift `Encryption.getKey` are internal, Go
      `GetKey` and the Go core font constants are removed, and the Java
      constant classes cannot be constructed. New: `Font(pdf, stream)` reads
      stream fonts, `Image.setLanguage`, the `SVGImage` link and marked
      content setters, and the `TextLine` URI getters.
      Breaking, names in one port: `Compliance.PDF_1_7` in Java, Go and
      Swift; C# `SVGImage.GetWidth` and `GetHeight`, and the C# `PathOperator`
      and `UserAccess` members and `Point.CONTROL_POINT_C`, `V` and `Y` in
      `UPPER_SNAKE`; the Swift `PathOperator` cases, `Point.CONTROL_POINT_C`,
      `V` and `Y` and the `StructElem` constants in `UPPER_SNAKE`; Go
      `RadioButton.Select`, `NewImageForObjects`, `Page.GetPenColor` and
      `GetBrushColor` and `mark.Uncheck`, and no exported `djb` and `round`
      packages, `ValidBitsMask`, QR code and PDF417 helpers, `JPGImage`,
      `BMPImage`, `FontStream1`, `FontStream2` or `NewCoreFontForPDFobj`; Java
      imports `com.pdfjet.fonts`, `com.pdfjet.qrcode`, `com.pdfjet.pdf417` and
      `com.pdfjet.datamatrix` for the font names and the 2D barcodes.
      Then: Data Matrix barcodes (Example_14), Swift encryption, random salts, `EncryptMetadata true`, right to
      left fixes, TODO cleanups, and the fixes and renames from the API audit.
      Fixed: the `## v9.0.0` entry in CHANGELOG.md, dated Sep 13, with the
      breaking changes first and then encryption, reading, right to left
      text, barcodes, tables, text, charts, pages, names, errors, parity,
      build and documentation.
- ✅ **B** Version bump: producer string `PDFjet v9.0.0` in `PDF.java`,
      `PDF.cs`, `pdf.go`, `PDF.swift`; `package-java.sh` and
      `package-dotnet.sh` archive names; README where 8.7.0 is mentioned.
      Fixed: the four producer strings and the two archive names; the README
      does not mention 8.7.0.
- ✅ **B** Regenerate the docs (`generate-documentation.sh`), check the four
      references on GitHub Pages after the push, and that the examples pages
      link the new Swift Example_30.
      Fixed: `generate-documentation.sh` builds the four references without
      errors, and DocFX without warnings once the C# `UserAccess` doc comments
      name the renamed members. The Java, .NET, Go and Swift references and
      the examples pages are up on GitHub Pages for 017d197b, with
      `scriptposition` and `com.pdfjet.qrcode`. The examples pages are for
      Java and .NET and both link Example_30; there is no Swift examples page.
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
  