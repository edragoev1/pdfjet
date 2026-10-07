# Viewer files to test

The PDFs of the manual viewer pass, goal 4 of `../TODO.md`, to open by hand in
Acrobat Reader, NVDA, Edge, Foxit, Preview, VoiceOver and Firefox. They are
not kept in the repository.

`Checklist.pdf` is the list to print and tick off with a pen. It is drawn by
PDFjet, from `.github/scripts/viewer-checklist`:

```sh
go run ./.github/scripts/viewer-checklist viewer-files-to-test/Checklist.pdf
```

The other files are the ones the checklist names: the Java examples of
`./build-java.sh`, Example_01, 02, 04, 06, 07, 08, 13, 22, 25, 27, 30, 34, 38,
44, 46 (the layers, optional content groups, of a map of Europe), 54 and 55,
and the two PDFs of `.github/scripts/encrypted-pdfs`:

```sh
go run ./.github/scripts/encrypted-pdfs viewer-files-to-test
```

Example_30 opens with the user password `hello` and the owner password
`world`; `Encrypted_Cyrillic.pdf` with `пароль`; `Encrypted_200_Bytes.pdf`
with `0123456789` typed 20 times. Build them again from the frozen main
before the pass of Oct 15-20.
