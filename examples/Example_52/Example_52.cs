/*
 * Example_52.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

using PDFjet.NET;

/**
 * Example_52.cs
 * A whole novel, "The Idiot" by Fyodor Dostoyevsky, set as a book: a title
 * page, and each chapter on new A5 pages that a TextFrame flows onto, with
 * the words in italics and a number on every page. The time it prints is the
 * time PDFjet takes to set the 241,527 words of data/the-idiot.txt.
 */
public class Example_52 {
    public Example_52() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_52.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("The Idiot");
        pdf.SetAuthor("Fyodor Dostoyevsky");

        Font regular = new Font(pdf, SourceSerif4.Regular);
        Font italic = new Font(pdf, SourceSerif4.Italic);
        Font semiBold = new Font(pdf, SourceSerif4.SemiBold);
        regular.SetSize(10f);
        italic.SetSize(10f);

        List<Page> pages = new List<Page>();

        // The title page.
        List<Paragraph> title = new List<Paragraph>();
        title.Add(Centered(new TextLine(semiBold, "The Idiot").SetFontSize(28f), StructElem.H1));
        title.Add(Centered(new TextLine(regular, "Fyodor Dostoyevsky").SetFontSize(14f), StructElem.P));
        title.Add(Centered(new TextLine(italic, "Translated by Eva Martin"), StructElem.P));
        new TextFrame(title).SetLocation(54f, 200f).SetWidth(312f).SetParagraphGap(16f)
                .DrawOn(pdf, pages, A5.PORTRAIT);
        int titlePages = pages.Count;

        // The novel: parts begin with "PART I" and chapters with their number,
        // "I.", and the paragraphs are separated by an empty line.
        String[] blocks = Regex.Split(Content.OfTextFile("data/the-idiot.txt"), "\\n\\s*\\n");
        List<Paragraph> chapter = null;
        foreach (String block in blocks) {
            String text = Regex.Replace(block.Trim(), "\\s+", " ");
            if (Regex.IsMatch(text, "^PART [IVX]+$")) {
                DrawChapter(pdf, pages, chapter);
                chapter = new List<Paragraph>();
                chapter.Add(Centered(new TextLine(semiBold, text).SetFontSize(16f), StructElem.H1));
            } else if (Regex.IsMatch(text, "^[IVXL]+\\.$")) {
                if (chapter != null && chapter.Count > 1) {
                    DrawChapter(pdf, pages, chapter);
                    chapter = new List<Paragraph>();
                }
                chapter.Add(Centered(new TextLine(semiBold, text).SetFontSize(14f), StructElem.H2));
            } else if (text.Length > 0) {
                chapter.Add(NewParagraph(text, regular, italic));
            }
        }
        DrawChapter(pdf, pages, chapter);

        // A number at the foot of every page but the title page.
        for (int i = titlePages; i < pages.Count; i++) {
            TextLine number = new TextLine(regular, (i - titlePages + 1).ToString());
            number.SetFontSize(9f);
            pages[i].AddFooter(number, 30f);
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    // Draws a chapter on new pages, which it starts at the top of.
    private static void DrawChapter(PDF pdf, List<Page> pages, List<Paragraph> chapter) {
        if (chapter != null) {
            new TextFrame(chapter).SetLocation(54f, 54f).SetWidth(312f).SetParagraphGap(4f)
                    .DrawOn(pdf, pages, A5.PORTRAIT);
        }
    }

    private static Paragraph Centered(TextLine textLine, StructElem structureType) {
        return new Paragraph(textLine).SetTextAlignment(Alignment.CENTER)
                .SetStructureType(structureType);
    }

    // A justified paragraph of the text, in which _underscores_ mark the
    // words in italics. A word in italics is set in italics whole, with the
    // punctuation around it, since a paragraph puts a space between its text
    // lines.
    private static Paragraph NewParagraph(String text, Font regular, Font italic) {
        Paragraph paragraph = new Paragraph().SetTextAlignment(Alignment.JUSTIFY);
        StringBuilder run = new StringBuilder();
        bool runItalic = false;
        bool inItalic = false;
        foreach (String word in text.Split(' ')) {
            bool wordItalic = inItalic || word.IndexOf('_') != -1;
            foreach (char ch in word) {
                if (ch == '_') {
                    inItalic = !inItalic;
                }
            }
            if (run.Length > 0 && wordItalic != runItalic) {
                paragraph.Add(new TextLine(runItalic ? italic : regular, run.ToString()));
                run.Clear();
            }
            if (run.Length > 0) {
                run.Append(' ');
            }
            run.Append(word.Replace("_", ""));
            runItalic = wordItalic;
        }
        if (run.Length > 0) {
            paragraph.Add(new TextLine(runItalic ? italic : regular, run.ToString()));
        }
        return paragraph;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_52();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_52 => {time1 - time0,4} ms");
    }
}   // End of Example_52.cs
