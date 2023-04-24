using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_16.cs
 *
 */
public class Example_16 {
    public Example_16() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_16.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["Lorem"] = Color.blue;
        colors["ipsum"] = Color.red;
        colors["dolor"] = Color.green;
        colors["ullamcorper"] = Color.gray;

        f1.SetSize(72f);

        GraphicsState gs = new GraphicsState();
        gs.SetAlphaStroking(0.5f);      // Set alpha for stroking operations
        gs.SetAlphaNonStroking(0.5f);   // Set alpha for nonstroking operations
        page.SetGraphicsState(gs);

        TextLine text = new TextLine(f1, "Hello, World");
        text.SetLocation(50f, 300f);
        text.DrawOn(page);

        String latinText = File.ReadAllText("data/latin.txt");

        f1.SetSize(14f);
        TextBox textBox = new TextBox(f1, latinText);
        textBox.SetLocation(50f, 50f);
        textBox.SetWidth(400f);
        // If no height is specified the height will be calculated based on the text.
        // textBox.SetHeight(400f);

        // textBox.SetVerticalAlignment(Align.TOP);
        // textBox.SetVerticalAlignment(Align.BOTTOM);
        // textBox.SetVerticalAlignment(Align.CENTER);
        textBox.SetBgColor(Color.whitesmoke);
        textBox.SetTextColors(colors);

        // Find x and y without actually drawing the text box.
        // float[] xy = textBox.DrawOn(page, false);
        textBox.SetBorder(Border.ALL);
        float[] xy = textBox.DrawOn(page);

        page.SetGraphicsState(new GraphicsState()); // Reset GS

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

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
