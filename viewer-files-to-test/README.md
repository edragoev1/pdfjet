# Viewer files to test

The PDFs of the manual viewer pass of `../TODO.md`, to open by hand in
Acrobat Reader, NVDA, Edge, Preview, Firefox, and Acrobat's Read Out Loud on
the Mac. They are not kept in the repository. Foxit and VoiceOver are out of
the pass, the owner's choice of 8 October 2026: Foxit has never had trouble
with PDFjet's files where Acrobat, the strictest, is the one to pass;
VoiceOver reads PDFs in Acrobat for Mac only in part, and NVDA with Acrobat
on Windows is the screen reader of the pass.

`Checklist.pdf` is the list to print and tick off with a pen. It is drawn by
PDFjet, from `.github/scripts/viewer-checklist`:

```sh
go run ./.github/scripts/viewer-checklist viewer-files-to-test/Checklist.pdf
```

The other files are the ones the checklist names, the Java examples of
`./build-java.sh`: Example_01, 02, 03, 04, 05, 06, 13, 19, 20, 21, 23, 28,
35 (the layers, optional content groups, of a map of Europe), 40, 42, 45, 46
and 47, and the two PDFs of `.github/scripts/encrypted-pdfs`:

```sh
go run ./.github/scripts/encrypted-pdfs viewer-files-to-test
```

And the two PDFs of `.github/scripts/new-code-pdfs`, of the code new in
9.1.3 that no example draws: `Kerning.pdf`, lines of IBM Plex Sans kerned
and not, and `Copied_Image.pdf`, a PNG with transparency on an orange box,
drawn and then copied from a PDF that was read (25 October 2026):

```sh
go run ./.github/scripts/new-code-pdfs viewer-files-to-test
```

Example_47 opens with the user password `hello` and the owner password
`world`; `Encrypted_Cyrillic.pdf` with `пароль`; `Encrypted_200_Bytes.pdf`
with `0123456789` typed 20 times. The list follows the checklist, which
`viewer-checklist` draws: when the examples are numbered anew, the checklist
says which files to copy (17 October 2026, for 9.1.1).

Build them again from the frozen main
before the pass of Oct 15-20.

## The pass of 9.0.5 (9 October 2026)

The Java examples of the check of ed6b1c9b, the files of the new code only,
in Acrobat Reader on Windows, about half an hour:

- Example_02 and Example_04: Chinese, Japanese and Korean drawn in the .otf
  of IBM Plex Sans SC, TC, JP and KR, each a subset. The text is all there,
  and copies out right.
- Example_06: are the three shapes as see-through as in Chrome (50%), or
  paler?
- Example_28: the fonts of CFF outlines and TrueType, each a subset, and
  one TrueType font kept whole.
- Example_37 and Example_50: a font added to an existing PDF, now a subset.
  The text drawn on the pages is there.
- Example_44: the Chinese one paragraph (NVDA reads it at once, in Chinese),
  its lines as before.
