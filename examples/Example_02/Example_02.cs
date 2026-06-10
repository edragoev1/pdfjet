using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_02.cs
 */
public class Example_02 {
    public Example_02() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_02.pdf", FileMode.Create)));

        Font font1 = new Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream");
        font1.SetSize(12f);

        Font font2 = new Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream");
        font2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(font1,
                File.ReadAllText("data/languages/japanese.txt"));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(415f);
        textBlock.DrawOn(page);

        textBlock = new TextBlock(font2,
                File.ReadAllText("data/languages/korean.txt"));
        textBlock.SetLocation(50f, 450f);
        textBlock.SetWidth(415f);
        textBlock.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_02();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_02", time0, time1);
    }
}   // End of Example_02.cs
