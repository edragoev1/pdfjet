/*
 * Example_60.java
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
 * A certificate of completion of a workshop, on Letter paper turned on its
 * side: a band of color at the top and the bottom, the studio that gives it,
 * the title, the name of the participant on a line, what was completed, and
 * the lines of the instructor's signature and of the date.
 * <p>
 * It is a document people receive, finished: everything on it is centered,
 * so each line is measured and placed half its width left of the middle of
 * the page, and the paragraph of what was completed is a TextBlock centered
 * in a column narrower than the page. The serif faces of IBM Plex give it
 * the look of a printed certificate; the name is the largest text after the
 * title, as it is what the reader looks for.
 * </p>
 * <p>
 * The certificate is drawn by a method that takes the name, so that a
 * certificate for each person of a class is a loop over the list of names, a
 * page each in one document: the fonts are embedded once, however many
 * pages there are, and the file is printed in one go.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is a heading, the bands and the lines
 * are artifacts, and the language is set. The layout is that of the
 * certificate template of PDFjet Forms. The people and the studio are made up.
 * </p>
 *
 * @see TextBlock
 * @see TextLine
 */
public class Example_60 {
    static final String ISSUER = "Northwind Studio";
    static final String TITLE = "Certificate of completion";
    static final String NAME = "Robin Brook";
    static final String WORKSHOP = "Design with PDF";
    static final LocalDate DATE = LocalDate.of(2026, 11, 14);
    static final String INSTRUCTOR = "Alex Morgan, instructor";

    static final int NAVY = 0x1d3557;
    static final int INK = 0x333333;
    static final int GRAY = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float BAND = 16f;              // The height of the bands

    private Font sans;
    private Font serif;
    private Font serifBold;
    private Font serifItalic;

    public Example_60() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_60.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle(TITLE);
        pdf.setAuthor(ISSUER);
        pdf.setLanguage("en-CA");

        sans = new Font(pdf, IBMPlexSans.SemiBold);
        serif = new Font(pdf, IBMPlexSerif.Regular);
        serifBold = new Font(pdf, IBMPlexSerif.SemiBold);
        serifItalic = new Font(pdf, IBMPlexSerif.Italic);

        // One certificate. For a class, a page each from the list of names:
        // for (String name : names) {
        //     drawCertificate(new Page(pdf, Letter.LANDSCAPE), name);
        // }
        drawCertificate(new Page(pdf, Letter.LANDSCAPE), NAME);

        pdf.complete();
    }

    // Draws the certificate of the person on the page.
    private void drawCertificate(Page page, String name) throws Exception {
        float width = page.getWidth();
        float inner = width - 2 * MARGIN;       // The width between the margins
        float middle = width / 2f;

        // The bands at the top and the bottom. A Rect is decoration, an
        // artifact, on its own.
        new Rect(MARGIN, MARGIN, inner, BAND).setFillColor(NAVY).drawOn(page);
        new Rect(MARGIN, page.getHeight() - MARGIN - BAND, inner, BAND)
                .setFillColor(NAVY).drawOn(page);

        // Who gives it, and the title, the heading of the page
        center(page, sans, 13f, ISSUER, GRAY, 112f).drawOn(page);
        TextLine title = center(page, serifBold, 38f, TITLE, NAVY, 170f);
        title.setStructureType(StructElem.H1);
        title.drawOn(page);

        // The name, on a line as wide as three fifths of the page
        center(page, serifItalic, 15f, "This certifies that", INK, 214f).drawOn(page);
        center(page, serifBold, 30f, name, INK, 264f).drawOn(page);
        float lineWidth = 0.6f * inner;
        rule(page, middle - lineWidth / 2f, middle + lineWidth / 2f, 286f);

        // What was completed, in a column of seven tenths of the page, each
        // line centered in it
        String date = DATE.format(DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US));
        float columnWidth = 0.7f * inner;
        TextBlock what = new TextBlock(serif, "has completed " + WORKSHOP + ", a hands-on workshop "
                + "of one day, on " + date + ", in Port Elgin, Ontario: layouts that print the "
                + "same everywhere, fillable forms, and accessible PDF.");
        what.setFontSize(14f);
        what.setTextColor(INK);
        what.setLineSpacing(1.15f);
        what.setTextAlignment(Alignment.CENTER);
        what.setLocation(middle - columnWidth / 2f, 306f);
        what.setWidth(columnWidth);
        float y = what.drawOn(page)[1] + 64f;

        // The signature and the date, each on its line, a little over a third
        // of the page wide, in the left and the right half of the page
        float signWidth = 0.35f * inner;
        float[] starts = {MARGIN + 0.1f * inner, MARGIN + 0.55f * inner};
        String[] captions = {INSTRUCTOR, date};
        for (int i = 0; i < 2; i++) {
            float x = starts[i];
            rule(page, x, x + signWidth, y);
            TextLine caption = new TextLine(serif, captions[i]);
            caption.setFontSize(11f);
            caption.setTextColor(GRAY);
            caption.setLocation(x + (signWidth - caption.getWidth()) / 2f, y + 22f);
            caption.drawOn(page);
        }
    }

    // Returns a line of text centered on the page, its baseline at y, for the
    // caller to draw, after it has made it a heading where it is one.
    private static TextLine center(Page page, Font font, float size, String text, int color, float y)
            throws Exception {
        TextLine line = new TextLine(font, text);
        line.setFontSize(size);
        line.setTextColor(color);
        line.setLocation((page.getWidth() - line.getWidth()) / 2f, y);
        return line;
    }

    // A line to write or sign on, decoration only, so an artifact that a
    // screen reader skips.
    private static void rule(Page page, float x1, float x2, float y) throws Exception {
        Line line = new Line(x1, y, x2, y);
        line.setStrokeWidth(0.75f);
        line.setStrokeColor(INK);
        page.addArtifactBMC();
        line.drawOn(page);
        page.addEMC();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_60();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_60 => %4d ms%n", time1 - time0);
    }
}   // End of Example_60.java
