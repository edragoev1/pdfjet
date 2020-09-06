using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_79.cs
 *
 */
class Example_79 {

    public Example_79(String fileNumber, String fileName) {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_79_" + fileNumber + ".pdf", FileMode.Create)));

        BufferedStream bis = new BufferedStream(
                new FileStream("data/testPDFs/" + fileName, FileMode.Open));
        List<PDFobj> objects = pdf.Read(bis);
        bis.Close();

        Image image = new Image(
                objects,
                new BufferedStream(new FileStream(
                        "images/qrcode.png", FileMode.Open, FileAccess.Read)),
                ImageType.PNG);
        image.SetLocation(100f, 300f);

        Font f1 = new Font(
                objects,
                new FileStream("fonts/Droid/DroidSans.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read), Font.STREAM);
        f1.SetSize(12f);


        List<PDFobj> pages = pdf.GetPageObjects(objects);

        Page page = null;
        for (int i = 0; i < pages.Count; i++) {
            page = new Page(pdf, pages[i]);

            page.AddResource(image, objects);
            image.DrawOn(page);

            page.AddResource(f1, objects);
            page.SetBrushColor(Color.blue);
            page.DrawString(f1, "John Smith", 100f, 270f);

            page.Complete(objects);
        }

        pdf.AddObjects(objects);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_79("00", "Example_01.pdf");
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine("Example_79 => " + (time1 - time0));
    }

}   // End of Example_79.cs
