# PDFjet — the plan

The library work that is open, in the four ports (Java, C#, Go and Swift),
and the checks that hold before each release. What each release brought is in
CHANGELOG.md.

## v9.0.7

The first public release since 9.0.3. v9.0.5 was tagged on 12 October 2026
as an internal release, not published (the owner: "We will label v9.0.5
internal release and focus on v9.0.7"); nothing more is backported to it,
and 9.0.7 carries all of it: subsets of every embedded font, TrueType and
CFF, fonts added to an existing PDF included; the `.ttf` and `.otf` fonts
read directly, with no `.stream` files; `ImageSize`; the EXIF orientation of
JPEGs; the public XML parser; `Table.setCellPadding`; `addCJKParagraph` with
a language; the fixes of the code review of 11 October 2026 (#1 to #38),
passwords prepared with SASLprep among them; and, new in 9.0.7, fonts that
forbid embedding refused and the stroke width of an Arc. Only a major issue
holds it; anything else goes into 9.0.9.

The version is 9.0.7 in the four ports since 12 October 2026, after a
last review of the day's changes and an hour of fuzzing the font and text
targets. Left before the tag: the items for 9.0.7 below, the manual viewer
pass (below) on the files of the new code, then the release.

PDFjet Pro is released with it, its first release, on the same day, with
the same number (the owner, 11 October 2026: the two move together), the
Swiss QR-bill in it (merged on 12 October 2026). Right after the library's
tag: in pdfjet-pro, `./set-version.sh 9.0.7`, the tests against the release
(`PDFJET=published`), CHANGELOG.md, the tag v9.0.7 and push; then
pdfjet-pro made public, on the owner's go-ahead. Its CI builds against the
library's main branch until then, and on a tag against the library's tag of
the same number.

And pdfjet-server to the release: its go.mod requires a snapshot of the
library of 9 October (v9.0.4-0.20261009203557-ed6b1c9bcec8), which its CI
and the Forms that is deployed build with, so they have none of the fixes
since, the reader's limits among them, until it requires v9.0.7:
`go get github.com/edragoev1/pdfjet/v9@v9.0.7 && go mod tidy` in
pdfjet-server, its tests, commit and push. Locally its go.work uses the
library beside it, which has them already.

PDFjet Forms takes the number too (the owner, 11 October 2026): Forms is
9.0.7 when it is built on the library's 9.0.7, and tags pdfjet-server and
pdfjet-client v9.0.7 with the deploy that moves it there. Deploys of Forms
alone between the library's releases keep its number, told apart by their
commit and date. The layout's format version (1) and the look's rules
(rules 1) are not these numbers, and never move with them.

## Checks before each release

- **PDFjet Pro, the same day, with the same number.** The library is tagged
  first, then PDFjet Pro: `./set-version.sh X` in pdfjet-pro, its tests
  against the release, CHANGELOG.md, the tag vX. The library's CHANGELOG
  says what changed in it, plainly, a small release as small: never a change
  made only so that it seems to change.

- **The four ports agree.** `./check-examples.sh` builds and runs the
  examples and the unit tests of the four ports, compares the example PDFs
  with Java's, page by page and in every content stream, checks the PDF/UA and
  PDF/A files with veraPDF, opens the Java PDFs in PDFium, pdf.js and
  Poppler, and compares the snippets of the booklet in the four ports. The
  Build workflow runs the same checks on every push, but for the snippets,
  which it compiles and does not compare.
- **The public API.** `./check-api.sh v9.0.9` (the last release) lists what
  was added and what was removed in each port: nothing is removed and no
  signature changes but those util/api-removed.txt lists, each with the
  owner's word and a line of CHANGELOG.md, and what is added is what
  CHANGELOG.md lists.
- **The manual viewer pass.** The PDFs in `viewer-files-to-test`, opened by
  hand in Acrobat Reader, and with NVDA where a file is tagged; see the README
  there for the files of each release. One question is open from the pass of
  9.0.3: Example_42's shapes have their opacity in an ExtGState and in the
  annotation's `/CA`, and ISO 32000 applies `/CA` to the appearance, so a
  strict viewer could draw them at 25% where MuPDF, PDFium, Poppler and pdf.js
  draw them at 50%.
- **PAC.** The PDF/UA files in `pdf-ua-files-to-test`, checked with PAC on
  Windows; see the README there for which files go in.
- **Windows and macOS.** The Windows and macOS workflows, started by hand
  from the Actions tab on the commit to tag: Windows builds and runs the
  examples of the four ports, macOS the examples and the unit tests of Go.
  The Build workflow runs on Linux, with Swift on macOS too; these two are
  not run on every push, only before a release (the owner, 19 October 2026).
- **Benchmarks.** `benchmarks/run.sh` and the four ports' text and table
  benchmarks in `benchmarks/ports` and `benchmarks/table`, against the last
  release; the results are kept in their `results` folders.

## Open

- **Release v9.1.5 next; 9.1.3 is skipped** (the owner, 26 October 2026:
  "I have decided to skip v9.1.3 and just go directly to 9.1.5 - let's
  stop backporting things to 9.1.3"). What was done for 9.1.3, kerning, an
  image copied from a PDF keeping its masks, Java's Deflater for each
  thread, Swift's faster text and page compression, CalendarMonth removed,
  getTextBottom, TextFlow and the BarChart axis fix, goes out in 9.1.5,
  with SimpleBarChart and the charts work (CHANGELOG.md); the items below
  that said 9.1.3 say 9.1.5 now. Before it: the checks before each release
  (above), and PDFjet Pro 9.1.5 the same day. The font subsetters were
  fuzzed 8 hours each on 25 October 2026. Was: 9.1.3 in early December
  2026 on its own, 9.1.5 the charts in 2027.
  **In November 2026** (the owner, 26 October 2026: "will release 9.1.5 in
  November"), after the Forms launch of 9 and 10 November. The chart items
  of 9.1.5 below that are not done by then go to 9.1.7; SimpleBarChart is
  done and goes in 9.1.5.
  **9.1.5 is the long-term support version, and nothing more is added to
  it** (the owner, 27 October 2026: "I really don't want to add anything
  more to 9.1.5 - it will be our LTS version"): only the fixes of the
  review below; new work goes after it.
  **A complete code review before the release** (the owner, 27 October
  2026: "another complete code review before we release 9.1.5 but not
  now"): the four ports, everything changed since v9.1.1, after the Forms
  launch and before the release checks.

- ✅ Done for 9.1.5, 26 October 2026 (the owner: "let's start implementing
  these"): setVerticalPadding, setHeaderColor, setFooterFontSize and
  setFooterOffset in the
  four ports, tests, the booklet's big-table snippet, CHANGELOG.md; the
  statement as SPEC.md lays it out is 12 pages in 5.6 ms, Table's 7.7.
  Was: **After 9.1.5: BigTable to draw an ordinary statement (found 26 October
  2026, the owner: "we can fix this for future versions").** The
  benchmark's statement (pdf-benchmarks SPEC.md) could not be drawn with
  BigTable, so it uses Table; a BigTable version as close as it allows took
  5.3 ms to Table's 7.5, but on 11 pages to 12, as it lacks three things,
  in the four ports:
  - Space above and below the text of a row: a row is as tall as its font,
    and setPadding is the space at the sides only. Add a vertical padding
    (the statement's is 0.75 pt).
  - A background for the header row of its own: setShadingColor shades the
    rows, not the header. Add a header color.
  - A size for the footer: it is drawn in a font at that font's size, so an
    8 pt footer under a 9 pt table needs the font loaded again. Add a size,
    drawn with TextLine.setFontSize.
  Tests in each port; CHANGELOG.md; then the benchmark's statement could
  use BigTable, in a rerun.
  Considered, probably not (the owner, 26 October 2026: "too much work,
  record it to consider but probably no"): one pass over the rows instead
  of two. The first pass measures the columns, their alignment and the
  rows, whose count gives the "Page n of N" footer; with N drawn as a Form
  XObject written after the last page, a table of given widths and
  alignments would need one pass. The writer would reserve an object and
  write it last, and a centered footer would need room for N kept.

- ✅ Done 25 October 2026. **v9.1.5: an image copied from a PDF that was
  read keeps its transparency, in the four ports (found 22 October 2026).**
  Each object read keeps the objects of its PDF by number (weakly in
  Swift), so the image's /SMask and a /Mask that refers to a stencil are
  copied as image objects of their own; /Decode and /Matte copied too. new Image(pdf,
  obj) in Java and C#, NewImageFromPDFobj in Go and init(pdf, obj) in Swift
  copy the image's stream as it is, but leave out /SMask (the alpha of a
  PNG, a separate image object) and /Mask (a color key, an array, or a
  reference to a stencil image), with no error: a logo on a transparent
  background is drawn on a solid box. The constructors already refuse a
  /ColorSpace or /DecodeParms that refers to another object. Fix: copy the
  soft mask with the image (an image object of its own, its stream as it
  is, encrypted when the document is), and the /Mask array as it is; a
  /Mask that refers to an object copied as the soft mask is. Tests in each
  port: a PNG with alpha and one with a color key, copied, keep their masks
  byte for byte; CHANGELOG.md.

- ✅ **Swift, the rest of a slowdown of 9 to 15 October 2026 (found 20 October,
  at 9.1.1): looked into and closed, 27 October 2026.** The ports
  benchmark's 500-page text: 86 ms at 413e3352, 98 at f511f976, 96 at
  9b52f4dc, measured together. Bisected: 2c0df767 and 98b1ca11 86, 639ce7a2
  86, c11ff7ed 92 (review #9, Font.isIgnorable, about twenty comparisons
  for each character above U+034F, Greek and Cyrillic among them), 6caf01ae
  93; then the drawn glyph of review #8 and the glyph set of 9.1.5 (perf:
  Page.appendPlainGlyphs 6%, Subset.GlyphSet.add 3%, its copy-on-write
  check of each glyph). Two simple changes, the PDF the same: isIgnorable
  answers at once for U+0350 to U+061B and U+3165 to U+FDFF, in the four
  ports, and GlyphSet adds the glyphs of a run together, in Swift; 92 ms.
  The other 6 ms are those fixes' work for each character: no major change
  (the owner, 27 October 2026). The README claims under 120 ms. Example_17,
  the 745-page novel, on 20 October: Go 398 ms, Java 972, C# 1,246, Swift
  1,505 (each a new process), for v9.2.1's restructuring.

- ✅ **v9.1.1: the first PDF of a process faster, in the four ports.** Measured
  on 15 October 2026 with typical documents, a one-page invoice, a 9-page
  report and an 11-page Markdown text (branch measure/common-documents,
  benchmarks/common): warm, complete() takes 2 to 4 ms, so the encoding of
  the text hardly counts; the first PDF of a process does. Java: new PDF()
  took 55 ms, SimpleDateFormat 37 ms, SecureRandom 22 ms and Logger.getLogger
  14 ms; with the date worked out by hand (Howard Hinnant's civil from days,
  the same as SimpleDateFormat on 100,000 times), the document ID from
  ThreadLocalRandom and the logger made when it is used, new PDF() takes
  4 ms and the first invoice 44 ms, not 89. C#: new PDF() 13.5 ms,
  RandomNumberGenerator 7.9 ms and the date's ToString 5.5 ms, of a first PDF
  of 45 ms. Swift: DateFormatter 1.7 ms of 5 ms. Go: 1.5 ms in all. To do in
  the four ports, so that they stay alike: the date by hand, and the
  document ID from a random generator that is not a cryptographic one, as it
  tells documents apart and is no secret (encryption keeps its own); in Java
  the logger made when it is used. Done on 15 October 2026 (0cb12f10): Java,
  C# and Swift; Go needed nothing. CHANGELOG.md, v9.1.1.

- ✅ **v9.1.1: from Stefan Ostermann's fork of PDFjet (soster/pdfjet, used
  by his Android app, which draws on PDFs that were read).** Done on 19
  October 2026 on the branch stefan-fixes, in the four ports, with tests in
  each; merged into main for 9.1.1, the owner's choice. Written from his descriptions, not his
  code (his fork is BSD-2-Clause), and credited to him in the CHANGELOG.
  - **A filled-in form that is drawn on keeps its fields.** addObjects()
    wrote a new catalog of /Type and /Pages only, so the /AcroForm of the
    PDF that was read was lost and viewers showed no field values. The new
    catalog carries it over, a dictionary or a reference. Only /AcroForm:
    outlines, names and a structure tree are still not carried over (a
    structure tree would be wrong with untagged content drawn on top).
  - **GraphicsState.setBlendMode()**, the sixteen blend modes of the PDF
    (BlendMode; blendmode.BlendMode in Go, a string type, "" for none), for
    new pages and pages that were read; no blend mode writes the state as
    before; PDF/A-1 takes only NORMAL. veraPDF passes PDF/A-2B and 3B with
    MULTIPLY. Asked by one developer; the owner chose to add it, 19 October
    2026.
  - **The same state set again on a page that was read is reused**, by
    exactly the same entries, as Page.setGraphicsState already did.
  - Already in PDFjet 9 before his fork had it: an /ExtGState found by its
    key whatever the names of its states (/Gs1, /R7).

- **v9.1.1: four small API fixes, from a developer evaluating the Java port
  (GitHub Discussion #5, 16 October 2026).** All additive or a check, all
  on Java 8, in the four ports where they apply; the owner, 16 October 2026:
  "add them to the TODO for 9.1.1".
  - (Done 17 October 2026, 86677f8e.) **complete() that leaves the stream open.** Java, C# and Swift close
    the output stream in complete(), so several PDFs cannot be written into
    one ZipOutputStream; Go already leaves a writer it is given open. Add
    complete(boolean closeStream) in Java and Swift, keeping today's default,
    and the leaveOpen of .NET's StreamWriter in C#. Until then: a
    FilterOutputStream whose close() does nothing.
  - (Done 17 October 2026: 35 internal methods of PDF, FontWriter,
    FontObjects and Subset narrowed, javac leading; PDFjet Pro builds and
    passes against it.) **v9.1.1: narrower exceptions on PDF's own methods
    (Java).** The
    owner, 17 October 2026, after weighing it: "OK - do the narrower Java
    exception too." complete() and the constructors declare IOException
    where that is all they throw. Weighed: a tidier signature, a careful
    refactor of the chain of internal methods complete() calls, and a
    source break, rare: a caller's catch of a checked exception other than
    IOException round complete() stops compiling, "exception is never
    thrown"; programs already compiled run as before; PDF is final; said in
    the CHANGELOG. Drawable.drawOn() keeps throws Exception.
  - **Not built: the end of a drawn component, by name.** Asked once
    (Discussion #5): a class with named getters in place of drawOn()'s
    float[] {x, y}. Designed on 17 October 2026 (ComponentEnd,
    drawAndGetEnd, one method a port), then dropped by the owner: "xy[0]
    and xy[1] are not difficult to decypher", and a public API is kept for
    good. Not to be proposed again unless more people ask. The reply to
    the developer, who was told it would come in 9.1.1, says so when 9.1.1
    is out.
  - (Done 17 October 2026, as a Cell's: a Cell already refused one since
    the third review of 12 October, so the setters stay as they are and a
    TextBlock is refused when drawn, in the four ports.)
    **A vertical alignment that can't be one is not taken.**
    setVerticalAlignment(Alignment.RIGHT), of TextBlock and Cell, is taken
    as TOP without a word, in the four ports. Java throws
    IllegalArgumentException and C# ArgumentException for anything but TOP,
    CENTER and BOTTOM. Go and Swift keep the alignment the object had, as
    said in their docs: Go has no exceptions, and a setter of Swift that
    throws, or one of Go that returns an error, would break the chains of
    setters every caller writes. A separate enum for vertical alignment
    would break existing code.
  - (Done 17 October 2026: 85 places, 74 of the library's folder and 11 of
    Barcodes and PDF417; a test keeps a plain one from coming back.)
    **C#: specific exceptions, not System.Exception** (found on 16 October
    2026, from the Java point above). 74 places throw a plain Exception, so
    a caller cannot tell a font that cannot be read from a file that cannot
    be written; throw ArgumentException, IOException, InvalidDataException
    or NotSupportedException, as the rest of the C# port already does, each
    by what went wrong. Breaks no caller: a catch of Exception still catches
    them all. Swift already throws its own PDFjetError, and Go returns
    errors.

- ✅ (17 October 2026: every job of JDK 21 on 25; the java8 job and
  check-examples.sh compile the library and the examples with JDK 8 and run
  nothing, the owner: "For Java 8 we will only do compile run.")
  **v9.1.1: CI on JDK 25 and JDK 8** (the owner, 17 October 2026: this
  machine on 25 by default; the versions between tried now and then by
  hand). JDK 23's javac warned of a doc comment before a package, an error
  with -Werror (fixed 17 October 2026, 07c7b7e5); 8, 11, 17, 21 and 25 all
  pass the 909 tests. The jobs of JDK 21 in build.yml, windows.yml and
  docs.yml move to 25, after the checks and the documentation pass on 25
  here.

- ✅ (17 October 2026: tests/swift/CryptographyTests.swift, all agree.)
  **v9.1.1: the Swift port's ciphers checked against published values.**
  Swift has its own AES, SHA-256, SHA-384, SHA-512, MD5 and RC4
  (Cryptography.swift), as Foundation has no cryptography on Linux; its
  tests open what it encrypts, but none checks a cipher against the
  standards' known answers: FIPS 197's and NIST's AES vectors, FIPS 180-4's
  and NIST's SHA ones, RFC 1321's MD5 and RFC 6229's RC4.

- **Dropped: faster compression of the hex text strings** (the owner,
  15 October 2026: "OUT"). zlib is slow on the "00" of hex glyph strings, a
  second of Example_24's 2,546 pages in Java, but typical documents of 5 to 10
  pages compress in 2 to 4 ms, and the hex strings stay. Not to be proposed
  again. The smaller BigTable wins were tried in Java and gave nothing.
- **v9.0.7: fonts and writing, faster and leaner, in the four ports.** The
  output byte for byte the same: every example and test PDF of the four
  ports written before and after, and compared. Done in 9.0.5 (the owner,
  12 October 2026): the ToUnicode map's character of each glyph found once
  when the font is read, the character-to-glyph table in two levels, and
  the glyphs drawn a set of bits; a page in three Plex fonts 25% faster in
  Go and 12 to 20% in the others, with half the memory or less, the 56
  examples of each port byte for byte the same. Done in 9.0.7, in the four
  ports: the GPOS table read when a mark is first drawn, and the format 4
  cmap read a segment at a time, and `Font.checksumOf` folding only the
  blocks of characters a font has glyphs in, which changes the subset tags
  (CHANGELOG.md).
  - Not to be done (the owner, 10 October 2026): a lower compression level
    for the font programs, which makes the files larger and changes their
    bytes; a Deflater for each thread in Java; a pool of FlateEncode's
    buffers in Swift, which gained 2% and nothing on top of reading a font
    once.

- ✅ Removed in 9.1.5 (the owner, 25 October 2026: "Remove the Calendar
  class - I don't think I even like its look anymore"): CalendarMonth, in
  the four ports, its tests and the booklet's snippet; its members recorded
  in util/api-removed.txt.
- **Done for 9.1.5: where a TextFrame's text ends** (the owner, 26 October
  2026: planned for 9.1.5, then "let add this now"). `drawOn(page)` returns the bottom of the frame, not of the text,
  when the frame has a height. It is documented, but easy to trip on: code
  placing a photo under the text read the frame's bottom and put each photo
  on a new page. Add a getter for the bottom of the text drawn by the last
  `drawOn`, `getTextBottom()` (Go `GetTextBottom`), in the four ports, with
  a test and the javadoc of `drawOn` pointing to it. `drawOn` returns what
  it does now, so no code changes its result. Done 26 October 2026, the
  four ports; check-api.sh v9.1.1 shows it as the only member added.
- **Done for 9.1.5: TextFlow, headings, paragraphs and images flowed onto
  pages** (named FlowText at first; TextFlow, the owner, 26 October 2026,
  as TextLine, TextBlock, TextColumn and TextFrame are named) (the owner, 26 October 2026: "core", for 9.1.5; then "ship
  TextFlow in 9.1.5", the same day, done and tested in the four ports). From writing common
  business documents in the four ports: a report of headings, paragraphs
  and photos is 53 lines today, the code moving a TextFrame from page to
  page and placing each photo by hand; about 20 with this. Its own class,
  not images inside TextFrame (the owner, 26 October 2026): TextFrame stays
  as it is, the frame placed by hand, and is not put at risk. A thin class
  over what the core has: TextFrame flows the paragraphs and makes the pages,
  `getTextBottom()` says where the text ends (9.1.5), Image has its size.
  - The API, Java (the others alike, Go `NewTextFlow`, `Add`...):
    ```java
    TextFlow flow = new TextFlow(pdf, Letter.PORTRAIT).setMargins(50f);
    flow.add(title);                       // a Paragraph, StructElem.H1
    flow.add(paragraphs, 6f);              // List<Paragraph>, the gap between them
    flow.add(photo);                       // an Image
    flow.add(caption).addSpace(12f);
    List<Page> pages = flow.drawOn();      // detached, for footers, then pdf.addPages
    ```
    `setMargins(float)` and `setMargins(top, right, bottom, left)`; the
    width of the text is the page's less the margins. `add(Paragraph)`,
    `add(List<Paragraph>, gap)`, `add(Image)`, `add(Image, Alignment)`,
    `addSpace(points)`,
    `drawOn()` returning the pages it made, and
    `drawOn(Page first, List<Page> pages)` to start on a page the caller has
    begun, below `setFirstPageTop(y)`, as Table's `setFirstPageTopMargin`.
  - The rules, few and plain:
    1. Paragraphs one after another are one TextFrame: they flow across
       pages, broken between lines, as TextFrame breaks them now.
    2. An image goes whole: on this page if it fits below the text, else at
       the top of the next. One taller than the space between the margins is
       scaled down to it, keeping its shape, never cut; one wider than the
       text, to the width. At the left margin, or as
       `add(Image, Alignment)` says: LEFT, CENTER or RIGHT, the one option.
    3. A heading, a paragraph of StructElem.H1 to H6, keeps with what
       follows: if the heading and the first line after it do not both fit,
       the heading goes to the next page. Nothing else is kept together: no
       widow or orphan rules.
    4. A space at the top of a page is dropped, as a paragraph gap is.
  - Tagging: nothing new. The paragraphs are tagged as in TextFrame (P, or
    the heading type set), the images as Figures with their alt text, in
    the order added, which is the reading order. PAC on the example.
  - Not in it, on purpose: tables (they break across pages themselves, and
    may join later as an item), columns, text wrapped around images,
    floats, keep-together of anything but headings, footers (the pages are
    returned for them, as TextFrame's and Table's are).
  - **Go done, 26 October 2026** (src/textflow.go, 285 lines with its
    comments; 9 tests in textflow_test.go). As built, following Table's
    names: `NewTextFlow(pdf, pageSize)`, margins 72 by default,
    `SetMargins(m)` and `SetEachMargin(top, right, bottom, left)`,
    `SetFirstPageTop(y)`, `Add(paragraph)`, `AddParagraphs(paragraphs,
    gap)`, `AddImage(image)`, `AddImageAligned(image, alignment)`,
    `AddSpace(points)`, `DrawOnPages(&pages)` and `DrawOnPagesFrom(first,
    &pages)`, which return the bottom right corner of the last thing drawn,
    as Table's do; Java `drawOn(pages)` and `drawOn(first, pages)`. Each
    Add is a frame of its own, one under the other with no gap: the space
    between them is AddSpace's, so that a heading sits where the code puts
    it. Margins that leave no room fail the PDF, and nothing added makes no
    page. A trial report (4 sections, 120 paragraphs, 4 photos, PDF/UA,
    footers) is 18 pages, about 20 lines of code, and veraPDF passes it.
    **Java done, 26 October 2026** (com/pdfjet/TextFlow.java,
    TextFlowTest.java, the same 9 tests): `setMargins` for one and for four
    margins, `add` for a paragraph, paragraphs, an image and an aligned
    image, `drawOn(pages)` and `drawOn(first, pages)`; margins that leave no
    room throw, as Java's misuses do. The trial report drawn in Java is the
    same as Go's, to the pixel, on its 18 pages. **C# done, 26 October
    2026** (net/pdfjet/TextFlow.cs, tests/dotnet/TextFlowTest.cs), as
    Java's, with Go's names in C#'s case (`SetMargins`, `Add`, `AddSpace`,
    `DrawOn`); its margins that leave no room throw ArgumentException. Its
    trial report is the same as Go's to the pixel. **Swift done, 26
    October 2026** (Sources/PDFjet/TextFlow.swift, TextFlowTests.swift):
    `add(image, alignment)` with the alignment LEFT by default; margins that
    leave no room fail the PDF, as Go's. The trial report drawn in Swift is
    the same as Go's to the pixel too, so the four ports agree.
    **A fifth rule, 26 October 2026 (the owner):** a paragraph of
    StructElem.CAPTION added right after an image, after a space or not, is
    its caption and goes to the next page with it, as the first draft of
    the example showed a caption left at the top of a page apart from its
    map; a test in each port. **The example and the snippet, the same day:**
    Example_22, which drew a table of text columns (the owner chose it as
    the weakest), is now "Four Countries and Their Regions", the four maps
    of images/ with their captions on 4 pages, PDF/UA, the same in the four
    ports to the pixel; the booklet has the text-flow snippet, after the
    text frames.
  - Size: about 150 to 250 lines in each port with its comments, Go first,
    then Java, C#, Swift; a test in each of the rules above, the same in
    the four ports; an example (a report of 4 sections, 120 paragraphs and
    4 photos) and a snippet in the booklet. check-api.sh shows the
    class as the only addition. No change to TextFrame, Image or the fonts.
- **Text wrapped around images, in TextFrame** (the owner, 26 October
  2026; no release yet). PageMaker's threaded text blocks were the idea of
  TextFrame, and it does that part; what it lacks most is text flowing
  around a picture. Not small, so a design first:
  - The frame is told of the areas to keep clear on its page,
    `addExclusion(x, y, w, h)` (an image's bounds, or any rectangle), with
    a gap around them. Each row of text then takes the widest run of the
    frame's width left free at its height: left of the image, right of it,
    or, when both sides are free, the wider side: text on both sides of
    one image, read across it, is hard to read.
  - A row too narrow for a word (say less than 3 em) is skipped and the
    text goes on below the image. Justified rows justify within their run.
  - Rectangles only, no wrap around the outline of a shape.
  - TextFlow (9.1.5) gets it later as an image item that text wraps around,
    left or right.
  - What it touches: the row building of TextFrame (the start and width of
    each row), in the four ports; the tagging is unchanged (the image a
    Figure, the text its paragraphs, in reading order: the text first, or
    as the code draws them). Tests of the rows' starts and widths beside an
    image, the same in the four ports; an example, a page of an article
    with a photo at the right.
  - Core or Pro is the owner's call when it is scheduled: big features go
    to Pro, but it is part of TextFrame, which is in the core.
- **Done for 9.1.5: Markdown, emphasis in a table cell** (found 26 October 2026,
  making the benchmark report with md2pdf; the owner: "add this to the
  TODO"). A cell such as `**2.1**` is drawn as plain 2.1: the four ports'
  Markdown draw a cell with one font and take its text without its inline
  markup (Java `Markdown.plainText`, used by `drawTable`), so a table cannot
  mark its best value, as a benchmark or a price list does. The fix that
  fits a cell of one font: a cell whose whole text is in one emphasis
  (`**2.1**`, `*note*`, `` `code` ``) is drawn in that font; a cell of mixed
  emphasis stays as now. Four ports, a test each, md2pdf rebuilt. Column
  alignment works already: `|---:|` right-aligns a column. Done the same
  day (the owner: "fix it for 9.1.5"), cellFont in the four ports.
- ✅ Done in PDFjet Pro for 9.1.5, 26 October 2026 (the owner: "do #1 in
  Pro"): `AssociatedFiles.readFrom` in Pro's invoice package, four ports,
  tests and a Go fuzz target; the MIT core is unchanged.
  Was: **For 9.1.5: the associated files of a PDF read back** (the owner, 26
  October 2026). PDFjet writes a file carried inside a PDF, an
  `EmbeddedFile` with its media type and relationship added with
  `pdf.addAssociatedFile`, as the XML of a Factur-X invoice is, but cannot
  read one back. With it an application can keep its own data, a JSON of the
  invoice or the report's figures, inside the PDF it makes, and read it again
  to make the next version, with no text to extract: the data travels with
  the document, as the design of a Forms form does. Other tools read such a
  file as they read any attachment.
  - The API, Java (the others alike, Go `GetAssociatedFiles`): a call on the
    objects of `PDF.read`, for example `PDF.getAssociatedFiles(objects)`,
    returning each file's name, media type, relationship, description and
    bytes, decoded and decrypted as the reader does now, within its budget.
    The files of the document's `/AF` and of the `/EmbeddedFiles` name tree,
    each once.
  - Tests in the four ports: a file written with `addAssociatedFile` read
    back byte for byte, compressed and not, in an encrypted document, in
    PDF/A-3; a PDF with none gives an empty list; a name tree of kids. A
    fuzz case in Go, as the reader has.
  - About a day for the four ports. Not text extraction, which is not built
    (the owner, 26 October 2026).
- **For 9.1.5: better charts, and one more** (the owner, 25 October 2026:
  "significantly improve all charts"). Where it stands, 26 October 2026:
  Chart, which items 1 to 5 were written about, is removed, and the
  simple charts (7 to 9) take its place; BarChart has the simple charts'
  look, grouping on, one axis line at zero and the totals of its stacks
  (6a295afd), which is item 1 and part of 4 for it; DonutChart has its
  title, subtitle and look (d99c0944), and the review of the charts of 26
  October is done (item 11). DonutChart's legend and its text in the hole
  (3, 4), the line chart's values at the points and its areas (4), the
  descriptions of item 5 and the bars with a line of item 6 are done too,
  26 October 2026, in the four ports (CHANGELOG.md). Left: nothing of this
  plan but what it leaves out, below. From a review of Example_25 to 28 and
  chart.go, barchart.go and donutchart.go; in the four ports, the examples
  updated to show it, before and after screenshots for each visible change.
  In this order:
  1. The default look. The grid lines are black by default and fight with
     the data (Example_25, 26, the vertical bars of 27): light gray,
     horizontal only, no vertical grid or inner border, as Example_27's
     horizontal bars set by hand. The default series colors distinct, in
     grayscale and print too, and for color-blind readers. The value labels
     of the bars white or black by the bar's color: white on the horizontal
     bars, black on dark green in the stacked ones now.
  2. Numbers. Chart has no SetGroupingUsed (Example_25's axis says 25000),
     BarChart has. The decimal point and the comma are fixed: the decimal
     and grouping characters settable ("1.234,5" for the German, French and
     Swiss customers of ZUGFeRD and the QR-bill), and a prefix or suffix
     ("$", "%", " km").
  3. One API. DonutChart has no title, subtitle or legend (Example_28 draws
     its title as page text); Chart sets its horizontal and vertical grids
     apart, BarChart has one. The same names and behavior in the three for
     title, subtitle, legend, alternate description, fonts, grid, borders and
     number format.
  4. What is expected and missing. Donut: a label in the hole, as the total,
     and a legend where the leader lines of small slices would crowd. Bar:
     the total above a stacked bar, category names on two lines or tilted
     when too long for their slot, a zero line for negative values. Line:
     text categories on the X axis (Jan, Feb, Q1), the values at the points
     if asked, and the area under a line filled, which makes area charts.
  5. Accessibility. Line and bar charts describe themselves, as the donut
     does with its totals: the series and their values by category, so that
     a PDF/UA document needs no description written for them.
  6. The new chart: bars with a line over them (a target, the average, last
     year), the most common chart of business reports, from the two that
     exist; one value axis, no second one. The runner-up, a horizontal
     progress or gauge bar for a KPI, if wanted later.
  7. **Done for 9.1.5, 26 October 2026, in the four ports, with the new
     Example_20 and a snippet. SimpleBarChart, a new class** (the owner, 26 October 2026: "what
     you drew by hand is perfect", "a much better way to present data -
     very simple bar charts and a table below with the data"; 9.1.5, the
     name SimpleBarChart, the navy and gray defaults). The chart of the
     fair test's results page as a class, in the four ports, in place of
     five more settings on BarChart, which has some 30 and needed most of
     them turned off for it (tried the same day: 17 lines, and no way to
     leave out the value axis, give the axis its own decimals, set one
     label's font or color, put the title at the left, or size the labels
     but by the font; the axis numbers running into each other was a bug,
     fixed for 9.1.5).
     ```java
     SimpleBarChart chart = new SimpleBarChart(regular, bold)
             .setTitle("Statement, 12 pages")
             .setSubtitle("warm ms")
             .add("PDFjet", 7.3f)
             .add("iText", 72.6f)
             .add("OpenPDF", 18.4f)
             .add("PDFBox", 44.1f)
             .setHighlight(0)          // navy bar, bold name and value
             .setFractionDigits(1);
     chart.setLocation(50f, 80f).setWidth(330f);   // the height from the bars
     chart.drawOn(page);
     ```
     - Horizontal bars alone: the names at the left, the values at the
       bars' ends; no axis, grid or legend. Each bar as long as its share
       of the largest value.
     - The look is the default: a light panel (0xF1F3F6), the title
       semibold and the subtitle small and gray at the left, gray bars
       (0xA7AFB8), the highlighted bar navy (0x1F3A5F) with its name and
       value bold; two or three colors settable, not needed.
     - The highlight is the code's to choose, setHighlight(index): the
       best is the lowest value or the highest, which the chart cannot
       know. None by default.
     - PDF/UA: one figure, described from its data ("Statement, 12 pages,
       warm ms: PDFjet 7.3, iText 72.6, ...") unless an alternate
       description is set.
     - Meant to stand beside a Table of the same numbers, the chart for
       the picture, the table for the record and for a screen reader; the
       example shows the two together (a results page).
     - BarChart stays for grouped, stacked and vertical bars with axes.
     - About 150 lines in each port, Go first; tests of the lengths, the
       highlight and the description, the same in the four ports; an
       example, a snippet, before and after screenshots for the owner.
  8. **SimpleScatterChart, a new class, for 9.1.5** (the owner, 26
     October 2026: the scatter plot "I developed originally for a client
     ... could be very useful if cleaned up and improved a bit"; a class of
     its own, as SimpleBarChart, not a change to Chart, which stays for full
     control and keeps the line charts of Example_25 as they are). What
     Example_26 shows today: a black grid, vertical and horizontal, and a
     black border over the data; the highlighted countries tiny colored
     markers among 200 gray ones, found by a table under the chart; the
     points near zero one gray blob; a point at the edge cut off; a
     centered title, and a legend for the trend line alone.
     ```java
     SimpleScatterChart chart = new SimpleScatterChart(bold, regular)
             .setTitle("Cell phones and internet use")
             .setSubtitle("by country, 2007")
             .setXAxisTitle("Cell phones per person")
             .setYAxisTitle("Internet users, % of the population");
     for (Country c : countries) {
         chart.add(c.phones, c.internet);           // a quiet gray dot
     }
     chart.add("United States", 0.83f, 72.5f);      // a navy dot, labeled
     chart.setTrendLine(true);
     chart.setLocation(50f, 80f).setSize(512f, 320f);
     chart.drawOn(page);
     ```
     - The many points as round dots, light gray and a little
       transparent, so that a cluster shows darker, not as a blob.
     - A named point is highlighted: navy, a little larger, its name beside
       it, placed so as not to leave the plot; no table to look it up in.
       A URI for a point, as Example_26's links, if wanted: addLink.
     - A light horizontal grid alone, no border; the axis numbers spaced by
       their widths (as BarChart's since 9.1.5), with grouping and the
       decimals they need; the title and subtitle at the left, as
       SimpleBarChart's.
     - The trend line, least squares, thin and dark gray, labeled "trend" at
       its end, not in a legend.
     - The axes from the data, rounded, with a pad so that no point is cut
       at an edge; set by hand if wanted.
     - PDF/UA: one figure, described from its data: the number of points,
       the highlighted ones with their values, and the trend.
     - The navy and gray of SimpleBarChart, so the two sit together.
     - Go first, then Java, C#, Swift; the same tests and the same content
       stream in the four ports; Example_26 a page of the scatter chart and
       a table of the highlighted countries, as Example_20 is of its bars;
       a snippet; before and after screenshots for the owner.
     - **Done in the four ports, 26 October 2026, links on points
       included (84e73b5f).** Was: **Go and Java done** (simplescatterchart.go,
       SimpleScatterChart.java, 6 tests each, the same content stream). It
       draws with the page alone and ChartScale, the helpers the charts
       share, moved out of Chart and BarChart the same day, so that Chart
       can be retired (the owner). Links on points are left out of it: a
       link inside the chart's figure needs tagging of its own. The data of
       Example_26 to be the World Bank's life expectancy and children per
       woman of 2023, 217 countries and economies (World Development
       Indicators, CC BY 4.0), in place of the 2007 file. C# and Swift
       next.
  9. **SimpleLineChart, a new class, for 9.1.5** (the owner, 26 October
     2026), so that Chart can be deprecated: lines over time, the one
     thing Chart draws that no Simple chart does (Example_25, the electric
     vehicles by model year). In the family of SimpleBarChart and
     SimpleScatterChart: the categories or the years left to right, a few
     lines, each labeled at its end with its name and last value (no
     legend), one line highlighted in navy and the others gray, markers at
     the points if asked, a light horizontal grid, the axis numbers spaced
     by their widths, the title and subtitle at the left; drawn with the
     page and ChartScale alone; one figure in PDF/UA, described from its
     data. Example_25 drawn with it.
  10. **Chart and Series removed in 9.1.5, 26 October 2026, in the four
     ports** (the owner: "remove all old code ... now is the time"), with
     Slice and addSlice, setValueAxisMinMax, the fraction-digits pair,
     BarChart's style knobs and the setDraw... names (CHANGELOG.md,
     Removed). The plan as it was, for the record: **Chart and Series deprecated in 9.1.5, removed in a later release
     (v9.2.1 or after)** (the owner, 26 October 2026: "I do plan to retire
     Chart"). **Deprecated 26 October 2026**, in the four ports, as below:
     Chart's tests and the two review tests that use it allow the warning
     alone; DrawableTest moved to SimpleLineChart, ColorTransparentTest
     keeps Chart and Series until they are removed; the booklet's line chart
     is a SimpleLineChart. Before the release: links on SimpleScatterChart's
     points, which Chart has (the owner, 26 October 2026: the links "we have
     to fix", for PDF/UA). Nothing in the library uses Chart but Series, which only
     feeds it; BarChart, DonutChart and the Simple charts no longer depend
     on it (ChartScale). Point stays: Cell, Path, Shape and Page use it.
     - Deprecated in each port's way: Java `@Deprecated` and a javadoc
       `@deprecated` naming SimpleLineChart and SimpleScatterChart; C#
       `[Obsolete("...")]`; Go `// Deprecated: ...`; Swift
       `@available(*, deprecated, message: "...")`. They keep working.
     - The builds treat warnings as errors, so before the markers: Example_25
       on SimpleLineChart, Example_26 on SimpleScatterChart, the booklet's
       line-chart snippet rewritten; Chart's own tests kept, the warnings
       allowed there alone (`@SuppressWarnings("deprecation")`, `#pragma
       warning disable CS0618`, the Swift test functions marked deprecated
       themselves); DrawableTest and ColorTransparentTest on another
       drawable.
     - The CHANGELOG says what replaces them; the README's and the
       booklet's "line and XY charts" point to the Simple charts.
     - Removed later, recorded in util/api-removed.txt as CalendarMonth was,
       so that check-api.sh takes it.
  11. **The smaller points of the charts review of 26 October 2026: done
     26 October 2026, in the four ports** (CHANGELOG.md), as below; the
     list kept for what was decided. Was: for 9.1.5 if there is time, or
     9.1.7. Its bugs are fixed (CHANGELOG.md:
     the label colors by contrast and a lighter sage, the room over a
     donut's title, values outside a set range, a BarChart without room,
     the month names, null in Java and C#). Done, in the four ports
     (21148e316):
     - Axis numbers on a set range: `setYAxisRange(3, 17)` writes 3.00,
       5.33, 7.67…, the number of lines from the rounded range and the step
       from the range as set (ChartScale `axisOf`). Steps of the range set,
       or lines on round numbers inside it.
     - DonutChart, small slices: the labels of several thin slices pile on
       one another (no moving apart, as SimpleLineChart's end labels have);
       the percentages, each rounded, can add up to more than 100 in the
       description (90 and six of 1.5: 91 and six of 2, 103); a very thin
       slice is all but covered by its two white lines.
     - DonutChart drops a slice whose value times 360 is past the largest
       float (about 9.4e35), though the value is valid.
     - DonutChart's description has the title but not the subtitle; the
       Simple charts have both.
     - DonutChart's title is in f2, the percentages' font; with no f2 (no
       percentages) the title falls back to f1, regular.
     - DonutChart's drawOn returns the circle's corner; the labels under
       it reach about 15 points lower, so what is placed by the returned y
       can touch them. The box of the labels is known and could be returned.
     - BarChart's description, unless set, is its title alone ("Months"),
       no data; the other charts describe theirs.
     - BarChart's legend: a series with a color per bar shows the palette
       color of its index, not its bars'; the legend does not wrap.
     - BarChart leaves two and a half lines for a title it does not have.
     - SimpleBarChart narrower than its names: the bars go to nothing and
       the values are written past the panel.
     - SimpleLineChart and SimpleScatterChart without room for the plot
       draw nothing, not even a figure, and say nothing (BarChart now too).
     - SimpleLineChart's end labels, moved apart, are pushed down without
       limit, below the plot and the panel.
     - SimpleScatterChart: a named point's label has no halo over the gray
       dots (Example_26's Japan and Canada, readable).
  Left out: 3D, logarithmic axes, radar charts, two value axes.
- **The code reviews of 11 and 12 October 2026, three rounds.** Every
  finding is fixed for 9.0.7, in the four ports, each with a test
  (CHANGELOG.md), but those below and object streams (above). Their CI gaps
  are closed: the tests run in Turkish, Arabic and Swedish locales, net48 is
  built, check-api.sh runs against the last tag and fails when a lister
  fails or lists nothing, FuzzPDFRead reads with a budget of 1 MiB that its
  inputs reach, the fuzz targets are timed against the input's length and
  run a slow input again before they fail, the Go tests run under the race
  detector too, and tests/crossport compares random shapes across the four
  ports in CI. The tests of a time, in the four ports, run a slow piece of
  work again before they fail. The net48 build takes the categories of
  the characters from a table of .NET 8's, Unicode 15.0, which a test holds
  to .NET 8's own (K3).
- **For 9.0.9: a cell's drawable put back exactly** (the final review of
  12 October 2026): a table puts the image or barcode of a cell back where
  the caller had it, its location found as its corner less its size, in
  float arithmetic, so 7.13, 8.29 comes back as 7.130005, 8.290001, in every
  port alike; the measuring costs two more drawOn(null) calls a draw. A
  getter of the location, or the cell drawing at an offset without moving
  the drawable, would put it back exactly.
- **Dropped: Swift's Flate to compress as zlib does** (the owner, 25
  October 2026: the 9% larger size is OK, not worth complicating things).
  Swift's PDFs stay about 9% larger than Go's, from its own Deflate encoder.
- **For 9.1.5: Swift a bit faster, simply** (22 October 2026; the owner, 25
  October 2026: "if we can make Swift a bit faster - that's good goal",
  without complicating things). Only contained changes where a profile
  shows the time, each measured against the last tag; no new compressor,
  no deep restructuring.
  Profiled 25 October 2026 (perf, Example_24): compression 15%, the kernel
  14% (168,602 page faults: FlateEncode's 400 KB of tables made for every
  page; reused since, 2,614 to 2,284 ms, in 9.1.5), reference counting 14%,
  exclusivity checks 10%, malloc and free 9%, reading the CSV 10%. The
  reference counting and the exclusivity checks are spread over the drawing
  code: their cure, a restructuring of how the drawing code holds its
  objects, goes to v9.2.1 with the large documents (the owner, 25 October
  2026). The CSV reader, 10%, split in place: 121 to 110 ms for the 30 MB
  file, 1% of Example_24, not kept (more code for too little).
  Example_24's 500 pages take 113 ms in Swift, against 56 to 64 in Go, C#
  and Java (9.1.1). Where the rest goes is not measured: Swift's own
  Deflate, CRC32 and AES (Go's are its standard library's, tuned for years,
  with assembly for AES and CRC32), the counting of references of many small
  objects, String work done where bytes would do, or arrays copied on
  change and bounds checks in tight loops. A profile of Example_24 first
  (perf on Linux, Instruments on a Mac), then the changes where the time
  is, each measured against the last tag.
- **For v9.2.1, about two years out: large documents** (the owner, 25
  October 2026: the size of tagged documents and documents at any size
  "are kind of related - we should do them together"). Two parts:
  1. Smaller tagged documents. Each cell of a table of PDF/UA costs 280 to
     415 bytes, its text and a structure element of its own, each an object
     written uncompressed: a price list of 3,000 rows of 6 columns is
     4.7 MB. First the structure elements written in fewer bytes, measured,
     with the xref table as it is. Object streams would shrink it a third or
     more, but are not planned (the owner, 25 October 2026: keep the xref
     table, which can be read when debugging, and do not complicate
     things): a classic xref table cannot point into one, only a
     cross-reference stream or a hybrid file can, and PDF/A-1 forbids them.
  2. PDF/A and tagged documents at any size (third review, W1, W2, W3, W5,
     W6). 9.0.7 refuses a PDF/A-1 document past its limits; this writes
     one within them at any size: a balanced page tree, a parent tree of
     /Kids, and resources for each page rather than one dictionary of every
     image and font, which also keeps a page taken out of the document from
     carrying all the images. Then: a tagged document keeps less of each
     page until complete (Java boxes the numbers of the parent tree), the
     pages keep less once written, and a bookmark tree of thousands of
     levels or siblings is built in linear time. Java's Compressor (W7) is
     a Deflater for each thread since 9.1.5.
- **For v9.2.1, after the Korean proofreader: Korean line breaking by
  design, not by omission** (found 27 October 2026, for Forms' Korean).
  isCJK, which chooses how TextBlock breaks a line, counts the Hangul jamo
  (U+1100 to U+11FF) but not the 11,172 syllables (U+AC00 to U+D7A3) that
  Korean is typed in, in the four ports. So a line of Korean breaks at its
  spaces, as English does: keep-all, which Korean readers mostly prefer for
  documents, but by chance; a line of loose jamo, which Forms joins first,
  would break between letters instead. Its upper bound, U+9FD5, also leaves
  out the newer CJK characters and the extensions (𨋢, U+282E2), and a line
  is CJK only when more than half of it is, so a long run of Chinese in a
  line mostly English is cut where the line is full, without the kinsoku
  rules. Once the proofreader has said whether keep-all is right: Korean
  named on purpose, breaking at spaces, with a test of each case, the ranges
  of Chinese brought up to date, and the PDF the same for every text it
  draws well today. Not before 9.1.5, whose scope is closed.
- **For v9.2.1: PDF/UA-2** (ISO 14289-2:2024; the owner, 26 October 2026),
  with the large tagged documents, as both touch the structure tree. A new
  level, `Compliance.PDF_UA_2`, that writes `%PDF-2.0`; PDF/UA-1 stays as it
  is. veraPDF's ua2 check of the 50 examples on 26 October 2026: none
  passes, and the 44 tagged ones fail six rules alone:
  1. The Document element in the PDF 2.0 namespace (44 examples).
  2. The XMP metadata `pdfuaid:part` 2 and `pdfuaid:rev` 2024 (43).
  3. Every destination within the document a structure destination, the
     element a link, a bookmark or a go-to lands on, beside its page (41).
  4. A ListNumbering attribute on a list whose items have labels (4).
  5. A Caption the first or last child of its Figure: TextFlow's caption of
     an image goes into the image's figure (1).
  6. An AFRelationship in the file specification of a file attachment (1).
  The other failures are of the six examples that are not tagged. Then the
  structure types PDF 2.0 renamed, such as Note, mapped, as testing shows.
  The four ports alike, and veraPDF checking UA-1 and UA-2 in CI. About a
  week.
- **For v9.2.1: hyphenation by dictionary** (the owner, 26 October 2026,
  from the fair test's list of what the other libraries have). Today a
  word breaks at a line's end only at a soft hyphen the text carries.
  Hyphenation patterns, of the TeX kind, for English first, then the other
  languages of IBM Plex Sans, chosen by the text line's language; used by
  TextBlock, TextColumn, TextFrame and TextFlow when asked, off by
  default so that no document changes. The patterns are data, kept out of
  the library jar the way the fonts are, or in a small file of their own:
  to decide. The four ports alike, tested by the words they break.
- **For v9.2.1: two things learned from common business documents** (an
  invoice, a statement, a report of text and photos, a batch of 1,000
  letters, written in the four ports; the owner, 26 October 2026):
  1. ✅ The first step done for 9.1.5, 26 October 2026 (the owner: "bring
     it forward to 9.1.5"): `cmap` and `name` left out of the subsets in the
     four ports. A profile of a one-page letter showed three quarters of
     it in zlib compressing the two subsets (33 KB, where OpenPDF's are
     22); 0.82 ms to 0.58, 22.2 KB to 15.5. Renumbering the glyphs is still
     for v9.2.1, if it is worth it.
     Was: Smaller font subsets. A subset keeps every glyph number of the font,
     1,019 slots in IBM Plex Sans, empty but for the ones used, with the
     font's full `hmtx`, `loca`, `cmap` and `name` tables: 23 KB and 18 KB
     uncompressed for the two fonts of a one-page invoice of 84 KB. A
     CIDFontType2 with an Identity CIDToGIDMap needs neither `cmap` nor
     `name`; drop them first, then perhaps renumber the glyphs used. An
     exception to the frozen font code (the owner, 26 October 2026): the
     same care as the kerning, the fuzzers run again, every viewer and
     veraPDF checked.
  2. An image read once and used by many documents. Measured 26 October
     2026 (Java, the letter of 1,000: a 386 by 395 RGBA PNG logo, two IBM
     Plex Sans fonts, a heading and three paragraphs): 8.78 ms a letter,
     1.00 without the logo, 0.78 with neither the logo nor the
     paragraphs; reading the two fonts is 0.11 ms. So the logo is nearly
     nine tenths of a letter: a PNG with alpha is decoded, its pixels and
     its alpha split, and both deflated again, about 600 KB, for every
     document. Sharing the fonts would save about 1%, and is not worth
     touching the font code for. An image whose decoded and deflated data
     a second document can take as it is, by a copy of the Image for
     another PDF or the like, would make such a batch several times
     faster; to design, measured in the four ports first.
- **Dropped: local letter shapes from GSUB** (the owner, 25 October 2026:
  "keep fonts frozen"). No port reads a font's GSUB table: Bulgarian text
  is drawn with the font's default Cyrillic letters, т as a small capital
  T, not the Bulgarian т that looks like an m, and ligatures and small
  capitals are not made. After kerning and the longer fuzzing of the
  subsetters, the font code is to be left as it is.
- **For 9.1.5: kerning of TrueType and OpenType fonts, opt in** (18 October
  2026; first, ahead of any other library work, the owner, 22 October 2026;
  9.1.5, the owner, 24 October 2026): no slower when off, and little code.
  Go done 24 October 2026 (kerning.go: the kern feature's PairPos formats 1
  and 2, and the kern table; kerned where each glyph is written, the same
  rule measured; Example_24 930 ms against 930 at v9.1.1); then Java, C#,
  Swift, each with the same widths as kerning_test.go.
  `Font.SetKernPairs(true)` kerns the 14 standard fonts today; the same call
  for a .ttf or .otf font, off by default, so that a document that does not
  ask for it is as fast as now. From the GPOS table, read already for the
  marks (`gpos.go`, lazily, within its work limit): its PairPos lookups,
  format 1 (glyph pairs) and format 2 (class pairs), of the `kern` feature;
  the `kern` table of an older TrueType font without GPOS. The pairs read
  the first time a kerned font measures or draws, into one map of glyph
  pairs. Every width counts them, as the standard fonts' do: StringWidth,
  the wrapping, justification and alignment of TextLine, TextBlock and the
  table cells; the text written as `TJ` with the adjustments. Four ports,
  one test of the widths and the drawn text agreeing in all four, and
  Example_24 timed with it off (no slower) and on.
  Code review of 24 October 2026, fixed in the four ports 25 October:
  the tables are read when the kerning is turned on, under a lock (Java,
  C#, Swift) or a sync.Once (Go), so that threads measuring with one font
  never see them half read, as Java, C# and Swift could, where a flag was
  set before the tables were read; the marks of the GPOS table are read
  only where text is drawn, which changes the font in any case;
  GetFitChars was a character short before a pair kerned wider; only the
  kern features of each script's default language; a class of a format 2
  subtable it has no values for goes on to the next subtable; Swift stops at
  the end of the kern table and sizes sharedGlyph by the glyphs of the font.
  Fixed too, 25 October: Java keeps the pairs and the classes in a map of
  ints of its own, IntMap, where a HashMap boxed every key and value; which
  glyph a character is drawn with, if any, is decided in one place in each
  port, drawnGlyph, which Page draws with and the kerning measures with
  (Swift's 500 pages of text 98 ms, as before). Kerned text was slow, the
  pairs looked up in a map for each subtable: 500 pages kerned took 226 ms
  in Go (57 not kerned), 142 in Java (69), 172 in C# (63), 341 in Swift
  (98). The subtables are arrays by glyph now, and the glyphs kerned and
  the pairs of printable ASCII are made once: 96, 118, 103 and 155 ms.
  Left as they are, for later:
  - Whether a glyph is kerned is decided by the character unicodeOf gives
    it, so a glyph a font uses both for a mark and for a spacing character,
    which is rare, is not kerned.
  Removed, 25 October 2026 (the owner): the reading of the kern table of
  an older TrueType font without GPOS, in the four ports. None of the 261
  fonts PDFjet ships has a kern table, and none of the 372 fonts of this
  computer has one without GPOS; the code had no font to test it with, and
  none is to be added. Such a font draws its text unkerned, as in 9.1.1.
  Bitstream Vera Sans of 2003, among the fonts fetched for the reference
  tests, is one; DejaVu Sans, which replaced it, kerns with GPOS.
- ✅ **Done, 25 October 2026: the font subsetters fuzzed 8 hours each**, and
  FuzzSubsetTrueType and FuzzOpenTypeFont 5 minutes each again on 26 October,
  after the subsets left out cmap and name, with no failure. Was: **Longer
  fuzzing of the font subsetters.** Go's fuzzers ran 5 minutes each
  on every target before 9.0.5 with no failure; the TrueType and CFF
  subsetters, which subset a real font for each input, ran 27,000 and 15,000
  inputs. A run of a night on those two.

## v9.1.1: the code review of 17 October 2026, all to fix before the release

Seven reviews, read-only: the changes since v9.0.9, Java's parsers, the C#,
Go and Swift ports, Pro, and the release. The owner: "We must fix all the
issues found before we release 9.1.1." Each fix in every port it applies to,
with a test. Pro's are in pdfjet-pro's TODO.

Fixed:
- [x] Swift, from the Swift Package Index's builds of v9.0.9 (18 October
      2026): on Android, Chart's grid rounding called ceil and floor on
      Floats, which Android's C library has for Doubles alone (now the
      Float's own `rounded(.up)` and `rounded(.down)`); on tvOS, watchOS and
      WebAssembly, the corpus harness's signals (`sigaltstack`, Darwin) did
      not build, and the index builds every target: the harness is for
      macOS and Linux alone. The library built on Swift 6.2 to 6.4, macOS,
      iOS, visionOS and Linux. Swift 6.1 again the oldest the package asks
      for, as before March 2026: the QR code's version polynomial written
      as one number, which 6.1 type-checks, and CI's oldest-Swift job on
      6.1.3. Android, built here with Swift 6.4.1's Android SDK and NDK r30
      as the index does: also round and sqrt on Floats in the fonts' widths
      and the donut's arcs, and ceil and asin in Page (now `rounded()`,
      `squareRoot()`, `rounded(.up)` and asin of a Double); the same values.
- [x] C#: the wrong password and the unreadable font were still a plain
      Exception (dea57791).

The 9.1.1 changes:
- [x] Table: a row drawn whole, refused on the first page when a
      first-page top margin leaves it no room, though the next page has
      it: refuse only when no page would have room (four ports).
- [x] CHANGELOG: the Swift and C# overload makes a bare method reference
      to complete ambiguous; the EULA of the packages; the boolean's
      opposite meanings (Java and Swift close, C# leaves open).
- [x] The creation date of years below 1000, not padded to four digits as
      Go's is (Java, C#, Swift).
- [x] Tests: complete(false) and Complete(true) on the error path, the
      stream left open.

Java's parsers (in the other ports where they share the code):
- [x] OTF name(): no limit on the text of the name records: a 593 KB
      font is an OutOfMemoryError, a 125 KB one 65 M characters in the XMP
      (Java, Go; check C# and Swift).
- [x] SVG StyleSheet.forClasses quadratic in the classes of an element
      (Java, Go; check C#, Swift).
- [x] SVG rules times elements, repeated rules not merged.
- [x] Decryptor isSignatureDict quadratic per dictionary (four ports):
      decide once per dictionary.
- [x] Image(PDF, PDFobj): /Filter and /BitsPerComponent written empty;
      an image object without a stream half written, complete() then
      succeeding with a corrupt PDF (four ports).
- [x] A font with neither glyf nor CFF (as CFF2) accepted and embedded as
      TrueType: refuse it.

The C# port against Java:
- [x] TextBlock soft hyphen after a Unicode space (Util.Trim against
      Java's trimSpace); Swift too.
- [x] Bookmark titles: Unicode \s in place of ASCII whitespace; Go's
      leaves out \v.
- [x] net48: NumberFormat F rounding half away from zero (chart labels).
- [x] Highlight keywords lower-cased per character (final sigma, İ).
- [x] A lone surrogate written as EF BF BD where Java writes 3F (URIs,
      QR, DataMatrix, and the other places): one choice for all ports.
- [x] SVG numbers: hex floats and underscores refused where the others
      take them.
- [x] EmbeddedFile registered before its stream is read (Go too).
- [x] AppendToken best-fits characters above U+00FF where Java writes ?.
- [x] Image ParseNumber: Unicode digits and Trim, "infinity"; "100f".
- [x] SVGState lower-casing of İ.
- [x] CalendarMonth: years past 9999, an open DayOfWeek.
- [x] Where Java is the odd one: Chart fractionDigitsOf saturating at
      2^63 (Go too), Float.parseFloat taking 595f and hex in /MediaBox,
      Page's trim() of a NBSP description.

The Go port:
- [x] qrcode: a negative error-correction level loops for ever; 4 to 7
      give wrong format bits: refuse them.
- [x] table wrapCellText: a slice bound panic on invalid UTF-8.
- [x] GetPageObjects rewrites shared objects on every call, and
      aesPkcs7Pad appends into a decrypted stream: a race when one Read is
      merged on several goroutines.
- [x] structelem.StructElem and relationship.Relationship written raw:
      refuse a value that is not one of theirs, as CapStyle.
- [x] BigTable.SetFirstPage of another PDF accepted, its error recorded
      on the other PDF.
- [x] contentRefs and GetContentObject take an annotation's /Contents for
      the page's (four ports): dictEntryIndex.
- [x] SetCompliance takes any value; NewCJKFont of an unknown font writes
      half an object before it panics; CalendarMonth months out of range.
- [x] Example 25 does not check Complete's error.
- [x] tests/crossport: Java and C# below(8), Go and Swift below(5).
- [x] TextBlock SetWidth(NaN): Go fails, Java draws.

The Swift port:
- [x] BufferedOutputStream: a failed write reads streamError, which traps
      on Linux with no error: a closed stream crashes where Java throws.
- [x] Its deinit flushes and closes the caller's stream when complete()
      was never called; Java leaves it.
- [x] RC4 and AES-128 passwords above U+FFFF: per code point, Java per
      UTF-16 unit (Go as Swift).
- [x] SVGImage and ImageSize hide a stream that cannot be read.

The release (scripts and the checklist):
- [x] The checklist's gh release step: the jar and dll from the Core zips,
      not the developer build.
- [x] The packaging builds Pro from its HEAD, not its tag: check its
      version.sh.
- [x] A check that the four version strings, Example_37 and the tests
      agree, and that CHANGELOG has the date.
- [x] Reproducible zips: javadoc -notimestamp; the Core zips for Maven and
      NuGet from the GitHub release.
- [x] CI runs the booklet's snippets.
- [x] build-*.sh: set -e; build-java.sh leaves old classes of the
      subpackages.
- [x] TODO's check-api line: v9.0.9; windows.yml checks only that a PDF
      is not empty (and build.yml: %%EOF now).
- [ ] Pro's library.sh comment of GONOSUMDB: with the Pro fixes, after the
      reset.

## Known and accepted (document, do not fix)

- Only Arabic and Persian letters are shaped.
- No Indic scripts (Devanagari, Bengali, Tamil...), Khmer, Myanmar, Sinhala or
  Nastaliq Urdu: they need GSUB and a shaping engine's reordering, which PDFjet
  does not have (checked 19 October 2026: हिन्दी drawn with its vowel after its
  consonant and its conjunct unjoined). Said in the README, under Marks, and on
  pdfjet.com's FAQs.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- MuPDF puts spaces inside vowelled Arabic and Hebrew words drawn from `.otf`
  fonts; Poppler extracts them whole.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
- A font draws only the characters up to U+FFFF that are inside the range
  its OS/2 table gives. Any other character counts as missing, like one the
  font lacks. This costs the glyphs above U+FFFF that 144 of the shipped fonts
  have, such as IBM Plex Math's bold italic letters and the CJK Extension B
  ideographs; no shipped font has an OS/2 range narrower than its glyphs.
- Full CommonMark is not planned: PDFjet's Markdown is the practical subset
  that documents use, as its Bidi covers what text needs without the whole of
  UAX #9.
- No GIF and no TIFF images: a GIF or a TIFF is a PNG in one command of
  any free converter, without loss, and a reader of either is code to write,
  fuzz and keep in four ports for what a developer fixes in seconds. PDFjet
  reads PNG, JPEG, BMP and SVG, and Markdown for documents; not HTML.
- No more fonts are bundled. Any `.ttf` or `.otf` file can be loaded, and it
  is embedded as a subset.
