using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using PDFjet.NET;

/**
 * Example_47.cs
 */
public class Example_47 {
    public Example_47() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_47.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Italic.ttf.stream");

        f1.SetSize(12f);
        f2.SetSize(12f);

        Image image1 = new Image(pdf, "images/AU-map.png");
        image1.ScaleBy(0.50f);

        Image image2 = new Image(pdf, "images/HU-map.png");
        image2.ScaleBy(0.50f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        image1.SetLocation(20f, 20f);
        image1.DrawOn(page);

        image2.SetLocation(
                page.GetWidth() - (image2.GetWidth() + 20f),
                page.GetHeight() - (image2.GetHeight() + 20f));
        image2.DrawOn(page);

        List<TextLine> paragraphs = new List<TextLine>();

        StringBuilder buf = new StringBuilder();
        StreamReader reader = new StreamReader("data/austria_hungary.txt");

        String text = null;
        while ((text = reader.ReadLine()) != null) {
            buf.Append(text);
            buf.Append("\n");
        }
        reader.Close();

        String[] textLines = Regex.Split(buf.ToString(), "\n\n");
        foreach (String textLine in textLines) {
            paragraphs.Add(new TextLine(f1, textLine));
        }

        float xPos = 20f;
        float yPos = 250f;

        float width = 180f;
        float height = 315f;

        TextFrame frame = new TextFrame(paragraphs);
        frame.SetLocation(xPos, yPos);
        frame.SetWidth(width);
        frame.SetHeight(height);
        frame.SetDrawBorder(true);
        frame.DrawOn(page);

        xPos += 200f;
        if (frame.IsNotEmpty()) {
            frame.SetLocation(xPos, yPos);
            frame.SetWidth(width);
            frame.SetHeight(height);
            frame.SetDrawBorder(false);
            frame.DrawOn(page);
        }

        xPos += 200f;
        if (frame.IsNotEmpty()) {
            frame.SetLocation(xPos, yPos);
            frame.SetWidth(width);
            frame.SetHeight(height);
            frame.SetDrawBorder(true);
            frame.DrawOn(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        System.Diagnostics.Stopwatch sw =
                System.Diagnostics.Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_47();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_47", time0, time1);
    }
} // End of Example_47.cs
