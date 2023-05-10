using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_37.cs
 */
class Example_37 {
    public Example_37() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_37.pdf", FileMode.Create)));

        FileStream fis = new FileStream("data/testPDFs/wirth.pdf", FileMode.Open, FileAccess.Read);
        // FileStream fis = new FileStream("../../eBooks/UniversityPhysicsVolume1.pdf", FileMode.Open, FileAccess.Read);
        // FileStream fis = new FileStream("data/testPDFs/Smalltalk-and-OO.pdf", FileMode.Open, FileAccess.Read);
        // FileStream fis = new FileStream("data/testPDFs/InsideSmalltalk1.pdf", FileMode.Open, FileAccess.Read);
        // FileStream fis = new FileStream("data/testPDFs/InsideSmalltalk2.pdf", FileMode.Open, FileAccess.Read);
        // FileStream fis = new FileStream("data/testPDFs/Greenbook.pdf", FileMode.Open);
        // FileStream fis = new FileStream("data/testPDFs/Bluebook.pdf", FileMode.Open);
        // FileStream fis = new FileStream("data/testPDFs/Orangebook.pdf", FileMode.Open);

        List<PDFobj> objects = pdf.Read(fis);
        fis.Close();

        Font f1 = new Font(objects,
                new FileStream("fonts/OpenSans/OpenSans-Regular.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);
        f1.SetSize(72f);

        TextLine line = new TextLine(f1, "This is a test!");
        line.SetLocation(50f, 350f);
        line.SetColor(Color.peru);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        foreach (PDFobj pageObj in pages) {
            GraphicsState gs = new GraphicsState();
            gs.SetAlphaStroking(0.75f);         // Set alpha for stroking operations
            gs.SetAlphaNonStroking(0.75f);      // Set alpha for nonstroking operations
            pageObj.SetGraphicsState(gs, objects);

            Page page = new Page(pdf, pageObj);
            page.AddResource(f1, objects);
            page.SetBrushColor(Color.blue);
            page.DrawString(f1, "Hello, World!", 50f, 200f);

            line.DrawOn(page);

            page.Complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.AddObjects(objects);
/*
        List<Image> images = new List<Image>();
        foreach (PDFobj obj in objects.Values) {
            if (obj.GetValue("/Subtype").Equals("/Image")) {
                float w = float.Parse(obj.GetValue("/Width"));
                float h = float.Parse(obj.GetValue("/Height"));
                if (w > 500f && h > 500f) {
                    images.Add(new Image(pdf, obj));
                }
            }
        }

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(72f);

        Page page = null;
        foreach (Image image in images) {
            page = new Page(pdf, A4.PORTRAIT);

            GraphicsState gs = new GraphicsState();
            gs.Set_CA(0.7f);    // Stroking alpha
            gs.Set_ca(0.7f);    // Nonstroking alpha
            page.SetGraphicsState(gs);

            image.ResizeToFit(page, true);

            // image.FlipUpsideDown(true);
            // image.SetLocation(0f, -image.GetHeight());

            // image.RotateClockwise(180);
            // image.SetLocation(0f, 0f);

            image.DrawOn(page);

            TextLine text = new TextLine(f1, "Hello, World!");
            text.SetColor(Color.blue);
            text.SetLocation(150f, 150f);
            text.DrawOn(page);

            page.SetGraphicsState(new GraphicsState());
        }
*/
        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_37();
        long time1 = sw.ElapsedMilliseconds;
        TextUtils.PrintDuration("Example_37", time0, time1);
    }
}   // End of Example_37.cs
