using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_71.java
 *
 */
public class Example_71 {

    public Example_71() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_71.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSerif-Bold.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSerif-Italic.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        CalendarMonth calendar = new CalendarMonth(f1, f2, 2018, 9);
        calendar.SetLocation(0f, 0f);
        float[] point = calendar.DrawOn(page);

	    CalendarMonth calendar2 = new CalendarMonth(f1, f2, 2018, 10);
        calendar2.SetLocation(0f, point[1]);
        calendar2.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_71();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_71 => " + (time1 - time0));
    }

}   // End of Example_71.java
