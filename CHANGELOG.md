# Changelog

All notable changes to PDFjet are documented in this file. PDFjet ships four
parallel, hand-synchronized ports — Java (`com/pdfjet`), C# (`net/pdfjet`), Go
(`src`), and Swift (`Sources/PDFjet`) — kept behaviorally identical across
languages.

This is the first entry in this file; earlier releases were not tracked here.

## Unreleased

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

### Deprecated methods removed from Go and Swift
- Go `TextLine.SetColor` and Swift `TextLine.setColor` are removed; call
  `SetTextColor` or `setTextColor`, which they only forwarded to. Java and C#
  keep `setColor`, marked deprecated.
- Swift `SVGImage.getPenWidth()` is removed; call `getWidth()`.
- Go `TextColumn.SetSize` and Swift `TextColumn.setSize` are removed, as C# marks
  `SetSize` obsolete. Call `SetWidth` and `SetHeight` (Go) or `setWidth` and
  `setHeight` (Swift); the height setters are new, matching C# `SetHeight`.
  `Example_10` in both ports uses them.
- The Swift `DonutChart` and `Example_03`, `Example_37` and `Example_41` call
  `setTextColor`.

### Stamp
- `Stamp` now conforms to `Drawable` in Swift and Go, as it already did in
  Java and C#, so it can be added to a `Container` or an
  `OptionalContentGroup`. Swift `drawOn` takes `Page?` and `setLocation`
  returns `Self`; Go `DrawOn` returns `[2]float32` instead of `[]float32`.

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

### Documentation
- The C# API reference is built with DocFX (configuration in `docfx/`) into
  `docs/_net`. It replaces the copy of the Javadoc HTML that
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
  <https://edragoev1.github.io/pdfjet/>, under `java/` and `net/`.
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
  `net/`; the site's home page links to it. It covers the 56 packages in `src`
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
