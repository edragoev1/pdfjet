# Test fonts

Fonts for the tests of the subsets of fonts with CFF outlines, which PDFjet
does not carry:

- IBM Plex Sans Regular and Italic (`.otf`), keyed by glyph names, as most
  `.otf` fonts are; PDFjet carries the `.ttf` of these fonts in
  `resources/fonts`. SIL Open Font License 1.1, in `IBMPlexSans-license.txt`.
- Source Han Sans JP Regular, Adobe's SubsetOTF/JP edition of release
  2.005R, keyed by CIDs, with what the CID-keyed fonts PDFjet carries (IBM
  Plex Sans JP, KR, SC and TC) do not have: 512 global subroutines, 18 font
  dictionaries, each with its own private dictionary and subroutines, and a
  charset whose CIDs are not the glyph numbers. SIL Open Font License 1.1,
  in `SourceHanSansJP-LICENSE.txt`.

They are test data only.
