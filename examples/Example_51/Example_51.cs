using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_51.cs
 *
 * Splits an existing PDF document into one PDF for each of its pages,
 * Example_51_1.pdf to Example_51_5.pdf, and writes all of its pages in reverse
 * order to Example_51.pdf. The objects that Read returns are merged into every
 * PDF. The pages keep their content, resources, annotations and links; a link
 * to a page that is not in the same PDF leads nowhere.
 */
public class Example_51 {
    public Example_51() {
        List<PDFobj> objects;
        using (BufferedStream stream = new BufferedStream(
                new FileStream("data/testPDFs/wirth.pdf", FileMode.Open, FileAccess.Read))) {
            objects = new PDF().Read(stream);
        }
        int count = new PDF().GetPageObjects(objects).Count;

        for (int i = 1; i <= count; i++) {
            PDF part = new PDF(new BufferedStream(
                    new FileStream("Example_51_" + i + ".pdf", FileMode.Create)));
            part.Merge(objects, i);
            part.Complete();
        }

        int[] reversed = new int[count];
        for (int i = 0; i < count; i++) {
            reversed[i] = count - i;
        }
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_51.pdf", FileMode.Create)));
        pdf.Merge(objects, reversed);
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_51();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_51 => {time1 - time0,4} ms");
    }
}   // End of Example_51.cs
