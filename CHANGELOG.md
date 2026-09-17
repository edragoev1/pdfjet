# Changelog

All notable changes to PDFjet are documented in this file. PDFjet ships four
parallel, hand-synchronized ports — Java (`com/pdfjet`), C# (`net/pdfjet`), Go
(`src`), and Swift (`Sources/PDFjet`) — kept behaviorally identical across
languages.

This is the first entry in this file; earlier releases were not tracked here.

## v9.0.1 — planned for 2026-10-21

Producer string bumped from `PDFjet v9.0.0` to `PDFjet v9.0.1` in all four
ports, so `.packaging/package-java.sh` and `.packaging/package-dotnet.sh` name
their archives v9.0.1. The public API does not change.

### Added
- A QR code is as large as its data needs, from version 4, 33 by 33 modules,
  to version 40, 177 by 177 modules, in all four ports. It held at most 78
  bytes at level L and 34 at level H, and now holds up to 2,953 and 1,273, so
  payment codes, contact cards and long links fit. Data that fitted before
  makes the same symbol as before; data too long for version 40 throws, or
  panics in Go, with the most bytes the level holds. Unit tests check the
  sizes and the version information, and every version and level was read
  back with a QR code reader. The Swift port no longer hangs on the timing
  patterns of version 7 and up, and shares its Galois field tables, as the
  other ports do.

### Changed
- `Cell` keeps its text, background and border colors as packed 0xRRGGBB
  values in Java, C# and Swift, instead of an array of three floats for each
  color of each cell; Go already kept them inline. The setters and getters
  are the same, and a color given as floats is kept to the nearest of 256
  steps, between 0.0 and 1.0. In Java the protected `textColor`,
  `backgroundColor` and `borderColor` fields are now `int`, -1 when not set.
  Example_43 drawn with `Table`, 1.12 million cells, allocates 1,718 MB
  instead of 1,821 and finishes in a 384 MB heap instead of 512, with the
  same PDF. `Page.setBrushColor(int)` and `setPenColor(int)` no longer
  allocate when the color is already set.

### Fixed
- Four bugs in `Cell`, in all four ports. The underline and the strikeout of
  the cell text are drawn in the text color; they were drawn in the border
  color, or in whatever color the pen carried when no border color was set.
  `setBorderColor(Color.transparent)` leaves the borders the color of the pen,
  as `setTextColor(Color.transparent)` leaves the text color unchanged; it made
  the borders white. In Java and C#, `setTextColor(null)` with an array of
  floats leaves the text color unchanged; it left the cell without a text
  color, so the text was drawn in the background color and disappeared on a
  shaded cell. And each border line starts half the pen width back, so that the
  corners of a thick border close; they had a notch of a quarter of the width.
  Cells with the default border width of 0 draw as before.
- `TextFrame` aligns a paragraph to the right, to the center or to both
  edges, as `TextColumn` does, in all four ports; it drew every paragraph
  aligned to the left. A right aligned or centered row is moved as a whole,
  and a justified paragraph widens the spaces of every row but its last.
  Paragraphs aligned to the left are drawn as before.

### Examples
- Examples 18, 22, 26, 48 and 49 draw documents instead of test pages, in all
  four ports: a short document with "Page X of N" footers, a contents page
  linked to chapters and back, a survey with check boxes and radio buttons, the
  bookmarked outline of a guide to the PDF file structure, and a menu with
  mixed text styles. They use the same features as before.
- Examples 06, 07, 11, 20 and 36 show their features next to labels and
  explanations, in all four ports: attachments and annotations, a "DRAFT"
  watermark on a two-page PDF/A-3B proposal, labeled linear barcodes, a
  letterhead with a logo read from a PDF file, and a contents page drawn last
  but added first.
- Examples 04, 12, 21, 29 and 31 explain what they show, in all four ports:
  greetings in CJK fonts that are not embedded, a PDF417 barcode that holds a
  source file, the four QR code error correction levels with the data each
  holds, English and Greek text columns in table cells, and Hindi and Marathi
  text with opaque and transparent rectangles.

## v9.0.0 — 2026-09-16

Producer string bumped from `PDFjet v8.7.0` to `PDFjet v9.0.0` in all four
ports (Java, C#, Go, Swift), and `.packaging/package-java.sh` and
`.packaging/package-dotnet.sh` name their archives v9.0.0. This major release
makes the four ports behave the same class by class, after a public API audit
of all four; gives each concept one name in every class and port; reads
encrypted and damaged PDFs; adds encryption to the Swift port, right to left
text shaped and ordered by the Unicode Bidirectional Algorithm, and Data Matrix
barcodes; and makes the Build workflow run and compare the examples of every
port. The public API changes in many places, so code written for v8.7.0 needs
changes, and the Go module path is now `github.com/edragoev1/pdfjet/v9`.
Highlights below; see `git log v8.7.0..v9.0.0` for the complete history.

### Breaking changes
- The Go module path is `github.com/edragoev1/pdfjet/v9`, as Go requires for a
  major version, so the Go imports change.
- Go `Drawable` declares `SetLocation`, which returns `Drawable` and so goes
  last in a chain, and every Go `DrawOn` returns `[2]float32`. `Arc.drawOn`
  returns the bottom right corner in all four ports. `Table`, `TextBlock`,
  `SVGImage` and `DonutChart` implement `Drawable`, and `DonutChart.drawOn`
  returns the bottom right corner of the outer circle. `DonutChart.setLocation`
  sets the top left corner of the outer circle, where it set the center.
- `drawOn(null)` measures in every `Drawable`: it draws nothing and returns
  the corner that drawing returns, without changing what a later draw draws.
  `Image`, `SVGImage`, `QRCode`, `PDF417`, `Chart`, `BarChart`, `DonutChart`,
  `Container`, `CalendarMonth`, `Form`, `CheckBox`, `RadioButton`, `Line`,
  `Arc`, `Path`, `Stamp`, `FileAttachment` and the annotations failed on it,
  and `TextLine`, `CompositeTextLine` and `Title` returned their location.
  `Table.drawOn` and `TextColumn.drawOn` return the bottom right corner, as
  `Drawable` says, where they returned the left x: code that drew next to them
  from `xy[0]` subtracts the width now, as Example_10 does.
- `Box` is removed in favor of `Rect`: `setColor` becomes `setBorderColor`, or
  `setFillColor` with `setFillShape(true)`, and `setLineWidth` and `setPattern`
  become `setBorderWidth` and `setBorderDashPattern`.
- A page size is an immutable `PageSize` with `getWidth` and `getHeight`. The
  `Page` and `BigTable` constructors and `Table.drawOn(pdf, pages, pageSize)`
  take one, and the Go page sizes are functions such as `letter.Portrait()`.
  `B5` is the ISO 216 B5 of 499 by 709 points; the Japanese B5 it was is
  `JISB5`.
- Go returns errors where it panicked: `NewPDFFile` returns `(*PDF, error)`,
  `Complete` and `AddObjects` return `error`, `AddObjects` takes the slice
  of objects and `SetTextRenderingMode` returns `(*Page, error)`.
  `CompositeTextLine.GetMinMaxY` returns `[2]float32` like every other Go
  coordinate pair. Swift `Font(pdf, coreFont)` and `Page.addResource(coreFont,
  &objects)` throw, `Page.addResource` and `PDFobj.addResource` take the
  objects as `inout` for a font and an image too, and
  `PDF.addResourceObjects(from:)` has the label of `getPageObjects(from:)`.
- `Page.drawPath` and `Stamp.drawPath` with fewer than two points paint
  nothing, where Java and C# threw and Go and Swift stopped the program. `RadioButton.setFontSize`
  sets the size of the label and no longer resizes the shared `Font`, as
  `CheckBox.setFontSize` never did. An empty `setTitle`, `setAuthor`,
  `setSubject`, `setKeywords` or `setCreator`, and an empty annotation title
  or contents, write nothing in the four ports.
- A program that uses the API in a way that would write a broken PDF fails,
  and `complete()` then refuses to finish the document: a NaN, infinite or
  too large number, a malformed dash pattern, a negative pen width,
  `saveGraphicsState` and `restoreGraphicsState` or `addBDC` and `addEMC` out
  of pairs (counted whatever the compliance), drawing on a page that was
  written, a page added twice, after `complete()` or to another PDF, a second
  `complete()`, no pages, a font, image, stamp, optional content group,
  embedded file or bookmark page of another PDF, a stamp drawn before its
  `complete()`, stamp text without a font or text or in a core font,
  encryption or compliance set after a
  font, image or page, and a page under 3 or over 14,400 points. Java and C#
  throw at the call; Go and Swift record the first mistake, which `Complete`
  returns and `complete()` throws. See "Mistakes that are refused" in the
  README.
- Many methods and constants are renamed or removed so that one concept has
  one name in every class and port. See "Names".
- `Text` is removed: a `TextFrame` without a height draws the same paragraphs
  the same way, with `setBorders(true)` for the border, and
  `Text.paragraphsFromFile` is `Paragraph.paragraphsFromFile`.
- Every color setter takes an `int` like `Color.blue` or the red, green and
  blue components as an array, in every port; the `setColor(r, g, b)` overloads
  of some setters in some ports are removed.
- `DonutChart` takes the values of its slices: `Slice(value, color, label)`,
  and the chart computes the angles and the percentages. The constructor has
  no `isDonutChart` flag; an inner radius of 0 makes a pie chart.
- The Go port exports only what the other ports make public: the helper
  packages `code128`, `compressor`, `crc32util`, `decompressor`, `device` and
  `fastfloat` are under `src/internal`, the `embed` package is gone, the
  internal annotation, PNG chunk, font table, optional content, encryption
  key, SVG and saved state types and the BMP and JPEG decoder methods are
  unexported, and `content.GetFromReader` is `GetFromStream`, as in the other
  ports.
- The public fields of `Paragraph`, `Container` and `Title` are private in Java,
  C# and Swift, as in Go; `Paragraph.getX1`, `getY1`, `getX2`, `getY2`,
  `getTextX` and `getTextY` and `Title.getPrefix` and `getTextLine` are the
  getters, in the four ports.
- One rotation: every rotation setter is `setRotation(degrees)`, text turns
  with `setTextRotation`, and a positive angle turns clockwise, as on a
  screen, because y grows downward on a PDFjet page; a negative angle turns
  counterclockwise. `Arc.setStartAngle` counts clockwise from 3 o'clock, and
  `Arc.setSweep` replaces `setSweepDegreesCW` and `setSweepDegreesCCW`. The v8
  `setRotationClockwise`, `setRotationCounterClockwise`,
  `setRotateDegreesCW` and `CCW`, `Image.rotateClockwise` and
  `setTextDirection` are gone. `Container.setRotation` and
  `Stamp.setRotation` turned counterclockwise in v8, so negate their angles.
  `Direction` stays with barcodes.
- One dash pattern name: `Rect` and `TextFrame.setBorderPattern` are
  `setBorderDashPattern`, like `setStrokeDashPattern` and
  `setGridLineDashPattern`. `Container.setScaleFactor` and `setScaleFactorXY`
  are `scaleBy`, like every other shape and image.
- `Page.drawString` keeps three public forms: with a font size, with a fallback
  font, and with a letter spacing; the color and highlight forms are internal
  and `TextLine` is the way to draw colored or highlighted text (Example_32).
- `Image` reads the type of an image from its first bytes: the constructors take
  the stream alone, and `ImageType` is internal.
- A spacing setter that takes points is a gap (`TextFrame.setParagraphGap`)
  and one that takes a multiple is a spacing
  (`TextBlock.setLineSpacing`, `TextColumn.setLineSpacing` and
  `setParagraphSpacing`); the doc comments say which.
- The Java internals are internal: `PDF.append`, `newObj`, `endObj` and
  `getObjNumber` are package-private, `Encryption` moves to `com.pdfjet` with
  the AES classes package-private (`Passwords`, `Permissions` and `UserAccess`
  stay in `com.pdfjet.encryption`), `ColorMap` and `Util` are internal in
  every port, `Barcode.drawOnPageAtLocation` is gone (`setLocation` then
  `drawOn`), `TextUtils` is gone and the examples print their own duration,
  and the font generators live in `util/` with their scripts, outside the
  library. `Util.readLines` is `Content.linesOfTextFile`.
- The font parser `OTF` and the PNG decoder `PNGImage` are internal in the
  four ports: `Font` and `Image` read those files. The ICC profile bytes of
  Go and Swift and the Swift `PDFobj.dict` field are internal too.
- `TextBox` is removed: `TextBlock` draws a wrapped text box, with a fixed
  height, vertical alignment and strikeout added from `TextBox`, and a cell
  holds a `TextBlock` (`Cell.setTextBox` is gone). See "Text".
- `Chart.setXYChart` is removed with its category mode; bar charts are drawn
  with the new `BarChart`. `Chart` axis labels with whole number steps have no
  decimal places. `Chart.setData` and `getData` are gone: a chart is built
  from `addSeries(name)`, which returns a `Series` with the points, the line
  and the marker, and `Point` keeps only its coordinates, its marker and its
  link. `Cell.setMarker` takes the alignment of the marker. See "Charts and
  calendars".
- Constants have types. The Go constant packages are typed, `PathOperator`,
  `PageLayout`, `PageMode`, `ScriptPosition`,
  `ErrorCorrectionLevel`, `Shape` and `StructElem` are enums in Java, C# and
  Swift, and `Align` is gone: every alignment is an `Alignment`. The `Point`
  shape constants are `Shape.CIRCLE` and so on, `Point.setShape`,
  `TextLine.setStructureType` and `Page.addBDC` take the enums, and
  `EmbeddedFile` takes a boolean instead of `Compress` (Go `structelem`
  replaces `structtype`, and the `compress` package is gone).
- Errors are reported. Swift `PDF.complete()` and `PDF.addObjects` throw, Go
  `PDF.Read` and `ReadWithPassword` return an error, and the Go port panics or
  returns an error where it called `log.Fatal` or ignored the error. See
  "Errors".
- The `UserAccess` values are the permission bits of the standard, so code
  that passed raw integers must use them, `/P` is written with its reserved
  bits set, and `Permissions.grant` and `revoke` replace
  `setPermissions(flags, grant)`. They take typed values in the four ports:
  Java `grant(UserAccess...)` and `revoke`, with a `Set<UserAccess>` for
  `getAccess` and `setAccess`, and in Swift a `UserAccess` option set combined
  with `|`, where both took an `int` built from `getValue()`.
- Java: the font name classes and the QR code, PDF417 and Data Matrix classes
  are in `com.pdfjet.fonts`, `com.pdfjet.qrcode`, `com.pdfjet.pdf417` and
  `com.pdfjet.datamatrix`; import them.
- The `double` overloads of the Java and C# setters are removed: every
  coordinate and size is a `float`, so a caller writes `50f` or casts. The
  methods that took only a `double` (`Container` and `Stamp` rotations, C#
  `Cell.SetWidth`, `Table.SetColumnWidth`, `Page.DrawLine`, `DrawRect` and
  `FillRect`) take a `float` too.
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
- `Permissions.grant` and `revoke` replace `setPermissions(flags, grant)`, and
  `Permissions` has the same members in the four ports: `getAccess` is the
  `/P` value without its reserved bits and `getRawValue` is gone, C# has the
  `GetAccess` and `CanPrint` methods instead of properties, Go has one
  constructor from an int and `UserAccess.IsSetIn` like Java and Swift instead
  of `Has`, `Add` and `Remove`. C# and Swift `Encryption.getKey` is internal,
  and Go `GetKey` is removed.

### Reading existing PDFs
- `PDF.merge` adds all the pages of a document that was read after the pages
  of a PDF, which can merge several documents and draw pages of its own
  between them. The pages keep their content, resources, annotations and
  links, with the size, rotation and resources they inherit from their page
  tree; bookmarks, form fields, tagging, named destinations and optional
  content settings are left out. An encrypted PDF encrypts the merged pages. A
  PDF/UA or PDF/A document cannot merge, and `merge` cannot be combined with
  `addObjects`. Example_41 merges three documents after a cover page; tests
  cover the order, the inherited entries, links to merged pages, encryption
  both ways and the refused uses, in the four ports.
- `PDF.merge(objects, pageNumbers)` (Go `MergePages`) merges the listed pages
  of a document that was read, in the order they are listed, so a document is
  split by merging each part of it into a PDF of its own; the objects that
  `read` returned can be merged into any number of PDFs. A link to a page that
  is left out leads nowhere, and a page number the document does not have or
  one listed twice is refused. Example_51 writes each page of a document to a
  PDF of its own and all of its pages in reverse order to one more; tests cover
  the order, the split, links to pages left out and the refused page numbers,
  in the four ports.
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
- The reader keeps the first byte of a stream when it is a line feed. It
  skipped one, meant for the LF of a CRLF after the `stream` keyword, after
  the tokenizer had already consumed the LF, so one encrypted stream in 256,
  whose random IV started with 0x0A, lost its first byte and failed to
  decrypt. All four ports; a test reads such a stream.

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
  column setters change the `TextBlock` of a cell, as in Java, and
  `Cell.setBorders(true)` and `Table.setCellBorders(true)` turn the four
  borders on in every port.
- `Cell.setTextBlock` and `setTextColumn` clear the cell text;
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
- The data files of `Table` and `BigTable` are read as RFC 4180 reads them: a
  quoted field holds its delimiters, and a quoted field with line breaks, as
  spreadsheets export a cell of several lines, goes on over the lines of the
  file to its closing quote, where it was refused. The line breaks are drawn
  as spaces, as are those in the rows a `BigTable` takes from memory, since a
  row is one line tall. A quote that is never closed is refused at the end of
  the file or after 10,000 lines.
- `BigTable.setTableData(header, rows)` takes the rows from memory, the
  results of a query or a list of objects, where they had to be written to a
  file first: an `Iterable<String[]>` in Java, an `IEnumerable<string[]>` in
  C#, a `Sequence` of `[String]` in Swift and an `iter.Seq[[]string]` in Go,
  where it is `SetTableRows`. The rows are gone through twice, once to measure
  and once to draw, and none is kept. The file form reads its first line as the
  header and hands the lines after it to the same code, and a header with fewer
  fields than the table has columns is refused.
- `BigTable.setColumns` takes the indexes of the fields to draw, in the order
  they are drawn, so the columns of a file or a query can be picked and
  reordered without rewriting it; `setNumberOfColumns(n)` draws the first n
  fields, as before. A row without a field for every index is skipped.
- `BigTable.setShadingColor`, `setBorderColor`, `setPadding` and `setFooter`
  set the color of every other row, the color of the lines, the space on each
  side of the text and the footer, which were fixed. `Color.transparent` or
  null leaves the shading or the lines out, and the footer is a text in which
  `{page}` and `{pages}` stand for the page number and the page count, in a
  font of its own, or none with a null or empty text.
- A `Cell` holds one drawable, set with `setDrawable` or with `setImage`,
  `setBarcode`, `setTextBlock` or `setTextColumn`, so the last of them wins;
  before, the four were kept side by side and drawn in a fixed order, so a
  text block was drawn over an image set after it. Any drawable whose location
  is its top left corner can go in a cell, a QR code, an SVG image, a chart or
  a table, measured with `drawOn(null)` and aligned as the text is.
  `getImage`, `getBarcode`, `getTextBlock` and `getTextColumn` return the
  drawable when it is of that type and null otherwise, and a cell measures
  text set after its drawable, which it draws, instead of the drawable. A
  barcode with its text in a cell makes the row tall enough for the descent of
  the text, as Example_08 shows. The Java and C# `Cell` fields `image`,
  `barcode`, `textBlock` and `textColumn` are one `drawable` field, and Swift
  `Drawable` is class-bound (`AnyObject`), so a cell stores it in 16 bytes.
- `Barcode.getHeight` returns the height of the barcode as it is drawn, the
  corner `drawOn` returns less the top: a barcode drawn top to bottom or bottom
  to top is as tall as it is long, where it was given the height of a
  horizontal one; the text under a Code 128 or Code 39 barcode counts with its
  descent; and the text of an EAN-13 or UPC-A barcode counts where it is drawn,
  where the height was the bar height and a line of text in every case.
- The `Table(f1, f2)` constructor, which ignored its fonts, and the
  `WITH_n_HEADER_ROWS` constants are removed; pass the number of header rows.
- `Table.getWidth` of an empty table is 0 in the four ports, where three
  failed on the missing first row. An empty table draws nothing, and
  `drawOn(pdf, pages, pageSize)` adds no page for it, where every port failed
  on the missing first row, as `setTableData` and `autoAdjustColumnWidths` did with
  no rows; a table with more header rows than rows draws the rows it has. A cell keeps its column span, four
  borders, underline and strikeout as fields in the four ports, so the
  Swift flags no longer report borders that `setBorder` turned off, and Go
  has `NewEmptyCell(font)` for the `Cell(font)` of the other ports.

### Text
- `TextColumn` no longer rotates: the constructor that took 0, 90 or 270
  degrees is gone, with the code that laid the lines out sideways, and the
  Go `NewTextColumn` takes no argument. A column that must be rotated goes
  in a `Container` or a `Stamp`, which rotate anything they hold by any
  angle, as Example_35 shows.
- `TextBox` is removed; `TextBlock` is the one wrapped text box. It gains
  what only `TextBox` had: a set height is the height of the block, the lines
  that do not fit are cut and the last line that fits ends with "...", and
  `setVerticalAlignment` aligns the lines to the top, the center or the bottom
  of the block; `setStrikeout` strikes the text out; and the getters
  `getLocation`, `getPadding`, `getBorderWidth`, `getTextColor`,
  `getBorderColor`, `getTextAlignment`, `getVerticalAlignment`,
  `getUnderline` and `getStrikeout`. A `TextBlock` without a height is as
  tall as its text, as before. The per-side borders, the text rotation and the
  line gap in points of `TextBox` are not carried over: `setBorderColor` draws
  the four borders, and `setLineSpacing` takes a multiple. `Cell.setTextBox`
  and `getTextBox` are gone with it; a cell holds a `TextBlock`. The
  highlight colors of a `TextBlock` match the keywords ignoring case, as
  documented: a page looks a word up as written and then in lower case.
- `TextBlock` draws the characters its font lacks in the fallback font, draws
  a text of only line breaks as one empty line, and `getHeight` returns the
  drawn height when the text is taller than the set height.
- `setFont` of `TextLine`, `TextBlock` and `Cell` changes the fallback font
  with the font, unless a different fallback font was set: `TextBlock`
  replaced a fallback font that was set, and `TextLine` and `Cell` kept the
  first font as the fallback. `Color.transparent` leaves the text color of a
  `TextBlock` and a `Cell` unchanged, as it does for a `TextLine`, where it
  made the text white.
- `TextFrame.setParagraphGap` is the space between paragraphs, from the
  bottom of the text of a paragraph to the top of the text of the next, so
  paragraphs never overlap. It was the distance from baseline to baseline, so
  a gap smaller than the line overlapped the text. The default is one empty
  line in the size of the next paragraph in both constructors, where the
  frame made from paragraphs used 24 points, so a heading is not followed by
  an empty line of its own size.
- `TextFrame` and `TextColumn` keep every setting of a line they wrap,
  including its vertical offset and its link. `TextFrame` always finishes
  flowing, and its border is black by default.
- `TextColumn.setTextAlignment` applies to the paragraphs without an alignment
  of their own.
- C# `CompositeTextLine` lays out its lines as Java does.
- `Content.ofTextFile` drops a byte order mark, and the lines of a text file
  come from `Content.linesOfTextFile` (Go `content.LinesOfTextFile`), which
  replaces `Text.readLines` and `Util.readLines`.

### Charts and calendars
- New `BarChart` in the four ports: categories on one axis, the bars of the
  series grouped inside each category, vertical or horizontal, with a value
  axis that always includes 0, a legend under the title, optional value labels
  at the bar ends, and grid, axis and border line settings. `setStacked(true)`
  stacks the series in each category, with the value axis rounded to the sums
  and the value labels inside the segments. `addSeries(name, values, colors)`
  gives each bar its own color, `setSubtitle` writes a subtitle in gray under
  the title, `setValueLabelsInside(true)` writes the values in white inside
  the bars, `setGroupingUsed(true)` groups the digits in thousands, as in
  6,650, `setGridLineColor` colors the grid lines and an axis line width of 0
  hides the axis lines. Example_39 draws a horizontal bar chart of the ten
  longest rivers with these, each bar in its own color, with a color key and a
  note under it, and Example_40 a grouped and a stacked vertical bar chart.
- `Chart` data is a list of `Series`: `chart.addSeries(name)` returns the
  series, with `addPoint(x, y)` for a point with the marker of the series,
  `addPoint(point)` for a point with its own marker and link, `setDrawPath` to
  connect the points, `setStrokeColor`, `setStrokeWidth` and
  `setStrokeDashPattern` for the line, and `setShape` and `setRadius` for the
  marker. A series without a color has the next color of the palette, and a
  point without a stroke color has the color of its series. The legend under
  the title lists the named series with their line or marker,
  `setDrawLegend(false)` hides it, and the text a path series wrote along its
  line is gone. `Point` keeps its coordinates, radius, shape, colors, stroke
  width and URI action; its text, text color, text rotation, alignment, dash
  pattern, path flag and path operator are gone. `Cell.setMarker(point,
  alignment)` places the marker at the left or the right of the cell, where
  the alignment was a property of the point.
- `Chart` draws only XY charts: `setXYChart` and its category mode are removed,
  bar charts are `BarChart`. `Chart` draws with the sizes of its two fonts, as
  `BarChart` does, so `setFontSize` is gone; `setAutoColors` is gone, the palette
  colors a series that has no color; and `slope` and `intercept` are gone,
  Example_09 computes its trend line. Stroke widths are page units and are no longer
  scaled by the plot to chart width ratio, so a path is as wide as it was set.
  The axis labels have the decimal places of the axis step, so an axis with
  whole number steps has whole number labels; the default minimum is 0
  decimal places. The title and axis titles are centered at the size they are
  drawn, and the text of a path series starts at its first point, centered
  across the stroke.
- `Chart` handles negative, empty and flat data and its colors the same way in
  the four ports, and writes the same axis labels whatever the locale.
- `Chart` follows the rules of `BarChart`: a chart or plot area border width
  of 0, the default, hides the border, where it drew the thinnest line; axis
  lines 0.5 wide run along the left and bottom sides of the plot area, and
  `setAxisLineWidth(0)` hides them; a grid line is drawn at every label,
  including the last; and `setSubtitle` and `setGridLineColor` work as in
  `BarChart`. A chart that sets no widths, like the one of Example_09, has
  axis lines instead of a frame.
- `DonutChart` takes a value per slice instead of an angle, so the user no
  longer does the chart's arithmetic, and computes the same percentages in
  every port.
- `CalendarMonth` lays out the calendar as Java does in the four ports.

### PDF, pages and drawing
- A page writes a brush color, a pen color, a pen width or a font only when it
  changes, where every setting wrote its operator again: the colors and the
  width of every table cell and the font of every text line were written
  again and again. What the content has set is saved and restored with the
  graphics state, and a CMYK color or a page read from a document starts it
  over. `fillRect` writes one `re` operator instead of a path of four, with
  the edges where the path put them. `Table` drawing Example_43's data writes
  21.18 MB instead of 25.23 MB, less than iText's 21.6 MB, in 4.5 s instead of
  5.8 s; Example_43 is 2% smaller and 2 to 7% faster in the four ports, and the
  example PDFs render the same, apart from MuPDF anti-aliasing the edges of
  some filled rectangles by a few levels. `BigTable` fills its shaded rows the
  same way, which takes Example_43 from 12.1 to 11.7 MB.
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
- The alternate description and the actual text of `Line`, `Arc`, `Image`,
  `SVGImage`, `CheckBox`, `RadioButton` and `Stamp` default to none, as for
  `Rect`, `TextLine` and the annotations, where they were a single space: in a
  PDF/UA document a drawing without a description writes no `/Alt` and no
  `/ActualText`, and its link is described by its URI. Swift `Page.addBDC`
  takes optional texts, as the other ports take null.
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
- `Stamp` is set up like a `Container`: `setSize` and `addFont` replace
  `withSize` and `withFont`, and `scaleBy` scales a placed stamp around its
  center as `Container.scaleBy` does. Example_35 places a stamp at a quarter
  of its size.
- Box outlines are borders (`TextBlock`, `Cell` and `CheckBox.setBorderColor`
  and `setBorderWidth`, `Table.setCellBorderColor`,
  `TextBlock.setCornerRadius`), and lines have strokes (`Line` and `Path`
  `setStrokeWidth`, `setStrokeColor` and `setStrokeDashPattern`,
  `Line.setLineCapStyle`, `Form.setStrokeWidth`). `Page` keeps pen and brush,
  with `setDefaultPenWidth`.
- An angle is set with `setRotation`, clockwise, a text rotation in degrees
  with `setTextRotation`, and the sweep of an arc with `Arc.setSweep`.
- Scaling and moving: `Arc.scaleBy`, `TextParameters.setLocation`,
  `CompositeTextLine.getLocation` and `getMinMaxY`, `TextLine.getLocation`,
  which replaces `advance`, and `Title.setOffset` sets the offset.
- Alignment: `Cell.setVerticalAlignment`, `Table.setTextAlignmentInColumn`,
  and `Paragraph` and `TextColumn.setTextAlignment`.
- Table data: `Table.setData` is `setTableData`, the name `BigTable` uses.
- Text boxes: a gap is in points and a spacing a multiplier, so `setPadding`,
  `setLineGap`, `setParagraphGap` and `setHighlightColors`;
  `Paragraph.setTextColor`, `TextLine.setDecorationColor` and
  `CheckBox.setCheckmarkColor`.
- Duplicates are removed: `Table.getCellAtRowColumn`, `getRowAtIndex` and
  `getColumnAtIndex`, `Font.getHeight`, `TextLine.getStringWidth`, which is
  `Font.stringWidth`, `Container.addBorder`, the `Cell`
  side border methods, and `TextColumn.addChineseParagraph` and
  `addJapaneseParagraph`, which are one `addCJKParagraph`.
- Misleading names are renamed: `BaseAnnotation.setOpacity`,
  `FileAttachment.setContents`, `DonutChart.setRadii`,
  `Bookmark.getDestinationName`, `Table.autoAdjustColumnWidths`,
  `Cell.setMarker`, `Page.addBDC`,
  `addArcToPath` and `addCircularArcToPath`, `Chart.setDrawHorizontalGridLines`
  and `setDrawVerticalGridLines`, `TextLine.setScriptPosition` with the `ScriptPosition`
  enum instead of `Effect`, `Form.setWidth`, `Path.setClosed`, and Go
  `DrawStringUsingHighlightColors`.
- `TextLine.setDestination(name)` replaces `getDestinationX` and
  `getDestinationY`: `drawOn` adds the destination to the page a font size
  above the baseline, the other end of `setGoToAction`. Example_22 uses it.
- Abbreviations are spelled out: the `Chart` grid line setters are
  `setHorizontalGridLineWidth`, `setVerticalGridLineWidth`,
  `setHorizontalGridLineDashPattern` and `setVerticalGridLineDashPattern`;
  `Shape.H_DASH` and `V_DASH` are `HORIZONTAL_DASH` and `VERTICAL_DASH` (Go
  `shape.HorizontalDash` and `VerticalDash`); and the Go QR code levels are
  `errorcorrectionlevel.L`, `M`, `Q` and `H`, as `ErrorCorrectionLevel.L` in
  the other ports.
- The fixed shapes of `Page` are drawn or filled, as `drawRect` and
  `fillRect` and `drawEllipse` and `fillEllipse` are: `fillCircle` replaces
  `drawCircle(x, y, r, PathOperator)` (Go `DrawCircleUsingPathOperator`), and
  `drawRoundedRect` and `fillRoundedRect` replace `drawRectRoundCorners`.
  `drawPath` keeps its `PathOperator`.
- Go names follow the rules of the Go port: `Font.GetAscent`, `GetDescent`,
  `GetBodyHeight`, `GetUnderlinePosition` and `GetUnderlineThickness` take the
  font size, as Go has the full form only, in place of the `At` forms and
  the forms without arguments; `StringWidthUsingFallbackFont` replaces
  `StringWidthFB`. `CheckBox.drawXMark` (Go `DrawXMark`) replaces
  `CheckBox.xMark` (Go `XMarkCheckBox`), in the four ports.
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
  throws on invalid PNG, BMP, OTF and SVG data.
- The Swift `Barcode` and `QRCode` initializers throw a `PDFjetError`, as Java
  and C# throw, where they stopped the program with `fatalError`: for UPC-A or
  EAN-13 text that is not 11 or 12 digits, a barcode type the class does not
  draw, Code 39 text with a character the code cannot encode, and QR data that
  does not fit the symbol. Java, C# and Go report the Code 39 and barcode type
  errors from `drawOn`; the Swift `Drawable.drawOn` cannot throw, so its
  initializer checks them, with the same messages.
- Swift `BMPImage` opens and closes its stream, as `PNGImage` and `JPGImage`
  do, and Swift `PNGImage.getAlpha` returns `nil` for an image with no alpha
  channel, as Java, C# and Go return null or nil. The hex helper of the ports
  (`Util.toHexString` in Java) is internal in every port.
- The Go port panics, or returns an error where the function returns one,
  where it exited the program with `log.Fatal`, and no longer ignores errors
  writing, compressing or encrypting a PDF or reading an image, font or
  embedded file.
- `Content.ofBinaryFile` and `Content.ofTextFile` report a missing file, and C#
  throws on a truncated font stream.
- The Go port no longer panics on an output file that cannot be created, a
  writer that fails, objects without a root `/Pages`, or a text rendering
  mode outside 0 to 7: the functions return the error. Its append functions
  keep the first error of the writer for `Complete` to return. `Font(pdf,
  coreFont)` rejects a number outside the fourteen core fonts in Java, C# and
  Swift, where the failure came later in `stringWidth`. C# and Swift write
  the out-of-range colour warning to standard error, as Java's logger and Go
  do, instead of the program's output.
- The number writer no longer writes `NaN` or `Infinity`, which viewers
  reject, and a pie chart, a `DonutChart` with an inner radius of 0, no
  longer writes NaN for its center. The XMP metadata leaves out the
  characters XML does not allow, which made it unreadable. An image, stamp or
  container scaled to 0 draws nothing instead of a singular matrix. Stamp
  text adds its font to the stamp's resources, where a font left out of
  `addFont` was a missing resource, and `addFont` adds a font once.

### Port parity
- The default configuration of the optional content lists the hidden groups
  in an `/OFF` array, for the viewers that read the configuration and not
  the usage of each group, so a layer with `setVisible(false)` starts hidden
  everywhere. Tests cover the marked content, the page properties and the
  one object of a group drawn on several pages, and the `/Dest` of a GoTo
  link, in the four ports.
- A second audit of the four ports fixed the drift that could crash or
  misplace something: Java `BarChart` drew no legend swatch for a named
  series with a color per bar and threw instead; Swift `Page(pdf, pageObj)`
  read the page size before stripping comments; `Title` tested the prefix
  object instead of its text; a polygon annotation without vertices crashed
  when rotated; Swift `Image` crashed on a missing file, rounded its link
  rectangle and passed an alpha channel for gray and 16-bit images read from
  an existing PDF; `Cell.setBorder` let stray bits reach the underline and
  strikeout flags in Java and C#; `CompositeTextLine.getLocation` handed out
  its own array in Java and C#; Go `OptionalContentGroup.Clear` aliased the
  old list; `BigTable` aligned numbers with each language's float parser
  where `Table.isNumber` now decides in all four ports, and Go `BigTable`
  takes an empty delimiter and long lines as `Table` does.
- The defaults, the copies that colour getters return and setters keep, and
  the errors are the same in the four ports, and the C#, Go and Swift examples
  read like the Java ones.
- The remaining drift of the second audit is gone: `Form` copies its list of
  fields and `OptionalContentGroup.getComponents` returns a copy in the
  four ports; the saved graphics state stores the brush and the pen in the
  same order everywhere; C# `setPenColor(int)` and `setBrushColor(int)` call
  the array overloads; Swift `Title.setPrefix` may be called without using
  the result; Go `Form.drawOn` names a nil page, and
  Go has `NewPDFReader()` for the reading-only `PDF()` constructor of the
  other ports. Go keeps `AddBDC` with the language, `NewParagraph()` and an
  array-only `Cell.SetBorderColorRGB` as documented conventions.

### Build, checks and examples
- Example_41 was a copy of Example_03 and is removed; the merge example that was
  Example_51 is Example_41, so the examples were numbered 1 to 50. Example_51
  is new: it splits a document.
- The examples draw with the embedded IBM Plex fonts, which a PDF/UA document
  needs, instead of the core fonts. The core fonts stay in Example_04, whose
  CJK fonts are not embedded either, in Example_05, which shows their
  kerning, and in Example_50, which adds one as a resource of a page of an
  existing PDF.
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
- The class comments of `TextBlock`, `TextColumn`, `TextFrame`, `Container`
  and `Stamp` say what each is for and which of the others to use instead,
  and the README has a "Which class to use" table for the text and grouping
  classes.
- The C# API reference is built by DocFX and published under `dotnet/`, next
  to the Java, Go and Swift references, and the Swift reference is published
  again.
- `generate-documentation.sh` stops on the first error and runs doc2go with
  `go run`, so it needs no install.
- The README documents the port differences, `PageSize` and right to left
  text.
- The Go doc comments list their parameters the Go way instead of with
  Javadoc `@param` and `@return` tags, `rotateAroundCenter` lives with
  `Container` in Go as in the other ports, and the Java `DonutChart` and
  `Slice` start with the license like every other file.

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
  Other input is rejected when the barcode is created: Java, C# and Swift
  throw and Go panics.
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
  the Swift `Example_03` and `Example_37` now call `setTextColor`.
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
  as they were in Java.
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
- `TextLine` measures its width, its bottom-right corner and its link rectangle
  at the font size it draws with (all four ports).
- The two `Table` wrapping loops measure the same way; Go broke long tokens one
  character early.
- `CompositeTextLine` applies the subscript and superscript size factors (Java,
  Swift), and C# positions the two effects with the constants it declares.

### Port parity
- Go gets a `border` package for the borders of a `Cell`.
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
