# PDFjet — the plan to v9.0.5

**v9.0.3 was released on 8 October 2026.** The plan to it is kept below, as
it was written.

**v9.0.5, aimed at Tuesday 27 October 2026** (the owner, 8 October 2026: "No
pressure if we can't get it on that specific date"). The owner works on
PDFjet Forms and comes back to the library now and then; this list is what
to do, in order, so that each visit can just pick up the next item. Only
what users would hit goes in; the rest waits for v9.1.

**v9.0.4, internal, tagged on 8 October 2026** (the owner: "let's do
internal release and call it v9.0.4 then we go all in with font subsets for
v9.0.5"): items 1 to 4 below and the Annot role map of PDF/A-1. The tag is
local and never pushed, as public releases keep odd numbers; nothing is
packaged and the Producer still says v9.0.3. **v9.0.5 then adds the
TrueType font subsetting** (the plan under v9.1 below, moved up), with
NuGet, net48's feedback and Jazzer's last findings when they come.

**Subsetting, done in the four ports on 8 October 2026:**
`src/subset.go`. The TrueType fonts are written at Complete under the number
reserved at construction, so the name with its tag, the widths and the
ToUnicode map list only the glyphs kept; their tables are those a reader
needs (head, hhea, hmtx, maxp, loca, glyf, cvt, fpgm, prep, gasp, cmap, OS/2,
name, post as version 3), GSUB, GPOS, GDEF and the vertical tables left out:
Noto Sans 344 KB to 25 KB, Noto Sans SC 6.5 MB to 93 KB, the 3,420 pages of
the Go examples drawn the same by Poppler, veraPDF passing 1a, 1b, 2b and UA.
`Font.SetSubset(false)` keeps a font whole. The ToUnicode map is compressed
for every font. Glyphs reach a page through `Page.appendGlyph` and
`Stamp.drawEncodedText`; the program is shared by the fonts of one file
(`shareTrueTypeProgram`). A .ttf.stream subset costs its inflation, about
0.1 s for Noto Sans SC; the .ttf, 0.04 s. Until the other ports follow, the
compare job of the Build tells their files apart (no longer: all four are
done). Java (`Subset.java`,
`Page.appendGlyph`, `PDF.newObj(int)`) embeds the same subsets as Go, byte
for byte uncompressed, under the same tags (Examples 28, 32 and 49). In Java,
warm, a page of English with Noto Sans takes 25.7 ms from the .ttf and 17.8
from the .ttf.stream (the .ttf's tables are parsed), a page of Chinese with
Noto Sans SC 42.5 ms from the .ttf and 97.3 from the .ttf.stream. C#
(`Subset.cs`, `PDF.NewObj(int)`) the same as Go too, and its net48 build
compiles. Swift (`Subset.swift`, `PDF.newObj(_:)`) the same too: the four
ports' Example_28 pass veraPDF's PDF/UA check. Left: Acrobat on Windows (the
owner), the .ttf.stream deprecation once that is done, and the benchmark of
8c in benchmarks/ (jet-noto of TextBench).
**Packages with one time, from 9.0.5 (the owner, 9 October 2026: "We will
start doing it starting with 9.0.5"):** package-java.sh and package-dotnet.sh
give every file and directory of a package, and every entry of PDFjet.jar
(jar --date, for the manifest too), the time of the commit the package is
made from, in UTC, and zip the entries in sorted order without the extra
fields of each system (-X): the reproducible builds of today, after the
software of old that shipped every file at one time. A test build of the Java
package on 9 October gave its 944 entries 06:11:32 UTC, and was 282 MB where
9.0.3's was 307 MB. The example PDFs still hold the time they were made, so
two builds are not yet the same to the byte; a fixed creation date for them
is a maybe.

**The .stream format removed, decided by the owner on 9 October 2026** ("I am
strongly leaning to removing EVERYTHING related to stream fonts right now ...
clients do not use them"): the reader in the four ports, the generator and
Zopfli in util, Go's NewFontStream1 and NewFontStream2 (NewFontForObjects in
its place), the stream tests and fuzz targets; FontWriter and FontObjects
hold the writers every font shares. check-api.sh lists the two Go functions as
gone. PDFjet Forms must move its fonts to .ttf before it takes 9.0.5 (its
server keeps .otf.stream and .ttf.stream copies), and the Jazzer FontStream
target in pdfjet-fuzz-replay goes.

**TrueType only, decided by the owner on 8 October 2026** ("YES we should use
only .ttf fonts in both PDFjet and PDFjet Forms"): the constants point to the
`.ttf` files, IBM Plex as IBM's `.ttf` editions (the Forms editor's `.woff2`
are the same quadratic outlines), every `.stream` file gone from
pdfjet-fonts, IBM Plex Sans kept as `.otf` "just to have something for
testing fonts with CFF outlines". The JP line metrics and the TC widths of
the `.ttf` accepted ("Go with 1, accept both"). Three stream files stay in
`tests/data/stream-fonts` for the tests of the reader. Still to do: PDFjet
Forms on the `.ttf` files (pdfjet-server, its own copies of the fonts, when it
moves to 9.0.5), and a font added to an existing PDF is embedded whole, which
made Examples 37 and 50 larger (124 KB to 172 KB, 170 KB to 264 KB): they
use IBM Plex Sans's .otf instead (the owner, 9 October 2026), whose CFF is
embedded, 127 KB and 177 KB. A subset there needs a completion of the
objects, a maybe for v9.1.

**CFF subsetting: done after all, on 9 October 2026** (the owner: "Source Han
Sans JP <- aparently this font is very popular! We have add subsetting for
OTF fonts with CFF outlines"): `src/cffsubset.go`, the charstrings and the
subroutines not used emptied, every number kept, the CFF written again in a
fixed order with five-byte offsets; the identity charset for a CID-keyed
font fixed its wrong glyphs. Source Han Sans JP Regular is in pdfjet-fonts as
the CID-keyed test font. Done in the four ports the same night, which embed
the same programs byte for byte, subset and whole. Kept below as it was
written:
**CFF subsetting: not needed** (8 October 2026). About 3,000 lines in the
four ports (5,000 with the subroutines pruned) for one gain, IBM Plex as
.otf, which its .ttf edition already gives: Example_01 in Go with IBM Plex
Sans 3.005 as a .ttf subset, 28 KB in 14.2 ms, against 64 KB in 12.8 ms
from the .otf.stream and 67 KB in 16.7 ms from the .otf. Most fonts are
TrueType (Google Fonts, Noto, the Windows and macOS fonts); the .otf.stream
stays the fast path for the fonts embedded whole and for PDFjet Forms.

1. ✅ **Done on 8 October 2026, evening, in the four ports with a test in
   each:** the OTF hmtx before hhea (ae905866; Go and Swift had drawn every
   glyph 0 wide, unseen), the malformed SVG number (5d3d6f73), and Jazzer's
   Markdown fault, whose cause was not the quotes but a table header taller
   than half the page, repeated on every page: drawn on the first page alone
   now (5444ebb6), 553 pages and 2 GB down to 17-24 pages in 0.1 s. Jazzer's
   run goes on until 9 October, 07:00: what else it finds comes here. As
   first written: **The two faults of the fuzz replay**, in the four ports with a test in
   each (item 7 below): an OTF whose hmtx comes before hhea, and a malformed
   SVG number. Then the findings of Jazzer's overnight run of 8-9 October
   (~/Projects/pdfjet-fuzz-replay/jazzer, status.sh), each checked first.
   Found by Jazzer on 8 October 2026, after four hours, checked the same
   day: **Markdown, deeply nested block quotes.** A text of 22 KB, about 40
   levels of `>` and a long run of `*`
   (jazzer/findings/Markdown/crash-3fdf839a...), takes 21 s and 1.7 GB in
   Java uninstrumented, and 9.5 s, 553 pages and 2.3 GB allocated in Go: the
   Markdown code of all the ports, not Java's alone. Likely cause, to
   confirm: each level indents the column, which at about 40 levels is
   narrower than a letter, so the text breaks after nearly every character.
   Probable fix, in the four: a cap on the nesting of quotes past which the
   indent stops growing, or a least width of the column; the input as a
   test, in time and pages. By 18:00 Jazzer had found two more inputs of
   the same shape (crash-295741b7..., crash-adf902fa...: 21 s and 1.7 GB;
   the second runs out of a 2 GB heap): the one fault, which the fix must
   cover for all three. The OTF target's timeout and two slow units run in
   0.5 to 0.6 s uninstrumented, about 170 MB: Jazzer's slowdown, not a
   fault. Markdown's four slow units to be run with Repro too.
2. ✅ **Done on 8 October 2026, evening (9917aa12): one XML parser for SVG in
   the four ports**, the 1,170 fuzzed SVGs accepted or refused alike (560,
   610), the 246 SVG files of the repository drawing the same content. Was:
   **Back in 9.0.5, the same evening** (the owner: "let's do the one XML
   parser for v9.0.5"), the deflate part still dropped (Go is the lenient
   one there, not a fault). Written first as moved out: The SVG part goes
   to v9.1 as **one XML parser for SVG in the four ports**, started from
   PDFjet Pro's `XMLParser` (the e-invoices', in Go, Java, C# and Swift,
   strict and fuzzed), moved into the MIT library ("the AGPL should not be
   an issue": the owner's own code), with one change for SVG: a DOCTYPE,
   which editors write, skipped instead of refused. In the core only
   SVGImage parses XML (Go's encoding/xml, Java's StAX, C#'s XmlReader,
   Swift's own tokenizer), so the four will then read SVG alike by
   construction, strict, with the same errors. The deflate part (Go to
   refuse a block without its end code, as the other three do) was dropped,
   then done the same evening on the owner's word, with a Go puff.c as the
   check, about 3 ms a megabyte of pixels (the owner chose the simple check
   over porting Swift's fast decoder). As first written: **One rule of
   strictness for the four** (item 7): an SVG that is not
   well-formed XML refused in PDFjet's words; a deflate block without its
   end code refused, Go to follow the other three.
3. ✅ **Example_46 as PDF/UA (7dba0ff5) and the language of a TextLine in a
   TextColumn (9a453539), done on 8 October 2026.** The third, the
   non-embedded CJK fonts full width, needs the width of each Latin
   character in each of the four Adobe CJK fonts, from Adobe's metrics: a
   half width for all would make W and M overlap their neighbours, worse
   than the gaps of today. Decided by the owner on 8 October 2026: not
   fixed, as NewCJKFont is deprecated and goes in v10; its deprecation note
   says now, in the four ports, that Latin letters and spaces are drawn full
   width in it. As first written:
   **Example_46 as PDF/UA, the language of a TextLine in a TextColumn, the
   non-embedded CJK fonts full width** (items 2 to 4).
4. ✅ **Done on 8 October 2026:** the font tool's `--pdfjet-forms-format`
   (item 8); the packages' license says what Solo and Team cover (item 9).
5. ⬜ **NuGet** (the owner, 8 October 2026: "We can probably get NuGet thing
   setup before then"): a package of PDFjet.dll, lib/net8.0 and lib/net48,
   strong-named, with System.Memory 4.5.5 as net48's dependency, as the
   .NET client asked. To decide first: public on nuget.org, which needs
   its license expression and the package's own README, or a private feed
   for buyers; and whether it is the evaluation or the licensed build.
6. ⬜ **Anything the .NET client finds in net48**, which ships untested on
   Windows in 9.0.3 (they test it).
7. ⬜ **The release:** the checks of the tag as for 9.0.3 (check-examples.sh,
   check-api.sh v9.0.3, the viewer pass kept to what can find a major
   issue), the packages, the GitHub release with the eval zips, FastSpring's
   four files, pdfjet.com by update-from-pdfjet.py and
   pdfjet-website-update.zip, the hidden links of v9.0.5 for direct buyers.

**Done the same evening: PDFjet Pro uses the library's parser, made public API, in the four ports; Pro's copies deleted.** Was: **After the one XML parser (8 October 2026): PDFjet Pro's copy.** The library
now has its own copy of Pro's `XMLParser`, for SVG, in the four ports
(src/internal/xmlparser, com/pdfjet/XMLParser.java, net/pdfjet/XMLParser.cs,
Sources/PDFjet/SVGXMLParser.swift), with two changes: a DOCTYPE skipped
instead of refused, and a space required between attributes, which XML asks
and Pro's copy does not check (it reads `b="1"c="2"`). Two copies of one
parser, until Pro uses the library's: then the library's made public (Go's is
internal now), with a setting for a DOCTYPE skipped (SVG) or refused (the
invoices). Meanwhile, the space between attributes into Pro's copy too, in its
four ports with a test (the owner asked, 8 October 2026: "So just to be clear
now we have the same XML parser in all 4 ports for both ZUGFeRD and SVG").

**Done for 9.0.5 on 8 October 2026, in the four ports with tests (f1027b4e, bcf3a76e, a9a9fd89, fda6dc6a, and the EXIF orientation, 9cc3afee), all in the v9.0.4 tag; checked again on 9 October.** As first written: **If 9.0.5 goes well, small v9.1 items come in** (the owner, 8 October
2026), only fixes users would see, in the four ports with tests, in this
order: a JPEG whose only fault is a missing end-of-image marker drawn, not
refused (the scan whole); the link box of a word drawn with its space ending
at the word; PDF/A-1 ignoring or refusing an annotation's opacity; the repair
scan of a broken cross-reference table not stopping at an "N G obj" inside a
stream. Big projects (one XML parser for SVG, object streams) stay in v9.1.
The goal of October: 9.0.5 and Forms both live by its end; after that, the
library in good shape, the work is Forms' teething issues.

**Sales:** PDFjet for Java and .NET stay on FastSpring, at least until 2027
(the owner, 8 October 2026); Forms is on Paddle alone. Paddle for the
library is ready in its sandbox for whenever (pdfjet-server TODO.md).

**A Hacker News post around 27 October**, with v9.0.5 (the owner, 8 October
2026): before it, Forms on AWS (CloudFront, WAF) and pdfjet.com behind
Cloudflare (pdfjet-server TODO.md, "Ready for a Hacker News day").

---


**The new plan, decided by the owner on 6 October 2026: v9.0.3 on Oct 8,**
not Oct 21, as every goal but the checks of the tag is closed and the freeze
of Oct 15 has nothing left to wait for; then PDFjet Forms, with the
library out of the way.

No code work is left: the library is feature-complete and frozen; what is
left is checking, tagging and packaging.

**Wed Oct 7: the freeze and the checks of the tag** (the list under "Oct
15–20" below, now on this day)
- ✅ `check-examples.sh` in the four ports: the 57 examples, the unit tests,
  veraPDF, the PDF/UA check, the cross-port and renderer comparisons. Passed
  on Oct 7 at 2e265241: every port and JDK 8, 180 files PDF/UA-1, the PDF/A
  files, 45 structure trees, the three viewer engines (largest block
  difference 3.2%, the same as at the calibration of Sep 29), and the 59
  snippets of the booklet.
- ✅ `./check-api.sh v9.0.2`: no public API changed. Java, C# and Swift the
  same; in Go, 13 setters of an int32 color list as gone and added, as
  their parameter was renamed `color` to `c` (the `Color.transparent` fix),
  which no caller can see: Go has no named arguments.
- ✅ `go test ./...` of pdfjet-server against master (its `go.work` builds
  `../pdfjet`), which reads images back out of the PDFs as the examples do
  not: ok, Oct 7.
- ✅ The benchmarks against 9.0.2 and 9.0.1, and the JDK 8 build: the same
  within noise (Example_43 1671 ms, 1665 at 9.0.2, 1673 at 9.0.1), the
  files the same size as at 9.0.2; `benchmarks/results/2026-10-07-2e265241.log`.
  JDK 8 in `check-examples.sh`.
- ✅ The viewer files built again, Example_30 with the `/Length 256` of
  Oct 1: from the PDFs of the check of Oct 7, Example_30 of the four ports
  opening with `hello` and `world`, the two encrypted PDFs with theirs.
- ⬜ The owner: the manual viewer pass on them, in Acrobat Reader, Preview,
  Chrome, Firefox and Edge.
- ✅ The `## v9.0.3` entry of CHANGELOG.md dated: 2026-10-08, the day of the tag.
- ✅ **The spaces of wrapped text, brought into 9.0.3 on the evening of Oct
  7** (the owner: "it bothers me that we advertise PDF/UA but the usability
  is not the way it should be"; the tag may slip for it). TextBlock draws
  the space it wrapped a line at, and a justified TextFrame row each word
  with its space, in the four ports, with a test in each (see "First after
  the tag", below, now done). The 62 example PDFs of each port render the
  same pixel for pixel as at 9804e50d, 3,420 pages a port. Committed as
  4adca138, and the checks above run again on it, 7 October:
  `check-examples.sh` passed (the four ports and JDK 8, 196 files PDF/UA-1,
  the PDF/A files, 50 structure trees, the three viewer engines, largest
  block difference 3.2% as before, the 59 snippets); `./check-api.sh
  v9.0.2` the same as on Oct 7, nothing new; pdfjet-server's `go test`
  passes but for its look test, which hashes the content of the pages and
  is refreshed when its go.mod moves to v9.0.3 (its 155 forms render the
  same pixel for pixel; see its TODO.md); the viewer files built again
  from this check, with Example_52 added, the justified TextFrame, for
  NVDA. Left: the owner's viewer pass on them.

- ✅ **A code review of every change since v9.0.2, on the evening of Oct 7**
  (the owner: "one more thorough code review"): six reviewers in parallel,
  Java, C#, Go, Swift, the Go float32 rounding of the arm64 fix, and the
  four ports side by side; each finding checked before acting on it.
  Fixed, in the four ports with a test in each, the 62 examples still the
  same pixel for pixel: a justified TextFrame row drew no space where
  Paragraph.spaceMovesToNext had moved it to the next text line (after a
  link, an underline or a change of font, found by three reviewers), and a
  row that ended at such a space, its word gone to the next row, ended
  with no space either, justified or not (older); Swift's trailingSpace
  used hasSuffix, which compares characters, and missed a space after a
  prepended mark such as U+0600. Clean: the float32 rounding (270 hunks,
  checked with go/types), and the ports agree. Left, not for 9.0.3:
  - ⬜ The owner's viewer pass: in Acrobat, are Example_06's three shapes as
    see-through as in Chrome (50%), or paler? The appearance has the
    opacity in an ExtGState and the annotation has /CA too; ISO 32000 says
    /CA applies to the appearance, so a strict viewer could draw them at
    25%. MuPDF, PDFium, Poppler and pdf.js draw them at 50%, measured.
  - ✅ v9.1: a JPEG whose only fault is a missing end-of-image marker, its
    scan whole, is refused since the cut-short check (c709d4c3), where
    viewers draw it with a warning: refuse only when the scan stops short?
  - ✅ v9.1: the repair scan of a PDF with a broken cross-reference table
    stops its search for endstream at the next "N G obj", which can be in a
    stream's own bytes (an embedded PDF, unfiltered).
  - ✅ v9.1: in PDF/A-1, an annotation's opacity below 1 is transparency,
    which PDF/A-1 forbids (/CA did so before 9.0.3; the appearance's
    ExtGState now too): ignore the opacity in PDF/A-1, or refuse it.
  - ✅ The four above, done for 9.0.5 on 8 October 2026 (f1027b4e, bcf3a76e,
    a9a9fd89, fda6dc6a), with the EXIF orientation of a JPEG (9cc3afee).
    Was: v9.1: the link box of a word drawn with its space (a justified
    TextFrame row, and TextColumn always) reaches one space past the word.
  - ✅ Done on 8 October 2026: Annot mapped to Span in PDF/A-1. Found by the PDF/A-1 fix: a PDF/A-1a document with an annotation fails
    veraPDF 6.8.3.4-1, the structure type Annot not role-mapped (PDF/A-1 is
    PDF 1.4, which has no Annot): a /RoleMap entry for it, in the four
    ports, an older fault; a PDF/A-1b one passes.

- ✅ **More automated checks, the night of Oct 7**, on ecd403c0, all clean:
  the C#, Java and Swift examples built under a German locale (decimal
  comma) render the same pixel for pixel, with no number written with a
  comma in their pages; qpdf (through pikepdf) and Ghostscript, two more
  parsers, find nothing wrong in the 228 example PDFs; a scan of every
  example with `mutool trace` for two words with a gap and no space between
  them finds none in PDFjet's own text (what it finds are table cells,
  list labels, subscripts, barcode digits and imported pages); and the 15
  Go fuzz targets fuzzed for 3 minutes each, the reader 8, about 23
  million inputs, with no crash or hang. Could become part of
  `check-examples.sh` and the workflow: the locale build and the two
  parsers (v9.1).

- ✅ **Overnight fuzzing, Oct 7-8**, 20:07 to 04:43 on 56d9a659, all clean: every
  Go fuzz target for 20 to 120 minutes, about 547 million inputs, with no
  crash, hang or failing input written. For a future run to set against:

  | Target | Minutes | Inputs |
  |---|---:|---:|
  | FuzzPDFRead | 120 | 137,741,293 |
  | FuzzJPGImage | 45 | 117,818,854 |
  | FuzzPNGImage | 45 | 46,724,959 |
  | FuzzSVGImage | 45 | 102,204,197 |
  | FuzzPNGImagePixels | 30 | 5,811,807 |
  | FuzzBMPImage | 30 | 26,948,673 |
  | FuzzOpenTypeFont | 30 | 4,110,821 |
  | FuzzOpenTypeFontTables | 30 | 1,061,298 |
  | FuzzSVGPath | 20 | 7,803,523 |
  | FuzzMarkdown | 20 | 1,669,055 |
  | FuzzMarkup | 20 | 808,915 |
  | FuzzFontStream | 20 | 44,697,717 |
  | FuzzFontStreamMetrics | 20 | 1,116,969 |
  | FuzzDecompressor | 20 | 40,669,122 |
  | FuzzDeflateRoundTrip | 20 | 7,624,830 |

  Each run as `GOWORK=off nice -n 15 go test ./src/ -run '^$' -fuzz '^Target$'
  -fuzztime 120m`, one after the other.

**Thu Oct 8, or when the checks of the spaces pass: the tag**
- ✅ The packages rebuilt on 04a3c6ac, 8 October 2026, both PDFjet.dll
  (net8.0 and net48) strong-named, public key token e66c1909913f295d. The
  owner, the same day: v9.0.3 out as soon as possible, v9.0.5 right after;
  only a major issue holds the tag. The net48 build is not run on Windows
  before the tag: the .NET client who asked for it tests it, and what they
  find goes into 9.0.5.
- ✅ **Tagged and released on 8 October 2026**, v9.0.3 at 174f6fcf, the GitHub
  release with the two evaluation zips; their releases/latest/download links
  answer, the Go proxy has v9.0.3 (.info and .zip 200) and sum.golang.org its
  checksum. The step as planned: tag v9.0.3 and make the GitHub release, with the two evaluation zips
  without the version attached (`gh release upload v9.0.3
  .commercial-packages/PDFjet-ForJava-Eval.zip
  .commercial-packages/PDFjet-For.NET-Eval.zip`); then the Go proxy's `.info` and
  `.zip` of it answering 200, and sum.golang.org having its checksum.
- ⬜ PDFjet Pro released on the tag, from `~/Projects/pdfjet-pro`, beside
  the library: `./set-version.sh 9.0.3` (go.mod, Package.swift and their
  sums move from 9.0.2), its four ports tested against the release, tag
  v9.0.3; the Java and .NET packages by `pdfjet-pro-private/packaging`.
- ✅ pdfjet.com rebuilt with the 9.0.3 packages, on the owner's go-ahead for
  the site: uploaded on 8 October 2026 (pdfjet-website b27802a), the
  evaluation's Accept buttons to the GitHub release, .NET Framework 4.8 on
  the .NET pages, a 9.0.3 entry in the news; the paid 9.0.3 packages on the
  four FastSpring products, which sell on (Paddle for Java and .NET later,
  pdfjet-server TODO.md).
- ⬜ pdfjet-pro made public with the release, by its PUBLIC-REPO-PLAN.md, on
  the owner's go-ahead.
- ⬜ The deliveries owed a new version: their links to the 9.0.3 packages
  (the owner's private notes).
- ⬜ **Where the packages live, decided by the owner on 8 October 2026:**
  - The free packages, the evaluation ones, as assets of the GitHub release
    of the tag, under names without the version, which the package scripts
    write beside the others: `PDFjet-ForJava-Eval.zip` and
    `PDFjet-For.NET-Eval.zip` (the version is in the folder inside). The
    website's evaluation links, on the owner's go-ahead for the site, are
    then for ever
    `https://github.com/edragoev1/pdfjet/releases/latest/download/PDFjet-ForJava-Eval.zip`
    and `.../PDFjet-For.NET-Eval.zip`, which GitHub sends to the latest
    release (the owner's choice, 8 October 2026). GitHub counts the
    downloads of each file of each release.
  - The paid packages on pdfjet.com, each release in a hidden folder of its
    own, `v9.0.3-JV-` or `v9.0.3-DN-` and 32 random hex characters, linked
    from Paddle's thank-you page and the delivery email, with the license
    certificate. Paddle hosts no files, as FastSpring did. Bandwidth is not
    expected to matter; the folder answers 403, not a listing.
  - Later, when sales grow, maybe per-order expiring links from S3 and
    CloudFront, once pdfjet-server's webhook and email exist for Forms.

**Fri Oct 9: PDFjet Pro sold through Paddle**, brought forward from
"after the launches": its steps are in the TODO.md of the commercial
product. Paddle's approval of each new product, and the license shown at
checkout, which waits on the lawyer, may take longer; FastSpring sells on
until then, so nothing is lost if it slips.

**v9.0.5, soon after v9.0.3** (the owner, 7 October 2026: public releases
have odd numbers, so 9.0.5 follows 9.0.3, never 9.0.4). Fixes found in the
viewer pass of Oct 7, no new API, in the four ports with tests, each
detailed in the list below:
1. ✅ The spaces of wrapped text: moved into 9.0.3 (above), on 7 October.
2. ✅ Done on 8 October 2026 (7dba0ff5). Example_46 as PDF/UA: in a tagged document, the optional content
   configuration (`/D` of `/OCProperties`) written with a `/Name` and
   without `/AS`, as PDF/UA 7.10 asks; veraPDF failed Example_46 on 7.10-1
   and 7.10-2 when it was made PDF/UA on Oct 7, so it stayed untagged. A
   layer set not to print cannot be said without /AS, so in PDF/UA it
   prints: the documentation of setPrintable says so, and Example_46's text
   too.
3. ✅ Done on 8 October 2026 (9a453539). The language of a TextLine kept in a TextColumn, so that Example_29's
   Greek is read as Greek (addCJKParagraph's language is new API: v9.1).
4. ✖️ Not fixed, decided on 8 October 2026: NewCJKFont is deprecated, goes in v10, and no example uses it since 7 October (8608ac64). The ASCII and the spaces of the non-embedded CJK fonts full width (the
   Korean gaps of the old Example_04): a /W, and stringWidth to match.
5. ✅ **PDFjet.dll strong-named**, done in 9.0.3 (both DLLs, net8.0 and net48, token e66c1909913f295d); as first planned, in every release from 9.0.5, the
   evaluation too. Asked for on 7 October 2026 by a prospective C# client
   ("is it possible to have PDFjet.dll signed, also for the evaluation?").
   Checked that day: the DLL of the 9.0.2 packages has neither a strong name
   nor an Authenticode signature, and no script signs it. Which they mean is
   asked in the reply; a strong name is the usual reason, as in .NET
   Framework a strong-named assembly can reference only strong-named ones.
   It is free: a key made once (`sn -k` or `dotnet` tooling), and
   `<SignAssembly>true</SignAssembly>` with `<AssemblyOriginatorKeyFile>` in
   PDFjet.csproj (and PDFjet.Sign.csproj of PDFjet Pro). The key is kept for
   ever, in the private folder and its backups, never lost: another key is
   another identity of the assembly for every client. Whether to publish
   the key, as Microsoft suggests for open source libraries, or keep it
   private, to decide. If the client needs it before 9.0.5: a **9.0.4
   custom build** for them, even numbers being the custom builds (the
   owner's rule). Authenticode, if asked instead: a code-signing
   certificate, about $200 to 400 a year, which a sole proprietor can get,
   and `signtool`, or `osslsigncode` on Linux, at each release.
   **Prepared on 7 October 2026:** the key made,
   `~/Projects/pdfjet-pro-private/signing/PDFjet.snk` (RSA 1024, the format
   of `sn -k`, mode 600; back it up with the private files, and never lose
   it). A strong-named PDFjet.dll built from master with the key given on the
   command line, PDFjet.csproj unchanged: `dotnet build PDFjet.csproj -c
   release -p:SignAssembly=true -p:AssemblyOriginatorKeyFile=<the key>`, its
   public key token `e66c1909913f295d`; a strong-named client referencing it
   made a PDF. Found besides: the DLL has no version, it says 0.0.0.0, as
   neither PDFjet.csproj nor package-dotnet.sh sets one; a strong-named DLL
   should say AssemblyVersion 9.0.0.0, kept through 9.x so that clients need
   no binding redirects, and the release (9.0.5) as FileVersion and
   InformationalVersion. And PDFjet targets net8.0 alone, while strong names
   matter most to .NET Framework 4.x, which cannot load a net8.0 DLL: to ask
   the client which .NET they use; .NET Framework would need a netstandard2.0
   build too, a larger piece of work, as some APIs of the code may not be
   there.
   **Authenticode, decided with the owner on 7 October 2026:** PDFjet
   Software buys it, if ever, as it proves who published the file, and in
   our name; one certificate signs everything we publish for Windows:
   PDFjet.dll and PDFjet.Sign.dll, Arcana's Windows build (which SmartScreen
   warns about unsigned, see arcana-secrets' TODO.md), any tool or installer
   later. Bought only when a paying customer needs it or Arcana ships for
   Windows, whichever is first; for a large client it can be said to come
   with their license, but its cost alone is no reason for a new price
   level: the four ports in one package at a higher price stands on its own,
   signing one more reason for it. The cheaper way to check first:
   Microsoft's Trusted Signing (Azure), about US$10 a month, the keys kept
   by Microsoft, whose eligibility has been limited at times (businesses of
   some years; individuals in some countries, Canada among them lately);
   else a code-signing certificate of Sectigo, DigiCert or SSL.com, about
   $200 to 400 a year, its key on a hardware token or a cloud HSM since 2023.
   The strong name is free and goes in every .NET release from 9.0.5.
   **A netstandard2.0 build, measured on 7 October 2026**, for .NET Framework
   4.6.1 and later (and Mono, Unity), should the client be on .NET Framework
   4.x, which cannot load a net8.0 DLL: a scratch copy built for
   netstandard2.0, master untouched. Three layers: Span and ReadOnlySpan,
   from Microsoft's System.Memory package; two Stream overrides of spans
   (Decompressor.EndOfInputStream.Read, Page.WrittenContent.Write), compiled
   for .NET 8 alone (`#if NETCOREAPP`); then about 56 errors of some 20 newer
   APIs, mostly in Page.cs, Decryptor.cs and Bidi.cs, with plain stand-ins:
   MD5/SHA256/SHA384/SHA512.HashData by Create().ComputeHash(); Array.Fill,
   Convert.ToHexString, BitOperations by small helpers; Encoding.Latin1 by
   GetEncoding(28591); MemoryStream.Write of a span and the char overloads by
   the array and string ones; IndexOfAnyInRange (Bidi) by a loop;
   RandomNumberGenerator.Fill and Aes.DecryptCbc by the older calls, and
   CryptographicOperations.FixedTimeEquals by a constant-time compare of our
   own, both tested with care; ZLibStream by DeflateStream with the zlib
   header and Adler-32 written by hand, moderate; and MathF by Math with
   casts, the careful one, as MathF.Sin and (float)Math.Sin may differ in
   the last bit and so move a coordinate: the PDFs must stay the same as
   Java's, byte for byte, in the cross-port comparison. Then a test on the
   real thing, .NET Framework on Windows or Mono, which CI on Linux does not
   do. About two to three days, with one target more in PDFjet.csproj
   (`<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>`), net8.0
   unchanged. Not to do on speculation: if the client is on .NET Framework
   and buys, as their 9.0.4 custom build, which then serves every C#
   customer still on .NET Framework; on .NET 6 or 8 the strong name alone,
   ready. Found besides: PDFjet.csproj has GenerateAssemblyInfo false and no
   AssemblyInfo.cs, hence the version 0.0.0.0.

6. ✅ **Moved into 9.0.3** (the owner, 8 October 2026, as the tag may slip a
   couple of days): `package-dotnet.sh` builds `net48/PDFjet.dll`, signed
   with the same key and the release's file version, into the packages'
   `net48/` folder with its four DLLs; the package's README and CHANGELOG say
   so. The client's 9.0.4 is needed only if they cannot wait for the tag.
   Still to do before the tag: the examples run on .NET Framework on
   Windows (`net48/make-examples-folder.sh`).
   **The plan as it was: .NET Framework 4.8 and .NET 8 in one package, from 9.0.5** (decided
   by the owner on 8 October 2026). The client of item 5 answered that day:
   their internal NuGet package is multi-target, net48 and net10, and the
   net8.0 PDFjet.dll fails on net48 ("uses System.Runtime 8.0.0.0, which has
   a higher version than referenced System.Runtime 4.1.2.0"). net10 needs
   nothing: a net8.0 assembly runs on .NET 10. So `<TargetFrameworks>net8.0;
   net48</TargetFrameworks>`, both strong-named with the same key, token
   e66c1909913f295d, AssemblyVersion 9.0.0.0; the packages with
   `lib/net48/` and `lib/net8.0/`, as NuGet chooses between them, .NET 9 and
   10 taking net8.0. Not net48 alone, which runs on Windows only and on .NET
   8 through a compatibility shim with a warning, and is slower; not
   netstandard2.0 alone, the older code everywhere and facade DLLs on 4.8.
   net48 rather than netstandard2.0 beside net8.0, as the client asks for
   it, and .NET Framework 4.8 is where the .NET Framework applications are.
   The work is the one measured on 7 October (item 5): System.Memory for
   net48 alone (with System.Buffers and Unsafe, three DLLs beside its
   PDFjet.dll), the two Stream overrides of spans `#if !NETFRAMEWORK`, the
   about 56 errors behind `#if NETFRAMEWORK` in one helper file, net8.0
   unchanged; ZLibStream and MathF the careful ones, the PDFs of net48 the
   same as net8.0's byte for byte. Measured again on 8 October for net48,
   with Microsoft.NETFramework.ReferenceAssemblies to build on Linux: the
   same list. About two to three days, the examples run on .NET Framework
   on the owner's Windows machine (Linux has no .NET Framework, and Mono is
   not installed). Before 9.0.5, the client can have the net48 build of
   that work as their 9.0.4, the custom build of the owner's rule.
   **Changed the same day, the owner's wish: nothing of net/pdfjet changes
   for net48, and the main branch never goes backward.** So `net48/`, a
   folder of its own (see net48/README.md): `PDFjet.Net48.csproj` compiles
   `net/pdfjet` as it is, with the .NET 10 SDK for C# 14; the two overrides
   of spans cut from copies by a task of the project file, found by their
   first lines or the build stops; `Compat/Polyfills.cs` the gaps, C# 14
   static extension members and classes of the missing names;
   `Compat/ZLibStream.cs` on `Compat/Puff.cs` (a C# translation of puff.c)
   for the zlib streams, and `check-zlib.sh`, which holds it to .NET 8's
   ZLibStream: Inflate, InflatePrefix and InflateExact the same on 1,032
   streams, whole, cut short, with wrong checksums, bytes after them, or
   corrupted. Built, 0 warnings; still to do: package-dotnet.sh building and
   signing both, the examples run on .NET Framework on Windows, and the
   client's 9.0.4.

7. ✅ **Fuzzing beyond Go, done on 8 October 2026:** the Go corpus replayed in Java, C# and Swift, and Jazzer overnight on Java (findings below and in item 1 of the plan). As first written: **Maybe: fuzzing beyond Go** (the owner, 8 October 2026). Only the Go
   port is fuzzed; what it finds is fixed in the four, which share the
   logic, and the corpora were replayed in the four in September (CHANGELOG:
   1,257 PDFs, 1,066 streams, the four the same). It is reasonable to expect
   the hardening to hold in Java, C# and Swift; but the same logic fails
   differently in each: an integer overflow wraps in Go, Java and C# and
   traps in Swift; deep recursion is an error caught in Java, a
   StackOverflowException that ends the process in .NET, a crash in Swift;
   each port has its own zlib and memory. So, to know rather than expect:
   - First, cheap: replay in Java, C# and Swift the corpus of the overnight
     run of Oct 7-8, in Go's cache (`go env GOCACHE`/fuzz), some 16,000
     inputs of PDFjet's targets (2,862 PDFs, 1,809 fonts, 1,513 PNGs...),
     each reaching code the earlier ones did not; a crash, a hang, or a
     file Go reads and another port fails, fixed in the four. This time as
     a script, to run after each long fuzz. About a day.
   - Then, maybe: Jazzer (Code Intelligence, on libFuzzer) for Java, the
     port that sells most: fuzz targets of the same parsers in Java, run
     overnight as the Go ones were, which reach the paths of Java alone.
     SharpFuzz for .NET after it, if Jazzer finds what the replay does not.

   ✅ **The replay, done on 8 October 2026; findings recorded, fixed in
   9.0.5, nothing changed before the tag.** A Go test, kept out of the
   repository, wrote each input of the 15 targets as the target builds it
   (the PNG, the font, the SVG path from their parts), with Go's outcome and,
   for PDF.read and the decompressor, Go's report of digests; Java, C# and
   Swift each replayed the 17,476 inputs and compared. The scripts are in
   ~/Projects/pdfjet-fuzz-replay (README.md), to move into tests/ after the
   tag. No hang and no input over 256 MB in any port; Swift, no trap at all.
   The decompressor's 428 reports the same in the four; PDF.read's 2,865
   the same in all but 8 to 12 each (below); the BMP, JPEG, PNG pixels,
   Markup, Markdown, SVG path and deflate targets the same throughout.

   Faults, both of malformed files only, to fix in the four:
   - **OTF: `hmtx` before `hhea`** (3 inputs, e.g. FuzzOpenTypeFont 00747):
     advanceWidth is made when hhea is read, so a font whose table directory
     lists hmtx first, or that has no hhea, fails with a
     NullPointerException in Java (OTF.java:402) and a
     NullReferenceException in C# (OTF.cs:389). Go loops over its nil slice
     and refuses the font later, "no advance widths"; Swift likewise. Fix:
     read hhea before hmtx whatever the directory's order, and refuse a font
     without it, in words.
   - **SVG: a malformed number** (157 inputs, e.g. FuzzSVGImage 00001):
     `-.`, `.`, `1d775`, `2.@5` get past the tokenizer to Float.parseFloat
     (Java, SVG.java, 26 calls, lines 164-320) or float.Parse (C#, SVG.cs,
     about 25, lines 149-306), whose NumberFormatException or
     FormatException escapes new SVGImage. Go refuses the same input with
     its own error. Fix: one parse function that refuses in PDFjet's words.

   Differences of strictness, to decide once for the four:
   - **SVG that is not well-formed XML.** Each port reads it with its own
     XML parser: Go's encoding/xml is lenient about some faults
     (`width="1"height="1"`, junk at the root, no root) and strict about
     others; Java and C# (XmlException escaping SVGImage.Read, C#
     SVGImage.cs:82, 448 inputs) refuse 112 that Go draws; Swift draws 336
     that Go refuses. Proposal: strict in the four, an SVG that is not
     well-formed refused in PDFjet's words, as the SVG files of PDFjet's
     users come from editors that write well-formed XML. C# also reads a
     number past float32's range as Infinity where Go refuses it (5).
   - **A PNG whose deflate block has no end-of-block code** (10 or 11):
     Go draws it, its decompressor stopping when it has the image's bytes;
     Java's Inflater, .NET's zlib and Swift's Puff refuse it (.NET with the
     misleading "unsupported compression method"). The stream is invalid;
     strict is right, Go to follow. The same in 11 font stream metrics
     inputs in C#, and Java refuses 78 "The marks of the font cannot be
     read." that Go reads: to look at.
   - **PDF.read, the stamp step** (12 inputs, Java and C#): a resource named
     `/` or `>` in a malformed dictionary; checkImportedName throws at once
     in Java and C#, where Go keeps the error for Complete(). Both refuse;
     no fix needed.
   - **PDF.read, Swift** (8 inputs): a page's joined content one newline
     longer, as Swift skips a part of /Contents whose stream is nil and Go
     one whose data is nil (pdfobj.go:609, PDFobj.swift:636). Draws the
     same; align when convenient.

8. ✖️ Obsolete since 9 October 2026, the font tool removed with the .stream format. **The font tool's `--old-format` renamed `--pdfjet-forms-format`**
   (the owner, 8 October 2026): util/GenerateStreamFontsFiles.java and
   util/generate-stream-fonts-files.sh, their usage text and comments; the
   format is the one pdfjet-server's fonts are written in (its DESIGN.md,
   "Which font files", names --old-format: change it there too), and the
   CHANGELOG entry of 9.0.5 says so. The flag is of a tool in util, not of
   the library, so nothing else changes.

9. ✅ Done on 9 October 2026 (575ded00), in both LICENSE files. **The license files of the packages say what Solo and Team cover**
   (8 October 2026): `.packaging/java/LICENSE` and `.packaging/dotnet/LICENSE`,
   section 1, the sentence the buy pages of pdfjet.com have had since that
   day, "A Solo License covers one developer; a Team License covers two to
   five developers.", so that the license in the download is the one on the
   site. The licenses were renamed that day, Named Developer to Solo ($295)
   and Transferable to Team, 2 to 5 developers ($695); the lawyer reviews the
   wording with PDFjet Pro's license.

**After the tag, when convenient, blocking nothing:**
- ✅ **Done in 9.0.3, on 7 October 2026 (it was "first after the tag"; the
  owner: "I really want to know if we are doing everything right"): the
  space at the end of each wrapped line is stripped, in the four ports.**
  TextBlock's lines keep a flag, trailingSpace, set when the line was
  wrapped at a space, and Page.drawTextBlock draws the space after the line;
  a justified TextFrame row draws "word " with isLastToken, so its underline
  stops at the word. Pixel test passed in the four ports (13,680 pages);
  `mutool trace` shows the spaces; Word's PDF beside ours, the comparison
  planned below, was not needed, since tagged PDF asks for the spaces anyway.
  The note as it was: Found by the owner's NVDA pass:
  Example_01, once its languages were fixed, reads in English, Greek and
  Bulgarian but choppy, with a pause at every line end. Checked on Oct 7:
  TextBlock builds a wrapped line word by word, each with its space, then
  trims it (Java `sb.toString().trim()`, C# `Util.Trim`, Go
  `strings.TrimRightFunc`, Swift `sb.trim()`), and TextColumn does the same
  at its breaks (`trimTrailingSpaces`); in Example_01 every wrapped line
  ends with no space ("…this right" then "includes…"), so a reader that
  joins the lines has no space between them and must guess the word break
  from the geometry, and a speech engine may take the line end for a stop.
  Part of the choppiness is Acrobat's, which gives a screen reader a line
  at a time, and the OneCore voices'. The likely fix: draw the space at the
  end of each wrapped line, as Word's PDFs are believed to, since it is
  invisible, but leave it out of the width, so that right aligned, centered
  and justified lines stay where they are; or an /ActualText of the line
  with its space. First confirm: Example_01's text exported from Word, read
  by NVDA in Acrobat beside PDFjet's, and the text of both as pdftotext,
  mutool and Acrobat give it. Then in the four ports with tests, and the
  examples' baselines move with it.

  Which classes, checked on 7 October 2026 in the Java PDFs with `mutool
  trace`, which lists the glyphs drawn (pdftotext and `mutool draw -F
  stext` add spaces of their own where they see a gap, even with `-O
  inhibit-spaces`, so they cannot tell):
  - TextBlock strips the space at the end of each wrapped line (Example_01).
  - TextColumn keeps it, justified or not: each word is drawn with its
    space (Example_10, 29, 44). Its trimTrailingSpaces is for the width
    alone; the note above that TextColumn strips it was wrong.
  - TextFrame keeps it in a row that is not justified (Example_47), but a
    justified row is drawn word by word with no space at all, between the
    words or at the end (Example_52: 289 of the 294 text runs of page 2 are
    single words; only the last line of each paragraph, not justified, has
    spaces). The worst of the three: a reader or an extractor has to guess
    every word break from the gaps. The fix: draw each word with its space,
    as TextColumn does, the space's width plus the share of the stretch.
  - Justified text is drawn by TextColumn and TextFrame alone; TextBlock and
    Cell do not justify (Cell draws JUSTIFY as LEFT, its text being one line).
  The Go, C# and Swift PDFs to trace the same way before the fix.

  The test that the look does not change: the spaces have no ink, so every
  example, rendered before and after the fix (mutool draw, 150 dpi, every
  page), must be the same pixel for pixel, in the four ports; a page that
  differs is a line that moved, most likely a justified one whose stretch
  took the added space into account. Then the text test: `mutool trace`
  shows a space at the end of each wrapped line and between the words of
  justified rows, and NVDA reads Example_01 and 52 with fewer pauses.
- ⬜ **The language of text in a TextColumn reaches the structure tree**
  (the NVDA pass of 7 October 2026: Example_01's Greek was spelled letter
  by letter and its Bulgarian skipped, as no block said its language; fixed
  in Example_01 with TextBlock.setLanguage). Two gaps of the library remain,
  in the four ports: a TextLine's setLanguage is dropped when the line is in
  a Paragraph of a TextColumn, as in Example_29, whose Greek in a table cell
  so reads in English; and TextColumn.addCJKParagraph makes its lines with no
  language, so Example_44's Chinese reads in English, and has no parameter
  for one. Fix: the Paragraph and the TextColumn carry each line's language
  into the /Lang of its structure element; and addCJKParagraph(font, text,
  language), or a setLanguage on Paragraph, an API addition, so in v9.1.
  Then Example_29 and 44 set theirs (el; zh-Hans).
- ⬜ Maybe, if a customer asks: **embedded look-alikes of the core fonts**
  (the owner, 7 October 2026). The 14 core fonts stay, not deprecated: they
  need no font files, make the smallest documents (Helvetica about 3 KB, a
  page in embedded Plex about 100 KB), and are the most used line of PDFjet's
  history; and some customers must use Helvetica or Times: US courts ask for
  Times New Roman 12 point, brand guides name Helvetica or Arial, old forms
  are laid out to Helvetica's widths. But a PDF/UA or PDF/A document cannot
  use them. Fonts drawn to the same widths, free, embedded as .otf.stream,
  would give those customers PDF/UA with the look and the line breaks they
  must keep, with no font to buy:
  - **Liberation** Sans, Serif and Mono (SIL OFL 1.1): the widths of Arial,
    and so of Helvetica, of Times New Roman and of Courier New.
  - **URW Nimbus**, the core 35 fonts of Ghostscript (AGPL with a font
    exception, which allows embedding): Nimbus Sans for Helvetica, Nimbus
    Roman for Times, Nimbus Mono PS for Courier, and Standard Symbols PS and
    D050000L for Symbol and ZapfDingbats, the only set that matches all 14.
  - **TeX Gyre** Heros, Termes and Cursor (GUST Font License), from the URW
    fonts: Helvetica, Times and Courier.
  - **Arimo, Tinos and Cousine** (Google, Apache 2.0), the same designs as
    Liberation 2.x.
  Which, if any, to choose by its license and its coverage (Latin, Greek,
  Cyrillic); a font class of each, as IBMPlexSans has. Meanwhile the core
  fonts' documentation says: small and needing no files, but not PDF/UA or
  PDF/A; for those, IBM Plex.
- ✅ Not fixed, a note instead (the owner, 8 October 2026: NewCJKFont is deprecated, gone in v10). **The spaces of Korean, and the ASCII of every non-embedded CJK font,
  full width** (the viewer pass of 7 October 2026: in Acrobat, Example_04's
  "새해 복 많이 받으세요!" has a gap of a whole em at each space). The fonts
  of Font(pdf, "AdobeMyungjoStd-Medium") and the other three Adobe CJK
  names, not embedded, for the Asian font packs, get no /W in their CIDFont
  dictionary, so every CID, the space and the ASCII letters too, is 1000
  units wide, the default /DW; and `stringWidth` counts every character as
  the font size, so the layout agrees with the drawing: full-width spaces by
  design, not a miscount. Chinese and Japanese write no spaces, so only
  Korean shows it, and Latin in any of the four (Example_04's "!" too). The
  fix, in the four ports at once, both sides together: a /W for the
  proportional or half-width CIDs of the ASCII range of each ordering (Adobe-
  Japan1, -GB1, -CNS1 and -Korea1 each put ASCII at CIDs 1 to 95 or so, by
  their own tables, to be read in Adobe's specifications), and
  `stringWidth` measuring those characters with the same widths. It changes
  Example_04's PDF in every port, so the cross-port baseline moves with it.
  Rare path (fonts not embedded, not PDF/UA), not new, so after the tag.
- ✅ **Example_02 split in two**, done for v9.0.3 on 7 October 2026 (the
  owner): Example_02 the Simplified and Traditional Chinese, with the whole
  IBM Plex Sans SC and TC, and Example_04, in place of its greeting in the
  Adobe CJK fonts, the Japanese and Korean, with IBM Plex Sans JP and KR;
  Example_44 with IBM Plex Sans SC; no example uses the Adobe CJK fonts.
- ⬜ Maybe: **IBM Plex Sans SC and TC subsets**, every weight, made on 7
  October 2026 and kept in
  `~/Projects/pdfjet-pro-private/plex-subsets/`, not on GitHub (the owner:
  "for later use / maybe"), with a README of how they were made: about
  0.7 MB in place of 5.4 for SC (3,500 characters), 0.9 in place of 3.6 for
  TC (4,808). If ever used, they go to pdfjet-fonts with a new pin, and
  Example_02 would embed about 1.7 MB of fonts, not 9.5. `--retain-gids` is
  needed, as the Plex CJK fonts are CID-keyed and PDFjet draws with the
  glyph IDs as CIDs. Real subsetting as the PDF is written is planned for
  the commercial product.
  **How, when it is done (talked over with the owner, 8 October 2026):**
  only the large fonts, over about 1 MB or of thousands of glyphs, in
  practice the CJK ones; every other font keeps the fast path, the
  precompressed `.stream` copied as it is (a whole IBM Plex Sans is about
  60 KB, not worth the time). The simple and safe way: no new font, the
  glyph IDs kept, the outlines of the glyphs the document does not use
  emptied (CFF: the CharStrings rewritten with empty entries, the
  subroutines kept whole; TrueType: the unused glyf entries emptied and
  loca made again), so the cmap, the widths and the ToUnicode for screen
  readers do not change. The `.stream` still serves as the source:
  inflated in memory, emptied at complete(), deflated again, much smaller.
  Expected faster overall for CJK documents (writing 15 MB costs more than
  one pass over the glyphs); the cost is the inflated font in memory while
  the document is open, cached across documents on a server. Measured
  first, a CJK document before and after.
  **Exactly how (8 October 2026, worked out on the Java port):**
  1. *The used glyphs, as pages are drawn.* Every glyph of an embedded font
     reaches a page through one private method of Page.java,
     `appendCodePointAsHex` (ten call sites: TextLine, TextBlock, cells,
     shaping, marks); its other callers, lines 808-810, write code points
     for the non-embedded CJK fonts, which have nothing to subset. A field
     in Font, a plain array of booleans, one per glyph, the same in the four
     ports (`boolean[]`, `bool[]`, `[]bool`, `[Bool]`, sized by the font's
     glyph count; not BitSet, BitArray or IndexSet, which differ from port
     to port), and the funnel made `appendGlyph(Font font, int gid) {
     font.used[gid] = true; appendCodePointAsHex(gid); }`. One array per
     font for the whole document, a byte a glyph (20 KB for 20,000 glyphs,
     nothing next to the font); pages are written and dropped as now. Two
     Font objects of one file, embedded once (name and checksum), have their
     arrays joined at complete().
  2. *The font program written last.* new Font(...) writes the font
     dictionary, the descriptor, /W and ToUnicode at once, as now, but only
     reserves the object number of the font program (FontFile3/FontFile2);
     it is written at complete(), after every page. A PDF's objects may come
     in any order, the cross-reference table says where each is. The one
     change to PDFjet's core: the writer accepts an object number reserved
     early and written late, in the xref code of each port, with its tests.
     So the streaming stays: pages go out as they are drawn; held meanwhile
     are the array and the font.
  3. *CFF, at complete().* The CharStrings INDEX (count, offSize, count+1
     offsets, the charstrings) rebuilt with the same count: glyph 0 and the
     used glyphs copied byte for byte, every other one `0x0E`, endchar, a
     glyph that draws nothing (not a box: the box of a missing character is
     .notdef, glyph 0, kept). The blocks after it move, so the CFF is written
     again in a fixed order with every offset of the Top DICT, and of the
     FDArray's Private entries of a CID font, in the 5-byte integer form, so
     that a changed offset never changes a DICT's size. The subroutines kept
     whole (a second step, maybe: emptying those no used glyph calls).
  4. *TrueType.* The used set completed with the parts of the composite
     glyphs used; unused glyphs of zero length in glyf (their loca offsets
     repeat); the other tables copied.
  5. *Embedded:* deflated, written to the reserved object; the font's name
     prefixed with a tag of six capitals and a plus, as the spec asks of a
     subset; for PDF/A-1, a CIDSet of the glyphs present, from the array.
     Nothing else changes: glyph numbers, /W, ToUnicode, the pages.
  6. *Not subset:* a font of a fill-in field, whose typed glyphs are unknown,
     and a font set to stay whole (a setting, e.g. setSubset(false)).
  7. *The test:* the CJK examples made with the whole font and with the
     subset, rendered and compared pixel for pixel, and their text extracted
     and compared; in the four ports, whose pages have the same funnel.
  8. *TrueType only, decided by the owner on 8 October 2026* ("Noto subset
     is good enough - do TrueType only"): no CFF rewrite; a CFF font is
     embedded whole, as now. IBM Plex's CJK families come as .ttf too
     (github.com/IBM/plex, packages/plex-sans-*/fonts/complete/ttf), 20 to
     40% larger than their .otf whole (SC 8.44 MB against 6.97, TC 5.45
     against 4.58, JP 5.71 against 4.05; KR 2.54 MB .ttf), which is why
     fonts/ has the .otf; subset, the difference hardly matters, so a user
     who wants the Plex look subsets its .ttf. What follows was written
     before the decision: *TrueType first, CFF second* (8 October 2026). PDFjet's fonts/ has
     both: IBM Plex Sans SC, TC, JP and KR are CFF (.otf, OTTO), the ones
     the examples use; Noto Sans SC, TC, JP and KR are TrueType (.ttf, glyf),
     as are the Windows CJK fonts. TrueType first, the easy case (glyf and
     loca, no offsets elsewhere), which proves the shared parts: the
     reserved object, the array, the test; then CFF, the offsets. With
     TrueType alone a user can already use Noto. Measured that day with
     fontTools on NotoSansSC-Regular.ttf (30,894 glyphs), the glyphs of a
     document kept and the rest emptied, compressed: the 235 characters of
     data/languages/simplified-chinese.txt, about 130 KB; 1,000 characters
     about 250 KB; 3,500 about 740 KB. Against the whole fonts embedded
     today: IBM Plex Sans SC 5.7 MB, Noto Sans SC 6.3 MB. About 100 KB of
     the subset is the other tables (cmap, hmtx, GSUB and GPOS), kept whole;
     trimming them is a later maybe.
  8b. *Which fonts, decided by the owner on 8 October 2026:* every TrueType
     font is subset, whatever its size, and every CFF font (.otf) is
     embedded whole from its precompressed .otf.stream, as now: a rule by
     the format, not by a size, so that a user can tell what happens to a
     font. It matches the TrueType-only subsetting, it gains on the Latin
     TrueType fonts too (Noto Sans, about half a megabyte a weight, to a few
     tens of KB in a document), and it keeps the fast path for the IBM Plex,
     Source Serif and other .otf fonts it was made for. Not subset still: a
     font of a fill-in field, a font whose license forbids it, and a font set
     to stay whole.
  8c. *The benchmark that comes with it* (the owner, 8 October 2026): one
     document made with Noto Sans (.ttf, subset) and with IBM Plex Sans
     (.otf.stream, embedded whole), the time to make the PDF, its memory and
     the size of the file, in benchmarks/ beside the results of 9.0.x, so
     that later releases are set against it. Noto is the case that matters:
     the font developers choose for its languages, and the one that makes
     PDFs heavy today.
  9. *The font's license allows it:* the OS/2 table's fsType bit 0x0100,
     "no subsetting", read first; a font that sets it is embedded whole.
     IBM Plex and Noto allow subsetting (SIL Open Font License); a user's
     own commercial CJK font may not.
- ⬜ The package registries, in this order (see "After the tag: the package
  registries" below): Go (one fetch of the proxy, and pkg.go.dev lists
  it), NuGet first of the others (C# is the largest audience, about an
  hour), the Swift Package Index (a form), Maven Central last (a TXT record
  at IONOS, a GPG key; half a day to a day the first time).

The calendar below is the plan as it was, kept for its lists.

Target: **2026-10-21**, the date v9.0.0 was planned for. v9.0.0 was released
early, on 2026-09-16 at e957f841, and v9.0.1 on 2026-09-18 at 72c41923, so
what Oct 21 carries is **v9.0.3**: a foundation to build on after it, and
two features the work was far enough ahead to fit in: `Cell.setRowSpan`
(Sep 21) and Markdown to PDF, merged from its branch on Sep 22. Today is
Sep 22, so 29 days are left, with one fix release in the middle. This file
is the working list; tick items off as they land on master.

The work runs ahead of the calendar below. Goals 1 and 2 are closed: every
class of the library is read end to end, three weeks before the Oct 9-14 week
that was to finish it. v9.0.2 was released on Sep 28, three days before the
Oct 1 planned for it, after a code review of the four ports.

What is done is in CHANGELOG.md and in the git history; this file lists only
what is left.

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## The three dates

- **Oct 1** — v9.0.2 of the MIT library only, the `pdfjet` repository, cut
  from master: the API of v9.0.1 and the members goal 6 lists, Markdown to
  PDF and `PDF_A_3A_UA_1` among them. The commercial product is not released
  with it. What is in it is under `## Unreleased` in CHANGELOG.md: the PDF/UA examples and the
  table tagging, kinsoku, the font line gap, the WinAnsi characters and
  kerning of the core fonts, the fallback font rule, the CMYK JPEG marker,
  the font stream, PNG, BMP, JPEG, SVG, OpenType, decompressor and `PDF.read`
  fixes that fuzzing found, and the reviews of the image classes, `Page`,
  `TextLine` and the reader.
- **Oct 15** — code freeze: fixes only, each with its check. *Now Oct 7.*
- **Oct 21** — *now Oct 8.* v9.0.3 of the MIT library and, the same day, v9.0.3 of the
  commercial product (`.commercial`), built on the 9.0.3 library. The same number says which
  library each release of the commercial product is built on.

## Why 9.0.3 is hardening and not features

The bugs found from Sep 17 to 19 were almost all in code nobody had read end
to end, and almost all were silently wrong output that every test and example
check passed: 23 in the reviews of `Cell`, `Table`, `TextBlock`, `TextColumn`
and `PDF`; then 32-bit BMPs in the wrong colors, ’ and € drawn as spaces in
the core fonts, every CMYK JPEG inverted, a fallback font that stuck, the CJK
line gaps, and what the first three fuzz targets turned up. Ten kinds of
untrusted input are read, from a file or from the text a document is
written from, and all ten are fuzzed. So the work to Oct 21 is the seven
goals below, in this order.

Six features are the exceptions. Five are in v9.0.2. The first is
`Cell.setRowSpan` (Sep 21): goals 1 and 2 closed three weeks early, the
feature is what clients ask `Table` for and could not have, and it landed
with tests in the four ports and an example that renders alike in all of
them. The second is the page breaks of `Table` (Sep 22): the wrapped lines
of a row are kept together, which changes one page break of Example_34, and
`keepRowWithNext`, `setNumberOfFooterRows`, `setPageSum`, `setRunningSum`
and `setBroughtForwardSum` are new, each with tests in the four ports. The
third and the fourth, the same day, are the rest of what "After v9.0.3 --
features" had, from the review of `Table` and `Cell` of Sep 18: striped
rows with styles for the header and the footer rows, and column widths
shared from the width of the table. The fifth, the same day,
is a `TextFrame` that flows onto as many pages as its text needs.

The seventh, on Sep 24, is `Compliance.PDF_A_3A_UA_1`, PDF/A-3a and
PDF/UA-1 in one document, which PDFjet Forms writes every PDF as: archival,
accessible, and able to carry the layout it was made from. It is a new
member of an enum and a few lines in the four ports, since the tagging of
PDF/UA and the metadata of PDF/A were both there, and it is checked by
veraPDF with both profiles, in check-examples.sh by Example_55. With it, the
same day, `setAltDescription` of `Barcode` and `QRCode`, so that a barcode
of such a document is read aloud rather than skipped, and the content of a
figure is the figure, marked no further.

The sixth is Markdown to PDF, which is in v9.0.2 with the others, since
9.0.2 is cut from master: it was written on a branch of its own with the
checks master has, and merged on Sep 22 once it had them all. It is the one feature of the release that is
not a small addition to something that was already there, which is why it
was kept off master until it was done. The other members master adds to the
API of v9.0.1 came with the PDF/UA work and the fixes; goal 6 lists them.

## The seven goals of v9.0.3

1. ✅ **B** Fuzz the parsers of untrusted input with Go's fuzzing, and fix each
   failure in the four ports, which share the logic. Any input either works or
   fails with a clean error: no hang, no index error, no runaway memory. The
   targets and their corpora stay in the repository, in `src/*_fuzz_test.go`
   and `src/testdata/fuzz`.
   - ✅ Done (Sep 19–21): the eight targets -- the `.stream` fonts, PNG, BMP,
     JPEG, SVG, OTF and TTF, the decompressor and `PDF.read` -- each replayed
     in the four ports, and every failure they found fixed in the four ports
     with a test. What was fixed is in CHANGELOG.md.
   - ✅ Two more since (Sep 22): `Markup` and `Markdown`, the readers of the
     text a document is written from. They found the quadratic joining of
     `TextFrame`, and a `Markdown` that looped forever on a surrogate pair in
     a code block 31 quotes deep. Ten targets in all.

2. ✅ **B** Finish the class-by-class review, as on Sep 17: one class end to
   end in the four ports, each finding proved by running it, fixed in the four
   ports with a test, and the example pages it touches rendered before and
   after. By exposure, and in this order: ✅ `Page` and `TextLine`; ✅ `Image`,
   `PNGImage`, `JPGImage`, `SVG` and `SVGImage`; ✅ `Font` and its loaders
   `OTF`, `OpenTypeFont`, `FontStream1` and `FontStream2`, which no test
   names;
   ✅ the reader, `PDF.read`, the merge and split, `PDFobj` and `Decryptor`,
   which no test names; ✅ `TextFrame`, `BigTable`, `CompositeTextLine` and
   `Bidi`; ✅ the barcodes, the charts, `Form`, `Container` and `Stamp`.

3. ✅ **B** PDF/UA as it is claimed, by the Matterhorn Protocol, not only
   veraPDF, which cannot see what a paragraph stands for. PAC runs on Windows
   only, so the checks it would make by hand are in
   `.github/scripts/check-pdfua-tags.py`, which reads the structure tree of
   every tagged example: the heading levels, the descriptions of the figures
   and the links, the nesting of the lists, and the header cells and the
   widths of the rows of the tables. It runs in `check-examples.sh` and in the
   Build workflow. There are 39 PDF/UA examples, not 41: Example_30 is
   encrypted and Example_43 has its `setCompliance` commented out and stays
   that way: a tagged table of 2000+ pages is an object for every cell, which
   is 249 MB rather than 11.8 MB, and the example says so. What the first run
   found is fixed (Sep 21): not one heading was tagged as a heading in any of
   them, though 23 draw a visible title, so every title was a paragraph. What
   came after it, and what is left for this goal:
   - ✅ Done (Sep 21): a paragraph of a `TextColumn` or a `TextFrame` is one
     structure element, the contents of Example_22 and the numbered
     paragraphs of Example_03 are lists, and a tagged `BigTable` writes its
     elements with each page.
   - ✅ Done (Sep 29): PAC itself, or the Matterhorn conditions that need eyes on a page:
     the reading order of each page, whether a colour alone carries meaning,
     and whether each Alt says what its figure shows.
     Only files that embed all their fonts go to PAC: one that does not fails
     Matterhorn 31-001 for that alone (see `pdf-ua-files-to-test/README.md`).
     On Sep 28 that left out Example_04, 05, 44 and 50.
     On Sep 29 PAC found five files with Quality issues: Example_21's and
     Example_54's are fixed, Example_06's, on its annotations, are left, as
     no structure of its `Annot` elements clears them (see
     `pdf-ua-files-to-test/README.md`), and Example_42's and 45's, an email
     address in a form field that PAC takes for a link, are left too. The
     PDF/UA testing was closed that day.

4. ⬜ **B** The manual viewer pass, open since 9.0.0: Acrobat Reader on
   Windows opens Example_30 with `hello` and `world`, shows print allowed and
   copy denied, opens the Cyrillic and the 200 byte password files, and opens
   the PDF/UA and PDF/A examples without warnings. The same files in the
   engines of the viewers most people use, not only veraPDF and MuPDF:
   - PDFium: Chrome, and Google Drive and Files by Google on Android;
   - Adobe: Acrobat Reader, and Edge, which draws PDFs with Adobe's engine
     since 2023;
   - Apple PDFKit: Preview, and Safari, Files and Mail on an iPhone or iPad;
   - PDF.js: Firefox, and the many web apps that embed it;
   - Foxit: Foxit PDF Reader, dropped from the pass on 8 October 2026 (the
     owner: years without a problem, where Acrobat is the strict one).

   With MuPDF (SumatraPDF) and Poppler (Okular, Evince, pdftoppm), which
   `check-examples.sh` already renders with, these six engines draw the PDFs
   of nearly everyone; WPS Office and PDF-XChange have engines of their own,
   and are left out. The ranking is an estimate of use, not measured shares.

   The five engines we focus on (Sep 29), with MuPDF (SumatraPDF) as the
   reference the others are compared against:

   | Viewer | Engine | Covers | How it is checked |
   |---|---|---|---|
   | Chrome | PDFium | Chrome, Brave, Opera, Vivaldi, Android | automatically, in CI and `check-examples.sh` |
   | Firefox | PDF.js | Firefox | automatically (PDF.js in Node, in CI and `check-examples.sh`), and in Firefox by hand now and then |
   | Safari | PDFKit | Preview, Safari, iPhone and iPad | automatically, in CI on macOS |
   | Acrobat Reader | Adobe PDF Library | Acrobat, Edge, Adobe Document Cloud | by hand, on Windows |
   | Evince / Okular | Poppler | Linux desktop viewers, CUPS printing, command-line tools | automatically, in CI and `check-examples.sh` |

   - ✅ Automated on Sep 29: the viewers job of the Build workflow opens the
     Java examples, Example_30 with its user and its owner password, and a
     PDF with a Cyrillic and one with a 200 byte password
     (`.github/scripts/encrypted-pdfs`), in PDFium (pypdfium2), pdf.js
     (pdfjs-dist in Node) and Poppler (pdftoppm and pdftotext) on Linux and
     in PDFKit on macOS, with
     `.github/scripts/check-viewers.py`: every page opens, renders and gives
     its text; none is blank or looks different from MuPDF's render, in
     blocks of 8 pixels; and it has every character MuPDF extracts. The
     renders are uploaded, a contact sheet of each PDF beside MuPDF's, to
     look at. `check-examples.sh` runs the PDFium, pdf.js and Poppler
     checks. All four pass; PDFKit first ran on Sep 29, its largest block
     difference 2.8%, on Example_13.
   - ✅ What they found: the annotations of Example_06 and the file
     attachment of Example_30 have no appearance stream (`/AP`), which PDF
     2.0 and PDF/A ask for, so each viewer draws them its own way. PDFium,
     so Chrome, draws neither file attachment icon nor the polygon, and
     draws the note icon above its place; the `/Rect` of the file
     attachments and the note has its y values the wrong way round
     ([70 617 94 593]). Fixed on Sep 29, in the four ports: every
     annotation but a link has an appearance, in every document and not in
     PDF/A alone, the shapes in their fill color and opacity, a file as its
     push pin or paperclip icon and a note as a speech bubble, drawn with
     paths in a white box with a black frame; and every `/Rect`, links too,
     is from its lower left corner to its upper right one. PDFium draws
     Example_06 as MuPDF does, and its exception in `check-viewers.py` is
     gone; PDFKit, which drew 16.7% of the blocks of its first page
     otherwise, passes it too.
     Also: PDFium does not cut a password at 127 bytes, as ISO 32000-2 asks
     of a viewer, so Chrome opens a PDF with a longer password only with its
     first 127 bytes; not ours to fix, and said in the docs of `Passwords`
     in the four ports (Sep 29). Poppler cuts a password at 127 bytes, but its
     pdftoppm and pdftotext keep only the first 32 bytes of the one they are
     given, so the check opens the 200 byte password file in them with its
     owner password; the Cyrillic password works.
   - ⬜ Still manual: Acrobat Reader, and Edge with Adobe's engine;
     Preview itself, which the PDFKit check stands in for but does not
     click through; PAC; NVDA with Acrobat Reader on Windows, and Acrobat's
     Read Out Loud on the Mac for the reading order (Foxit and VoiceOver
     dropped, 8 October 2026); and what
     is interactive: typing the passwords, the permissions each viewer
     shows (print allowed, copy denied), the attachments, the links and the
     form fields. The CJK examples whose fonts are not embedded, Example_04
     and 44, are drawn with the fonts of each machine, so only the manual
     pass shows them as users see them.
   What the pass is for, besides finding faults: evidence for what we say
   when we sell PDF/A and PDF/UA (Sep 28).
   - PDF/A: every font and colour profile is in the file, and nothing
     outside it is needed, so a PDF looks the same in every viewer, in print
     and in 20 years. The pass shows it in each engine, as screenshots to
     keep.
   - PDF/UA: most viewers ignore the tags. A screen reader gets the
     structure mainly through Acrobat and Reader, partly through Edge's
     Adobe engine and Apple's VoiceOver. So the claim is compliance, not
     that every viewer reads a PDF aloud: the law asks for accessible
     documents (the ADA Title II rule in the US, Section 508, the European
     Accessibility Act in force since June 2025), most tools write PDFs that
     fail PDF/UA, and every PDF/UA example of ours passes veraPDF, in the
     four ports, and PAC. The pass reads one tagged example with NVDA and
     Acrobat Reader on Windows and with VoiceOver in Preview, to say which
     of them do what.
   - The wording to sell with: every PDF passes PDF/A and PDF/UA
     validation, so it looks the same in every viewer and in print, and it
     meets the accessibility law. Not "works in every screen reader", which
     is not ours to promise.

5. ✅ **B** Test the output against independent references and real files:
   the text of every example, the images against Pillow, the fonts against
   fontTools, and the reader against the pdf.js and veraPDF corpora. Done on
   Sep 21, three weeks early: all four are checks the Build workflow runs,
   and every bug they found is fixed in the four ports with tests.

6. ⬜ **B** Freeze the API and the behavior. After 9.0.2, fixes only; the
   public API is that of v9.0.1 and the members master adds to it, the
   same in the four ports, checked as for 9.0.1 before each tag:
   - `Cell.setRowSpan` and `Cell.getRowSpan`, the row spans (Sep 21);
   - `Chart`, `BarChart` and `DonutChart` `setAltDescription`,
     `Page.beginStructElement` and `endStructElement`, `Paragraph.setListLabel`
     and `setStructureType`, and `StructElem.LBODY`, for PDF/UA (Sep 21);
   - `Font.getLineGap` and `CalendarMonth.setFirstDayOfWeek`;
   - `Table.keepRowWithNext`, `setNumberOfFooterRows`, `setPageSum`,
     `setRunningSum` and `setBroughtForwardSum`, the page breaks of a table
     (Sep 22);
   - `Table.setAlternateRowColor`, of a 0xRRGGBB value and of red, green
     and blue (in Go `SetAlternateRowColorRGB`), `setHeaderRowStyle`,
     `setFooterRowStyle`, `setWidth`, `setColumnWidthsInPercent` and
     `fitToWidth`, the row styles and the column widths (Sep 22);
   - `TextFrame.drawOn(pdf, pages, pageSize)`, in Go `DrawOnPages`, a frame
     that flows onto as many pages as its text needs (Sep 22).

   Three types and twenty-one members came after those, all on Sep 22:
   - `Markup`, with its constructor of five fonts, `paragraph`, `paragraphs`,
     `setLinkColor` and `setLinkUnderline`, and `Paragraph.addJoined`;
   - `Markdown`, with its constructor of five fonts, `setHeadingFont`,
     `setMargins`, `setImageDirectory` and `drawOn`, and the structure types
     `StructElem.BLOCKQUOTE` and `StructElem.CODE`;
   - `Relationship` and its five values, `PDF.addAssociatedFile`,
     `PDF.addMetadata`, and the constructor of `EmbeddedFile` that takes the
     media type, the relationship and the description, which are the files a
     document of PDF/A-3 carries.

   One type and four members came on Sep 23, the GS1 barcodes:
   - `GS1` and `GS1.digitalLink(domain, data)`, in Go `GS1DigitalLink`, the
     GS1 Digital Link of GS1 data, for an ordinary QR code;
   - `Barcode.GS1_128` and `Barcode.ITF_14`;
   - `DataMatrix.fromGS1(str)` and `fromGS1(str, shape)`, in Go
     `NewGS1DataMatrix` and `NewGS1DataMatrixWithShape`, in Swift
     `init(gs1:_:)`, GS1 DataMatrix from GS1 data written as people read it.

   `MarkdownParser` is not public in any port, so the Markdown a document is
   written from is read one way only, through `Markdown`.

   One more value and three members came on Sep 24, decided that day to go
   into 9.0.2, which is cut from master, rather than to wait for 9.0.3:
   - `Compliance.PDF_A_3A_UA_1`, PDF/A-3a and PDF/UA-1 in one document, after
     `PDF_A_3B`, so that no other value changes; and `PDF_A_3A` deprecated
     for it (`@Deprecated`, `[Obsolete]`, `@available(*, deprecated)` and a
     `Deprecated:` comment in Go), still written as before;
   - `Barcode.setAltDescription` and `QRCode.setAltDescription`, a barcode
     read aloud as one figure;
   - `TextBlock.setStructureType`, as `TextLine` has it, for a heading.

   And one on Sep 28, for PAC, which fails a figure with no bounding box:
   - `Page.setFigureBoundingBox`, for a figure drawn with `addBDC` and
     `addEMC` around other drawing; the figures of the library set it
     themselves.

   And more that day, for PAC and for the review of the four ports:
   - `Point.setAltDescription` and `getAltDescription`, the description of
     a linked point of a chart or a cell, a figure in its Link;
   - `Table.drawOn(pdf, first, pages, pageSize)`, in Go `DrawOnPagesFrom`,
     and `BigTable.setFirstPage(page, y)`, a table that starts under a
     heading on a page of its own;
   - `DataMatrix.setAltDescription` and `PDF417.setAltDescription`, as
     `Barcode` and `QRCode` have it.

   And one more that day, in Go only, where the constructor of the other
   ports that throws has no error to return:
   - `ReadTableFromFile`, `NewTableFromFile` that returns the error of a file
     that cannot be read, or has a quoted field that is not closed, rather
     than panic with it.

   With them, two changes of behavior, which the CHANGELOG has under
   Changed: the A levels of PDF/A tag their content, as level A asks, and
   refuse a figure with no description as PDF/UA does; and what is drawn
   inside a figure is the figure, marked no further.

   Nothing is removed and no signature changes. Some differences are not
   members to list: Swift's `Alignment` has an `init(rawValue:)`, and with it
   `Hashable`, since its cases are the numbers a `Cell` packs; the
   constructors of Swift's `DataMatrix` and `EmbeddedFile` that became
   convenience ones keep their signatures; Go's `Compliance` has a `String`
   method (Sep 22), as a Go enumeration does; and Go's
   `internal/utf8text.Decode` is not importable. Java has no internal access
   across packages, so what the packages of the library share and is not API
   is in `com.pdfjet.internal`, which is left out of the Javadoc and of the
   API diff: the GS1 parser that `Barcode` and `DataMatrix` share, as Go's
   `internal/gs1` (Sep 24, before any release had it public).
   Any other difference fails the check. `./check-api.sh` lists each port's
   public declarations at the tag and at v9.0.1, of `javap -public`,
   `util/goapi` with Go's own parser, reflection over the C# assembly and the
   Swift symbol graph, and prints what is gone, changed and added; it fails
   if anything is gone. On Sep 24 nothing was, and everything added is
   listed here.

7. ⬜ **B** Guard against regressions: benchmarks recorded at 9.0.2 and 9.0.3
   against 9.0.1, Example_43's printed time among them, and the JDK 8 build,
   the packages and the docs made from the tag.

## The calendar

Four and a half weeks. The reviews and the fuzzing run first because they
change code; the checks that must hold at the tag run after the freeze.

Where it stands on Sep 21: the Sep 20-26 week is done, and with it the
review of the `Font` loaders that the Sep 27-Oct 1 week had, two of the three
blockers of the Oct 2-8 week -- the review and the fuzzing of the reader --
and, of the Oct 9-14 week, the rest of goal 2 and the fonts of goal 5. Goal 2
is closed. That buys about two weeks. They go to the work that has to be done
by hand and cannot be hurried at the end -- goal 4, the viewer pass, and goal
3, the Matterhorn conditions that need eyes on a page. Goal 5 closed the same
day.

### Sep 20–26: the decoders, `Page`, `TextLine` and the reader

- ✅ **B** Goal 1: the eight fuzz targets, in `src/*_fuzz_test.go` (Sep 19–21).
- ✅ Goal 2: the reviews of the image classes, `Page` and `TextLine`, the
      reader, `Font` and its loaders, `TextFrame`, `BigTable`,
      `CompositeTextLine` and `Bidi`, the barcodes, `Container`, the charts,
      `Form` and `Stamp` (Sep 21). Goal 2 is done.
- ✅ Goal 1: `FuzzPDFRead` fails on a Go runtime error, and drives the merge,
      the split and the stamp (Sep 21).
- ✅ What is fixed is recorded under `## Unreleased` in CHANGELOG.md as it
      lands (Sep 21).
- ✅ Goal 5, in part: the 252 fonts PDFjet ships against fontTools (Sep 21).
- ✅ True row spans, `Cell.setRowSpan` (Sep 21), and beside them the bottom
      border of a cell whose text wraps, a `BigTable` wider than its page, the
      small hexadecimal digits of Swift's strings, and a PNG, JPEG and BMP
      drawn at the size they ask for.
- ✅ Example_17 is an example rather than a test case, and a line chart
      (Sep 21).
- ✅ `Markup`, the inline markup of Markdown in a `Paragraph`, with
      `Paragraph.addJoined` beside it and Example_53 (Sep 22).
- ✅ Markdown to PDF merged from its branch, with Example_54 (Sep 22).
- ✅ The files a document carries, `PDF.addAssociatedFile` and
      `PDF.addMetadata`, which PDF/A-3 asks for and an electronic invoice is
      made of (Sep 22). See the invoices section below.

### Sep 27–Oct 1: release v9.0.2

- ✅ **B** Goal 2: the review of `Font` and its loaders. Done on Sep 21.
- ✅ **B** The release checks: `check-examples.sh` clean, the public API that
      of goal 6 in the four ports, the JDK 8 build, the benchmarks recorded
      against 9.0.1 with Example_43's time, the docs, the packages and the
      site rebuilt, the CHANGELOG entry dated. 9.0.2 is cut from master, so it
      carries the members of goal 6, those of Sep 24 among them, and the
      fixes under `## Unreleased`.
      Done on Sep 22, to be run again at the tag: the API diff, the JDK 8
      build, and the benchmarks, in `benchmarks/results/2026-09-22-*.log` and
      `benchmarks/table/results/2026-09-22-010c7c21.log`. They found `Table`
      measuring every row on every page and Java's `BigTable` reading its
      file a byte at a time, both since v9.0.1 and both fixed. Example_43 is
      as fast as at v9.0.1 with `BigTable` (1.66 s) and 8% slower with
      `Table` (3.76 s); `Table` in the four ports is 5 to 22% slower than on
      Sep 18, spread over the tagging, the fallback font of each character and
      the row spans, with no one place that costs it.
      Run again on Sep 24, after the PDF/UA and PDF/A work of that day:
      `check-examples.sh` clean in the four ports and with JDK 8,
      `./check-api.sh` with nothing of v9.0.1 gone and every addition in goal
      6, and the benchmarks in `benchmarks/results/2026-09-24-f583352f.log`
      and `benchmarks/table/results/2026-09-24-f583352f.log`: Example_43
      takes 1.65 s with `BigTable` (1.67 s at v9.0.1) and 3.61 s with `Table`
      (3.49 s, 3.5% slower, from 8% on Sep 22), and the text is as fast as at
      v9.0.1. Run again on Sep 28, after the code review of the four ports:
      `./check-api.sh` with nothing of v9.0.1 gone and the additions of that
      day added to goal 6, and the benchmarks in
      `benchmarks/results/2026-09-28-74f21e90.log` and
      `benchmarks/table/results/2026-09-28-74f21e90.log`: Example_43 takes
      1.67 s with `BigTable` and 3.62 s with `Table`, as on Sep 24, and the
      text is as fast; the 50,000-row table is as fast in the four ports,
      and Go's PDF was 4% larger with its pages at compression level 5,
      which is now for the samples of images only.
      Left for the tag: these again on the tagged tree, the docs, the
      packages, the booklet and the site rebuilt, and the CHANGELOG entry
      dated.
- ✅ **B** Tag v9.0.2 on Oct 1 and make the GitHub release. Done three days
  early, on Sep 28: the checks of Sep 28 above, `check-examples.sh` clean at
  the tag, the API references regenerated into `docs/` for the site, and the
  release on GitHub with a summary of the CHANGELOG entry. The packages of
  `.packaging/` are the tag's: checked on Sep 30, their CHANGELOG, examples
  and Producer are those of the tagged commit. Left: the site, rebuilt from
  the tag (on the owner's go-ahead).

### Oct 2–8: the reader

- ✅ The Oct 8 decision on the `markdown` branch, made early: merged on
      Sep 22. See the Markdown section below.
- ✅ **B** Goal 2: the review of the reader. Done on Sep 21.
- ✅ **B** Goal 1: fuzz `PDF.read`. Done on Sep 21.
- ✅ **B** Goal 5: the reader against the pdf.js and veraPDF corpora, and the
      PDFs pdf.js links to, in the four ports (Sep 21). Every port reads the
      4,300-odd files with no failure, and the ones that differ by design are
      in `tests/corpus/known-differences.txt`.

### Oct 9–14: the rest of the review, and the references

- ✅ **B** Goal 2: the rest of the review. Done on Sep 21.
- ✅ **B** Goal 5: round-trip text, images against Pillow and fonts against
      fontTools, three checks the Build workflow runs (Sep 21).
- ✅ **B** Goal 3: PAC or Matterhorn over the 41 PDF/UA examples, and the
      fixes it asks for — the last day a tagging fix can land before the
      freeze. Done on Sep 29.

### Oct 15: code freeze

- ⬜ **B** Goal 6: fixes only from here, each with its check. Whatever is
      still open moves to 9.0.4 or the AGPL major rather than into the tag.

### Oct 15–20: the checks that must hold at the tag

- ⬜ **B** Since the checks of Sep 30 (6bd05dfe), the library changed: on Oct
      1, `/Length 256` in the encryption dictionary, in the four ports
      (fb43c9b6), as Acrobat could not decrypt PDFjet's AES-256 PDFs without
      it. Run the checks of the tag again on the frozen master, and build
      the viewer files again: `viewer-files-to-test/Example_30.pdf` of Sep
      30 has not the fix. Checked in Acrobat Reader on Oct 1: Example_30
      with the passwords hello and world, and without an open password.
- ⬜ **B** Goal 4: the manual viewer pass, on the files built from the frozen
      master, in Acrobat Reader, Preview, Chrome, Firefox and Edge. The
      engines of Chrome, Firefox, Preview and the Linux viewers are checked
      by the Build workflow since Sep 29; see goal 4 for what is left.
- ⬜ **B** Goal 7: benchmarks at 9.0.3 against 9.0.2 and 9.0.1, Example_43's
      printed time among them; the JDK 8 build; the packages and the docs made
      from the tag.
- ⬜ **B** `check-examples.sh` clean in the four ports, and the public API
      still that of goal 6.
- ⬜ **B** `go test ./...` of pdfjet-server passes against the library to be
      tagged, as its `go.work`, which uses `../pdfjet`, builds it. It reads images back
      out of the PDFs PDFjet writes, which `check-examples.sh` does not: the
      PNG pass-through of Sep 28 (99486dea) broke five of its tests and went
      into v9.0.2 unseen, worked around the same day in pdfjet-server
      (dee3909).
- ⬜ Rebuild the site, and date the `## v9.0.3` entry of CHANGELOG.md.
- Keep Oct 19 and 20 empty: they are the buffer for what the checks find.

A trial run of these checks on Sep 29, at 7aa67a7a: `check-examples.sh`
clean in the four ports, the viewers and the booklet; `./check-api.sh
v9.0.2` with nothing gone, changed or added in any port; `go test ./...`
of pdfjet-server passing against master; and the benchmarks, in
`benchmarks/results/2026-09-29-e784a127.log` and
`benchmarks/table/results/2026-09-29-e784a127.log`, as on Sep 28 within the
spread of the runs: Example_43 takes 1.65 s with `BigTable` and 3.69 s
with `Table`, the text is as fast, and the 50,000-row table is as fast in
the four ports, Go's PDF 4% smaller.
Again on Sep 30, at 6bd05dfe, after the 9.0.3 Producer: `check-examples.sh`
clean in the four ports, the viewers and the booklet, in 10 minutes;
`./check-api.sh v9.0.2` with nothing gone, changed or added in any port;
and `go test ./...` of pdfjet-server passing. The files of the viewer pass
were built from that tree into `viewer-files-to-test`, for a dry run.

### Oct 21: release v9.0.3

- ⬜ **B** Go users cannot `go get` the library: proxy.golang.org lists v9.0.0,
      v9.0.1 and v9.0.2 but serves none of them (`.info` answers "not found:
      fetch timed out"), and sum.golang.org has no checksum for any, so `go
      get github.com/edragoev1/pdfjet/v9@v9.0.2` fails its checksum check
      with the default settings, for every user since v9.0.0, and so does a
      module that requires pdfjet-commercial. Found by the rehearsal of Sep 28.
      The cause, found on Sep 28: the proxy fetches only the tag, shallow
      (about 470 MB), then `git archive`s the whole tree at it, a zip of 487
      MiB, most of it `fonts/` (604 MiB uncompressed), before it drops the
      nested modules; the
      work passes the proxy's limit of about 56 s a request, and the failure
      is cached for 30 minutes. The history is not the problem, and the raw
      archive is 13 MiB under Go's 500 MiB cap, so `GOPROXY=direct` breaks too
      once the tree grows. The module zip is 4 MB. Until it is solved, users
      need `GONOSUMDB=github.com/edragoev1/pdfjet` (or `GOPRIVATE`), and
      `GOPROXY=direct` clones the whole repository. To decide: ask the Go team
      (golang/go issues), shrink what the proxy clones, or give the Go module a
      small repository of its own, which changes its import path unless a
      vanity path is set up first. Check the proxy again after each tag:
      `curl https://proxy.golang.org/github.com/edragoev1/pdfjet/v9/@v/v9.0.3.info`.
      Done on Sep 29: `fonts/` and `data/` moved to the public repositories
      edragoev1/pdfjet-fonts and edragoev1/pdfjet-data (`fonts/go.mod` and
      `data/go.mod` are gone; `images/` keeps its `go.mod`), with no history
      rewritten, and came back at the same paths as folders that git ignores
      and `get-fonts-and-data.sh` (and `.cmd`) fetches at the commits
      `fonts-and-data.txt` pins, the pinned commit alone; the build, run and
      test scripts run it. They were submodules for a day, but SwiftPM checks
      out the submodules of a package it depends on, so a Swift user
      downloaded about 1.3 GB instead of 0.8 GB; a fetched folder keeps them
      out of both. The zip of the tree is 8 MB and the module zip, made with
      golang.org/x/mod/zip from the tree, 4 MB (12 MB, 1520 files,
      uncompressed). The packaging scripts, `check-api.sh`, the workflows, the
      README and the booklet follow; a fresh `git clone` passes `test-go.sh`,
      which fetches the folders. Left: tag v9.0.3
      and check that the proxy answers 200 for its `.info` and `.zip`, and that
      sum.golang.org has its checksum. go.mod retracts v9.0.0 to v9.0.2
      (Sep 29, the owner's decision), so `go get @latest` skips the versions
      the proxy cannot serve once v9.0.3 is out.
      Checked again on Sep 29, evening: the proxy still times out on v9.0.0
      to v9.0.2, and on the untagged master too, whose pseudo-version needs
      the whole history, about 900 MB. A tag needs only its commit: fetched
      alone, v9.0.2 is 471 MB (15 s from here) and master 7.7 MB (1 s), so a
      tag on master should pass. Tagging early for the Go users was weighed
      and left for Oct 21 (the owner's decision): a tag is for good once the
      proxy has it. The tree says 9.0.3 since Sep 30: the Producer strings
      of the four ports, the ReviewWriter tests that check them and the
      DataMatrix text of Example_14 in the four. Left for the tag: the
      `exact:` pin of `.commercial/Package.swift`, and its go.mod.
- ⬜ **B** Tag v9.0.3 and make the GitHub release.
- ⬜ **B** Release v9.0.3 of the commercial product, built on the tag of the
      library, the same day: its steps are in TODO.md of its own repository.

### After the tag: the package registries (1 October 2026)

None changes the library's code; each is packaging around the tag. Free,
and the names are free: no `PDFjet` on NuGet, no `com.pdfjet` on Maven
Central (checked Oct 1). GitHub counts no downloads of a repository, only
of the files attached to a release, and its traffic shows 14 days and keeps
no history; the registries count, and are where developers look.

- ⬜ **Go**, at the tag: nothing to register. Fetch
      `https://proxy.golang.org/github.com/edragoev1/pdfjet/v9/@v/v9.0.3.info`
      once, as above, and pkg.go.dev lists the module within minutes, with
      its docs, its license and who imports it; no download counts.
- ⬜ **NuGet**, first, as C# is the largest audience of the library: an
      account at nuget.org and an API key; `PDFjet.csproj` given its package
      metadata (PackageId `PDFjet`, the version, the license expression
      `MIT`, the README, an icon); `dotnet pack` and `dotnet nuget push`.
      About an hour; its download counts are public. Ask NuGet to reserve
      the prefix `PDFjet.*`, so that only we publish packages of that name.
- ⬜ **Swift Package Index**: the repository's URL added by its form or a
      pull request; it shows the docs and the platforms, no downloads.
- ⬜ **Maven Central**, after the others: an account at central.sonatype.com;
      the namespace `com.pdfjet` proved by a TXT record in pdfjet.com's DNS at
      IONOS, which can take a day to be seen; a GPG key, published to a
      keyserver and kept for good with the other secrets. Each release is a
      bundle of the jar, a sources jar, a javadoc jar and a POM (name,
      description, URL, license, developers, SCM), each signed and with its
      checksums, uploaded by the Portal's API. The Java port builds with
      scripts, not Maven or Gradle, so a script makes the bundle; javadoc
      that fails its checks is the likeliest work. Half a day to a day the
      first time, one script after.

## Markdown to PDF — merged

- ✅ A practical subset of Markdown to PDF, in the four ports: headings,
  paragraphs with the inline markup of `Markup`, bullet and numbered lists
  that nest, block quotes, fenced and indented code, thematic breaks, GitHub
  tables and images, flowed down the pages and tagged for PDF/UA. Written on
  the branch `markdown` and merged into master on **Sep 22**, sixteen days
  before the Oct 8 date it was given, with its tests, its fuzz target,
  Example_54 and a booklet snippet in each port. The branch is deleted.
  - It is called Markdown, not CommonMark: CommonMark's 652 examples are the
    goal of v9.1 below.
  - The merge holds the checks that hold at the tag: the unit tests of the
    four ports, and `check-examples.sh` over the 54 examples and the 59
    snippet PDFs.

## Found by pdfjet-server, to fix later

- ✅ Done on 5 October 2026, before the tag, on the owner's word ("correct
  is more important than fast"): the Go port's results on arm64 the same as
  amd64's and the other ports'. The Go spec lets the compiler fuse x*y + z
  into one fused multiply-add, rounding once, and Go's arm64 compiler did,
  in 333 places of the library (TextBlock.layout, where lines break, and
  Cell, where rows are measured, among them). Every float multiplication of
  the Go port, and every division by a constant, which the compiler makes a
  multiplication, is written in a conversion to its own type,
  float32(a*b), by a small tool that read the types of the source; the
  examples, read by people, left as they were. Checked: no fused
  multiply-add left in the arm64 code of the 57 packages; on amd64 the
  PDFs of the 57 examples the same byte for byte, the dates and the ids
  aside, and Example_30, encrypted with a random salt, the same text; all
  the Go tests; and, through pdfjet-server, its 151 forms of the look test,
  check-layout.sh's 385 layouts and the other checks of the client.
  check-no-fma.sh, in the Build workflow, fails if one comes back; it reads
  the arm64 code the compiler prints, so needs no ARM runner. Left: Go's
  own math package (math.Sin, math.Cos and the like) is fused on arm64 too,
  in Go's code; PDFjet uses it for arcs, ellipses and SVG alone, where a
  last bit changes no line or page.
- ✅ **A glyph two characters share is copied as one of them.** Source Serif 4
  draws the Greek small letter mu (U+03BC) and the micro sign (U+00B5) with the
  same glyph, and the ToUnicode map of the embedded font gives that glyph one of
  them, the micro sign: Greek text drawn in it, "δικαιώματα", is right on the
  page, but is copied, searched and read aloud with µ in place of μ. The map
  of a glyph should be the character it was drawn for, as an ActualText span
  or a glyph of its own in the map would give it. Found by comparing the PDFs
  of Latin, Greek and Cyrillic text in the four families of the editor, on 23
  September 2026; IBM Plex Sans, JetBrains Mono and Noto Sans read back as
  drawn. Fixed the same day in the four ports: the micro sign, and the ohm,
  kelvin and angstrom signs, are among the characters text seldom has, so a
  glyph they share with a letter is copied as the letter.

- ✅ **Code 128 cuts a long text short without a word.** `drawCode128` stops
  at 48 codewords and draws the rest of the barcode, so the barcode holds
  another value than its text: 60 sevens draw exactly as wide as 48. It
  should refuse the text, or say so, as `NewQRCode` does for a text too long.
  pdfjet-server refuses such a text before it is drawn (`code128Codewords`
  in document.go). Found Sep 23; fixed Sep 23 in the four ports: the
  constructor refuses it.
- ✅ **Code 128 panics on a character above U+00FF.** `drawCode128` appends
  codeword 256, "This will generate an exception", and indexes the table
  with it: "A€" is `index out of range [256] with length 107`. It should
  refuse the text with a clear error. pdfjet-server refuses it first. Found
  Sep 23; fixed Sep 23 in the four ports: the constructor refuses it.
- ✅ **A barcode is drawn in whatever pen color the page has.** `Barcode`
  sets the pen width of each bar but never its color, and does not save and
  restore the graphics state, so after `Page.SetPenColor`, which an ellipse or
  a path drawn with the methods of `Page` leaves set, the bars come out in
  that color. It should draw in a color of its own, black unless set, inside
  q and Q. pdfjet-server sets the pen to black before each barcode. Found
  Sep 23; fixed Sep 23 in the four ports: the bars are black, in q and Q.

- ✅ **The guard bars of EAN-13 and UPC-A are 8 points longer, not 5
  modules.** The GS1 General Specifications have the guard bars extend 5X
  below the other bars, so the extension follows the module length;
  `drawEGuard` and `drawMGuard` are drawn `h+8`, 8 points whatever the
  module, which is about 10.7 modules at the default of 0.75, 8 at 1 and 4
  at 2. Barcodes scan either way; they look unlike the standard's. Found Sep
  23, while porting the drawing to pdfjet-client, whose preview draws them
  as PDFjet does until it changes. Fixed Sep 24 in the four ports, and in
  pdfjet-client's preview.
- ✅ **UPC-A does not extend the bars of its first and last digits.** In a
  UPC-A the bars of the number system digit and of the check digit are as
  long as the guard bars, which is why the two digits are printed outside
  the bars; `drawCodeUPC` draws every digit at `h`. Found Sep 23, with the
  item above. Fixed Sep 24 in the four ports, and in pdfjet-client's preview.

- ✅ **The lines of a table's cells are drawn in whatever pen color the page
  has.** A cell's border color is transparent until `SetBorderColor` or the
  table's `SetCellBorderColor` sets one, and `drawBorders` then sets no pen
  color at all, so after `Page.SetPenColor` the lines of the table come out
  in that color, as the bars of a barcode did (fixed Sep 23). A cell should
  draw its lines black unless a color is set, in a graphics state of its
  own. pdfjet-server sets them black with `SetCellBorderColor`. Found Sep 23,
  while adding tables to the editor. Fixed Sep 24 in the four ports: a cell
  with no color set draws its lines black. Not in a graphics state of its
  own: the pen color is written only when it changes, so black costs one
  operator a page, where q and Q would cost two for every cell. pdfjet-server
  no longer sets it.

- ✅ **A wrapped cell line can keep a trailing space, which moves right and
  centered text.** When a word wider than the column follows other text and
  its first character does not fit, `wrapCellText` pushes the line with the
  space after it; the rows below are wrapped again and lose theirs, but the
  first line keeps it, and the space counts in the width of the text. A
  table of columns 30% right, 30% center and 40% at a width of 100, each
  cell "abcd Wxyzwxyzw" in IBM Plex Sans at 10: the first line is "abcd ",
  drawn 2.36 points too far left in the right column and 1.19 in the center
  one. Found Sep 23, porting Table to pdfjet-client, whose check has it as a
  case. Fixed Sep 24 in the four ports, and in pdfjet-client's preview.
- ✅ **Not a fault: the lines of a wrapped cell are evenly spaced.** It was
  noted that the first row of a wrapped cell keeps its top and bottom padding
  and each row added below it drops only its top padding, so the rows are 17
  and 15 points tall in IBM Plex Sans at 10. The lines of text are 15 points
  apart all the way down, which is what matters: the text of a row starts
  under its top padding, and the extra 2 points of the first row are the top
  padding above the first line alone. Measured in the Go port on Sep 24.
- ✅ **Column percentages that are all 0 leave the columns 75 points wide.**
  `applyColumnPercents` returns when their total is not above 0, so
  `SetWidth` is ignored and each column keeps the width of a new cell: two
  columns at 0% and a width of 300 make a table 150 wide. It should share
  the width equally, as for columns with no percentage. Found Sep 23, with
  the items above. Fixed Sep 24 in the four ports, and in pdfjet-client's
  preview.
- ✅ **A corner radius over half the rect is drawn as crossed loops.**
  `Rect` draws each side from the radius in from one corner to the radius in
  from the next, and does not cap the radius, so a radius over half the
  width or the height turns those sides back on themselves, and the curves
  of the corners cross: a rect 20 high with a radius of 30 has loops at its
  ends. It should cap the radius at half the shorter side, as SVG caps rx
  and ry, which is what a pill shape is drawn with. pdfjet-server caps it
  before it draws (`cornerRadius` in document.go). Found Sep 24, writing the
  help of the corner radius of a box in the editor. Fixed Sep 24 in the four
  ports; pdfjet-server no longer caps it.
- ✅ **The header row has a line under it without borders.**
  `drawHeaderRows` sets the bottom border of the last header row whatever
  `SetCellBorders` says, so a table without lines still has one under its
  header. Decided Sep 24 that it is a fault: the line is drawn only under the
  header cells that have borders. Fixed in the four ports, and in
  pdfjet-client's preview. Found Sep 23.

- ✅ **No level is PDF/A and PDF/UA at once.** A document is one or the
  other, `PDF_UA_1` or a PDF/A level, where the two are made to go together,
  and PDFjet Forms writes documents that are to be archived and read aloud.
  Added Sep 24: `Compliance.PDF_A_3A_UA_1` in the four ports.
- ✅ **The A levels of PDF/A tag no content.** `PDF_A_1A`, `PDF_A_2A` and
  `PDF_A_3A` write a structure tree, but the marked content of the text and
  the images is written only for PDF/UA (`isUA` in the four ports), so a
  document of an A level has no tagged content, which the A of PDF/A asks
  for, and the booklet says the A levels are tagged as PDF/UA is. veraPDF's
  PDF/A profiles pass them all the same. `PDF_A_3A_UA_1` is tagged. To
  decide for v9.0.3: tag the A levels as PDF/UA is, which asks each figure
  of them for a description, as PDF/UA does, and so refuses documents that
  are written today; or tag them without that rule; or say in the booklet
  that they are not tagged. Found Sep 24, adding `PDF_A_3A_UA_1`. Decided and
  fixed the same day: the A levels are tagged as PDF/UA is, rules and all,
  in the four ports, which may refuse a document that is written today, as
  CHANGELOG.md says; and `PDF_A_3A` is deprecated, since `PDF_A_3A_UA_1` is
  the same document that says it is PDF/UA-1 too. The PDF/UA examples, of
  PDF/A-1a, 2a and 3a, pass veraPDF's profiles of those levels but for what
  each of PDF/A forbids, such as transparency in PDF/A-1.

- ✅ **A long word takes minutes to break across lines.** `appendBrokenWordLines`
  in textblock.go measured the whole rest of the word for every line it
  broke it over, and copied the word up to each break to measure it, so the
  time grew with the square of the word: a word of 20,000 characters in a
  full-width block took 1.6 s, 100,000 took 39 s, and 2 million in a narrow
  column had not finished after 5 minutes. Normal text was fast, 2 MB of
  words in 0.4 s. Found by a review of pdfjet-server on 25 September 2026,
  and fixed the same day in the four ports: each line is found by measuring
  the word from the line's start to each character break until one does not
  fit, and the rest of the word is measured only when the breaks run out, so
  a word is measured about once a line's worth of characters a line; the word
  of 100,000 characters breaks in 0.02 s. The lines are the same as before,
  checked in Go against the old code on Latin, combining marks, Thai and
  Arabic, and in the four ports by the height of the block. A right-to-left
  word was still reordered and shaped up to each break, 27 s for 16,000
  Arabic letters and about 20 minutes for 100,000; it is reordered and shaped
  once, whole, and each line is a part of it, measured as the sum of the
  widths of its characters, so the letter a line ends on keeps the form it
  has in the word. pdfjet-server keeps its limit of 1000 characters a word
  (`checkText`, layout.go).

- ✅ **Reading a PDF decodes every stream in full, with no total.** `Read` in
  pdf.go inflated every Flate stream as it read, capped at 256 MB each by
  `decompressor.MaxDecodedLength` and not at all in total: a PDF of 8 MB, 40
  streams of 203 KB that each inflate to 200 MB, took 32 GB to read. Found by
  the same review, and fixed the same day in the four ports, in two parts.
  A stream that is not a cross-reference or an object stream is decoded when
  its data is first asked for, `GetData`, not when the PDF is read, so that
  reading a PDF of large images takes the memory of none of them; a stream
  that cannot be decoded has no data, as before. And all the streams of one
  PDF may decode to 256 MiB together (`maxDecodedTotal`, a budget the objects
  of one `Read` share): an object stream or a cross-reference stream past it
  is the error "the streams of the PDF decode to more than N bytes together",
  as the PDF cannot be read without its objects, and any other stream past
  it has no data. pdfjet-server keeps its checks before reading, the name of
  the layout in the PDF and 20 MB at most.

- ✅ **An image is decoded whole to give its size.** `NewImage` decodes the
  pixels of a PNG or a JPEG to know its width and height, 256 MB for a PNG of
  16,000 by 16,000, which pdfjet-server did three or four times a request.
  Found by the same review. Measured on Sep 28, after the IDAT of a PNG is
  embedded as it is: a JPEG was never decoded, its header is read to the
  frame header, 0.2 ms for 36 megapixels; an opaque PNG is inflated once, to
  check the filter type of each row and that the data is the rows and nothing
  more, 100 ms for 36 megapixels of RGB; a PNG with alpha is decoded, as its
  alpha is a soft mask of its own, 670 ms. That is what embedding the image
  takes, so the library is not changed, and a size-only reader is new API,
  a maybe for v9.1. Fixed in pdfjet-server the same day: a request reads
  each image once for its size and its fingerprint, and once more to draw it,
  two reads where it made three, or four for a form (`imageReads`,
  xobject.go); a form with a PNG of 36 megapixels takes 0.36 s in place of
  0.61 s, and 1.75 s in place of 3.25 s with alpha. It still refuses an image
  of more than 40 megapixels from its header (`checkPixels`, layout.go).

- ✅ **`PDFobj.GetData` of an image leaves the PNG predictor in place, and an
  /Indexed image in its indexes.** `applyDecodeParms` in pdfobj.go skips a
  stream of `/Subtype /Image`, "as they are copied with their stream, and
  their data is not used", so the data of an image with `/DecodeParms
  <</Predictor 15 ...>>` is its rows each with the filter type and the
  filter of its PNG row, and of an /Indexed image its packed indexes. Since
  99486dea embeds the IDAT data of an opaque PNG as it is, and a palette PNG
  as /Indexed on RGB, that is the data of every such image PDFjet writes:
  pdfjet-server's fingerprint of an image, and the PNG it makes again from
  it, read 32 bytes more for a 32 by 32 gray image, and the fingerprints of
  forms signed before no longer matched. Found Sep 28, by five tests of
  pdfjet-server. Worked around the same day in pdfjet-server, which undoes
  the predictor and expands the palette to RGB of eight bits itself
  (`imageSamples`, xobject.go), and gets the fingerprints of before for
  every PNG of PngSuite. The predictor is fixed, the same day, in the four
  ports, with a test in each: `GetData` of an image undoes it as it does for
  any other stream, the PNG predictors and the TIFF one. It was skipped to
  save the time of undoing it on read, when every stream was decoded as the
  PDF was read; a stream is decoded now when its data is asked for, and
  nothing in the library asks for the data of an image: `Image` from a
  `PDFobj`, `merge`, `addObjects` and `addResourceObjects` copy its stream
  and its `/DecodeParms` as they are. pdfjet-server no longer undoes the
  predictor. The samples of an /Indexed image in its colors are not what
  the stream is, so they are not `GetData`'s to give: that is new API, and
  waits for v9.1, below.

- ✅ **`Color.transparent`, -1, was drawn white by most color setters;
  fixed for v9.0.3, in the four ports, on Oct 5, 2026, on the owner's word.**
  A color is an int, 0xRRGGBB, and the setters kept its low 24 bits, so -1,
  the value of `Color.transparent`, was 0xFFFFFF: `Line.SetStrokeColor(-1)`
  drew a white line. Of the 36 int-color setters, 13 left the color as it
  was for it, as their comments said (TextLine, TextBlock, Cell, BigTable,
  Table's alternate row, Rect's border, TextFrame); the other 23 drew it
  white: Line, Path, Arc, Point and Series strokes, Rect, Arc, Point, Stamp
  and annotation fills, Page's pen and brush, Paragraph's text, Form's
  label and value, Markup's links, CheckBox, Container and Table cell
  borders, and the grid lines of Chart and BarChart. Each of the 23 now
  leaves the color as it was, says so in its comment, and a test in each
  port sets it red, then transparent, and finds it red (22 of the 23 failed
  before; Paragraph's passed, as TextLine already ignored it). Any other
  value keeps its low 24 bits, as documented: 0xFFRRGGBB, a negative int
  in Java and C#, draws as before. No caller in the library passed -1 to
  them; pdfjet-server tests for transparent before each call. Found by the
  review of Sep 30, 2026.

- ⬜ **The four ports read SVG with four XML parsers, which may take
  different files; for v9.1, in the four.** Go reads it with encoding/xml,
  Java with StAX, C# with XmlReader (its DTD ignored) and Swift with a
  reader of its own, so a file one port draws another may refuse. What Go
  refuses, proved by pdfjet-client's `check-svg.sh` (89 cases in its
  `tests/svg/cases.json`), where the editor follows it: XML of version 1.1;
  an encoding declared other than UTF-8, "ISO-8859-1" and the like, as the
  decoder has no CharsetReader; a processing instruction with no target; a
  name of an element or an attribute that starts with a digit, or has a
  character a name may not; bytes that are not UTF-8, but in comments and
  processing instructions, which it reads past; an entity XML does not
  define, `&nbsp;`; and the entities of a DTD, as older files of Illustrator
  have them, `<!ENTITY ns_svg "http://www.w3.org/2000/svg">`. To do, in
  the four, with a test:
  - **Decided on Sep 30, 2026: one parser for SVG and for the invoices.**
    The XML parser of ZUGFeRD and Factur-X (`XMLParser` of the invoices,
    written alike in the four ports, hardened and fuzzed: no DOCTYPE, the
    five entities of XML and the numeric ones alone, a depth bounded without
    recursion, 20 MB at most) moves into the MIT core, and SVGImage reads
    with it in the four, in place of encoding/xml, StAX, XmlReader and
    Swift's own reader; the invoices then use it from the core. Released
    under MIT, as it is a plain XML parser: what is paid for is the
    invoices, not it.
  - The cases of `tests/svg/cases.json` read by each port's tests, and what
    each takes and refuses compared, as the fixtures of all four; with them
    the fuzz tests of the parser, and of SVGImage through it.
  - Its behavior written down once, the refusals of the list above among
    it, in words alike in the four.
  - To weigh after, as real files have them: an encoding declared of
    Latin-1 or Windows-1252, and the internal entities of older Illustrator
    files, which a parser that refuses a DOCTYPE refuses; if taken, text
    entities alone, no external ones, and their expansion bounded, so that
    no file of nested entities runs the reader out of memory.
  - pdfjet-server's `svg_test.go` and pdfjet-client's `SVGImage.ts`, which
    follows Go, then follow the parser, and `check-svg.sh` says where they
    differ.
  pdfjet-server draws with the Go port, and the editor refuses what it
  refuses, so nothing is to work around meanwhile. Found by the review of
  Sep 30, 2026.

- ⬜ Maybe: **check once that positions are the same on arm64.** Go's
  compiler fuses a float32 multiply and add into one instruction on arm64
  (FMADDS, FMSUBS), rounding once where amd64 rounds twice, so a position
  could differ in its last bit, about one part in ten million, and a
  comparison exactly at a boundary, as of a line that just fits, come out
  the other way. Seen in the arm64 assembly of `TextBlock.layout` and
  `Cell.GetHeight`; no difference seen on a page. Not a fix: run the
  examples' comparison on arm64 once (a GitHub arm64 runner); only if a
  PDF differs, an explicit `float32(...)` round each product, which the Go
  spec says keeps it from being fused. pdfjet-server runs on Lambda's
  arm64 and checks its own the same way (its TODO.md). Found by the review
  of PDFjet Forms, 5 October 2026.

- ✅ **A JPEG cut short is taken.** `NewImage` read a JPEG's header for its
  size and embedded its bytes as they were, so a file cut short in its
  upload was drawn as far as it went, or as noise, and veraPDF, which does
  not decode the image, passed the PDF. Fixed for v9.0.3 on 5 October 2026,
  in the four ports, with a test in each: the end-of-image marker must
  follow the header of the scan (a thumbnail's own before the scan does not
  count, and data after the end is left), else "Error: The JPEG is cut
  short: its image data has no end." Found by the review of PDFjet Forms.

- ⬜ **A cell's wrapped lines are read as paragraphs of their own** (the
  second part, the break, fixed for v9.0.3 on 5 October 2026, below).
  `wrapCellText` (table.go) makes each line of a cell that wraps a P of its
  own in the structure tree, so a screen reader reads one cell as several
  paragraphs; and a word wider than its column is broken where it reaches
  the edge, even where a space follows soon after
  ("Superc / alifragi / listic e / xpialid / ocious"). veraPDF and PAC do
  not flag it. The cell one P, its lines the content of it, and a line
  broken at a space where there is one, in the four ports, with a test.
  The second, seen by everyone, was fixed for v9.0.3 on 5 October 2026, in
  the four ports and pdfjet-client's SVGTable.ts, before PDFjet Forms
  launches, whose look of a form is kept after: a word wider than its
  column starts a line of its own. Left, for v9.1: the cell one P. Found by
  the review of PDFjet Forms, 5 October 2026.

- **TextBlock breaks long words between letters, never after a hyphen or at a
  soft hyphen.** Found on 6 October 2026, when the editor's line breaking was
  checked against the Go port's in Spanish, German, French, Italian, Greek and
  Bulgarian (pdfjet-client, tests/textblock/cases.json, the cases named
  "languages:"). What is right: a line never breaks at a no-break space or a
  narrow no-break space, so French `questions ?` and `1 250 €` stay whole;
  Greek and Cyrillic break at their spaces; a soft hyphen (U+00AD) is not
  drawn. What could be better, in all four ports and in the editor's
  SVGTextBlock, which follows the Go:
  - A word wider than the line is cut between two letters, with no hyphen:
    `Datenschutzgru` / `ndverordnung` in a narrow column.
  - A line does not break after a hyphen: `Bindestrich-Wör` / `ter.`, where
    `Bindestrich-` / `Wörter.` reads better.
  - A soft hyphen is not a place to break: `Rechts\u00adschutz\u00adversicherung`
    is cut between letters, where `Rechtsschutz-` / `versicherung`, its hyphen
    drawn at the break alone, is what the soft hyphen is for.
  Seen only in narrow columns with long words, as German has. Not for 9.0.3:
  it changes where lines break in every port and in the editor, and so how a
  form already made looks (pdfjet-server's TestLookOfRules1IsKept); for v9.1,
  the owner's call, Go first, then the other ports and SVGTextBlock, the cases
  above to show the new breaks.

## v9.1 — features, after v9.0.3

- ⬜ Maybe: **subsets of the fonts added to an existing PDF** (the owner, 9
  October 2026). `new Font(objects, stream)` embeds a font whole, as its
  objects are written at once, before any text is drawn with it: a whole
  `.ttf` is 2 to 3 times its CFF, so Examples 37 and 50 add IBM Plex Sans's
  `.otf` (127 KB and 177 KB; 172 KB and 264 KB with the `.ttf`). How: the
  font's objects made as now, but the font file's `PDFobj` left without its
  stream; the glyphs recorded through `Page.appendGlyph` as on a new page
  (the page of an existing PDF draws through it already); the stream, the
  subset, its `/W` and its ToUnicode map filled in by `pdf.addObjects(objects)`,
  which writes the objects after every page is drawn, with the subsetter of
  9.0.5 (`Subset.subsetTrueType`), in the four ports. A form filled in with a
  few words in a CJK font is the case it is for: a whole Noto Sans SC is
  6.5 MB.

- ⬜ CommonMark itself, in the four ports, where v9.0.3 has the practical
  subset above: a parser written from the spec, with the GitHub tables,
  strikethrough and task lists. The renderer is written and tagged for
  PDF/UA already, so what is left is the parser and the 652 examples of the
  spec.
  - The parser is about 2,500 to 3,000 lines a port, 1,200 to 1,500 of them
    for the blocks and as many for the inlines; the GitHub extensions add
    800 to 1,200, the renderer 800 to 1,500, and the table of the 2,125
    named entities is data, generated once for the four ports.
  - The check is the 652 examples of the CommonMark spec, from its JSON
    file, run in every port, as the examples are compared across the ports.
  - The hard parts: the emphasis algorithm, with its flanking rules and
    the rule of 3; lazy continuation lines in block quotes and lists; tight
    and loose lists; the seven kinds of HTML block; and link labels matched
    with Unicode case folding.
  - About a week for the first port to pass the spec, and a few days for
    each of the others; the renderer is done.
- ✅ Done for 9.0.5 (9cc3afee, 8 October 2026). Maybe: the EXIF orientation of a JPEG, in the four ports. A photo
  taken with a phone is stored as the sensor saw it, with a tag that says
  how to turn it, and is drawn sideways or upside down. The orientation is
  read from the APP1 segment, and the image is drawn turned or mirrored with
  its matrix, its width and height swapped for a quarter turn; the samples
  are not changed. A medium feature, with test images of the eight
  orientations; found by the code review of Sep 28.
- ✅ Done on 5 October 2026, ahead of the one XML parser, on the owner's
  word: `viewbox` read when there is no `viewBox`, in the four ports and
  SVGImage.ts, a test in each, and two cases of check-svg.sh. As it was:
  An SVG's `viewbox` in lower case, in the four ports and pdfjet-client's
  SVGImage.ts at once, as check-svg.sh compares them. SVGImage reads only
  `viewBox`, as XML has attribute names case-sensitive, so a file that writes
  `viewbox` is drawn with no viewBox at all, silently: the size of its width
  and height, its paths neither scaled nor moved. Such files are common, as
  an HTML page's parser takes `viewbox` for `viewBox` in an inline svg, and
  files saved from web pages keep it. Found in Example_33's europe.svg, whose
  `viewbox="0 0 1000 684"` was ignored and whose map stood off the middle of
  the page; the file was fixed instead, with `viewBox` (98d13f70, 4 October
  2026). The fix: read `viewbox` when there is no `viewBox`, as the HTML
  parser does, and `viewBox` first when a file has both; a test of each, with
  a viewBox that scales and moves the paths. Small. Done in the same pass as
  the one XML parser for SVG, above, after the switch: the parser keeps
  `viewbox` and `viewBox` apart, as XML's names are case-sensitive, so the
  fix is in SVGImage, and `check-svg.sh` checks both at once.
- ⬜ Maybe: smaller tagged tables, with object streams, in the four ports. A
  cell of a table of PDF/UA costs 280 to 415 bytes in the PDF, its text and a
  structure element of its own, each an object written uncompressed: a price
  list of 3,000 rows of 6 columns is 4.7 MB, past the 4 MB PDFjet Forms can
  send, with no image in it (measured by pdfjet-server on 5 October 2026).
  PDF 1.5's object streams, the structure elements compressed together, and
  a cross-reference stream would shrink them several times. Found by the
  review of PDFjet Forms; pdfjet-server meanwhile
  refuses such a table before it is drawn, and says to make it shorter.
  **The plan, v9.1 (talked over with the owner, 8 October 2026):** an
  option, on by default, off for PDF/A-1, which forbids object streams
  (PDF/A-2 and 3 allow them). The small objects (pages, annotations, the
  structure elements) written to an object stream a batch of 100 to 200 at
  a time, so that memory stays flat as PDFjet writes as it goes; the
  cross-reference table becomes a cross-reference stream. Speed expected
  about the same (deflating small text is fast, fewer bytes are written);
  the gain is the size of tagged documents, a third or more. Measured
  first, with benchmarks/: a long PDF/UA table, before and after, the time
  and the size; PDFjet's name is speed, so the numbers decide.
  **Maybe, with it: the cross-reference stream** (talked over with the
  owner, 8 October 2026). The two go together: only a cross-reference
  stream can point into an object stream, the text table cannot; alone it
  saves little, 20 bytes an object in the table against 2 or 3 compressed
  (10,000 objects: about 200 KB to 25 KB). One option for both, on by
  default, off for PDF/A-1 (PDF 1.4 based, no cross-reference streams;
  PDF/A-2, 3 and PDF/UA allow them), and a switch for the old layout. The
  cons, each to handle: Acrobat before 6 (2003) and homemade scripts that
  look for `xref` cannot read it (the switch is for them); the table is no
  longer readable by eye (mutool show or qpdf --qdf for debugging); the
  cross-reference stream is never encrypted, as the spec says, so the writer
  leaves it out when it encrypts, with a test of an AES-256 file; PDFjet
  Pro's signer appends an update, which should use a cross-reference stream
  when the original does: check it before the library writes them by
  default. Not a con: the streaming, as the cross-reference stream is
  written at the end from the offsets PDFjet keeps now; PDFjet's reader
  already reads them.
  **PDF/A-1, and the owner's view (8 October 2026).** PDF/A-1 (ISO 19005-1,
  2005) is built on PDF 1.4: no object streams, no cross-reference streams.
  Replaced by PDF/A-2 (2011, PDF 1.7, which allows both), PDF/A-3 (2012,
  embedded files: ZUGFeRD, Factur-X, PDFjet Forms) and PDF/A-4 (2020, PDF
  2.0). Still asked for by some archive and filing systems set up around
  2005-2012; most requirements now accept PDF/A-2 or later. PDFjet offers
  PDF_A_1A and PDF_A_1B (Example_01, Example_34), so customers may use it,
  and it must keep working. Object streams would then mean two layouts of a
  file, and not one `if`: every writer of objects (encryption, merge, split,
  stamp, forms, PDFjet Pro's signer) tested in both, in four ports, for
  good. The owner: "If it was me - one or the other". So the one layout
  stays the plain cross-reference table, as now, for every document; object
  streams only if a real need is shown (a customer, or Forms' large tables
  beyond what pdfjet-server can limit), and then with that cost accepted on
  purpose. The large CJK fonts, subset, are the bigger and simpler win, and
  come first. Later the same day the owner put object streams at **v9.3 or later**:
  "Maybe by than PDF/A-1 will be not very important"; the question of the
  two layouts is looked at again then. The CJK fonts are the outrageous
  case and are fixed first; for Latin, Greek and Cyrillic the small IBM Plex
  Sans .stream fonts serve, embedded whole, as now.
- ✅ Done for 9.0.5 on 9 October 2026 (the owner: "it is useful and make PDFjet more robust rejecting malicious files early"): `ImageSize`, in the four ports, see the CHANGELOG. As first written: Maybe: a size-only reader of an image, in the four ports: the width and
  the height a PNG, a JPEG or a BMP is drawn at, read from its header, the
  IHDR and pHYs chunks, the SOF and JFIF segments, without embedding it, for
  a layout that places images before it draws them. New API, so after the
  freeze of 9.0.2; pdfjet-server has no need of it, as it embeds each image
  once for its fingerprint anyway. Apart from it, the check of the rows of an
  opaque PNG could inflate them a piece at a time, not whole: in Go the
  buffer grows to about twice the rows, 250 MB for 108 MB of them, which
  the image does not need, as its IDAT data is embedded as it is.
- ⬜ The decoded samples of an image read from a PDF, in the four ports:
  the colors of an /Indexed image, as `GetData` gives its indexes, the
  stream being its indexes. pdfjet-server expands the palette to RGB of
  eight bits itself (`indexedToRGB`, xobject.go), for the fingerprint of an
  image and the PNG it makes again from it. New API, so after the freeze of
  9.0.2; see the item on `PDFobj.GetData` above, whose predictor part was
  fixed on Sep 28.
- ⬜ `Table.SetCellPadding(side, vertical float32)`, in the four ports: the
  padding of every cell of the table, at its sides and over and under its
  text, as `SetCellBorders`, `SetCellBorderColor` and `SetCellBorderWidth`
  set the borders of every cell. Today it is set cell by cell, with
  `Cell.SetPadding` or one side at a time, as pdfjet-server's newTable does
  for PDFjet Forms, 4 points at the sides and 3 over and under, since a
  table whose cells have borders looked cramped at the default (5 October
  2026). Opt-in: NewCell's 2 points on every side stay the default, as
  changing it would make every row of every customer's table taller and
  move its page breaks. The rows added under a wrapped cell keep their own
  padding as now: none over them, NewCell's under them. A test in each port,
  the rows' heights compared across the ports. Small.
- ⬜ The Swift port slow with a JPEG: 20 pages drawing one photo of 600 KB,
  embedded once, took 30 ms a document in Swift, against 0.9 in Go, 1.4 in
  C# and 3.9 in Java, the same PDF from each (measured on 5 October 2026,
  Linux, Swift 6.4, a release build; the benchmark is kept privately). A
  JPEG is stored as it is, with no Deflate, so the time is in reading or
  copying its bytes: `Content.getFromStream` reads it 4 KB at a time and
  appends, `JPGImage` parses it, `Image.init` makes it a `Data` and an
  `InputStream` again, and `addImage` writes it; a profile says which. The
  Swift port is the slowest of the four with text too, less so: 100 pages
  of IBM Plex Sans in 24 ms against 10 to 20, and a plain .ttf font parsed
  at length (one page with Noto Sans, 44 ms against 20 as a .stream). Worth
  doing before Swift is sold in the commercial product.
- ✖️ Obsolete since 9 October 2026: the .stream format is removed from PDFjet, its converter too. The older stream format as advice, not only a switch: a developer
  who makes PDFs on their own server, and doesn't give the fonts to
  anyone, can convert an OpenType font with
  `util/generate-stream-fonts-files.sh --old-format`. The .otf.stream then
  holds the CFF alone, without the block that rebuilds the whole font, so
  it is smaller and faster to load, and the PDFs come out the same (the
  same CFF is embedded either way). Say so in the README's "Stream fonts,
  and what a PDF embeds" and in the converter's help, with the licence
  caveat: a CFF-only stream is a modified font under the OFL, fine on
  one's own server, not to be passed on. Only .otf fonts gain; a
  .ttf.stream (Source Serif 4, JetBrains Mono) has always held the whole
  font. Measured on 5 October 2026: PDFjet Forms' 12 weights of IBM Plex
  Sans are 575 KB in the older format against 979 KB, 403 KB less.
- ⬜ Maybe: a faster Deflate for Swift, which has its own, written in
  Swift. After the review of Sep 28 it is Swift's main cost for a PNG that
  is decoded and compressed again, one with transparency: about 500 ms for
  a photo of 3000 by 3993 pixels. Its time goes to the work per byte, the
  hash, the tokens and the Huffman codes, not to the search for matches, so
  its one setting, the chain of 32, barely changes it; a faster one needs a
  design of its own, as zlib's levels 1 to 3 have. A large job, worth it
  if a customer draws such images from Swift.
- ⬜ Maybe: Andika in pdfjet-fonts, its four styles, as .ttf files (no .stream files since 9 October 2026).
  **The owner, 9 October 2026: not in PDFjet Forms** (a fourth family is a good deal of work: the
  .ttf, the .woff2 and the metrics, the 32 languages checked, and only four styles where the
  editor offers twelve weights); in the editor it would have been "Andika", as the other families
  are named. For the library, still a maybe, leaning no: "people that care about this can find it
  and use it. It would be stupid to try to bundle all the fonts.google in the product." OEM12x20,
  the item before this one, was taken out the same day.
  (SIL, https://software.sil.org/andika/, SIL Open Font License, as IBM Plex
  and Noto). A font designed for legibility: I, l and 1 clearly apart, b, d,
  p and q not mirrors of one another, single-storey a and g, open shapes and
  generous spacing, and Latin, Cyrillic, Greek and phonetic letters. Pleasant
  to read for anyone, so a candidate for a default reading font, of Forms and
  of the booklet, not only an option. Where it would add value to PDFjet:
  1. The accessibility story. PDFjet sells PDF/UA, and a font designed for
     legibility is the natural companion. Tagging makes a document readable
     by a screen reader; Andika makes it readable by people who read it
     themselves. "Accessible structure and an accessible font" is a stronger
     pitch than PDF/UA alone.
  2. PDFjet Forms. Forms for public services, schools and healthcare are
     exactly where clear letters matter. It could be a font choice in the
     editor, something like "Clear (Andika)".
  3. Education and children's material: worksheets and reading material,
     where the single-storey a and g matter.

  With one example, a reading worksheet or a form tagged for PDF/UA, which
  passes the viewer checks as every example does. No new library API. Its
  size does not matter: pdfjet-fonts is fetched apart from the library, and a
  document embeds what it uses.

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
