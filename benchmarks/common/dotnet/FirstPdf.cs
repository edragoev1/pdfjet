// The first PDF of a process, by phase, as in the Java and Go probes.
using System;
using System.Diagnostics;
using System.IO;
using PDFjet.NET;

class FirstPdf {
    static double Ms(long ticks) { return ticks * 1000.0 / Stopwatch.Frequency; }

    static void Main(string[] args) {
        string root = args[0] + "/";
        if (args.Length > 1) {
            long a = Stopwatch.GetTimestamp();
            if (args[1] == "random") {
                Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            } else {
                DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            }
            Console.WriteLine("{0} first use {1:F1} ms", args[1], Ms(Stopwatch.GetTimestamp() - a));
            return;
        }
        long t0 = Stopwatch.GetTimestamp();
        PDF pdf = new PDF(new BufferedStream(Stream.Null));
        long t1 = Stopwatch.GetTimestamp();
        Font regular = new Font(pdf, root + "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf");
        long t2 = Stopwatch.GetTimestamp();
        Font bold = new Font(pdf, root + "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf");
        long t3 = Stopwatch.GetTimestamp();
        Page page = new Page(pdf, Letter.PORTRAIT);
        for (int i = 0; i < 40; i++) {
            TextLine line = new TextLine(i % 5 == 0 ? bold : regular, "Line " + i + " of the invoice, with some words on it");
            line.SetLocation(50f, 60f + 15f * i);
            line.DrawOn(page);
        }
        long t4 = Stopwatch.GetTimestamp();
        pdf.Complete();
        long t5 = Stopwatch.GetTimestamp();
        Console.WriteLine("new PDF {0:F1}, first font {1:F1}, second {2:F1}, page {3:F1}, complete {4:F1}, total {5:F1} ms",
                Ms(t1 - t0), Ms(t2 - t1), Ms(t3 - t2), Ms(t4 - t3), Ms(t5 - t4), Ms(t5 - t0));
    }
}
