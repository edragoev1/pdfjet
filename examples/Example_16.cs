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

        // Font f1 = new Font(pdf, CoreFont.HELVETICA);
        // Font f1 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf");
        // Font f1 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf");
        // Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf");
        Font f1 = new Font(pdf, "fonts/RedHatText/RedHatText-Regular.ttf");
        f1.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Dictionary<String, Int32> colors = new Dictionary<String, Int32>();
        colors["Lorem"] = Color.blue;
        colors["ipsum"] = Color.red;
        colors["dolor"] = Color.green;
        colors["ullamcorper"] = Color.gray;

        GraphicsState gs = new GraphicsState();
        gs.SetAlphaStroking(0.5f);      // Set alpha for stroking operations
        gs.SetAlphaNonStroking(0.5f);   // Set alpha for nonstroking operations
        page.SetGraphicsState(gs);
/*
        f1.SetSize(72f);
        TextLine text = new TextLine(f1, "Hello, World");
        text.SetLocation(50f, 300f);
        text.DrawOn(page);
*/
        String latinText = File.ReadAllText("data/latin.txt");

        f1.SetSize(14f);
        TextBox textBox = new TextBox(f1, latinText);
        textBox.SetLocation(100f, 50f);
        textBox.SetWidth(400f);
        // If no height is specified the height will be calculated based on the text.
        textBox.SetHeight(450f);
        textBox.SetWidth(400f);
        textBox.SetHeight(450f);
        textBox.SetTextDirection(Direction.LEFT_TO_RIGHT);
        // textBox.SetTextDirection(Direction.BOTTOM_TO_TOP);
        // textBox.SetTextDirection(Direction.TOP_TO_BOTTOM);
        // textBox.setVerticalAlignment(Align.TOP);
        textBox.SetVerticalAlignment(Align.BOTTOM);
        // textBox.SetVerticalAlignment(Align.CENTER);
        // If no height is specified the height will be calculated based on the text.
        textBox.SetBgColor(Color.whitesmoke);
        textBox.SetTextColors(colors);
        textBox.SetBorders(true);
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
