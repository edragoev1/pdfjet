using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;


public class Example_01 {
    public Example_01() {
        FileStream fs = new FileStream("Example_01.pdf", FileMode.Create);
        PDF pdf = new PDF(new BufferedStream(fs));

        // Font font = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        Font font = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(font,
                File.ReadAllText("data/languages/english.txt", Encoding.UTF8));
        textBlock.SetLocation(50f, 50f);
        textBlock.SetWidth(430f);
        textBlock.SetTextPadding(10f);
        float[] xy = textBlock.DrawOn(page);

        Rect rect = new Rect(xy[0], xy[1], 30f, 30f);
        rect.SetBorderColor(Color.blue);
        rect.DrawOn(page);

        textBlock = new TextBlock(font,
                File.ReadAllText("data/languages/greek.txt", Encoding.UTF8));
        textBlock.SetLocation(50f, xy[1] + 30f);
        textBlock.SetWidth(430f);
        textBlock.SetBorderColor(Color.none);
        xy = textBlock.DrawOn(page);

        textBlock = new TextBlock(font,
                File.ReadAllText("data/languages/bulgarian.txt", Encoding.UTF8));
        textBlock.SetLocation(50f, xy[1] + 30f);
        textBlock.SetWidth(430f);
        textBlock.SetTextPadding(10f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetBorderCornerRadius(10f);
        textBlock.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_01();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_01", time0, time1);
    }
}
