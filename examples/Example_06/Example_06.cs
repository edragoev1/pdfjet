using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_06.cs
 */
public class Example_06 {
    public Example_06() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_06.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02/Example_02.cs", Compress.YES);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.SetLocation(100f, 600f);
        attachment.SetIconPushPin();
        attachment.SetTitle("Attached File: " + file1.GetFileName());
        attachment.SetDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.SetLocation(200f, 600f);
        attachment.SetIconPaperclip();
        attachment.SetTitle("Attached File: " + file2.GetFileName());
        attachment.SetDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);

        TextLine textLine = new TextLine(f1, "pdfjet.com");
        textLine.SetLocation(300f, 618f);
        textLine.SetURIAction("https://pdfjet.com");
        textLine.DrawOn(page);

        TextAnnotation textAnnotation = new TextAnnotation();
        textAnnotation.SetLocation(400f, 600f);
        textAnnotation.SetSize(24f, 24f);
        textAnnotation.SetTitle("Hello");
        textAnnotation.SetContents("World");
        textAnnotation.DrawOn(page);

        Container container = new Container(400f, 400f);
        container.SetLocation(100f, 100f);
        container.SetBorderColor(Color.black);
        container.SetRotationClockwise(90);

        Rect rect = new Rect(0f, 0f, 25f, 25f);
        rect.SetBorderColor(Color.black);
        rect.SetBorderWidth(1f);
        container.Add(rect);

        PolygonAnnotation polygonAnnotation = new PolygonAnnotation();
        polygonAnnotation.SetLocation(0f, 0f);
        polygonAnnotation.SetVertices(new float[] {0f, 0f, 50f, 0f, 0f, 50f, 0f, 0f});
        polygonAnnotation.SetFillColor(Color.red);
        polygonAnnotation.SetTransparency(0.5f);
        polygonAnnotation.SetTitle("Polygon");
        polygonAnnotation.SetContents("Polygon Annotation");
        container.Add(polygonAnnotation);

        SquareAnnotation squareAnnotation = new SquareAnnotation();
        squareAnnotation.SetLocation(25f, 0f);
        squareAnnotation.SetSize(50f, 50f);
        squareAnnotation.SetFillColor(new float[] {0f, 0f, 1f});
        squareAnnotation.SetTransparency(0.5f);
        squareAnnotation.SetTitle("Square");
        squareAnnotation.SetContents("Square Annotation");
        container.Add(squareAnnotation);

        CircleAnnotation circleAnnotation = new CircleAnnotation();
        circleAnnotation.SetLocation(50f, 0f);
        circleAnnotation.SetSize(50f, 50f);
        circleAnnotation.SetFillColor(new float[] {0f, 0f, 1f});
        circleAnnotation.SetTransparency(0.5f);
        circleAnnotation.SetTitle("Circle");
        circleAnnotation.SetContents("Circle Annotation");
        container.Add(circleAnnotation);

        container.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_06();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_06", time0, time1);
    }
}   // End of Example_06.cs
