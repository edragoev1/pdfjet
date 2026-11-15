/*
 * Example_59.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * A business letter on Letter paper: the letterhead of the sender, the date,
 * the address of the recipient, the subject, the paragraphs of the letter,
 * the closing and the signature block, and a line for the enclosure.
 * <p>
 * Each part is placed under the one before it: drawOn returns where a
 * TextBlock ended, so a longer address or one more paragraph moves
 * everything below it down, and nothing is placed by hand twice. The
 * paragraphs are TextBlocks, which wrap the text to the width of the page,
 * with a blank line between them and no indent.
 * </p>
 * <p>
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * rule under the letterhead is an artifact, and the language is set, so that
 * a screen reader reads the letter as it is written. The layout is that of
 * the business letter template of PDFjet Forms. The people and the companies
 * are made up.
 * </p>
 *
 * @see TextBlock
 * @see TextLine
 */
public class Example_59 {
    static final String SENDER = "Northwind Studio";
    static final String SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";

    static final LocalDate DATE = LocalDate.of(2026, 10, 12);
    static final String RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4";
    static final String SUBJECT = "Your annual report, accessible edition";
    static final String SALUTATION = "Dear Ms. Raman,";

    static final String[] PARAGRAPHS = {
        "Thank you for choosing us for this year's annual report. The printed copies are on "
        + "their way, and the accessible PDF edition is on the USB drive enclosed.",

        "The PDF meets the PDF/UA standard, so screen readers read it in the right order, "
        + "with every picture described. If any of your readers has trouble with it, tell us "
        + "and we will fix it at no charge.",

        "Our invoice for the work is enclosed. We look forward to working with you again.",
    };

    static final String CLOSING = "Sincerely,";
    static final String SIGNER = "Dana Whitfield\nDesign Lead, Northwind Studio";
    static final String ENCLOSURE = "Enclosures: Invoice 2026-042, USB drive";

    static final int RULE = 0xdde1e8;

    // The space between the parts of the letter, and between its paragraphs
    static final float GAP = 14f;

    public Example_59() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_59.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle(SUBJECT);
        pdf.setAuthor(SENDER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);
        float left = 48f;
        float width = page.getWidth() - 2 * left;

        // The letterhead
        new TextLine(semiBold, SENDER).setFontSize(16f).setLocation(left, 64f).drawOn(page);
        float y = paragraph(page, regular, SENDER_ADDRESS, left, 80f, width);

        // A thin rule under the letterhead, decoration only
        y += GAP;
        Line rule = new Line(left, y, left + width, y);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The date, written out in words, so that 10/12 is not read as the
        // 10th of December
        String date = DATE.format(DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US));
        y = paragraph(page, regular, date, left, y + 2 * GAP, width);

        // The recipient, as on the envelope
        y = paragraph(page, regular, RECIPIENT, left, y + GAP, width);

        // The subject, the heading of the letter for a screen reader
        TextBlock subject = new TextBlock(semiBold, SUBJECT);
        subject.setStructureType(StructElem.H1);
        subject.setLocation(left, y + GAP);
        subject.setWidth(width);
        y = subject.drawOn(page)[1];

        // The body: the salutation and the paragraphs, a blank line between them
        y = paragraph(page, regular, SALUTATION, left, y + GAP, width);
        for (String text : PARAGRAPHS) {
            y = paragraph(page, regular, text, left, y + GAP, width);
        }

        // The closing, room to sign by hand, and the name under it
        y = paragraph(page, regular, CLOSING, left, y + GAP, width);
        y = paragraph(page, regular, SIGNER, left, y + 3 * GAP, width);

        paragraph(page, regular, ENCLOSURE, left, y + 2 * GAP, width);

        pdf.complete();
    }

    // Draws the text wrapped to the width, its top at y, and returns the y
    // where it ended, for the next part of the letter.
    private static float paragraph(Page page, Font font, String text, float x, float y, float width)
            throws Exception {
        TextBlock block = new TextBlock(font, text);
        block.setLineSpacing(1.25f);
        block.setLocation(x, y);
        block.setWidth(width);
        return block.drawOn(page)[1];
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_59();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_59 => %4d ms%n", time1 - time0);
    }
}   // End of Example_59.java
