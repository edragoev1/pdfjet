# PDF/UA files to test

The PDFs put here are checked with PAC, on Windows (see Goal 3 in
`../TODO.md`). They are not kept in the repository.

**Only files that claim PDF/UA go here:** those whose XMP metadata has pdfuaid:part, as an example has when it calls `setCompliance` with a PDF/UA level. A file that does not is untagged, and PAC fails it on every text object and its metadata, which says nothing about PDFjet's tagging. On 28 September 2026 Example_07 (PDF/A-3b), Example_34 (PDF/A-1b), Example_43 (its PDF/UA commented out, as a tagged table of 2,000 pages is too large; its 11.7 MB of 2,000 pages crash PAC) and Example_37, 41, 46 and 51 (no compliance) did not.

**Leave out Example_33 and Example_52.** They claim PDF/UA and pass veraPDF,
but they crash PAC (29 September 2026); Example_52 has 745 pages.

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
and SC, as no example uses the Adobe CJK fonts; Example_05 and Example_50
keep Helvetica.

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

**Example_42 and Example_45: an email address in a form.** PAC's PDF/UA and
WCAG checks pass; its Quality check reports the value of the Email field
(jsmith12345@gmail.ca, anna.lindqvist@example.com) as "Link in text does not
have a Link element". A `Form` field holds text and cannot be a `mailto:`
link, and the API is frozen, so both stay as they are (29 September 2026).

**PDFjet Forms: an email address typed into a field.** The same Quality
check, "Completeness of Link elements: Link in text does not have a Link
element", on the filled-in forms whose Email field holds an address
(jane.muster@example.com); PDF/UA and WCAG pass, and the templates, the free
version and the "Fill in this form online" link are clean. A value typed into
a form is an answer, not a link, so it stays as it is (1 October 2026).
