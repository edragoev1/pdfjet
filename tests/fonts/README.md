# Test fonts

Fonts for tests, which PDFjet does not carry. For the subsets of fonts with
CFF outlines:

- IBM Plex Sans Regular and Italic (`.otf`), keyed by glyph names, as most
  `.otf` fonts are; PDFjet carries these fonts in `resources/fonts` too, from
  9.1.9. SIL Open Font License 1.1, in `IBMPlexSans-license.txt`.
- Source Han Sans JP Regular, Adobe's SubsetOTF/JP edition of release
  2.005R, keyed by CIDs, with what the CID-keyed fonts PDFjet carries (IBM
  Plex Sans JP, KR, SC and TC) do not have: 512 global subroutines, 18 font
  dictionaries, each with its own private dictionary and subroutines, and a
  charset whose CIDs are not the glyph numbers. SIL Open Font License 1.1,
  in `SourceHanSansJP-LICENSE.txt`.

For the subsets of fonts with TrueType outlines, and the tests that read or
patch the tables of a TrueType font:

- IBM Plex Sans Regular (`.ttf`), IBM's TrueType edition of the `.otf`
  above, which PDFjet carried in `resources/fonts` up to 9.1.7. SIL Open Font
  License 1.1, in `IBMPlexSans-license.txt`.

For the subsets of fonts with TrueType outlines and the long offsets of
`loca`, of 2,048 units to the em, and their outlines:

- Cascadia Mono Regular (`.ttf`), Microsoft's TrueType edition, of release
  2407.24, of the `.otf` PDFjet carries in `resources/fonts` from 9.1.9; it
  carried this `.ttf` up to 9.1.7. SIL Open Font License 1.1, with the
  Reserved Font Name Cascadia Code, in `CascadiaMono-LICENSE.txt`.

For the subsets of fonts with TrueType outlines and the long offsets of
`loca`, and the cap height read from `OS/2`:

- IBM Plex Math Regular, which PDFjet does not carry from 9.1.7. SIL Open
  Font License 1.1, in `IBMPlexMath-license.txt`.

For kerning:

- Source Serif 4 Regular, which PDFjet does not carry from 9.1.7, for the tests of
  a font that kerns the space with the letters beside it, as Source Serif 4
  kerns it with T, V, W and Y and no font PDFjet carries does. SIL Open Font
  License 1.1, in `SourceSerif4-OFL.txt`.

They are test data only.
