# Benchmarks

PDFjet for Java measured against iText Core (AGPL) and Apache PDFBox, with the
programs, the method and the results that `jet-vs-box.html` quotes.

| File | What it is |
|---|---|
| `TextBench.java` | A multilingual document: pages of 60 lines of Latin, Greek and Cyrillic text |
| `BigTableBench.java` | Example_43: a 2,546-page table from a 124,716-row CSV file |
| `pom.xml` | iText Core 9.7.1 and PDFBox 3.0.8, which `run.sh` copies into `build/lib` |
| `run.sh` | Builds PDFjet from this checkout and the benchmarks, runs them and writes `build/results-<date>.log` |

## Running

```
benchmarks/run.sh              # both benchmarks, about 25 minutes
benchmarks/run.sh text         # the text document only, about 10 minutes
benchmarks/run.sh table        # Example_43 only, about 13 minutes
benchmarks/run.sh all --quick  # a short run that checks that everything works
```

It needs a JDK (21 was used), Maven (`MVN=/path/to/mvn` when it is not on the
path), curl and GNU time, and mutool for the checks of the sample files. PDFBox
cannot embed the OpenType (CFF) IBM Plex Sans in `fonts/`, so `run.sh`
downloads IBM's TrueType build of it into `build/fonts`. Nothing else should run
on the machine meanwhile. `build/` is not tracked.

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
- In `BigTableBench`, iText and PDFBox draw through one driver that follows
  `BigTable` step by step: two passes over the file, column widths measured from
  every field with the header font, the same row pitch (from PDFjet's metrics of
  IBM Plex Sans), shading, lines, headers and footers.
- The sample files are checked for their page count, their text (with mutool)
  and the last footer.

## Results, 15 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1, PDFjet at 5c0ca7e1.

### The text document, 500 pages

| Configuration | Time | File | Allocated | Peak memory |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 72 ms | 571,768 bytes | 36 MB | 90 MB |
| PDFjet, Noto Sans | 75 ms | 848,576 bytes | 39 MB | 95 MB |
| iText, IBM Plex Sans subset | 71 ms | 501,395 bytes | 74 MB | 132 MB |
| iText, IBM Plex Sans full | 74 ms | 537,005 bytes | 80 MB | 137 MB |
| iText, Noto Sans subset | 73 ms | 496,442 bytes | 71 MB | 135 MB |
| iText, Noto Sans full | 98 ms | 807,159 bytes | 81 MB | 135 MB |
| PDFBox, Noto Sans subset | 21,021 ms | 426,467 bytes | 38,262 MB | 383 MB |
| PDFBox, Noto Sans full | 19,904 ms | 728,346 bytes | 38,241 MB | 384 MB |

With each page flushed when the next one starts, iText peaked at 125 MB with
IBM Plex Sans and 133 MB with Noto Sans.

| Time by length | 50 pages | 100 pages | 200 pages | 500 pages |
|---|---:|---:|---:|---:|
| PDFjet, IBM Plex Sans | 11 ms | 17 ms | 31 ms | 72 ms |
| iText, IBM Plex Sans subset | 12 ms | 26 ms | 47 ms | 71 ms |
| PDFBox, Noto Sans subset | 2,201 ms | 4,258 ms | 8,569 ms | 21,021 ms |

First document in a new JVM, 20 pages: PDFjet 93 ms, iText 226 ms, PDFBox 1,293 ms.
Jars with their dependencies: PDFjet 390,622 bytes, iText 5,752,771 bytes,
PDFBox 3,845,317 bytes.

### Example_43, 2,546 pages

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File |
|---|---:|---:|---:|---:|---:|---:|
| PDFjet `BigTable` | 1,945 ms | 2,433 ms | 1,308 MB | 860 MB | 256 MB | 12.3 MB |
| iText `PdfCanvas` | 2,030 ms | 2,607 ms | 1,109 MB | 810 MB | 256 MB | 11.8 MB |
| iText `Table` | 35,341 ms | 37,559 ms | 47,121 MB | 7,008 MB | 8 GB | 21.6 MB |
| PDFBox, content stream | 12,507 ms | 13,280 ms | 25,302 MB | 459 MB | 128 MB | 11.9 MB |

All four files have 2,546 pages and end with "Page 2546 of 2546".

### What the numbers say

- PDFjet and iText's low-level API take the same time for long documents; PDFjet
  is faster on shorter ones and allocates half as much, with a lower peak, a
  faster start and a jar a fifteenth of the size. PDFBox is 290 times slower on
  the text document with Noto Sans, whose Devanagari substitution rules it
  applies to every line, and 6.4 times slower on the table.
- On the table, PDFjet is the fastest, and iText's own `Table` is 18 times
  slower than PDFjet and needs an 8 GB heap.
- PDFjet does not lead on memory for the table. `BigTable` keeps all its pages
  until the footers are drawn, and a page that has not been added to the PDF
  holds its content uncompressed: 97.5 MB for the 2,546 pages, against 11.6 MB
  compressed. PDFBox compresses each page as it is closed and finishes in half
  the heap. Compressing a detached page's content when it is finished is the fix.

## Caveats

- One machine and one JDK; absolute times will differ elsewhere.
- PDFBox cannot embed the OpenType IBM Plex Sans: the text document uses Noto
  Sans for it, and the table IBM's TrueType build of IBM Plex Sans.
- iText's layout API was measured only for the table.
- The four ports of PDFjet are compared with each other in section 10 of
  `jet-vs-box.html`, with each port's own build, not with these programs.
