# The four ports compared with each other

The same document written by PDFjet for Java, C#, Go and Swift, with each
port's own build. It is the text document of `../TextBench.java`: pages of 60
lines of 10 point Latin, Greek and Cyrillic text in IBM Plex Sans
(`IBMPlexSans-Regular.otf.stream`), one `drawString` call per line, on Letter
portrait pages. Section 10 of `jet-vs-box.html` quotes these numbers; the
comparison with iText Core and Apache PDFBox is in `../README.md`.

| File | What it is |
|---|---|
| `PortBench.java` | The Java port |
| `PortBench.cs`, `PortBench.csproj` | The C# port; the csproj refers to `bin/release/net8.0/PDFjet.dll` |
| `portbench/main.go` | The Go port |
| `swift/` | The Swift port, a package of its own that depends on the one in the repository root |
| `run.sh` | Builds all four and runs them, and writes `build/results-<date>-<time>.log` |
| `results/` | The logs of the runs |

## Running

```
benchmarks/ports/run.sh            # all four ports, about 10 minutes
benchmarks/ports/run.sh java go    # only the ports named
```

It needs a JDK, the .NET SDK, Go and Swift, and GNU time, and mutool for the
checks of the sample files. It builds every port before it measures any of
them, so that no build runs while a port is timed. Nothing else should run on
the machine meanwhile. `build/` is not tracked.

## Method

- Each port writes the document to memory, with its runtime at its defaults:
  no `-Xmx` for Java, no server GC setting for .NET.
- Time: 2 warm-up runs and 7 measured runs in one process; the median is
  reported, at 100 and at 500 pages.
- First document: a new process, timed from the start of `main`, 500 pages.
- Peak memory: the largest resident size, from `/usr/bin/time`, of a new
  process writing one 500-page document, the median of 3 runs. It includes the
  runtime itself, so the JVM and the .NET runtime start higher than Go and
  Swift before the first page is drawn.
- The 3-page sample of each port is checked with mutool for its three lines of
  Latin, Greek and Cyrillic text.

## Results, 16 September 2026

AMD Ryzen 5 5600G, 12 threads, Linux, with nothing else running. OpenJDK
21.0.12.1, .NET SDK 8.0.424, Go 1.27.1, Swift 6.3.3. The first three rows are
one run at 4ee4e7cb, logged in `results/2026-09-16-4ee4e7cb.log`. The Swift
row was measured again at 300d67ab, where its `FlateEncode` began choosing the
Huffman codes of each block, and is logged in
`results/2026-09-16-300d67ab-swift.log`; nothing in the other three ports
changed in between, and a file of a given document is the same bytes every
run.

| Port | 100 pages | 500 pages | First document | File, 500 pages | Peak memory |
|---|---:|---:|---:|---:|---:|
| Java | 14 ms | 58 ms | 170 ms | 567,688 bytes | 87 MB |
| C# | 21 ms | 71 ms | 176 ms | 567,688 bytes | 54 MB |
| Go | 11 ms | 51 ms | 50 ms | 518,785 bytes | 12 MB |
| Swift | 23 ms | 100 ms | 105 ms | 601,363 bytes | 29 MB |

- Java and C# write files of the same size, to the byte. Go's 500-page file is
  about 9% smaller and Swift's about 6% larger.
- Swift's was 688,640 bytes before 300d67ab, and 718,115 before that. Its
  `FlateEncode` is the only compressor in the four ports that is not zlib, and
  it has been closing the gap in steps: 7c4988ad and 312697d5 shortened the
  page content of every port and it was taught to find longer matches, 4% off
  the file, and it then stopped writing one fixed-Huffman block per stream and
  began choosing stored, fixed or dynamic codes a block at a time, 12.7% off.
- Go starts a first document fastest, 50 ms against 170 for Java, and keeps the
  smallest process, 12 MB against 87. The JVM and the .NET runtime carry their
  own footprint before the first page is drawn.
- mutool extracts the three lines of Latin, Greek and Cyrillic text from the
  3-page sample of every port.

## Caveats

- One machine; absolute times will differ elsewhere.
- Peak memory includes the runtime, which is most of it for Java and C#.
- Each port is timed with its runtime at its defaults, so Java has no `-Xmx`
  here where section 5 of `jet-vs-box.html` gives it `-Xmx4g`.
