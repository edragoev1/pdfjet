using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_29.cs
 */
public class Example_29 {
    public Example_29() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_29.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font font = new Font(pdf, CoreFont.HELVETICA);
        font.SetSize(16f);

        Paragraph paragraph = new Paragraph();
        paragraph.Add(new TextLine(font, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis."));

        TextColumn column = new TextColumn();
        column.SetLocation(50f, 50f);
        column.SetSize(540f, 0f);
        // column.SetLineBetweenParagraphs(true);
        column.SetLineBetweenParagraphs(false);
        column.AddParagraph(paragraph);
/*
        float[] point1 = column.DrawOn(page);
*/
        column.DrawOn(page);
        float[] point2 = column.DrawOn(null);
/*
        Dimension dim1 = column.GetSize();
        Dimension dim2 = column.GetSize();
        Dimension dim3 = column.GetSize();

        Console.WriteLine("point1.x: " + point1[0] + "    point1.y " + point1[1]);
        Console.WriteLine("point2.x: " + point2[0] + "    point2.y " + point2[1]);
        Console.WriteLine("height1: " + dim1.GetHeight());
        Console.WriteLine("height2: " + dim2.GetHeight());
        Console.WriteLine("height3: " + dim3.GetHeight());
        Console.WriteLine();
*/
        column.RemoveLastParagraph();
        column.SetLocation(50f, point2[1]);
        paragraph = new Paragraph();
        paragraph.Add(new TextLine(font, "Peter Blood, bachelor of medicine and several other things besides, smoked a pipe and tended the geraniums boxed on the sill of his window above Water Lane in the town of Bridgewater."));
        column.AddParagraph(paragraph);

        float[] xy = column.DrawOn(page);  // Draw the updated text column

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(540f, 25f);
        box.SetLineWidth(2f);
        box.SetColor(Color.darkblue);
        box.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_29();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_29", time0, time1);
    }
}   // End of Example_29.cs
