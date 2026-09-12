using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_16.cs
 */
public class Example_16 {
    public Example_16() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_16.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Text block with highlighted keywords");

        // Font f1 = new Font(pdf, SourceSerif4.Regular);
        // Font f1 = new Font(pdf, NotoSans.Regular);
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["Everyone"] = Color.red;
        colors["pay"] = Color.green;
        colors["freedom"] = Color.blue;

        // page.SaveGraphicsState();

        GraphicsState gs = new GraphicsState();
        gs.SetAlphaStroking(0.5f);                  // Stroking alpha
        gs.SetAlphaNonStroking(0.5f);               // Non-Stroking alpha
        page.SetGraphicsState(gs);

        String englishText = Content.OfTextFile("data/languages/english.txt");
        // f1.SetSize(14f);
        TextBox textBox = new TextBox(f1, englishText);
        // textBox.SetLocation(50f, 50f);
        // textBox.SetLocation(50f, 100f);
        textBox.SetLocation(100f, 50f);
        textBox.SetWidth(400f);
        // If no height is specified the height will be calculated based on the text.
        textBox.SetHeight(450f);
        // textBox.SetTextDirection(Direction.LEFT_TO_RIGHT);
        // textBox.SetTextDirection(Direction.BOTTOM_TO_TOP);
        // textBox.SetTextDirection(Direction.TOP_TO_BOTTOM);

        textBox.SetVerticalAlignment(Align.TOP);
        // textBox.SetVerticalAlignment(Align.BOTTOM);
        // textBox.SetVerticalAlignment(Align.CENTER);

        // textBox.SetTextAlignment(Align.CENTER);
        // textBox.SetHeight(400f);

        textBox.SetBackgroundColor(Color.whitesmoke);
        textBox.SetTextColors(colors);
        textBox.SetBorders(true);
        float[] xy = textBox.DrawOn(page);

        page.SetGraphicsState(new GraphicsState()); // Reset GS
        // page.RestoreGraphicsState();

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.SetBorderColor(Color.black);
        rect.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_16();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_16", time0, time1);
    }
}   // End of Example_16.cs
