using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_11.cs
 */
public class Example_11 {
    public Example_11() {
        PDF pdf = new PDF( new BufferedStream(
                new FileStream("Example_11.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Barcode code = new Barcode(Barcode.CODE128, "Hellö, World!");
        code.SetLocation(170f, 70f);
        code.SetModuleLength(0.75f);
        code.SetFont(f1);
        code.DrawOn(page);

        code = new Barcode(Barcode.CODE128, "G86513JVW0C");
        code.SetLocation(170f, 170f);
        code.SetModuleLength(0.75f);
        code.SetDirection(Barcode.TOP_TO_BOTTOM);
        code.SetFont(f1);
        code.DrawOn(page);

        code = new Barcode(Barcode.CODE39, "WIKIPEDIA");
        code.SetLocation(270f, 370f);
        code.SetModuleLength(0.75f);
        code.SetFont(f1);
        code.DrawOn(page);

        code = new Barcode(Barcode.CODE39, "CODE39");
        code.SetLocation(400f, 70f);
        code.SetModuleLength(0.75f);
        code.SetDirection(Barcode.TOP_TO_BOTTOM);
        code.SetFont(f1);
        code.DrawOn(page);

        code = new Barcode(Barcode.CODE39, "CODE39");
        code.SetLocation(450f, 70f);
        code.SetModuleLength(0.75f);
        code.SetDirection(Barcode.BOTTOM_TO_TOP);
        code.SetFont(f1);
        code.DrawOn(page);

        code = new Barcode(Barcode.UPC, "712345678904");
        code.SetLocation(450f, 270f);
        code.SetModuleLength(0.75f);
        code.SetDirection(Barcode.BOTTOM_TO_TOP);
        code.SetFont(f1);
        code.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_11();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_11", time0, time1);
    }
}   // End of Example_11.cs
