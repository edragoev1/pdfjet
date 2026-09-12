using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_37.cs
 */
class Example_37 {
    public Example_37(String fileName) {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_37.pdf", FileMode.Create)));
        List<PDFobj> objects = pdf.Read(new FileStream(fileName, FileMode.Open, FileAccess.Read));

        Font f1 = new Font(objects,
                new FileStream(IBMPlexSans.Regular,
                FileMode.Open,
                FileAccess.Read), Font.STREAM);
        f1.SetSize(72f);

        TextLine text = new TextLine(f1, "This is a test!");
        text.SetLocation(150f, 350f);
        text.SetTextColor(Color.peru);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        foreach (PDFobj pageObj in pages) {
            GraphicsState gs = new GraphicsState();
            gs.SetAlphaStroking(0.75f);         // Stroking alpha
            gs.SetAlphaNonStroking(0.75f);      // Non-stroking alpha
            pageObj.SetGraphicsState(gs, objects);

            Page page = new Page(pdf, pageObj);
            page.AddResource(f1, objects);
            page.SetBrushColor(Color.blue);
            // page.DrawString(f1, "Hello, World!", 50f, 200f);
            text.DrawOn(page);

            page.Complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.AddObjects(objects);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_37("data/testPDFs/wirth.pdf");
        // new Example_37("../../eBooks/UniversityPhysicsVolume1.pdf");
        // new Example_37("../../eBooks/Smalltalk-and-OO.pdf");
        // new Example_37("../../eBooks/InsideSmalltalk1.pdf");
        // new Example_37("../../eBooks/InsideSmalltalk2.pdf");
        // new Example_37("../../eBooks/Greenbook.pdf");
        // new Example_37("../../eBooks/Bluebook.pdf");
        // new Example_37("../../eBooks/Orangebook.pdf");
        long time1 = sw.ElapsedMilliseconds;
        TextUtils.PrintDuration("Example_37", time0, time1);
    }
}   // End of Example_37.cs
