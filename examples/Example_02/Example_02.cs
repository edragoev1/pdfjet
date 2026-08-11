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

        // Font f1 = new Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream");
        Font f1 = new Font(pdf, IBMPlexSansJP.Regular);
        f1.SetSize(14f);

        Font f2 = new Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream");
        f2.SetSize(14f);

        Font f3 = new Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf.stream");
        // Font f3 = new Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream");
        f3.SetSize(14f);

        Font f4 = new Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular-TC4808.ttf.stream");
        // Font f4 = new Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream");
        f4.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(f1,
                File.ReadAllText("data/languages/japanese.txt"));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(415f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        textBlock = new TextBlock(f2,
                File.ReadAllText("data/languages/korean.txt"));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(415f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        textBlock = new TextBlock(
                f3, File.ReadAllText("data/languages/simplified-chinese.txt"));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(415f);
        textBlock.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        textBlock = new TextBlock(
                f4, File.ReadAllText("data/languages/traditional-chinese.txt"));
        textBlock.SetLocation(50f, 50f);
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
