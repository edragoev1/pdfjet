using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/**
 *  Example_73.cs
 */
public class Example_73 {
    public Example_73() {
        PDF pdf = new PDF(
                new BufferedStream(
                        new FileStream("Example_73.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        f1.SetSize(12f);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine line1 = new TextLine(f1, "Hello, Beautiful World");
        TextLine line2 = new TextLine(f1, "Hello,BeautifulWorld");

        TextBox textBox = new TextBox(f1, line1.GetText());
        textBox.SetMargin(0f);
        textBox.SetLocation(50f, 50f);
        textBox.SetWidth(line1.GetWidth() + 2*textBox.GetMargin());
        textBox.SetBgColor(Color.lightgreen);
        // The DrawOn method returns the x and y of the bottom right corner of the TextBox
        float[] xy = textBox.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        textBox = new TextBox(f1, line1.GetText() + "!");
        textBox.SetWidth(line1.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 100f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);
        
        textBox = new TextBox(f1, line2.GetText());
        textBox.SetWidth(line2.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 200f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        textBox = new TextBox(f1, line2.GetText() + "!");
        textBox.SetWidth(line2.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 300f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        textBox = new TextBox(f1, line2.GetText() + "! Left Align");
        textBox.SetMargin(10f);
        textBox.SetWidth(line2.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 400f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        textBox = new TextBox(f1, line2.GetText() + "! Right Align");
        textBox.SetMargin(10f);
        textBox.SetTextAlignment(Align.RIGHT);
        textBox.SetWidth(line2.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 500f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        textBox = new TextBox(f1, line2.GetText() + "! Center");
        textBox.SetMargin(10f);
        textBox.SetTextAlignment(Align.CENTER);
        textBox.SetWidth(line2.GetWidth() + 2*textBox.GetMargin());
        textBox.SetLocation(50f, 600f);
        xy = textBox.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        String text = Content.OfTextFile("data/chinese-text.txt");

        textBox = new TextBox(f1);
        textBox.SetFallbackFont(f2);
        textBox.SetText(text);
        // textBox.SetMargin(10f);
        textBox.SetBgColor(Color.lightblue);
        textBox.SetVerticalAlignment(Align.TOP);
        // textBox.SetHeight(210f);
        // textBox.SetHeight(151f);
        textBox.SetHeight(14f);
        textBox.SetWidth(300f);
        textBox.SetLocation(250f, 50f);
        textBox.DrawOn(page);

        textBox = new TextBox(f1);
        textBox.SetFallbackFont(f2);
        textBox.SetText(text);
        // textBox.SetMargin(10f);
        textBox.SetBgColor(Color.lightblue);
        textBox.SetVerticalAlignment(Align.CENTER);
        // textBox.SetHeight(210f);
        textBox.SetHeight(151f);
        textBox.SetWidth(300f);
        textBox.SetLocation(250f, 300f);
        textBox.DrawOn(page);

        textBox = new TextBox(f1);
        textBox.SetFallbackFont(f2);
        textBox.SetText(text);
        // textBox.SetMargin(10f);
        textBox.SetBgColor(Color.lightblue);
        textBox.SetVerticalAlignment(Align.BOTTOM);
        // textBox.SetHeight(210f);
        textBox.SetHeight(151f);
        textBox.SetWidth(300f);
        textBox.SetLocation(250f, 550f);
        textBox.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_73();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_73 => " + (time1 - time0));
    }
}   // End of Example_73.cs
