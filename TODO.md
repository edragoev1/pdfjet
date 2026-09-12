# PDFjet v9.0.0 — release plan

Target: **2026-10-11** (30 days from 2026-09-11). This file is the working list
for the release; tick items off as they land on master.

v9.0.0 is a major release because the API changed since v8.7.0: the Go `Drawable`
declares `SetLocation` and returns `Drawable` from it, `DrawOn` returns
`[2]float32` in Go, `Arc.drawOn` returns the corner of the arc, the `UserAccess`
values are halved, and `/P` is negative. Everything below that is not done is
either a blocker (must ship in 9.0.0) or a stretch goal (ships if ready).

Legend: ⬜ open, ✅ done, **B** blocker, S stretch.

## Week 1 (Sep 11–17): decisions and breaking changes

- ✅ **B** Go `Drawable` declares `SetLocation`; `SetLocation` returns `Drawable`
      and goes last in a chain. Commit 95ea973d.
- ✅ **B** Go `DrawOn` returns `[2]float32` everywhere; `Arc.drawOn` returns the
      bottom right corner in all four ports. Commit 3fbafe4c.
- ✅ **B** Go module path for a major version: `go get github.com/edragoev1/pdfjet@v8.7.0`
      failed with *module path must match major version*, so nobody could
      fetch a tagged Go release. The module is now
      `github.com/edragoev1/pdfjet/v9` in `go.mod`, every import, doc2go and
      the README. Still to do after tagging: `go list -m
      github.com/edragoev1/pdfjet/v9@v9.0.0` from a scratch module.
- ✅ **B** `Table`, `TextBlock`, `SVGImage` and `DonutChart` implement
      `Drawable` in all four ports. `DonutChart.drawOn` returns the bottom
      right corner of the outer circle (it was `void` in Java, C# and Swift
      and returned `error` in Go), and Go `DonutChart.SetLocation` returns
      `Drawable`, so it goes last in a chain like the other Go drawables.
- ✅ **B** `Stamp.Rectangle()` and `Stamp.Draw()` were empty stubs in Go and C#,
      `rectangle()` and `draw()` in Java, `rectangle()` in Swift. Removed from
      all four ports; nothing used them.
- ✅ S `Permissions`/`UserAccess` stay int based in Java and Swift: Java has
      no flags enum and a Swift `OptionSet` is not an enum, so both keep the
      enum with the bit values of the standard, combined with `|` on
      `getValue()`. Documented in the README Port differences section.

## Week 2 (Sep 18–24): encryption and PDF reading

- ✅ **B** Password handling is the same in the four ports: UTF-8, at most 127
      bytes, no normalization (Go has none without a dependency; documented in
      the README). A 200 byte password opens with its first 127 bytes and not
      with 126 in MuPDF and qpdf; a Cyrillic password opens in Poppler, MuPDF
      and qpdf. Still to check: Acrobat.
- ✅ **B** `Passwords` default to `""` in all four ports; a PDF with no user
      password opens without a prompt, with the permissions applied.
- ✅ **B** `Perms` bytes 12–15 are random bytes in all four ports, as the
      standard says; Java, C# and Go wrote `----`.
- ✅ **B** `Decryptor` opened only PDFs with an empty user password. The PDF
      reading entry points take a password in all four ports (`read(stream,
      password)`, `ReadWithPassword` in Go, `read(from:password:)` in Swift),
      checked as user then owner password for revisions 2 to 6. Example_30
      and qpdf-made R2, R3, R4 and R6 files read back with either password in
      all four ports; a wrong or missing password raises a clear error.
- ✅ Swift AES speed: 26 ms per MiB in a release build, 1.2 s per MiB in a
      debug build (`swiftc -O` / `-Onone`, `Cryptography.aesEncryptCBC`).
      Example_30 with the 7 MB NotoSansTC-Regular.ttf takes 0.47 s in release
      and 11.6 s in debug, so the cipher stays as it is; no T-table.
- ✅ An encrypted PDF/UA file (Example_22 plus the Example_30 passwords)
      failed veraPDF ua1 in all four ports on ISO 14289-1 7.16: bit 10 of
      `/P` (extract for accessibility) must be set. `Encryption` now grants it
      when the compliance is PDF/UA, and the files pass with `--password` in
      all four ports. Noted in the README, with the veraPDF 1.30.2 hash loop
      bug that rejects about one in forty AES-256 files from any producer.

## Week 3 (Sep 25–Oct 1): right to left text, marks and code TODOs

Pick from the README limitations; the first three are the ones users hit.

- ✅ **B** Letters unjoined at a forced break inside a too wide right to left
      word (`TextBlock`): `Bidi.reorderVisually(str, from, to)` shapes the
      whole string and returns the part for a range of it, and `TextBlock`
      makes each line of a broken word, and the line the rest of the word
      starts, that way, so the joined forms survive the break in all four
      ports.
- ✅ **B** ZWNJ and ZWJ were dropped from copied text (`می‌خواهم` → `میخواهم`).
      Bidi keeps them, Page puts each in the ActualText of the glyph before
      it with a zero-width stand-in glyph (MuPDF needs a glyph per character),
      or draws the font's own zero-width glyph when it has one. Poppler and
      MuPDF copy them; rendering is unchanged.
- ⬜ Brackets around left to right text nested in a right to left line still
      copy the wrong way round: apply the mirrored-bracket ActualText to the
      nested run as well.
- ⬜ S Marks in `.otf.stream`/`.ttf.stream` fonts are not positioned (no GPOS
      data in the stream format). Either add the mark anchors to the stream
      format and regenerate `fonts/**/*.stream`, or steer users to `.otf`/`.ttf`
      in the docs.
- ⬜ S MuPDF puts spaces inside some words with moved marks; check whether one
      span per word instead of per mark cluster fixes it.
- ⬜ Code TODOs that change output (all four ports):
  - ⬜ `CheckBox` with a URI: wrap the link in BMC/EMC so tagged PDFs stay valid.
  - ⬜ `CheckBox`/`RadioButton`: use the font size, not the body height, for the
    link rectangle.
  - ⬜ `Rect`: `structureType` and `fillShape` TODOs.
  - ⬜ `PDF417`: keep a quiet zone of 10 modules.
  - ⬜ C# `SVG`: elliptical arc (`A`/`a`) is not implemented; Java and Go have it,
    so C# output differs for SVGs with arcs.
  - ⬜ C# `PNGImage`: `gAMA`, `cHRM`, `sBIT`, `bKGD` chunks are TODO; confirm Java
    ignores them too, then drop the markers.
- ⬜ Internal TODOs (no output change, do if time allows): `Cell` field
      renames, `State` brush colour copy, Go `image.go` close error, Go
      `qrcode/qrutil.go` doc comments. 49 markers in total; leave none that
      describe a real gap without an issue or a line here.

## Week 4 (Oct 2–11): parity audit, docs, release

- ⬜ **B** Public API audit across the four ports: script that lists public
      types and methods per class in Java, C#, Go and Swift and diffs them.
      Fix the differences that are not language conventions; document the rest
      in the README Port differences section.
- ⬜ **B** `check-examples.sh` clean, `go vet` clean, Swift builds with
      warnings as errors, Windows workflow run from the Actions tab and green.
- ⬜ **B** Manual viewer pass: Acrobat Reader on Windows opens Example_30 with
      `hello` and `world`, shows print allowed and copy denied, and opens the
      PDF/UA and PDF/A examples without warnings. Same files in Preview,
      Chrome and Firefox.
- ⬜ **B** CHANGELOG `## v9.0.0` entry. Breaking changes first: Go `Drawable`
      and `[2]float32`, `Arc.drawOn`, `UserAccess` values (any code that used
      raw ints must change), negative `/P`, Go module path `/v9`, and whatever
      week 1 decides for `Table`/`TextBlock`/`SVGImage`/`DonutChart` and
      `Stamp`. Then: Swift encryption, random salts, `EncryptMetadata true`,
      right to left fixes, TODO cleanups.
- ⬜ **B** Version bump: producer string `PDFjet v9.0.0` in `PDF.java`,
      `PDF.cs`, `pdf.go`, `PDF.swift`; `package-java.sh` and
      `package-dotnet.sh` archive names; README where 8.7.0 is mentioned.
- ⬜ **B** Regenerate the docs (`generate-documentation.sh`), check the four
      references on GitHub Pages after the push, and that the examples pages
      link the new Swift Example_30.
- ⬜ Release: tag `v9.0.0`, build the Java and .NET archives, confirm
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
  