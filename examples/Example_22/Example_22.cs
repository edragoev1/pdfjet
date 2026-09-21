/*
 * Example_22.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_22.cs
 * This example links a contents page to three chapters, and each chapter
 * back to the contents, with destinations and "Go To" actions.
 */
public class Example_22 {
    public Example_22() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_22.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Internal links and destinations");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        String[] chapters = {
            "Destinations",
            "Go To actions",
            "Links on shapes and images",
        };
        String[] texts = {
            "A destination is a named place in a document. The title of this chapter "
                + "is the destination \"chapter1\", set with setDestination.",
            "A Go To action makes something a link to a destination. The chapter titles "
                + "on the contents page are text lines with a Go To action.",
            "Rectangles and images can be links too. Click the arrow in the top left "
                + "corner, or the arrow image next to it, to go back to the contents.",
        };

        // The contents page. The destination is the top of the page.
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.AddDestination("contents", 0f, 0f);

        TextLine text = new TextLine(f2, "Contents");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(24f);
        text.SetLocation(90f, 100f);
        text.DrawOn(page);

        // The contents are a list: each item is the number of its chapter,
        // which labels the item, and the title of the chapter, which is the
        // body of the item and the link to it. A reader reads them as the
        // items of a list and not as lines of text that follow one another.
        float y = 150f;
        page.BeginStructElement(StructElem.L);
        for (int i = 0; i < chapters.Length; i++) {
            page.BeginStructElement(StructElem.LI);
            String label = "Chapter " + (i + 1) + ":";
            text = new TextLine(f1, label);
            text.SetStructureType(StructElem.LBL);
            text.SetFontSize(14f);
            text.SetLocation(90f, y);
            text.DrawOn(page);

            // The title, its underline and its link are the body of the item,
            // which is what an LI may hold beside its label.
            page.BeginStructElement(StructElem.LBODY);
            text = new TextLine(f1, chapters[i]);
            text.SetFontSize(14f);
            text.SetTextColor(Color.blue);
            text.SetUnderline(true);
            text.SetGoToAction("chapter" + (i + 1));
            text.SetLocation(90f + f1.StringWidth(14f, label + " "), y);
            text.DrawOn(page);
            page.EndStructElement();
            page.EndStructElement();
            y += 30f;
        }
        page.EndStructElement();

        for (int i = 0; i < chapters.Length; i++) {
            page = new Page(pdf, Letter.PORTRAIT);

            // The title of the chapter is its destination.
            text = new TextLine(f2, "Chapter " + (i + 1) + ": " + chapters[i]);
            text.SetFontSize(20f);
            text.SetDestination("chapter" + (i + 1));
            text.SetLocation(90f, 100f);
            text.DrawOn(page);

            TextBlock textBlock = new TextBlock(f1, texts[i]);
            textBlock.SetFontSize(12f);
            textBlock.SetLineSpacing(1.5f);
            textBlock.SetLocation(90f, 125f);
            textBlock.SetWidth(430f);
            textBlock.DrawOn(page);

            text = new TextLine(f1, "Back to the contents");
            text.SetFontSize(12f);
            text.SetTextColor(Color.blue);
            text.SetUnderline(true);
            text.SetGoToAction("contents");
            text.SetLocation(90f, 250f);
            text.DrawOn(page);
        }

        // On the last page, a rect with no border links to the contents too.
        Rect rect = new Rect(20f, 20f, 20f, 20f);
        rect.SetGoToAction("contents");
        rect.DrawOn(page);

        // Create an up arrow and place it in the rect
        PDFjet.NET.Path path = new PDFjet.NET.Path();
        path.Add(new Point(30f, 21f));
        path.Add(new Point(37f, 29f));
        path.Add(new Point(33f, 29f));
        path.Add(new Point(33f, 39f));
        path.Add(new Point(27f, 39f));
        path.Add(new Point(27f, 29f));
        path.Add(new Point(23f, 29f));
        path.SetClosed(true);
        path.SetStrokeColor(Color.deepskyblue);
        path.SetFillShape(true);
        path.DrawOn(page);

        // And so does an image of an arrow.
        Image image = new Image(pdf, "images/up-arrow.png");
        image.SetAltDescription(
                "An arrow pointing up, which goes back to the contents of the document when it is clicked.");
        image.SetLocation(50f, 20f);
        image.SetGoToAction("contents");
        image.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_22();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_22 => {time1 - time0,4} ms");
    }
}   // End of Example_22.cs
