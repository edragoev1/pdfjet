using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/// <summary>
/// Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
/// with Courier. None of these fonts is embedded: the PDF names them and the
/// viewer supplies them.
/// </summary>
/// <remarks>
/// The advantage is size and speed. A CJK font holds tens of thousands of
/// glyphs, and this document carries none of them, so it is a few kilobytes
/// and is written in a moment. The disadvantages: the viewer must have the
/// Adobe Asian font packs, or a substitute, and the text takes the shapes and
/// widths of whatever font it finds, so the document does not look the same
/// everywhere; the core font Courier is limited to the WinAnsi characters; and
/// a document with a font that is not embedded cannot claim PDF/A or PDF/UA
/// compliance. To ship the glyphs with the document, use an embedded font like
/// IBM Plex Sans JP, KR, SC or TC, as Example_02 and 19 do.
/// </remarks>
public class Example_04 {

    public Example_04() {
        // Create a new PDF document
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_04.pdf", FileMode.Create)));

        Font f0 = new Font(pdf, CoreFont.COURIER);
        f0.SetSize(14f);

        // Create font for Traditional Chinese text
        // Uses Adobe's Ming Standard Light font (明體)
        Font f1 = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);
        f1.SetSize(14f);

        // Create font for Simplified Chinese text
        // Uses Adobe's Heiti SC Light font (黑体-简)
        Font f2 = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT);
        f2.SetSize(14f);

        // Create font for Japanese text
        // Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
        Font f3 = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR);
        f3.SetSize(14f);

        // Create font for Korean text
        // Uses Adobe's Myungjo Standard Medium font (명조체)
        Font f4 = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM);
        f4.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        String fileName = "data/happy-new-year.txt";
        float x_pos = 100f;
        float y_pos = 100f;
        StreamReader reader = new StreamReader(
                new FileStream(fileName, FileMode.Open, FileAccess.Read));
        TextLine text = new TextLine(f0);
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            text.SetText(line);
            text.SetLocation(x_pos, y_pos);
            text.DrawOn(page);
            if (line.Contains("Traditional")) {
                text.SetFont(f1);
            } else if (line.Contains("Simplified")) {
                text.SetFont(f2);
            } else if (line.Contains("Japanese")) {
                text.SetFont(f3);
            } else if (line.Contains("Korean")) {
                text.SetFont(f4);
            } else {
                text.SetFont(f0);
            }
            y_pos += 25f;
        }
        reader.Close();

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_04();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine("Example_04 => " + (time1 - time0) + " ms");
    }
}   // End of Example_04.cs
