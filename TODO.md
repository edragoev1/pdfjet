# PDFjet v9.0.0 — release plan

Target: **2026-10-11** (30 days from 2026-09-11). This file is the working list
for the release; tick items off as they land on master.

v9.0.0 is a major release because the API changed since v8.7.0: the Go `Drawable`
declares `SetLocation` and returns `Drawable` from it, `DrawOn` returns
`[2]float32` in Go, `Arc.drawOn` returns the corner of the arc, the `UserAccess`
values are halved, and `/P` is negative. Everything below that is not done is
either a blocker (must ship in 9.0.0) or a stretch goal (ships if ready).

Legend: `[ ]` open, `[x]` done, **B** blocker, S stretch.

## Week 1 (Sep 11–17): decisions and breaking changes

- [x] **B** Go `Drawable` declares `SetLocation`; `SetLocation` returns `Drawable`
      and goes last in a chain. Commit 95ea973d.
- [x] **B** Go `DrawOn` returns `[2]float32` everywhere; `Arc.drawOn` returns the
      bottom right corner in all four ports. Commit 3fbafe4c.
- [x] **B** Go module path for a major version: `go get github.com/edragoev1/pdfjet@v8.7.0`
      failed with *module path must match major version*, so nobody could
      fetch a tagged Go release. The module is now
      `github.com/edragoev1/pdfjet/v9` in `go.mod`, every import, doc2go and
      the README. Still to do after tagging: `go list -m
      github.com/edragoev1/pdfjet/v9@v9.0.0` from a scratch module.
- [ ] **B** Decide `Drawable` for the classes that have `setLocation` and
      `drawOn` but do not implement it in Java: `Table`, `TextBlock`,
      `SVGImage`, and `DonutChart` (whose `drawOn` is `void` in Java and returns
      `error` in Go). Recommended: implement `Drawable` in all four ports and
      make `DonutChart.drawOn` return the bottom right corner.
- [x] **B** `Stamp.Rectangle()` and `Stamp.Draw()` were empty stubs in Go and C#,
      `rectangle()` and `draw()` in Java, `rectangle()` in Swift. Removed from
      all four ports; nothing used them.
- [ ] S Decide whether `Permissions`/`UserAccess` get a typed flags API in Java
      and Swift like C# and Go, or stay int based. Document the choice.

## Week 2 (Sep 18–24): encryption and PDF reading

- [x] **B** Password handling is the same in the four ports: UTF-8, at most 127
      bytes, no normalization (Go has none without a dependency; documented in
      the README). A 200 byte password opens with its first 127 bytes and not
      with 126 in MuPDF and qpdf; a Cyrillic password opens in Poppler, MuPDF
      and qpdf. Still to check: Acrobat.
- [x] **B** `Passwords` default to `""` in all four ports; a PDF with no user
      password opens without a prompt, with the permissions applied.
- [x] **B** `Perms` bytes 12–15 are random bytes in all four ports, as the
      standard says; Java, C# and Go wrote `----`.
- [x] **B** `Decryptor` opened only PDFs with an empty user password. The PDF
      reading entry points take a password in all four ports (`read(stream,
      password)`, `ReadWithPassword` in Go, `read(from:password:)` in Swift),
      checked as user then owner password for revisions 2 to 6. Example_30
      and qpdf-made R2, R3, R4 and R6 files read back with either password in
      all four ports; a wrong or missing password raises a clear error.
- [ ] Swift AES speed: the pure Swift cipher runs per block on `[UInt8]`. Time
      Example_30 with a 1 MB font in release and debug builds; if release is
      over a second, switch to a T-table implementation.
- [ ] Check that an encrypted PDF/UA file passes veraPDF ua1 in all four ports
      (encryption is allowed in PDF/UA, forbidden in PDF/A) and note it in the
      README Encryption section.

## Week 3 (Sep 25–Oct 1): right to left text, marks and code TODOs

Pick from the README limitations; the first three are the ones users hit.

- [x] **B** Letters unjoined at a forced break inside a too wide right to left
      word (`TextBlock`): `Bidi.reorderVisually(str, from, to)` shapes the
      whole string and returns the part for a range of it, and `TextBlock`
      makes each line of a broken word, and the line the rest of the word
      starts, that way, so the joined forms survive the break in all four
      ports.
- [x] **B** ZWNJ and ZWJ were dropped from copied text (`می‌خواهم` → `میخواهم`).
      Bidi keeps them, Page puts each in the ActualText of the glyph before
      it with a zero-width stand-in glyph (MuPDF needs a glyph per character),
      or draws the font's own zero-width glyph when it has one. Poppler and
      MuPDF copy them; rendering is unchanged.
- [ ] Brackets around left to right text nested in a right to left line still
      copy the wrong way round: apply the mirrored-bracket ActualText to the
      nested run as well.
- [ ] S Marks in `.otf.stream`/`.ttf.stream` fonts are not positioned (no GPOS
      data in the stream format). Either add the mark anchors to the stream
      format and regenerate `fonts/**/*.stream`, or steer users to `.otf`/`.ttf`
      in the docs.
- [ ] S MuPDF puts spaces inside some words with moved marks; check whether one
      span per word instead of per mark cluster fixes it.
- [ ] Code TODOs that change output (all four ports):
  - `CheckBox` with a URI: wrap the link in BMC/EMC so tagged PDFs stay valid.
  - `CheckBox`/`RadioButton`: use the font size, not the body height, for the
    link rectangle.
  - `Rect`: `structureType` and `fillShape` TODOs.
  - `PDF417`: keep a quiet zone of 10 modules.
  - C# `SVG`: elliptical arc (`A`/`a`) is not implemented; Java and Go have it,
    so C# output differs for SVGs with arcs.
  - C# `PNGImage`: `gAMA`, `cHRM`, `sBIT`, `bKGD` chunks are TODO; confirm Java
    ignores them too, then drop the markers.
- [ ] Internal TODOs (no output change, do if time allows): `Cell` field
      renames, `State` brush colour copy, Go `image.go` close error, Go
      `qrcode/qrutil.go` doc comments. 49 markers in total; leave none that
      describe a real gap without an issue or a line here.

## Week 4 (Oct 2–11): parity audit, docs, release

- [ ] **B** Public API audit across the four ports: script that lists public
      types and methods per class in Java, C#, Go and Swift and diffs them.
      Fix the differences that are not language conventions; document the rest
      in the README Port differences section.
- [ ] **B** `check-examples.sh` clean, `go vet` clean, Swift builds with
      warnings as errors, Windows workflow run from the Actions tab and green.
- [ ] **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, and opens the
      PDF/UA and PDF/A examples without warnings. Same files in Preview,
      Chrome and Firefox.
- [ ] **B** CHANGELOG `## v9.0.0` entry. Breaking changes first: Go `Drawable`
      and `[2]float32`, `Arc.drawOn`, `UserAccess` values (any code that used
      raw ints must change), negative `/P`, Go module path `/v9`, and whatever
      week 1 decides for `Table`/`TextBlock`/`SVGImage`/`DonutChart` and
      `Stamp`. Then: Swift encryption, random salts, `EncryptMetadata true`,
      right to left fixes, TODO cleanups.
- [ ] **B** Version bump: producer string `PDFjet v9.0.0` in `PDF.java`,
      `PDF.cs`, `pdf.go`, `PDF.swift`; `package-java.sh` and
      `package-dotnet.sh` archive names; README where 8.7.0 is mentioned.
- [ ] **B** Regenerate the docs (`generate-documentation.sh`), check the four
      references on GitHub Pages after the push, and that the examples pages
      link the new Swift Example_30.
- [ ] Release: tag `v9.0.0`, build the Java and .NET archives, confirm
      `go get github.com/edragoev1/pdfjet/v9@v9.0.0` works from a clean module,
      and confirm the Swift package resolves from the tag.

## Known and accepted for 9.0.0 (document, do not fix)

- Urdu is drawn in Naskh; Nastaliq fonts need GSUB and are not supported.
- Only Arabic, Persian and Urdu letters are shaped; Pashto, Sindhi and Kurdish
  letters are drawn unjoined.
- No explicit bidi embedding, override or isolate controls.
- Poppler separates an Arabic comma from its word; MuPDF moves numbers next to
  words and can move a bracket at a line end.
- Readers disagree on `EncryptMetadata false`, so PDFjet always encrypts the
  metadata and says so.
