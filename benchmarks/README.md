# Benchmarks

PDFjet for Java measured against iText Core (AGPL) and Apache PDFBox, with the
programs, the method and the results that `jet-vs-box.html` quotes.

| File | What it is |
|---|---|
| `TextBench.java` | A multilingual document: pages of 60 lines of Latin, Greek and Cyrillic text |
| `BigTableBench.java` | Example_43: a 2,546-page table from a 124,716-row CSV file |
| `pom.xml` | iText Core 9.7.1 and PDFBox 3.0.8, which `run.sh` copies into `build/lib` |
| `run.sh` | Builds PDFjet from this checkout and the benchmarks, runs them and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs; the results below are from `2026-09-15-7c4988ad.log` |

## Running

```
benchmarks/run.sh              # both benchmarks, about 25 minutes
benchmarks/run.sh text         # the text document only, about 10 minutes
benchmarks/run.sh table        # Example_43 only, about 13 minutes
benchmarks/run.sh all --quick  # a short run that checks that everything works
TABLE_CONFIGS="jet jet-page" benchmarks/run.sh table   # some of the table configurations
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
- Smallest heap (the table only): the smallest of `-Xmx` 32 MB to 8 GB, in
  steps of a factor of two, with which the document is finished.
- iText and PDFBox draw with their low-level APIs, `PdfCanvas` and
  `PDPageContentStream`, one call per line of text or per cell, as PDFjet does.
  For the table, iText also runs with its own `Table` in large-table mode: added
  to the `Document` before its rows, flushed every 50 rows, with header cells,
  and with immediate flushing off so that the "Page i of N" footers can be added
  before the document is closed.
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

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1, PDFjet at 7c4988ad, in one run with
nothing else running. Its log is `results/2026-09-15-7c4988ad.log`.

### The text document, 500 pages

| Configuration | Time | File | Allocated | Peak memory |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 59 ms | 571,768 bytes | 26 MB | 88 MB |
| PDFjet, Noto Sans | 62 ms | 848,576 bytes | 29 MB | 86 MB |
| iText, IBM Plex Sans subset | 73 ms | 501,395 bytes | 74 MB | 135 MB |
| iText, IBM Plex Sans full | 75 ms | 537,005 bytes | 79 MB | 140 MB |
| iText, Noto Sans subset | 74 ms | 496,442 bytes | 71 MB | 138 MB |
| iText, Noto Sans full | 98 ms | 807,159 bytes | 81 MB | 137 MB |
| PDFBox, Noto Sans subset | 20,923 ms | 426,467 bytes | 38,262 MB | 383 MB |
| PDFBox, Noto Sans full | 19,944 ms | 728,346 bytes | 38,241 MB | 384 MB |

With each page flushed when the next one starts, iText peaked at 125 MB with
IBM Plex Sans and 133 MB with Noto Sans.

| Time by length | 50 pages | 100 pages | 200 pages | 500 pages |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 8 ms | 15 ms | 22 ms | 59 ms |
| iText, IBM Plex Sans subset | 12 ms | 24 ms | 50 ms | 73 ms |
| PDFBox, Noto Sans subset | 2,133 ms | 4,367 ms | 8,340 ms | 20,923 ms |

First document in a new JVM, 20 pages: PDFjet 88 ms, iText 213 ms, PDFBox 1,282 ms.
Jars with their dependencies: PDFjet 392,236 bytes, iText 5,752,771 bytes,
PDFBox 3,845,317 bytes.

### Example_43, 2,546 pages

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File |
|---|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,727 ms | 2,183 ms | 759 MB | 751 MB | 256 MB | 12.3 MB |
| PDFjet `Page` | 1,856 ms | 2,287 ms | 816 MB | 756 MB | 256 MB | 12.7 MB |
| iText `PdfCanvas` | 2,065 ms | 2,648 ms | 1,109 MB | 822 MB | 256 MB | 11.8 MB |
| iText `Table` | 33,992 ms | 36,869 ms | 47,245 MB | 7,265 MB | 8 GB | 21.6 MB |
| PDFBox, content stream | 12,381 ms | 13,343 ms | 25,200 MB | 545 MB | 128 MB | 11.9 MB |
| PDFjet `Page`, page by page | 1,843 ms | 2,159 ms | 816 MB | 394 MB | 32 MB | 12.7 MB |
| iText `PdfCanvas`, page by page | 2,037 ms | 2,602 ms | 1,133 MB | 407 MB | 32 MB | 11.8 MB |

All seven files have 2,546 pages and end with "Page 2546 of 2546". Pages 1,
1273 and 2546 drawn on PDFjet's `Page`, kept or page by page, render the same as
`BigTable`'s at 72 dpi, pixel for pixel.

Before 7c4988ad, which writes page content without an array for each string and
number, a run at d963a7c8 (`results/2026-09-15-d963a7c8.log`) measured the text
document at 71 ms and 36 MB, `BigTable` at 1,930 ms and 1,282 MB, and `Page` at
2,128 ms and 1,460 MB.

### What the numbers say

- PDFjet is faster than iText's low-level API at every length: 59 against 73 ms
  for 500 pages and 15 against 24 ms for 100. It allocates about a third as much,
  with a lower peak, a faster start and a jar a fifteenth of the size. PDFBox is
  about 340 times slower on the text document with Noto Sans, whose Devanagari
  substitution rules it applies to every line, and 7.2 times slower on the table.
- On the table, `BigTable` takes 16% less time than iText's `PdfCanvas` (1,727
  against 2,065 ms), and iText's own `Table` is 20 times slower and needs an 8 GB
  heap.
- Drawn with the same calls, PDFjet is ahead of iText too: on `Page` the table
  takes 10% less time than on `PdfCanvas` and allocates 26% less; page by page,
  10% less time and 28% less. `BigTable` takes 7% less time than the same drawing
  on `Page`: for each string, `Page.drawString` writes a text matrix and the text
  color and makes two arrays for the color, where `BigTable` writes only a
  position.
- Memory for the table depends on keeping the pages, not on the library. With
  every page kept for the footers, PDFjet and iText need a 256 MB heap: a PDFjet
  page that has not been added to the PDF holds its content uncompressed, 97.5 MB
  for the 2,546 pages against 11.6 MB compressed, and PDFBox, which compresses
  each page as it is closed, finishes in 128 MB. Written page by page, PDFjet and
  iText finish in 32 MB. For `BigTable`, whose pages are kept for the footers,
  the proposed fix is to compress a detached page's content when it is finished;
  it has not been measured.

## Caveats

- One machine and one JDK; absolute times will differ elsewhere.
- PDFBox cannot embed the OpenType IBM Plex Sans: the text document uses Noto
  Sans for it, and the table IBM's TrueType build of IBM Plex Sans.
- iText's layout API was measured only for the table.
- The four ports of PDFjet are compared with each other in section 10 of
  `jet-vs-box.html`, with each port's own build, not with these programs.
