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

        Font font = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream");
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

        Page page = new Page(pdf, Letter.PORTRAIT);
        float x = 50f;
        float y = 50f;
        float leading = font.GetBodyHeight();
        List<String> lines = Text.ReadLines("examples/Example_02.cs");
        foreach (String line in lines) {
            page.DrawString(font, null, line, x, y, Color.black, colors);
            y += leading;
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
        TextUtils.PrintDuration("Example_32", time0, time1);
    }
}   // End of Example_32.cs
