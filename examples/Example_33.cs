using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_33.cs
 *
 */
public class Example_33 {

    public Example_33() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_33.pdf", FileMode.Create)));

        Page page = new Page(pdf, A4.PORTRAIT);

        Image image = new Image(
                pdf,
                new FileStream("images/photoshop.jpg", FileMode.Open, FileAccess.Read),
                ImageType.JPG);
        image.SetLocation(10f, 10f);
        image.ScaleBy(0.25f);
        image.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_33 => " + (time1 - time0));
    }

}   // End of Example_33.cs
