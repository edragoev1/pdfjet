/*
 * Example_06.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_06.cs
 * This example attaches two files to a page, and adds a note, a link and
 * polygon, square and circle annotations next to labels that describe them.
 */
public class Example_06 {
    public Example_06() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_06.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Attachments and Annotations");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(14f);

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02/Example_02.cs", true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Attachments and annotations");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(22f);
        text.SetLocation(70f, 80f);
        text.DrawOn(page);

        text = new TextLine(f1,
                "Open this page in a PDF viewer that shows annotations, and hover over the icons.");
        text.SetTextColor(Color.gray);
        text.SetLocation(70f, 105f);
        text.DrawOn(page);

        // File attachments. The files are stored inside the PDF.
        new TextLine(f2, "Attached files").SetStructureType(StructElem.H2).SetLocation(70f, 160f).DrawOn(page);

        FileAttachment attachment = new FileAttachment(file1);
        attachment.SetLocation(70f, 175f);
        attachment.SetIconPushPin();
        attachment.SetTitle("Attached File: " + file1.GetFileName());
        attachment.SetContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);
        new TextLine(f1, "linux-logo.png, an image, with a push pin icon")
                .SetLocation(105f, 192f).DrawOn(page);

        attachment = new FileAttachment(file2);
        attachment.SetLocation(70f, 210f);
        attachment.SetIconPaperclip();
        attachment.SetTitle("Attached File: " + file2.GetFileName());
        attachment.SetContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);
        new TextLine(f1, "The source code of Example_02, with a paperclip icon")
                .SetLocation(105f, 227f).DrawOn(page);

        // A note, and a link.
        new TextLine(f2, "A note and a link").SetStructureType(StructElem.H2).SetLocation(70f, 290f).DrawOn(page);

        TextAnnotation textAnnotation = new TextAnnotation();
        textAnnotation.SetLocation(70f, 305f);
        textAnnotation.SetSize(24f, 24f);
        textAnnotation.SetTitle("Reviewer");
        textAnnotation.SetContents("Please check the figures on page 2.");
        textAnnotation.DrawOn(page);
        new TextLine(f1, "A text annotation: click the note icon to read it")
                .SetLocation(105f, 322f).DrawOn(page);

        text = new TextLine(f1, "Visit https://pdfjet.com");
        text.SetTextColor(Color.blue);
        text.SetUnderline(true);
        text.SetURIAction("https://pdfjet.com");
        text.SetLocation(105f, 357f);
        text.DrawOn(page);

        // Shape annotations, drawn half transparent over the page.
        new TextLine(f2, "Shape annotations").SetLocation(70f, 420f).DrawOn(page);

        PolygonAnnotation polygonAnnotation = new PolygonAnnotation();
        polygonAnnotation.SetLocation(70f, 440f);
        polygonAnnotation.SetVertices(new float[] {0f, 60f, 30f, 0f, 60f, 60f, 0f, 60f});
        polygonAnnotation.SetFillColor(Color.red);
        polygonAnnotation.SetOpacity(0.5f);
        polygonAnnotation.SetTitle("Polygon");
        polygonAnnotation.SetContents("Polygon Annotation");
        polygonAnnotation.DrawOn(page);

        SquareAnnotation squareAnnotation = new SquareAnnotation();
        squareAnnotation.SetLocation(170f, 440f);
        squareAnnotation.SetSize(60f, 60f);
        squareAnnotation.SetFillColor(new float[] {0f, 0.5f, 0f});
        squareAnnotation.SetOpacity(0.5f);
        squareAnnotation.SetTitle("Square");
        squareAnnotation.SetContents("Square Annotation");
        squareAnnotation.DrawOn(page);

        CircleAnnotation circleAnnotation = new CircleAnnotation();
        circleAnnotation.SetLocation(270f, 440f);
        circleAnnotation.SetSize(60f, 60f);
        circleAnnotation.SetFillColor(new float[] {0f, 0f, 1f});
        circleAnnotation.SetOpacity(0.5f);
        circleAnnotation.SetTitle("Circle");
        circleAnnotation.SetContents("Circle Annotation");
        circleAnnotation.DrawOn(page);

        new TextLine(f1, "Polygon").SetLocation(78f, 520f).DrawOn(page);
        new TextLine(f1, "Square").SetLocation(180f, 520f).DrawOn(page);
        new TextLine(f1, "Circle").SetLocation(283f, 520f).DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_06();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_06 => {time1 - time0,4} ms");
    }
}   // End of Example_06.cs
