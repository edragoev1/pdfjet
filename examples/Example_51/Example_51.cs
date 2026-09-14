using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_51.cs
 *
 * Merges existing PDF documents into one, after a cover page drawn with PDFjet.
 * The pages of each document follow in their order and keep their content,
 * resources, annotations and links. The parts of a document that belong to the
 * whole document, such as its bookmarks, form fields and tagging, are left out.
 */
public class Example_51 {
    public Example_51() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_51.pdf", FileMode.Create)));

        String[] fileNames = {
            "data/testPDFs/wirth.pdf",
            "data/testPDFs/rc65-16e.pdf",
            "data/testPDFs/PDFjetLogo.pdf"
        };
        List<List<PDFobj>> documents = new List<List<PDFobj>>();
        foreach (String fileName in fileNames) {
            using (BufferedStream stream = new BufferedStream(
                    new FileStream(fileName, FileMode.Open, FileAccess.Read))) {
                documents.Add(pdf.Read(stream));
            }
        }

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(24f);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(f1, "Merged documents").SetLocation(50f, 80f).DrawOn(page);
        float y = 130f;
        for (int i = 0; i < fileNames.Length; i++) {
            int pages = pdf.GetPageObjects(documents[i]).Count;
            String text = fileNames[i] + ", " + pages + (pages == 1 ? " page" : " pages");
            new TextLine(f2, text).SetLocation(50f, y).DrawOn(page);
            y += 20f;
        }

        foreach (List<PDFobj> objects in documents) {
            pdf.Merge(objects);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_51();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine("Example_51 => " + (time1 - time0) + " ms");
    }
}   // End of Example_51.cs
