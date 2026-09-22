/*
 * Example_53.java
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
 * Example_53.java
 * Paragraphs written with the inline markup of Markdown: **bold**, *italic*,
 * `code` and [links](url), each drawn in its own font by Markup, with the
 * punctuation after a word in another style next to it. The paragraphs flow
 * in a text frame, and the list is a list of paragraphs with labels.
 */
public class Example_53 {
    public Example_53() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_53.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Inline markup");

        Font regular = new Font(pdf, IBMPlexSans.Regular).setSize(11f);
        Font bold = new Font(pdf, IBMPlexSans.Bold).setSize(11f);
        Font italic = new Font(pdf, IBMPlexSans.Italic).setSize(11f);
        Font boldItalic = new Font(pdf, IBMPlexSans.BoldItalic).setSize(11f);
        Font code = new Font(pdf, IBMPlexMono.Regular).setSize(10f);
        Font heading = new Font(pdf, IBMPlexSans.SemiBold);
        Markup markup = new Markup(regular, bold, italic, boldItalic, code);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        paragraphs.add(new Paragraph(new TextLine(heading, "Inline markup").setFontSize(22f))
                .setStructureType(StructElem.H1));
        paragraphs.addAll(markup.paragraphs(
                "**Markup** reads the inline markup of Markdown and draws each part in its own "
                + "font: **bold**, *italic*, ***bold italic***, `code` and "
                + "[links](https://pdfjet.com). A word keeps the punctuation after it, as in "
                + "*this*, and a mark with no match, such as the one in 2 * 3, is text.\n"
                + "\n"
                + "A backslash makes a mark text too: \\*not italic\\*. Code keeps its marks "
                + "as they are, as in `a*b*c`, and a link can have emphasis in it: "
                + "[the **PDFjet** repository](https://github.com/edragoev1/pdfjet)."));

        String[] items = {
            "`Markup.paragraph` makes one paragraph of a text.",
            "`Markup.paragraphs` makes one of each part between the empty lines.",
            "The paragraphs go in a **TextFrame** or a **TextColumn**, as any others do.",
        };
        for (int i = 0; i < items.length; i++) {
            paragraphs.add(markup.paragraph(items[i])
                    .setListLabel(new TextLine(regular, (i + 1) + "."), 16f));
        }

        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(70f, 70f);
        frame.setWidth(470f);
        frame.setParagraphGap(8f);
        List<Page> pages = new ArrayList<Page>();
        frame.drawOn(pdf, pages, Letter.PORTRAIT);
        pdf.addPages(pages);
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_53();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_53 => %4d ms%n", time1 - time0);
    }
}   // End of Example_53.java
