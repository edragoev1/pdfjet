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

## Results, 16 September 2026

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

A 50,000-row table is 1,072 pages and about 9.7 MB.

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
  is about 1% larger and Swift's about 50%, which is `FlateEncode` rather than
  `Table`.

## At the size of Example_43

The release plan asks how `Table` fares on the table `BigTable` draws in
Example_43, the whole `data/Electric_Vehicle_Population_Data.csv`. That is
124,716 rows, so 1.12 million `Cell` objects, and 2,673 pages here:

```
ROWS=124716 benchmarks/table/run.sh
```

| Port | First document | Peak memory | File |
|---|---:|---:|---:|
| Java | 4,983 ms | 1,135 MB | 23.9 MB |
| C# | 5,910 ms | 566 MB | 23.9 MB |
| Go | 2,389 ms | 800 MB | 24.1 MB |
| Swift | 7,446 ms | 474 MB | 36.1 MB |

The geometry is this benchmark's, not Example_43's, so the page counts differ
a little: Example_43 is 2,546 pages of the same data. On that table, at
4ee4e7cb and on this machine, iText Core's own `Table` in large-table mode
took 36,543 ms in a new JVM and 7,180 MB, and needed an 8 GB heap to run at
all, while `BigTable` took 2,109 ms and 391 MB (`../results/`). So `Table`
holding all 1.12 million cells is still about seven times faster than iText's
and needs about a sixth of the memory; what it cannot do is draw the table
without holding them, which is what `BigTable` is for.

## Caveats

- One machine; absolute times will differ elsewhere.
- Peak memory includes the runtime, which is most of it for Java and C#.
- Each port is timed with its runtime at its defaults, so Java has no `-Xmx`
  here where section 5 of `jet-vs-box.html` gives it `-Xmx4g`.
- The cells are held in memory before the first page is drawn, which is what
  the release plan wants measured; `BigTable` reads its file instead and draws
  the same data in a fraction of the memory. See `../README.md`.
