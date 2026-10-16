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
  Build workflow runs the same checks on every push.
- **The public API.** `./check-api.sh v9.0.3` (the last release) lists what
  was added and what was removed in each port: nothing is removed and no
  signature changes in a 9.0.x release, and what is added is what CHANGELOG.md
  lists.
- **The manual viewer pass.** The PDFs in `viewer-files-to-test`, opened by
  hand in Acrobat Reader, and with NVDA where a file is tagged; see the README
  there for the files of each release. One question is open from the pass of
  9.0.3: Example_42's shapes have their opacity in an ExtGState and in the
  annotation's `/CA`, and ISO 32000 applies `/CA` to the appearance, so a
  strict viewer could draw them at 25% where MuPDF, PDFium, Poppler and pdf.js
  draw them at 50%.
- **PAC.** The PDF/UA files in `pdf-ua-files-to-test`, checked with PAC on
  Windows; see the README there for which files go in.
- **Benchmarks.** `benchmarks/run.sh` and the four ports' text and table
  benchmarks in `benchmarks/ports` and `benchmarks/table`, against the last
  release; the results are kept in their `results` folders.

## Open

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

- **v9.1.1: four small API fixes, from a developer evaluating the Java port
  (GitHub Discussion #5, 16 October 2026).** All additive or a check, all
  on Java 8, in the four ports where they apply; the owner, 16 October 2026:
  "add them to the TODO for 9.1.1".
  - **complete() that leaves the stream open.** Java, C# and Swift close
    the output stream in complete(), so several PDFs cannot be written into
    one ZipOutputStream; Go already leaves a writer it is given open. Add
    complete(boolean closeStream) in Java and Swift, keeping today's default,
    and the leaveOpen of .NET's StreamWriter in C#. Until then: a
    FilterOutputStream whose close() does nothing.
  - **Narrower exceptions on PDF's own methods (Java only).** complete() and
    the constructors declare throws Exception; narrow them to IOException
    where that is all they throw, after checking what they really throw.
    Drawable.drawOn() keeps throws Exception: narrowing an interface breaks
    every class that implements it with throws Exception.
  - **The end of a drawn table, by name.** drawOn() returns float[] {x, y};
    add a method that returns a small immutable class with named getters,
    and keep drawOn() as it is. Not named Point, which is PDFjet's drawable
    point.
  - **A vertical alignment that can't be one is refused.**
    setVerticalAlignment(Alignment.RIGHT), of TextBlock and Cell, is taken
    as TOP without a word; throw IllegalArgumentException for anything but
    TOP, CENTER and BOTTOM, and the same in the other ports, each its own
    way. A separate enum for vertical alignment would break existing code.

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

- **For 9.1.5: smaller tagged documents, with object streams.** Each cell of a table of
  PDF/UA costs 280 to 415 bytes, its text and a structure element of its own,
  each an object written uncompressed: a price list of 3,000 rows of 6
  columns is 4.7 MB. PDF 1.5's object streams, the small objects compressed
  together a batch of 100 to 200 at a time so that memory stays flat, and a
  cross-reference stream would shrink them a third or more. An option, on by
  default, off for PDF/A-1, which forbids object streams. Measured first, the
  time and the size of a long PDF/UA table, before and after.
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
- **For 9.1.5: Swift's Flate compresses less** (third review, K6; the
  owner, 12 October 2026): Swift's PDFs are
  about 9% larger than Go's, the same content after inflating, from its own
  Deflate encoder. Its matches could be searched as zlib's level 6 does.
- **For 9.1.5: PDF/A and tagged documents at scale (third review, W1, W2,
  W3, W5, W6, W7).** 9.0.7 refuses a PDF/A-1 document past its limits; 9.1.5
  writes one within them at any size: a balanced page tree, a parent tree of
  /Kids, and resources for each page rather than one dictionary of every
  image and font, which also keeps a page taken out of the document from
  carrying all the images. Then: a tagged document keeps less of each page
  until complete (Java boxes the numbers of the parent tree), the pages
  keep less once written, a bookmark tree of thousands of levels or
  siblings is built in linear time, and Java's Compressor is not one
  synchronized Deflater for every thread.
- **Longer fuzzing of the font subsetters.** Go's fuzzers ran 5 minutes each
  on every target before 9.0.5 with no failure; the TrueType and CFF
  subsetters, which subset a real font for each input, ran 27,000 and 15,000
  inputs. A run of a night on those two.

## Known and accepted (document, do not fix)

- Only Arabic and Persian letters are shaped.
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
