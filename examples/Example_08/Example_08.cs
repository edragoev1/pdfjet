using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_08.cs
 */
public class Example_08 {
    public Example_08() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_08.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(7f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(7f);

        Font f3 = new Font(pdf, IBMPlexSans.BoldItalic);
        f3.SetSize(7f);

        Image image = new Image(pdf, "images/TeslaX.png");
        image.SetScaleFactor(0.20f);

        Barcode barcode = new Barcode(Barcode.CODE128, "Hello, World!");
        barcode.SetModuleLength(0.75f);
	    // Comment out the line below if you don't want to print the text underneath the barcode.
        barcode.SetFont(f1);

        Table table = new Table(f1, f2, "data/Electric_Vehicle_Population_10_Pages.csv");
        table.SetVisibleColumns(1, 2, 3, 4, 5, 6, 7, 9);
        table.GetCellAt(4, 0).SetImage(image);
        table.GetCellAt(5, 0).SetColSpan(8);
        table.GetCellAt(5, 0).SetBarcode(barcode);
        table.GetCellAt(20, 0).SetColSpan(6);
        table.GetCellAt(20, 6).SetColSpan(2);
        table.SetColumnWidths();
        table.SetColumnWidth(0, image.GetWidth() + 4f);
        table.SetColumnWidth(3, table.GetColumnWidth(3) + 10f);
        table.SetColumnWidth(5, table.GetColumnWidth(5) + 10f);
        table.RightAlignNumbers();

        table.SetLocation(30f, 30f);
        // table.SetFirstPageTopMargin(150f);
        table.SetBottomMargin(15f);
        table.SetTextColorInRow(12, Color.blue);
        table.SetTextColorInRow(13, Color.red);
        table.SetFontInRow(14, f3);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
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
        new Example_08();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_08", time0, time1);
    }
}   // End of Example_08.cs
