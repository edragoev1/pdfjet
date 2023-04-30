using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;
using System.Text;

/**
 *  Example_19.cs
 */
public class Example_19 {
    public Example_19() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_19.pdf", FileMode.Create)));
        Font f1 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");
        f1.SetSize(10f);
        f2.SetSize(10f);
        String contents = Contents.OfTextFile("data/calculus-short.txt");

        Page page = new Page(pdf, Letter.PORTRAIT);
        // Columns x coordinates
        float x1 = 50f;
        float y1 = 50f;
        float x2 = 300f;
        float w2 = 300f;    // Width of the second column

        Image image1 = new Image(pdf, "images/fruit.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");

        // Draw the first image and text:
        image1.SetLocation(x1, y1);
        image1.ScaleBy(0.75f);
        float[] xy = image1.DrawOn(page);

        TextBox textBox = new TextBox(f1);
        textBox.SetText(contents);
        textBox.SetLocation(x2, y1);
        textBox.SetWidth(w2);
        textBox.SetBorders(true);
        // textBox.SetTextAlignment(Align.RIGHT);
        // textBox.SetTextAlignment(Align.CENTER);
        xy = textBox.DrawOn(page);

        // Draw the second row image and text:
        image2.SetLocation(x1, xy[1] + 10f);
        image2.ScaleBy(1f/3f);
        image2.DrawOn(page);

        textBox = new TextBox(f1);
        textBox.SetText(Contents.OfTextFile("data/latin.txt"));
        textBox.SetLocation(x2, xy[1] + 10f);
        textBox.SetWidth(w2);
        textBox.SetBorders(true);
        xy = textBox.DrawOn(page);

        textBox = new TextBox(f1);
        textBox.SetFallbackFont(f2);
        textBox.SetText(Contents.OfTextFile("data/chinese.txt"));
        textBox.SetLocation(x1, 530f);
        textBox.SetWidth(350f);
        textBox.SetBorders(true);
        xy = textBox.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

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
