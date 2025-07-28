using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/**
 *  Example_01.cs
 */
class Example_01 {
    public Example_01() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_01.pdf", FileMode.Create)));

        Font font1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBox textBox = new TextBox(font1,
                File.ReadAllText("data/languages/english.txt"));
        textBox.SetLocation(50f, 50f);
        textBox.SetWidth(430f);
        textBox.DrawOn(page);

        textBox = new TextBox(font1,
                File.ReadAllText("data/languages/greek.txt"));
        textBox.SetLocation(50f, 250f);
        textBox.SetWidth(430f);
        textBox.DrawOn(page);

        textBox = new TextBox(font1,
                File.ReadAllText("data/languages/bulgarian.txt"));
        textBox.SetLocation(50f, 450f);
        textBox.SetWidth(430f);
        textBox.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_01();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_01", time0, time1);
    }
}   // End of Example_01.cs
