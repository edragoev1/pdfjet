# PDF/UA files to test

The PDFs put here are checked with PAC, on Windows (see Goal 3 in
`../TODO.md`). They are not kept in the repository.

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
