using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

/**
 * Example_43.cs
 */
public class Example_43 {
    public Example_43() {
        PDF pdf = new PDF(
                new BufferedStream(new FileStream("Example_43.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA);

        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f1 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream");
        Font f2 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream");

        f1.SetSize(8f);
        f2.SetSize(8f);

        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_1000.csv";

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        List<float> widths = table.getColumnWidths(fileName);
        // Optionally you can fine tune the widths of the columns:
        widths[8] = 70f; // Override the calculated width
        widths[9] = 99f; // Override the calculated width
        table.SetColumnSpacing(7f);
        table.SetLocation(20f, 15f);
        table.SetBottomMargin(15f);
        table.SetColumnWidths(widths);

        // You can override that auto column alignments if required:
        // final int LEFT = 0;                  // Align Left
        // final int RIGHT = 1;                 // Align Right
        // table.SetTextAlignment(1, RIGHT);    // Override the auto alignment
        // table.SetTextAlignment(5, LEFT);     // Override the auto alignment

        StreamReader reader = new StreamReader(fileName);
        bool headerRow = true;
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            String[] fields = line.Split(',');
            // Optional step:
            fields = SelectAndProcessFields(table, fields, headerRow);
            if (fields[6].Equals("TOYOTA")) {
                table.DrawRow(fields, Color.red);
            } else if (fields[6].Equals("JEEP")) {
                table.DrawRow(fields, Color.green);
            } else if (fields[6].Equals("FORD")) {
                table.DrawRow(fields, Color.blue);
            } else {
                table.DrawRow(fields, Color.black);
            }
            headerRow = false;
        }
        table.Complete();
        reader.Close();

        List<Page> pages = table.GetPages();
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    private String[] SelectAndProcessFields(BigTable table, String[] fields, bool headerRow) {
        List<String> row = new List<String>();
        for (int i = 0; i < 10; i++) {
            String field = fields[i];
            if (i == 8) {
                if (field[0] == 'B') {
                    row.Add("BEV");
                } else if (field[0] == 'P') {
                    row.Add("PHEV");
                } else {
                    row.Add(field);
                }
            } else if (i == 9) {
                if (headerRow) {
                    row.Add("Clean Alternative Fuel Vehicle");
                } else {
                    if (field[0] == 'C') {
                        row.Add("YES");
                    } else if (field[0] == 'N') {
                        row.Add("NO");
                    } else {
                        row.Add("UNKNOWN");
                    }
                }
            } else {
                row.Add(field);
            }
        }
        return row.ToArray();
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
