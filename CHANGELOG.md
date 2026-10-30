# Changelog

All notable changes to PDFjet are documented in this file. PDFjet ships four
parallel, hand-synchronized ports — Java (`com/pdfjet`), C# (`net/pdfjet`), Go
(`src`), and Swift (`Sources/PDFjet`) — kept behaviorally identical across
languages.

This is the first entry in this file; earlier releases were not tracked here.

## v9.1.7 — not released yet

The next release, with PDFjet Pro's 9.1.7 (TODO.md).

### Added
- Go: `XMLNode.Content()` in the package xmlparser returns the text and the
  child elements of a node in the order of the document, a string for each
  piece of text and an `*XMLNode` for each element. The SVG reader draws
  text with it; Java, C# and Swift have the same, inside the library.
- SimpleBarChart, all four ports, three additions, each off unless set, so
  that a chart drawn before draws the same (checked against 9.1.5, byte for
  byte but for the date and the document ID):
  - `setHighlightLowest()` and `setHighlightHighest()`: the chart
    highlights the bar of the lowest or the highest value itself, the first
    of them on a tie, values that are NaN or infinite left out; bars added
    later count. `setHighlight(index)` and these take each other's place.
  - `setReferenceLine(value, label)`: a thin dashed line across the bars at
    a value, a target or an average, labeled under them ("Average 1,234"),
    the bars scaled so that it fits; it leaves out the rows where it would
    cross a bar's value, and the PDF/UA description ends with it.
  - `setRoundedBars(true)`: bars with slightly rounded ends.
  Example_20's chart by model year uses all three.
- SVGImage, all four ports, draws the charts of D3 and the like as a
  browser does:
  - `text` and `tspan` elements: x, y, dx and dy (in em too), font-size,
    font-family, font-weight, font-style and the `font` shorthand,
    text-anchor for each chunk, dominant-baseline, and a stroke drawn under
    the text with `paint-order: stroke`, the white halo of a label. The
    text is drawn in the fonts PDFjet carries (below), embedded, so that a
    PDF/A or PDF/UA document needs nothing set: IBM Plex Sans for
    sans-serif, Helvetica, Arial and the like, IBM Plex Serif for serif,
    Times and the like, Cascadia Mono for monospace, Courier and the like,
    in the weight and the style the text asks for; monospace text of weight
    400 is drawn in Cascadia Mono SemiLight, whose strokes match IBM Plex
    Sans Regular. A font of one's own, for Chinese for instance, is set
    with `setFont()` and `setBoldFont()`. A stroke drawn under text is a
    second copy of its characters in the content, which copying the text
    from the PDF gives twice.
- The fonts PDFjet carries, all four ports, for every language it draws,
  so that it works out of the box: IBM Plex Sans and Serif (Thin to Bold,
  and Text) and Cascadia Mono (Light to Bold), each upright and italic; IBM Plex Sans Arabic, Hebrew and Thai (Thin to Bold); IBM
  Plex Math; and IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold and
  Bold, as .otf. 72 faces, under the SIL Open Font License, each family
  with its license in `resources/fonts` (THIRD-PARTIES.TXT). A font path,
  such as the constant `IBMPlexSans.Regular`, is read from the file of the
  path, else from the folder `resources/fonts`, else from the library, with
  the same API in every case:
  - Java: the jar of Maven Central carries the fonts, about 46 MB; the
    `PDFjet.jar` of the release packages on GitHub, and the one built from
    the repository, does not, 0.7 MB, its package having `resources/fonts`.
  - .NET: the package of NuGet carries them in `PDFjet.dll`, about 67 MB;
    the `PDFjet.dll` of the release packages on GitHub does not, 0.9 MB.
  - Go: a program built with PDFjet carries the fonts of every script but
    Chinese, Japanese and Korean, about 20 MB in all; `import _
    "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"` adds those, 48
    MB more.
  - Swift: the fonts come with the product `PDFjetFonts`, about 63 MB in
    an app that adds it; the library alone has none.
  New: `IBMPlexSerif` and `CascadiaMono` constants, and `Font(objects,
  path)`, a font added to an existing PDF by its path. A font PDFjet does
  not carry throws, saying so. Every face carried has a constant, and every
  face for Latin text has all of Greek and of the Cyrillic of Russian,
  Ukrainian, Belarusian, Bulgarian, Serbian and Macedonian, which tests
  check.
  - `use` elements, of symbols too, and `marker-start`, `marker-mid` and
    `marker-end`, the arrowheads of a graph. A file that draws more than a
    million elements again through them throws.
  - `stroke-dasharray` and `stroke-dashoffset`, and `clip-path`.
  - What is drawn outside the size of the image is cut off.
  - A gradient is drawn in one color, the mean of its stops.
- SimpleScatterChart, all four ports: `addSeries(name)`. The points added
  after it are its points, drawn as the outlines of its shape in its color,
  circle, square, triangle, star, diamond and cross in turn, with a legend
  under the title; a highlighted point of a series is its shape filled.
  Points added before the first series are gray dots, as before.

### Deprecated
- `NotoSans`, `IBMPlexMono` (no Greek) and `JetBrainsMono` (no Ѐ Ѝ ѐ ѝ),
  and the ExtraLight and Black weights and, of IBM Plex Sans JP, KR, SC and
  TC, Thin, Light, Text and Medium: PDFjet does not carry them. Their
  constants read the font file when it is there; the release packages
  before 9.1.7 have them.
- `SourceSerif4`, all four ports: PDFjet does not carry Source Serif 4 from
  9.1.7, and carries IBM Plex Serif in its place. Its constants read the
  font file when it is there; the release packages before 9.1.7 have it.
- The CJK constants Regular, SemiBold and Bold name the .otf of the font,
  where they named the .ttf; the same font, with smaller subsets.

### Changed
- PDFjet no longer carries Source Serif 4: IBM Plex Serif, which it carried
  already, is its serif font, so that the fonts it carries are the IBM Plex
  families and Cascadia Mono, 72 faces. The fonts are 2.7 MB smaller: the
  `PDFjet.dll` of NuGet, a Go program and the Swift product `PDFjetFonts`
  by 2.7 MB, the jar of Maven Central by about 1 MB. The examples (07, 10,
  16, 17), the snippets of the booklet and the text benchmark use IBM Plex
  Serif; Example_17 sets the novel on 694 pages kerned, 696 not. Like the
  ExtraLight faces above, `SourceSerif4` is deprecated, not removed.
- Behavior change: a Source Serif 4 constant finds the font only if the
  program supplies its file, in the folder `resources/fonts` or at its path;
  PDFjet no longer reads it from the library. Example:
  `pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)` panics in Go, and
  `new Font(pdf, SourceSerif4.Regular)` in Java and C# and `try Font(pdf,
  SourceSerif4.Regular)` in Swift throw, saying PDFjet does not carry the
  font, where 9.1.5 read it from the folder `fonts` of its package; use
  `IBMPlexSerif.Regular` instead.
- Behavior change: BarChart and DonutChart draw their series and slices in
  the Tableau 10 palette, the colors of D3's charts: blue #4E79A7, orange
  #F28E2C, red #E15759, teal #76B7B2, green #59A14F, yellow #EDC949, purple
  #AF7AA1, pink #FF9DA7, brown #9C755F and gray #BAB0AB. A chart that sets
  no colors looks different from 9.1.5: its first series was navy #1F3A5F,
  and is blue.
- Bars and slices label their values in white or ink, whichever contrasts
  more, as before, and in a near black, #111111, where neither contrasts
  4.5:1, as WCAG asks: on the red, the purple and the brown of the palette,
  and on a color as pure red.
- Behavior change: in SVGImage, a fill or a stroke of `url(#id)` that names
  no gradient, with no color after it, is none, as a browser draws it; it
  was the inherited color, black unless set. Example: `fill="url(#missing)"`
  drew a black shape and now draws nothing.
- Swift: less memory for every document with fonts. The font program and
  the ToUnicode map of each font were compressed with Deflate tables of
  their own, about 450 KB a stream, made again for each: 1.8 MB for a
  document in two fonts. They now use the tables the PDF keeps for the
  content of its pages, one stream after another. And
  `Content.getFromStream` keeps the pieces of a stream as they are read and
  joins them once, instead of an array copied each time it grew: 0.8 MB
  less for a font of the size of Noto Sans. An invoice of one page in IBM
  Plex Sans allocates 4.0 MB instead of 5.9, the same in Noto Sans 6.9
  instead of 10.3. The PDFs are the same, byte for byte, but for their date
  and document ID. The other three ports compress with the Deflate of
  their platform, and were not changed.
- The repository needs nothing fetched to build, test and run the examples.
  The fonts PDFjet carries are in `resources/fonts`, read from the library,
  and the data files the examples and the tests read, those of the
  repository pdfjet-data, are in `resources/data`: the examples read
  `resources/data/the-idiot.txt` where they read `data/the-idiot.txt`.
  `get-fonts-and-data.sh`, `get-fonts-and-data.cmd` and
  `fonts-and-data.txt` are gone, and the scripts no longer call them.
  `resources/data` has a `go.mod` of its own, so that the Go module, which
  `go get` downloads, leaves the data files out. The Java and .NET zips
  have them in `resources/data` too.
- Example_24, the four ports, draws the 10-page sample,
  `resources/data/Electric_Vehicle_Population_10_Pages.csv`, 12 pages, by
  default, and the CSV file given as its first argument, such as the full
  Electric Vehicle Population Data, 2,546 pages, for performance testing.
  The commented-out line that chose the sample is gone.
- Behavior change: Example_24 run with no argument draws 12 pages, where it
  drew 2,546. Example: `go run ./src/examples/example24
  path/to/Electric_Vehicle_Population_Data.csv` draws the 2,546 pages as
  before.
- Example_24's sample is now a PDF/UA document, its table tagged for screen
  readers. A file given as the argument is drawn as before, without PDF/UA,
  so that the benchmark stays comparable with earlier releases.
- Example_25, the four ports, reads its counts from
  `resources/data/Electric_Vehicles_by_Model_Year.csv`, the vehicles of the
  Washington State Electric Vehicle Population Data by model year and by
  kind, written once from the full data by `util/make-ev-by-model-year.py`,
  where they were written into the example. The chart is the same, with the
  same numbers; its text names the summary, not the full data file.
- The full data file, `resources/data/Electric_Vehicle_Population_Data.csv`,
  30 MB, is no longer in the repository or the packages.
  `benchmarks/run.sh table` reads it from
  `../pdfjet-test/Electric_Vehicle_Population_Data.csv`, or from the path in
  `EV_CSV`, and says what to do when it is missing.

### Fixed
- The four ports: the `/F` of an embedded file's file specification is a
  string of bytes, as ISO 32000-2, 7.11.3, Table 43, asks of a file
  specification string; it was the name in UTF-16, as `/UF` is. Each
  character from space to tilde is written as it is, and each other one, a
  Unicode code point, as an underscore: `résumé.txt` is `r_sum_.txt` in
  `/F`. `/UF` keeps the full name in UTF-16, the one readers of PDF 1.7 and
  later use, and the name tree of `/EmbeddedFiles` is the same. Example:
  for `notes.txt`, `/F` is `<6e6f7465732e747874>`, where it was
  `<feff006e006f007400650073002e007400780074>`.
- C#, on .NET 10: a stream of no bytes, such as the content of an empty
  page, is the zlib stream of no bytes, 8 bytes, as on .NET 8. .NET 10's
  ZLibStream writes nothing for no bytes, so such a `/FlateDecode` stream
  had length 0, which is no zlib stream. On .NET 8 nothing changes.
- The four ports: the `/F` of an embedded file follows ISO 32000-2, 7.11.2,
  on file specification strings. The name is one component, the file name:
  a solidus in it is preceded by a reverse solidus, which tells it from the
  separator of components, as 7.11.2 asks, and a reverse solidus in it is an
  underscore, as a reader removes each reverse solidus and 7.11.2 gives no
  way to write one that stays. `/UF` keeps the full name.
- Behavior change: a solidus or a reverse solidus in the name of an
  embedded file is written differently in `/F`. Example: for `in/out.txt`,
  `/F` is the bytes `in\/out.txt`, where it was `in/out.txt`, a folder `in`
  and a file `out.txt` to a reader; for `C:\temp\a.txt` it is
  `C:_temp_a.txt`, where it was `C:\temp\a.txt`, which a reader reads as
  `C:tempa.txt`.
- Markup, the four ports: a hard line break at the very end of a text, two
  spaces and a line break, is no break, as in CommonMark, and the spaces
  are no longer in the last text line. `paragraphs` strips the whitespace
  at the end of each paragraph, as CommonMark strips a paragraph's final
  spaces and as Markdown did already.
- Behavior change: `paragraph("0  \n")` is the one text line `0`, with no
  line break after it, where it was `0  ` and a line break, and
  `paragraphs("0  \n")` is `0`, where it was `0  `. Example:
  `paragraphs("Total \n\nNext")` gives the text lines `Total` and `Next`,
  where the first was `Total `.

## v9.1.5 — 2026-10-28

There is no 9.1.3: what was done for it is in 9.1.5 (the owner, 26 October
2026).

### Added
- All four ports: kerning of TrueType and OpenType fonts, with
  `Font.SetKernPairs(true)`, as the 14 standard fonts kerned already. The
  pairs come from the kern feature of the font's GPOS table, of each
  script's default language (pairs of glyphs and pairs of classes), read
  once, under a lock, when the kerning is turned on, so that measuring text
  does not change the font. The kern table of an older TrueType font with
  no GPOS table is not read, and such a font draws its text unkerned. Text
  is measured as it is drawn: StringWidth, GetFitChars, and so the
  wrapping, alignment and justification of TextLine, TextBlock, TextColumn,
  TextFrame and the table cells, count the same pairs. Marks and right to
  left text are not kerned. Off by default, and a document that does not
  ask for it is as fast as before and draws the same PDF. In IBM Plex Sans
  at 10 points, "AVATAR To Wally" is 75.14 points wide kerned, 78.16 not;
  the four ports measure and draw it the same. Kerned, 500 pages of Latin,
  Greek and Cyrillic text in IBM Plex Sans take 96 ms in Go (57 not
  kerned), 118 in Java (69), 103 in C# (63) and 155 in Swift (98), and the
  document is 11 to 17% larger, for the moves written between the glyphs.

- All four ports: `setModuleColor(color)` on Barcode, the linear codes, and
  on PDF417, as QRCode and DataMatrix have it (Go `SetModuleColor`): the
  color of the bars, and of a linear code's text under them, black unless
  set. The documentation of the four asks for a dark color on a light
  background, as most scanners read with red light, in which red, orange
  and yellow bars look white.

- Markdown and Markup, the four ports, four small additions:
  - The headings of a Markdown text are the bookmarks of the document, the
    outline a reader shows, in a document that is not tagged too; a tagged
    one had them already. A document with bookmarks of its own keeps them
    alone, as before.
  - Hard line breaks, as GitHub's Markdown has them: a line that ends in two
    spaces or in a backslash, and `<br>`, `<br/>` or `<br />`, end the line
    there, for an address, a signature or a verse. TextFrame draws the text
    after a break on the next row, the row before it not justified, and the
    paragraph is still one paragraph, and one P in a PDF/UA document.
  - `<br>` in a Markdown table cell breaks its line, the column as wide as
    its widest line.
  - The entities `&amp; &lt; &gt; &quot; &apos; &nbsp; &copy; &reg; &trade;
    &mdash; &ndash; &hellip;`, the curly quotes, `&laquo; &raquo; &bull;
    &middot; &deg; &times; &divide; &plusmn; &sect; &para; &euro; &pound;
    &yen; &cent; &shy;`, and the numbers `&#8212;` and `&#x2014;`, are the
    characters they name; in code they are the text they are, and any other
    `&` is text, as it was.
  Behavior change: a Markdown text in a document that is not tagged gives
  the document bookmarks of its headings; a Markup or Markdown line ending in
  two spaces, a backslash or a `<br>`, or a text with `&amp;` and the other
  entities above, draws differently: "Fish &amp; Chips" was drawn as it is
  written, and is now "Fish & Chips".

- The charts, the four ports, the last items of their plan:
  - `BarChart.addLineSeries(name, values)`, and with a color (Go
    `AddLineSeries`, `AddLineSeriesWithColor`): a series drawn as a line over
    the bars, in ink, a dot at each category, broken where a value is NaN:
    a target, the average, last year. It shares the value axis with the
    bars, is not stacked, and is in the legend, as a line with a dot, and in
    the description. Example_27's third page draws the units of 2026 under
    the target of each month.
  - `DonutChart.setLegend(true)`: a legend under the circle, a swatch, the
    label and the percentage of each slice, in rows as wide as the circle,
    in place of the leader lines and the labels beside it, for many slices
    or small ones. `DonutChart.setCenterText(text)`: the text in the hole of
    a donut, the total, in the title's font, as large as one and a half
    labels or as fits, and in the description. Example_28's second page
    draws eight kinds of fruit with both. The palette's eight colors repeat
    from a ninth slice on, as the documentation of `DonutChart.add(name,
    value)` says.
  - `SimpleLineChart.setValueLabels(true)`: each value written small by its
    point, above a peak, under a valley, before a point on a rise and after
    one on a fall, where the line leaves it room; the last of a line is in
    its end label. `SimpleLineChart.setAreaFilled(true)`: the area under each
    line, down to 0 or the bottom of the plot, in a tint of the line's
    color, a quarter of it over white, which makes an area chart. Example_25's
    second page draws the plug-in hybrids with both.

- All four ports: `TextFrame.getTextBottom()` (Go `GetTextBottom`), the y
  coordinate of the bottom of the text drawn by the last `drawOn`, or
  measured by it with no page: where what comes under the text goes.
  `drawOn` returns the bottom of the frame when the frame has a height, and
  code placing a photo under the text by it put the photo too low; the text
  could only be found by the last paragraph's `getY2()`. `drawOn` returns
  what it did.

- All four ports: `TextFlow`, which flows headings, paragraphs and images
  onto as many pages as they need, one under the other between the margins:
  a report, a manual, a long letter. Paragraphs break across pages between
  their lines, as a TextFrame breaks them; an image goes whole, to the top
  of the next page when it does not fit, and is made as large as the text
  and the page at most, at the left, in the center or at the right; a
  paragraph of `StructElem.CAPTION` added after an image goes with it; a
  heading, a paragraph of `StructElem.H1` to `H6`, goes to the next page
  with the line after it rather than end a page; `addSpace` puts space
  between two things, and a space at the top of a page is left out. The
  pages are made detached, for footers, from a page the caller has begun or
  not. It is a class of its own over TextFrame, Image and
  `getTextBottom()`, which are unchanged, and the order things are added in
  is the reading order of a PDF/UA document. A report of 4 sections, 120
  paragraphs and 4 photos is about 20 lines of code with it, where placing
  the frames and the photos by hand took 53.
- All four ports: `SimpleBarChart`, a small chart of horizontal bars that
  compares a few values: a name at the left of each bar and its value at
  its end, no axis, grid or legend, on a light panel with a title and a
  subtitle at the left; one bar can be highlighted, navy with its name and
  value bold, the others gray. Each bar is its share of the largest value,
  and the height follows from the fonts and the bars. In a PDF/UA document
  it is one figure, described from its title and values unless a
  description is set. Meant to stand beside a table of the same numbers;
  BarChart stays for grouped, stacked and vertical bars with an axis.
- Example_20 is now a page of results from the electric vehicles file: two
  SimpleBarCharts, the five makes and the model years from 2019 to 2023, a
  table of the same numbers under them, and a table of one car with its
  photo and its VIN as a Code 128 barcode in their cells. It was the first
  10 pages of the file as a table, which Example_24 draws whole.
- All four ports: `SimpleScatterChart`, a chart of points on two axes in the
  look of SimpleBarChart: the points as small gray dots, a little
  transparent so that a cluster shows darker (opaque in PDF/A-1), named
  points navy and labeled beside them, the least squares trend line if
  asked, labeled at its end, a light horizontal grid, the axes from the data
  rounded to steps or set by hand. In PDF/UA it is one figure, described
  from its data. Like SimpleBarChart it needs no other chart: the helpers
  the charts share moved from Chart and BarChart into an internal class.
  A named point can be a link, `add(name, x, y, uri)` (Go `AddLinked`): its
  dot and its name are the link, and in PDF/UA it is a figure of its own,
  described by its name, in the Link that holds its annotation, as Chart's
  linked points are.
- Example_26 is now a SimpleScatterChart of the life expectancy and the
  children per woman of 217 countries and economies in 2023, from the
  World Bank's World Development Indicators (CC BY 4.0), seven of them
  named, each a link to its page of the World Bank's data, with a table of
  the named ones. It was a Chart of cell phones and
  internet use by country from a file of about 2007, which Example_23 still
  draws as a table.
- All four ports: `SimpleLineChart`, a chart of a few lines over the same
  categories, the years or the months, in the look of SimpleBarChart and
  SimpleScatterChart: each line labeled at its right end with its name and
  its last value, so that it needs no legend, the labels moved apart where
  they would meet; one line highlighted in navy, its label bold, the others
  gray; dots at the values if asked; a value that is NaN leaves a gap in its
  line. In PDF/UA it is one figure, described from its data: each line's
  first and last values and its highest. Like the other two it needs no
  other chart. Swift takes the categories as an array too, as BarChart does.
- Example_25 is now a SimpleLineChart of the electric vehicles of the data
  file by model year, battery electric highlighted, with a dot at each year.
  It was a Chart with a Series for each line.
- Example_22 is now a guide to four countries and their regions, laid out
  by TextFlow: headings, paragraphs and maps with captions on 4 pages, in a
  PDF/UA document. It was a table of text columns in English and Greek,
  which Example_21 and the TextColumn examples show the parts of.

- All four ports: an `Image` made from the bytes of a PNG, JPEG or BMP
  file already in memory: `new Image(pdf, bytes)` (Go
  `NewImageFromBytes(pdf, bytes)`), without the copy a stream of them
  needs. The PDF is the same as from a stream.

- All four ports: `BigTable.setVerticalLines(false)` (Go and C#
  `SetVerticalLines`) leaves out the vertical lines between the columns and
  at the sides of the table, for a lighter look with the lines between the
  rows alone. The lines are drawn unless it is set, so a BigTable draws as
  before.

- All four ports: `PDF.setCompression(false)` (Go and C# `SetCompression`)
  writes the content of the pages uncompressed, larger and readable in a text
  editor, to see what a page draws; encrypted documents too. The fonts,
  images and embedded files are compressed either way. The content is
  compressed unless it is set, so a PDF is written as before.

- Markdown, the four ports: `setFullWidthTables(true)` (Go and C#
  `SetFullWidthTables`) makes every table as wide as the text, its columns
  widened by the share of the text each holds. Tables are as wide as their
  text unless it is set, as before.

- BigTable, the four ports: `setVerticalPadding` (Go and C#
  `SetVerticalPadding`), room above and below the text of every row, the
  header's too; `setHeaderColor`, a background for the header row of its own,
  where it was shaded as the first of every other row; and
  `setFooterFontSize`, a footer smaller than the table in the same font,
  where an 8 pt footer under a 9 pt table needed the font loaded again; and
  `setFooterOffset`, the distance from the bottom of the page to the
  footer's baseline, so that it can sit at the bottom margin, where it was
  just above the edge. A table is drawn as before unless they are set. The 12-page statement of the
  fair test, drawn as SPEC.md lays it out with 0.75 pt of vertical padding, a
  gray header and an 8 pt footer, takes 12 pages, as Table's does, in 5.6 ms
  to Table's 7.7.

### Changed
- Example_23, the four ports: its table of PDF/A-1a is the World Bank's life
  expectancy and children per woman of 2023, 217 countries and economies
  (World Development Indicators, CC BY 4.0, credited in the footer), the
  file Example_26 draws, in place of a file of world communications of the
  mid-2000s. Its markers and row colors are found by country and by value,
  not by row number, and it is four pages, the table in the middle of each.
- BarChart, the four ports: the look of the simple charts. The title and
  the subtitle at the left, in ink and in a muted gray, and the legend under
  them at the left; light gray solid grid lines along the value axis; one
  axis line, in mid gray at the base of the bars, which is the zero line of
  negative values; the category and axis labels and the axis titles gray; a
  default palette of navy, steel blue, amber, sage, gray, brick, slate and
  ochre, whose colors differ in lightness for grayscale and for readers who
  do not tell red from green; the value labels on a bar white or ink,
  whichever contrasts more with it; digits grouped in thousands. A stacked chart with value labels
  writes the total of each stack above it. Example_27 draws its sales with
  the default colors, and the ten longest rivers with their data in a table
  under the chart.
  Behavior change: a BarChart on its defaults looks as above, where its
  title was centered, its grid lines black and dotted, an axis line along its
  labels, its colors blue, red and green, and its numbers ungrouped (8000,
  now 8,000); a chart that sets its colors, its grid or its grouping keeps
  them.
- DonutChart, the four ports: the look of the simple charts. A title and a
  subtitle, `setTitle` and `setSubtitle`, at the top left, in ink and in a
  muted gray, with the circle under them; a slice added with
  `add(name, value)` takes the next color of BarChart's palette, and one
  added with `add(name, value, color)` its color; thin white lines between the slices; the leader lines
  thin and gray, the labels in ink, and each percentage white or ink,
  whichever contrasts more with its slice. The room above the circle is
  what a label at its top needs, so that it clears the subtitle. The text
  for screen readers starts with the title.
  Example_28 draws its fruit with a title and the default colors.
  Behavior change: the leader lines are 0.75 pt and gray, where they were
  1 pt and black; the labels are ink, not black; a percentage on a light
  slice is ink, where it was always white; and white lines separate the
  slices. A chart without a title is where it was, and slices keep the
  colors they are given.
- Markdown, the four ports: a table that fits on a page is not split. It
  starts the next page when it does not fit in what is left of this one,
  and a paragraph that ends in a colon right before it, such as "The size of
  the PDFs, KB:", goes to the next page with it. A table longer than a page
  starts where it is and goes on onto the next pages, its header row on
  each, as before.
  Behavior change: a document whose short table fell across a page break
  has it on the next page whole, and the pages after it break where the
  table now ends.
- All four ports: a TrueType font is embedded as a smaller subset. The
  subset leaves out the font's `cmap` and `name` tables: it is embedded as
  a CIDFontType2 with an Identity CIDToGIDMap, whose glyphs the PDF names by
  their numbers, and the two were nearly a third of a subset of IBM Plex
  Sans, more to write and compress for every document. A one-page letter in
  two IBM Plex Sans fonts is 15.5 KB, where it was 22.2, and is made in 0.58
  ms in Java, where it took 0.82, three quarters of which went to
  compressing the two fonts.
  Behavior change: the bytes of a PDF with an embedded TrueType font differ
  from those of 9.1.1, and the font program in it has no `cmap` or `name`
  table; a program that took the font back out of a PDF to use as a font
  file of its own would find no `cmap`. How the PDF is drawn and its text
  extracted are the same, by its ToUnicode map.
- Java: everything is deflated at level 5 of zlib instead of the default 6:
  the content of the pages, the fonts, the images and the embedded files.
  The documents of the fair test are 4 to 25% faster to make, a 12-page
  statement 7.7 ms to 5.8, for PDFs 0.1 to 0.4% larger.
  Behavior change: the bytes of a PDF differ from those of 9.1.1, as the
  streams are compressed at another level; its content and how it is drawn
  are the same. The 12-page statement is 70,417 bytes, where it was 70,343.
  Go keeps its default level, as its own encoder gained 3% and grew 3% at
  5; C# has no level 5 (.NET offers its Optimal, level 6, or Fastest, level
  1); Swift has its own encoder.
- All four ports: a JPEG is read faster. Its end-of-image marker, which
  PDFjet looks for to tell a whole photo from one cut short, is nearly
  always its last two bytes, and these are looked at first, before the
  rest of the file is scanned; the PDF is the same. Java, C# and Swift
  gain the most; Go's search was quick already. Swift no longer copies
  a JPEG into a stream it does not read. The 18-page report of the fair
  test, with four photos of 3.3 MB, takes 6.9 ms in Java (8.2 before) and
  allocates 17.3 MB (20.7) with the new constructor.
- Swift: text other than printable ASCII is drawn faster: a 500-page text
  in Latin, Greek and Cyrillic takes 92 ms, where 9.0.9 and 9.1.1 took 113.
  Only the glyphs a font draws for more than one character, as one glyph for
  two kinds of space, have the character drawn with them recorded for the
  ToUnicode map; every character was, since 9.0.7. The glyphs drawn are
  marked used together, with no check of each, and a character of Greek,
  Cyrillic, Hebrew, the CJK ideographs or Hangul is known at once to be one
  that is drawn, in the four ports, where it was compared with some twenty
  that are not. The PDF is the same.

- Swift: the content of the pages is compressed in memory the PDF keeps,
  made once: 400 KB of tables made for every page came from the system and
  went back to it. Example_24, a table of 2,546 pages, takes 2,284 ms,
  where it took 2,614; the PDF is the same.
- Java: PDFs made in several threads at once are compressed at once: each
  thread compresses its font programs, ToUnicode maps and PNG alpha with
  a Deflater of its own, where all of them waited for one, synchronized.
  800 one-page documents with an embedded font in 4 threads take 274 ms,
  where they took 626; one thread is as fast as before.

- Markdown, the four ports: a table cell that starts with a check mark,
  U+2713, is drawn in the check color, the green of a task that is done
  unless `setCheckColor` (Go and C# `SetCheckColor`) sets another, as a
  table of features marks what each has: `| Tables | ✓ |`, or
  `| SVG | ✓ with a module |`. The header row and the other cells are as
  before.
  Behavior change: a table cell of a document that already starts with a
  check mark was black and is green; a status table's `| ✓ shipped |` is
  drawn in 0x1A7F37.

### Removed
- `Chart` and `Series`, in the four ports: SimpleLineChart draws lines over
  time or over categories (Example_25), and SimpleScatterChart points on
  two axes (Example_26), in the look of the other charts, each described
  for screen readers from its data. The charts were new, and the owner
  chose to remove the old ones in this release rather than keep them
  deprecated (26 October 2026). Point stays: Cell, Path, Shape and Page use
  it. The booklet's line chart is a SimpleLineChart.
- `Slice` and `DonutChart.addSlice`, in the four ports (Go `NewSlice`,
  `NewSliceInPalette`): a slice is added with `DonutChart.add(name, value)`,
  in the next color of the palette, or `add(name, value, color)` (Go `Add`
  and `AddWithColor`), the name
  first as in `SimpleBarChart.add(name, value)`. `new Slice(25, Color.red,
  "Apples")` is written `add("Apples", 25, Color.red)`.
- BarChart, in the four ports: `setValueAxisMinMax(min, max, gridLines)`,
  for `setValueAxisRange(min, max)`, named as the line and scatter charts'
  `setYAxisRange`, whose grid lines are on a round step; and
  `setMinimumFractionDigits` and `setMaximumFractionDigits`, for
  `setFractionDigits(digits)`, as SimpleBarChart's. Its style knobs
  `setChartBorderWidth`, `setInnerBorderWidth`, `setGridLineDashPattern`,
  `setGridLineWidth` and `setAxisLineWidth` are removed: a BarChart has the
  look of the simple charts, thin solid grid lines, the axis line at the
  base of the bars and no border; `setGridLines` and `setGridLineColor`
  stay. And `setDrawGridLines`, `setDrawValueLabels` and `setDrawLegend`,
  for `setGridLines`, `setValueLabels` and `setLegend`, the names the other
  charts use.
  Behavior change: a program that calls one of these no longer builds; a
  BarChart that set a border or a dashed grid draws neither, and one that
  hid its axis line with `setAxisLineWidth(0)` draws it.
- `CalendarMonth`, in the four ports. It drew one month with its dates in
  blue circles: no month title, the names of the days in English only, no
  day marked apart from the others, and each date a line of text where a
  screen reader needs a table. No example used it. A program that draws
  one no longer builds; a calendar is a `Table` of seven columns.

### Fixed
- The four ports: the characters past U+FFFF that a font has, as the
  Chinese characters of Hong Kong in IBM Plex Sans TC (𨋢, the lift, among
  its 1,736), are drawn with their glyphs. The character map was read only
  as far as U+FFFF, the last character the font's OS/2 table can name, which
  says "or more" there; such a character was drawn as a box, or in the font
  and not its fallback font, and copied out of the PDF wrong. They are in
  the glyph map, read from the format 12 subtable, and written into the
  ToUnicode map as UTF-16 surrogate pairs; a font is read no further than
  65,536 of them.
- Go: the package's documentation, on pkg.go.dev and from `go doc`, began
  "Package encryption implements the AES encryption of PDF documents.", a
  second package comment, in aes128.go, above "Package pdfjet is a library
  for creating PDF documents." It is a comment of the file now.
- The four ports, from a fresh review of the charts, Markdown and the
  barcodes of 27 October 2026:
  - The bookmark of a heading, and the text a tagged heading is noted by,
    have no space where the runs of its markup are joined: "# Version
    **9.1.5**: notes" was "Version 9.1.5 : notes" in the outline.
  - A heading on a page that is not added to the PDF has no bookmark, where
    it failed the document, as for Markdown drawn on the pages of a first
    pass that were not kept.
  - Markup.paragraphs keeps a backslash at the end of a paragraph, which it
    read as a hard line break and dropped: "C:\Temp\" was "C:\Temp".
  - The alternate description of a Markdown image, and the text drawn in its
    place, are its text as it reads: "Fish &amp; Chips" was written so.
  - Two hard line breaks in a row, `<br><br>`, leave an empty row, in a
    table cell too; they made one break. A `\|` in a table cell is a `|`, in
    code too, as in GitHub's Markdown. Markup.paragraph reads the hard
    breaks of a text of `\r\n` line ends. The entities in the address of a
    link are the characters they name, `&amp;` an `&`, and a number of a
    control character, as `&#10;`, is a space, where it broke the line of a
    table cell.
  - BarChart: a value of a line series outside a range that is set is drawn
    past the plot, which clips the line, as in SimpleLineChart, where it was
    drawn at the edge: a target of 150 over a range of 0 to 100 had its dot
    at 100.
  - SimpleScatterChart: the trend line is of the points drawn, not of those
    outside a range that is set.
  - Barcode: an empty Code 128 text is refused, as an empty Code 39 is; it
    drew a barcode of no data.
  - The documentation: PDF417 holds 51 rows of 918 codewords, 853 of the
    text, where it said 928 and 864; the 48 codewords of Code 128 are the
    limit of GS1-128 that PDFjet keeps; Barcode, QRCode and DataMatrix give
    the quiet zone a scanner needs, which they do not draw; DonutChart's
    center text is shortened with an ellipsis where it is too wide.
  Behavior change: `new Barcode(Barcode.CODE_128, "")` throws; a scatter
  chart over a set range with points outside it draws a different trend.
- Markup, the four ports: a backslash in code is text, as in GitHub's
  Markdown, and a backtick after it closes the code: `` `foo\`bar` `` is the
  code "foo\" and the text "bar`", where the code was not closed there.
  Outside code, \` is still a backtick that opens no code.
- QRCode and DataMatrix, the four ports: `setModuleColor(Color.transparent)`
  leaves the color as it was, as every other color setter does; it drew the
  modules in whatever color the page had last.
- The charts, the four ports, from a fourth review of 26 October 2026:
  - BarChart: the numbers of the value axis have the digits of its step
    whatever `setFractionDigits` says, where with 0 an axis in steps of 0.25
    was written 0, 0, 1, 1 and data under 0.01 was written 0.00; values whose
    span, or a stack's, is past the largest float draw nothing, as in the
    line and scatter charts, where they were drawn on an axis of 0 to 1; and
    the margins make room only for the values written, not for those of bars
    outside a set range or totals past the largest float.
  - The axes of all the charts: a step under 0.0001 has the digits it needs,
    where it had none, and a step under 0.000001 too, where the digits
    stopped at 6; data at the largest float has an axis of its own, where it
    was drawn on 0 to 1.
  - The names and the numbers under the plots stay inside the chart: the
    first category of a SimpleLineChart, as "January 2026", the last number
    under a SimpleScatterChart, as "1,000,000", and the last category under
    vertical bars went past its edges. The line and scatter charts leave
    room for them at the ends of the plot.
  - SimpleBarChart: a value that is NaN or infinite has no bar and no value,
    as in the other charts, where it was written "NaN" or "In,fin,ity", and
    the description leaves it out.
  - SimpleLineChart: a value alone between gaps, or the only one, is drawn as
    a dot, where a line of no length drew nothing without markers; the
    description of one category is "2019", where it was "2019 to 2019", and
    it ends at the last category that has a name, where an empty one made
    "2019 to :".
  - SimpleScatterChart: the description counts and names only the points
    drawn, not those outside a set range, and tells the trend only when its
    line is drawn; the "trend" label stays over the plot, where a short steep
    line put it left of it.
  - The documentation of `BarChart.setGroupGap` gives its range as 0 to 0.9,
    as the code keeps it, and that of `setPanelColor` in the three simple
    charts calls Color.transparent, -1, for no panel the one exception to
    the rule that it leaves a color as it was.
  Behavior change: BarChart axes written with `setFractionDigits(0)` keep the
  decimals of their step, 0.25 and not 0; the plots of the line and scatter
  charts are narrower by up to half a label when a first or last label is
  wider than the padding.
- Markdown, the four ports, from a review of 26 October 2026:
  - The first block of a quote or a list item keeps the room it needs on its
    page, as the other blocks do: a first image, code or table was drawn
    into the bottom margin, a short table there was split, and a lead-in
    there was left at the bottom of the page, its table on the next.
  - A short table after a lead-in that ends in a colon stays with it: the
    table went to the next page alone, for the room of a header and two
    rows it asked for where it fit.
  - A heading keeps on its page what comes after it, as far as a page keeps
    that together: a table, an image, three lines of code, or a lead-in and
    its table; it was left at the bottom of a page over each. When the two
    do not fit on one page, the heading keeps the first rows of its table,
    which then starts under it. A heading of two lines or more is kept
    whole; it was kept by its first line.
  - A table split inside a quote has the quote's bar on every page it is on;
    the bar stopped where the table started.
  - An empty heading, "#" with no text, draws nothing and takes no level of
    the tags: the next "##" was an H2 with no H1 over it.
  - A table interrupts a paragraph, as in GitHub's Markdown: "Sizes:" on the
    line over a table's header, with no blank line between, was drawn as a
    paragraph of pipes.
  - A lead-in that ends in the full-width colon "：" of CJK text keeps its
    table too.
  - The lines of a block of code are cut by their width in the code font,
    where they were cut by a count of characters as wide as a "0": wide
    characters, as CJK, ran past the gray background. An empty block of
    code draws nothing, where it made empty tags and a background.
  - The four ports trim a lead-in alike, of spaces and control characters
    only, and Swift finds a check mark followed by a variation selector,
    "✓️", as the others do.
  - A table is made once to be measured and drawn, where it was made again.
  Behavior change: documents whose headings, quotes, list items or tables
  fell at the bottom of a page break their pages elsewhere, as above.
- The charts, the four ports, from a third review of 26 October 2026:
  - BarChart: a set range past the largest float, as -3e38 to 3e38, draws
    nothing, where its infinite steps failed the document; a line series
    longer than the categories no longer stretches the axis past what it
    draws; a bar below 0 has room for its value between the plot and the
    categories, where the value was written over the category's name; the
    gaps are kept in their ranges, `setGroupGap` from 0 to 0.9 and
    `setBarGap` from 0 up, where a gap of 1.5 made bars of negative width
    and -1 divided by 0; a stacked sum past the largest float is not
    written, where it was "Infinity"; and a bar wholly outside a set range
    has no value written by it.
  - SimpleScatterChart: no trend line where it misses the plot, both of its
    ends past one edge, as a flat one over a range that is set, where it was
    drawn outside the chart; the trend is computed from the distances from
    the means, which keep their precision for values near 1e8 of a small
    spread; and an empty URI is no link, as in the Go port.
  - SimpleLineChart and SimpleScatterChart: the numbers have the digits the
    step of their axis needs, up to 6, where data under 0.01 was written 0.00
    at every grid line and "from 0 to 0" in the description; data whose span
    is past the largest float draws nothing, where it drew an empty plot;
    and SimpleLineChart writes as many labels at the ends of the lines as
    the plot holds, the highlighted one first, where more were pushed up past
    the top of the chart.
  - SimpleBarChart and SimpleScatterChart group the digits of their numbers
    in thousands, 23,507, as the other charts do; in a SimpleBarChart
    narrower than its names and values the names give the values room, and
    a name is left out where not even an ellipsis fits.
  - DonutChart: a radius that is not a number is 0, in the four ports alike
    (Swift drew a ring of none where the others failed); the percentages
    are written in a ring as wide as one and a half lines of them, or a pie,
    where they spilled out of a thin one.
  - The ranges set with `setXAxisRange`, `setYAxisRange` and
    `setValueAxisRange` say that one whose end is not past its start is not
    set; ChartScale's unused palette is gone.
  - Example_26 writes the figures of its table as its data file does, two
    decimals and one, where the ports rounded them each their own way.
  Behavior change: a SimpleBarChart or SimpleScatterChart writes 23507 as
  23,507; a line chart with more lines ending together than its plot holds
  leaves some of their labels out.
- Markdown, the four ports: a heading over a paragraph that ends in a colon
  and its table goes to the next page with them, when the three fit on a
  page together but not in what is left of this one. The paragraph and the
  table went, and left the heading at the bottom of the page, as in the
  appendix of the fair test's report.
- The charts, the four ports, from a second review of 26 October 2026:
  - Data whose range, rounded to round steps, would be past the largest
    float, as bars of 3e38, is drawn on the range of its values, where it
    fell back to 0 to 1 and the bars went past the plot; a chart whose
    values reach so far no longer throws "A coordinate, size or width is
    NaN, infinite or too large for a PDF" when it draws them. A number of 16
    digits or more is written in three significant digits and a power of
    ten, 3e38 and 1.23e20, where it was written out to its last digit.
  - DonutChart: the labels of thin slices side by side, moved apart, have
    their leader lines' elbows out where their heights leave the circle, so
    that they stay off the ring, where they stacked across it; a label wider
    than the radius, or than six of its letters, is shortened with an
    ellipsis, and so is a legend item too long for a row, its percentage
    kept; the text in the hole is three quarters of a label at the smallest,
    shortened to fit there, where a long one was drawn too small to read;
    the caller's dash pattern no longer dashes the leader lines and the
    white ones; `setRadii` takes the larger radius as the outer one, where an
    inner radius past the outer drew the ring inside out; and the last
    slice, by the first around the circle, does not have the first's color
    (the ninth of nine was navy by the navy first).
  - BarChart: a chart without its fonts draws nothing, where it threw; its
    title, subtitle, legend names and axis titles are shortened to the
    chart, and a category name under vertical bars to a quarter of it, with
    an ellipsis, where they ran past it; a line series adds no categories
    of its own to bars, where a longer one added empty slots; the legend's
    line is no longer dashed by the caller's dash pattern; and a line series
    is drawn on a white line a little wider, so that it stays clear across
    the bars.
  - SimpleBarChart, SimpleLineChart, SimpleScatterChart: a chart without its
    fonts draws nothing, where it threw, and SimpleBarChart's `getHeight` is
    0; the title and the subtitle are shortened to the chart, a line's name
    at its end to a quarter of it, and a highlighted point's name to two
    fifths of the plot, kept in it; the caller's dash pattern no longer
    dashes the lines and the grid. SimpleLineChart's value labels: the first
    of a rise is after its point and under the line, where it touched it,
    and one above the plot is under its point.
  - The descriptions of the charts join the title, the subtitle and a
    donut's text in the hole as sentences, "Vehicles. By model year: ...",
    the second's first letter a capital, where a comma joined them; BarChart
    says which series is a line ("Target, a line: ...").
  - Documentation: what a chart with nothing to draw returns,
    the text in the hole in the description, SimpleLineChart's areas of
    values under 0 and its value labels outside a set range.
  Behavior change: `new DonutChart(bold, regular)` takes its fonts in the
  order of the other charts, the bold one first, where it took the regular
  one first: `new DonutChart(regular, bold)` of 9.1.1 and before draws its
  labels in the bold font and its percentages in the regular one, and is
  written `new DonutChart(bold, regular)` now. Example_28 and the booklet
  are.
  Behavior change: a chart's description joins its title and subtitle with
  a full stop, "Phones. By country: ...", where it was "Phones, by country:
  ..."; a long title, name or label is shortened with an ellipsis where it
  was drawn whole past the chart; a number of 16 digits or more is written
  as 3e38; and a DonutChart's labels moved apart are further out.
- SimpleLineChart and SimpleScatterChart, the four ports: a value outside a
  range set with `setYAxisRange` or `setXAxisRange` is clipped at the edges
  of the plot, a line where it leaves it, and a point outside it is not
  drawn; the line ran up the page, and a point was drawn off it. A value far
  past the range, such as 1e37, or values whose span is past the largest
  float, threw "A coordinate, size or width is NaN, infinite or too large
  for a PDF", and the document could not be completed; the chart is drawn.
  A chart whose values are all in its range draws the same PDF as before
  (the review of the charts, 26 October 2026).
- BarChart, the four ports: a horizontal chart narrower than its category
  names draws nothing, as the simple charts do when they have no room; its
  axis ran backwards and the bars covered the names. Under vertical bars a
  category name is written at every second or third bar, or more, when the
  names would run into each other, as the numbers of the axis are: twelve
  month names in full ran together.
- The charts, the four ports, the smaller points of the same review:
  - A range set with `setYAxisRange` or `setXAxisRange` has its grid lines
    on a round step, from 2 to 10 lines, the nearest to those of its
    rounded span: 3 to 17 is written 3, 5, 7 … 17, where it was 3.00, 5.33,
    7.67 … 17.00.
  - DonutChart: the labels of slices side by side on one side of the
    circle are moved apart, a line apart at least, where they piled on one
    another; the percentages add up to 100, the points left over given to
    the largest remainders (90 and six of 1.5 are 91, 2, 2, 2, 1, 1, 1,
    where each rounded alone they were 91 and six of 2); the white line by
    a thin slice is at most a quarter as wide as the slice is across the
    middle of the ring, so that it does not cover it; a slice whose value
    times 360, or whose sum with the others, is past the largest float is
    drawn, the values divided by the largest of them, where it was left out;
    the description has the subtitle after the title; and a chart without
    the font of the percentages draws its title in the font of the labels,
    as its documentation now says.
  - BarChart: its description, unless one is set, has its data, the name
    and the values of each series by category ("Sales: a 1, b 2."), where it
    was the title alone; its legend wraps at the right edge of the chart;
    a series with a color per bar has the color of its first bar in the
    legend, where it had the palette's; a chart without a title has no room
    left for one, its plot higher; and a category name wider than two fifths
    of a horizontal chart is shortened with an ellipsis.
  - SimpleBarChart: a name wider than two fifths of the chart is shortened
    with an ellipsis; in a chart narrower than its names the bars went to
    nothing and the values past the panel. The description keeps the name
    whole.
  - SimpleLineChart: the labels at the ends of the lines, moved apart, stay
    over the plot, moved up from its bottom, where they were pushed below
    the panel.
  - SimpleScatterChart: the name of a highlighted point is on a patch of
    the panel's color, which the gray dots and the grid do not show through.
  - SimpleLineChart, SimpleScatterChart and BarChart say in their
    documentation that a chart too small for its plot draws nothing.
  Behavior change: DonutChart's `drawOn` returns the bottom right corner of
  the whole chart, its title, its circle and its labels, where it returned
  the circle's: a chart of one slice labeled "a" at (100, 100) with a radius
  of 100 returns (300, 315), the label's leader line 15 points under the
  circle, where it returned (300, 300); measured with no page, it returns
  the same.
  Behavior change: a set axis range draws other grid lines when the lines
  of its rounded span made an uneven step, as above; BarChart's default
  description lists its data; a BarChart without a title is drawn higher in
  its box, its plot taller; the percentages of a DonutChart can differ by
  one from those it wrote, so that they add up to 100; and a scatter
  chart's names cover what is under them.
- Java and C#: a null title, subtitle, axis title, name, list of
  categories or values, given to SimpleBarChart, SimpleLineChart,
  SimpleScatterChart or BarChart, is empty, and a null slice given to
  DonutChart is left out; each threw a NullPointerException when the chart
  was drawn. Go's `AddSlice(nil)` is left out too.
- Markdown, the four ports: a table cell whose whole text is in one
  emphasis, as `**2.1**`, is drawn in that font, bold, italic or code, so
  that a table can mark a value; it was drawn plain. A cell of mixed
  emphasis is drawn in the regular font, as before, as a cell has one font.
- An image copied from a PDF that was read, with `new Image(pdf, obj)`
  (`NewImageFromPDFobj` in Go), keeps its transparency, in the four ports:
  its soft mask, the alpha of a PNG, and its mask, a color key or a stencil,
  are copied with it, encrypted when the document is. They were left out,
  with no error, and a logo on a transparent background was drawn on a
  solid box. Its /Decode and /Matte are copied too. A mask that cannot be
  copied, of an object that was not read from a PDF, is an error, as a
  color space that refers to another object is.
  A document of PDF/A refuses a copied image it cannot hold, as it does an
  image made from a file: one with a soft mask in PDF/A-1, a CMYK image in
  any PDF/A, and one of 16 bits per component in PDF/A-1. A CMYK image, or
  one of 16 bits, went in unchecked before, and the document was not PDF/A.
  Behavior change: a CMYK JPEG copied from a PDF into a PDF/A-2B document
  now fails it, "A document of PDF_A_2B cannot hold a CMYK image", where
  the document was completed.
- BarChart's value axis labels no longer run into each other, in the four
  ports. With many steps, or with fraction digits, on a narrow chart, they
  were drawn at every grid line, "0.0 10.020.030.0" on a chart 330 points
  wide. A label is now drawn at every grid line that leaves it room, the
  widest label and a pad across, or a line high on a vertical axis, and at
  every second or third line, or more, when it would not; the grid lines are
  all drawn. The charts of Example_27 are the same as before.
  Behavior change: a chart whose axis labels would meet shows fewer of them;
  a horizontal chart of 0 to 80 in steps of 10 with one decimal, 120 points
  wide, labels 0.0, 20.0 to 80.0 and leaves 10.0, 30.0 and the others out.
- SimpleLineChart and SimpleScatterChart, the four ports: the axis titles
  are shortened with an ellipsis, as the title and the names are, to the
  width of the plot for the x title and to its height for the y title; a y
  title 310 points long on a chart 250 points high ran past its top and its
  bottom. The PDF/UA description keeps the titles whole (the review of 28
  October 2026).
- SimpleLineChart, Go, Java and C#: the chart keeps a copy of the
  categories and of each line's values; a caller that reused one array for
  every line drew every line with the last values. Swift arrays are values,
  copied already. A null category is an empty one in Java and C#, where it
  failed the drawing.
- The PDF/UA descriptions of SimpleLineChart, SimpleBarChart and
  SimpleScatterChart, the four ports, add no comma or colon after a title
  that ends in ".", "?" or "!": "Sales.: a 5" is now "Sales. a 5". A line of
  one value is described by that value, "a 5", where it was "a from 5 to 5".
- TextFrame, the four ports: `getTextBottom()` (Go `GetTextBottom`) gives
  the bottom of the last row drawn when the frame breaks inside a text
  line. It gave the bottom of the last whole text line, or the top of the
  frame for one long text line, the usual case of `new TextFrame(font,
  strings)` with a height: a frame at y 100, 100 high, drew rows down to 197
  and returned 100. Markdown measures a paragraph or a heading longer than a
  page by it, which it measured too short (the review of 28 October 2026).
- TextFrame, the four ports: a justified row ends at the right edge of the
  frame in a font that kerns the space with the letters beside it, as Source
  Serif 4 does T, V, W and Y. The row was measured whole, with that kerning,
  and its words placed one by one, without it: a 300 point frame at x 40
  ended rows at 342.94 in 14 point Source Serif 4. The width is now measured
  as the words are placed. Example_17 draws its justified rows a little
  narrower.
- TextLine, the four ports: the underline and the strike-out of a word
  drawn with the space after it reach the end of the word in a kerned font;
  "WAY " in 14 point Source Serif 4 was underlined 30.10 points of its
  30.44. The word is measured without its space.
- BarChart, DonutChart and the numbers of an axis, the four ports (the
  review of 28 October 2026):
  - BarChart: the total of a stack below a range that is set is not
    written; it was written under the plot, or left of it by horizontal
    bars, as the "20" of a stack of 10 and 10 over a range of 50 to 100.
  - BarChart: without `setFractionDigits`, the values have the digits of
    the step of the axis, two at least, as in the simple charts, and the
    description gives each value two significant digits at least: values of
    0.00012 and 0.00034 were written "0", and described "A 0, B 0". Values
    such as 12.5 and 1,234 are written as before.
  - BarChart: a value is written inside a bar only when the bar is as wide
    as the value, or as thick as a line of it by horizontal bars: a stacked
    one is left out, and one inside a grouped bar is written past its end.
    The values past the ends of narrow bars, and the totals of narrow
    stacks, are written where they leave the one before them room, as the
    numbers of the axis are, and over all the bars, where the next bar cut
    them.
  - BarChart: by horizontal bars a category is written where it leaves the
    one before it room, as under vertical bars; forty of them on a chart 200
    points high ran into each other.
  - DonutChart: a slice's percentage is written only when the slice is as
    long across the middle of the ring as the percentage is wide.
  - BarChart: the numbers of an axis of 1e15 and more have the significant
    digits that set them apart: an axis of 4.84e15 to 4.85e15 was written
    4.84e15 at each line.
  - Swift: the smaller and the larger of two numbers of an axis are NaN
    when one is, as in Go; Swift.min and Swift.max dropped a NaN that was not
    the first.
  - C# and Java: a null category of BarChart is an empty one; it failed the
    drawing.
  Behavior change: a BarChart without `setFractionDigits` writes 0.00012
  as 0.00012, where it wrote 0; values that do not fit their bars, and
  names of horizontal categories that do not fit their slots, are left out.
- Kerning, the four ports: a crafted GPOS table no longer makes
  `setKernPairs(true)` take gigabytes of memory. The arrays of the kerning,
  by glyph number, grew as far as the glyphs its subtables named, uncounted,
  and a table of 8 KB whose 4,000 subtables all named glyph 65535 made 4.8
  GB of them. The room they take now counts against the same limit of work
  that bounds the rest of the GPOS table, a glyph past the glyphs of the
  font is left out of a coverage table, and a subtable that a lookup names
  again is read once. The kerning of the fonts PDFjet ships, and of a Linux
  system's, is the same as before (the review of 28 October 2026).
- SimpleLineChart and SimpleScatterChart, the four ports: the numbers of an
  axis of 1e15 and more have the significant digits that set them apart, as
  BarChart's do: an axis of 4.84e15 to 4.85e15 was written 4.84e15 at each
  line, and is now 4.84e15, 4.841e15 and on (the review of 28 October 2026).
- The four ports: colored text in the 14 standard fonts with
  `setKernPairs(true)` is kerned across its words and colors, as
  `stringWidth` measures it. A highlighted "Y" before a period, or a word
  after a space, was kerned only inside each word, so right-aligned and
  centered colored text ended past the place it measured. Text that is not
  colored is drawn as before. Also in v9.1.1 (the review of 28 October
  2026).
  Behavior change: Helvetica "Y." with the "Y" highlighted draws the period
  140 units closer, `[140<2E>] TJ`, as the same text uncolored always did.
- Markdown and TextFlow, the four ports (the review of 28 October 2026):
  - Markdown lays out a run of headings in linear time. Each heading
    measured the headings after it to the end of the run, so that 5,000
    "# h" lines took 40 seconds in Go and 16 in C#, and overflowed the stack
    in Java; they take a fifth of a second. Each heading's room is measured
    once; the pages break where they did.
  - TextFlow keeps a heading with a heading after it, and with an image and
    its caption: it kept the first line of the next heading, or the image
    alone, and was left at the bottom of a page when they went on to the
    next. When the heading and all it keeps do not fit on a page, it keeps
    the first line or the image after it, as before.
  - A Markdown heading that draws nothing, as "# <br>", takes no level and
    no space: "# <br>" then "### Intro" tagged Intro an H2, with no H1
    drawn.
  - Swift: a Markdown paragraph leads into the table after it by its last
    scalar being a colon, as in the other ports.
  - Swift and C#: Markdown reads an image only from a regular file, its
    links followed, as Go and Java do: a FIFO in the image directory was
    read, and blocked the drawing. In C#, which cannot tell a FIFO from a
    file, a file of no bytes is not read either, and its text is drawn.
  Behavior change: a TextFlow heading that ended a page alone goes to the
  next page with what follows it, so a document may gain a page.

## v9.1.1 — 2026-10-20

### Added
- complete() can leave the output stream open, so that several PDFs can be
  written into one stream, as the entries of a ZipOutputStream (GitHub
  Discussion #5): complete(false) in Java and complete(closeStream: false)
  in Swift flush it and leave it open; in C#, Complete(true) does, its
  leaveOpen as StreamWriter's. complete() closes the stream as before. Go
  already leaves a writer it is given open. Mind the boolean when porting
  code: true closes the stream in Java and Swift, and leaves it open in C#.
  In C# and Swift a bare reference to the method, as var f = pdf.Complete
  or let f = pdf.complete, now names two methods and does not compile:
  call it in a lambda, or name the one, as pdf.complete(closeStream:).
- PDFjet on Maven Central, com.pdfjet:pdfjet: package-maven.sh makes the
  bundle, and the Pro package has a pom.xml for com.pdfjet:pdfjet-pro, to
  install it in a local repository.

- A blend mode for a graphics state: GraphicsState.setBlendMode() with the
  BlendMode of the PDF's sixteen (blendmode.BlendMode in Go), for new pages
  and for pages of a PDF that was read. BlendMode.MULTIPLY draws like a
  highlighter pen that leaves the text under it readable. A state with no
  blend mode is written as before, with no /BM entry. A PDF/A-1 document
  takes only NORMAL, as it has no transparency. In Go, a blend mode that
  is not one of blendmode's panics. Suggested by Stefan Ostermann, from his
  fork of PDFjet.

### Changed
- The license of the Java and .NET packages: no limit on servers,
  computers, CPUs or cores, as the buy pages say; it said one copy on one
  computer at a time.
- C#: the library throws exceptions of their kind, not a plain
  System.Exception, in every place that did (GitHub Discussion #5), so a
  caller can tell what went wrong: InvalidDataException for a PNG, BMP,
  JPEG, SVG or PDF that cannot be read; EndOfStreamException for one cut
  short; NotSupportedException for what PDFjet does not support, as an
  interlaced PNG, a compressed BMP or an encryption it does not know;
  ArgumentException for a wrong argument, as a rotation of 45 degrees, a
  text rendering mode of 9, or a barcode's text it cannot hold; and
  InvalidOperationException for a font whose license does not allow it to
  be embedded; UnauthorizedAccessException for a PDF whose password is
  wrong or missing, so that a caller can ask for it again; and
  InvalidDataException for a font file that cannot be read. The messages
  are as before. A catch of Exception catches them all, as before.
- Java: the constructors PDF(OutputStream) and PDF(OutputStream,
  Compliance) and complete() declare throws IOException, not throws
  Exception: it is all they throw, besides unchecked exceptions (GitHub
  Discussion #5). A caller's catch of Exception or IOException is as before,
  and programs already compiled run as before. Code that catches another
  checked exception round them, which they never threw, no longer
  compiles: "exception is never thrown". Drawable.drawOn() keeps throws
  Exception, as narrowing an interface breaks the classes that implement
  it.
- The first PDF of a process is made faster: new PDF() no longer starts
  what Java, .NET and Swift take a long time to start the first time. The
  creation date is worked out by hand, as the formatters of the platform
  give it (SimpleDateFormat in Java took 37 ms, ToString in C# 5.5 ms and
  DateFormatter in Swift 1.7 ms of the first PDF); the document ID, which
  tells documents apart and is no secret, comes from ThreadLocalRandom in
  Java, not SecureRandom (22 ms), and from a Guid in C#, not
  RandomNumberGenerator (7.9 ms), and is 32 hexadecimal digits as before;
  and in Java the logger is made when something is logged (14 ms). The
  first one-page invoice of a JVM takes about 45 ms, not 89, and new PDF()
  4 ms, not 55; in C# new PDF() takes 6 ms, not 13.5, and in Swift 0.2 ms,
  not 2.2. Go took 1.5 ms for its first PDF, and is as it was. Encryption keeps
  its own cryptographic random numbers. The benchmark is in
  benchmarks/common.

### Fixed
- A filled-in PDF form that is drawn on keeps its fields: addObjects() now
  carries the /AcroForm of the catalog of the PDF that was read over to the
  catalog of the new one. Viewers showed none of the field values, as the
  form was no longer a form. Other entries of that catalog, as its outlines
  or its structure tree, are still not carried over. Found by Stefan
  Ostermann, in his fork of PDFjet.
- Setting the same graphics state again on a page of a PDF that was read
  reuses the state rather than adding another to its /ExtGState, as the
  graphics states of a new page already did. Only exactly the same entries
  are reused, so a state with a blend mode is never taken for one without.
  From Stefan Ostermann's fork of PDFjet.
- A table row taller than the page, of a cell drawn whole such as a
  TextBlock or an image, was drawn on one page and ran off it, and what did
  not fit was lost with no error (issue #4). It is now refused, saying which
  row it is, how tall, and that a text cell, which a table splits across
  pages, holds such text: in Java and C# drawOn throws, as their other
  refusals do, and complete() throws after it; in Go and Swift complete()
  fails. A row that only reaches into the bottom margin is drawn as before.
  In the four ports.
- A TextBlock's vertical alignment that is not one, as Alignment.RIGHT,
  was taken as TOP without a word (GitHub Discussion #5). It is now
  refused when the block is drawn, as a Cell's already is: "The
  vertical alignment of a text block is top, center or bottom." In Java
  and C# drawOn throws; in Go and Swift complete() fails. Measuring with
  drawOn(null) is as before. In the four ports.
- Swift: a PDF written into a stream that is closed, as a second PDF into
  one that complete() closed, crashed the program on Linux, where Foundation
  traps on the error of a stream that has none; it now throws, as Java
  does. A PDF released without complete() no longer flushes what it has
  into the caller's stream and closes it: the stream is left as it was, as
  in Java.
- A font whose name table has many records pointing at one long string was
  gigabytes of text as its information: a font of 593 KB ran out of
  memory, and one of 125 KB put 65 million characters in the metadata of
  every PDF that used it. The information is now kept to 64 KB of the
  records of each platform; a real font's copyright and license are a few
  KB. In the four ports.
- A font with neither TrueType nor CFF outlines, as a CFF2 variable font,
  was embedded as TrueType, which viewers could not draw. It is now
  refused with "a CFF2 font is not supported". In the four ports.
- Go: a stream encrypted with AES had its padding written into the spare
  capacity of the stream that was read, so merging one Read on several
  goroutines wrote the same bytes at once. The padding is a new slice, a
  second GetPageObjects leaves the page boxes as they are, and the docs of
  Read and MergePages say that the objects of one Read are used on one
  goroutine at a time.
- Go: a structelem.StructElem or relationship.Relationship that is not one
  of theirs was written raw, "P /Bogus (x" into the content and the
  structure. It is now refused, as a cap style is.
- An embedded file whose stream could not be read stayed in the PDF's list
  of files, with no object (Go, C# and Swift; Java adds it once written).
- BigTable's setFirstPage took a page of another PDF, drew the table on it
  and recorded its mistakes on that PDF. The page is now refused with "The
  page belongs to another PDF." In the four ports.
- Go: SetCompliance took any number, and NewCJKFont of a font that is not
  one wrote half of its objects before it panicked. Both are refused first.
- CalendarMonth: month 13 was January of the next year in Go and Swift,
  and is now refused, as Java does. C# draws the years past 9999, as Java
  does, and a first day of the week out of range is wrapped, as in Go and
  Swift.
- A text block of a width of NaN or an infinity drew a character on each
  line and gave NaN as its corner. It is refused when drawn: "The width of
  a text block is a finite number." In the four ports.
- The password of a PDF encrypted with RC4 or AES-128 has one '?' for a
  character past U+FFFF in Java and C#, as in Go and Swift, and not one for
  each of its two UTF-16 units.
- Swift: ImageSize and SVGImage of a stream that cannot be read, as one of a
  missing file, throw the stream's error; they said "not a PNG, JPEG or BMP
  file" and drew an empty SVG.
- A soft hyphen after a Unicode space, as an em space, breaks a text block's
  line in Java as in the other ports.
- Bookmark titles: the whitespace that runs into one space is Java's, the
  vertical tab with the others in Go, and not a no-break space in C#.
- C# on .NET Framework 4.8: the labels of a chart round the exact value
  half to even, as Java and .NET 8 do; 0.125 was 0.13.
- The keywords of a text block and the words they are matched with are in
  lower case as in Java in the four ports: İ is i with a combining dot, and
  a sigma that ends a word is ς, in Go, C# and Swift.
- Java: a lone surrogate, in a link's URI, a QR code, a DataMatrix, the
  metadata or the language, is written as the replacement character, EF BF
  BD, as in the other ports; it was '?'.
- SVG: a number of an attribute is read only when written in decimal, in
  the four ports: Java, Go and Swift read 0x1p3 and 1_000, which are none
  of SVG's, where C# did not.
- The /Width and /Height of an image object read from a PDF are numbers in
  decimal, with an exponent or not, in the four ports, and other text is
  refused: Java read 100f and hex, C# white space and Infinity, Go inf, and
  Swift took any other text, NaN too, for 0.
- C# on .NET Framework 4.8: a token above U+00FF of a merged object is
  written as '?', as in .NET 8 and Java; its Latin-1 best-fitted Ā as A.
- C#: İ in an SVG color is i, as in the other ports.
- Java: a chart step past 2^63 has no fraction digits, as in the other
  ports; Math.round stopped at 2^63.
- The numbers of a /MediaBox of a PDF that was read are in decimal, in the
  four ports: Java read 595f, and Java, Go and Swift hex.
- Java: an annotation of a tagged document whose description is a no-break
  space is refused, as in the other ports.
- Table: a row drawn whole, of a TextBlock or an image, that fits a page
  from its top but not under the first-page top margin goes on the next
  page; it was refused, though the next page had room for it. In the four
  ports.
- The creation date of a year below 1000, of a clock set wrong, is written
  in four digits in Java, C# and Swift, as Go writes it.
- Reading an encrypted PDF with many strings after /Contents in one
  dictionary took time growing with the square of their number, as each
  string looked for its dictionary's ends again: a PDF of 560 KB took 17
  s. Each dictionary is now looked at once; the same file reads in about
  0.1 s. The strings of a signature are left as they were. In the four
  ports.
- SVG: an element with many classes took time growing with the square of
  their number (432 KB, 8 s), and a style sheet of many rules for a class
  with many elements of it the rules times the elements (416 KB, 20 s). The
  classes are now looked up in a set, and a style sheet applies 1,000,000
  declarations at most: past that, the SVG is refused with "The style
  sheet of the SVG applies more than 1000000 declarations." A real SVG
  applies a few thousand. In the four ports.
- An image made from an image object of a PDF that was read wrote
  "/Filter " and "/BitsPerComponent " with no value when the object had
  none, so the next key was taken for their value and viewers dropped the
  image; and one with no stream was half written, after which complete()
  wrote a corrupt PDF. Now the two keys are written only when the object
  has them, and an object with no stream is refused before any of it is
  written: "The image object has no stream." In the four ports.
- Go: a QR code of an error correction level that is not one looped for
  ever when it was negative, and gave format bits that are not its blocks'
  for 4 to 7; C# the same for 4 to 7, as its enum holds any int. Both now
  refuse it: "The error correction level 4 is not one: use
  errorcorrectionlevel.L, M, Q or H." Java and Swift have closed enums.
- Go: a cell's word of invalid UTF-8, as Latin-1 text, broken between its
  characters in a narrow column panicked with a slice bound past the text:
  each character is now its bytes in the text.
- A page that was read, with an annotation in its /Annots before its own
  /Contents, had the annotation's /Contents, a string, taken for its
  content: stamping it was refused as "not a stream". The page's own
  entries are looked at now, for /Contents and the /Kids of the page tree.
  In the four ports.
- Java: the library builds with JDK 23 and later, whose javac warned of the
  documentation comment before the package of ImageSize.java and
  SASLprepTables.java, an error as the build treats warnings. It is
  developed and tested on JDK 25, and compiled for Java 8 with
  --release 8.

## v9.0.9 — 2026-10-15

### Added
- Task lists in `Markdown`, in the four ports, as GitHub's Markdown writes
  them: an item that starts with `[ ]` or `[x]` is drawn with a box in
  place of its bullet or number, gray when the task is open, and filled
  with a white check mark when it is done. In a PDF/UA document the box is
  the label of the item, read as ☐ or ☑. A list without tasks is drawn as
  before.
- `Markdown.setHeadingColor`, `setLinkColor` and `setCheckColor`, in the
  four ports: the color of the headings, which are in the color of the text
  unless it is set; of the links, blue unless it is set; and of the box of a
  task that is done, green, 0x1A7F37, unless it is set;
  `Color.transparent` leaves each as it is.
- In `Markup`, and so in `Markdown`, in the four ports, the inline markup
  of GitHub's Markdown that was drawn as it was typed: `_italic_` and
  `__bold__`, which emphasize only at the edge of a word, so that
  `snake_case` is text; `~~strikethrough~~`, two tildes, one or three being
  text, as in `~5`; and links that are their address, `<https://pdfjet.com>`
  or `https://pdfjet.com` alone, which ends before the punctuation at its
  end, as the period of a sentence, and before a `)` that closes no `(` of
  its own.

### Changed
- The Go port asks for Go 1.26 or later, not 1.27: nothing in it needs
  1.27, and Go supports its last two releases. CI tests it with 1.26 as
  well as 1.27, as it does Swift 6.2 beside the newest.
- Text of printable ASCII characters is measured and drawn faster, in the
  four ports: each font keeps the glyph and the width of the ones it has,
  read once, where every character went through the tests that 9.0.3 to
  9.0.5 added for control, ignorable and missing characters. The 2,546
  pages of Example_24 took 37% longer in Go than in 9.0.1: 1,145 ms, now
  905 ms, against 845 ms in 9.0.1. In C# they take 11% less time, 2,296 ms
  where they took 2,576 ms; in Java and Swift about the same. The PDFs are
  the same, apart from their dates and IDs.

### Fixed
- A link, or any underlined or struck-through text, that ended a paragraph
  of a `TextFrame`, as in `Markdown` and `Markup`, had its line drawn one
  space past its last word, in the four ports.
- The underline of a link of `Markup` and `Markdown` is in the color of the
  link; it was black under the blue.

## v9.0.7 — 2026-10-12

### Added
- `BigTable.setColumnWidthsInPercent(percents)`, in the four ports: the
  width of each column, in the order `setColumns` draws them, as a
  percentage of the table's width, the page less the margin on each side,
  as Table's `setColumnWidthsInPercent` gives them; percentages that do not
  add up to 100 are shares of it. A field wider than its column is cut to
  fit it, ending in " ...". Without it each column is as wide as its widest
  field, as before. Percentages that are not one for each column, or a
  negative one, are refused.

### Changed
- A font is read a little faster, in the four ports: the characters of the
  format 12 character map are set with one lookup each, not two, and Java
  decodes the Windows names as UTF-16BE, as C# and Go do, without looking
  the charset up by its name for each. 2 to 7% of reading a font.
- Drawing text is as fast as before the fixes of 12 October, in the four
  ports: the character first drawn with each glyph, which decides whether
  a character is copied as another, is kept in a table of plain numbers,
  not a map, and a character drawn is looked up once to be recorded and
  checked. Java drew 100 pages of text in 20.6 ms after the fixes, 18.0
  before them, and 17.9 now.
- Go reads a font into a buffer of its size, once: NewFontFromFile reads
  the file in one read, and NewFont a reader that says its length, as a
  bytes.Reader does, at that length. io.ReadAll grew its buffer as it
  read and copied a font of 200 KB several times, about half of what a
  document of embedded fonts allocated: a form of PDFjet Forms is made 17%
  faster, with a third less memory. Swift reads a font file in one read
  too; Java and C# read at the size already.
- The examples are 50, in an order by topic, the same in the four ports:
  01 to 05 the scripts and fonts (Latin, Greek and Cyrillic; Chinese;
  Japanese and Korean; Arabic, Hebrew, Persian and Thai; and, new, the
  fonts that are not embedded, the core fonts and the CJK fonts, with
  what they gain and cost), then fonts and text (06 to 09), text layout
  (10 to 19), tables (20 to 24), charts (25 to 28), graphics and images
  (29 to 35), barcodes (36 to 38), navigation and pages (39 to 42), forms
  (43, 44), standards and security (45 to 47) and existing PDFs (48 to
  50). Examples that showed the same thing twice are one: the table
  headers with the spanned cells (21), the horizontal with the vertical
  bar charts (27), QR codes with Data Matrix (37), page numbers with the
  contents page drawn last (39), and merging with splitting (49). The
  company form, the second of two forms, the source listing, the calendar
  and the tagged copy of the BigTable example are gone; Example_24 says
  how to tag its table. examples-java.html and examples-dotnet.html list
  them, and the old numbers are not kept.
- The CJK font constructor is no longer deprecated, nor to be removed in
  v10, in the four ports: a font that is not embedded keeps a document
  small, and its documentation says plainly what it costs, as Example_05
  shows.
- The number that tells a font apart, of which the tag of its subset is
  made, folds only the blocks of 256 characters the font has glyphs in, in
  the four ports: ten times faster, 6 microseconds for IBM Plex Sans where
  it took 62. The subset tags
  change, and so the bytes of every PDF with an embedded font; the content
  is the same.
- A font whose license forbids embedding it is refused, in the four ports:
  the fsType of its OS/2 table "Restricted License embedding", and none of
  the bits that allow embedding, Preview and Print or Editable. It was
  embedded. The message names the font and points to "Choosing a font you
  may embed" in the README. None of the fonts that come with PDFjet is such
  a font.
- A font's GPOS table, where its marks go, is read the first time a mark is
  drawn in it, not when the font is loaded, in the four ports: most
  documents draw no mark. A page in Noto Sans takes 2.2 ms instead of 6.3
  in Java, 2.4 instead of 5.3 in C#, 1.9 instead of 7.0 in Go and 3.7
  instead of 9.3 in Swift, and allocates a sixth to a third of what it did;
  a page in three IBM Plex Sans fonts allocates 15% less. A document that
  draws marks reads the table as before. The output is the same, byte for
  byte.
- A font's character map, the format 4 subtable, is read a segment at a
  time, in the four ports, not a character at a time from the first the
  font has to the last, the gaps between its segments too: a page in three
  IBM Plex Sans fonts takes 1.9 ms instead of 2.2 in Java. The characters
  map to the same glyphs in all 261 fonts that come with PDFjet.

### Fixed
- An Arc, and so an Ellipse, given a stroke width and no colors is drawn a
  black line of that width, in the four ports, where it was drawn 0 wide,
  the thinnest line the device draws, whatever the width.
- The password of AES-256 encryption is cut at 127 bytes of UTF-8, as PDF
  2.0 asks, even inside a character, in the four ports, as before 9.0.5 and
  as qpdf, pdf.js and pypdf cut it. 9.0.5 cut it before a character that
  did not fit whole, so a password longer than 127 bytes could make a PDF
  that other readers did not open, and a PDF of theirs that PDFjet did not.
  In Go, a password that is not UTF-8, as one in Latin-1, failed to open
  a PDF with "slice bounds out of range"; it is tried as its own bytes
  again.
- A Chart or a BarChart of data whose range does not fit a float, as
  points at an infinity or near the largest float, stacked bars whose sums
  overflow it, or a series of NaN alone, has the axis from 0 to 1, in the
  four ports. Swift stopped the program; the other ports could draw an
  axis of NaN. A point at an infinity is left out of the range, as one at
  NaN was, and is refused when drawn, as before.
- Swift no longer stops the program on data from the caller or from a file:
  an image of a PDF whose /Width or /Height is past an Int, or NaN, is as
  wide as Java makes it; a DataMatrix of text too long for the largest
  symbol has no modules, and drawing it makes complete() throw; and
  getFitChars of a deprecated CJK font for a width of NaN or an infinity
  returns 0 or every character.
- A page that /Kids lists again is returned again by GetPageObjects, but
  has the entries it inherits added once, in the four ports: one page of
  2,000 fonts listed 2,000 times, a file of 37 KB, allocated 3.7 GB and
  took a second in Go; it takes 5 MB and 19 ms with Merge.
- An entry of the page tree too large for what it is, a /MediaBox,
  /CropBox or /Rotate of more than 64 tokens, or a /Resources that large
  that is not a dictionary, is not inherited by the pages under it, in the
  four ports: 1,000 pages under a node of a /CropBox of 20,000 numbers, a
  file of 117 KB, allocated 6.2 GB in Go and took 18 seconds in Swift. A
  page then has the /MediaBox of Letter, as one with none has. A /MediaBox
  that refers to an object of more than a box is not read either: 1,000
  pages over an array of a million numbers, a file of 2 MB, allocated 15 GB
  and took 8.3 seconds in Go; it takes 94 MB and 0.1 seconds.
- The header of an object stream is read no further than the objects its
  /N says it has, and the PDF's budget allows, in the four ports. It was
  split into its tokens first: a header of 16 MB of one object allocated
  892 MB in Go and 569 MB in Java, and took 6 seconds in Swift.
- ASCII85Decode stops as soon as a stream decodes to more than the PDF's
  budget allows, as the other filters do, in the four ports. It decoded the
  whole stream first: a file of 195 KB, Flate and then ASCII85 of 200
  million 'z's, allocated 4.5 GB in Go, with a peak of 2.9 GB, before it
  was refused; it takes 557 MB, with a peak of 482 MB.
- AddResourceObjects adds the resources that pages share once, as an object
  of their own or from a node of the page tree, in the four ports, not once
  for each page: 4,000 pages of one resources object of 4,000 fonts, a file
  of 529 KB, allocated 4.8 GB and took 1.9 seconds in Go; it takes 36 MB
  and 29 ms. A /Font, /XObject or /ExtGState dictionary that is an object
  of its own is added once too: small resources on each of 4,000 pages
  that refer to one /Font dictionary of 4,000 fonts, a file of 1 MB,
  allocated 4.6 GB and took 2.5 seconds in Go; it takes 42 MB and 39 ms.
- GetContentObject charges every stream it copies into the content of a
  page to the PDF's budget, in the four ports, not only a stream the page
  names again: each page that joins its streams has a copy of its own, and
  64 pages of /Contents [4 0 R 5 0 R], of a stream of 16 MB, held 1 GB
  against the budget of 256 MB. Past the budget, a page has no content, as
  before; each call makes a copy, and is charged.
- A document of PDF/A-1, PDF_A_1A or PDF_A_1B, refuses transparency and
  optional content, which PDF/A-1 has not, in the four ports, as it refuses
  an image with transparency: SetGraphicsState of an opacity below 1, and
  drawing an OptionalContentGroup, are errors, and an SVG of an opacity
  below 1 is drawn opaque, as an annotation is. They made a document that
  veraPDF refused. PDF/A-2 and PDF/A-3 have both, as before.
- A font of another PDF, which drawing refuses, is not changed by it, in Go
  and Swift: the glyphs and characters drawn were recorded in it, for the
  PDF it belongs to, and with one font drawn on 4 PDFs at once the Go race
  detector found data races, which can stop the program. Nothing is drawn
  with it, on a page or in a stamp: Go read what the font recorded while
  another goroutine drew on its PDF, and stopped with "concurrent map read
  and map write". Java and C# throw before they change it, as before.
- Java: Permissions.toString writes its raw value in ASCII digits whatever
  the locale; in an Arabic one it wrote them in Arabic-Indic digits.
- An Arc, and so an Ellipse, of no colors is drawn with its dash pattern,
  in the four ports; the dash was written only with a stroke color, and a
  dashed arc of no colors was drawn solid.
- Drawing a Table leaves the TextBlock and the marker Point of a cell where
  the caller put them, in the four ports: measuring the cell moved the text
  block to 0, 0, and drawing it moved both to where the cell drew them.
- Java and C# read the offset of a GPOS extension lookup as the unsigned
  number it is: an offset of 0x80000000 or more in a malformed font wrapped
  round to a subtable earlier in the table, which was read; past the table
  there is no subtable, as in Go and Swift.
- ImageSize refuses a PNG whose first chunk is not IHDR, or that has two, as
  NewImage does, in the four ports: it gave the size of such a PNG, which
  NewImage then refused.
- Go: a JPEG of an EXIF offset of 0x7FFFFFFE no longer panics on 32-bit
  platforms, where the offset plus 2 overflowed and passed the bounds check.
  CI runs the image tests on 32 bits too.
- The objects of the object streams of a PDF take nothing of the reader's
  budget up to one for each 8 bytes of the PDF, in the four ports, as many as
  a PDF of its length can hold of its own; past them each takes 256 bytes, as
  before. Every one took it, and a real PDF of 5 MB of 1.1 million objects
  was refused; a short PDF that makes millions of objects still is.
- A word far wider than a TextFrame, or a TextBlock or a table cell, broken
  into rows is broken in about linear time, in the four ports: the search
  for a soft hyphen to break at looked at the whole rest of the word for
  each row, and TextFrame copied it, so 4 times the word took 16 times the
  time. A word of 400,000 characters takes 86 ms in Go, where it took 1.8 s,
  and 145 ms in Java, where it took 1.45 s. TextFrame keeps the rest of the
  word in place as it draws its rows, and copies it once, when the frame is
  full.
- AddAssociatedFile refuses an EmbeddedFile of another PDF, as a
  FileAttachment does, in the four ports: the document's /AF and
  /EmbeddedFiles named an object of the other PDF, a corrupt PDF/A-3.
- A document of PDF/A-3 takes a file attached to a page with FileAttachment
  when it carries the file as an associated file too, in the four ports: the
  annotation's /FS is then the file specification /AF lists, which veraPDF
  passes. It was refused since 9.0.5. A file attached alone is refused, as
  in PDF/A-1 and PDF/A-2.
- Go: an EmbeddedFile no longer keeps its content after it is written; the
  PDF kept every file whole until it was freed.
- The XML parser of SVG images and of the markup refuses what is not XML, in
  the four ports: a comment with -- in it, an XML declaration after the
  start, a tag or attribute whose name is not a name of XML, two attributes
  that are the same once their prefixes are resolved, and ]]> in the text.
  Such files were read, and drawn, where other readers refuse them.
- A table of Markdown with more than 64 columns, and tables of more than
  100,000 cells in all, make the text an error, in the four ports: the
  first was drawn as a paragraph of pipes, and a header of 64 columns over
  64 KB of rows of "|" alone took 895 MB in Go. Draw a table that large
  with BigTable. The rows past the limit are counted and not kept, so the
  text is refused before it takes the memory: 195 KB of such rows took
  336 MB in Go before it was refused.
- A font whose fsType allows its bitmaps alone to be embedded, bit 9, is
  refused, in the four ports: PDFjet embeds outlines, and it was embedded.
  An fsType of Restricted License with the reserved bit 0 set is refused
  too; it was taken.
- The core fonts do not draw or measure the characters Unicode does not
  draw, in the four ports: a variation selector, a zero width space, a word
  joiner, the joiners, the marks of direction and the byte order mark were
  each a space, so that "©️ 2026" pasted from a phone had two, in the text
  copied too. The deprecated CJK fonts drew and measured them, and the soft
  hyphen, 1 em wide; they leave them out too.
- A TextBlock breaks a word at a soft hyphen where TextFrame does, in the
  four ports: a word broken between its characters was never broken at one
  after its first line, so 12345678 with a soft hyphen after the 4 drew
  "34" where TextFrame drew "34-", and a right to left TextBlock never
  broke at one, cutting the word between two characters with no hyphen.
  Its hyphen is at the end of the line, on the left.
- GetFitChars of a core font kerns a character with the next one drawn, as
  StringWidth does, in the four ports: it kerned it with a soft hyphen after
  it, and counted fewer characters than StringWidth fit.
- A document that draws two characters of one glyph, as "١٢٣" and then
  "۱۲۳", or "∕" and then "／", copies each as drawn, in the four ports: the
  second is drawn with itself as its actual text, as the ToUnicode map
  gives the glyph the first. It was copied as the first. Characters copied
  as the same text, as an Arabic letter and its presentation form, need
  none, and in a word with marks the glyph stays in the word's span: the
  span of its own put a space inside the word when it was copied.
- A stamp gives a zero width non-joiner or joiner its font has no glyph for
  to the actual text of the glyph before it, as a page does, in the four
  ports: Persian text in a stamp was copied without its ZWNJ.
- A text rendering mode set back to 0 is written, in the four ports: the
  mode lasts past the end of the text, and text drawn after invisible text,
  mode 3, stayed invisible.
- A TextFrame drawn on pages at a NaN or infinite location, width or
  height, and a Markdown text of NaN or infinite margins, are refused, in
  the four ports: no word was placed, and pages were made until the memory
  ran out.
- Go: an image of a Markdown text that cannot be read, a file that is not
  an image, a PNG cut short or an SVG that is not SVG, fails the document;
  it panicked, so Markdown from a user could stop a server. Java, C# and
  Swift throw, as before.
- A word of a table cell broken between its characters breaks again at a
  soft hyphen, as TextBlock and TextFrame do, in the four ports:
  "1234<SHY>5678" was 123 / 456 / 78 where they draw 123 / 4- / 567 / 8.
- GetFitChars of a CJK font fits all of a text in a width of more
  characters than it has, as 1e30 or an infinity, in Go and C#, as in Java
  and Swift: none fit.
- A zero width non-joiner or joiner the font has no glyph for, in left to
  right text between two left-to-right marks, is in the text copied, in
  the four ports, as in a stamp: it was left out.
- C#: a lone surrogate in the text of a stamp is drawn as a character the
  font does not have, as in Java and Go: it threw.
- Reading a PDF copies no stream without charging it to the PDF's budget:
  a token of an object stream's header longer than a number is copied no
  further than 20 bytes, in the four ports, and the TIFF predictor is
  undone in the data decoded, in Go, Java and C#.
- Java: a lone surrogate in a password is EF BF BD in its UTF-8, as in C#
  and Swift; it was '?', so a PDF one port wrote with it another did not
  open.
- An image of a PDF whose /Width or /Height is past an int takes the
  largest or the smallest int, in Go and C#, as in Java and Swift: Go took
  the smallest int64, C# the smallest int, and Go panicked on 1e400.
- The XML parser refuses a declaration of the reserved prefixes, in the
  four ports: xml stands for its namespace alone, and xmlns and its
  namespace are never declared. Both were read.
- Go: an offset of 0x80000000 or more of a GPOS extension lookup is read
  unsigned on 32 bits too, as in Java and C#: it was negative there, into
  the table, and the marks were moved by the wrong subtable.
- An Image turned 90 or 270 degrees is as wide as it was tall, in the four
  ports: DrawOn returned its corner unturned, and GetWidth and GetHeight
  its size unturned, so in a table cell it ran over the rows after it;
  ResizeToFit fits it as it is drawn.
- A text line in a TextColumn is raised or lowered by its vertical offset
  once, in the four ports: a superscript rose twice as far as on its own,
  and its link with it.
- A destination the document names keeps its name, in the four ports: a
  bookmark's, dest#1 and on, took it, and the document's link to it went
  to the bookmark. The bookmark's destination is named again.
- A bookmark on a page that is not added to the PDF fails the document, in
  the four ports: it went to /Dest [0 0 R], which readers refused.
- Go: an empty dash pattern of an Arc with a stroke color is refused, as
  without one, as the other shapes and the other ports refuse it; it was
  taken for none.
- BigTable.setColumnWidthsInPercent, in the four ports: columns set again
  after the percentages, fewer or more of them, are refused, where fewer
  stopped the program and more were drawn past the page; a column too
  narrow for " ..." draws nothing, where it drew the mark over the next
  column; the largest float as a share is a share, where it failed the
  document; and a margin of half the page or more is refused.
- A NaN or infinite rotation of an Arc, a Container or a Stamp fails the
  document, as a NaN coordinate does, in the four ports: it wrote
  0 0 0 0 0 0 cm, and what was drawn vanished.
- A curve of a path has two "c" control points, or one "v" or "y" one; a
  path of others is refused, on a page and in a stamp, in the four ports:
  a "c" curve of one control point wrote c with four numbers, not six,
  which readers refused.
- Path.ScaleBy scales a point the path lists more than once once, in the
  four ports: (10, 10) listed twice became (40, 40).
- A Rect of a negative width or height with a corner radius is the same
  rectangle the other way round, in the four ports: its corners were
  turned inside out.
- A Stamp is given its size before anything is drawn on it, in the four
  ports: drawn before, the content was below the stamp and not seen; it
  is refused, and so is a new height after drawing.
- Go: nothing is written after %%EOF: an image made after Complete was
  written after it, into the writer of the caller, with no error.
- A Point that is a link, drawn on its own, makes its link, and a marker
  of lines, a plus, a dash, an X or a multiply sign, is stroked in the
  point's color, in the four ports: the link was not made, and the marker
  was drawn in the pen's color.
- Markdown: a heading that starts a list item or a quote keeps a line of
  the text after it on its page, as any other heading does, in the four
  ports: it was drawn into the bottom margin, with nothing after it.
- Drawing on a page of a PDF that was read, in the four ports: the page's
  own content is drawn in a q and a Q of its own, so that a scale or a
  text rendering mode it leaves set does not change the drawing; a page
  with no /Contents gets the drawing, which was lost; the drawing starts
  at the corner of the /MediaBox, as one of [100 100 712 892] put it
  outside the page; and a /Contents that is not a stream or an array of
  them fails the document.
- A Paragraph drawn by a TextColumn has its coordinates, GetX1 to GetY2,
  GetTextX and GetTextY, in the four ports, as one a TextFrame draws: they
  stayed 0.
- Go: a cap style, a join style, a page layout or a page mode that is not
  one of its package's is refused: CapStyle(7) wrote "7 J", and "Single
  Page" broke the catalog. A cell's vertical alignment that is not Top,
  Center or Bottom fails the document; alignment.Left, the zero value,
  panicked.
- Swift: a watermark on a page of a NaN or infinite size fails the
  document, as in the other ports; it stopped the program.
- A document of PDF/A is refused where it would break PDF/A's limits, in
  the four ports, rather than written and failed by veraPDF: in PDF/A-1,
  an array of more than 8191 entries, a page tree, a parent tree, a
  structure element or the marked content of a page, and a resources
  dictionary of more than 4095; in any PDF/A, a line of text of more than
  16383 characters, a string of more than 32767 bytes, and more than
  8,388,607 objects. PDF/A-1 documents that large can be PDF/A-2 or
  PDF/A-3 instead. A page tree and resources that keep within PDF/A-1's
  limits at any size are planned for 9.1.1.
- Go: the elements of a tagged page still open when it is written are
  copied out, so the page's array of them is freed; it was kept until
  Complete, 27% of the heap of a tagged BigTable of a million rows.
- README: the speed of 500 pages in under 100 ms is that of a warm run,
  and a tagged BigTable keeps a little of each row until complete().
- C#: a structure element is tagged with a language of its own only when
  it has one, as in the other ports: every table, row, cell, list and
  paragraph element was tagged en-US, in a German document too, so a
  screen reader read them in English.
- A page of a PDF that was read, whose /MediaBox is of a NaN or infinite
  size, is Letter, as one of no size is, in the four ports: Swift stopped
  where it was watermarked.
- Swift builds with Swift 6.2, which Package.swift asks for: it gave up on
  type-checking an expression of CRC32. CI builds with 6.2 too.
- C#: a number past the range of a double, as a /Width of 1e400, is an
  infinity on .NET Framework 4.8 as on .NET 8; it threw there.
- Drawing a Table leaves any drawable of a cell, an Image, a barcode or one
  of the caller's, where the caller put it, in the four ports, as it left
  a TextBlock: an image at 7, 8 was left at 202, 319.9.
- A cell's vertical alignment that is not top, center or bottom fails the
  document with the same message in the four ports; Java and C# threw a
  bare "Invalid vertical text alignment option."
- A TextColumn breaks a word too wide for what is left of its line at a
  soft hyphen, as TextBlock and TextFrame do, in the four ports: it moved
  the whole word to the next line. A word wider than the column whose first
  piece does not fit the rest of its line breaks on the next lines.
- .NET Framework 4.8: the category of a character is that of .NET 8, of
  Unicode 15.0, from a table of the build's own; .NET Framework's data is
  of Unicode 8.0, and a mark newer than that, as the Arabic U+08D4 of
  Unicode 9, was laid out as a spacing glyph, not placed as a mark.

## v9.0.5 — 2026-10-12, internal

An internal release, not published: its changes come out with v9.0.7
(the owner, 12 October 2026).

### Added
- `TextColumn.addCJKParagraph(font, text, language)`, in the four ports
  (`AddCJKParagraphWithLanguage` in Go): a paragraph of Chinese, Japanese or
  Korean in the language given, which every line it makes carries into its
  marked content, so a screen reader reads it in that language; without
  one, Example_44's Chinese was read as English. Example_44 sets `zh-Hans`.
- `Table.setCellPadding(side, vertical)`, in the four ports: the padding of
  every cell of a table in one call, at the sides and over and under the
  text, as `setCellBorders`, `setCellBorderColor` and `setCellBorderWidth`
  set the borders of every cell. A table whose cells have borders reads
  better with more than a new cell's 2 points, such as 4 and 3. Opt-in: a
  new cell keeps its 2 points on every side, so no table changes unless it
  asks.
- `ImageSize`, in the four ports: the size an image is drawn at, and its
  pixels, read from the header of its file alone, without its image data in
  memory, so that a page can be laid out before its images are drawn, and an
  image refused early, for its size or for a header that `Image` refuses,
  before a byte of it is decoded: a PNG of 100,000 by 100,000 pixels is
  refused from its 33 bytes of header. `ReadImageSize` and
  `ReadImageSizeFromFile` in Go, `ImageSize.read` in Java and Swift,
  `ImageSize.Read` in C#; `getWidth`, `getHeight`, `getPixelWidth` and
  `getPixelHeight`. The size is the one `Image` gives: the physical size of a
  PNG's pHYs chunk, a JPEG's JFIF density or a BMP's pixels per metre, a JPEG
  turned by its Exif orientation height by width. A JPEG is read to its frame
  header, a BMP to its pixels per metre, a PNG a chunk at a time to its end,
  its image data checked by its CRC and passed over. The tests hold it to
  `Image` on 710 images, and Go's fuzzing on any bytes.
- The XML parser that reads SVG is public API, in the four ports, for PDFjet
  Pro's electronic invoices too, so that the library and Pro read XML with one
  parser: `xmlparser` in Go (`src/xmlparser`), `XMLParser`, `XMLNode` and
  `XMLException` in Java and C#, `PDFjetXMLParser`, `PDFjetXMLNode` and
  `PDFjetXMLError` in Swift (Foundation has an XMLParser). `parse` refuses a
  DOCTYPE, the safe choice for documents such as invoices; `parseSkippingDoctype`
  skips one, as the SVG reader does, and still reads none of its entities.
- A JPEG with an Exif orientation, as phones and cameras write them, is drawn
  as it is meant to be seen, in the four ports: the Orientation tag (1 to 8)
  of the Exif segment turns or flips the image when it is drawn, and
  `getWidth` and `getHeight` give its size as seen, height and width swapped
  for 5 to 8. The image object keeps the pixels as stored. An Exif segment
  that is not whole or not read so, its byte order, its directory or the tag
  wrong, is passed over, the image drawn as stored. Orientation 1, or none,
  draws the page byte for byte as before.
- TrueType fonts, `.ttf` files, are embedded as subsets:
  the outlines of the glyphs a document does not draw are left out when the
  PDF is completed, with the tables a reader needs and no others (the
  shaping tables, which PDFjet reads, and the vertical metrics are left
  out). Glyph numbers stay, so pages, widths and text extraction are as
  before. Noto Sans SC goes from 6.5 MB to 93 KB in a page of Chinese, and
  Noto Sans from 344 KB to 25 KB in a page of English. The font's name
  carries the six-letter tag of a subset, its widths and ToUnicode map list
  the glyphs kept, and a PDF/A-1 file has the CIDSet PDF/A-1 asks of a
  subset. A font whose license forbids subsetting (fsType 0x0100 in its
  OS/2 table) is embedded whole, and so is a font set to with
  `setSubset(false)`. Fonts of CFF outlines, `.otf` files, are embedded as
  subsets too: the charstrings of the glyphs drawn and the subroutines they
  call, the others emptied (`CFFSubset` in the four ports, `cffsubset.go` in
  Go); Source Han Sans JP goes
  from 4.1 MB to 115 KB in a page of Japanese, IBM Plex Sans from 58 KB to
  14 KB in a page of English. The four ports embed the same subsets, byte for byte,
  under the same tags.
- `new Font(objects, stream)`, a font added to an existing PDF, reads `.ttf`
  and `.otf` files (`NewFontForObjects` in Go), embedded as subsets too, in
  the four ports, with no change to the API: the font's objects are
  numbered when it is added and filled in when the objects are added to the
  PDF (`addObjects`, or `addResourceObjects`), after the pages are drawn.
  `setSubset(false)`, before then, keeps it whole. Example_37 goes from 127
  KB to 72 KB, Example_50 from 177 KB to 68 KB, their pages the same; a
  form filled in with a few words of a CJK font no longer carries the
  whole font, 6.5 MB for Noto Sans SC. Its ToUnicode map is compressed now.
- A path to a `.ttf.stream` or `.otf.stream` file opens the `.ttf`, or else the
  `.otf`, file of the same name beside it, so code written for the stream
  files PDFjet shipped runs as it is with the fonts of 9.0.5.

### Changed
- **The bundled fonts are TrueType `.ttf` files, and there are no `.stream`
  files any more.** The font constants (`IBMPlexSans.Regular`,
  `NotoSans.Bold`, `SourceSerif4`, `JetBrainsMono` and the rest) point to the
  `.ttf` files, embedded as subsets: the Go examples went from 52.8 MB to
  30.4 MB together, Example_02 from 9.6 MB to 442 KB. The IBM Plex families
  are IBM's `.ttf` editions, of the versions of the `.otf` files they replace,
  the ones the PDFjet Forms editor shows as `.woff2`. Their glyphs are the
  same; three things differ. IBM Plex Sans JP's `.ttf` sets its text 1.5 em
  apart, as Plex Sans KR does, where its `.otf` sets it 2 em apart, as SC
  and TC do in both editions (ascent 1.06 em, descent 0.44 em, no line gap,
  where the `.otf` has 0.88, 0.12 and 1): IBM's two editions disagree. The
  Latin letters of IBM Plex Sans TC are 1 to 5 thousandths of an em
  narrower. The italic angle of IBM Plex Sans, Serif and Mono is rounded to a
  whole degree. IBM Plex Sans is still there as `.otf`, a font of CFF
  outlines. The fonts folder went from 606 MB to 414 MB. The subsets of Noto
  Sans SC and TC made ahead of time (`-SC3500`, `-TC4808`) are gone with the
  lists of characters they were made of, and the tools that made them.
- Examples 02 and 04 draw their Chinese, Japanese and Korean in the `.otf`
  of IBM Plex Sans SC, TC, JP and KR, named by path in the code: a page of
  them draws hundreds of glyphs, whose CFF outlines make a smaller subset,
  Example_02 336 KB in place of 442 and Example_04 228 KB in place of 311.
  The font constants stay the `.ttf`. Examples 37 and 50 draw a line or two
  in IBM Plex Sans, so the `.ttf`, the smaller there.
- The ToUnicode map of every embedded font is compressed, a few
  hundred kilobytes less for a large CJK font embedded whole.
- SVG is read with one XML parser, the same in the four ports, in place of
  Go's encoding/xml, Java's StAX, C#'s XmlReader and Swift's own tokenizer,
  so that the four read an SVG alike. It came from the parser of PDFjet Pro's
  electronic invoices: strict, not recursive (256 levels of elements at
  most), 20 MB at most, nothing fetched, and only XML's five entities and
  the numeric ones. A DOCTYPE, which drawing programs write, is skipped, and
  the entities it declares are not read. What is not well-formed XML is
  refused, "parsing SVG: ...", where Go and Swift drew some of it before:
  attributes without a space between them, text or a second element after
  the root, no root element. The 246 SVG files of the repository draw the
  same as before, and the 1,170 SVG files of the Go fuzz corpus are accepted
  or refused alike in the four ports (560 and 610).
- Go refuses the image data of a PNG whose Deflate data is invalid before the
  bytes of the image, as the JDK, .NET and Swift do: a dynamic block whose
  code has no end of block, or a stored block whose length is not the
  complement of its check, which Go's compress/flate does not refuse when it
  has the bytes of the image before it needs the end of the block. A Go
  translation of puff.c, as Swift and net48 have, checks the data up to those
  bytes (src/internal/decompressor/puff.go); it adds about 3 ms to each
  megabyte of the pixels of a PNG. The 2,698 PNG files of the Go fuzz corpus
  are now accepted or refused alike in Go and Java.
- A number of SVG path data is finite: one past the range of a float, such as
  `1e40`, is refused as invalid path data in the four ports, where Java, C#
  and Swift read it as an infinity.
- The license in the Java and .NET packages says what the Solo License and the
  Team License cover, one developer, and two to five, as pdfjet.com has said
  since the licenses were renamed (they were the Named Developer and the
  Transferable Developer licenses).
- A document of PDF/A or PDF/UA refuses what it cannot be, in the four
  ports: Complete returns an error, where it wrote a document that said it
  was PDF/A or PDF/UA and was not, as veraPDF found.
  - A core font, such as Helvetica, or one of the deprecated CJK fonts, is
    not embedded, which both standards ask of every font: "The font
    Helvetica is not embedded, which a document of PDF_UA_1 needs: use a
    font that is, such as IBM Plex Sans."
  - In PDF/A, a file attached to a page with FileAttachment, or embedded
    with NewEmbeddedFile and not carried with AddAssociatedFile: PDF/A-1
    carries no files, PDF/A-2 only documents of PDF/A, and PDF/A-3 each with
    its relationship, which AddAssociatedFile writes.

  Code that wrote such documents gets the error now: an embedded font in
  place of a core one, and AddAssociatedFile in a document of PDF/A-3, make
  them what they say they are.
- The soft hyphen, U+00AD, in the four ports: it takes no width and is not
  drawn, and a word too wide for its line breaks at its last soft hyphen
  that fits, the line ending with a hyphen, in TextBlock, TextFrame (and so
  Markdown) and a table's cells, before it is broken between its
  characters. It was drawn as a hyphen wherever it was, and never broken
  at. In a TextColumn it is not drawn, and not broken at. Text that has soft
  hyphens wraps otherwise than before.
- The passwords of AES-256 encryption, in the four ports, are prepared with
  SASLprep (RFC 4013), as PDF 2.0 asks: the spaces other than the ASCII one
  are spaces, the characters "commonly mapped to nothing", as the soft
  hyphen, are left out, and the text is in normalization form KC of Unicode
  3.2, so that an accented letter typed as one character or as a letter and
  an accent, or a full-width letter and its ASCII one, is the same password,
  as Acrobat takes it. A password of a control, private use or other
  character that SASLprep prohibits, or that mixes right to left with left
  to right text, is refused. An ASCII password is the same as before, and a
  PDF whose password was taken as it was still opens with it. The tables
  are written from RFC 3454 and Unicode 3.2 by util/gen-saslprep.py, with
  no library added.
- The glyphs drawn with a font are a set of bits, in the four ports: 128
  bytes for IBM Plex Sans, sized by the font's glyphs, where an array of a
  boolean for each of the 65,536 glyph numbers was 64 KB for every font of
  every document. It grows for a glyph past the font's count, so it keeps
  what the array kept; the subsetters read it as an array of the font's
  glyphs. A page in three fonts allocates 0.2 MB less, with the same PDF.
- The characters of a font and their glyphs, in the four ports: the
  character-to-glyph table is two levels, 256 blocks of 256 characters made
  only where the font has a character, some kilobytes for IBM Plex Sans
  where an array of 65,536 glyphs was 256 or 512 KB for every font of every
  document; and the character of each glyph, which the ToUnicode map
  writes, is found once when the font is read, where writing the map walked
  the 65,536 characters twice. A page in three Plex fonts takes a quarter
  less time in Go, 3.0 ms where it took 4.0, and allocates 4.2 MB where it
  allocated 7.6, with the same PDF, byte for byte.
- Swift: a page no longer keeps its PDF alive, as the page and its PDF held
  each other and no document was freed. A page whose PDF the caller dropped
  stops with "The PDF of this page was freed: keep the PDF while drawing on
  its pages."; Go, Java and C#, which collect the two together, are as they
  were.

### Removed
- **The `.stream` font format, in the four ports**, as no client uses it: the
  reader of `.otf.stream` and `.ttf.stream` files, the font tool that made
  them (`util/generate-stream-fonts-files.sh`, with the Zopfli it ran), and in
  Go `NewFontStream1`, which `NewFont` replaces, and `NewFontStream2`, which
  `NewFontForObjects` replaces. A stream file gives "Invalid font file: not an
  OpenType or TrueType font: PDFjet reads .otf and .ttf fonts."; a path to a
  bundled one opens its `.ttf`, as above. The code that writes the objects of
  a font, shared by every font, is `FontWriter`, and that of a font added to
  an existing PDF `FontObjects`, in place of `FontStream1` and `FontStream2`;
  a font of CFF outlines is written by them too, byte for byte as before.

### Fixed
- Reading untrusted PDFs, in the four ports: three ways a file of a few
  kilobytes could take gigabytes of memory, past the 256 MB that the
  decoded streams of a PDF may take, are closed. A page's /Contents array
  that names one stream again and again is charged for each copy, not once:
  a 17 KB PDF made 1 GB of page content. An object stream gives no more
  objects than its /N says, and each takes its share of the limit: a header
  of millions of "1 0 " made 5.7 GB of objects from 65 KB. A
  cross-reference stream that lists more objects than the file has room
  for, or an offset past its end, is set aside as it is read, and the
  objects are found by scanning, as for a broken table: one of 64 MB of
  entries took 4.5 GB. Each with a test.
- A stamp in an encrypted document, in the four ports: its stream was
  written unencrypted, so readers decrypted plain bytes and lost the stamp
  (pypdf: "Invalid padding"; Poppler drew a blank page). It is encrypted as
  every other stream is.
- A slice of a DonutChart that is NaN, from 0/0 in an empty category of
  real data, infinite, or so large that its 360ths overflow, made the arc's
  loop endless until memory ran out, in the four ports. Such a slice is
  left out, and the others share the circle.
- Java in a Turkish or Azeri locale: the names of the core fonts were
  upper-cased by the locale's rules, "i" to "İ", so the page's font did not
  match its resource and its text was not drawn. They are upper-cased by
  the rules of no locale.
- Java and C#: an image of a JPEG is made three times faster. The
  check that the file is not cut short read the rest of the file a byte at a
  time from a stream, a synchronized read() in Java; it scans the bytes
  themselves now. A 600 KB photo took 3.3 ms in Java, 1.1 now, and
  the 20 pages of it in the benchmark against OpenPDF 1.2 ms, not 3.7.
- Java and C#: an image or a font read from a stream allocates its size
  and no more. `Content.getFromStream` grew an array from 32 bytes by
  doubling and copied it out, about three times the size of the file; it
  reads into an array of the size a file or bytes in memory say they hold,
  and returns it without a copy. A document of the 600 KB photo allocates
  3.2 MB, not 5.2, and peaks at 81 MB, not 112; a page with Noto Sans 7.6
  MB, not 9.6.
- Go: the font subsets, the ToUnicode CMaps and the embedded files are
  compressed with the pooled writers that page content and images use,
  rather than a new zlib writer of about a megabyte for each stream. A page
  of 30 lines in three IBM Plex Sans fonts takes 3.9 ms, not 5.5, and
  allocates 7.2 MB, not 13.0; the PDFs of the 57 examples are the same, byte
  for byte.
- Swift: a font read from a file or a stream is read once, 64 KB at a time,
  and parsed from those bytes, where it was read in 4 KB pieces, copied into
  a Data and read again from a stream of it. A page of 30 lines in three IBM
  Plex Sans fonts takes 3.8 ms, not 4.0; the PDFs of the examples are the
  same.
- Found by a code review of every change since 9.0.3, on 9 October 2026, and
  fixed in the four ports, each with a test:
  - A font made with `new Font(pdf, ...)` before `addObjects` or
    `addResourceObjects` took the number of an object of the existing PDF,
    which was lost; it is refused as before 9.0.5, and the objects come first.
  - A font added to the objects of an existing PDF was left out when the
    objects were merged with `merge` rather than added; its text was lost.
  - The linked marker of a wrapped table cell in a tagged document was not a
    Figure, and lost its description.
  - An SVG with a prefix it does not declare, as `xlink:href` without
    `xmlns:xlink` in SVG copied from web pages, is read again, as before
    9.0.5. Reading the XML of an SVG or an invoice takes a half to a sixth of
    the memory it took, and is faster; a document has a million elements at
    most (`MAX_ELEMENTS`), so that 20 MB of tiny elements cannot take
    hundreds of megabytes.
  - The lines of a wrapped table cell are evenly apart with
    `setCellPadding`: its bottom padding is under its last line, where it was
    between the first two. Tables of the default padding are drawn as
    before.
  - `addCJKParagraph` makes one paragraph of its lines, not one of each: a
    screen reader reads one paragraph, and the space between paragraphs is
    after it only. The lines are set with the font's line gap, as those of
    any paragraph: IBM Plex Sans SC's sets them 2 em apart. Example_44 sets
    its Chinese at a line spacing of 1.2.
  - Fonts made wrong are embedded whole, where they made a subset of
    gigabytes, threw out of `complete()`, or trapped in Swift: glyphs whose
    outlines overlap, a table twice, a loca format other than 0 or 1,
    offsets past 2^31, a CFF accented letter drawn with seac (which drew
    blank), and a format 12 cmap cut short. A format 12 group that starts
    before the font's first character keeps its glyphs.
  - `ImageSize` refuses a BMP palette of more than 256 colors and a PNG of
    no image data, as `Image` does.
  - SVG numbers are read by SVG's grammar alone in the four ports, so
    `0x1p3` or `1f` is refused in all of them.
  - Swift: the language of a word in a paragraph of an encrypted tagged
    document was encrypted twice; a width past 65535 in a font trapped; a
    font added to objects that were never added to a PDF was never freed.
- Text is drawn faster, in the four ports, and much faster in Swift: a
  TextLine's fallback font is its font unless one is set, and each of its
  characters was asked which of the two fonts has it; a fallback font that
  is the font itself is now drawn and measured as no fallback font, the same
  PDF. In Swift the loops that look up glyphs and widths read the font's
  tables once a string, not once a character, as each read of a class's
  property is checked at run time. 100 pages of 50 lines of IBM Plex Sans:
  Swift 65 to 17.5 ms, Go 21.8 to 15.4, C# 19.0 to 15.5, Java 14.8 to 13.9.
- The text of a table cell that wraps is one paragraph in a tagged document,
  PDF/UA or PDF/A of level A, in the four ports: each of its lines was a P of
  its own, so a screen reader read one cell as several paragraphs. A line
  that goes on to the next page begins a P of its own there. veraPDF and PAC
  did not flag it; found by the review of PDFjet Forms.
- A CID-keyed font of CFF outlines, as Source Han Sans and Noto Sans CJK are,
  drew the wrong glyphs or none, as a PDF looks its glyphs up by CID and
  PDFjet wrote glyph numbers: its CFF is written with the identity charset,
  whole or subset, and passes veraPDF's PDF/A and PDF/UA checks.
- A font whose format 4 character map is empty and whose characters are in
  its format 12 map alone, as IBM Plex Sans TC's `.ttf`, drew every
  character as the .notdef box, in the four ports: the format 12 map is read
  too, for the characters the format 4 map leaves without a glyph. Every
  font read before maps as it did.
- A PDF/A-1a document with an annotation passes veraPDF, in the four ports:
  PDF/A-1 is of PDF 1.4, which does not have the structure type Annot of PDF
  1.5, so in PDF/A-1 the role map maps it to Span (rule 6.8.3.4); other
  levels are as before.
- The language of a TextLine is kept in a TextColumn, in the four ports: the
  paragraph is one structure element for its words, and dropped the language
  of the text line they came from, so a screen reader read Example_29's
  Greek as English. Each word of another language says so in its marked
  content, `/Lang`, which PDF/UA and the readers honor; a paragraph of mixed
  languages keeps each. Example_29 marks its Greek as Greek.
- In a PDF/UA or PDF/A document, the configuration of the optional content
  groups, the layers, has a `/Name` and no `/AS`, as PDF/UA (7.10) and
  PDF/A ask, in the four ports: veraPDF refused a tagged document with
  layers. `/AS` is what says when a group is printed, so in such a document a
  group set not to print prints, as `setPrintable` now says; a plain PDF is
  as before. Example_46, the map of layers, is a PDF/UA document now, and
  passes veraPDF's PDF/UA-1 check in the four ports.
- The link of a word drawn with its space ends at the word, in the four ports:
  a word of a TextColumn, or of a justified TextFrame row, is drawn with the
  space after it, and its link box reached one space past it. The box of a
  TextLine's link now holds the text that shows, without the spaces at either
  end of it; a text of spaces alone keeps its whole width.
- A square, circle or polygon annotation with an opacity below 1 in a
  document of PDF_A_1A or PDF_A_1B is drawn opaque, in the four ports.
  PDF/A-1 has no transparency, and the annotation had `/CA` and an
  appearance with an ExtGState of `ca` and `CA` below 1, which veraPDF
  refuses. The opacity is ignored there, not refused, as setOpacity says;
  the other levels keep it.
- An OpenType or TrueType font whose table directory lists `hmtx` before
  `hhea` is read as any other, in the four ports. The number of advance
  widths is in `hhea`, so `hmtx` is now read after the directory: Java and C#
  failed on such a font, and Go and Swift drew every glyph 0 wide. A font
  without `hhea` is refused, "no advance widths". Real fonts list their
  tables in order; found by replaying the Go fuzz corpus in the other ports.
- SVG path data with a number that is not one, such as `-.` or `2.@5`, is
  refused in words, "Invalid path data", in Java and C# as in Go and Swift,
  where Java failed with a NumberFormatException and C# with a
  FormatException. Found by replaying the Go fuzz corpus.
- A table whose header rows take more than half of the page under its top
  draws them on the first page alone, in the four ports, instead of again on
  every page, where they left a line or two of the body to each page: a
  Markdown table of 20 rows, its header tall in 60 narrow columns, ran to 553
  pages and 2 GB (found by Jazzer, fuzzing the Java port). Headers of the
  usual height repeat as before. In a PDF/UA document, a header drawn on a
  page where no row of the body fitted under it is no longer tagged as TH
  cells a second time on the next page.
- A PDF whose cross-reference table is broken, read by looking for its
  objects, keeps a stream whole when its bytes hold an object, as an
  unfiltered embedded PDF has, in the four ports. The search for the
  stream's endstream stopped at the next "number generation obj", so a stream
  with a wrong /Length was cut short there, the object in it was read as one
  of the PDF, and its endstream was taken for the stream's own. The search
  now skips the streams and objects in the stream's bytes, within the
  reader's budget; the 2,865 inputs of the Go fuzz corpus read as before.
- A JPEG whose only fault is a missing end-of-image marker (FF D9) is drawn,
  in the four ports, as viewers draw it, where 9.0.3 refused it as cut
  short. The entropy-coded data of each scan of a sequential JPEG is walked,
  its Huffman codes read block by block, and when every block of the image is
  there the marker is added to the data embedded. A JPEG that stops in its
  scan, as an upload or a copy cut short, is refused as before, and so is a
  progressive one without the marker.
- Reading untrusted PDFs, in the four ports: the output of ASCII85Decode
  and ASCIIHexDecode is counted against the 256 MB that the decoded streams
  of a PDF may take, as the other filters' is; a stream of 'z's after
  FlateDecode decoded to 800 MB from 203 KB. Adding the resources of the
  pages finds each name at once, not by a look through all the names added
  before, and adds a page that /Kids lists again once: 1,000 pages of 2,000
  fonts took 36 seconds, and one page listed 2,000 times two minutes.
- Reading and merging pages that inherit a large /Resources, in the four
  ports: a /Resources dictionary of more than 64 tokens in a node of the
  page tree is shared by the pages under it, not copied into each, and
  Merge writes it once, as an object that every page refers to. 1,000 pages
  under a node of 20,000 fonts, a PDF of 242 KB, took 2.9 GB of memory and
  36 seconds, and Merge wrote 189 MB; now 0.05 seconds, and a merged PDF of
  about the source's size. A page's GetDict() shows such resources no
  longer; GetResourcesObject() gives them, and adding a resource to the page
  gives it a copy of its own first. A smaller /Resources is copied into each
  page, as before.
- Text, in the four ports: a character that Unicode does not draw without a
  glyph, as the variation selector after ™ or © in text pasted from a
  phone, a zero width space or a word joiner, takes no width and draws
  nothing, where it was a box. A Stamp leaves out the marks of direction
  and the joiners its font has no glyph for, as a Page does. A right to
  left TextBlock cut to its height keeps the start of its last line, its
  "..." at the left, where the line ends in the reading.
- Tables, in the four ports: drawing a table leaves the caller's cells as
  they were; a long text was cut to its first line in the cell, and a table
  made again of the same cells lost the rest.
- Code 39, in the four ports: a '*' in its data, the character that starts
  and ends the code, and no data at all are refused when the barcode is
  made, as the other symbologies refuse what they cannot hold; each drew a
  code that no scanner reads.
- Markdown, in the four ports: a table has at most 64 columns, and a wider
  header is a paragraph. Each row is padded to the header's columns, so a
  header of thousands of columns over rows of "|" took memory as the square
  of the text, 1.17 GB from 24 KB.
- AddObjects, in the four ports: objects and references of a generation
  other than 0, "1 1 obj" and "2 1 R", are written at generation 0, as the
  cross-reference table has them; a reader found no pages in such a PDF.
- Swift: an SVG arc whose radii, scaled to reach its end, overflow a Float
  is left out, as in the other ports, where Int(NaN) ended the program; and
  a carriage return and line feed in path data is white space.
- Swift on Linux: a missing file given to Content, SVGImage or ImageSize is
  refused without Foundation logging the CoreFoundation symbols it could not
  link.
- Images copied from a PDF with NewImageFromPDFobj, in the four ports: into
  an encrypted PDF, the stream and the strings of the image's dictionary, as
  the palette of an /Indexed color space, are encrypted, where they were
  copied as they were and viewers found no image. An image whose /ColorSpace
  or /DecodeParms refers to another object of the PDF it was read from, as
  an /ICCBased profile, is refused: the object is not copied with it, and
  the reference pointed at another object.
- TextFrame, and so Markdown, in the four ports: a word wider than the frame
  is cut between its characters as TextBlock cuts one, never before a
  combining mark or a Thai vowel that stays with the letter before it.
- Page sums of tables, in the four ports: a cell of a space inside a number,
  as ". 5", is not a number; Java threw on it, the other ports counted 0.
- Swift: a cell of a vertical alignment other than TOP, CENTER and BOTTOM
  makes complete() throw, where it ended the program.
- Encrypted documents, in the four ports: the strings of the system of
  characters of a CJK font, /Registry and /Ordering, and the /Name of the
  configuration of the layers of a document of PDF/UA or PDF/A, are
  encrypted, where a reader decrypted them to bytes of no meaning.
- The XMP metadata, of the document and of a font, in the four ports: the
  packet's header begins with the byte order mark, `begin="\uFEFF"`, which
  ISO 16684-1 asks of it.
- Reading a BMP, in the four ports: a row is read in pieces, so that a header
  of a width the file does not have takes no more memory than the file; a
  54-byte header took 256 MB before it failed at the end of its data.
- Reading a PNG, in the four ports: IHDR must be the first chunk, and the
  only IHDR. A tRNS before it was read for a gray image, and an RGB image
  was written with the /Mask of a gray one.
- GetFitChars, documented in Go and Swift: Go and Swift count in runes and
  Unicode scalars, Java and C# in UTF-16 chars, as each port's strings do.
- Tables, in the four ports: AutoAdjustColumnWidths measures a row longer
  than the first, where it indexed past the widths; a table whose first row
  is empty is drawn, its rows completed from the first row that has cells,
  and rows of no cells draw nothing, as no rows do.
- Text copied from a PDF, in the four ports: a glyph that a font draws for
  several characters maps to the character the document drew with it, not
  to the lowest of them. A Persian ۱۳۹۸ in Noto Sans Arabic was copied as the
  Arabic ١٣٩٨, the full-width slash of IBM Plex Sans TC as a division slash,
  and an ideographic space of the JP and SC fonts as an em space. A
  document that draws two characters of one glyph gives the first drawn for
  both.
- Swift: a document is freed once the caller drops it, after complete() or
  without it, with its pages, fonts, images, stamps, layers, bookmarks and
  links: a page and its PDF, a stamp, an optional content group and their
  PDF, a bookmark and its parent and the one before it, and a link and its
  structure element held each other. 500 one-page documents took 170 MB,
  and now take 28, as 20 do.

## v9.0.3 — 2026-10-08

### Added
- The viewers job of the Build workflow opens the example PDFs in the
  engines of Chrome, Firefox and Preview: PDFium, through pypdfium2, and
  pdf.js, in Node, on Linux, and Apple's PDFKit on macOS, with
  `.github/scripts/check-viewers.py`. Every page must open, render and give
  its text; none may render blank or look different from MuPDF's render,
  or lack a character MuPDF extracts from it. Example_30 is opened with its
  user and its owner password, and two more PDFs with a Cyrillic and a 200
  byte password. The renders are uploaded, with a contact sheet of each PDF
  beside MuPDF's. `check-examples.sh` runs the PDFium check.
- `PDFjet.dll` is strong-named, in the licensed and in the evaluation
  package alike: public key token `e66c1909913f295d`, so that strong-named
  applications can reference it. Its assembly version is 9.0.0.0, the same
  through 9.x, so that no binding redirects are needed, and its file and
  product versions are those of the release, 9.0.3; it said 0.0.0.0.
- PDFjet for .NET Framework 4.8, `net48/PDFjet.dll` in the .NET packages,
  beside the `PDFjet.dll` for .NET 8 and later, for applications that are
  still on .NET Framework, which cannot load a .NET 8 assembly. It is built
  from the same sources, unchanged, by `net48/` of the repository, and is
  strong-named with the same identity, `PDFjet, Version=9.0.0.0,
  PublicKeyToken=e66c1909913f295d`. It needs four small DLLs of Microsoft
  beside it, System.Memory and what it uses, which the package has. Its zlib
  streams are decoded by a C# translation of Mark Adler's puff.c, as .NET
  Framework has no ZLibStream, held to .NET 8's own by `net48/check-zlib.sh`.

### Deprecated
- The constructor of the Chinese, Japanese and Korean fonts that are not
  embedded: `Font(PDF, CJKFont)` in Java, C# and Swift, `NewCJKFont` in Go.
  The viewer must supply these fonts, Acrobat asks to download its Asian
  font pack for them and offers to make the file accessible, and a PDF with
  them cannot be PDF/A or PDF/UA. Use an embedded font, such as IBM Plex Sans
  JP, KR, SC or TC. It works as before, and is to be removed in v10. The
  booklet's snippet of CJK fonts uses the IBM Plex ones now.

### Changed
- Example_01 marks its Greek and Bulgarian text blocks with their languages,
  el and bg, so that a screen reader reads each in its language; NVDA spelled
  the Greek letter by letter and skipped the Bulgarian, reading both as
  English, the language of the document.
- The examples are tagged wherever PDFjet draws the whole page, as Acrobat
  offers to make an untagged PDF accessible when it opens it: Example_05
  draws the same words in five weights of IBM Plex Sans, as PDF/UA, in place
  of its kerning in Helvetica, which a PDF/UA document cannot use; Example_07
  is PDF/A-3a and PDF/UA-1 in place of PDF/A-3b, Example_34 PDF/A-1a in
  place of PDF/A-1b, and Example_44 PDF/UA. Left untagged: Example_37, 41,
  50 and 51, which stamp, merge, fill and split PDFs that are not tagged;
  Example_43, whose 2,546 pages are too many to tag; and Example_46, whose
  layer that is shown but not printed needs the /AS entry PDF/UA forbids.
- No example uses the Adobe CJK fonts any more, which are not embedded:
  Acrobat asks to download its Asian font pack for them, and offers to make
  the file accessible. Example_02, which drew Japanese, Korean, Simplified
  and Traditional Chinese with four whole embedded fonts, 12.6 MB, and took
  Acrobat seconds to open, draws the two Chinese now, with IBM Plex Sans SC
  and TC; Example_04, which drew a greeting in the Adobe CJK fonts, draws
  the Japanese and the Korean, with IBM Plex Sans JP and KR, both PDF/UA;
  and Example_44 draws its Chinese paragraphs with IBM Plex Sans SC in
  place of STHeiti SC Light.
- `go.mod` retracts v9.0.0 to v9.0.2, which the Go module proxy cannot
  serve, as the fonts in their tree make it time out: `go get` of the
  library at `@latest` takes v9.0.3 or later, and `go list -m -retracted`
  says why.
- `fonts/` and `data/` are no longer in the repository: they are the
  repositories [pdfjet-fonts](https://github.com/edragoev1/pdfjet-fonts) and
  [pdfjet-data](https://github.com/edragoev1/pdfjet-data), with the same
  files as before but `fonts/go.mod` and `data/go.mod`, at the commits
  `fonts-and-data.txt` pins. The build, run and test scripts fetch them the
  first time they run, the pinned commits alone, and move them when a pin
  changes; `get-fonts-and-data.sh` and `get-fonts-and-data.cmd` do it alone.
  The folders are ignored by git, and the examples, the tests and the
  scripts read them as before. The Go module proxy makes the module of a
  version from `git archive` of its tag, which has the whole tree and was
  487 MiB with the fonts, and it timed out, so `go get` of v9.0.0 to v9.0.2
  fails; the tree is now 8 MB zipped and the module 4 MB. They are not
  submodules, as SwiftPM checks out the submodules of a package it depends
  on. The Go module has no fonts: the Go section of the README says to
  clone pdfjet-fonts as `fonts` next to the program.
  `.packaging/package-java.sh` and `package-dotnet.sh` take the files of the
  two repositories at the pinned commits, and the workflows fetch them.

### Fixed
- The spaces of wrapped text are in the PDF, so that a screen reader and a
  text extractor see every word break, as tagged PDF asks. `TextBlock`
  draws the space it wrapped a line at after the line, where it dropped it,
  so the last word of one line and the first of the next ran together for a
  reader that does not guess the break from the gap, and NVDA paused at
  every line end; and a justified row of `TextFrame`, drawn word by word,
  draws each word with the space after it, where it drew no space at all,
  also where the space moved to the next text line, as it does after a link,
  an underline or a change of font. A row of `TextFrame`, justified or not,
  that ends where such a space moved and its word goes on the next row ends
  with the space, where the two words ran together.
  The space is drawn but not measured: the alignment, the stretch of a
  justified row, the underline and the strikeout are as before, and the
  pages of the 62 example PDFs render the same pixel for pixel, in the four
  ports. `TextColumn`, and the rows of `TextFrame` that are not justified,
  had their spaces already. A line broken inside a word, at a zero width
  space, or in Chinese, Japanese or Korean text has no space to draw, nor
  does right to left text, as before.
- The Go port computes the same on arm64 as on amd64 and as the Java, C#
  and Swift ports. The Go spec lets the compiler fuse `x*y + z` into one
  fused multiply-add, rounding once, and Go's arm64 compiler did so in 333
  places of the library, among them `TextBlock` where it breaks lines and
  `Cell` where it measures rows, so a result could differ in its last bit
  and, at a boundary, break a line otherwise on ARM servers such as AWS
  Graviton. Every float multiplication of the Go port, and every division
  by a constant, which the compiler makes a multiplication, is now written
  in a conversion to its own type, `float32(a*b)`, which rounds it, as the
  spec provides. On amd64 nothing changes: the PDFs of the 57 examples are
  the same byte for byte. `check-no-fma.sh`, run by the Build workflow,
  fails if a fused multiply-add appears in the Go port's arm64 code again;
  it needs no ARM computer.
- `Color.transparent` leaves the color as it was in every int-color setter:
  23 of the 36 kept the low 24 bits of -1 and drew white, a white line for
  `Line.SetStrokeColor(Color.transparent)`, where the other 13 already left
  the color unchanged. Any other value is read as before. The four ports,
  with a test in each of all 23 setters.
- An SVG that writes its `viewbox` in lower case, as files saved from web
  pages do, is drawn with it: SVGImage read only `viewBox`, so such a file
  was drawn with no viewBox at all, its paths neither scaled nor moved.
  `viewBox` wins when a file has both. The four ports, with a test in each.
- A word wider than its table column starts a line of its own, then is
  broken between its characters: after the words before it, or the end of
  another such word, it shared their line, "listic e" where the cell read
  "...listic expialidocious". The four ports, with a test in each. Found by
  the review of PDFjet Forms, 5 October 2026.
- A JPEG cut short, as in an upload or a copy, is refused, "Error: The
  JPEG is cut short: its image data has no end.", where it was embedded as
  it was and drawn as far as it went, or as noise: the end-of-image marker
  must follow the header of its scan. A thumbnail's own marker before the
  scan does not count, and data cameras append after the end is left as it
  is. Found by the review of PDFjet Forms, 5 October 2026; the four ports,
  with a test in each.
- An encrypted PDF, AES-256, opens in Adobe Acrobat and Acrobat Reader,
  which said it could not decrypt it: the encryption dictionary has
  `/Length 256`, which PDF 2.0 leaves optional for `/V 5` but Acrobat asks
  for. MuPDF, PDFium, pdf.js and Preview opened it without. Found on 1
  October 2026 with Example_30; the four ports, with a test in each.
- `Markdown` tags a code block as a paragraph that holds its `Code`, not as
  a `Code` right under the document: `Code` is an inline element, and PAC
  warned of it as possibly inappropriate.
- Every annotation that is not a link, a file attachment, a note
  (`TextAnnotation`), a polygon, a square or a circle, has an appearance
  stream (`/AP`), in all four ports, as it had in PDF/A alone: without one
  each viewer drew it its own way, and PDFium, so Chrome, drew no file
  attachment icon and no polygon, and drew the note icon above its place.
  A square, a circle and a polygon are drawn in their fill color with their
  opacity, a file attachment as its push pin or paperclip icon, and a note
  as a speech bubble, each icon in a white box with a black frame. The
  `/Rect` of every annotation, links too, is written from its lower left
  corner to its upper right one: its y values were the wrong way round,
  but for a polygon. The PDFium check of `check-examples.sh` no longer
  makes an exception of Example_06.
- `PDFobj.getData` of an image read from a PDF undoes the predictor of its
  `/DecodeParms`, the PNG predictors and the TIFF one, in all four ports, as
  it does for any other stream: the data of an image with the PNG predictor
  was its rows each with the filter type and the filter of its row, and that
  is every opaque PNG PDFjet embeds since v9.0.2, whose IDAT data is
  embedded as it is. The predictor was skipped to save the time of undoing
  it when the PDF was read, and a stream is decoded now when its data is
  asked for. What copies an image, `Image` from a `PDFobj`, `merge`,
  `addObjects` and `addResourceObjects`, copies its stream and its
  `/DecodeParms` as before, and the PDFs it writes do not change. The data
  of an `/Indexed` image is still its indexes, as its stream is.
- `PDF.read` reads a PDF in time and memory in proportion to its size, in
  all four ports, where a PDF of a few megabytes made to be slow to read
  took minutes. The endstream of a stream whose `/Length` is wrong, which
  the stream ends at, as MuPDF and pdf.js read it, is looked for in the
  stream's own object, up to the next object that the cross-reference
  section lists or, in a PDF read by looking for its objects, up to the next
  object in the PDF: it was looked for to the end of the PDF, for every such
  stream, and 8,000 streams with no endstream, 8.5 MB, took 25 seconds. A
  stream with no endstream in its object keeps its `/Length`, where the
  endstream of a later object made it the bytes up to there, as in two
  files of the pdf.js corpus, and the objects after a stream with no
  endstream are read in a PDF read by looking for its objects. What the
  reader may read in all to find the objects of the cross-reference
  sections and the endstreams is eight times the length of the PDF, and a
  search past it keeps the `/Length`: a thousand sections chained by
  `/Prev` with no startxref after them, or a table that lists one object or
  one stream thousands of times, each read to the end of the PDF, and a
  PDF of a megabyte took half a minute, or ran out of memory in Java. A
  section past it is not read, and the PDF is read by looking for its
  objects. The objects of an object stream are read in no more than its
  length in all, as those whose offset is listed again were read again: a
  file of 3 kB took seconds.

## v9.0.2 — 2026-09-28

Released three days before the Oct 1 planned for it, cut from master: the
PDF/UA and PDF/A work, a heading in every PDF/UA example, a code review of
the four ports and the fixes it found, and faster PNG images. The producer
string is `PDFjet v9.0.2` in all four ports. Nothing of the public API of
v9.0.1 is removed and no signature changes; what is added is under Added,
and the input now refused, which callers should know about, is under
Changed.

### Added
- A table read from a file that returns its error, in Go only:
  `ReadTableFromFile(f1, f2, fileName)` returns the table, or the error of a
  file that cannot be read or has a quoted field that is not closed, as the
  constructors of Java, C# and Swift throw it. `NewTableFromFile`, which
  returns no error, panics with it, as it did.
- A table that starts under a heading, in all four ports: `Table.drawOn(pdf,
  first, pages, pageSize)` (`DrawOnPagesFrom` in Go) starts the table on a
  page of your own, under the top margin `setFirstPageTopMargin` gives it,
  and goes on to new pages; and `BigTable.setFirstPage(page, y)` starts a big
  table at y on a page of the PDF, with the next pages at the y of its
  location, and the footer counting them all, each at its own size, in "Page
  i of N"; a first page with no room for the header and a row is left for a
  new one. Example_08 and Example_57 have a heading on their first page with
  them, which PAC asks a document for.
- A description of a point, in all four ports: `Point.setAltDescription`,
  what a point that is a link stands for, like the country a point of a
  chart is. In a tagged document a point of a chart, or of a cell, that is a
  link is a figure of its own in its Link, described by it, or by its URI
  when it has none.
- Bookmarks of the headings, in all four ports: a tagged document, PDF/UA or
  a PDF/A of level A, that has headings, H1 to H6, and no bookmarks of its
  own has a bookmark for each heading, under the heading before it of a
  higher level, which goes to the top of the heading on its page. A reader
  shows them as the outline of the document, and PAC asks a document with
  headings for them. The headings are those of a `TextLine`, a `TextBlock`
  and a paragraph of a `TextFrame` or a `TextColumn`, which Markdown draws
  its headings as; one inside an artifact, like a running header, is not
  one, and a heading that goes on into the next frame has one bookmark. A
  document with bookmarks of its own keeps them, and one that is not tagged
  has none made.
- The bounding box of a figure, in all four ports: `Page.setFigureBoundingBox`,
  for a figure drawn with `addBDC` and `addEMC` around other drawing, such
  as the content of another PDF. The figures of the library, images, SVG
  images, charts, barcodes and QR codes, set it themselves.
- The structure type of a text block, in all four ports:
  `TextBlock.setStructureType`, as `TextLine` has it, so that a text block
  of a tagged document is a heading, `StructElem.H1` to `H6`, rather than a
  paragraph, which is what it is unless it is set.
- A description of a barcode, a QR code, a Data Matrix and a PDF417
  barcode, for a screen reader, in all four ports: `setAltDescription` of
  `Barcode`, `QRCode`, `DataMatrix` and `PDF417`, as `Image` and `SVGImage`
  have it. In a tagged document, PDF/UA or a PDF/A of level A, a described
  barcode is one figure of that description, its bars and its digits with
  it, and a described QR code, Data Matrix or PDF417 barcode is a figure
  too. Without one, a barcode is drawn as before: its bars are decoration
  and its digits text, and a QR code, a Data Matrix and a PDF417 barcode are
  decoration, which a screen reader skips.
- A compliance level that is PDF/A-3a and PDF/UA-1 at once, in all four
  ports: `Compliance.PDF_A_3A_UA_1`, after `PDF_A_3B`, so that the values of
  the others do not change. The content is tagged, and follows the rules of
  PDF/UA, as with `PDF_UA_1`, and the document is kept as PDF/A-3, which may
  carry files, as an e-invoice carries its XML, and may not be encrypted. The
  metadata names both parts, and describes the PDF/UA identification schema
  in the extension schemas PDF/A asks for, in the list of a description
  added with `addMetadata`, such as that of Factur-X, when it has one, as the
  metadata may have only one. Example_55, the Factur-X invoice, is of this
  level, and check-examples.sh checks it with veraPDF's PDF/A-3a and
  PDF/UA-1 profiles, which pass it in the four ports.
- ITF-14 barcodes, the GTIN of a carton or a case, in all four ports:
  `Barcode.ITF_14`, with the 13 digits of the GTIN, to which the check digit
  is added, as for UPC-A and EAN-13. The 14 digits are in Interleaved 2 of 5,
  wide bars and spaces 2.5 times the narrow ones, and a frame of bearer bars
  four modules thick is around the bars and their quiet zones of ten
  modules, as GS1 asks of a barcode on corrugated board; the text is under
  the frame, and the barcode turns as the others do. A text of another
  length, or not of digits, is refused. ZXing reads the barcodes, in each
  direction, with the symbology identifier `]I0` of ITF. Example_11 draws one
  on a page of its own.
- A GS1 Digital Link of GS1 data, the web address an ordinary QR code
  carries, in all four ports: `GS1.digitalLink(domain, data)` in Java,
  `GS1.DigitalLink` in C#, `pdfjet.GS1DigitalLink` in Go and
  `GS1.digitalLink(domain:data:)` in Swift, whose `GS1` is public now.
  `(01)09506000134352(10)ABC123(17)261231` at `https://id.gs1.org` is
  `https://id.gs1.org/01/09506000134352/10/ABC123?17=261231`: the primary
  key of the data first in the path, then its qualifiers in the order of the
  GS1 Digital Link standard, such as (22), the batch (10) and the serial
  (21) of a GTIN, and the other fields in the query, in their order, each
  value percent-encoded where a web address needs it. The data is checked
  as for the barcodes, and refused in the same words in every port when it
  has no primary key or two, both (254) and (7040) of a GLN, or a domain
  that does not start with https:// or http://. Example_21 draws one in a
  QR code.
- GS1-128 barcodes, as the labels of pallets, cartons and shipments carry, in
  all four ports: `Barcode.GS1_128`, with the text written as GS1 data is
  read, such as `(00)106141412345678908` or `(01)09506000134352(10)ABC123`,
  and drawn so under the bars. The barcode starts with FNC1, which makes it
  GS1-128, ends a field of no set length with FNC1 when another follows, and
  puts runs of four digits or more in code set C, two digits to a codeword,
  so that an SSCC is 156 modules wide where code set B would make it 266. It
  takes the data GS1 DataMatrix takes and refuses it in the same words, and
  refuses data of more than the 48 characters a GS1-128 barcode holds. The
  GS1 parser is shared by the two, and is internal in every port: in Java
  `com.pdfjet.internal.GS1Parser`, which is public only because the barcodes
  are in packages of their own, and is not API. ZXing reads the barcodes with
  the symbology identifier `]C1` of GS1-128. Example_11 draws one. The
  internal class `GS1_128` of Java, C# and Swift, which held the table of
  Code 128, is `Code128Table`.
- GS1 DataMatrix, as medicines, medical devices and, more and more, retail
  goods carry, in all four ports: `DataMatrix.fromGS1(data)` and
  `fromGS1(data, shape)` in Java, `DataMatrix.FromGS1` in C#,
  `datamatrix.NewGS1DataMatrix` and `NewGS1DataMatrixWithShape` in Go, and
  `DataMatrix(gs1:)` in Swift. The data is written as people read it, each
  Application Identifier in parentheses and its data after it, such as
  `(01)09506000134352(17)261231(10)ABC123`. The symbol starts with FNC1,
  which makes it GS1, is in ASCII encodation, two digits to a codeword, and
  ends a field of no set length with GS when another follows. Data that is
  not GS1 is refused, with the same words in every port: an Application
  Identifier that is not two to four digits or has no data, a character
  GS1 does not allow, data longer than 90 characters, a field of set length
  whose data is not that many digits, and a wrong check digit of an SSCC
  (00), a GTIN (01) or (02), or a GLN (410) to (417). ZXing reads the
  symbols with the symbology identifier `]d2` of GS1 DataMatrix. Example_14
  draws one.
- A document can carry files with it, as PDF/A-3 does: an invoice carrying
  the same invoice as data, a chart carrying its numbers.
  `PDF.addAssociatedFile(embeddedFile)` adds one, which writes `/AF` and the
  name tree of `/EmbeddedFiles` in the catalog, in all four ports
  (`AddAssociatedFile` in C# and Go). The file is embedded with the
  constructor of `EmbeddedFile` that takes what it holds, how it relates to
  the document — `Relationship.SOURCE`, `DATA`, `ALTERNATIVE`, `SUPPLEMENT`
  or `UNSPECIFIED` — and what it is, which PDF/A-3 asks for of each of them;
  the file specification writes `/Subtype`, `/Params` with the size and the
  date, `/AFRelationship` and `/Desc`, and the date is encrypted with the
  document. A document of PDF/A-1 or PDF/A-2 refuses the file rather than
  write one it may not carry, and a document of PDF/A-3 refuses one without
  a media type. veraPDF reads a document of PDF/A-3b that carries a file as
  valid.
- `PDF.addMetadata(rdfDescription)` adds a description to the XMP metadata of
  the document, for a standard that asks for properties of its own, such as
  the invoice standards that say which of the files the document carries is
  the invoice, in all four ports (`AddMetadata` in C# and Go).
- `Markdown` draws a Markdown text on as many pages as it needs, in all four
  ports: headings of `#` and of underlines, paragraphs with the inline markup
  of `Markup`, bullet and numbered lists that nest, block quotes, fenced and
  indented code, thematic breaks, the tables of GitHub's Markdown, and images
  alone in their paragraph. A heading keeps a line of the text after it on
  its page, and a table, a list, a quote or code goes on onto the next page.
  In a PDF/UA document the headings are H1 to H6 with no level skipped, and
  the lists, quotes, code, tables and images are tagged as such; one that
  goes on over pages is one structure element. Images are read only from the
  directory `setImageDirectory` names, never from an absolute path, a URL or
  one with `..`, so that a text from anyone reads no other file, alike in the
  four ports; a source that ends in `/` or `/.` names a directory, and an
  empty image directory is the working directory. HTML is drawn as the text
  it is, and a line break is a space. The blocks nest at most 32 levels, so
  that the text is read in time linear in its length, and quotes and lists
  nested deeply leave their text at least ten sizes of the text wide.
  `StructElem.BLOCKQUOTE` and `StructElem.CODE` are new.
- `Markup` makes paragraphs of text with the inline markup of Markdown,
  `**bold**`, `*italic*`, `***bold italic***`, `` `code` `` and
  `[links](url)`, in the fonts given for each, in all four ports:
  `paragraph(text)` makes one paragraph and `paragraphs(text)` one of each
  part between empty lines. A backslash makes a mark text, and so is a mark
  with no match, such as the `*` of `2 * 3`. Emphasis can be inside a link,
  and neither inside code. The runs of each style are joined with
  `Paragraph.addJoined`, so the punctuation after a word in another style is
  next to it. Headings, lists and the other blocks of Markdown are not read.
  The markup is read in time linear in the length of the text, and the Go
  port fuzzes it; the Go and the Java parsers give the same paragraphs for
  200,000 random texts.
- `Paragraph.addJoined(textLine)` adds a text line with no space before
  it, so that a word can change its font or its color partway through: a
  word in bold followed by a comma in the regular font, or a link that ends
  before the period after it, in all four ports (`AddJoined` in C# and Go).
  `TextFrame` and `TextColumn` do not break a row between the two, unless
  the word they make is wider than the frame or the column, and a justified
  row does not widen the join. A text line that starts with a space, or
  follows one that ends with a space, is apart from the text before it, as
  with `add`, and text in Chinese, Japanese or Korean is not joined. `add`
  is as it was: it puts a space between the text lines.
- `TextFrame.drawOn(pdf, pages, pageSize)` draws the text on as many new
  pages as it needs, a whole book at once, as `Table.drawOn` draws a table,
  in all four ports (`DrawOnPages` in Go). The frame has its location and its
  width on each page, and its height, or with none it reaches down to the
  margin its location leaves at the top, so text at 72, 72 keeps 72 points
  free at the bottom too. The pages are created detached and added to the
  list, so that a footer or a page number can be drawn on each before they
  are added to the PDF. A frame with no height whose location is in the
  bottom half of the page, which would leave the text no room, is refused.
  A frame drew one page and kept the rest before, and the caller had to make
  the next page and draw it again.
- `Table.setAlternateRowColor(color)` colors every other row of the body of a
  table, the second, the fourth and so on, counting the rows of the data and
  not those of each page, as `BigTable.setShadingColor` shades its rows, in
  all four ports. The lines a row wraps into have its color, a cell with a
  background of its own keeps it, and the header and footer rows are not
  striped. `setHeaderRowStyle(font, textColor, backgroundColor)` and
  `setFooterRowStyle` give the header and the footer rows a font, a text
  color and a background at once, where each cell was colored before; a null
  font or `Color.transparent` keeps what the cells have.
- `Table.setColumnWidthsInPercent(percents...)` and `Table.setWidth(width)`
  share the width of a table among its columns by percentages, in either
  order of the calls: the columns without one share what is left of 100, and
  the widths add up to the width of the table. `Table.fitToWidth(width)`
  shares it by the widths the columns have, after `autoAdjustColumnWidths` by
  the text each holds, and `setWidth` does the same when there are no
  percentages. In all four ports.
- `Table.setPageSum(row, column, decimals)` and `Table.setRunningSum(row,
  column, decimals)` make a cell of a footer row show the sum of its column,
  in all four ports: over the rows of each page, the page total, or over all
  the rows up to the end of each page, the total carried forward, which is
  the total of the table on its last page. The numbers are read as
  `rightAlignNumbers` reads them, with commas and apostrophes between the
  thousands and a period before the decimals; a cell that has no number is
  left out. The sums are exact, with the number of decimals rounded half away
  from zero and commas between the thousands, and the same text in every
  port; the numbers of a column are read once for each draw, so a running sum
  over 40,000 rows takes a tenth of a second.
  `Table.setBroughtForwardSum(row, column, decimals)` makes a cell of a
  header row show the total of the pages before, which the running sum shows
  at the end of the page before; a header row with such a cell is drawn from
  the second page on.
- `Table.setNumberOfFooterRows(n)` makes the last rows of the table data
  footer rows, in all four ports: they are drawn again at the end of the
  table on every page, under the last row the page holds, as the header rows
  are drawn again at the top, and a page leaves room for them. A footer row
  whose text wraps is drawn whole. In a PDF/UA document the footer rows are
  rows of the table once, where it ends, and artifacts on the other pages.
- `Table.keepRowWithNext(index)` keeps a row on the same page as the next
  row, as a heading row is kept with the rows under it, in all four ports: a
  page break does not fall between them, and moves both to the next page.
  Rows kept with the next one one after another are kept together, and a
  row whose text wraps is kept whole. Rows kept together that are taller than
  a page are drawn from where they start, each on its own, as if they were
  not kept together.
- `Font.getLineGap(fontSize)`, the space a font puts between its lines, read
  from the `hhea` table of an `.otf` or `.ttf` file and from a `.stream` file,
  in all four ports. `TextBlock`, `TextColumn` and `TextFrame` put it between
  lines, so text looks as its font was made to look without a line spacing.
  Of the fonts PDFjet ships only IBM Plex Sans JP, SC and TC have one, 1 em on
  an ascent and descent of 1 em, so their lines no longer touch; the text of
  every other shipped font does not move. A `.stream` file holds the line gap
  after the marks at the end of its metrics, where older libraries stop
  reading, so they still read the new streams; the generator writes it, and
  the 24 JP, SC and TC streams have it.
- `CalendarMonth.setFirstDayOfWeek` starts the weeks on another day than
  Sunday, with the date library of each port.
- Three checks of the output against independent references, which the Build
  workflow runs. `.github/scripts/check-example-text.py` compares the text
  every example draws, recorded from the Go port built with the `texttrace`
  tag, with what MuPDF and pdftotext extract from the PDFs of each port: no
  .notdef glyph, no character in another font than it was drawn in, and none
  off the page. `tests/references/images` embeds 884 PNG, JPEG and BMP files
  -- Pillow's test images, the BMP Suite, PngSuite, the repository's own and
  made ones -- and compares MuPDF's decoding of them with the pixels Pillow
  decodes. `tests/references/fonts/check-fonts.py` checks the 252 fonts PDFjet
  ships, their `.stream` files, six other fonts and the 14 core fonts against
  fontTools and Adobe's AFM files, with their kerning pairs.
- `booklet/build.sh` writes the PDFjet booklet with PDFjet, a guide to the
  API, as a PDF/UA document with a title page, a linked table of contents,
  bookmarks and page numbers. Its chapters go through documents and pages,
  fonts, text, tables, graphics, images, charts, barcodes, links and
  annotations, forms, and existing PDFs, 57 features, each with what its
  classes and methods do and the smallest code that shows it, 5 to 15 lines.
  It writes one booklet for each port, of 34 or 35 pages, and one with the
  code of the four, of 71, from `booklet/content.txt` and the snippet
  programs in `booklet/snippets`, one for each port; the Swift snippets are
  the `BookletSnippets` target of `Package.swift`.
  `booklet/check-snippets.sh` builds and runs them and checks that the four
  ports write the same PDFs, page by page and text run by text run, with
  veraPDF for the PDF/UA and PDF/A snippets. `booklet/Booklet.java` lays it
  out with PDFjet itself, which is what a document of this size asks of the
  library, and stops when a port lacks a snippet the text shows or has one
  it does not show.
- In a PDF/UA document a `Chart`, a `DonutChart` and a `BarChart` are each one
  Figure, with the description `setAltDescription` gives or one made from the
  title or the slices.
- In a PDF/UA document a `Table` is tagged as a table, in all four ports: a
  `Table` structure element with a `TR` for each row and a `TH` or `TD` for
  each cell, which holds the text, text line, text block or image the cell
  draws, so a screen reader can say "row 3, column Price". Header cells have
  the `Column` scope, a cell that spans columns a `ColSpan`, and the lines of
  wrapped cell text are one cell, even across a page break. The header rows
  are tagged where the table starts and are artifacts where they repeat on
  the next pages. The structure tree can nest elements for this; the other
  drawables are children of the Document element as before.
- `Cell.setRowSpan` spans a cell over the rows under it, as `setColSpan`
  spans it over the columns beside it, in all four ports. The cell draws its
  text, its background and its borders once over all the rows it covers, and
  the cells it covers draw nothing. The table keeps its shape, so every row
  holds a cell for every column and the cells a span covers are there and are
  empty, as they are for a column span. A page break keeps the rows of a span
  together: they go to the next page with it, so a span is cut in two only
  when it is taller than a page, between its rows, and is drawn on each page
  over the rows it covers there. In a PDF/UA document the cell has a
  `RowSpan` attribute, which counts the rows of the table and not the lines
  its rows wrap into, and a row that a span covers whole holds no cell of
  its own. Example_38 is drawn with it,
  where it used to fake row spans by turning cell borders off and marking the
  cells under a span with a caret; it is now the table of the HTML in its
  comment, cell for cell, and all four ports draw it identically.
  `check-pdfua-tags.py` lays the cells of a tagged table out on a grid, as a
  browser lays out an HTML table, and checks that they fill every square of
  it and none of them twice; it counted the columns of each row before, which
  a row span makes uneven on purpose.

- `SVGImage` draws the SVG files of drawing programs, Illustrator, Inkscape
  and Figma among them, and icon sets such as Lucide, in all four ports,
  where it drew only the `<path>` elements and the colors of each path and
  of the `<svg>` element:
  - groups, whose fill, stroke, stroke-width and the other properties below
    their paths and shapes inherit, as in SVG, and whose opacity multiplies
    that of what is in them;
  - transforms, `matrix`, `translate`, `scale`, `rotate` about the origin or
    a point, `skewX` and `skewY`, of groups, paths and shapes, applied in
    turn; a stroke is scaled with them;
  - the basic shapes: `<rect>`, with rounded corners of `rx` and `ry`,
    `<circle>`, `<ellipse>`, `<line>`, `<polyline>` and `<polygon>`;
  - the properties as attributes, in a `style` attribute, and in the rules of
    a `<style>` element for a class, `.name { ... }`, the style attribute
    over the rules, and the rules, in the order of the style sheet, over the
    attributes; rules of other selectors are left out;
  - `fill-rule="evenodd"`, filled with `f*`; `opacity`, `fill-opacity` and
    `stroke-opacity`, in a graphics state of the path's own; and
    `stroke-linecap` and `stroke-linejoin`;
  - the colors `rgb()` and `rgba()`, of numbers or percentages,
    `currentColor` and the `color` property, color names in any case, and
    the color after a `url()` of a gradient, which is drawn when a gradient
    cannot be;
  - `preserveAspectRatio`, whose default fits a viewBox of other proportions
    into the size, as large as it fits and in the middle, where it was
    stretched, and a width or a height that is not given, which is the other
    in the proportions of the viewBox, where it was that of the viewBox.

  What is in `<defs>`, `<clipPath>`, `<mask>`, `<pattern>`, `<symbol>`,
  `<marker>` and the gradients, and what has `display="none"`, is not drawn;
  it was drawn as ordinary paths. Gradients and patterns, text, `<use>`,
  embedded images, clipping, masks, filters and dashed strokes are still left
  out. Values that cannot be read, such as a `stroke-width` of `calc()`, a
  transform of a function PDFjet does not know, a skew of 90 degrees, which
  flattens the shape, or a number too large for a float, are left out as SVG
  leaves them, the property taking what it inherits; a color that starts with
  `#` and is not hexadecimal fails as before. A width or a height of `inf` or
  `nan` is no size, and a path that a transform or `ScaleBy` takes beyond the
  numbers a PDF holds, 2^31, is not drawn, where Java and C# threw and Go
  wrote 0. The viewBox may separate its numbers with commas. Four files of
  those programs, drawn by PDFjet and by Chrome, differ in no pixel but along
  the edges.

### Changed
- Go compresses the samples of a PNG or a BMP image that it decodes at
  level 5 instead of the default 6: a screenshot's samples compress about
  30% faster, and come out under 1% larger. The pages keep the default, as
  level 5 makes their content 4% larger.
- The producer is `PDFjet v9.0.2`, in all four ports.
- The writer appends numbers, strings and hexadecimal without making a
  string for each, in all four ports; a tagged document of 200 pages is
  written 22% faster in Go, with a third of the allocations.
- A PDF/A document refuses what it cannot hold, in all four ports, where it
  was written as it was and failed veraPDF: a CMYK image, and a CMYK color
  of `setPenColorCMYK` or `setBrushColorCMYK`, which wrote DeviceCMYK (rule
  6.2.4.3), as its output intent is sRGB, and in PDF/A-1 an image with
  transparency or of 16 bits per component. The document fails with a
  message, the same in every port, as it does for a file PDF/A-1 or 2
  cannot carry, and the color writes nothing: "A document of PDF_A_2B
  cannot use a CMYK color: its output intent is sRGB, so its colors are
  gray or RGB."
- A document of PDF/UA without a title is refused when it is completed, in
  all four ports, as PDF/UA asks for one.
- An annotation of a tagged document is refused on a page that was already
  written, where its element was left without a `/StructParent`, and one
  that has no contents, no title and no alternative description is refused,
  as a figure without a description is, in all four ports.
- A link to a destination the document does not have is refused when the PDF
  is completed, in all four ports; it was written as a link that did
  nothing.
- `addObjects` refuses what it would lose, in all four ports: a PDF/UA or
  PDF/A document, whose pages the objects were not made for, and a document
  with pages of its own, made before or added after the objects, which the
  page tree of the objects does not list. A number the PDF that was read has
  no object for is a free entry, where it was an empty object marked in use.
- `addResourceObjects` refuses two pages that give one name to different
  fonts, images or graphics states, in all four ports: the pages of a
  document share one resources dictionary, and the second page drew the
  resource of the first.
- The IDAT data of a grayscale, truecolor or palette PNG is embedded as it
  is, in all four ports, with the `/DecodeParms` of the PNG predictor, which
  undo the filters of its rows in the reader, where it was decoded and
  compressed again. A palette image is `/Indexed` on RGB, with the bits of
  its indexes and a color for each of them, black past the colors of the
  palette as before; with a tRNS chunk its alpha is still decoded for its
  soft mask. The data is checked as before, the filter type of each row too,
  and data that is more than the rows of the image, or has no checksum or
  bytes after it, is decoded and compressed again, as the images with alpha
  are. A truecolor PNG of 3000 by 4000 pixels is embedded 4.7 times faster
  in Go and the PDF is 7.9 MB where it was 10.6 MB; a palette PNG of that
  size 5 times faster, 3.2 MB where it was 5.2 MB. The images draw as before,
  pixel for pixel in MuPDF and Poppler, and an `/Indexed` image passes
  veraPDF in PDF/A-1b and PDF/A-2b.
- A PNG with alpha whose pixels are all opaque has no soft mask, in all four
  ports: an RGBA or a gray and alpha image whose alpha is 255 in every pixel,
  as that of most screenshots is, and a palette image whose tRNS chunk gives
  255 to every color its pixels use. Its alpha is not compressed, the PDF is
  smaller, and it draws the same, pixel for pixel in MuPDF and Poppler. A
  document of PDF/A-1, which has no soft masks, holds such an image, and
  passes veraPDF in PDF/A-1b; one with a pixel that is not opaque is refused,
  as above. An image whose alpha is 0 in every pixel keeps its soft mask, and
  is transparent, as browsers and libpng draw it. An RGBA screenshot of 2560
  by 1600 pixels is embedded 10% faster in Go, and 8 KB smaller.
- The Swift port inflates a zlib stream two to four times faster: its codes
  of up to 12 bits are decoded with a table, a step for each code instead of
  a step for each bit, and a stream of a known length is decoded into the
  memory of its bytes, with no copy; the CRC-32 of a PNG chunk is taken
  eight bytes at a time. A stream decodes to the same bytes and fails with
  the same errors as before, at the same bit. A truecolor PNG of 3000 by 4000
  pixels is embedded in 118 ms, where it took 329 ms, and the RGBA
  screenshot in 122 ms, where it took 176 ms.
- What is drawn inside a figure of a tagged document, between
  `addBDC(StructElem.FIGURE, ...)` and its `addEMC`, is the figure, in all
  four ports: a drawable that tags itself, or marks itself as an artifact,
  is neither tagged again nor marked inside it, as inside an artifact. It
  was tagged again, a marked content inside another, which PDF/UA does not
  allow. What PDFjet draws inside a figure of its own, an image, an SVG or a
  chart, is drawn as before.
- The A levels of PDF/A, `PDF_A_1A`, `PDF_A_2A` and `PDF_A_3A`, tag their
  content, as level A asks, in all four ports: the text, the tables and the
  images are marked content of the structure tree, and the lines, the shapes
  and the backgrounds are artifacts, as for `PDF_UA_1`. They wrote a
  structure tree with no marked content in it, which veraPDF passed, but a
  screen reader has nothing to read in. They follow the rules of PDF/UA too,
  so a document of an A level with a figure that has no alternative
  description is refused, as one of PDF/UA is; a document that was written
  before may need `setAltDescription`, or a B level. The words of the refusal
  name both: "A figure of a tagged document, PDF/UA or PDF/A of level A,
  needs an alternative description."
- An SVG path is filled black unless its fill is `none`, whether it has a
  stroke or not, a stroke is one unit wide unless the file says otherwise,
  and a `stroke-width` of 0 draws no stroke, as SVG draws them, in all four
  ports: a path with a stroke and no fill was not filled, a stroke had the
  width 0, the thinnest line a device draws, and a width of 0 drew that line.
  A stroke width is scaled with the viewBox and with `ScaleBy`, as the
  path is, where it kept its width in points. The map of Example_33, whose
  file gave its countries a blue stroke of width 0, no longer outlined them,
  so its file gives them a width of 1, half a point on the page, which
  outlines them in blue as in v8.
- A Code 128 barcode puts runs of four digits or more in code set C, two
  digits to a codeword, in all four ports, where it put every character in
  code set B, as GS1-128 does: it starts in code set C when the text starts
  with four digits or more, or is two digits, changes to code set C at an
  even run of four digits or more and back to code set B after it. A barcode
  of digits is narrower than before, `0123456789` 67.5 points rather
  than 108.75 at the module length of 0.75, and the 48 codewords a barcode
  holds are up to 96 digits; a barcode of text without such runs is drawn
  as before. Characters below 32 and from 160 to 255 are in code set B as
  before, after SHIFT or FNC 4. ZXing reads the barcodes back as their
  texts.
- The space between two text lines of a paragraph is the narrower of their
  two spaces, in `TextFrame` and `TextColumn`, in all four ports. It was
  always the space of the text line before it, in its font, so after a word
  in a monospaced font, such as code, it was about twice as wide as a space
  of the text. A space that goes with the text line after it is left out
  when its word starts a row, and a justified row widens it as any other.
  Paragraphs that mix fonts move by a fraction of a point where the fonts
  meet; a text line with an underline, a strikeout or a link keeps the space
  before it in the text line before, so that its decoration does not start
  at a space, and gives the space after it to the text line after it when
  the two spaces are as wide, so that its decoration does not go on under
  the space.
- A character an embedded font does not have is drawn with its `.notdef`
  glyph, glyph 0, in all four ports, as other PDF writers draw it, so that a
  reader sees a box where it is missing rather than a space. It was drawn as a
  space when it was outside the range of characters of the font's OS/2 table,
  and with `.notdef` only when it was inside it, where a copy of the text gave
  U+FFFD. `stringWidth` and `getFitChars` give it the width of `.notdef`, and
  it is drawn in a marked content span with the character as its actual text,
  so that a copy of the text has it and not the U+FFFD that the ToUnicode map
  gives `.notdef`; a `Stamp` draws it so too. A control character, U+0000 to
  U+001F and U+007F to U+009F, is drawn as a space, and stays in the font of
  its text rather than going to the fallback font. A CJK font that is not
  embedded is drawn as before: the reader's font draws it, and PDFjet does not
  know which characters that font has. PDF/UA and PDF/A forbid `.notdef`, so
  a PDF/UA or PDF/A document draws a missing character with the font's
  replacement character U+FFFD, or a question mark, or a space, the first the
  font has, with the same actual text and that glyph's width, and passes
  veraPDF; it drew `.notdef` inside the font's range before, and failed.
- The API reference of `Container` and `Stamp` says what each is good at, what
  it costs and when to use the other, in all four ports. A container takes
  anything drawable -- an image, a table, a chart, a barcode, an annotation --
  and every element tags itself in a PDF/UA document, but it writes its
  drawing into every page it is drawn on. A stamp is in the file once and each
  placement is 83 bytes against the 367 a container writes for the same 200 by
  50 point box, so it is the smaller of the two from the fourth page on and
  saves 12,855 bytes of a 104,530 byte file over 100 pages; it draws only
  paths and text in an embedded font, and is one structure element with one
  alternate description.
- `BigTable` is tagged as a table in a PDF/UA document, as `Table` is: one
  Table element over all its pages, a TR for each row, and a TH with the
  Column scope for each field of the header the first time it is drawn or a TD
  for each field of a row, each holding the text of its cell. The header where
  it repeats on the next pages, the row shading and the lines are artifacts,
  and the page number is a pagination artifact, as that of `Page.addFooter`
  is. Example_43, which draws a table of 2000+ pages, says in a
  comment how to turn it on and what it costs at that size: every tagged cell
  is an object of its own, so the document goes from 5,108 objects and 11.8 MB
  to 1.25 million objects and 249 MB. It is left off there for that reason.
- The structure elements of a page are written with the page, so a tagged
  document holds no more of them than the page it is drawing. They were all
  held until the document was written, which is what a structure tree of one
  element per cell costs: Example_43 over the whole 29 MB data file, which is
  2000+ pages, took 778 MB of heap where the same document untagged takes 13
  MB. It takes 86 MB now, which is one cross-reference entry for each object,
  and every object of a PDF has one. An element of a PDF/UA document is given
  its object number when it is made, rather than when the document is written,
  and so is a page; a table that runs over pages keeps its Table element open
  until the end. A cell of a `BigTable` holds the text itself rather than a
  paragraph under it, which halves the objects of a tagged table: the document
  of the whole file went from 384 MB to 249 MB, and the time from 4.4 to 3.0
  seconds.
- The contents of Example_22 are tagged as a list, which no example was: an
  `L` of an `LI` for each chapter, each holding the `Lbl` of its number and
  the `LBody` of its title and the link to it. A list is drawn one item at a
  time and nothing could hold the items together, so `Page.beginStructElement`
  and `endStructElement` group what is drawn between them into a structure
  element of any type; they nest, and do nothing in a document that is not
  PDF/UA. An `endStructElement` without a `beginStructElement` is recorded
  as misuse, and `complete` then refuses the document. `StructElem` has
  `LBody`, which all four ports were missing.
- `Paragraph.setListLabel` makes a paragraph of a `TextFrame` or a
  `TextColumn` an item of a list, labelled by a text line that the frame
  draws to the left of it and on the baseline of its first line, once, when
  the item goes on into the next frame. A run of paragraphs that have a label
  is an L of an LI for each, each holding the Lbl of its label and the LBody
  of its text. The numbered paragraphs of Example_03 are two such lists:
  their numbers were drawn in a second pass, after all of the text, so a
  reader read every number after every paragraph rather than each before its
  own. The example draws what it drew, to the point.
- A paragraph of a `TextColumn` or a `TextFrame` is one structure element in
  a PDF/UA document, of the type `Paragraph.setStructureType` gives it: a
  heading with `StructElem.H1` to `H6`, and a paragraph, which it is, with
  `StructElem.P`. A paragraph is drawn word by word, and every word was a
  structure element of its own, so a screen reader read each word as its own
  paragraph: Example_10 had 240 elements for its 7 paragraphs and Example_03
  had 48 for its 26. The words of a paragraph are the marked contents of its
  one element now, and the titles of Example_03 and the section titles of
  Example_10 are headings.
- The heading of a page is tagged as a heading in the examples that have one,
  where every one of them was a paragraph. PDF/UA-1 asks for a heading to be tagged H or Hn, which veraPDF
  cannot check, since it cannot see what a paragraph stands for; the new
  `.github/scripts/check-pdfua-tags.py` reads the structure tree and reports
  what the Matterhorn Protocol leaves to a reviewer — the heading levels, the
  descriptions of the figures and the links, the nesting of the lists and the
  header cells of the tables — and runs in `check-examples.sh` and in the
  Build workflow beside veraPDF.
- In a PDF/UA document the underline and the strikeout of a `Cell` and of a
  `TextLine` are artifacts, in all four ports, where each was a paragraph
  with the alternate description "underline" or "strike out" in a cell, and
  a structure element whose alternate description was "Underlined text: "
  or "Strikethrough text: " and the whole text in a text line, which a
  screen reader read out after the text. The tagged examples hold eight
  fewer paragraph elements for the text lines.
  Marked content that a drawable begins inside an artifact is no longer
  written, since tagged content inside an artifact breaks PDF/UA.
- Text in IBM Plex Sans JP, SC or TC in a `TextBlock`, `TextColumn` or
  `TextFrame` is twice as tall per line as before, the spacing of the font's
  line gap (see Added). A document that set a line spacing to make up for the
  missing gap gets more space than before.
- A `CompositeTextLine` in a PDF/UA document is one structure element, in all
  four ports, where every part of it was one: the six runs of C6H12O6 were six
  paragraphs of "C", "6", "H", "12", "O" and "6", which a screen reader reads
  one at a time. A line that a paragraph already owns, as in a table cell that
  draws one, is left to that paragraph.
- An `Image` and an `SVGImage` on a page of a PDF/UA document are a Figure
  with the description `setAltDescription` gives, in all four ports, where
  every one of them was a paragraph. PDF/UA asks for a figure to be tagged as
  one and described, so a screen reader can say what it shows instead of
  reading an empty paragraph; drawing one with no description on a tagged page
  now fails with a message, as the other PDF/UA misuse does. The nine examples
  that draw images in a tagged document describe every one of them, 79 in all,
  and the structure tree of the examples holds 85 Figure elements instead of 6.
  veraPDF passed the paragraphs, since it cannot see that the content is an
  image; a Figure with no description fails its clause 7.3.
- A `Cell` keeps its four paddings in the four bytes of one integer and its
  text alignment, vertical alignment and marker alignment in three bits each
  of the flags it already had, in all four ports, instead of four floats and
  two enum references. A padding is kept to the nearest quarter of a point,
  between 0 and 63.75, as a color given as floats is kept to the nearest of
  256 steps: one below 0, or not a number, is 0, and one above 63.75 is
  63.75. The comment of `Cell` says how the paddings are kept; every padding
  the examples and the tests use is exact, and the pages they draw are byte
  for byte the same. The setters and getters are unchanged. In Go a cell is
  112 bytes instead of 144, and Example_43 drawn with `Table`, 1,122,453
  cells, takes 161 MB of heap instead of 195. In Java the same document
  allocates 1,338 MB instead of 1,364 and finishes in a 328 MB heap instead
  of 352. The Swift `Alignment` enum is `UInt32`-backed, with the values of
  the other three ports.

### Fixed
- A document made or read by the C# port is the same whatever the culture
  of the thread. In a culture such as sv-SE, which writes a minus sign of its
  own, a negative number, as in a bookmark, was written with that sign, a
  font added to a PDF that was read had a font box and a descent of `null`,
  and the access permissions of an encrypted document were not a
  number; in tr-TR a core font added to such a PDF was named `HELVETİCA`.
  Numbers are written and read in the invariant culture, and text is compared
  ordinally and upper-cased with `ToUpperInvariant`. The examples are the
  same byte for byte in sv-SE, de-DE and tr-TR as in the invariant culture.
- A destination and a heading inside a `Container`, in all four ports: they
  were where the text would be on the page, not where the container moves
  and turns it. A destination, and so the link to it, and the bookmark of a
  heading now go to where the text is drawn, as the rectangle of a link and
  the box of a figure did.
- A chart of flat data above 2^24, in all four ports: the range of 1 that
  flat data got was lost in a float32, and the NaN it gave failed the PDF.
  The range is 1, or a hundred thousandth of the value when that is more,
  and the rounded range always has a grid line. A value of a `BarChart` that
  is NaN or infinite has no bar, and no longer makes the value axis NaN.
- A Code 128 barcode with a character from U+0080 to U+009F, in all four
  ports: drawing it panicked or threw. It is FNC 4, SHIFT and the control
  character in code set A, three of the 48 codewords, and ZXing reads it
  back.
- A PDF417 barcode with a control character other than HT, LF and CR, in all
  four ports: it was drawn as a space. It is in byte compaction, after the
  shift 913, so a GS or an RS reaches the reader; ZXing reads them back.
- The bars of a PDF417 barcode are black, in all four ports: they took the
  pen colour the page had. They are one artifact, not one each, and the
  modules of a QR code and of a Data Matrix are drawn inside q and Q, so the
  page keeps its colours.
- A QR code of text that is not ASCII starts with the ECI of UTF-8, in all
  four ports, so a reader does not guess its character set. The ECI is
  counted in the capacity: one byte less at version 40.
- The mask of a QR code is chosen with the penalty rules of ISO/IEC 18004,
  in all four ports, with the format information in place, so some QR codes
  have another mask. The codewords are made once rather than nine times,
  which makes a version 40 code about 7 times faster. Each run of dark
  modules in a row is one rectangle, not one per module.
- A PDF encrypted with AES and a key shorter than AES takes, in all four
  ports: reading it panicked or threw. It is refused with a message. The
  `/Contents` of a signature dictionary, which is not encrypted, is no
  longer decrypted into garbage.
- The `Encryption` of a PDF/UA document no longer changes the caller's
  `Permissions`, in all four ports, when it grants the extraction for
  accessibility.
- The character map of a font is read in one pass over its segments, in
  all four ports, which makes a CJK font load in half the time, and a font
  without an OS/2 table has every character of its map, where it drew each
  as .notdef.
- A JPEG that is lossless, hierarchical or arithmetic coded is refused, in
  all four ports, as the DCTDecode filter of a reader cannot decode it; a
  PNG row of a filter type PNG does not define is refused, as libpng refuses
  it; and a pHYs chunk of more pixels per metre than a PNG number holds is
  passed over in Go and Swift, as Java and C# did.
- PNG images decode faster, in all four ports: the filters are undone a row
  at a time with integers, the first pixel of a row and the first row apart
  from the rest, so that the bytes of the rest need no check of whether they
  have a byte on the left or a row above, and Java and C# join the IDAT
  chunks of a large image in one buffer, where a 19 MB image took 6 to 7
  seconds.
- An image of more than 2^24 pixels across or down is written with every
  pixel, and the link of an image turned a quarter of the way covers it as
  it is drawn, in all four ports.
- SVG paths follow SVG, in all four ports: a smooth curve reflects the
  control point of a curve of its kind only, a command after Z starts at the
  start of the subpath it closed, a path is stroked once with all its
  subpaths, and path data that does not start with a moveto draws nothing.
  The comments and the class rules of a style sheet are read without going
  through the whole of it for each, so a large one no longer takes minutes.
- A font file that does not end in .stream is told by its first bytes in
  Java, C# and Swift, as in Go; a font added twice is compressed once; and
  `getFitChars` at a font size of 0 counts every character as fitting, where
  Swift trapped.
- Reading a malformed PDF takes time in proportion to its size, in all four
  ports: a run of white space, objects with no endobj, a /Prev that leads
  back to its own cross-reference section and many streams whose /Length is
  an object of their own each took from seconds to minutes. A stream whose
  /Length object was updated reads the newest version of it.
- A page tree or a chain of objects a hundred thousand deep, in all four
  ports: they were followed by recursion, which ran out of stack in Java,
  ended the process in C# and trapped in Swift.
- `PDFobj.getValue` finds an entry of the object's own dictionary, in all
  four ports, and not one of a dictionary inside it: the /Type of a /Group
  before the /Type of a page made the page not a page.
- A reference or an object stream number that is not digits, or too large
  for an int of 32 bits, is skipped or refused alike in all four ports,
  where Java and C# threw, and the offsets of an object stream no longer
  overflow in Java and C#.
- A `TextFrame` breaks a word wider than the frame in linear time, in all
  four ports: 100,000 characters of Markdown `>` took 75 seconds.
- The pages of a `Table`, in all four ports. A row that does not fit under a
  heading on the first page goes on the next page, where it was drawn past
  the bottom of the page. A column span of less than 1 is 1, and one past
  the end of its row stops there: either hung the drawing. With no rows left
  to draw, a table returns where it starts, where Java and C# threw.
- `BigTable.complete()` without data draws nothing in Java and C#, as in Go
  and Swift.
- Delimited data files, in all four ports: a record whose lines each close a
  quoted field and open another is read in linear time. A line ends at a
  carriage return, a line feed or both, in Go and Swift as in Java and C#. A
  quoted field that is never closed is an error that Go's `BigTable` returns
  and Swift throws, where both stopped the program. Swift streams the file
  of a `Table` and of a `BigTable` without copying what it has read.
- Java's `TextBlock` is aligned left by default, as in the other ports.
- A PDF/A document of level B, `PDF_A_1B`, `PDF_A_2B` or `PDF_A_3B`, is not
  tagged, in all four ports: its catalog has no `/StructTreeRoot` and no
  `/MarkInfo`, and its pages no `/Tabs` and no `/StructParents`, which said
  its content was tagged when it had an empty structure tree. It keeps its
  `/Lang` and `DisplayDocTitle`.
- The notice of a font is escaped in the metadata of the font, in all four
  ports: the notices of Noto Sans JP, KR, SC and TC hold an ampersand, which
  made the metadata of a document of PDF/A that embedded them unreadable. In
  Swift, the font file of an OpenType font refers to its metadata from its
  own dictionary; the reference was written outside any object.
- Every annotation is printed, `/F 4`, and in a document of PDF/A every
  annotation that is not a link has an appearance of its own, in all four
  ports, as PDF/A asks: the square, the circle or the polygon in its fill
  color, and a note or a file as a white box with a black frame.
- The structure tree of a tagged document has a role map for the types of
  PDF 2.0 it uses, in all four ports: Title is a P, and Em and Strong are a
  Span, as PDF/UA-1 asks. A text of the structure type Artifact is marked as
  an artifact, and a structure element of the type Artifact is refused.
- The title, the author, the subject, the keywords and the creator are
  cleaned of what XML does not allow when they are set, in all four ports,
  so that the information dictionary says what the metadata says, as PDF/A
  asks.
- `complete` closes the file or the stream it writes to when it fails too,
  in all four ports.
- The layers are listed in the order of their names by their UTF-16 code
  units, and those of the same name in the order they were added, in all
  four ports; Go, C# and Swift each sorted them their own way.
- Swift: an empty text string is the byte order mark alone, as in the other
  ports.
- The links, annotations and figure bounding boxes of what a `Container`
  holds, in all four ports: they are moved, turned and scaled with the
  container, at any nesting, as its drawing is. A link of a text line in a
  container was where the text would be outside it, a polygon annotation in
  a turned container was written unturned, and a third level of nesting was
  not offset.
- A point that is in a `Path` twice is offset once, in all four ports, and
  the path's own points are left as they were.
- A path that ends on a control point is refused, in all four ports, by
  `Page.drawPath` and `Stamp.drawPath`, and nothing of it is drawn. The page
  wrote coordinates with no operator, and the stamp threw after writing
  them.
- The link of a turned `TextLine` covers the turned text, in all four ports;
  its rectangle was that of the unturned text.
- A superscript or subscript of a `CompositeTextLine` is raised or lowered
  once, in all four ports: the 2 of "x^2" at 12 points is 4.2 points up, not
  8.24. The measures of the line are where it is drawn.
- A highlighted word keeps its combining marks, in all four ports, so that
  its accents are placed on their letters and the word is matched with them.
- Decoration is an artifact in a tagged document, in all four ports: a
  `Line`, an `Arc`, a `Stamp`, and a check box or radio button with no
  label, unless given a description or actual text. The header, footer and
  watermark of a page are Pagination artifacts. They were paragraphs of no
  text.
- A rotation is written with five decimals, in all four ports: rounded to
  hundredths, a text turned 1 degree was turned 1.15.
- A path, the underline and strikeout of a text line, a check box and a
  radio button leave the pen of the page as they found it, in all four
  ports.
- An arc of no sweep draws nothing, one of more than a turn is a turn, and
  one that is not a number is refused, in all four ports. Swift trapped and
  Go on arm64 looped.
- The size of an annotation is measured from its location whether
  `setLocation` is called before or after `setSize`, in all four ports.
- A `CheckBox` is the size of its font size, in all four ports, and a `Form`
  without its fonts is refused rather than crashing.
- A `TextLine` with no text adds its destination, in all four ports.
- The four ports agree: an empty URI or destination name is none; C# draws a
  lone surrogate as .notdef, as Java does; keywords are lower-cased whatever
  the locale, and Swift matches them by their code points; Java trims and
  splits on the white space of Unicode; a dash pattern has no vertical tab;
  Swift refuses an RGB color of fewer than three values and a transform of
  fewer than six, and no longer traps on `setTextRotation(Int.min)`; Java
  and C# draw the diamond and box points in float.
- An object with no endobj that the cross-reference table or stream of a PDF
  lists, in all four ports: it was read to the end of the PDF, for each of
  them; it ends where the next object of its section starts.
- The rectangle of a `PolygonAnnotation`, in all four ports: it was written
  as the location and 0 and the height of the page, as its second corner is
  not set; it is the box of its vertices.
- The element of an annotation that is not a link, a note, a shape or a file
  attached, in a tagged document, in all four ports: it is described by what
  the annotation says, or by its title, as a viewer shows them. It had no
  text of its own, which PAC reported as tagged text of only whitespace, 6
  times in Example_06.
- The tags PAC warned of in a tagged document, in all four ports. A figure,
  a link or an annotation that is a kid of the Document, or of another
  element that groups, has the attribute Placement Block, as PDF asks of an
  element of an inline type that stands as a block: PAC warned of each as a
  possibly inappropriate use, 6 Annot elements in Example_06, 218 Link
  elements in Example_09 and the Figure of Example_10. And the text of a
  cell has no Alt of its own, which PAC warns of on text, 4,385 of them in
  Example_08; a cell of a `BigTable` has one only when its text is cut
  short to fit, as the whole of it. check-pdfua-tags.py checks both.
- The Link element of a link holds the text of the link, in all four ports:
  in a tagged document, PDF/UA or a PDF/A of level A, a text line, a word of
  a paragraph, the text of a cell, a check box or a radio button that is a
  link is a Link element of its text and its annotation both, in the element
  of the text, as PDF/UA asks; and a linked image or SVG image is a Figure in
  a Link that holds its annotation. It was a Link of the annotation alone,
  beside the text, which a screen reader reads as a link of no text, and PAC
  warns of as a possibly inappropriate use of a Link. A point of a chart or
  of a cell that is a link is a Figure of its marker in its Link, drawn
  after the chart in a tagged document; it was a Link of the annotation
  alone over the chart's figure, 223 of them in Example_09, whose points are
  now described by their countries. A link over content that is not
  tagged, like a rectangle with no border, is still a Link of its annotation
  and its description.
- A compliance set after the encryption was refused with "Set the compliance
  before adding fonts, images or pages to the PDF.", in all four ports,
  which named what had not been added. A PDF/A compliance is refused with
  "A PDF/A document cannot be encrypted.", as the encryption is when it is
  set after a PDF/A compliance, and any other with "Set the compliance
  before the encryption, which is written for it.": the encryption grants a
  PDF/UA document the permission to extract its content for accessibility.
- On a page with a link or another annotation, the parent tree of a tagged
  document gave the first marked content of the page to the element of the
  annotation, in all four ports: the element of an annotation has no marked
  content, but its mcid of 0 was taken as one. A screen reader that finds
  the element of what it reads from the parent tree found the annotation.
  PAC reports it as "Inconsistent entry found", in 3 examples, and 3 more
  had it. check-pdfua-tags.py checks every entry of the parent tree of the
  examples. Found by PAC.
- A figure of a tagged document had no bounding box, in all four ports:
  PDF/UA asks for one, the BBox of the Layout attributes of the Figure
  element, which a screen reader or a program that reflows the page uses to
  find it. PAC failed 11 examples on it, and veraPDF does not check it. An
  image, an SVG image, a chart, a bar chart, a donut chart, with its leader
  lines and labels, a described barcode, with its text, and a described QR
  code, Data Matrix or PDF417 barcode have the box they are drawn in, an
  image turned a quarter of the way the box it is turned into, and
  check-pdfua-tags.py checks that every figure of the examples has one.
  Found by PAC.
- A rectangle, an arc and an ellipse set their colors, line width and dash
  after they had begun their paths, and `Page.drawContents` wrote a text
  matrix, Tm, outside a text object, in all four ports: operators ISO 32000-1
  does not allow there, in its Figure 9, which PAC reports as errors of PDF
  syntax and veraPDF does not check. PAC failed 14 examples on them. The
  colors, the width and the dash are set before the path, and the Tm, which
  BT resets, is left out. check-example-pdfs.py checks the operators of every
  content stream of the examples. Found by PAC.
- A word too wide for a line of a `TextBlock`, broken between its characters,
  took time that grew with the square of the word, in all four ports: the
  rest of the word was measured before each line, and the word up to each
  break was copied to measure it, so a word of 100,000 characters took 39
  seconds. Each line is found by measuring from its start to each character
  break until one does not fit, and the rest of the word only when the breaks
  run out; the same word breaks in a fraction of a second, into the same
  lines as before. A right-to-left word is reordered and shaped once, whole,
  and a line of it is a part of the shaped word, so the letter a line ends on
  keeps the form it has in the word; reordering the word up to each break had
  taken 27 seconds for 16,000 Arabic letters, and about 20 minutes for
  100,000. Found by pdfjet-server.
- Reading a PDF decoded every stream in full, with a limit for each stream
  and none for all of them, so a PDF of a few megabytes could take gigabytes
  to read, in all four ports. A stream that is not a cross-reference or an
  object stream is decoded when its data is first asked for, `getData`, and
  all the streams of one PDF may decode to 256 MiB together: an object
  stream or a cross-reference stream past that is an error, as the PDF
  cannot be read without its objects, and any other stream past it has no
  data, as a stream that cannot be decoded has none. A page whose content is
  an array of streams has all of them, in Java, .NET and Swift, which read
  them before they were decoded. In Go, `GetData` returns nil for a stream
  whose decoder fails with a runtime error, where it panicked in the caller,
  as the other ports return no data. Found by pdfjet-server.
- Greek text in a font that draws the micro sign with the glyph of mu, such
  as Source Serif 4, was copied, searched and read aloud with the micro sign
  µ in place of each μ, in all four ports: the ToUnicode map of a glyph two
  characters share gives the one of them text seldom has only when there is
  no other, and the micro sign is one such now, and so are the ohm, kelvin
  and angstrom signs, which fonts draw with omega, K and A with a ring. A
  shared glyph is copied as the letter, which Unicode makes the signs. Found
  by pdfjet-server, comparing PDFs of Latin, Greek and Cyrillic text.
- A Code 128 barcode whose text takes more than 48 codewords is refused, in
  all four ports, rather than drawn with the codewords after the 48th left
  out, so that it held another value than its text: 60 sevens were drawn
  exactly as 48. The constructor throws, or panics in Go, as it does for the
  digits of UPC-A and EAN-13; a character below 32, or from 160 to 255,
  takes two codewords, and one from 128 to 159 three.
- A Code 128 barcode with a character above U+00FF, which its code set B
  cannot hold, is refused by the constructor, in all four ports. Drawing one
  indexed the table of Code 128 with codeword 256: an
  `ArrayIndexOutOfBoundsException` in Java, an `IndexOutOfRangeException`
  in C#, an index out of range panic in Go and a crash in Swift.
- The lines of a table's cells are black, in all four ports, whatever pen
  color the page was left with. A cell with no border color set drew its
  lines in the pen's color, so after `Page.setPenColor` a table came out in
  that color; `Cell.setBorderColor(Color.transparent)` now draws them black
  too. Found by pdfjet-server.
- A table without lines, after `Table.setCellBorders(false)`, has no line
  under its header row, in all four ports. The last header row was given a
  bottom border on every page whatever its borders; it is now given one only
  under the cells that have borders. Found by pdfjet-server.
- Column percentages that are all 0 share the width of the table equally, in
  all four ports, as columns with no percentage do, rather than leaving each
  column 75 points wide: two columns at 0% in a table 300 wide are 150 each.
  Found by pdfjet-server.
- The corner radius of a `Rect` is at most half its shorter side, in all
  four ports, as SVG has it. A larger radius turned the sides back on
  themselves and drew the corners as crossed loops; a rect 20 high with a
  radius of 30 is now the pill that a radius of 10 draws. Found by
  pdfjet-server.
- The guard bars of EAN-13 and UPC-A barcodes reach 5 modules below the
  other bars, in all four ports, as the GS1 General Specifications have
  them, rather than 8 points whatever the module: about 10.7 modules at the
  default of 0.75, 8 at 1 and 4 at 2. At the default module the guards, and
  the barcode, are 4.25 points shorter. Found by pdfjet-server.
- The bars of the first and the last digit of a UPC-A barcode are as long as
  the guard bars, in all four ports, as the standard draws them, which is why
  the two digits are printed outside the bars. Found by pdfjet-server.
- A line of a table cell that breaks before a word wider than the column ends
  without the space before the word, in all four ports. The space counted in
  the width of the text, so a cell aligned right drew the line that far left
  of its padding, and a centered one half as far: 2.36 and 1.19 points for
  "abcd Wxyzwxyzw" in IBM Plex Sans at 10 in a column 30 wide. Found by
  pdfjet-server.
- The bars of a barcode are black, in all four ports, whatever pen color the
  page was left with. `Barcode` set the width of each bar but not its color,
  so after `Page.setPenColor`, which a shape drawn with the methods of `Page`
  leaves set, the bars came out in that color; it now draws in a graphics
  state of its own.
- A paragraph of many text lines joined with `Paragraph.addJoined` is drawn
  in time linear in its length, in all four ports. `TextFrame` measured the
  words joined to a word over and over, which took 9 seconds for 64,000
  characters of text lines of one word each, as the inline markup of a link
  and the punctuation around it makes; it stops measuring now as soon as
  they cannot fit, which cannot change where the row breaks.
- A word too wide for the column of a table is broken between two
  characters, not inside one that is outside the basic plane, such as an
  emoji, in Java and C#; Go and Swift already broke it between characters.
  The two halves of the character were drawn as two missing glyphs on two
  lines of the cell.
- A table whose columns `autoAdjustColumnWidths` makes as wide as their
  text no longer wraps the text that fits, and no longer fails to draw, in
  all four ports. The width of a column less its padding could come out a
  little less than the width of its text in floating point, as in IBM Plex
  Sans Bold at 11 points with a cell of "a", so the text was wrapped; a word
  that no line fits was then broken with an empty line before its first
  letter, and drawing the table went past its rows: an
  IndexOutOfBoundsException in Java, a panic in Go and a crash in Swift. Text
  fits a column now when it is at most a hundredth of a point wider, and a
  word broken letter by letter has a letter on every line.
- The transparent color that the `tRNS` chunk of a grayscale or truecolor
  PNG names is transparent, in all four ports; PDFjet ignored the chunk and
  drew those pixels opaque, where browsers and libpng leave them clear. The
  image gets the color key as a `/Mask` array, the range of each component in
  the bits the image is embedded in -- 1 to 16 of gray, 8 or 16 of RGB -- so
  its samples are as they were and no soft mask is added. Only the bits of the
  image are read of the chunk's value, as libpng reads it, so 255 in an image
  of 1 bit is white; a chunk of another length than a pixel is ignored. The
  image check against Pillow reads the color key of a `/Mask`, and the 11
  files it listed as differing for this now match Pillow.
- A page break of a `Table` no longer falls between the lines of a cell
  whose text wraps, in all four ports. The first lines ended one page, with
  the other cells of the row, and the rest started the next, beside empty
  cells. The lines of a row now go to the next page together, as the rows of
  a span do. Lines that are taller than a page would not fit the next page
  either, so they are still cut where the page ends, from where the row
  starts.
- The font descriptor of an embedded font has the italic angle of its `post`
  table, in all four ports: -11.31 for IBM Plex Sans Italic, -14.04 for IBM
  Plex Serif Italic. It was 0 for every font. A font with an italic angle has
  the Italic flag too, `/Flags 96` in place of 32. A `.stream` file holds the
  angle after the line gap at the end of its metrics, where older libraries
  stop reading, so they still read the new streams; the generator writes it
  for an italic font, and the 64 italic streams PDFjet ships have it. Nothing
  draws differently: the glyphs of an italic font were italic already.
  `tests/references/fonts/check-fonts.py` compares the angle with fontTools.
- Two differences from fontTools and Adobe's AFM files that
  `tests/references/fonts/check-fonts.py` found, in all four ports:
  - The cap height of a font whose OS/2 table is version 0 or 1, which has no
    `sCapHeight`, was read from the bytes after the table, often those of the
    next table: DejaVu Sans got 0 and Bitstream Vera Sans 1. It is now the top
    of the glyph of H in the `glyf` table, 1493 for both, and the ascent of a
    font with CFF outlines or no H. A version 2 table that ends before
    `sCapHeight` has none either.
  - U+007F was drawn in a core font with code 127, which WinAnsiEncoding
    draws as a bullet, 350 units wide in Helvetica and Times, where the widths
    of PDFjet gave it a space's. It is a space now, as U+0080 to U+009F are.
- The reader, `PDF.read`, against the test PDFs of pdf.js and veraPDF, in all
  four ports. `tests/corpus/check-corpus.py` reads, merges and splits each of
  their 3,888 files, and the 431 that pdf.js links to, and compares the pages
  with MuPDF's; it found:
  - A stream whose `/Length` is missing, too short or too long was cut to it,
    and a merge wrote the page's content cut: the text of a page was gone.
    The stream now ends at the end of line before its `endstream` when the
    `/Length` does not, as MuPDF and pdf.js read it, and the `/Length` is set
    to what the stream is.
  - One stream that could not be decoded, like a Flate stream cut short or
    with a wrong checksum, made the whole PDF unreadable. Only a
    cross-reference stream or an object stream, whose objects are needed,
    fails the read now; any other such stream has no data, `getData` returns
    none, and a merge copies it as it is.
  - A PDF whose `/CF` dictionary, or a crypt filter in it, is an object of
    its own was read as if it were not encrypted, and its streams and strings
    were merged still encrypted.
  - `getPageSize` did not follow a `/MediaBox` that is an object of its own or
    whose numbers are, and gave letter size; `getPageObjects` now writes such
    a box as the four numbers it is. An empty box is letter size, as MuPDF
    and pdf.js draw it, and not 0 by 0.
  - A PDF with an object numbered higher than it has bytes was refused, a
    guard the fuzzing added against a file of a few bytes taking gigabytes of
    memory; a PDF cut from a larger document keeps the numbers of its objects,
    like the 41 objects numbered up to 156,341 in 107 KB of one pdf.js tests.
    Numbers up to 262,144 are read in a file of any size now.
  - The pages were read from the first `/Pages` node with no `/Parent`, which
    can be a tree left from an earlier version of the file, as the one page
    in front of the 350 of pdf.js's issue19281, or one in front of the pages
    of an XFA form. The pages are the tree that the catalog the trailer's
    `/Root` names has, as MuPDF and pdf.js read them.
  - A cross-reference table with an entry that marks object 0 in use was
    refused. The entry is skipped, as MuPDF skips it.
  - The `/Length` of a stream was the first `/Length` of its object, which
    can be a value, as in `/Height/Length`. It is the entry of the stream's
    dictionary now.
  - A reference whose number has leading zeros, as `0000000003 0 R`, was not
    read as a reference, so a merged page lost its `/Parent` and its
    contents.
  - Each page looked up what it inherits, like its `/CropBox` and `/Rotate`,
    through its parent, and copied the parent's `/Kids` to do so: a page tree
    node of 10,000 pages, as one of veraPDF's tests has, took from 35 seconds
    to three minutes to read and merge. What a node has or inherits is found
    once for each read or merge now, and the same file takes 0.09 seconds.
- A JPEG is drawn at the size the density of its JFIF segment asks for and a
  BMP at the size the pixels per metre of its header ask for, in all four
  ports, as a PNG is drawn at the size of its `pHYs` chunk below. The three
  formats all carry the size the image is meant to be, and honouring it for
  one of them and not the others is the worse of the two inconsistencies.
  Unit 0 of a JFIF segment is the ratio of the two axes with no size to it,
  and an image with that, with no JFIF segment, or with a BMP header whose
  two fields are 0, which is what most writers leave them at, is drawn at one
  point for each pixel as before. The resolution an Exif segment of a JPEG
  holds is not read: it is a TIFF image file directory, a format of its own,
  and the JFIF segment is what the density of a JPEG is. `images/cmyk.jpg`
  and `images/spain-admin.jpg` are 300 dpi and `images/palette.bmp` is 120
  dpi; Example_19 and Example_36 size their image from its width so they are
  unchanged, and the factor Example_24 scales by is adjusted so its page is
  unchanged too. The two resolution fields of `images/rgb24pal.bmp` held
  2835 pixels per metre, which is 72.009 dots per inch and not the 72 it was
  meant to be -- 72 dpi is 2834.6457 pixels per metre and cannot be written
  as the whole number the field holds -- so they are 0 now, which is what a
  BMP with no resolution in it says and what most writers leave them at. Its
  samples are the same to the byte and it is drawn 127 by 64 points, one for
  each of its pixels, as it was.
- A PNG is drawn at the size its `pHYs` chunk asks for, in all four ports.
  The chunk holds the pixels per unit of each axis, and a PNG written at 300
  or 120 dots per inch was drawn at one point for each of its pixels, which
  is 72 dots per inch: `images/rgba-8bit-chunks.png` is 380 by 100 pixels at
  300 dpi, 91.2 by 24 points, and Example_17 drew it 380 by 100 points, four
  times too large and visibly soft. Only unit 1, the metre, is a physical
  size; unit 0 is the ratio of the two axes with no size to it, and an image
  with that, with no `pHYs` chunk, or with one that asks for a size too large
  for a PDF number, is drawn at one point for each pixel as before. The two
  axes are read on their own, so an image of pixels that are not square is
  drawn with each of them at its own size. The image object of the PDF holds
  the pixels of the image whatever the chunk says. Ten of the files in the
  repository carry the chunk; Example_08 and Example_10 scale theirs by hand
  and their factors are adjusted, so the only example page that changes is
  Example_17, where the image is now drawn at its own size and is sharp, and
  Example_22, where the arrow of the go-to action is 12 points instead of 20.
- The Swift port writes the hexadecimal digits of a string object in small
  letters, as the Java, C# and Go ports do. It wrote them in capitals, so the
  same document was not the same bytes in the four ports: Example_22 had
  `/Title <feff0049` in three of them and `/Title <FEFF0049` in Swift, and
  every Alt, ActualText of a structure element, bookmark title, information
  entry, file identifier and CID system registry was the same. Both are valid
  PDF and draw the same, which is why `check-examples.sh`, which compares the
  rendering and the content streams of the four ports, passed either way. The
  digits of the text drawn on a page stay capitals, as they are in all four.
  One call site that lowercased what it was given no longer needs to.
- A `BigTable` wider than the page it is drawn on is cut back to it, in all
  four ports. Its columns are as wide as their widest field, which can come
  to more than the page holds: the nine columns of Example_43 came to 795
  points of a 792 point landscape page, so the last of them was drawn off the
  right edge, where it is lost. The columns are cut from the last one on, and
  no further than the width of the mark below, so the table keeps the widths
  it asked for as far as the page allows. A field of a column that was cut is
  drawn with as much of its text as fits, cut between code points in every
  port, ending in " ..." in place of the
  last four characters of it, so the reader can see it was cut; the cell of a
  PDF/UA structure tree keeps the whole of the text, which is what a screen
  reader reads. A column that was not cut is as wide as its widest field, so
  every field of it is drawn whole and none is measured as it is drawn, which
  is what keeps a table of 2000+ pages as quick as it was. Example_43 is the
  one example that changes.
- A cell whose text wraps draws the border under it once, under the last of
  its lines, in all four ports. The rows a table wraps a cell into are one
  cell -- they hold the lines of its text and are one cell of a PDF/UA
  structure tree -- but each of them kept the borders of the cell it came
  from, so a cell of seven lines drew seven rules across itself and a table
  of wrapped cells came out ruled like a ledger. The top border stays on the
  first of the rows, as it was, the bottom border moves to the last, and the
  left and right borders are drawn down all of them, which is what makes the
  rows read as one cell. A cell that spans rows is drawn over all of them at
  once and so draws its own bottom border under the whole of it, unchanged.
  None of the 51 examples draws a wrapped cell with a bottom border, so no
  example PDF changes.
- A `Chart`, a `BarChart` and a `DonutChart` leave the page with the pen, the
  brush and the dash pattern they found on it, in all four ports, as a `Stamp`
  and a `CalendarMonth` do. `Chart` and `BarChart` set them to the default of
  a page when they were done, which is not what the caller had, and
  `DonutChart` left the black pen of its pointers and the color of its last
  slice: a line drawn after a chart came out in the wrong pen. They save the
  graphics state and restore it now, which also keeps the font and the line
  styles. The five example pages that draw a chart render pixel for pixel as
  they did. Found in the review of the barcodes, the charts, `Form`,
  `Container` and `Stamp` of Sep 21.
- A `Container` draws its annotations where it draws them, however often it is
  drawn, in all four ports. It moved the corners of every annotation it holds
  by its own location on each drawing, so a container on a second page put its
  annotations that far from what it drew there: a container at 50, 60 with a
  square at 10, 10 drew the square in the same place on three pages and left
  its annotation at 60,722, then 110,662, then 160,602. The corners are put
  back after the drawing now. Found in the review of the barcodes, the charts,
  `Form`, `Container` and `Stamp` of Sep 21; of the 28 drawables PDFjet has,
  only a `TextFrame` and a `Table` draw something else the second time, which
  is what makes their text and their rows flow from page to page.
- A column of a `BigTable` is as wide as the font of each row draws it, in all
  four ports. Every row was measured with the header font, so a body font
  wider than the header font ran over the column on its right: with a header
  in Helvetica-Bold 8 and a body in Helvetica 14, a cell of "wwww" was 40
  points wide in a column 27 points wide, and was drawn over the cell beside
  it. The header is measured with the header font and each row with the body
  font now. The table of Example_43, whose header is IBM Plex Sans SemiBold
  10 and whose rows are Regular 9, is narrower for it and holds the same
  2,546 pages. Found in the review of `TextFrame`, `BigTable`,
  `CompositeTextLine` and `Bidi` of Sep 21.
- `BigTable.setTextAlignment` refuses a column the table does not have, in all
  four ports, where it wrote into the alignments of the columns whatever the
  column was: it threw before `setTableData`, which makes them, and for a
  column past the last one. It is recorded on the PDF as misuse, as a negative
  column index and a negative padding are, and `complete` then refuses to
  write the document.
- A PDF embeds each font program once, where it embedded the first of two
  fonts of one name for both, in all four ports. PDFjet ships subsets of the
  Noto Sans SC and TC fonts beside the whole fonts, twelve pairs whose name
  inside is the same, and 3,821 of the 3,917 characters both hold have a
  different glyph in the subset: a document that drew with both drew the text
  of the second in the glyphs of the first, so 中文字 came out as Ι㈜♡, which
  is what pdftotext and MuPDF read from it. The font file, the font
  descriptor, the CID font dictionary and the ToUnicode map are shared now
  when the name and a checksum of the font program -- the units per em, the
  advance widths and the character map -- are both the same, so a font read
  twice, or read from a `.otf` and from the `.stream` file made of it, is
  still embedded once, as Example_28 draws it. Found in the review of `Font`
  and its loaders of Sep 21.
- A page that `PDF.getPageObjects` returns holds the entries it inherits from
  the page tree -- `/Resources`, `/MediaBox`, `/CropBox` and `/Rotate` -- in
  all four ports. A PDF may write them once on a node above the pages rather
  than on every page, as the form of Example_50 does, and `read` gives the
  objects as the file has them: `getPageSize` read every such page as letter
  size whatever its size was, and `getResourcesObject` found nothing, so
  `addResourceObjects` copied none of the fonts and images of its pages. A
  page that has an entry of its own keeps it, the entries are added once
  however often the pages are returned, and a merge, which resolved them for
  itself, is unchanged. The page of Example_50 is written with its
  `/MediaBox` now, 52 bytes more.
- The methods of `PDFobj` that add a font, an image, a content stream or a
  graphics state to a page of a PDF that was read check the tokens they index
  and the objects they name, in all four ports. Every one of them read the
  token after a key without looking, and took the number after `/Resources`,
  `/Font`, `/XObject`, `/ExtGState` or `/Contents` for an object of the file:
  a page whose dictionary ends where a value belongs, or that names an object
  the file does not have, threw in Java, C# and Go and trapped in Swift, which
  ends the program. A page that does not hold what a method needs is left as
  it is now. Found in the review of the reader of Sep 21 and by the fuzz
  target, which drives these methods now.
- `PDFobj.addResource(coreFont, objects)` adds the font to the first
  `/Resources` of the page and stops, in all four ports. It went on over the
  whole page dictionary, and adding the font grows it, so a resources object
  whose `/Font` names its own page grew the dictionary without end: a PDF of
  1,264 bytes took every byte of memory there is. A page has one `/Resources`,
  so nothing else changes.
- The `/Length` of a stream that is not a number, and a dictionary that ends
  where it belongs, fail with a message in Java, C# and Go, as they do in
  Swift. `/Length` is read for every stream of a PDF that is read, and the
  value, the two tokens after it and the fourth token of the object a
  reference names were all read without looking: `<< /Length stream` threw a
  `NumberFormatException` in Java, and a `/Length` that names an object of
  three tokens an index error there and in C#, where Go turned the index
  error into an error of its own and Swift already said what was wrong.
- `PDFobj.getPageSize` returns letter size for a `/MediaBox` that is not four
  numbers, in all four ports, where it read the fourth and fifth token after
  the key whatever they were: `[0 0 612` read past the tokens in Java, C# and
  Go, and trapped in Swift, as `[a b c d]` did.
- `PDFobj.getPageSize` measures the page between the corners of its
  `/MediaBox`, in all four ports, where it took the last two numbers of the
  box for the width and the height. The box is a rectangle of two opposite
  corners, in either order, and its origin is not always `0 0`: a page whose
  box is `[9 9 621 801]` is 612 by 792 points, not 621 by 801, and a stamp
  placed by the width was 9 points off on every such page.
- The `/XObject`, `/Font` and `/ExtGState` of the resources of a page can be
  an object of their own, and the Java and C# ports took the number of that
  object without checking it, where Go and Swift did: `addResourceObjects`
  threw on a page whose `/XObject` names an object the file does not have.
- The generation number that the key of an object is made from is read only
  when the object has one, in the Java and C# ports as in Go and Swift.
- The fuzz target of the reader, `FuzzPDFRead`, fails on a Go runtime error
  where it read one as an ordinary error before. `PDF.ReadWithPassword`
  recovers from a panic and returns it, which is how the Go port carries the
  errors the other ports throw, and an index out of range came back the same
  way: every index error of the reader was invisible to the fuzzer, though
  the same input throws in Java and C# and traps in Swift. The target also
  drives what a merge, a split and a stamp do with what was read -- the page
  size, the resources, `MergePages`, `AddObjects`, `AddResourceObjects` and
  the `PDFobj` methods that add to a page -- and fails on a runtime error in
  any of them.
- `Page.transform` divides the height of the page by the vertical scale, and
  the height is now saved and restored with the graphics state, in all four
  ports. It was left divided, so every y coordinate after the restore was
  measured from the scaled height: a line drawn at y 100 of a Letter page went
  to 296 instead of 692 after a `q`, a transform by 2 and a `Q`.
- A JPEG whose frame header is not the length of its component
  specifications, three bytes for each component after its eight, is refused
  in all four ports, where PDFjet read the header and embedded the file.
  libjpeg refuses it as a "Bogus SOF length" and so does every reader a PDF is
  drawn with: MuPDF drew nothing of `images/610-30x30.jpg` with a length three
  bytes out, and said "jpeg error: Bogus marker length". Found by reading
  `JPGImage` against libjpeg in the review of Sep 21; the 7 JPEG files of the
  repository are all of the right length.
- A PNG whose IHDR names a compression or a filter method other than 0, the
  only ones the format defines, is refused in all four ports. PDFjet read the
  rows of such a file as if the method were 0, where libpng refuses both and
  Pillow refuses the filter method.
- The width and the height of an `<svg>` element are read the same way in the
  four ports, and a length with a unit is read at last: `48`, `48px` and
  `48pt` are 48 points, and `pc`, `in`, `mm` and `cm` are converted, so the
  `210mm` of a page-size SVG file is 595.28 points. Go refused the whole file,
  and Java, C# and Swift took the size as 0, which scaled every path of a file
  with a viewBox to the origin and drew nothing. A size PDFjet cannot read, a
  percentage among them, and a size the file does not give, now leave the
  drawing the size of its viewBox. A viewBox that is not four numbers, or
  whose width or height is zero, fails with the same message in the four
  ports: Java and C# threw an index out of bounds or wrote coordinates that
  are not numbers, and Swift drew nothing at all.
- Example_17 draws `OI2N2C16.PNG` and `OI2N0G16.PNG`, the PngSuite images of
  two IDAT chunks, where it drew the four-chunk file twice and said the first
  of them had two, so the two-chunk case was never read.
- A PDF that is not valid fails with a message or is read for what it holds,
  in all four ports, where reading one could take gigabytes of memory, read
  past the end of the file, or trap in Swift. An object numbered higher than
  the file has bytes made one empty object of every number up to it, so a
  31-byte file numbered 44,444,441 took 7 GB; a /Length longer than the file
  was allocated before the bytes were found not to be there; an object stream
  with no stream of its own, or one whose header is not a number, failed
  differently in each port; and a dictionary that ends where a value belongs,
  or a reference to an object that is not in the file, read past the end of
  the tokens in `getValue`, `getObjectNumbers`, `getContentObject` and
  `getResourcesObject`. A dictionary or an array is now closed where the PDF
  ends, a reference to an object that is not there has no value, and the
  object streams are read alike in the four ports. Found by fuzzing the Go
  reader with `FuzzPDFRead`, which fuzzes the bytes of a PDF and the password
  it is read with, and reads, pages and merges what it gets; 95 M runs clean
  after the fixes. Of the 1,257 PDFs of the replay the four ports read the
  same ones and draw the same pages from every one.
- Swift read one symbol too many from a Flate stream when only the first
  bytes of it were asked for. `Decompressor.inflatePrefix` returns the first
  bytes a stream decodes to and ignores the rest of it, but the decoder of
  Swift, which is a port of Mark Adler's `puff.c` and not a library, decoded
  the symbol after those bytes before it stopped. A stream cut short right
  after them then failed in Swift where it read in Java, C# and Go, which is
  what a font whose file is read in pieces, or a PDF cut short, can look
  like. Found by fuzzing the Go filters with `FuzzDecompressor`, which runs
  every filter of a PDF stream -- Flate, LZW, ASCIIHex, ASCII85, RunLength
  and the predictor of a /DecodeParms -- on the same bytes, and
  `FuzzDeflateRoundTrip`, which checks that what PDFjet writes compressed
  reads back as the same bytes; 46.5 M and 12.9 M runs clean. Of the 1,066
  streams of the replay the four ports now decode every one to the same
  bytes with every filter.
- An OpenType or TrueType font (`.otf`, `.ttf`) that is not valid fails with
  "Invalid font file: ..." in all four ports, where it read past the end of
  the file, followed a character map that was not there, made an array of a
  negative size, or took every byte of memory there is reading a GPOS table
  that says it holds more than any font does. Every read of a font now checks
  that its bytes are there. A font is refused when it has no character map,
  when its character map is not the format 4 subtable PDFjet reads, when its
  units per em are outside the range OpenType allows, when it has no advance
  widths, when its CFF table is not inside the file, or when its name is not
  one a PDF name can hold, as a stream font with such a name is refused. A
  name record outside the font, and a character map segment that points
  outside its glyph ID array, are left out rather than read. The GPOS table
  is read for as much work as a font needs, bounded by the glyphs of the
  font: the glyphs of its coverage tables and the room their indexes make,
  and the marks, the letters and the pairs of its mark lookups, count toward
  it, so a font whose GPOS table claims more than it holds, such as 40 KB of
  subtables that are one another, loads at once. The most the 252 fonts
  PDFjet ships take is 76,168 of the 1,048,576 allowed. Java also failed with
  a null pointer on a font with no name, and Swift trapped on a font with no
  `OS/2` or `post` table, which Go and C# read as zeros. Found by
  fuzzing the Go font loaders with `FuzzOpenTypeFont`, which fuzzes a whole
  file, and `FuzzOpenTypeFontTables`, which fuzzes each table PDFjet reads on
  its own and joins them into a font whose directory is right; 21 M and 8.5 M
  runs clean after the fixes. Of the 2,181 fonts of the replay, the four
  ports read the same ones and draw the same page from every one they read.
- The four ports read the same text from bytes that are not UTF-8. Java read
  an encoded surrogate, the three bytes ED A0 80, as one U+FFFD, and Go read a
  sequence cut short at the end of a file as one U+FFFD a byte, where C# and
  Swift replace the maximal subparts of an ill formed sequence, which is what
  the Unicode Standard recommends in section 3.9: ED A0 80 is three
  replacements, and E2 82 at the end of a file is one. Java has a decoder of
  its own now, `UTF8`, and Go the package `internal/utf8text`; both are used
  wherever the bytes of a file or a font become text — `Content.ofTextFile`,
  `Table`, `BigTable`, `OTF` and the `.stream` font reader. Swift `Table` no
  longer fails the whole file when it is not UTF-8, and drops a byte order
  mark as the other ports do, and Swift `OTF` no longer drops the name of a
  font whose Macintosh name record is not UTF-8. Well formed UTF-8 reads as
  before in all four ports, and the example documents are unchanged.
- A BMP file could allocate a gigabyte for a skip (in Go) or an image size
  (in the four ports) its data did not have: a 76-byte file 1 GB. What is
  skipped is read as it comes, and the rows before the image is allocated. A
  palette index past the palette is black, as browsers draw it, where it
  failed, and the last row can end without its padding. Found by fuzzing the
  Go BMP decoder and comparing it with Pillow.
- The alpha of a 16 or 32-bit BMP with `BI_BITFIELDS` and a V3, V4 or V5
  header, which has an alpha mask after the red, green and blue ones, was not
  read, and the image was drawn opaque, in all four ports. Its alpha is the
  `/SMask` of the image now, as that of a PNG is. An image whose alpha is 0 in
  every pixel is drawn opaque, as Chrome and Firefox draw it, since writers
  that do not know of the alpha leave it at 0; a 32-bit `BI_RGB` BMP stays
  opaque, and `BI_ALPHABITFIELDS` stays refused, as Pillow refuses it. The
  12-byte header of OS/2 1.x is refused with "Unsupported BMP header of 12
  bytes." rather than with the message of a bit depth, which was read from
  where a larger header has it. Found by the image check against Pillow.
- A PNG palette image with an index past its palette colors failed with an
  index error; the pixel is black, as libpng and browsers draw it. A palette
  of no colors or of more than 256 fails with "Incorrect palette length."
  Found by fuzzing the Go PNG decoder against Go's image/png.
- The image data of a PNG after the rows of the image is ignored in all four
  ports: Java and C# failed on a wrong Adler-32 when the stream ended right
  after the rows, and Go on a stream cut short there.
- A stream font (`.otf.stream`, `.ttf.stream`) that is not valid fails with
  "Invalid font stream: ..." or an end of stream error, in all four ports,
  where it could allocate the gigabytes its lengths gave before reading them,
  read its metrics and marks past their end, or trap in Swift. The units per
  em, the character range, the tables, the mark classes and the font name,
  which is written as a PDF name, are checked, and the font file is embedded
  at the length the stream gives, and in Go embedding the font file no longer
  loops on a read error. Found by fuzzing the Go reader; the fuzz targets are
  in `src/fontstream_fuzz_test.go`.
- A glyph past the advance widths of a font, which OpenType allows when the
  last glyphs share a width, had the width of the first glyph, in measured
  text and as the `/DW` of the PDF font; it has the width of the last one
  now. In IBM Plex Sans JP, ↺ and 14 other arrows are such glyphs, and one
  followed by a combining mark threw an index error, or trapped in Swift. No
  font PDFjet ships draws a different width, but the `/DW` of most fonts
  changes, which the glyphs of their `/W` arrays do not use.
- Swift decoded zlib data with a wrong header or checksum, which the other
  ports reject, and drew the marks of a stream font whose mark data could not
  be read where they were; it checks both, and fails the document, now.
- A fallback font drew only the characters the font had no glyph for until
  the fallback font lacked one: in "abc日本def" the "def", and the spaces and
  digits after Japanese text, were in the fallback font; a character that
  neither font had was drawn as .notdef in the fallback font; and a core font
  ignored its fallback font. Each character is now drawn in the font when it
  has a glyph for it, else in the fallback font when that has one, else in
  the font; a combining mark stays with the character before it when that font
  has it; and a core font can have a fallback font or be one. `TextLine`,
  `TextBlock` and the widths they measure follow the same rule.
- A core font drew the characters WinAnsi puts from 128 to 159 as spaces,
  since it took their Unicode values, which are above 255, for their codes:
  "Don’t pay €5 — “Hi”" came out as "Don t pay  5    Hi ". ’ “ ” ‘ – — € … • ™
  and the others are mapped to their WinAnsi codes when text is measured,
  fitted and drawn, and are kerned. The soft hyphen, drawn as a hyphen, was
  measured as a space, and neither it nor the no-break space had the kerning
  pairs of the glyph it is drawn with; the metrics of the fourteen core fonts
  now agree with their AFM files in `fonts/Core`.
- A 32-bit BMP was drawn with its colors shifted, since its pixels were read
  as a byte that is not a color followed by blue, green and red, where they are
  blue, green, red and that byte. A 1, 4 or 8-bit BMP with a header larger than
  40 bytes, as GIMP and Photoshop write, failed, since the palette was read
  where the header went on; the palette now follows the header of the size the
  file gives, and the pixels start at the offset it gives. A compressed BMP,
  RLE8 or RLE4, was read as if its data were pixels, and is now refused. The
  masks of a 16 or 32-bit BMP are read, where a 16-bit image with masks was
  taken for 5, 6 and 5 bits, and a color of fewer than 8 bits is scaled to the
  full range, so that white is 255 rather than 248.
- A CMYK JPEG is written with the Decode array that inverts its inks back only
  when an APP14 marker says that Adobe software wrote it, which stores the inks
  inverted; every CMYK JPEG had the array, so one from other software came out
  as a negative.
- A JPEG whose header holds a marker with no parameter segment — a restart
  marker, TEM, a nested SOI, or the 0xFF 0x00 of a stuffed 0xFF byte — had the
  two bytes after it read as a length, which skipped over the frame header, so
  the image failed to load or was drawn at the wrong size; those markers carry
  nothing to skip, as in libjpeg, and a JPEG that ends at its EOI marker before
  the frame header fails with a message. An APP14 segment says that Adobe
  software wrote the image, and so that the inks of a CMYK image are inverted,
  only when it is the twelve bytes Adobe writes, as in libjpeg, where its first
  five were enough; and an APP14 segment of another kind after Adobe's no
  longer unmarks the image, which wrote such a CMYK image as a negative. The
  segment is looked for from the start of the file to the scan, as libjpeg's
  `jpeg_read_header` reads a header, where PDFjet stopped at the frame header
  and missed one that follows it. Found by fuzzing the Go reader against Go's
  image/jpeg; the fuzz target is in `src/jpgimage_fuzz_test.go`.
- SVG path data written over more than one line was read wrong, and in Go it
  failed the image with "invalid path data": only a space and a comma
  separated the numbers, so a line feed or a tab between them made one number
  of two. The white space of SVG 1.1 section 8.3.9, which is the white space
  of XML, separates them now. Java, C# and Swift drew such a path because
  their XML parsers turn the white space of an attribute into spaces, where
  Go's does not.
- SVG path data with a number written in exponent notation, `1.5e-3` or
  `1000E-2`, failed with "invalid path data": the sign of the exponent was
  read as the start of the next number, so the image was refused. A sign
  belongs to the number when it follows the `e` of an exponent, and a `+`
  starts a number where nothing separates it from the one before, as SVG 1.1
  writes them.
- An SVG elliptical arc whose flags are written without a separator after
  them, `A10 10 0 0120 20` for `A10 10 0 0 1 20 20`, drew nothing: the two
  flags and the number that follows were read as one number, so the arc had
  six arguments where it needs seven, and it was dropped without a message.
  A flag of an arc is one character, `0` or `1`, which SVG 1.1 section 8.3.9
  needs no separator after, and which is how minifiers write path data. Only
  the two flags are read this way; the radii, the rotation and the end point
  of an arc are numbers like any other.
- An SVG elliptical arc was drawn with one cubic curve more in one port than
  in another. An arc is drawn in pieces of at most a quarter turn, and the
  sweep it is cut into them by is computed with `atan2`, whose last bit the
  four languages do not agree on, so an arc of a whole number of quarter turns
  fell on either side of the cut. A sweep is now taken as a whole number of
  quarter turns when it is within a rounding error of one, and the center of
  an arc whose radii were too small for its end points, and were scaled up to
  fit them, is the middle of the chord rather than a square root of the
  difference of two nearly equal numbers. The curves drawn are the same arc as
  before. Found by replaying the Go SVG fuzz corpus in the four ports.
- A coordinate, size or width a PDF cannot hold — NaN, an infinity or a number
  of 2^31 or more — was left out of the content stream by Swift, so the
  operator lost an operand and the stream was no longer valid syntax; Go left
  such a number out of an object of the document. The misuse is recorded as
  before, and the number is written `0`, as Java and C# write it. Found by
  replaying the Go SVG fuzz corpus in the four ports.
- An SVG path threw or trapped in Java, C# and Swift where Go drew it: path
  data that starts with a number rather than a command threw a null pointer
  exception, as did a `<path>` element with no `d` attribute in Java and C#;
  path data whose first command needs a current point, `L90 40` or
  `Q10 10 90 40`, threw there and trapped in Swift, and an argument that is
  not a number trapped in Swift, where the other ports fail with a message.
  The numbers before the first command are left out, the current point starts
  at the origin, a path without data draws nothing, and Swift fails on an
  argument that is not a number, as in Go. A path of no operations now draws
  nothing at all, where it wrote its colors and a fill operator with no path.
  Found by fuzzing the Go SVG reader and replaying its corpus in the four
  ports; the fuzz targets are in `src/svgimage_fuzz_test.go`.
- A JPEG of other than eight bits per color component, a 12-bit one among
  them, was embedded with `/BitsPerComponent 8` whatever the sample precision
  of its frame header said, so it was drawn as noise; it fails with "The JPEG
  has 12 bits per color component, not 8." now, in the four ports. A PDF image
  stream of DCTDecode data delivers eight bit samples, and each color component
  value occupies a byte (ISO 32000-1, 7.4.8 and Table 89).
- Chinese and Japanese text in a `TextBlock` or `TextFrame` no longer starts a
  line with a closing mark, punctuation such as 。 and 、, or a small kana, nor
  ends one with an opening bracket (kinsoku shori): the character before such a
  mark moves to the next line with it.
- In a PDF/UA document a `Point` and the marker of a `Cell` that are not a
  link, and a QR code and a Data Matrix symbol that are not described, are
  artifacts, where they drew content that was neither tagged nor an artifact,
  and an annotation other than a link is in an `Annot` structure element
  rather than a `Link` one.
- A `Chart` kept the axis ranges of its first drawing, so a chart drawn again
  after new points kept its old axes; a range with 0 grid lines widened the
  range of the data; and a point's marker set after `addPoint` was ignored.
  The subtitles of `Chart` and `BarChart` and the labels inside the bars are
  drawn in their colors instead of black.
- A `CalendarMonth` left the page with its blue pen, drew the line under the
  names of the days once for each day, and left its circles untagged.
- A `TextFrame` wrapped a word early when there was room for it but not for
  the space after it, and broke a word as wide as the frame; a negative
  paragraph gap is 0.

### Deprecated
- `Compliance.PDF_A_3A`, in all four ports, for `PDF_A_3A_UA_1`, which is the
  same document, tagged as PDF/UA asks, that says it is PDF/UA-1 too.
  `PDF_A_3A` is still written as before.

### Removed
- `images/mt-map.bmp` and `images/mt-map.png`, a map of Malta in two formats,
  which no example draws and no test or fuzz target reads.
- `data/testPDFs/pdfjet-5.81-logo.pdf`, the logo PDFjet 5.81 wrote in 2016,
  which `data/testPDFs/PDFjetLogo.pdf` replaces everywhere it was read.
- `data/Electric_Vehicle_Population_5_Lines.csv`, and the ten files in `data`
  that nothing read, which the note on Example_24 below mentions.
- `images/readme/pdfjet-logo.png`, which `images/readme/pdfjet-logo.svg`
  replaces in the README, in the booklet and, with a dark variant, on the
  .NET API reference that docfx builds.

### Examples
- Example_57 is new: the table of Example_43 cut to 550 rows, 12 pages, as
  a PDF/UA document, in all four ports, to check how a `BigTable` is
  tagged, a TR for each row and a TH or a TD for each cell, at a size a
  PDF/UA checker opens. Example_43's 2,000 pages crash PAC, and tagged they
  would be 249 MB. The scripts, the workflows, the packaging and the lists
  of examples build, check and show 57 examples.
- A heading in every PDF/UA example, in all four ports: Example_01, 02, 09,
  13, 14, 16, 20, 23, 25, 27, 32, 35, 39, 40, 42 and 47 have an H1 on their
  first page, a line of their own or one they already drew, so that PAC's
  quality checks find a heading in each.
- The examples PAC reported, in all four ports: the addresses Example_09's
  table and Example_20's page show are links to them, as PAC asks of text
  that reads as an address; Example_14's caption of the GS1 symbol is on two
  lines, where it ran over the larger symbol beside it, black on black; and
  Example_35's text is clear of the square of its stamp. check-pdfua-tags.py
  checks the contrast of each letter against the page drawn without its
  text, which found the last two.
- The tables of Example_09 and Example_38 have a header row, tagged TH, in
  all four ports: "Country" over the markers and the names, and "Page about
  the country" over the links, in Example_09, and the numbers of the columns
  in Example_38. check-pdfua-tags.py noted them as tables with no header.
- The text of the examples contrasts with its background as WCAG 1.4.3 asks,
  4.5:1, in all four ports: gray text is dim gray, red text firebrick, and
  orange text sienna, and the colors of the charts of Example_25 and
  Example_39 are darker shades of themselves, so that their white labels can
  be read. PAC failed 25 examples on it. check-pdfua-tags.py checks the
  contrast of the text of the tagged examples, but for the DRAFT watermark
  of Example_07, which is decoration.
- Example_05 shows kerning, and only kerning: it says what kerning is, and
  draws the same words in two text blocks, one above the other, in
  Helvetica-Bold without kerning and with it, and by how many points kerning
  makes them narrower, in all four ports. It drew them as three lines of
  small text beside a wheel of rotated text, an arc and an ellipse, which are
  in Example_56 now.
- Example_56 is new: the shapes PDFjet draws, each in a cell under its name,
  `Line` with dashes and round caps, `Rect` with square and rounded corners,
  the `Point` markers of charts, `Ellipse`, `Arc`, a `Path` of Bézier curves
  and a closed `Path`, filled, and on a second page text at every angle
  around a point, as a PDF/UA document, in all four ports. The scripts, the
  workflows, the packaging and the lists of examples build, check and show 56
  examples.
- Example_55 is new: an invoice that is also data, in all four ports. It is a
  PDF/A-3A and PDF/UA-1 document, `PDF_A_3A_UA_1`, that carries
  `data/invoice/factur-x.xml`, the invoice in the profile BASIC of Factur-X
  and ZUGFeRD, with `addAssociatedFile` and the relationship Alternative, and
  says so in its metadata with `addMetadata`, with the extension schema of
  the properties, as those standards ask; no example carried a file for the
  document before. The page has a logo from an SVG file written as a drawing
  program exports one, a table of the items with a header row, striped rows
  and the totals in footer rows, and an EPC QR code that banking apps read as
  a SEPA credit transfer. veraPDF finds it PDF/A-3A and PDF/UA-1, the
  `factur-x` library of Python finds the XML and reads its profile from the
  metadata, the XML is valid against the XSD of the profile BASIC, and ZXing
  reads the QR code. The scripts, the workflows, the packaging and the lists
  of examples build, check and show 55 examples.
- Example_54 is new: `data/markdown/pdfjet.md` drawn by `Markdown` as a
  PDF/UA document with a number on every page, in all four ports, and the
  scripts, the workflows and the packaging build and check 54 examples.
- Example_53 is new: paragraphs written with the inline markup of Markdown,
  drawn with `Markup` in a text frame, with a numbered list, as a PDF/UA
  document, in all four ports. The booklet has a snippet of `Markup`, and the
  scripts, the workflows and the packaging build and check 53 examples.
- Example_52 is new: it sets the whole of Dostoyevsky's "The Idiot" as a
  book, in all four ports, with `TextFrame.drawOn(pdf, pages, pageSize)`. It
  makes a title page, then puts each chapter on new A5 pages in Source Serif
  4, with the parts as H1 and the chapters as H2 headings, justified
  paragraphs, the words marked with underscores in italics, and a number at
  the foot of every page. It makes 745 pages, the same in every port, as a
  PDF/UA document, and prints how long it takes: about 1.0 s in Java and C#,
  0.5 s in Go and 1.9 s in Swift. The text, `data/the-idiot.txt`, is Project
  Gutenberg's eBook #2638, the translation by Eva Martin of 1915, without the
  Gutenberg header and license. It is in the public domain;
  `data/the-idiot-source.txt` says where it is from. The scripts, the
  workflows and the packaging build and check 52 examples.
- Example_10 draws a map of the administrative divisions of Switzerland,
  `images/swiss-admin.jpg`, of the series of the maps of Italy, Spain and
  Greece the other examples draw, in place of the small map
  `images/sz-map.png`, which is removed. The map is 310 points
  tall and centered over the text, which moves down under it, and the example
  is one page, as before, in all four ports. `images/swiss-admin.txt` says
  where the map is from: it is in the public domain. The title "Switzerland"
  above the text is left out, since the map has it: "Introduction" and
  "Economy" are the headings of the page, H1 in the PDF/UA document, and look
  as they did.
- Example_32 and Example_34 draw within the page, in all four ports.
  Example_32 drew the source of Example_02 at 10 points on portrait pages,
  and its longest lines ran to x = 858 on a page 612 points wide; it draws it
  at 8 points on landscape pages now. Example_34's table ended at x = 625.45,
  which cut off the Internet Users column; it starts at x = 30 now, with the
  column widths as they were.
- Example_17 draws a line chart, which no example did: a `Chart` with a
  `Series` for each line, the points added in the order they are joined and
  `setDrawPath` drawing the line through them. The two lines are the electric
  vehicles of `data/Electric_Vehicle_Population_Data.csv`, the file
  Example_43 draws as a table of 2,546 pages, counted by model year and by
  kind -- battery electric against plug-in hybrid, 2011 to 2023. The axes are
  set with `setXAxisMinMax` and `setYAxisMinMax` rather than worked out from
  the points, so that every label of the years is a year. The other charts
  are Example_09, a scatter with a trend line, Example_25, a donut, and
  Example_39 and Example_40, bars.
- Example_24 draws the PNG pages Example_17 drew: a palette, grayscale and
  truecolor image of 8 and 16 bits, the alpha channel and the `tRNS` chunk
  that carry transparency, each over a yellow square so the soft mask can be
  seen, and the PNG that asks to be drawn at 300 dots per inch. It is the
  example of the image formats, so they belong with the JPEG, the PNG, the
  BMP and the CMYK JPEG it drew already; it is five pages now.
- `images/rgba-8bit-chunks.png` is drawn again at 1520 by 400 pixels, four
  times the 380 by 100 it was, so that at the 300 dots per inch its `pHYs`
  chunk asks for it is drawn 364.8 by 96 points rather than 91.2 by 24. It is
  drawn rather than enlarged, since enlarging adds no detail: the circles and
  the text are laid out again at the new size in IBM Plex Sans, the font the
  examples use, and it carries the same iCCP, bKGD, pHYs, tIME and two IDAT
  chunks its own text names. The text now says 1520 by 400, which is what it
  is.
- Example_17 draws a PNG of each kind PDFjet reads and says what each one is,
  where it drew 55 images of PngSuite on one page with nothing to tell them
  apart: the website called it a test case, which is what it was. It is a
  palette, grayscale and truecolor image of 8 and 16 bits, the alpha channel
  and the `tRNS` chunk that carry transparency -- each drawn over a yellow
  square so the soft mask can be seen -- and the `pHYs` chunk that asks for a
  size, each with what it is written under it and a description a screen
  reader can read. The images it no longer draws are covered by the tests
  instead, which check their samples rather than drawing them: `PNGImageTest`
  goes from 22 files to 58 in all four ports, and every CRC of the table was
  read from Pillow as well as from this decoder. Two tests beside it say what
  the new files are for: the images that are one image written in several
  ways -- in one, two, four and nine `IDAT` chunks, at four deflate levels,
  and with a background chunk beside them -- decode to the same samples, and
  the palette images of every width from 1 to 40 pixels are decoded whole,
  which is where the bits that pad a row to a byte are read as pixels or the
  last ones are lost.
- Example_15 draws chemical formulas rather than 300 cells of filler. It was
  a table of 60 rows by 5 columns over three pages where every body cell read
  "Hello 1 2" and every cell of the header row and the first column drew the
  same H2O, 64 times; the text set on those cells was never drawn, since the
  composite line replaced it. It is one page now: 29 formulas in five
  sections, the compounds from water to potassium alum, the organic ones from
  glucose to palmitic acid, the ions, the isotopes and four reactions, each
  drawn with `CompositeTextLine.addFormula` and each one structure element.
  The example is still the only one of `CompositeTextLine`, of
  `Cell.setCompositeTextLine` — which it no longer uses — and of a script
  position. Its line in `examples-java.html` and `examples-dotnet.html` says
  what it draws now.
- Example_20 draws the logo of pdfjet.com. `data/testPDFs/PDFjetLogo.pdf`,
  which it reads, Example_41 merges, the merge tests of the four ports read
  and `FuzzPDFRead` seeds, is written from `images/readme/pdfjet-logo.svg` by
  `util/make-logo-pdf.sh`, so the logo is vector paths on a Letter page and
  the file is 2,378 bytes instead of 76,062. It replaces the logo PDFjet 5.81
  wrote in 2016, which held the wordmark as text in an embedded Dosis-Bold
  font. The logo is in the top left corner of its page, where a reader opens
  the file at and where a one page logo belongs; it was in the bottom left
  corner. Example_20 draws the same letterhead as before: `drawContents`
  anchors the content by the point that is `height` points above the origin
  of its page, and with the logo at the top of its page that point is the top
  of the page, so the height to pass is the height of the page and no longer
  the height of the logo. The page Example_41 merges is the file itself, so
  the logo is at the top of it now.
- 28 more examples are PDF/UA documents: 02, 03, 06, 08, 09, 10, 11, 13, 14,
  17, 18, 19, 20, 21, 23, 24, 25, 26, 28, 29, 31, 32, 33, 35, 36, 38, 42 and
  45, and 39 and 40 before them. The ones that are not use core fonts or
  fonts that are not embedded (04, 05, 44, 50), are PDF/A (07, 34), show a
  layer that PDF/UA does not allow (46), draw on or merge existing PDFs (37,
  41, 51), or are Example_43.
- Example_02 prints the official texts of the Universal Declaration of Human
  Rights, its preamble and Articles 1 and 2, in Japanese, Korean, Simplified
  and Traditional Chinese, a paragraph on each line, where the files held cut
  or run-together texts with ASCII punctuation and a character the font lacked.
  Each page names its language, "This block is Japanese: 日本語", in IBM Plex
  Sans with the font of the page as its fallback font.
- Example_38 says on its page what it draws, where the page held nothing but
  the table: a title, and a paragraph on the cells that span columns with
  `setColSpan` and rows with `setRowSpan`. Its cells are a pastel mint rather
  than light blue.
- Example_37 draws its text left aligned with the scanned text under it.
- Example_40 draws the calendar of 2026, with the weeks starting on Monday.
- Example_24 draws a CMYK JPEG saved as Photoshop saves one, `images/cmyk.jpg`,
  a print test chart that `util/make-cmyk-jpeg.py` draws. It replaces
  `images/photoshop.jpg`, an advertisement no example used any more, and ten
  files in `data` that nothing read are gone.

## v9.0.1 — 2026-09-18

Producer string bumped from `PDFjet v9.0.0` to `PDFjet v9.0.1` in all four
ports, so `.packaging/package-java.sh` and `.packaging/package-dotnet.sh` name
their archives v9.0.1. The public API does not change, but for two things: the
C# `PDF` class is sealed, as the Java class is final, and the
`IBMPlexSansDevanagari` font constants are gone with the Devanagari fonts.

### Added
- `BaselineDrawable`, a drawable whose location is the baseline of its text
  rather than the top left corner of a box, in all four ports. `TextLine` and
  `CompositeTextLine` are such drawables, and a `Cell` draws either of them
  where it draws its own text, on the baseline its vertical alignment asks
  for, making room for the ascent and the descent of the line. A `TextLine`
  given to `Cell.setDrawable` used to be placed by its top left corner, so it
  was drawn above the cell in a cell 4 points tall. `TextLine.getAscent`,
  `TextLine.getDescent`, `CompositeTextLine.getAscent` and
  `CompositeTextLine.getDescent` are new; a drawable of your own becomes a
  line of text in a cell by implementing the interface.
- `CompositeTextLine.addFormula(font, formula)` builds the components of a
  chemical formula, in all four ports: the digits that follow an element or a
  closing bracket are subscripts, as the 2 of `H2O` and the 6, 12 and 6 of
  `C6H12O6`; a run of digits and signs after a circumflex is a superscript, as
  the charge of `Ca^2+` and `SO4^2-`; and everything else is drawn on the
  baseline, including a digit that begins the formula, as the 2 of `2H2O`.
  H2SO4 is now one call rather than five `TextLine` objects.
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

### Removed
- The IBM Plex Sans Devanagari and Noto Sans Devanagari fonts, and the
  `IBMPlexSansDevanagari` font constants of the four ports. PDFjet has no
  GSUB engine and no Indic reordering, so it draws Devanagari glyph by glyph,
  without the conjuncts and the reordering of vowel signs that Hindi and
  Marathi need, and the fonts only suggested that it could. A program that
  used the constants no longer compiles; one that passes the path of a
  Devanagari font file still runs, and draws it as before.

### Changed
- The Java and .NET packages ship the `.stream` fonts and, of the `.otf` and
  `.ttf` files they are made from, only the two that Example_28 reads. The
  Java package is 296 MB where v9.0.0 was 536 MB.
- The 113 `.otf.stream` fonts, the IBM Plex families, hold the whole font:
  after the metrics comes an `R` block with the tables that are not in the
  CFF data, compressed with Zopfli, so that the original `.otf` can be rebuilt
  from the stream byte for byte, as it always could from a `.ttf.stream`. The
  four ports skip the block and embed the same CFF data as before, and they
  still read streams without it. A library older than this one cannot read
  the new `.otf.stream` files, so the fonts directory and the library go
  together; `util/generate-stream-fonts-files.sh --old-format` writes streams
  that every version reads.
- Every `.otf.stream` and `.ttf.stream` font keeps where the GPOS table of the
  font puts the marks, so a stream font places Thai tone marks, Hebrew and
  Arabic vowel marks and combining accents as the `.otf` or `.ttf` file does,
  in all four ports. The data is compressed on its own at the end of the
  metrics, where an older library stops reading, and it is read the first
  time a mark is drawn in the font. Loading a Plex font takes as long as
  before, and a Noto Sans font, which has the most marks, about 0.8 ms more.
  Example_27 draws its Thai text with `IBMPlexSansThai.Regular` instead of
  reading the `.otf` file. The mark data adds 7.3 MB to the 272 streams, 5.9 MB
  of it in Noto Sans.
- Text is looked at once, rather than four times, to tell if it needs more
  than a glyph for each character: an RLM, LRM, ZWNJ or ZWJ, or a mark that
  the font places. Most text has neither and is drawn as before.
- The C# `PDF` class is `sealed`, as the Java class is `final`, and the Swift
  class is marked `final`, which it already was outside the module. A C#
  program that derived a class from `PDF` no longer compiles; nothing in the
  class was made to be overridden.
- In Go, `Complete` returns an error for a document too large for a
  cross-reference table, one with an object past byte 9,999,999,999, where it
  panicked; the other ports throw.
- The borders of a `Cell` are the subpaths of one path, stroked once, instead
  of a path and a stroke for each side, and a cell with no visible border
  writes nothing at all, where it wrote its pen width. Example_43 drawn with
  `Table` is 9,148 bytes smaller, 21,163,276 against 21,172,424, and takes the
  same time. The pages look the same.
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
- Eight bugs in `PDF`, the main class, in all four ports, found by reading the
  four ports side by side and running each case. Nested bookmarks make a
  correct outline tree: every outline item named the outline dictionary as its
  `/Parent`, and the dictionary had the last item of the deepest level as its
  `/Last` and the number of all the items as its `/Count`, so MuPDF reported
  "repaired broken tree structure in outline" for Example_48. An item now has
  the item above it as its parent, the dictionary ends at the last item of the
  first level, the counts are those of the items that are shown, and the `/F 4`
  entry, which means nothing in an outline item, is gone. Text strings are
  UTF-16BE with a byte order mark, as the information dictionary already was:
  the bookmark titles, the `/T` and `/Contents` of the annotations, the `/Alt`
  and `/ActualText` of the structure elements and the name of an optional
  content group were bare UTF-8, which readers take for PDFDocEncoding, so
  Poppler showed "Übersicht" as "Ãœbersicht" and a screen reader got the same
  from an alternate description. The structure tree is made of the pages of
  the document, in page order: a page made with `Page.DETACHED` and never
  added, like one used for a dry run, left structure elements with `/Pg 0 0 R`
  and an annotation that no page has, and moved the target of every link and
  bookmark by one object, so "go to page 2" led to page 1; and pages that were
  drawn in one order and added in another were read aloud in the order they
  were drawn. `addObjects` and `addResourceObjects` refuse what they cannot
  write: the objects keep their numbers, so adding them after a font, an image
  or a page silently wrote two objects under one number, and in an encrypted
  PDF they replaced the encryption dictionary and were written without
  encryption; both now fail with a message that says to add them first, or
  that an encrypted PDF cannot take them. `addResourceObjects` copies a font
  with every object it refers to, as it copies an image: the `/Widths` and
  `/Encoding` that Word writes as objects of their own were left behind, and
  their numbers were then given to new objects, so the widths of the font
  pointed at a content stream. `setEncryption` refuses a PDF/A document, which
  ISO 19005 does not allow to be encrypted and veraPDF failed on clause 6.1.3.
  A page tree in which a node lists itself or a node above it as a kid is read
  once, where `getPageObjects` and `merge` overflowed the stack, which ends a
  Go or Swift program; a kid that the document does not have is skipped, and
  objects without a page tree have no pages. And `Page.drawContents` puts a
  line feed after the content, which can end with an operator: `ET` followed
  by the `Q` that restores the graphics state read as the unknown `ETQ`.
- The name of an embedded file is a text string, in the `/F` and the `/UF`
  entries of its file specification, in all four ports. It was bare UTF-8 in
  `/F` alone, so `pdfdetach` listed and saved "Übersicht – résumé.txt" as
  "Ãœbersicht â•ﬁ rÃ©sumÃ©.txt". `/UF` is the entry that readers of PDF 1.7
  look for first, and PDF/A-3 requires both.
- Two more in the Swift port of `PDF`: `read` throws a `PDFjetError` for a
  malformed object stream, as Java throws and Go returns an error, where it
  stopped the program; and `complete()` throws for a number that is not
  writable found while it writes the page tree, like a NaN in a crop box,
  where it returned normally and left a `/CropBox` of three numbers. In Go a
  number that is refused is no longer written after it is refused.
- Five bugs in `CompositeTextLine`, in all four ports. Subscripts and
  superscripts are drawn smaller and off the baseline without a call to
  `setFontSize` first: the font size of the component is the base when the
  composite text line has none of its own, where a composite text line without
  one drew every component at its full size on the baseline, so `H2O` came out
  flat. `setFontSize` and the script factors and positions reach the components
  added before them, where they only applied to the components added
  afterwards. `getMinMaxY` and `getHeight` measure each component where it is
  drawn and at the size it is drawn at, where they read the metrics of the
  font object: a composite text line of a 12 point base and a subscript from
  fonts of other sizes reported 27.74 points for text that spans 14.44. An
  empty composite text line measures its own location rather than 0, 0, as the
  other drawables do. And `getWidth` adds up the components instead of
  trusting a running total, so it cannot go stale.
- A `Cell` holds a composite text line in the drawable it holds, rather than
  in a field of its own, so one field holds the content of a cell, as the
  image, the barcode, the text block and the text column already do.
  `setCompositeTextLine` and `getCompositeTextLine` are unchanged, and a
  composite given to `setDrawable` is drawn the same way. In Java the
  protected `Cell.compositeTextLine` field is gone; `getCompositeTextLine()`
  returns it. `Table` no longer wraps the text of a cell that draws a line of
  text of its own, as that text is not drawn.
- A `Cell` measures and draws a composite text line of its own, in all four
  ports. `setCompositeTextLine` needed the cell text to be set as well: the
  cell drew nothing and measured no height without it, and the text it needed
  was then thrown away. A cell with a composite text line is now as tall as
  the taller of its font and the composite.
- Four bugs in `TextColumn`, in all four ports. A right aligned, centered or
  justified line now reaches the edge of the column: every token was measured
  with the space that follows it, so the text stopped a space short of the
  right edge and justified lines were never flush. The height of a column is
  the height of its text: the spacing that goes between paragraphs was added
  after the last one as well and the descent of the last line was left out, so
  a column of one 12 point line reported 25.04 points instead of 13.87, and
  every table row holding a `TextColumn` was about 11 points too tall. A
  paragraph whose first word is wider than the column no longer starts with a
  blank line. And the underline or strikeout of a wrapped line stops at the
  text of the line instead of a space further on, and runs through a line that
  two text lines share, where it had a gap.
- `Table.setFontInColumn` and `setFontInRow` set the font of a `TextBlock` in a
  cell with `setFont`, in all four ports. They assigned the field, so the block
  kept the font size of the old font, and its fallback font still pointed at the
  old font: every character the new font lacked was drawn in the old one.
- Four bugs in `TextBlock`, in all four ports. A padding wider than half the
  block no longer breaks a word past its last character: it threw in Java and
  C#, panicked in Go and trapped in Swift, and a `Cell` narrower than its
  paddings reached it. The underline and the strikeout are drawn in the text
  color and as thick as the font says, where they took the color of the pen the
  page drew with and the width of the block's border. `setCornerRadius` rounds
  a block that has a background and no border, where the radius was dropped
  unless a border color was also set. And in Java and C#, `setTextColor(null)`
  with an array of floats leaves the text color unchanged, where it left the
  block with no text color, so the text took the brush color of the page.
- `Table.drawOn(pdf, pages, pageSize)` no longer asks for pages forever when a
  row is taller than the page, in all four ports. A row that does not fit was
  left for the next page, and a row that fits no page was left for page after
  page: the call filled memory with pages until it died, with no error. Such a
  row is now drawn on the page it starts, past the bottom margin, and the table
  goes on with the next row. Rows that fit are drawn exactly as before.
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
- `SVGImage.scaleBy` scales the width and the height of the image with its
  paths, in all four ports. `getWidth`, `getHeight` and the corner that
  `drawOn` returns kept the size before scaling, as did the click box of a
  link on the image.

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
- Examples 19, 33 and 45 explain what they show, in all four ports: two maps
  with a description next to each, each row below the taller of the two; a map
  of Europe scaled to fit the page and a set of labeled SVG icons; and a
  shipment request drawn with the `Form` class.
- Example_31 shows transparency alone: opaque and half transparent rectangles,
  a fill at four levels of alpha over a gray bar, and circles whose stroke and
  fill are transparent on their own. It drew Hindi and Marathi text first,
  which PDFjet draws glyph by glyph, without the conjuncts and the reordering
  of vowel signs that Devanagari needs.
- Example_28 reads IBM Plex Sans from its `.otf` file and Noto Sans from its
  `.ttf` file, and both from their `.stream` files, and says that any
  OpenType or TrueType file on the computer is a font for PDFjet. It drew
  rows of symbols from the Noto Sans Symbols stream.
- Example_46 draws a shaded relief map of Europe, from the public domain
  Natural Earth data, in three layers that a viewer can show or hide: the
  relief, the lines of latitude and longitude, which are not printed, and
  fifteen capital cities. It replaces a street map screenshot with a line
  and a QR code over it, in all four ports.
- `images/qrcode.png`, which Examples 17 and 50 draw, is a QR code for
  https://pdfjet.com with its quiet zone, still a 1-bit palette PNG; it was a
  sample for the QR generator's own web site with no margin around it.
  `images/map407.png`, an OpenStreetMap screenshot that only the misuse tests
  still read, is gone; they read a PngSuite image instead.
- Example_17 draws `images/rgba-8bit-chunks.png`, an 8-bit RGBA PNG that
  describes itself: anti-aliased text and half transparent circles on a
  transparent background, with iCCP, bKGD, pHYs and tIME chunks and two IDAT
  chunks. It replaces the logo and address of a company, a PNG of the same
  kind, in all four ports.
- Examples 37, 41 and 51 and the merge tests read
  `data/testPDFs/scanned-linearized.pdf`, which `util/make-scanned-pdf.py`
  writes, instead of a scanned magazine article. It has the structure of the
  article: PDF 1.2 with lines that end in a carriage return, linearized with
  hint tables, and five pages that are each a CCITT Group 4 image drawn by an
  array of two LZW content streams, with the length of every stream in an
  indirect object. Its pages describe these features.

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
