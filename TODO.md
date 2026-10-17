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

## v9.1.1: the code review of 17 October 2026, all to fix before the release

Seven reviews, read-only: the changes since v9.0.9, Java's parsers, the C#,
Go and Swift ports, Pro, and the release. The owner: "We must fix all the
issues found before we release 9.1.1." Each fix in every port it applies to,
with a test. Pro's are in pdfjet-pro's TODO.

Fixed:
- [x] C#: the wrong password and the unreadable font were still a plain
      Exception (dea57791).

The 9.1.1 changes:
- [ ] Table: a row drawn whole, refused on the first page when a
      first-page top margin leaves it no room, though the next page has
      it: refuse only when no page would have room (four ports).
- [ ] CHANGELOG: the Swift and C# overload makes a bare method reference
      to complete ambiguous; the EULA of the packages; the boolean's
      opposite meanings (Java and Swift close, C# leaves open).
- [ ] The creation date of years below 1000, not padded to four digits as
      Go's is (Java, C#, Swift).
- [ ] Tests: complete(false) and Complete(true) on the error path, the
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
- [ ] A font with neither glyf nor CFF (as CFF2) accepted and embedded as
      TrueType: refuse it.

The C# port against Java:
- [ ] TextBlock soft hyphen after a Unicode space (Util.Trim against
      Java's trimSpace); Swift too.
- [ ] Bookmark titles: Unicode \s in place of ASCII whitespace; Go's
      leaves out \v.
- [ ] net48: NumberFormat F rounding half away from zero (chart labels).
- [ ] Highlight keywords lower-cased per character (final sigma, İ).
- [ ] A lone surrogate written as EF BF BD where Java writes 3F (URIs,
      QR, DataMatrix, and the other places): one choice for all ports.
- [ ] SVG numbers: hex floats and underscores refused where the others
      take them.
- [ ] EmbeddedFile registered before its stream is read (Go too).
- [ ] AppendToken best-fits characters above U+00FF where Java writes ?.
- [ ] Image ParseNumber: Unicode digits and Trim, "infinity"; "100f".
- [ ] SVGState lower-casing of İ.
- [ ] CalendarMonth: years past 9999, an open DayOfWeek.
- [ ] Where Java is the odd one: Chart fractionDigitsOf saturating at
      2^63 (Go too), Float.parseFloat taking 595f and hex in /MediaBox,
      Page's trim() of a NBSP description.

The Go port:
- [x] qrcode: a negative error-correction level loops for ever; 4 to 7
      give wrong format bits: refuse them.
- [ ] table wrapCellText: a slice bound panic on invalid UTF-8.
- [ ] GetPageObjects rewrites shared objects on every call, and
      aesPkcs7Pad appends into a decrypted stream: a race when one Read is
      merged on several goroutines.
- [ ] structelem.StructElem and relationship.Relationship written raw:
      refuse a value that is not one of theirs, as CapStyle.
- [ ] BigTable.SetFirstPage of another PDF accepted, its error recorded
      on the other PDF.
- [ ] contentRefs and GetContentObject take an annotation's /Contents for
      the page's (four ports): dictEntryIndex.
- [ ] SetCompliance takes any value; NewCJKFont of an unknown font writes
      half an object before it panics; CalendarMonth months out of range.
- [ ] Example 25 does not check Complete's error.
- [ ] tests/crossport: Java and C# below(8), Go and Swift below(5).
- [ ] TextBlock SetWidth(NaN): Go fails, Java draws.

The Swift port:
- [x] BufferedOutputStream: a failed write reads streamError, which traps
      on Linux with no error: a closed stream crashes where Java throws.
- [x] Its deinit flushes and closes the caller's stream when complete()
      was never called; Java leaves it.
- [ ] RC4 and AES-128 passwords above U+FFFF: per code point, Java per
      UTF-16 unit (Go as Swift).
- [ ] SVGImage and ImageSize hide a stream that cannot be read.

The release (scripts and the checklist):
- [ ] The checklist's gh release step: the jar and dll from the Core zips,
      not the developer build.
- [ ] The packaging builds Pro from its HEAD, not its tag: check its
      version.sh.
- [ ] A check that the four version strings, Example_37 and the tests
      agree, and that CHANGELOG has the date.
- [ ] Reproducible zips: javadoc -notimestamp; the Core zips for Maven and
      NuGet from the GitHub release.
- [ ] CI runs the booklet's snippets.
- [ ] build-*.sh: set -e; build-java.sh leaves old classes of the
      subpackages.
- [ ] TODO's check-api line: v9.0.9; Pro's library.sh comment of
      GONOSUMDB; windows.yml checks only that a PDF is not empty.

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
