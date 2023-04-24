using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_19.cs
 */
public class Example_19 {
    public Example_19() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_19.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        f1.SetSize(10f);
        f2.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;

        float x2 = 300f;

        // Width of the second column:
        float w2 = 300f;

        Image image1 = new Image(pdf, "images/fruit.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");

        // Draw the first image and text:
        image1.SetLocation(x1, y1);
        image1.ScaleBy(0.75f);
        image1.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1);
        textBlock.SetText("Geometry arose independently in a number of early cultures as a practical way for dealing with lengths, areas, and volumes.");
        textBlock.SetLocation(x2, y1);
        textBlock.SetWidth(w2);
        textBlock.SetDrawBorder(true);
        // textBlock.SetTextAlignment(Align.RIGHT);
        // textBlock.SetTextAlignment(Align.CENTER);
        float[] xy = textBlock.DrawOn(page);

        // Draw the second row image and text:
        image2.SetLocation(x1, xy[1] + 10f);
        image2.ScaleBy(1f/3f);
        image2.DrawOn(page);

        textBlock = new TextBlock(f1);
        textBlock.SetText("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.\n\n");
        textBlock.SetLocation(x2, xy[1] + 10f);
        textBlock.SetWidth(w2);
        textBlock.SetDrawBorder(true);
        textBlock.DrawOn(page);

        textBlock = new TextBlock(f1);
        textBlock.SetFallbackFont(f2);
        textBlock.SetText("保健所によると、女性は１３日に旅行先のタイから札幌に戻り、１６日午後５～８時ごろ同店を訪れ、帰宅後に発熱などの症状が出て、２３日に医療機関ではしかと診断された。はしかのウイルスは発症日の１日前から感染者の呼吸などから放出され、本人がいなくなっても、２時間程度空気中に漂い、空気感染する。保健所は１６日午後５～１１時に同店を訪れた人に、発熱などの異常が出た場合、早期にマスクをして医療機関を受診するよう呼びかけている。（本郷由美子）");
        textBlock.SetLocation(x1, 600f);
        textBlock.SetWidth(350f);
        textBlock.SetDrawBorder(true);
        textBlock.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_19();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_19", time0, time1);
    }
}   // End of Example_19.cs
