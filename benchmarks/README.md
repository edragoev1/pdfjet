# Benchmarks

PDFjet for Java measured against iText Core (AGPL) and Apache PDFBox, with the
programs, the method and the results that `jet-vs-box.html` quotes.

| File | What it is |
|---|---|
| `TextBench.java` | A multilingual document: pages of 60 lines of Latin, Greek and Cyrillic text |
| `BigTableBench.java` | Example_43: a 2,546-page table from a 124,716-row CSV file |
| `pom.xml` | iText Core 9.7.1 and PDFBox 3.0.8, which `run.sh` copies into `build/lib` |
| `run.sh` | Builds PDFjet from this checkout and the benchmarks, runs them and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs; the text document below is from `2026-09-15-4ee4e7cb.log` and the table from `2026-09-16-3ebd321b-table.log` |

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

## Results, 15 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1, with nothing else running.
The text document is one run at 4ee4e7cb on a freshly rebooted machine,
`results/2026-09-15-4ee4e7cb.log`; the table is one run of the table benchmark
at 3ebd321b on 16 September, `results/2026-09-16-3ebd321b-table.log`.

### The text document, 500 pages

| Configuration | Time | File | Allocated | Peak memory |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 59 ms | 567,688 bytes | 26 MB | 83 MB |
| PDFjet, Noto Sans | 61 ms | 843,926 bytes | 29 MB | 84 MB |
| iText, IBM Plex Sans subset | 72 ms | 501,395 bytes | 74 MB | 132 MB |
| iText, IBM Plex Sans full | 76 ms | 537,005 bytes | 79 MB | 136 MB |
| iText, Noto Sans subset | 74 ms | 496,442 bytes | 71 MB | 134 MB |
| iText, Noto Sans full | 97 ms | 807,159 bytes | 81 MB | 136 MB |
| PDFBox, Noto Sans subset | 21,323 ms | 426,467 bytes | 38,309 MB | 382 MB |
| PDFBox, Noto Sans full | 20,237 ms | 728,346 bytes | 38,251 MB | 380 MB |

With each page flushed when the next one starts, iText peaked at 126 MB with
IBM Plex Sans and 133 MB with Noto Sans, and took a little longer: 77 against
75 ms with IBM Plex Sans and 79 against 75 ms with Noto Sans, in a separate
check after the run.

| Time by length | 50 pages | 100 pages | 200 pages | 500 pages |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 8 ms | 14 ms | 22 ms | 59 ms |
| iText, IBM Plex Sans subset | 13 ms | 26 ms | 47 ms | 72 ms |
| PDFBox, Noto Sans subset | 2,188 ms | 4,298 ms | 8,685 ms | 21,323 ms |

First document in a new JVM, 20 pages: PDFjet 86 ms, iText 213 ms, PDFBox 1,289 ms.
Jars with their dependencies: PDFjet 392,834 bytes, iText 5,752,771 bytes,
PDFBox 3,845,317 bytes.

### Example_43, 2,546 pages

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File |
|---|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,703 ms | 2,065 ms | 761 MB | 389 MB | 32 MB | 12.3 MB |
| PDFjet `Page` | 1,800 ms | 2,219 ms | 815 MB | 759 MB | 256 MB | 12.6 MB |
| iText `PdfCanvas` | 2,046 ms | 2,678 ms | 1,109 MB | 835 MB | 256 MB | 11.8 MB |
| iText `Table` | 35,304 ms | 36,965 ms | 47,220 MB | 7,104 MB | 8,192 MB | 21.6 MB |
| PDFBox, content stream | 12,370 ms | 13,387 ms | 25,124 MB | 467 MB | 128 MB | 11.9 MB |
| PDFjet `Page`, page by page | 1,744 ms | 2,083 ms | 815 MB | 384 MB | 32 MB | 12.6 MB |
| iText `PdfCanvas`, page by page | 2,020 ms | 2,551 ms | 1,107 MB | 405 MB | 32 MB | 11.8 MB |

All seven files have 2,546 pages and end with "Page 2546 of 2546", and every
sample file is the same size as at f72b9f08. Pages 1, 1273 and 2546 drawn on
PDFjet's `Page`, kept or page by page, render the same as `BigTable`'s at
72 dpi, pixel for pixel.

Earlier runs of the same programs: 4ee4e7cb, the run the text document is
from, where `BigTable` read its file with the first RFC 4180 parser, at 1,761
ms and 809 MB, and every other configuration came within 3% of the run above
(`results/2026-09-15-4ee4e7cb.log`); f72b9f08, which counts the pages of a
`BigTable` and writes each one as it is finished, so that `BigTable` needs a
32 MB heap rather than 256 (`results/2026-09-15-f72b9f08.log`); 312697d5, which
places a string with `Td` (`results/2026-09-15-312697d5.log`); 7c4988ad, which
writes page content without an array for each string and number, and measured
the text document at 59 ms (`results/2026-09-15-7c4988ad.log`); and d963a7c8
before all three, at 71 ms (`results/2026-09-15-d963a7c8.log`).

### The two table APIs, 16 September 2026

`Table` against iText's `Table` on the same data, in the same run of the
table benchmark as the rows above (`results/2026-09-16-3ebd321b-table.log`).
It was first measured at 542b3dbf (`results/2026-09-16-542b3dbf-table.log`),
where `Table` came out at 5,038 ms and iText's at 35,258, within 2% of these.

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File | Code |
|---|---:|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,703 ms | 2,065 ms | 761 MB | 389 MB | 32 MB | 12.3 MB | 17 |
| PDFjet `Table` | 5,099 ms | 5,706 ms | 2,255 MB | 1,748 MB | 1,024 MB | 25.2 MB | 153 |
| iText `Table` | 35,304 ms | 36,965 ms | 47,220 MB | 7,104 MB | 8,192 MB | 21.6 MB | 167 |

- PDFjet's `Table` is 6.9 times faster than iText's, 5,099 against 35,304 ms,
  allocates a twenty-first as much, 2,255 against 47,220 MB, and finishes in a
  1 GB heap where iText's needs 8. Both are given the same column widths and
  draw the same 2,546 pages.
- It writes a larger file, 25.2 MB against iText's 21.6. Both table APIs draw
  per cell rather than per row: on page 1 of the sample each writes 225 fills
  for the shaded cells, where `BigTable` writes 25, one per shaded row. What
  PDFjet adds on top is the colour: 2,027 `rg` and `RG` operators against
  iText's 675, because `Cell` sets the brush and the pen for every cell it
  draws. That is the first place to look if this file size is worth closing.
- Against `BigTable` on the same data, `Table` is 3.0 times slower, allocates
  3.0 times as much, needs 32 times the heap and writes twice the file, and it
  is 153 lines of program against 17. It is the API to reach for when the rows
  are already in memory and the table is not enormous; `BigTable` is the one
  for a file this size.

### What the numbers say

- PDFjet is faster than iText's low-level API at every length: 59 against 72 ms
  for 500 pages and 14 against 26 ms for 100. It allocates about a third as much,
  with a lower peak, a faster start and a jar a fifteenth of the size. PDFBox is
  about 350 times slower on the text document with Noto Sans, whose Devanagari
  substitution rules it applies to every line, and 7.3 times slower on the table.
- On the table, `BigTable` takes 17% less time than iText's `PdfCanvas` (1,703
  against 2,046 ms), and iText's own `Table` is 21 times slower and needs an
  8,192 MB heap. The programs that draw this table are 17 lines for `BigTable`,
  215 for `PdfCanvas`, 167 for iText's `Table` and 217 for PDFBox, without the
  blank lines and the comments.
- Drawn with the same calls, PDFjet is ahead of iText too: on `Page` the table
  takes 12% less time than on `PdfCanvas` and allocates 27% less; page by page,
  14% less time and 26% less. `BigTable` takes 5% less time than the same drawing
  on `Page`: both write only a position for each string, but `Page.drawString`
  also writes the text color and makes two arrays for it.
- `BigTable` allocates 7% less than that drawing, 761 against 815 MB. Since
  4ee4e7cb it reads the quoted fields of its data file as RFC 4180 does, which
  the driver of the other configurations does not. The first parser copied every
  field and took it from 754 MB and 1,726 ms at f72b9f08 to 809 MB and 1,761;
  3ebd321b takes the fields as substrings of the line, 761 MB and 1,703 ms.
- Memory depends on keeping the pages, not on the library. A page that has not
  been added to the PDF holds its content uncompressed, 97.5 MB for the 2,546
  pages against 11.6 MB compressed, so keeping them all costs a 256 MB heap, as
  drawing on `Page` and on `PdfCanvas` does. `BigTable` counts its pages in the
  pass that measures the columns, draws each footer when its page is finished and
  writes the page out, so it needs 32 MB and peaks at 389 MB. PDFBox compresses
  each page as it is closed but writes the document only when it is saved: 128 MB
  and 467 MB. Drawn page by page, `Page` and `PdfCanvas` also finish in 32 MB.

## Caveats

- One machine and one JDK; absolute times will differ elsewhere.
- PDFBox cannot embed the OpenType IBM Plex Sans: the text document uses Noto
  Sans for it, and the table IBM's TrueType build of IBM Plex Sans.
- iText's layout API was measured only for the table.
- The four ports of PDFjet are compared with each other in section 10 of
  `jet-vs-box.html`, with programs of their own in `ports/`, built with each
  port's own toolchain. `ports/run.sh` runs them.
