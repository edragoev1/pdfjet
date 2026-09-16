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

AMD Ryzen 5 5600G, 12 threads, Linux. OpenJDK 21.0.12.1, .NET SDK 8.0.424,
Go 1.27.1, Swift 6.3.3. One run at d2f5d4cb, logged in
`results/2026-09-16-d2f5d4cb.log`, on a machine that had not been rebooted.

| Port | 100 pages | 500 pages | First document | File, 500 pages | Peak memory |
|---|---:|---:|---:|---:|---:|
| Java | 14 ms | 56 ms | 173 ms | 533,287 bytes | 79 MB |
| C# | 21 ms | 61 ms | 181 ms | 533,287 bytes | 55 MB |
| Go | 10 ms | 48 ms | 49 ms | 518,650 bytes | 12 MB |
| Swift | 22 ms | 93 ms | 97 ms | 599,518 bytes | 29 MB |

- Java and C# write files of the same size, to the byte. Go's 500-page file is
  about 3% smaller and Swift's about 12% larger. The four write the same page
  content, so the differences are the compressors'.
- d2f5d4cb writes a colour, a pen width or a font only when it changes. That
  took 6% off the Java and C# file, which was 567,688 bytes, and next to nothing
  off Go's, 518,785 bytes before: Go's compressor had already squeezed most of
  the repetition out of the old content. Swift's was 601,363 bytes at 300d67ab
  (`results/2026-09-16-300d67ab-swift.log`), 688,640 before that and 718,115
  before that. Its `FlateEncode` is the only compressor in the four ports that
  is not zlib, and it has been closing the gap in steps: longer matches, 4% off
  the file, and then stored, fixed or dynamic Huffman codes chosen a block at a
  time, 12.7% off.
- At 4ee4e7cb (`results/2026-09-16-4ee4e7cb.log`) the 500-page times were
  Java 58, C# 71, Go 51 and Swift 100 ms, on a freshly rebooted machine. All
  four are faster now, C# by 10 ms; differences of a few milliseconds are
  within the noise of a machine that was not rebooted.
- Go starts a first document fastest, 49 ms against 173 for Java, and keeps the
  smallest process, 12 MB against 79. The JVM and the .NET runtime carry their
  own footprint before the first page is drawn.
- mutool extracts the three lines of Latin, Greek and Cyrillic text from the
  3-page sample of every port.
- The C# figures of the first run at d2f5d4cb were of an old `PDFjet.dll`:
  the benchmark project took every file under its directory as a candidate,
  and a `build/` directory left from an earlier comparison held an old copy,
  which .NET preferred to the one the `HintPath` names. `PortBench.csproj` now
  names its one source file, and the run above is the second. The run at
  4ee4e7cb was made before that directory existed.

## Caveats

- One machine; absolute times will differ elsewhere.
- Peak memory includes the runtime, which is most of it for Java and C#.
- Each port is timed with its runtime at its defaults, so Java has no `-Xmx`
  here where section 5 of `jet-vs-box.html` gives it `-Xmx4g`.
