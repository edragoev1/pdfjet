using System;
using System.Diagnostics;
using System.IO;
using PDFjet.NET;

/// <summary>
/// The text document of section 3 of pdfjet-benchmarks.html, written by PDFjet for
/// C#: pages of 60 lines of 10 point Latin, Greek and Cyrillic text in
/// IBM Plex Sans, one drawing call per line.
///
/// Usage: PortBench bench|cold &lt;pages&gt;
///        PortBench sample &lt;pages&gt; &lt;file&gt;
///
/// Run from the root of the repository, as the font path is relative to it.
/// </summary>
public class PortBench {
    const String FONT = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
    const int LINES = 60;
    static readonly String[] SAMPLES = {
        "The quick brown fox jumps over the lazy dog",
        "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία",
        "Съешь же ещё этих мягких французских булок",
    };

    static byte[] Document(int pages) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = new Font(pdf, FONT);
        for (int p = 0; p < pages; p++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            for (int l = 0; l < LINES; l++) {
                page.DrawString(font, null, 10f,
                        SAMPLES[l % 3] + " " + p + "." + l, 50f, 50f + l * 12f);
            }
        }
        pdf.Complete();
        return stream.ToArray();
    }

    public static void Main(String[] args) {
        Stopwatch start = Stopwatch.StartNew();
        String mode = args[0];
        int pages = Int32.Parse(args[1]);
        if (mode == "cold") {
            byte[] pdf = Document(pages);
            Console.WriteLine("dotnet cold {0} pages: {1} ms, {2} bytes",
                    pages, start.ElapsedMilliseconds, pdf.Length);
            return;
        }
        if (mode == "sample") {
            File.WriteAllBytes(args[2], Document(pages));
            return;
        }
        for (int i = 0; i < 2; i++) {
            Document(pages);
        }
        long[] ms = new long[7];
        int size = 0;
        for (int i = 0; i < ms.Length; i++) {
            Stopwatch sw = Stopwatch.StartNew();
            size = Document(pages).Length;
            ms[i] = sw.ElapsedMilliseconds;
        }
        Array.Sort(ms);
        Console.WriteLine("dotnet {0} pages: median {1} ms (min {2}, max {3}, {4} runs), {5} bytes",
                pages, ms[ms.Length / 2], ms[0], ms[ms.Length - 1], ms.Length, size);
    }
}
