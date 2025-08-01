using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

public class Example_43 {
    public Example_43() {
        PDF pdf = new PDF(
                new BufferedStream(new FileStream("Example_43.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA);

        // Used for performance testing. Results in 2000+ pages PDF.
        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream");
        f1.SetSize(10f);

        Font f2 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        f2.SetSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        table.SetNumberOfColumns(9);        // The order of the
        table.SetTableData(fileName, ",");  // these statements
        table.SetLocation(0f, 0f);          // is
        table.SetBottomMargin(20f);         // very
        table.Complete();                   // important!

        List<Page> pages = table.GetPages();
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_43();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_43", time0, time1);
    }
}
