using System;
using System.IO;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_12.cs
 *
 */
public class Example_12 {

    public Example_12() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_12.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        StringBuilder buf = new StringBuilder();
        StreamReader reader = new StreamReader(
                new FileStream("examples/Example_12.cs", FileMode.Open));
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            buf.Append(line);
            // Both CR and LF are required by the scanner!
            buf.Append("\r\n");
        }
        reader.Close();

        BarCode2D code2D = new BarCode2D(buf.ToString());
        code2D.SetModuleWidth(0.5f);
        code2D.SetLocation(100f, 60f);
        float[] xy = code2D.DrawOn(page);
/*
        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);
*/
        TextLine text = new TextLine(f1,
                "PDF417 barcode containing the program that created it.");
        text.SetLocation(100f, 40f);
        text.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_12();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_12 => " + (time1 - time0));
    }

}   // End of Example_12.cs
