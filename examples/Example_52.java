/*
 * Example_52.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_52.java
 * A whole novel, "The Idiot" by Fyodor Dostoyevsky, set as a book: a title
 * page, and each chapter on new A5 pages that a TextFrame flows onto, with
 * the words in italics and a number on every page. The time it prints is the
 * time PDFjet takes to set the 241,527 words of data/the-idiot.txt.
 */
public class Example_52 {
    public Example_52() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_52.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("The Idiot");
        pdf.setAuthor("Fyodor Dostoyevsky");

        Font regular = new Font(pdf, SourceSerif4.Regular);
        Font italic = new Font(pdf, SourceSerif4.Italic);
        Font semiBold = new Font(pdf, SourceSerif4.SemiBold);
        regular.setSize(10f);
        italic.setSize(10f);

        List<Page> pages = new ArrayList<Page>();

        // The title page.
        List<Paragraph> title = new ArrayList<Paragraph>();
        title.add(centered(new TextLine(semiBold, "The Idiot").setFontSize(28f), StructElem.H1));
        title.add(centered(new TextLine(regular, "Fyodor Dostoyevsky").setFontSize(14f), StructElem.P));
        title.add(centered(new TextLine(italic, "Translated by Eva Martin"), StructElem.P));
        new TextFrame(title).setLocation(54f, 200f).setWidth(312f).setParagraphGap(16f)
                .drawOn(pdf, pages, A5.PORTRAIT);
        int titlePages = pages.size();

        // The novel: parts begin with "PART I" and chapters with their number,
        // "I.", and the paragraphs are separated by an empty line.
        String[] blocks = Content.ofTextFile("data/the-idiot.txt").split("\n\\s*\n");
        List<Paragraph> chapter = null;
        for (String block : blocks) {
            String text = block.trim().replaceAll("\\s+", " ");
            if (text.matches("PART [IVX]+")) {
                drawChapter(pdf, pages, chapter);
                chapter = new ArrayList<Paragraph>();
                chapter.add(centered(new TextLine(semiBold, text).setFontSize(16f), StructElem.H1));
            } else if (text.matches("[IVXL]+\\.")) {
                if (chapter != null && chapter.size() > 1) {
                    drawChapter(pdf, pages, chapter);
                    chapter = new ArrayList<Paragraph>();
                }
                chapter.add(centered(new TextLine(semiBold, text).setFontSize(14f), StructElem.H2));
            } else if (!text.isEmpty()) {
                chapter.add(paragraph(text, regular, italic));
            }
        }
        drawChapter(pdf, pages, chapter);

        // A number at the foot of every page but the title page.
        for (int i = titlePages; i < pages.size(); i++) {
            TextLine number = new TextLine(regular, String.valueOf(i - titlePages + 1));
            number.setFontSize(9f);
            pages.get(i).addFooter(number, 30f);
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // Draws a chapter on new pages, which it starts at the top of.
    private static void drawChapter(PDF pdf, List<Page> pages, List<Paragraph> chapter)
            throws Exception {
        if (chapter != null) {
            new TextFrame(chapter).setLocation(54f, 54f).setWidth(312f).setParagraphGap(4f)
                    .drawOn(pdf, pages, A5.PORTRAIT);
        }
    }

    private static Paragraph centered(TextLine textLine, StructElem structureType) {
        return new Paragraph(textLine).setTextAlignment(Alignment.CENTER)
                .setStructureType(structureType);
    }

    // A justified paragraph of the text, in which _underscores_ mark the
    // words in italics. A word in italics is set in italics whole, with the
    // punctuation around it, since a paragraph puts a space between its text
    // lines.
    private static Paragraph paragraph(String text, Font regular, Font italic) {
        Paragraph paragraph = new Paragraph().setTextAlignment(Alignment.JUSTIFY);
        StringBuilder run = new StringBuilder();
        boolean runItalic = false;
        boolean inItalic = false;
        for (String word : text.split(" ")) {
            boolean wordItalic = inItalic || word.indexOf('_') != -1;
            for (int i = 0; i < word.length(); i++) {
                if (word.charAt(i) == '_') {
                    inItalic = !inItalic;
                }
            }
            if (run.length() > 0 && wordItalic != runItalic) {
                paragraph.add(new TextLine(runItalic ? italic : regular, run.toString()));
                run.setLength(0);
            }
            if (run.length() > 0) {
                run.append(' ');
            }
            run.append(word.replace("_", ""));
            runItalic = wordItalic;
        }
        if (run.length() > 0) {
            paragraph.add(new TextLine(runItalic ? italic : regular, run.toString()));
        }
        return paragraph;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_52();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_52 => %4d ms%n", time1 - time0);
    }
}   // End of Example_52.java
