using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_32.cs
 */
public class Example_32 {

    private Font f1;
    private float x = 50f;
    private float y = 50f;
    private float leading = 14f;

    public Example_32() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_32.pdf", FileMode.Create)));

        f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(10f);

        StreamReader reader = new StreamReader(
                new FileStream("examples/Example_02.cs", FileMode.Open, FileAccess.Read));

        String line = reader.ReadLine();
        Page page = null;
        while (line != null) {
            if (page == null) {
                y = 50f;
                page = NewPage(pdf);
            }
            page.Println(line);
            y += leading;
            if (y > (Letter.PORTRAIT[1] - 20f)) {
                page.SetTextEnd();
                page = null;
            }
            line = reader.ReadLine();
        }
        if (page != null) {
            page.SetTextEnd();
        }
        reader.Close();

        pdf.Complete();
    }


    private Page NewPage(PDF pdf) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetTextStart();
        page.SetTextFont(f1);
        page.SetTextLocation(x, y);
        page.SetTextLeading(leading);
        return page;
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_32();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_32 => " + (time1 - time0));
    }

}   // End of Example_32.cs
