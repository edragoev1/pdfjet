using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_14.cs
 * Drawing Data Matrix barcodes.
 */
public class Example_14 {
    public Example_14() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_14.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        DataMatrix barcode = new DataMatrix("https://github.com/edragoev1/pdfjet");
        barcode.SetLocation(50f, 50f);
        barcode.SetModuleLength(3f);
        float[] xy = barcode.DrawOn(page);
        TextLine caption = new TextLine(f1, "A web address");
        caption.SetLocation(50f, xy[1] + 20f);
        caption.DrawOn(page);

        barcode = new DataMatrix("Grüße aus München! こんにちは 😀");
        barcode.SetLocation(300f, 50f);
        barcode.SetModuleLength(3f);
        xy = barcode.DrawOn(page);
        caption = new TextLine(f1, "Text in UTF-8");
        caption.SetLocation(300f, xy[1] + 20f);
        caption.DrawOn(page);

        barcode = new DataMatrix("PDFjet 9.0.0", DataMatrix.RECTANGLE);
        barcode.SetLocation(50f, 250f);
        barcode.SetModuleLength(4f);
        barcode.SetColor(Color.blue);
        xy = barcode.DrawOn(page);
        caption = new TextLine(f1, "A rectangular symbol");
        caption.SetLocation(50f, xy[1] + 20f);
        caption.DrawOn(page);

        StringBuilder sb = new StringBuilder();
        for (int i = 1; i <= 20; i++) {
            sb.Append("Line ").Append(i).Append(" of a longer text in a larger symbol.\n");
        }
        barcode = new DataMatrix(sb.ToString());
        barcode.SetLocation(300f, 250f);
        barcode.SetModuleLength(2f);
        xy = barcode.DrawOn(page);
        caption = new TextLine(f1, "A larger symbol");
        caption.SetLocation(300f, xy[1] + 20f);
        caption.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_14();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_14", time0, time1);
    }
}   // End of Example_14.cs
