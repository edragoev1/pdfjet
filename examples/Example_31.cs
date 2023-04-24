using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_31.cs
 */
public class Example_31 {
    public Example_31() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_31.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, "fonts/Noto/NotoSansDevanagari-Regular.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");

        f1.SetSize(15f);
        f2.SetSize(15f);

        StringBuilder buf = new StringBuilder();
        StreamReader reader = new StreamReader(
                new FileStream("data/marathi.txt", FileMode.Open, FileAccess.Read));
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            buf.Append(line + "\n");
        }
        reader.Close();

        TextBox textBox = new TextBox(f1, buf.ToString(), 500f, 300f);
        textBox.SetFallbackFont(f2);
        textBox.SetLocation(50f, 50f);
        textBox.SetBorder(Border.LEFT);
        textBox.SetBorder(Border.RIGHT);
        textBox.DrawOn(page);

        String str = "असम के बाद UP में भी CM कैंडिडेट का ऐलान करेगी BJP?";
        TextLine textLine = new TextLine(f1, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(50f, 175f);
        textLine.DrawOn(page);

        page.SetPenColor(Color.blue);
        page.SetBrushColor(Color.blue);
        page.FillRect(50f, 200f, 200f, 200f);

        GraphicsState gs = new GraphicsState();
        gs.SetAlphaStroking(0.5f);      // The stroking alpha constant
        gs.SetAlphaNonStroking(0.5f);   // The nonstroking alpha constant
        page.SetGraphicsState(gs);

        page.SetPenColor(Color.green);
        page.SetBrushColor(Color.green);
        page.FillRect(100f, 250f, 200f, 200f);

        page.SetPenColor(Color.red);
        page.SetBrushColor(Color.red);
        page.FillRect(150, 300, 200f, 200f);

        // Reset the parameters to the default values
        page.SetGraphicsState(new GraphicsState());

        page.SetPenColor(Color.orange);
        page.SetBrushColor(Color.orange);
        page.FillRect(200, 350, 200f, 200f);

        page.SetBrushColor(0x00003865);
        page.FillRect(50, 550, 200f, 200f);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_31();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_31", time0, time1);
    }
}   // End of Example_31.cs
