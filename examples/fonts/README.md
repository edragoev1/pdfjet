# Fonts of the examples

Fonts that only the examples use, not packaged with PDFjet:

- `IBMPlexSans/IBMPlexSans-Regular.ttf`, IBM Plex Sans 3.005, IBM's
  TrueType edition of the `.otf` PDFjet carries, for Example_07: the same
  design from a `.ttf` file and from an `.otf` file. Under the SIL Open
  Font License 1.1 (license.txt beside it).
- `IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf`, IBM Plex Sans
  Devanagari 1.2 (IBM's release @ibm/plex-sans-devanagari 1.1.0), and
  `NotoSansKhmer/NotoSansKhmer-Regular.otf`, Noto Sans Khmer 2.004, unhinted
  (Noto's release NotoSansKhmer-v2.004), for Example_52: Hindi and Khmer
  shaped by the platform's text engine and drawn with Page.drawGlyphs. Both
  .otf since 9.2.0: their CFF outlines go into the PDF as they are, where a
  .ttf's curves are converted, and the pages are 16 to 22% smaller (measured
  17 November 2026). IBM Plex for Hindi: complete for Hindi, Marathi, Nepali
  and Sanskrit, the IBM Plex of the rest of the page; there is no IBM Plex
  for Khmer.
- `NotoSansDevanagari/NotoSansDevanagari-Regular.ttf`, Noto Sans Devanagari
  2.001, for the drawGlyphs tests of the four ports: a TrueType font, where
  Example_52's Devanagari is CFF.

All under the SIL Open Font License 1.1 (license.txt beside each).
