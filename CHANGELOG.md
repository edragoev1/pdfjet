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

### Stamp
- `Stamp` now conforms to `Drawable` in Swift and Go, as it already did in
  Java and C#, so it can be added to a `Container` or an
  `OptionalContentGroup`. Swift `drawOn` takes `Page?` and `setLocation`
  returns `Self`; Go `DrawOn` returns `[2]float32` instead of `[]float32`.

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
- `docs/` is no longer tracked in git. The `Documentation` GitHub Actions
  workflow (`.github/workflows/docs.yml`) builds the Java and C# references on
  every push to `master` and publishes them to GitHub Pages at
  <https://edragoev1.github.io/pdfjet/>, under `java/` and `net/`.

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
