# Benchmarks

PDFjet for Java measured on its own, run after run, to track its progress, with
the programs, the method and the results that `pdfjet-benchmarks.html` quotes.

| File | What it is |
|---|---|
| `TextBench.java` | A multilingual document: pages of 60 lines of Latin, Greek and Cyrillic text |
| `BigTableBench.java` | Example_43: a 2,546-page table from a 124,716-row CSV file |
| `run.sh` | Builds PDFjet from this checkout and the benchmarks, runs them and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs; the figures below are from `2026-09-18-e662e6bc.log` |
| `ports/` | The four ports of PDFjet on the text document; see `ports/README.md` |
| `table/` | `Table` in the four ports; see `table/README.md` |

## Running

```
benchmarks/run.sh              # both benchmarks, a few minutes
benchmarks/run.sh text         # the text document only
benchmarks/run.sh table        # Example_43 only
benchmarks/run.sh all --quick  # a short run that checks that everything works
TABLE_CONFIGS="jet jet-table" benchmarks/run.sh table   # some of the table configurations
```

It needs a JDK (21 was used) and GNU time, and mutool for the checks of the
sample files. The programs are compiled against the PDFjet classes of this
checkout and nothing else. Nothing else should run on the machine meanwhile.
Each run writes its own `build/results-<date>-<time>.log`, and `build/` is not
tracked.

The text document has two configurations: `jet-plex`, with IBM Plex Sans
(`IBMPlexSans-Regular.otf.stream`), and `jet-noto`, with Noto Sans
(`NotoSans-Regular.ttf.stream`). The table has four:

| Configuration | What it draws with |
|---|---|
| `jet` | Example_43 as it is, with `BigTable` |
| `jet-table` | `Table`, the same 9 columns built as `Cell` objects |
| `jet-page` | `Page`, through a driver that follows `BigTable` step by step, every page kept until the footers are drawn |
| `jet-page-stream` | `Page`, through the same driver, each page written when the next one starts |

## Method

- Each configuration runs in its own JVM: 2 warm-up runs and 7 measured runs,
  or 3 measured runs and no warm-up when a run takes more than 5 seconds. The
  median is reported.
- Allocations are the bytes the thread allocated per document
  (`ThreadMXBean.getCurrentThreadAllocatedBytes`).
- The text document is written to memory with `-Xmx4g`, one `drawString` call
  per line; the table is written through an 8 MB buffer, as Example_43 writes
  it, to a stream that only counts the bytes, with `-Xmx8g`.
- First run: a new JVM, timed from the start of `main`.
- Peak memory: the largest resident size, from `/usr/bin/time`, of a new JVM
  at its defaults writing one document, the median of 3 runs. It includes the
  JVM itself.
- Smallest heap (the table only): the smallest of `-Xmx` 32 MB to 8,192 MB, in
  steps of a factor of two, with which the document is finished.
- `jet-table` builds the same 9 columns as `Cell` objects with the widths the
  driver measures, the header row repeated on every page, alternate rows shaded
  and a rule above each row. It draws no vertical rules, which `BigTable` draws,
  and it holds every cell of the file. It produces the same 2,546 pages as
  `BigTable`.
- `jet-page` draws on `Page` through a driver that follows `BigTable` step by
  step: two passes over the file, column widths measured from every field with
  the header font, the same row pitch (from PDFjet's metrics of IBM Plex Sans),
  shading, lines, headers and footers. Like `BigTable`, it keeps every page
  until the footers are drawn at the end.
- `jet-page-stream` takes the number of pages from the first pass and draws
  each footer with its page, so that each page is added to the PDF as it is
  created and written when the next one starts.
- The sample files are checked for their page count, their text (with mutool)
  and the last footer.

## Results, 18 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1. Both benchmarks ran
at e662e6bc, the v9.0.1 release, in one run with nothing else running:
`results/2026-09-18-e662e6bc.log`. The logs of earlier runs also hold lines of
configurations that the benchmarks no longer run.

e662e6bc keeps the whole font in every `.otf.stream` and the GPOS marks of
every stream font, read the first time a mark is drawn, and looks at a string
once, rather than four times, to tell if it needs shaping. Against 68669ad1
(`results/2026-09-17-68669ad1.log`, a freshly rebooted machine): the text
document 54 against 54 ms and 94 against 92 ms for a first document, `BigTable`
1,635 against 1,619 ms and `Table` 3,422 against 3,435, with the same files and
the same allocations. All of it is within the noise of the runs; the fonts
cost nothing at run time.

### The text document, 500 pages

| Configuration | Time | File | Allocated | Peak memory |
|---|---:|---:|---:|---:|
| IBM Plex Sans | 54 ms | 533,287 bytes | 25 MB | 80 MB |
| Noto Sans | 59 ms | 804,928 bytes | 29 MB | 81 MB |

| Time by length | 50 pages | 100 pages | 200 pages | 500 pages |
|---|---:|---:|---:|---:|
| IBM Plex Sans | 8 ms | 13 ms | 25 ms | 54 ms |
| Noto Sans | 12 ms | 17 ms | 27 ms | 59 ms |

First document in a new JVM, 20 pages: 94 ms with IBM Plex Sans and 98 ms with
Noto Sans. The PDFjet jar is 402,651 bytes and needs nothing else.

The files are 6% smaller and the time 5 ms shorter than at 4ee4e7cb (59 ms and
567,688 bytes with IBM Plex Sans), as d2f5d4cb writes a color or a font only
when it changes.

### Example_43, 2,546 pages

| Configuration | Time | First run | Allocated | Peak memory | Smallest heap | File | Code |
|---|---:|---:|---:|---:|---:|---:|---:|
| `BigTable` | 1,635 ms | 1,969 ms | 670 MB | 386 MB | 32 MB | 11.7 MB | 17 |
| `Table` | 3,422 ms | 4,198 ms | 1,491 MB | 1,097 MB | 512 MB | 21.2 MB | 153 |
| `Page` | 1,597 ms | 1,943 ms | 628 MB | 465 MB | 128 MB | 11.5 MB | |
| `Page`, page by page | 1,567 ms | 1,907 ms | 628 MB | 371 MB | 32 MB | 11.5 MB | |

Code is the lines of program that draw the table, without the blank lines and
the comments. All four files have 2,546 pages and end with "Page 2546 of 2546".
Pages 1, 1273 and 2546 drawn on `Page`, kept or page by page, render the same
as `BigTable`'s at 72 dpi, pixel for pixel.

- The files are smaller than before: d2f5d4cb writes a color, a pen width or a
  font only when it changes and fills a rectangle with one `re` operator, which
  took `Page` from 12.6 to 11.5 MB, and 7dbfee5d fills the shaded rows of
  `BigTable` with one `re` too, which took it from 12.3 MB at 3ebd321b to 12.1
  at d2f5d4cb and 11.7 now.
- `BigTable` is within 2.4% of the same drawing on `Page`, 1,635 against 1,597
  ms. At d2f5d4cb `Page` was 5% faster, 1,584 against 1,671 ms, and wrote 11.5
  MB against 12.1: the driver filled each shaded row with `fillRect`, one `re`,
  where `BigTable` filled it with a path of four operators. `BigTable` still
  allocates more than `Page`, 670 against 632 MB, as it reads the quoted fields
  of its file as RFC 4180 does, which the driver does not.
- `Table` is 2.1 times slower than `BigTable` on the same data, allocates 2.2
  times as much, needs 16 times the heap and writes 81% more. It is the API to
  reach for when the rows are already in memory and the table is not enormous;
  `BigTable` is the one for a file this size. Until d2f5d4cb it wrote 25.2 MB:
  `Cell` set the brush and the pen for every cell, wrote the font and the pen
  width again for every cell, and filled each shaded cell with a path of four
  operators. A page now writes a color, a width or a font only when it changes
  and fills a rectangle with one `re`, which also took `Table` from 5,099 to
  3,672 ms and from a 1,024 MB heap to 512. 8db803f0 keeps the three colors of
  a `Cell` as packed 0xRRGGBB ints rather than arrays of three floats, which
  took `Table` from 1,821 to 1,718 MB allocated, from a 1,330 to a 1,252 MB
  peak and from 3,607 to 3,536 ms, with the same file. 68669ad1 measures the
  text of a cell without copying it and packs the six flags of a `Cell` into
  one int, which took it from 1,718 to 1,491 MB allocated, from a 1,252 to a
  1,098 MB peak and from 3,536 to 3,435 ms; and 660ab679 strokes the visible
  sides of a cell as one path, and writes nothing for a cell with no border,
  which took the file from 21,179,889 to 21,163,276 bytes.
- Memory depends on keeping the pages. A page that has not been added to the
  PDF holds its content uncompressed, 31,338 bytes a page, or 79.8 MB for the
  2,546 pages against 10.7 MB compressed, so keeping them all costs a 128 MB
  heap on `Page`; it was 38,307 bytes a page and a 256 MB heap before d2f5d4cb.
  `BigTable` counts its pages in the pass that measures the columns, draws each
  footer when its page is finished and writes the page out, so it needs 32 MB
  and peaks at 388 MB. Drawn page by page, `Page` also finishes in 32 MB.

## Earlier runs

Of the same programs: 8db803f0, which keeps the colors of a `Cell` as packed
RGB values, where the text document took 53 ms, `BigTable` 1,603 ms and 671 MB
and `Table` 3,536 ms, 1,718 MB and a 1,252 MB peak
(`results/2026-09-17-8db803f0.log`); 7dbfee5d, which fills the shaded rows of
a `BigTable` with one `re`, where `BigTable` took 1,599 ms and 674 MB and
`Table` 3,607 ms and 1,821 MB (`results/2026-09-16-7dbfee5d-text.log` and
`results/2026-09-16-7dbfee5d-table.log`); d2f5d4cb, where `BigTable` took 1,671
ms and 778 MB and wrote 12.1 MB, and `Table` 3,672 ms
(`results/2026-09-16-d2f5d4cb.log`, whose text figures are not quoted, as the
machine had not been rebooted); 3ebd321b, where `BigTable` took 1,703 ms and
761 MB, `Page` 1,800 ms and 815 MB in a 256 MB heap, and `Table` 5,099 ms and
2,255 MB in a 1,024 MB heap and wrote 25.2 MB
(`results/2026-09-16-3ebd321b-table.log`); 542b3dbf, the first run of `Table`,
at 5,038 ms (`results/2026-09-16-542b3dbf-table.log`); 4ee4e7cb, where the text
document took 59 ms and `BigTable` read its file with the first RFC 4180
parser, at 1,761 ms and 809 MB (`results/2026-09-15-4ee4e7cb.log`); f72b9f08,
which counts the pages of a `BigTable` and writes each one as it is finished,
so that `BigTable` needs a 32 MB heap rather than 256
(`results/2026-09-15-f72b9f08.log`); 312697d5, which places a string with `Td`
(`results/2026-09-15-312697d5.log`); 7c4988ad, which writes page content
without an array for each string and number, and measured the text document at
59 ms (`results/2026-09-15-7c4988ad.log`); and d963a7c8 before all three, at 71
ms (`results/2026-09-15-d963a7c8.log`).

## Caveats

- One machine and one JDK; absolute times will differ elsewhere.
- The four ports of PDFjet are compared with each other in section 5 of
  `pdfjet-benchmarks.html`, with programs of their own in `ports/`, built with
  each port's own toolchain. `ports/run.sh` runs them.
