using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;
using System.Collections.Generic;

/**
 *  Example_32.cs
 */
public class Example_32 {
    public Example_32() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_32.pdf", FileMode.Create)));

        Font font = new Font(pdf, CoreFont.COURIER);
        font.SetSize(8f);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["new"] = Color.red;
        colors["ArrayList"] =  Color.blue;
        colors["List"] = Color.blue;
        colors["String"] = Color.blue;
        colors["Field"] = Color.blue;
        colors["Form"] = Color.blue;
        colors["Smart"] = Color.green;
        colors["Widget"] = Color.green;
        colors["Designs"] = Color.green;

        float x = 50f;
        float y = 50f;
        float dy = font.GetBodyHeight();
        Page page = new Page(pdf, Letter.PORTRAIT);
        List<String> lines = Text.ReadLines("examples/Example_02.cs");
        foreach (String line in lines) {
            page.DrawString(font, line, x, y, colors);
            y += dy;
            if (y > (page.GetHeight() - 20f)) {
                page = new Page(pdf, Letter.PORTRAIT);
                y = 50f;
            }
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_32();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_32 => " + (time1 - time0));
    }
}   // End of Example_32.cs
