# Benchmarks

PDFjet for Java measured against iText Core (AGPL) and Apache PDFBox, with the
programs, the method and the results that `jet-vs-box.html` quotes.

| File | What it is |
|---|---|
| `TextBench.java` | A multilingual document: pages of 60 lines of Latin, Greek and Cyrillic text |
| `BigTableBench.java` | Example_43: a 2,546-page table from a 124,716-row CSV file |
| `pom.xml` | iText Core 9.7.1 and PDFBox 3.0.8, which `run.sh` copies into `build/lib` |
| `run.sh` | Builds PDFjet from this checkout and the benchmarks, runs them and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs; the text document below is from `2026-09-16-7dbfee5d-text.log` and the table from `2026-09-16-7dbfee5d-table.log` |

## Running

```
benchmarks/run.sh              # both benchmarks, about 25 minutes
benchmarks/run.sh text         # the text document only, about 10 minutes
benchmarks/run.sh table        # Example_43 only, about 13 minutes
benchmarks/run.sh all --quick  # a short run that checks that everything works
TABLE_CONFIGS="jet jet-table it-layout" benchmarks/run.sh table   # some of the table configurations
```

It needs a JDK (21 was used), Maven (`MVN=/path/to/mvn` when it is not on the
path), curl and GNU time, and mutool for the checks of the sample files. PDFBox
cannot embed the OpenType (CFF) IBM Plex Sans in `fonts/`, so `run.sh`
downloads IBM's TrueType build of it into `build/fonts`. Nothing else should run
on the machine meanwhile. Each run writes its own `build/results-<date>-<time>.log`,
and `build/` is not tracked.

## Method

- Each configuration runs in its own JVM: 2 warm-up runs and 7 measured runs,
  or 3 measured runs and no warm-up when a run takes more than 5 seconds. The
  median is reported.
- Allocations are the bytes the thread allocated per document
  (`ThreadMXBean.getCurrentThreadAllocatedBytes`).
- The text document is written to memory with `-Xmx4g`; the table is written
  through an 8 MB buffer, as Example_43 writes it, to a stream that only counts
  the bytes, with `-Xmx8g`.
- First run: a new JVM, timed from the start of `main`.
- Peak memory: the largest resident size, from `/usr/bin/time`, of a new JVM
  at its defaults writing one document, the median of 3 runs. It includes the
  JVM itself.
- Smallest heap (the table only): the smallest of `-Xmx` 32 MB to 8,192 MB, in
  steps of a factor of two, with which the document is finished.
- iText and PDFBox draw with their low-level APIs, `PdfCanvas` and
  `PDPageContentStream`, one call per line of text or per cell, as PDFjet does.
  For the table, iText also runs with its own `Table` in large-table mode: added
  to the `Document` before its rows, flushed every 50 rows, with header cells,
  and with immediate flushing off so that the "Page i of N" footers can be added
  before the document is closed.
- `jet-table` is PDFjet's own `Table` against that one: the same 9 columns built
  as `Cell` objects with the widths the driver measures, the header row repeated
  on every page, alternate rows shaded and a rule above each row. Neither table
  API draws the vertical rules that `BigTable` draws, and both hold every cell
  of the file. The two produce the same 2,546 pages as `BigTable`.
- In `BigTableBench`, PDFjet's `Page` (`jet-page`), iText's `PdfCanvas` and
  PDFBox draw through one driver that follows `BigTable` step by step: two
  passes over the file, column widths measured from every field with the header
  font, the same row pitch (from PDFjet's metrics of IBM Plex Sans), shading,
  lines, headers and footers. Like `BigTable`, they keep every page until the
  footers are drawn at the end.
- `jet-page-stream` and `it-canvas-stream` take the number of pages from the
  first pass and draw each footer with its page, so that each page is written
  when the next one starts: PDFjet adds each page to the PDF as it is created,
  and iText flushes the previous page.
- The sample files are checked for their page count, their text (with mutool)
  and the last footer.

## Results, 16 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1. Both benchmarks ran
at 7dbfee5d, one after the other, on a freshly rebooted machine with nothing
else running: `results/2026-09-16-7dbfee5d-table.log` and
`results/2026-09-16-7dbfee5d-text.log`. iText and PDFBox, whose code did not
change, came within 2% of their 4ee4e7cb figures on the text document. The
run at d2f5d4cb, on a machine that had not been rebooted, had them 6 to 22%
slower on its short runs, so its text figures were not quoted.

### The text document, 500 pages

| Configuration | Time | File | Allocated | Peak memory |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 54 ms | 533,287 bytes | 25 MB | 79 MB |
| PDFjet, Noto Sans | 57 ms | 804,928 bytes | 28 MB | 79 MB |
| iText, IBM Plex Sans subset | 72 ms | 501,395 bytes | 74 MB | 133 MB |
| iText, IBM Plex Sans full | 76 ms | 537,005 bytes | 79 MB | 139 MB |
| iText, Noto Sans subset | 75 ms | 496,442 bytes | 71 MB | 135 MB |
| iText, Noto Sans full | 98 ms | 807,159 bytes | 81 MB | 139 MB |
| PDFBox, Noto Sans subset | 21,401 ms | 426,467 bytes | 38,262 MB | 384 MB |
| PDFBox, Noto Sans full | 19,925 ms | 728,346 bytes | 38,241 MB | 382 MB |

PDFjet's files are 6% smaller and its time 5 ms shorter than at 4ee4e7cb (59 ms
and 567,688 bytes with IBM Plex Sans), as d2f5d4cb writes a color or a font
only when it changes. With each page flushed when the next one starts, iText
peaked at 126 MB with IBM Plex Sans and 134 MB with Noto Sans; in a separate
check after the 4ee4e7cb run, flushing took it a little longer: 77 against
75 ms with IBM Plex Sans and 79 against 75 ms with Noto Sans.

| Time by length | 50 pages | 100 pages | 200 pages | 500 pages |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 8 ms | 13 ms | 20 ms | 54 ms |
| iText, IBM Plex Sans subset | 14 ms | 26 ms | 49 ms | 72 ms |
| PDFBox, Noto Sans subset | 2,174 ms | 4,307 ms | 8,448 ms | 21,401 ms |

First document in a new JVM, 20 pages: PDFjet 88 ms, iText 218 ms, PDFBox 1,292 ms.
Jars with their dependencies: PDFjet 398,253 bytes, iText 5,752,771 bytes,
PDFBox 3,845,317 bytes.

### Example_43, 2,546 pages

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File |
|---|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,599 ms | 1,959 ms | 674 MB | 390 MB | 32 MB | 11.7 MB |
| PDFjet `Page` | 1,579 ms | 1,910 ms | 628 MB | 469 MB | 128 MB | 11.5 MB |
| iText `PdfCanvas` | 2,052 ms | 2,649 ms | 1,109 MB | 846 MB | 256 MB | 11.8 MB |
| iText `Table` | 34,249 ms | 36,609 ms | 47,178 MB | 6,949 MB | 8,192 MB | 21.6 MB |
| PDFBox, content stream | 12,664 ms | 13,561 ms | 25,200 MB | 548 MB | 128 MB | 11.9 MB |
| PDFjet `Page`, page by page | 1,546 ms | 1,876 ms | 628 MB | 376 MB | 32 MB | 11.5 MB |
| iText `PdfCanvas`, page by page | 2,045 ms | 2,560 ms | 1,133 MB | 405 MB | 32 MB | 11.8 MB |

All seven files have 2,546 pages and end with "Page 2546 of 2546". The iText and
PDFBox files are the same size as at 3ebd321b, and their times are within 5% of
the run at d2f5d4cb. PDFBox's peak memory falls into one of two groups from run
to run, 465 to 485 MB or 557 to 563 MB, with the same code; this run is in the
second. PDFjet's files are smaller: d2f5d4cb writes a color, a pen width or a
font only when it changes and fills a rectangle with one `re` operator, which
took `Page` from 12.6 to 11.5 MB, and 7dbfee5d fills the shaded rows of
`BigTable` with one `re` too, which took it from 12.3 MB at 3ebd321b to 12.1 at
d2f5d4cb and 11.7 now. Pages 1, 1273 and 2546 drawn on PDFjet's `Page`, kept
or page by page, render the same as `BigTable`'s at 72 dpi, pixel for pixel.

Earlier runs of the same programs: d2f5d4cb, where `BigTable` took 1,671 ms and
778 MB and wrote 12.1 MB, and `Table` 3,672 ms (`results/2026-09-16-d2f5d4cb.log`,
whose text figures are not quoted); 3ebd321b, the table before d2f5d4cb, where
`BigTable` took 1,703 ms and 761 MB, `Page` 1,800 ms and 815 MB in a 256 MB
heap, and `Table` 5,099 ms and 2,255 MB in a 1,024 MB heap
(`results/2026-09-16-3ebd321b-table.log`); 4ee4e7cb, the previous run of the
text document, at 59 ms, where `BigTable` read its file with the first RFC 4180
parser, at 1,761 ms and 809 MB (`results/2026-09-15-4ee4e7cb.log`); f72b9f08, which counts
the pages of a `BigTable` and writes each one as it is finished, so that
`BigTable` needs a 32 MB heap rather than 256 (`results/2026-09-15-f72b9f08.log`);
312697d5, which places a string with `Td` (`results/2026-09-15-312697d5.log`);
7c4988ad, which writes page content without an array for each string and number,
and measured the text document at 59 ms (`results/2026-09-15-7c4988ad.log`); and
d963a7c8 before all three, at 71 ms (`results/2026-09-15-d963a7c8.log`).

### The two table APIs, 16 September 2026

`Table` against iText's `Table` on the same data, in the same run as the rows
above (`results/2026-09-16-7dbfee5d-table.log`). It was first measured at
542b3dbf (`results/2026-09-16-542b3dbf-table.log`) and again at 3ebd321b, at
5,038 and 5,099 ms and 25.2 MB, before d2f5d4cb, and at d2f5d4cb at 3,672 ms.

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File | Code |
|---|---:|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,599 ms | 1,959 ms | 674 MB | 390 MB | 32 MB | 11.7 MB | 17 |
| PDFjet `Table` | 3,607 ms | 4,467 ms | 1,821 MB | 1,330 MB | 512 MB | 21.2 MB | 153 |
| iText `Table` | 34,249 ms | 36,609 ms | 47,178 MB | 6,949 MB | 8,192 MB | 21.6 MB | 167 |

- PDFjet's `Table` is 9.5 times faster than iText's, 3,607 against 34,249 ms,
  allocates a twenty-sixth as much, 1,821 against 47,178 MB, and finishes in a
  512 MB heap where iText's needs 8,192. Both are given the same column widths
  and draw the same 2,546 pages.
- It writes the smaller file, 21.2 MB against 21.6. Until d2f5d4cb it wrote
  25.2 MB: both table APIs draw per cell, and PDFjet's `Cell` set the brush and
  the pen for every cell (2,027 `rg` and `RG` operators on page 1 of the sample
  against iText's 675), wrote the font and the pen width again for every cell,
  and filled each shaded cell with a path of four operators. A page now writes a
  color, a width or a font only when it changes and fills a rectangle with one
  `re`, which also took `Table` from 5,099 to 3,672 ms and from a 1,024 MB heap
  to 512.
- Against `BigTable` on the same data, `Table` is 2.3 times slower, allocates
  2.7 times as much, needs 16 times the heap and writes 82% more, and it is 153
  lines of program against 17. It is the API to reach for when the rows are
  already in memory and the table is not enormous; `BigTable` is the one for a
  file this size.

### What the numbers say

- PDFjet is faster than iText's low-level API at every length: 54 against 72 ms
  for 500 pages and 13 against 26 ms for 100. It allocates about a third as much,
  with a lower peak, a faster start and a jar a fourteenth of the size. PDFBox is
  about 375 times slower on the text document with Noto Sans, whose Devanagari
  substitution rules it applies to every line, and 7.9 times slower than
  `BigTable` on the table.
- On the table, `BigTable` takes 22% less time than iText's `PdfCanvas` (1,599
  against 2,052 ms), and iText's own `Table` is 21 times slower and needs an
  8,192 MB heap. The programs that draw this table are 17 lines for `BigTable`,
  215 for `PdfCanvas`, 167 for iText's `Table` and 217 for PDFBox, without the
  blank lines and the comments.
- Drawn with the same calls, PDFjet is ahead of iText too: on `Page` the table
  takes 23% less time than on `PdfCanvas` and allocates 43% less; page by page,
  24% less time and 45% less.
- `BigTable` is within 1.3% of the same drawing on `Page`, 1,599 against 1,579
  ms. At d2f5d4cb `Page` was 5% faster, 1,584 against 1,671 ms, and wrote 11.5
  MB against 12.1: the driver filled each shaded row with `fillRect`, one `re`,
  where `BigTable` filled it with a path of four operators. 7dbfee5d fills the
  rows of `BigTable` with one `re` too, which took it to 11.7 MB, 1,599 ms and
  674 MB allocated. `BigTable` still allocates more than `Page`, 674 against
  628 MB, as it reads the quoted fields of its file as RFC 4180 does, which the
  driver does not.
- Memory depends on keeping the pages, not on the library. A page that has not
  been added to the PDF holds its content uncompressed, 31,338 bytes a page, or
  79.8 MB for the 2,546 pages against 10.7 MB compressed, so keeping them all
  costs a 128 MB heap on `Page` and 256 MB on `PdfCanvas`; it was 38,307 bytes a
  page and a 256 MB heap on `Page` before d2f5d4cb. `BigTable` counts its pages
  in the pass that measures the columns, draws each footer when its page is
  finished and writes the page out, so it needs 32 MB and peaks at 390 MB.
  PDFBox compresses each page as it is closed but writes the document only when
  it is saved: 128 MB and 548 MB. Drawn page by page, `Page` and `PdfCanvas` also
  finish in 32 MB.

## Caveats

- One machine and one JDK; absolute times will differ elsewhere.
- PDFBox cannot embed the OpenType IBM Plex Sans: the text document uses Noto
  Sans for it, and the table IBM's TrueType build of IBM Plex Sans.
- iText's layout API was measured only for the table.
- The four ports of PDFjet are compared with each other in section 10 of
  `jet-vs-box.html`, with programs of their own in `ports/`, built with each
  port's own toolchain. `ports/run.sh` runs them.
