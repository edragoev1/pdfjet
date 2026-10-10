# PDFjet — the plan

The library work that is open, in the four ports (Java, C#, Go and Swift),
and the checks that hold before each release. What each release brought is in
CHANGELOG.md.

## v9.0.5

Frozen. It brings subsets of every embedded font, TrueType and CFF, fonts
added to an existing PDF included; the `.ttf` and `.otf` fonts read directly,
with no `.stream` files; `ImageSize`; the EXIF orientation of JPEGs; the public
XML parser; `Table.setCellPadding`; `addCJKParagraph` with a language; and
the fixes of a code review of every change since 9.0.3, with a test of each.
Only a major issue holds it now; anything else goes into the next 9.0.x.

Left before the tag: the manual viewer pass (below) on the files of the new
code, then the release.

## Checks before each release

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
  9.0.3: Example_06's shapes have their opacity in an ExtGState and in the
  annotation's `/CA`, and ISO 32000 applies `/CA` to the appearance, so a
  strict viewer could draw them at 25% where MuPDF, PDFium, Poppler and pdf.js
  draw them at 50%.
- **PAC.** The PDF/UA files in `pdf-ua-files-to-test`, checked with PAC on
  Windows; see the README there for which files go in.
- **Benchmarks.** `benchmarks/run.sh` and the four ports' text and table
  benchmarks in `benchmarks/ports` and `benchmarks/table`, against the last
  release; the results are kept in their `results` folders.

## Open

- **Smaller tagged documents, with object streams.** Each cell of a table of
  PDF/UA costs 280 to 415 bytes, its text and a structure element of its own,
  each an object written uncompressed: a price list of 3,000 rows of 6
  columns is 4.7 MB. PDF 1.5's object streams, the small objects compressed
  together a batch of 100 to 200 at a time so that memory stays flat, and a
  cross-reference stream would shrink them a third or more. An option, on by
  default, off for PDF/A-1, which forbids object streams. Measured first, the
  time and the size of a long PDF/UA table, before and after.
- **For 9.0.7: an Arc's stroke width without a stroke color.** An Arc given
  `setStrokeWidth(4)` and no `setStrokeColor` is drawn 0 wide, a hairline:
  the branch of DrawOn for no colors set sets the pen width to 0, whatever the
  width. Found by the booklet's drawings, whose shapes snippet sets a color
  since. The fix in the four ports, with a test, and the other shapes checked
  for the same pattern (Ellipse, Rect, Line, Point, Path).
- **For 9.0.7: a font whose fsType forbids embedding.** The fsType of a
  font's `OS/2` table, bit 0x0002 ("Restricted License embedding"), says the
  font must not be embedded without its maker's permission; PDFjet reads
  only the bit that forbids subsetting, and embeds such a font. Refused, in
  the four ports, with a message that names the font and points to "Choosing
  a font you may embed" in the README; a test with a font whose fsType has
  the bit set.
- **For 9.0.7: a character the font lacks in a PDF/UA document.** It is
  drawn as the font's `.notdef` glyph, mapped to U+FFFD so that its text
  reads; PDF/UA-1 forbids any reference to `.notdef` from text. Reported:
  an error in PDF/UA mode, naming the character and the font, the fallback
  font the remedy.
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
