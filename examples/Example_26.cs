using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_26.cs
 */
public class Example_26 {
    public Example_26() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_26.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 50f;
        float y = 50f;

        new CheckBox(f1, "Hello")
                .SetLocation(x, y)
                .SetCheckmark(Color.blue)
                .Check(Mark.CHECK)
                .DrawOn(page);

        y += 30f;
        new CheckBox(f1, "World!")
                .SetLocation(x, y)
                .SetCheckmark(Color.blue)
                .SetURIAction("http://pdfjet.com")
                .Check(Mark.CHECK)
                .DrawOn(page);

        y += 30f;
        new CheckBox(f1, "This is a test.")
                .SetLocation(x, y)
                .SetURIAction("http://pdfjet.com")
                .DrawOn(page);

        y += 30f;
        new RadioButton(f1, "Hello, World!")
                .SetLocation(x, y)
                .Select(true)
                .DrawOn(page);

        float[] xy = (new RadioButton(f1, "Yes"))
                .SetLocation(x + 100f, 50f)
                .SetURIAction("http://pdfjet.com")
                .Select(true)
                .DrawOn(page);

        xy = (new RadioButton(f1, "No"))
                .SetLocation(xy[0], 50f)
                .DrawOn(page);

        xy = (new CheckBox(f1, "Hello"))
                .SetLocation(xy[0], 50f)
                .SetCheckmark(Color.blue)
                .Check(Mark.X)
                .DrawOn(page);

        xy = (new CheckBox(f1, "Yahoo")
                .SetLocation(xy[0], 50f)
                .SetCheckmark(Color.blue)
                .Check(Mark.CHECK)
                .DrawOn(page));

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_26();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_26", time0, time1);
    }
}   // End of Example_26.cs
