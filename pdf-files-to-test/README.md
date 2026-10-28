# PDF files to test

The PDFs of the two manual checks before a release, PAC and the viewer pass
(see "PAC" and "The manual viewer pass" in `../TODO.md`). They are not kept
in the repository. The viewer pass is described at the end, under "The viewer
pass"; it had a folder of its own, viewer-files-to-test, until 28 October 2026.

## PAC

PAC runs in Wine on Ubuntu (it ran on Windows before 28 October 2026).

**Only files that claim PDF/UA go to PAC:** those whose XMP metadata has pdfuaid:part, as an example has when it calls `setCompliance` with a PDF/UA level. A file that does not is untagged, and PAC fails it on every text object and its metadata, which says nothing about PDFjet's tagging. On 28 September 2026 Example_07 (PDF/A-3b), Example_34 (PDF/A-1b), Example_43 (its PDF/UA commented out, as a tagged table of 2,000 pages is too large; its 11.7 MB of 2,000 pages crash PAC) and Example_37, 41, 46 and 51 (no compliance) did not.

**Leave out Example_17, Example_33 and Example_52.** They claim PDF/UA and pass
veraPDF, but they crash PAC (29 September 2026; Example_17 on 28 October 2026).
Example_17 has 736 pages and Example_52 has 745.

**Only files that embed all their fonts go here.** PDF/UA asks for every font
to be embedded (Matterhorn 31-001), so a file with one that is not fails for
that alone, and says nothing about its tagging. Leave out such files, such as
the examples that draw with the standard 14 fonts (Helvetica) or with the
Adobe CID fonts of Chinese, Japanese and Korean, which a reader supplies.

Check before copying files here: `pdffonts FILE.pdf` shows `no` under `emb`
for a font that is not embedded, and this lists every file with one:

```sh
for f in *.pdf; do
  pdffonts "$f" 2>/dev/null | awk 'NR > 2 && $(NF-4) == "no" { n++ } END { exit !n }' && echo "$f"
done
```

On 28 September 2026 these four of the examples did not embed all their
fonts: Example_04 (Helvetica and the CID fonts), Example_05 (Helvetica),
Example_44 (a Chinese CID font) and Example_50 (Helvetica).
Since 7 October 2026 Example_04 and Example_44 embed IBM Plex Sans JP, KR
and SC, as no example uses the Adobe CJK fonts, and Example_05 IBM Plex
Sans, in place of its kerning in Helvetica; Example_50 keeps Helvetica. On
the same day Example_05, 07 (PDF/A-3a and PDF/UA-1), 34 (PDF/A-1a) and 44
became tagged, so that Acrobat does not offer to make them accessible.

## Known PAC warnings, left as they are

**Example_06: six Quality warnings on its annotations.** PAC's PDF/UA and WCAG
checks pass; its Quality check warns once for each annotation that is not a
link: the two file attachments, the note and the three shapes. Each is an
`Annot` structure element, directly under the document with Placement Block,
holding its annotation, as PDF/UA asks (Matterhorn 28-011). No structure we
tried clears the warnings (29 September 2026), so Example_06 stays as it is:

| Variant | Annot elements | PAC Quality |
|---|---|---|
| as written | under the document, Placement Block | 6 × Tagged text consists of only whitespace |
| no appearance streams | the same | the same |
| a BBox, the rectangle of the annotation | the same | the same |
| no Lang | the same | the same |
| each holding the text of its label | the same | the same |
| no Alt, or ActualText as well | the same | the same |
| no Placement Block | under the document, inline | 6 × Possibly inappropriate use of an Annot element |
| in the P of their labels | inline in a P | 6 × Possibly inappropriate use of an Annot element |
| cut out of the tree, still in the parent tree | none reachable | green, but the tree is broken |
| no annotations but the link | none | green |

PAC's AI-assisted check also takes the two file attachment lines for a list
(score 0.69). Tagging them as one would put their `Annot` elements in list
items, which, by the table, trades the warning for another.

**Example_44: an email address in a form.** PAC's PDF/UA and WCAG checks
pass; its Quality check reports the value of the Email field
(anna.lindqvist@example.com) as "Link in text does not have a Link element".
A `Form` field holds text and cannot be a `mailto:` link, and the API is
frozen, so it stays as it is (29 September 2026; seen again at 9.1.5 on
28 October 2026, when the other 40 files passed every check).

**PDFjet Forms: an email address typed into a field.** The same Quality
check, "Completeness of Link elements: Link in text does not have a Link
element", on the filled-in forms whose Email field holds an address
(jane.muster@example.com); PDF/UA and WCAG pass, and the templates, the free
version and the "Fill in this form online" link are clean. A value typed into
a form is an answer, not a link, so it stays as it is (1 October 2026).

## The viewer pass

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
go run ./.github/scripts/viewer-checklist pdf-files-to-test/Checklist.pdf
```

The other files are the ones the checklist names, the Java examples of
`./build-java.sh`: Example_01, 02, 03, 04, 05, 06, 13, 19, 20, 21, 23, 28,
35 (the layers, optional content groups, of a map of Europe), 40, 42, 45, 46
and 47, and the two PDFs of `.github/scripts/encrypted-pdfs`:

```sh
go run ./.github/scripts/encrypted-pdfs pdf-files-to-test
```

And the two PDFs of `.github/scripts/new-code-pdfs`, of the code new in
9.1.5 that no example draws: `Kerning.pdf`, lines of IBM Plex Sans kerned
and not, and `Copied_Image.pdf`, a PNG with transparency on an orange box,
drawn and then copied from a PDF that was read (25 October 2026):

```sh
go run ./.github/scripts/new-code-pdfs pdf-files-to-test
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
