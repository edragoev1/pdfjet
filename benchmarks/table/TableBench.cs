using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PDFjet.NET;

/// <summary>
/// Table at the scale the TODO asks about: the 9 columns of the Electric
/// Vehicle Population CSV built as Cell objects and drawn with Table on as
/// many Letter pages as they need.
///
/// The widths are narrow enough that some columns wrap, so the benchmark
/// measures the wrapping as well as the drawing; the rows of the file are
/// repeated until the table has the number of rows asked for.
///
/// Usage: TableBench bench|cold &lt;rows&gt;
///        TableBench sample &lt;rows&gt; &lt;file&gt;
///
/// Run from the root of the repository, as the paths are relative to it.
/// </summary>
public class TableBench {
    const String CSV = "data/Electric_Vehicle_Population_10_Pages.csv";
    const String SEMIBOLD = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf.stream";
    const String REGULAR = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
    const int COLUMNS = 9;
    static readonly float[] WIDTHS = {68f, 58f, 58f, 26f, 44f, 34f, 52f, 60f, 152f};

    // Counts the bytes written, so that the output costs no memory or disk.
    class Sink : Stream {
        internal long n;
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return n; } }
        public override long Position { get { return n; } set { } }
        public override void Flush() { }
        public override int Read(byte[] b, int off, int len) { return 0; }
        public override long Seek(long off, SeekOrigin origin) { return n; }
        public override void SetLength(long len) { }
        public override void Write(byte[] b, int off, int len) { n += len; }
        public override void WriteByte(byte b) { n++; }
    }

    // The first COLUMNS fields of every line of the CSV, the header first.
    static List<String[]> ReadFields() {
        List<String[]> lines = new List<String[]>();
        foreach (String line in File.ReadAllLines(CSV, Encoding.UTF8)) {
            String[] fields = line.Split(',');
            if (fields.Length < COLUMNS) {
                continue;
            }
            String[] first = new String[COLUMNS];
            Array.Copy(fields, first, COLUMNS);
            lines.Add(first);
        }
        return lines;
    }

    static List<List<Cell>> TableData(List<String[]> fields, int rows, Font f1, Font f2) {
        List<List<Cell>> data = new List<List<Cell>>();
        for (int r = 0; r <= rows; r++) {
            // Row 0 is the header; the data rows repeat the file from line 1.
            String[] values = (r == 0) ? fields[0] : fields[1 + (r - 1) % (fields.Count - 1)];
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < COLUMNS; c++) {
                Cell cell = new Cell((r == 0) ? f1 : f2, values[c]);
                cell.SetWidth(WIDTHS[c]);
                cell.SetTextAlignment(c == 4 ? Alignment.RIGHT : Alignment.LEFT);
                row.Add(cell);
            }
            data.Add(row);
        }
        return data;
    }

    static int Document(List<String[]> fields, int rows, Stream stream, int[] pageCount) {
        PDF pdf = new PDF(stream);
        Font f1 = new Font(pdf, SEMIBOLD);
        f1.SetSize(8f);
        Font f2 = new Font(pdf, REGULAR);
        f2.SetSize(8f);

        Table table = new Table();
        table.SetTableData(TableData(fields, rows, f1, f2), 1);
        table.SetLocation(20f, 20f);
        table.SetBottomMargin(20f);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        foreach (Page page in pages) {
            pdf.AddPage(page);
        }
        pdf.Complete();
        pageCount[0] = pages.Count;
        return pages.Count;
    }

    static long Run(List<String[]> fields, int rows, int[] pageCount) {
        Sink sink = new Sink();
        Document(fields, rows, sink, pageCount);
        return sink.n;
    }

    public static void Main(String[] args) {
        Stopwatch start = Stopwatch.StartNew();
        String mode = args[0];
        int rows = Int32.Parse(args[1]);
        List<String[]> fields = ReadFields();
        int[] pageCount = new int[1];
        if (mode == "cold") {
            long bytes = Run(fields, rows, pageCount);
            Console.WriteLine("dotnet cold {0} rows: {1} ms, {2} pages, {3} bytes",
                    rows, start.ElapsedMilliseconds, pageCount[0], bytes);
            return;
        }
        if (mode == "sample") {
            using (FileStream out_ = new FileStream(args[2], FileMode.Create)) {
                Document(fields, rows, out_, pageCount);
            }
            return;
        }
        for (int i = 0; i < 2; i++) {
            Run(fields, rows, pageCount);
        }
        long[] ms = new long[7];
        long size = 0;
        for (int i = 0; i < ms.Length; i++) {
            Stopwatch sw = Stopwatch.StartNew();
            size = Run(fields, rows, pageCount);
            ms[i] = sw.ElapsedMilliseconds;
        }
        Array.Sort(ms);
        Console.WriteLine("dotnet {0} rows: median {1} ms (min {2}, max {3}, {4} runs), {5} pages, {6} bytes",
                rows, ms[ms.Length / 2], ms[0], ms[ms.Length - 1], ms.Length, pageCount[0], size);
    }
}
