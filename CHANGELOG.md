# Changelog

All notable changes to PDFjet are documented in this file. PDFjet ships four
parallel, hand-synchronized ports — Java (`com/pdfjet`), C# (`net/pdfjet`), Go
(`src`), and Swift (`Sources/PDFjet`) — kept behaviorally identical across
languages.

This is the first entry in this file; earlier releases were not tracked here.

## v9.0.0 — 2026-09-13

Producer string bumped from `PDFjet v8.7.0` to `PDFjet v9.0.0` in all four
ports (Java, C#, Go, Swift), and `package-java.sh` and `package-dotnet.sh` name
their archives v9.0.0. This major release makes the four ports behave the same
class by class, after a public API audit of all four; gives each concept one
name in every class and port; reads encrypted and damaged PDFs; adds encryption
to the Swift port, right to left text shaped and ordered by the Unicode
Bidirectional Algorithm, and Data Matrix barcodes; and makes the Build workflow
run and compare the examples of every port. The public API changes in many
places, so code written for v8.7.0 needs changes, and the Go module path is now
`github.com/edragoev1/pdfjet/v9`. Highlights below; see
`git log v8.7.0..v9.0.0` for the complete history.

### Breaking changes
- The Go module path is `github.com/edragoev1/pdfjet/v9`, as Go requires for a
  major version, so the Go imports change.
- Go `Drawable` declares `SetLocation`, which returns `Drawable` and so goes
  last in a chain, and every Go `DrawOn` returns `[2]float32`. `Arc.drawOn`
  returns the bottom right corner in all four ports. `Table`, `TextBlock`,
  `SVGImage` and `DonutChart` implement `Drawable`, and `DonutChart.drawOn`
  returns the bottom right corner of the outer circle.
- `Box` is removed in favor of `Rect`: `setColor` becomes `setBorderColor`, or
  `setFillColor` with `setFillShape(true)`, and `setLineWidth` and `setPattern`
  become `setBorderWidth` and `setBorderPattern`.
- A page size is an immutable `PageSize` with `getWidth` and `getHeight`. The
  `Page` and `BigTable` constructors and `Table.drawOn(pdf, pages, pageSize)`
  take one, and the Go page sizes are functions such as `letter.Portrait()`.
  `B5` is the ISO 216 B5 of 499 by 709 points; the Japanese B5 it was is
  `JISB5`.
- Many methods and constants are renamed or removed so that one concept has
  one name in every class and port. See "Names".
- `Text` is removed: a `TextFrame` without a height draws the same paragraphs
  the same way, with `setBorders(true)` for the border, and
  `Text.paragraphsFromFile` is `Paragraph.paragraphsFromFile`.
- Every color setter takes an `int` like `Color.blue` or the red, green and
  blue components as an array, in every port; the `setColor(r, g, b)` overloads
  of some setters in some ports are removed.
- `Chart.setXYChart` is removed with its category mode; bar charts are drawn
  with the new `BarChart`. `Chart` axis labels with whole number steps have no
  decimal places. See "Charts and calendars".
- Constants have types. The Go constant packages are typed, `PathOperator`,
  `ImageType`, `PageLayout`, `PageMode`, `ScriptPosition` and
  `ErrorCorrectionLevel` are enums in Java, C# and Swift, and `Align` is gone:
  every alignment is an `Alignment`.
- Errors are reported. Swift `PDF.complete()` and `PDF.addObjects` throw, Go
  `PDF.Read` and `ReadWithPassword` return an error, and the Go port panics or
  returns an error where it called `log.Fatal` or ignored the error. See
  "Errors".
- The `UserAccess` values are the permission bits of the standard, so code
  that passed raw integers must use them, `/P` is written with its reserved
  bits set, and `Permissions.grant` and `revoke` replace
  `setPermissions(flags, grant)`.
- Java: the font name classes and the QR code, PDF417 and Data Matrix classes
  are in `com.pdfjet.fonts`, `com.pdfjet.qrcode`, `com.pdfjet.pdf417` and
  `com.pdfjet.datamatrix`; import them.
- Java and C# code compiled against v8.7.0 must be recompiled.

### Encryption
- The Swift port encrypts PDFs, as the other ports do (Example_30).
- Encrypted PDFs are read with their user or owner password in all four ports,
  and passwords are used the same way in all of them.
- The permission bits of encrypted PDFs are fixed: `/P` has the reserved bits
  of ISO 32000-2 set, which makes it negative, and the encrypted metadata is
  declared with `EncryptMetadata true`.
- The last four bytes of `/Perms` are random in all ports, and the Java
  password hashes use random salts.
- An encrypted PDF/UA file grants the permission to extract its contents for
  accessibility.
- `Permissions.grant` and `revoke` replace `setPermissions(flags, grant)`.
  Java and Swift `Permissions` keep `getAccess` and lose `getRawValue`; C# and
  Go keep `GetRawValue`, which returns the flags as an unsigned number. C# and
  Swift `Encryption.getKey` is internal, and Go `GetKey` is removed.

### Reading existing PDFs
- Filter chains, encrypted PDFs, hybrid reference files and broken
  cross-reference tables are read.
- The `/DecodeParms` predictor is applied to FlateDecode and LZWDecode streams,
  and skipped for the image XObjects that were read.
- The bytes of strings and names are copied unchanged, and the images and the
  joined content streams of a read PDF are copied.
- The C#, Go and Swift ports read PDFs as Java does, and Swift numbers the
  `/GS` graphics states of `setGraphicsState` correctly.
- `PDF.addObjects` raises a clear error when the objects have no root `/Pages`
  object.

### Right to left text
- Bidi directions are resolved with the Unicode Bidirectional Algorithm, and
  the Bidi embedding controls are left out of the drawn text.
- Arabic, Persian and Urdu letters are shaped, the lam-alef ligature is put in,
  letters join around a zero width joiner, and the joined forms of a word
  broken over two lines are kept.
- `TextBlock` wraps right to left text, breaks lines at zero width spaces and
  breaks a word too wide for a line. `TextBlock.setLanguage` is new.
- Marks are placed on letters, ligatures and other marks from the GPOS table
  of .otf and .ttf fonts.
- Copied text reads right: each glyph maps to one character in the ToUnicode
  CMaps, shaped letters map back to Unicode, shared CJK glyphs map to the
  ideographs instead of the radicals, mirrored brackets carry the typed
  brackets as ActualText, brackets around left to right text are copied
  right, the zero width non-joiner and joiner are kept, and the drawn text of
  a `TextLine` is left out of its ActualText and Alt.
- The README documents right to left text, how it comes out when copied and
  its limits, and Example_27 is PDF/UA compliant in all ports.

### Barcodes
- Data Matrix barcodes are new in the four ports (Example_14).
- A PDF417 symbol is sized to its data instead of truncating it, and keeps its
  location when it is drawn again. Go `QRCode` and `PDF417` implement
  `Drawable`.
- EAN-13, UPC-A, Code 128 and Code 39 barcodes are drawn left to right and
  turned to the direction of the barcode, so EAN-13 and UPC-A honor the
  direction, Code 128 is drawn bottom to top, and top to bottom the text of
  Code 128 is on the left, as that of Code 39 is. `drawOn` returns the bottom
  right corner of the bars and the text in every direction.
- An invalid Code 39 character or barcode type fails in the four ports, and Go
  draws the text of a barcode in a table cell under the barcode.
- `Barcode.setDirection` takes a `Direction`, and the barcode constants of
  `Barcode` are gone; `PDF417.setModuleLength`, `QRCode` and
  `DataMatrix.getModules` and `setModuleColor`, and the
  `ErrorCorrectionLevel` enum.

### Tables and cells
- A table no longer skips as many data rows as it has header rows on the first
  page and at every page break, and the lines of a wrapped header cell repeat
  on every page.
- `Table.drawOn(null)` measures a table without changing what a later `drawOn`
  draws, where that draw crashed.
- `rightAlignNumbers` right-aligns the same texts in the four ports, the
  column setters change the `TextBox` of a cell, as in Java, and
  `Cell.setBorders(true)` and `Table.setCellBorders(true)` turn the four
  borders on in every port.
- `Cell.setTextBox`, `setTextBlock` and `setTextColumn` clear the cell text;
  `Cell.setFont` sets only the font, while `Table.setFontInRow` and
  `setFontInColumn` set the font and its size; the text of a cell is measured
  at the cell's font size with the fallback font; a justified cell draws its
  content left aligned instead of throwing; and a right aligned image or
  barcode keeps the right padding.
- The Java file constructor of `Table` throws `IOException`. Tables and
  `BigTable` read their files as UTF-8, drop a byte order mark and keep the
  empty fields at the end of a line. `BigTable` splits lines at the delimiter
  literally, `setLocation` sets the location, and `setLanguage`, which did
  nothing, is removed.
- The `Table(f1, f2)` constructor, which ignored its fonts, and the
  `WITH_n_HEADER_ROWS` constants are removed; pass the number of header rows.

### Text
- `TextBox`: measuring a text box that grows to fit its text, as
  `Cell.getHeight` does, no longer fixes its height; underline and strikeout
  are drawn in the text color; `setBorder` takes a flag, so a border can be
  removed.
- `TextBlock` draws the characters its font lacks in the fallback font, draws
  a text of only line breaks as one empty line, and `getHeight` returns the
  drawn height when the text is taller than the set height.
- `TextFrame` and `TextColumn` keep every setting of a line they wrap,
  including its vertical offset and its link. `TextFrame` always finishes
  flowing, and its border is black by default.
- `TextColumn.setTextAlignment` applies to the paragraphs without an alignment
  of their own.
- C# `CompositeTextLine` lays out its lines as Java does.
- `Content.ofTextFile`, and so `Util.readLines`, drops a byte order mark, and
  `Text.readLines` moves to `Util.readLines` (Go `util.ReadLines`).

### Charts and calendars
- New `BarChart` in the four ports: categories on one axis, the bars of the
  series grouped inside each category, vertical or horizontal, with a value
  axis that always includes 0, a legend under the title, optional value labels
  at the bar ends, and grid, axis and border line settings. Example_39 and
  Example_40 draw a horizontal and a grouped vertical bar chart with it.
- `Chart` draws only XY charts: `setXYChart` and its category mode are removed,
  bar charts are `BarChart`. Stroke widths are page units and are no longer
  scaled by the plot to chart width ratio, so a path is as wide as it was set.
  The axis labels have the decimal places of the axis step, so an axis with
  whole number steps has whole number labels; the default minimum is 0
  decimal places. The title and axis titles are centered at the size they are
  drawn, and the text of a path series starts at its first point, centered
  across the stroke.
- `Chart` handles negative, empty and flat data and its colors the same way in
  the four ports, and writes the same axis labels whatever the locale.
- `DonutChart` computes the same percentages in every port.
- `CalendarMonth` lays out the calendar as Java does in the four ports.

### PDF, pages and drawing
- Every PDF gets an `/Info` dictionary with its producer, creation date and
  the properties set with `setTitle`, `setAuthor`, `setSubject`, `setKeywords`
  and `setCreator`, and every PDF gets its own ID: 16 random bytes from the
  system's secure random number generator, where Java, Go and Swift hashed the
  time in milliseconds, so documents made in the same millisecond had the
  same ID.
- Numbers in content streams are rounded the same way in every port, the font
  descriptor metrics are in 1/1000 em, and the output does not depend on the
  platform charset or locale.
- The pen width, colors and graphics state getters of `Page` return what
  readers draw with.
- `Font` measures what `Page` draws. `Font(pdf, stream)` reads OpenType,
  TrueType and `.otf.stream` and `.ttf.stream` fonts, told apart by their
  first bytes, so the `Font.STREAM` flag is gone.
- `Stamp` behaves the same in the four ports and tags the content it draws.
- PNG images with row filters and palette transparency, top-down BMP images and
  SVG files are read correctly, and `Image.setFlipUpsideDown` flips an image in
  place.
- Grayscale PNG images with alpha are embedded as a gray image with a soft
  mask instead of crashing, and an interlaced PNG image fails with a clear
  message instead of crashing.
- SVG images: `fill="none"` without a stroke draws nothing instead of a black
  shape, `none` on a path overrides the colors of the svg element, and an open
  path with a stroke is stroked.
- Untrusted input is limited: a stream, a font stream or the samples of a PNG
  or BMP image decode to at most 256 MiB, and the size, bit depth, color type
  and palette of an image are checked before any buffer is allocated for it,
  so a decompression bomb or a lying header fails with an error instead of
  exhausting the memory. PNG chunks are read in pieces, which also reads
  streams that return few bytes at a time, and a truecolor PNG with a
  suggested palette is decoded as truecolor. Java and C# write PDFs larger
  than 2 GiB with correct offsets.
- Java reads a PDF that has a blank page. A truncated Flate stream fails with
  an error in the four ports: Swift crashed and C# returned the bytes decoded
  so far. C# no longer hangs on a truncated BMP image.
- `Rect`, `Point`, `Page`, `PDF` and `PDFobj` behave the same in the four
  ports. `Line.setLocation` moves the whole line, `Rect.scaleBy` keeps the
  location, and `Path.setLocation` sets the offset instead of adding to it.

### Names
- Box outlines are borders (`TextBox`, `Cell` and `CheckBox.setBorderColor`
  and `setBorderWidth`, `Table.setCellBorderColor`,
  `TextBlock.setCornerRadius`), and lines have strokes (`Line` and `Path`
  `setStrokeWidth`, `setStrokeColor` and `setStrokeDashPattern`,
  `Line.setLineCapStyle`, `Form.setStrokeWidth`). `Page` keeps pen and brush,
  with `setDefaultPenWidth`.
- An angle is set with `setRotation`, counter-clockwise, or
  `setRotationClockwise`, and a text rotation in degrees with
  `setTextRotation`.
- Scaling and moving: `Arc.scaleBy`, `TextParameters.setLocation`,
  `CompositeTextLine.getLocation` and `getMinMaxY`, and `Title.setOffset` sets
  the offset.
- Alignment: `Cell.setVerticalAlignment`, `Table.setTextAlignmentInColumn`,
  and `Paragraph` and `TextColumn.setTextAlignment`.
- Text boxes: a gap is in points and a spacing a multiplier, so `setPadding`,
  `setLineGap`, `setParagraphGap` and `setHighlightColors`;
  `Paragraph.setTextColor`, `TextLine.setDecorationColor` and
  `CheckBox.setCheckmarkColor`.
- Duplicates are removed: `Table.getCellAtRowColumn`, `getRowAtIndex` and
  `getColumnAtIndex`, `Font.getHeight`, `Container.addBorder`, the `Cell`
  side border methods, and `TextColumn.addChineseParagraph` and
  `addJapaneseParagraph`, which are one `addCJKParagraph`.
- Misleading names are renamed: `BaseAnnotation.setOpacity`,
  `FileAttachment.setContents` and `setIconPushpin`, `DonutChart.setRadii`,
  `Bookmark.getDestinationName`, `Table.autoAdjustColumnWidths`,
  `Point.setDrawPath(boolean)`, `Cell.setMarker`, `Page.addBDC`,
  `addArcToPath` and `addCircularArcToPath`, `Chart.setDrawHGridLines` and
  `setDrawVGridLines`, `TextLine.setScriptPosition` with the `ScriptPosition`
  enum instead of `Effect`, and Go `DrawStringUsingHighlightColors`.
- Dead members are removed or reachable: `FileAttachment` takes no `PDF`,
  `Slice` has no tooltip, `BaseAnnotation` is abstract, the `Destination`
  constructors are internal, and `Image.setLanguage`, the `SVGImage` link and
  marked content setters and the `TextLine` URI getters are new.
- Members that were public by accident are internal: `Token`, `Single`, the
  core font metrics classes and the Java `PDFobj` members, and the Java
  constant classes cannot be constructed.
- Names in one port: `Compliance.PDF_1_7` in every port; the C# and Swift
  constants use Java's `UPPER_SNAKE` names (`PathOperator.STROKE`,
  `UserAccess.PRINT`, `Point.CONTROL_POINT_C`, Swift `StructElem.DOCUMENT`);
  C# `SVGImage.GetWidth` and `GetHeight`; Go `RadioButton.Select`,
  `NewImageForObjects`, `Page.GetPenColor` and `GetBrushColor` and
  `mark.Uncheck`, and the Go helpers are no longer exported.

### Errors
- Swift `PDF.complete()` throws when the PDF cannot be written, and Swift
  throws on invalid PNG, BMP, OTF and SVG data and stops with an error on QR
  data that does not fit, `Stamp` text without a font and `Form.drawOn(nil)`.
- The Go port panics, or returns an error where the function returns one,
  where it exited the program with `log.Fatal`, and no longer ignores errors
  writing, compressing or encrypting a PDF or reading an image, font or
  embedded file.
- `Content.ofBinaryFile` and `Content.ofTextFile` report a missing file, and C#
  throws on a truncated font stream.

### Port parity
- `audit-api.py` lists the public types and members of the four ports and what
  is not in every port; the first run found some 60 types and 400 members, and
  the report now shows the port differences the README documents.
- The defaults, the copies that colour getters return and setters keep, and
  the errors are the same in the four ports, and the C#, Go and Swift examples
  read like the Java ones.

### Build, checks and examples
- The Build workflow builds and runs the examples of all four ports, compares
  their PDFs and content streams with Java's, checks the PDF/UA and PDF/A
  examples with veraPDF, and fails on compiler warnings and `go vet` problems.
  `check-examples.sh` runs the same checks locally.
- A Windows workflow tests the `.cmd` build scripts, the Java port builds and
  runs on JDK 8 too, and the Swift port handles Windows line endings.
- The Swift port uses no NS classes, and the commented-out code and the 49
  TODO markers are gone from the four ports.
- Example_14 is the Data Matrix example, and the example pages describe what
  each example does.
- `go get github.com/edragoev1/pdfjet/v9` works: `fonts/`, `data/` and
  `images/` have their own `go.mod`, which keeps them out of the Go module, as
  the fonts made it larger than the 500 MiB Go allows.
- The Swift package declares the `PDFjet` library product, so other Swift
  packages can depend on it.
- Unit tests in all four ports, with the same cases and expected values:
  JUnit 5 for Java, xUnit for C#, the `testing` package for Go and Swift
  Testing for Swift. `test-java.sh`, `test-dotnet.sh`, `test-go.sh` and
  `test-swift.sh` run them, and so do the Build workflow, which also runs the
  Java tests on Java 8, and `check-examples.sh`. They add no dependency to the
  libraries.

### Documentation
- The C# API reference is built by DocFX and published under `dotnet/`, next
  to the Java, Go and Swift references, and the Swift reference is published
  again.
- `generate-documentation.sh` stops on the first error and runs doc2go with
  `go run`, so it needs no install.
- The README documents the port differences, `PageSize` and right to left
  text.

## v8.7.0 — 2026-09-10

Producer string bumped from `PDFjet v8.6.0` to `PDFjet v8.7.0` in all four
ports (Java, C#, Go, Swift), and `package-java.sh` and `package-dotnet.sh` name
their archives v8.7.0. This release, 128 commits since v8.6.0, brings the C#,
Go and Swift ports back in line with the Java reference example by example,
fixes the tagged PDF (PDF/UA) and PDF/A output, and publishes API references
for all four ports on GitHub Pages. It changes the public API: setters return
the object they are called on, and `setPosition`, the deprecated methods and
the unused `Embed` enum are removed. Highlights below; see
`git log 1d2ff4bf..9bf91da8` for the complete history.

### Breaking changes
- Setters return the object they are called on, so Java and C# code compiled
  against v8.6.0 must be recompiled. See "Fluent setters".
- `setPosition` is removed in favor of `setLocation`, and `Drawable` declares
  `setLocation`. See "`setPosition` removed; use `setLocation`".
- The deprecated methods are removed from all four ports. See "Deprecated
  methods removed".
- The `Embed` enum (`YES` and `NO`), which nothing in the library used, is
  removed from Java, C# and Swift. Go keeps its `embed` package.
- Go `NewCJKFont` takes a `cjkfont.Font` (`cjkfont.AdobeMingStdLight`,
  `STHeitiSCLight`, `KozMinProVIRegular` or `AdobeMyungjoStdMedium`) instead of
  a font name, and the four font name constants in the `pdfjet` package are
  gone.
- UPC-A barcodes require exactly 11 digits and EAN-13 barcodes exactly 12.
  Other input is rejected when the barcode is created: Java and C# throw, Go
  calls `log.Fatal` and Swift calls `fatalError`.
- Go and Swift `Cell` default to a 75pt width, like Java and C#, so tables that
  do not set their column widths lay out wider than before.
- Java `Ellipse` is `final`, and Go `Stamp.DrawOn` returns `[2]float32`.

### Fluent setters
- Every public setter that returned nothing now returns the object it was
  called on, so calls can be chained:
  `box.setSize(20f, 20f).setColor(Color.red).setLineWidth(1f)`. Setters that
  already returned the object are unchanged.
- Swift setters are marked `@discardableResult`, so existing calls that ignore
  the result compile without warnings.
- Java and C# code compiled against v8.6.0 must be recompiled, because the
  setters' return types are part of the compiled method signatures.

### `setPosition` removed; use `setLocation`
- `setPosition` is gone from every class in all four ports. Call
  `setLocation`, which does the same thing: on `Arc` it sets the center, and
  on `Line` the start point.
- `Drawable` declares `setLocation` instead of `setPosition`. It returns
  `Drawable` in Java (each class returns its own type), `IDrawable` in C#
  (each class has a public `SetLocation` returning its own type plus an
  explicit `IDrawable.SetLocation`), and `Self` in Swift. Classes outside
  PDFjet that implement `Drawable` or `IDrawable` must implement `setLocation`.
- Go's `Drawable` declares only `DrawOn`. A Go type only satisfies an
  interface with an exact signature match, and each Go `SetLocation` returns
  its own type so it can be chained.
- `Title.setLocation` now moves both the prefix and the title text in Java, C#
  and Swift (Go's `Title` has no location setter). Before, Java's
  `setLocation` and C#'s `SetPosition` moved only the text, and Java's
  `setPosition(double, double)` called itself until the stack overflowed.

### Deprecated methods removed
- `TextLine.setColor` is removed from all four ports; call `setTextColor`,
  which it only forwarded to. The Swift `DonutChart`, the Java `Example_37` and
  the Swift `Example_03`, `Example_37` and `Example_41` now call `setTextColor`.
- `TextColumn.setSize` is removed from all four ports; call `setWidth` and
  `setHeight`. Java, Go and Swift get `setHeight`, which C# already had, and
  their `Example_10` uses the two setters, as the C# example already did.
- C# `Page.SetPenColor(float, float, float)` is removed; call
  `SetPenColor(float[])`.
- C# `Page.DrawBezierCurve`, deprecated since v4.00, is removed; call
  `DrawPath`.
- Swift `SVGImage.getPenWidth()` is removed; call `getWidth()`.
- The commented-out `setPenColor` and `setBrushColor` overloads that took
  separate red, green and blue values are removed from the Java and C# `Page`
  sources.
- Code that calls a removed method must switch to its replacement.

### Stamp
- `Stamp` now conforms to `Drawable` in Swift and Go, as it already did in
  Java and C#, so it can be added to a `Container` or an
  `OptionalContentGroup`. Swift `drawOn` takes `Page?` and `setLocation`
  returns `Self`; Go `DrawOn` returns `[2]float32` instead of `[]float32`.

### Tagged PDF (PDF/UA) and PDF/A
- In a tagged document every `/Pg` reference pointed at the last page, and the
  parent tree keyed the annotations by numbers that did not match their
  `/StructParent` values, so nothing resolved. Each page now tracks its own
  structure elements and the keys match (Java, C#, Go).
- The Swift port never wrote the structure tree: every PDF/UA document had an
  empty `/K` and `/Nums` while its pages declared `/StructParents`. It is
  written now, as in the other ports.
- In PDF/UA mode every annotation dictionary was written twice and the pages
  referenced the untagged copy, so links could not be resolved through the
  parent tree. Each annotation is written once (all four ports).
- `Rect`, `Path` and the backgrounds and borders a `Cell` draws are marked as
  artifacts, so tagged documents with rectangles, paths or tables pass PDF/UA
  rule 7.1-3 (all four ports). PDF417 bars are artifacts in C#, Go and Swift,
  as they were in Java, and the C# `TextBox` tags its text.
- Link annotations are written with a `/Contents` entry, and a link made with
  `setGoToAction` uses the destination name as its description.
- In encrypted documents the XMP metadata and ICC profile streams are encrypted
  like every other stream; readers used to decrypt them into garbage. The font
  license notice in the XMP metadata is the notice text instead of `[B@...`
  (Java) or `System.Byte[]` (C#).
- The examples set the same compliance level in every port: PDF/UA-1 on
  Example_01, 12, 15, 16, 22, 47, 48 and 49, PDF/A-1B on Example_34 and PDF/A-3B
  on Example_07. Example_34 embeds its fonts, which PDF/A requires. The tagged
  examples were checked with veraPDF 1.30.2.

### Reading existing PDFs
- `getFontObjects` picked up only the first font of an imported resource
  dictionary, so the other fonts were referenced but never written (all four
  ports).
- `addResourceObjects` wrote imported objects under their own numbers while
  the xref counted them from 1, so documents whose fonts were not numbered from
  1 got wrong offsets. Imported objects keep their numbers, and the xref has
  free entries for the gaps (all four ports).
- The Swift tokenizer split PDF names such as `/colon` in two when a document
  was rewritten; it now matches the other ports. The `FontStream2` xpacket
  marker is fixed.
- The dictionary scans are bounds-checked, and `/ExtGState` is written in a
  deterministic order.

### Text layout
- `Page.drawString` with a fallback font advanced past each run at the font's
  own size in Go and Swift, so the runs were misplaced when the drawn size
  differed.
- `TextBox` wraps its text at the font size it draws with; after `setFontSize`
  the text could overflow the box (all four ports).
- `TextLine` measures its width, its bottom-right corner and its link rectangle
  at the font size it draws with (all four ports).
- The two `Table` wrapping loops measure the same way; Go broke long tokens one
  character early.
- `CompositeTextLine` applies the subscript and superscript size factors (Java,
  Swift), and C# positions the two effects with the constants it declares.

### Port parity
- `TextBox` is ported to Go and Swift: wrapping for word-separated and CJK
  text, fixed-height and grow-to-fit layouts, vertical and horizontal
  alignment, the four borders, underline and strikeout, the text directions
  and the link annotation. Go gets a `border` package.
- Go and Swift `Cell` can hold a text column, a text box and a composite text
  line, like Java.
- Java `RadioButton` draws its label, as the other ports already did.
- Java `Cell.setTextColor(int)` dropped the green channel,
  `Cell.setBackgroundColor(float[])` did nothing, and
  `Table.setTextColorInRow`, `setTextColorInColumn` and `setCellBordersColor`
  were empty stubs. All of them work now, in every port.
- Go `TextBlock` no longer splits every line into per-word color operators, Go
  `SVGImage` no longer fills icons that set no colors in black, Go `Stamp`
  writes colors with two decimals, Go `Arc` writes its default dash pattern,
  and Go ToUnicode CMaps use lowercase hex, like Java.
- C# `CheckBox.DrawOn` returns the same height as the other ports.
- Swift `EmbeddedFile` writes `/Filter /FlateDecode` only for compressed files,
  and writes the file name as a hex string, like Java and Go.

### Barcodes
- UPC-A barcodes require exactly 11 digits and EAN-13 barcodes exactly 12, with
  an error when the barcode is created. Short or non-numeric input used to draw
  a wrong barcode or fail inside `drawOn`.
- Example_12 encodes `data/Example_12.java` instead of its own source file, so
  its PDF417 barcode is the same in every port.

### Fixes
- `PageMode.USE_ATTACHMENTS` is `"UseAttachments"`, the page mode name in the
  PDF specification, in Java, C# and Swift. It was `"UseAttachements"`, which
  is not a PDF page mode, so viewers ignored it instead of opening the
  attachments panel.
- Swift `A3.PORTRAIT` is 842 × 1191 points, like the other ports; it was the A4
  size.
- Swift `Stamp.fillRect` fills the whole rectangle; it drew only three corners
  and filled a triangle.
- Swift `SVGImage` has `getWidth()`, like the other ports, instead of the
  misnamed `getPenWidth()`.
- Java and C# `setLineColor(Color.transparent)` leaves the line color unchanged,
  as in Swift. It used to clear the text color.
- C# `Table.GetColCount` returns 0 for a row index equal to `GetRowCount()`
  instead of throwing.
- C# `NumberFormat.Format` honors the minimum number of fraction digits.
- Go `Arc.SetFillColorRGBArray` enables the fill, like the other fill setters;
  the color was stored but never drawn.

### Performance
- Go QR codes are about 48 times faster: the Galois-field tables are built once
  instead of on every lookup.
- Go loads large CJK fonts faster: the ToUnicode CMap no longer formats with
  `fmt.Sprintf`, and the glyph tables are read without an allocation per value.
- The Java, C# and Go compressor and decompressor code is simplified and checks
  more errors, and the Swift `FlateEncode` is faster.

### Examples
- The C#, Go and Swift examples were compared with the Java originals, by page
  content stream and by rendering, and brought in line with them.
- Example_30 is the encryption example in Java, C# and Go; it swapped places
  with Example_46. The Swift port has no encryption support, so it has 49
  examples and no Example_30.
- The Go Example_45 writes `Example_45.pdf`; it used to overwrite
  `Example_42.pdf`.
- The Example_21 heading no longer ends with the name of the port's language.
- The extra C# Example_51 and Java Example_52 are removed.

### Documentation
- The C# API reference is built with DocFX (configuration in `docfx/`) into
  `docs/dotnet`. It replaces the copy of the Javadoc HTML that
  `util/Translate.java` rewrote with Java-to-C# word substitutions;
  `Translate.java`, `translate-words.txt` and `capitalize-words.txt` are
  removed. `./generate-documentation.sh` builds both references; install
  DocFX once with `dotnet tool install -g docfx`.
- The C# doc comments are XML doc comments (`/// <summary>`, `<param>`,
  `<returns>`) instead of Javadoc-style `/** @param */` blocks, which DocFX and
  IDEs do not read. Class comments that sat above `namespace` moved onto their
  types, and file headers became plain `/* */` comments.
- Javadoc for the Java port reports no warnings or errors. 443 doc comments
  were added across 49 files in `com/pdfjet`, covering the public classes,
  constructors, methods, fields and enum constants that had none, and the
  missing `@param` and `@return` tags. Classes that only hold constants or
  static methods got the same documented no-argument constructor they already
  had implicitly, so the public API is unchanged.
- The Java reference also covers the `com.pdfjet.barcodes`,
  `com.pdfjet.corefonts` and `com.pdfjet.encryption` packages, and the
  `com.pdfjet` classes kept in `fonts/`, `pdf417/` and `qrcode/` (`NotoSans`,
  `PDF417`, `ErrorCorrectLevel` and others), which `generate-documentation.sh`
  used to skip. 331 doc comments were added across 36 of their files, and
  Javadoc still reports no warnings.
- `docs/` is no longer tracked in git. The `Documentation` GitHub Actions
  workflow (`.github/workflows/docs.yml`) builds the Java and C# references on
  every push to `master` and publishes them to GitHub Pages at
  <https://edragoev1.github.io/pdfjet/>, under `java/` and `dotnet/`.
- The published site also serves `examples-java.html`, `examples-dotnet.html`
  and the example sources they link to, which GitHub Pages used to publish
  straight from `master`, and its home page links to all of them. The Pages
  source must be set to GitHub Actions; the rendered README, `CHANGELOG.html`
  and `SECURITY.html` are no longer published.
- The source links in `examples-dotnet.html` point to
  `examples/Example_NN/Example_NN.cs`, where the C# examples are, instead of
  `examples/Example_NN.cs`, which returned 404.
- The `Example_NN.pdf` links on `examples-java.html` and
  `examples-dotnet.html` work on the published site; they returned 404,
  because the PDF files are not kept in git. The `Documentation` workflow checks
  out `fonts`, `images`, `data` and `PngSuite`, runs `build-java.sh`, fails if
  an example does not create its PDF file, and publishes the 50 PDF files next
  to the pages. The C# page says that its PDF files come from the Java
  examples, which produce the same documents.
- Both example pages have a title and a character set, and the stray
  `</strong>` in their introduction is gone. The C# page is headed "PDFjet C#
  Examples" instead of "PDFjet Java Examples", and names `build-dotnet.cmd` and
  `build-dotnet.sh` instead of `build-dotnet-core.cmd`, `build-mono.cmd` and
  `build-mono.sh`, which do not exist.
- In the C# reference, the `PDFjet` part of "Namespace: PDFjet.NET" on every
  class page linked to a page that did not exist. DocFX now builds
  `api/PDFjet.html` from `docfx/redirects/PDFjet.md`, which redirects to the
  `PDFjet.NET` namespace page. The home page title is "PDFjet for .NET"
  instead of "PDFjet for .NET | PDFjet for .NET".
- The C# `Salsa20` class, which generates the document ID, is `internal` in the
  `PDFjet.NET` namespace, like the package-private Java class. It was a public
  class in the global namespace, the only C# type outside `PDFjet.NET`, and
  DocFX left it out of the reference.
- The Go API reference is built with doc2go into `docs/go` by
  `./generate-documentation.sh` and published under `go/`, next to `java/` and
  `dotnet/`; the site's home page links to it. It covers the 56 packages in `src`
  and leaves out the example programs in `src/examples`. Install doc2go once
  with `go install go.abhg.dev/doc2go@v0.12.2`.
- The Swift API reference is built with DocC, which comes with the Swift
  toolchain, into `docs/swift` and published under `swift/`; the site's home
  page links to it. `generate-documentation.sh` extracts the public API with
  `swift package dump-symbol-graph`, and `swift/` redirects to the `PDFjet`
  module page.
- The Swift doc comments use DocC markup, so DocC renders them and reports no
  warnings: `- Parameter name:`, `- Returns:` and `- Throws:` instead of the
  Javadoc tags `@param`, `@return` and `@throws`, which DocC showed as plain
  text, Markdown instead of HTML tags, and fenced code blocks instead of
  `<pre>`. `- Parameter` lines without the colon DocC needs got one. `@throws`
  lines on functions that do not throw are removed, and parameter
  descriptions that named the wrong parameter now match the signatures.
- Every exported Go identifier and every Go package has a doc comment, and
  the C# XML docs and the Swift public API have no undocumented members.
  Javadoc also reports no warnings for the protected fields and methods.

### Build scripts and sources
- `SECURITY.md` is added.
- Java, C# and Swift sources use 4-space indentation and Go sources use tabs;
  `.editorconfig` pins both.
- The Java library compiles without compiler warnings.
- The `.cmd` scripts compile the same sources as the `.sh` scripts, and
  `clean.cmd` is added.
- The profiling scripts and `switch.cmd` are removed.

### Java 8 compatibility
- `build-java.sh`, `build-java.cmd`, `run-java.sh` and `run-java.cmd` compile
  the library with `javac --release 8`, so `PDFjet.jar` runs on Java 8 and
  later whichever JDK builds it. Before, the class files matched the JDK that
  built them (Java 11 with this machine's default `javac`). `--release 8` also
  rejects any API newer than Java 8; the library needed no changes, and all 50
  examples run on Java 8.
- The scripts need `javac` from JDK 9 or newer, because Java 8's `javac` has no
  `--release` option. `-Xlint:-options` hides the "release 8 is obsolete"
  warning that JDK 21 and newer print.

## v8.6.0 — 2026-09-05

Producer string bumped from `PDFjet v8.5.0` to `PDFjet v8.6.0` in all four
ports (Java, C#, Go, Swift). This release consolidates roughly a year of
incremental work since v8.5.0 (2025-09-07) — 1,570+ commits — across all
four ports. Highlights below; see `git log 31641df8..bad917c0` for the
complete history.

### Font parsing (OTF/TrueType)
- Fixed `cmap` format-4 glyph-ID arithmetic in all four ports: the final
  glyph ID is now correctly wrapped to unsigned 16 bits (`& 0xFFFF`) instead
  of leaving the sum unwrapped, which could overflow past the valid glyph-ID
  range for certain fonts.
- Fixed the `name`-table font-info fallback logic in Java and C# (the
  Macintosh/Windows fallback check was always true, a dead branch).
- Fixed a Go-specific bug where the Macintosh font-info branch wrote the
  wrong field (`fontName` instead of the record's own decoded text).
- Verified by scanning all 272 real font files shipped in `fonts/` and by
  byte-for-byte diffing every example's PDF output before and after each
  fix; all differences were confirmed non-deterministic (timestamps,
  trailer `/ID`, and — in Go/Swift — randomized map/dictionary iteration
  order), never a change in rendered output.

### Charts
- `Chart.java`: removed dead code in the category-mode point transform and
  replaced a fragile `List.indexOf`-based palette lookup (correct only by
  coincidence of `Point`'s default identity-based `equals()`) with an
  explicit loop counter.
- Continued Chart and DonutChart improvements across the four ports.

### Barcodes
- Fixed CODE_128 codeword-cap bypass and text-mutation bugs.
- Fixed EAN-13 label layout (leading digit rendered outside the guard bars)
  and UPC-A label layout (outside digits overflowing the barcode).
- Synced Barcode.cs, barcode.go, and Barcode.swift to match the corrected
  Java implementation.

### Text layout
- Fixed a spurious blank line and O(n²) wrapping behavior in TextBox (Java
  and C#).
- Ongoing TextColumn/TextLine layout and line-height fixes.

### Images
- Hardened JPGImage decoding: added EOF checks and fixed several decoding
  edge cases.

### Performance
- Pooled `zlib.Writer` in the Go compressor instead of allocating one per
  call.
- Shared the fixed Huffman tables in FlateEncode instead of rebuilding them
  per use.
- Sped up Page's per-character text-drawing hot path and BigTable's CSV
  splitting.

### Encryption
- Continued encryption work: AES-128/256 support, encryption of embedded
  files and PNG images, and fixes so embedded/TTF fonts work correctly
  under encryption.

### Cross-language parity
- Ongoing synchronization work keeping the C#, Go, and Swift ports
  behaviorally identical to the Java reference implementation.

## v8.5.0 — 2025-09-07

Baseline for this changelog. See `git log` prior to `31641df8` for history.
