using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;
using System.Collections.Generic;

/**
 * Example_99.cs
 */
public class Example_99 {
    public Example_99() {
        PDF pdf = new PDF(
                new BufferedStream(new FileStream("Example_99.pdf", FileMode.Create)));
        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        // Font f1 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream");
        // Font f2 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream");
        // Font f1 = new Font(pdf, "fonts/Andika/Andika-Bold.ttf.stream");
        // Font f2 = new Font(pdf, "fonts/Andika/Andika-Regular.ttf.stream");
        Font f1 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-SemiBold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream");

        // f1.SetSize(9f);
        // f2.SetSize(9f);

        f1.SetSize(7f);
        f2.SetSize(7f);

        int L = 0;
        int R = 1;

        BigTable table = new BigTable(pdf, f1, f2, Letter.PORTRAIT);
        table.SetLocation(20f, 15f);
        table.SetBottomMargin(15f);
        table.SetColumnWidths(new int[] {80, 80, 35, 60, 60, 75, 110, 90});
        table.SetTextAlignment(new int[] {L,  L,  L,  R,  R,  L,   L,  L});
        table.SetColumnSpacing(2f);
        table.SetDrawVerticalLines(false);
        // table.SetHeaderRowColor(Color.darkolivegreen);

        int[] widths = {15, 15, 18, 7, 12, 12, 15, 15, 25};
        int[] align = {  L,  L,  L, L,  R,  R,  L,  L,  L};

        StreamReader br = new StreamReader("../datasets/Electric_Vehicle_Population_Data.csv");
        String line = null;
        while ((line = br.ReadLine()) != null) {
            String[] fields = line.Split(',');

            String textLine = table.GetTextLine(fields, widths, align);
            table.Add(textLine);
            if (textLine.Contains("FORD")) {
                table.DrawRow(Color.blue);
            } else if (textLine.Contains("VOLKSWAGEN")) {
                table.DrawRow(Color.red);
            } else {
                table.DrawRow(Color.black);
            }

            // table.Add(fields[0]);
            // table.Add(fields[2]);
            // table.Add(fields[3]);
            // table.Add(fields[4]);
            // table.Add(fields[5]);
            // table.Add(fields[6]);
            // table.Add(fields[7]);
            // table.Add(fields[8][0] == 'B' ? "BEV" : fields[8][0] == 'P' ? "PHEV" : fields[8]);
            // if (fields[6].Equals("FORD")) {
            //     table.DrawRow(Color.blue);
            // } else if (fields[6].Equals("VOLKSWAGEN")) {
            //     table.DrawRow(Color.red);
            // } else {
            //     table.DrawRow(Color.black);
            // }
        }
        br.Close();

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
        new Example_99();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_99", time0, time1);
    }
}
