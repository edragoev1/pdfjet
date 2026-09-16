# Table in the four ports

`Table` at the scale the release plan asks about: the 9 columns of
`data/Electric_Vehicle_Population_10_Pages.csv` built as `Cell` objects and
drawn on as many Letter portrait pages as they need, in IBM Plex Sans
(`IBMPlexSans-SemiBold.otf.stream` for the header row, `-Regular` for the
rest) at 8 points. The rows of the file repeat until the table has the number
of rows asked for, and the widths are narrow enough that some columns wrap, so
the benchmark measures the wrapping as well as the drawing.

This is the table of `../BigTableBench.java` built the way a caller builds it,
one `Cell` per field, where `BigTable` reads the file itself: 50,000 rows here
are 450,000 cells in memory before a page is drawn.

| File | What it is |
|---|---|
| `TableBench.java` | The Java port |
| `TableBench.cs`, `TableBench.csproj` | The C# port; the csproj refers to `bin/release/net8.0/PDFjet.dll` |
| `tablebench/main.go` | The Go port |
| `swift/` | The Swift port, a package of its own that depends on the one in the repository root |
| `run.sh` | Builds all four and runs them, and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs |

## Running

```
benchmarks/table/run.sh                     # all four ports, a few minutes
benchmarks/table/run.sh java go             # only the ports named
ROWS="2000 10000" benchmarks/table/run.sh   # other table sizes
```

It needs a JDK, the .NET SDK, Go and Swift, and GNU time, and mutool for the
checks of the sample files. It builds every port before it measures any of
them, so that no build runs while a port is timed. Nothing else should run on
the machine meanwhile. `build/` is not tracked.

## Method

- Each port writes the document to a sink that counts the bytes, with its
  runtime at its defaults: no `-Xmx` for Java, no server GC setting for .NET.
- Time: 2 warm-up runs and 7 measured runs in one process; the median is
  reported, at 2,000, 10,000 and 50,000 rows.
- First document: a new process, timed from the start of `main`, 50,000 rows.
- Peak memory: the largest resident size, from `/usr/bin/time`, of a new
  process writing one 50,000-row table, the median of 3 runs. It includes the
  runtime itself, so the JVM and the .NET runtime start higher than Go and
  Swift before the first page is drawn.
- The 40-row sample of each port is checked with mutool for the text of its
  first page.

## Results at d2f5d4cb, 16 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, OpenJDK 21.0.12.1, .NET SDK 8.0.424,
Go 1.27.1, Swift 6.3.3, in one run on a machine that had not been rebooted
(`results/2026-09-16-d2f5d4cb.log`).

| Port | 2,000 rows | 10,000 rows | 50,000 rows | First document | Peak memory | File, 50,000 rows |
|---|---:|---:|---:|---:|---:|---:|
| Java | 83 ms | 376 ms | 1,676 ms | 2,063 ms | 462 MB | 9,116,668 bytes |
| C# | 84 ms | 411 ms | 2,081 ms | 2,362 ms | 251 MB | 9,116,668 bytes |
| Go | 32 ms | 155 ms | 776 ms | 852 ms | 289 MB | 9,147,319 bytes |
| Swift | 112 ms | 524 ms | 2,599 ms | 2,694 ms | 188 MB | 11,197,754 bytes |

- d2f5d4cb writes a colour, a pen width or a font only when it changes, and
  fills a rectangle with one `re` operator, in the four ports. Every cell used
  to set the brush and the pen, the font and the pen width again, and fill its
  background with a path of four operators. Against the run below, at 50,000
  rows, that took 5% off Java, 8% off C#, 12% off Go and 11% off Swift, and the
  files from about 9.7 MB to 9.1 MB in Java and C#, and Swift's from 11,839,117
  bytes to 11,197,754.
- Java's peak fell from 600 MB to 462, and Go's from 342 to 289, with less
  page content to hold until each page is written.
- Java and C# write the same bytes, Go's file is 0.3% larger and Swift's 23%,
  which is `FlateEncode` rather than `Table`.

## Results, 16 September 2026: one walk over the cells

AMD Ryzen 5 5600G, 12 threads, Linux, in two runs with nothing else running.
OpenJDK 21.0.12.1, .NET SDK 8.0.424, Go 1.27.1, Swift 6.3.3.

Before is PDFjet at 311b6e92 (`results/2026-09-16-before.log`), after is the
working tree of the commit that added this directory
(`results/2026-09-16-after.log`), which wraps every cell once instead of
twice. The two runs write the same bytes: the 204 example PDFs of the four
ports have the same content streams and render the same, and the samples here
are the same size to the byte.

| Port | 50,000 rows, before | after | First document, before | after | Peak memory, before | after |
|---|---:|---:|---:|---:|---:|---:|
| Java | 2,073 ms | 1,768 ms | 2,886 ms | 2,197 ms | 964 MB | 600 MB |
| C# | 2,546 ms | 2,272 ms | 2,865 ms | 2,562 ms | 260 MB | 260 MB |
| Go | 1,106 ms | 884 ms | 1,178 ms | 963 ms | 327 MB | 342 MB |
| Swift | 3,815 ms | 2,928 ms | 3,873 ms | 3,021 ms | 207 MB | 207 MB |

A 50,000-row table is 1,072 pages, and about 9.7 MB in Java and C#.

- The wrapping used to run twice over every cell: once to count the lines the
  text needs, and again to write them. One walk now serves both, which is
  11% off C#, 20% off Go and 23% off Swift at 50,000 rows.
- Java gains 15%, from two changes. The single walk takes it from 2,073 ms to
  1,846, and dropping a regular expression from `Util.splitOnWhitespace` takes
  it to 1,768: it split with `split("\\s+")`, which `String.split` cannot take
  its fast path for, so every cell of the table compiled a `Pattern`. The
  other three ports have always scanned the characters, and Java does now.
- Java's peak memory falls by 38%, from 964 MB to 600 MB: half the wrapping is
  half the garbage it made, and the JVM grows its heap less. Go's rises 4.5%,
  which is the same effect the other way round — less garbage, so its
  collector runs later. C# and Swift do not move.
- The order of the ports is the one in `../ports/README.md`: Go draws this
  table fastest and Swift slowest. Java and C# write the same bytes, Go's file
  is about 1% larger and Swift's about 23%, which is `FlateEncode` rather than
  `Table`. Swift's was about 50% larger until 300d67ab gave each block the
  Huffman codes that fit it, which took its 50,000-row file from 14,566,771
  bytes to 11,839,117.

## At the size of Example_43

The release plan asks how `Table` fares on the table `BigTable` draws in
Example_43, the whole `data/Electric_Vehicle_Population_Data.csv`. That is
124,716 rows, so 1.12 million `Cell` objects, and 2,673 pages here:

```
ROWS=124716 benchmarks/table/run.sh
```

| Port | Time | First document | Peak memory | File |
|---|---:|---:|---:|---:|
| Java | 4,220 ms | 4,920 ms | 998 MB | 22.6 MB |
| C# | 5,158 ms | 5,558 ms | 537 MB | 22.6 MB |
| Go | 1,946 ms | 2,141 ms | 755 MB | 22.6 MB |
| Swift | 6,591 ms | 6,742 ms | 428 MB | 27.7 MB |

One run at d2f5d4cb, `results/2026-09-16-d2f5d4cb-124716.log`. `run.sh` takes
the first document and the peak memory at 50,000 rows whatever `ROWS` says, so
at this size both were measured on their own after the run, three new
processes each, and the medians are above; the log has all three. Before
d2f5d4cb, which writes a colour, a pen width or a font only when it changes and
fills a rectangle with one `re`, the first document took 4,983 ms in Java,
5,910 in C#, 2,389 in Go and 7,203 in Swift, with peaks of 1,135, 566, 800 and
472 MB and files of 23.9, 23.9, 24.1 and 29.3 MB
(`results/2026-09-16-300d67ab-swift-124716.log` for Swift, where its
`FlateEncode` began choosing the Huffman codes of each block).

The geometry is this benchmark's, not Example_43's, so the page counts differ
a little: Example_43 is 2,546 pages of the same data. On that table, in the run
at d2f5d4cb and on this machine, iText Core's own `Table` in large-table mode
took 37,052 ms in a new JVM and peaked at 7,015 MB, and needed an 8 GB heap to
run at all, while `BigTable` took 2,099 ms and 389 MB (`../results/`). So
`Table` holding all 1.12 million cells is about seven times faster than iText's
and needs about a seventh of the memory; what it cannot do is draw the table
without holding them, which is what `BigTable` is for.

## Caveats

- One machine; absolute times will differ elsewhere.
- Peak memory includes the runtime, which is most of it for Java and C#.
- Each port is timed with its runtime at its defaults, so Java has no `-Xmx`
  here where section 5 of `jet-vs-box.html` gives it `-Xmx4g`.
- The cells are held in memory before the first page is drawn, which is what
  the release plan wants measured; `BigTable` reads its file instead and draws
  the same data in a fraction of the memory. See `../README.md`.
