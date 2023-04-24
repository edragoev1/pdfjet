using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_06.cs
 *  We will draw the American flag using Box, Line and Point objects.
 */
// namespace examples {

public class Example_06 {
    public Example_06() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_06.pdf", FileMode.Create)));
        pdf.SetTitle("Hello");
        pdf.SetAuthor("Eugene");
        pdf.SetSubject("Example");
        pdf.SetKeywords("Hello World This is a test");
        pdf.SetCreator("Application Name");

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02.cs", Compress.YES);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Box flag = new Box();
        flag.SetLocation(100.0f, 100.0f);
        flag.SetSize(190.0f, 100.0f);
        flag.SetColor(Color.white);
        flag.DrawOn(page);

        float sw = 7.69f;       // stripe width
        Line stripe = new Line(0.0f, sw/2, 190.0f, sw/2);
        stripe.SetWidth(sw);
        stripe.SetColor(Color.oldgloryred);
        for (int row = 0; row < 7; row++) {
            stripe.PlaceIn(flag, 0.0f, row * 2 * sw);
            stripe.DrawOn(page);
        }

        Box union = new Box();
        union.SetSize(76.0f, 53.85f);
        union.SetColor(Color.oldgloryblue);
        union.SetFillShape(true);
        union.PlaceIn(flag, 0.0f, 0.0f);
        union.DrawOn(page);

        float h_si = 12.6f;    // horizontal star interval
        float v_si = 10.8f;    // vertical star interval
        Point star = new Point(h_si/2, v_si/2);
        star.SetShape(Point.STAR);
        star.SetRadius(3.0f);
        star.SetColor(Color.white);
        star.SetFillShape(true);

        for (int row = 0; row < 6; row++) {
            for (int col = 0; col < 5; col++) {
                star.PlaceIn(union, row * h_si, col * v_si);
                star.DrawOn(page);
            }
        }

        star.SetLocation(h_si, v_si);
        for (int row = 0; row < 5; row++) {
            for (int col = 0; col < 4; col++) {
                star.PlaceIn(union, row * h_si, col * v_si);
                star.DrawOn(page);
            }
        }

        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.SetLocation(100f, 300f);
        attachment.SetIconPushPin();
        attachment.SetIconSize(24f);
        attachment.SetTitle("Attached File: " + file1.GetFileName());
        attachment.SetDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.SetLocation(200f, 300f);
        attachment.SetIconPaperclip();
        attachment.SetIconSize(24f);
        attachment.SetTitle("Attached File: " + file2.GetFileName());
        attachment.SetDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_06();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_06", time0, time1);
    }

}   // End of Example_06.cs

// }   // End of namespace examples
