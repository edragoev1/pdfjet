using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;

/**
 *  Example_74.cs
 *
 */
public class Example_74 {

    public Example_74() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_74.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(72f);

        Font f2 = new Font(pdf,
                new FileStream(
                        "fonts/Droid/DroidSans.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);
        f2.SetSize(24f);


        Page page = new Page(pdf, Letter.PORTRAIT);

        StringBuilder buf = new StringBuilder();
        buf.Append("Heya, World! This is a test to show the functionality of a TextBox.");

        float x1 = 90f;
        float y1 = 150f;

        Point p1 = new Point(x1, y1);
        p1.SetRadius(5f);
        p1.SetFillShape(true);
        p1.DrawOn(page);

        TextLine textline = new TextLine(f2, "(x1, y1)");
        textline.SetLocation(x1, y1 - 15f);
        textline.DrawOn(page);

        float width = 500f;
        float height = 450f;
        TextBox textBox = new TextBox(f1, buf.ToString());
        textBox.SetLocation(x1, y1);
        textBox.SetWidth(width);
        textBox.SetHeight(height);
        textBox.SetMargin(0f);
        textBox.SetSpacing(0f);
        float[] xy = textBox.DrawOn(page);

        float x2 = x1 + width;
        float y2 = y1 + textBox.GetHeight();

        f2.SetSize(18f);

        // Text on the left
        TextLine ascent_text = new TextLine(f2, "Ascent");
        ascent_text.SetLocation(x1 - 85f, y1 + 40f); //(y1 + f1.getAscent()) / 2);
        ascent_text.DrawOn(page);

        TextLine descent_text = new TextLine(f2, "Descent");
        descent_text.SetLocation(x1 - 85f, y1 + f1.GetAscent() + 15f);
        descent_text.DrawOn(page);

        // Lines beside the text
        Line arrow_line1 = new Line(x1 - 10f, y1, x1 - 10f, y1 + f1.GetAscent());
        arrow_line1.SetColor(Color.blue);
        arrow_line1.SetWidth(3f);
        arrow_line1.DrawOn(page);

        Line arrow_line2 = new Line(x1 - 10f, y1 + f1.GetAscent(),
                            x1 - 10f, y1 + f1.GetAscent() + f1.GetDescent());
        arrow_line2.SetColor(Color.red);
        arrow_line2.SetWidth(3f);
        arrow_line2.DrawOn(page);


        // Lines for first line of text
        Line text_line1 = new Line(x1, y1 + f1.GetAscent(), x2, y1 + f1.GetAscent());
        text_line1.DrawOn(page);

        Line descent_line1 = new Line(x1, y1 + (f1.GetAscent() + f1.GetDescent()),
                                x2, y1 + (f1.GetAscent() + f1.GetDescent()));
        descent_line1.DrawOn(page);


        // Lines for second line of text
        float curr_y = y1 + f1.GetBodyHeight();

        Line text_line2 = new Line(x1, curr_y + f1.GetAscent(), x2, curr_y + f1.GetAscent());
        text_line2.DrawOn(page);

        Line descent_line2 = new Line(x1, curr_y + f1.GetAscent() + f1.GetDescent(),
                                x2, curr_y + f1.GetAscent() + f1.GetDescent());
        descent_line2.DrawOn(page);


        Point p2 = new Point(x2, y2);
        p2.SetRadius(5f);
        p2.SetFillShape(true);
        p2.DrawOn(page);

        f2.SetSize(24f);
        TextLine textline2 = new TextLine(f2, "(x2, y2)");
        textline2.SetLocation(x2 - 80f, y2 + 30f);
        textline2.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        pdf.Complete();
    }


    public void drawTextAndLines(
            String text, Page page, Font font, float x, float y) {
        TextLine textline = new TextLine(font, text);
        textline.SetLocation(x, y);
        textline.DrawOn(page);

        Line ascenderLine = new Line(x, y - font.GetAscent(), x + 100f, y - font.GetAscent());
        ascenderLine.SetWidth(2f);
        ascenderLine.DrawOn(page);

        Line line = new Line(x, y, x + 100f, y);
        line.SetWidth(2f);
        line.DrawOn(page);

        Line descenderLine = new Line(x, y + font.GetDescent(), x + 100f, y + font.GetDescent());
        descenderLine.SetWidth(2f);
        descenderLine.DrawOn(page);
    }


    public static void Main(String[] args) {
        System.Diagnostics.Stopwatch sw =
                System.Diagnostics.Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_74();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_74 => " + (time1 - time0));
    }

}   // End of Example_74.cs
