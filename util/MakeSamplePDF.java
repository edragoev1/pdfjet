/*
 * MakeSamplePDF.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Writes resources/data/testPDFs/sample-document.pdf, the quarterly report of
 * a made-up company on two Letter pages, which Example_49 merges and
 * Example_50 stamps. The company, its project and its numbers are invented,
 * and the report names no one. The top right corner and the foot of each page
 * are left empty, for the stamp and the page numbers of Example_50.
 *
 * A PDF holds the moment it was made and a random document ID. Both are
 * replaced here, with values of the same length, so the file is the same on
 * every run.
 */
public class MakeSamplePDF {
    private static final String OUTPUT = "resources/data/testPDFs/sample-document.pdf";
    private static final float LEFT = 72f;
    private static final float WIDTH = 468f;
    private static final float[] COLUMNS = {LEFT, 270f, 400f};

    private final Font bold;
    private final Font body;

    private MakeSamplePDF(PDF pdf) throws Exception {
        bold = new Font(pdf, IBMPlexSans.Bold).setSize(10f);
        body = new Font(pdf, IBMPlexSerif.Regular).setSize(11f);
    }

    private float heading(Page page, String text, float y) throws Exception {
        new TextLine(bold, text).setFontSize(14f).setLocation(LEFT, y).drawOn(page);
        return y + 8f;
    }

    private float paragraph(Page page, String text, float y) throws Exception {
        TextBlock block = new TextBlock(body, text);
        block.setLocation(LEFT, y);
        block.setWidth(WIDTH);
        return block.drawOn(page)[1] + 12f;
    }

    private float row(Page page, Font font, float y, String... cells) throws Exception {
        for (int i = 0; i < cells.length; i++) {
            new TextLine(font, cells[i]).setLocation(COLUMNS[i], y).drawOn(page);
        }
        return y + 17f;
    }

    private void write(PDF pdf) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(bold, "Quarterly Project Report").setFontSize(24f).setLocation(LEFT, 90f).drawOn(page);
        new TextLine(body, "Quillhaven Instruments, third quarter of 2026").setLocation(LEFT, 112f).drawOn(page);

        float y = heading(page, "Summary", 160f);
        y = paragraph(page, "The Larkmoor tide gauge project closed the quarter on schedule and "
                + "slightly under budget. The second field unit has reported every six minutes "
                + "since August, and its readings agree with the reference staff to within two "
                + "centimeters. The team now turns to the winter trial, the last step before the "
                + "first production run.", y);

        y = heading(page, "Milestones", y + 16f);
        y = row(page, bold, y + 12f, "Milestone", "Due", "Status");
        y = row(page, body, y, "Sensor housing, second revision", "July 17", "Done");
        y = row(page, body, y, "Field unit installed at the pier", "August 7", "Done");
        y = row(page, body, y, "Telemetry link, thirty day soak", "September 25", "Done");
        y = row(page, body, y, "Calibration report", "October 16", "In progress");
        y = row(page, body, y, "Winter trial begins", "December 4", "Planned");

        y = heading(page, "Field Results", y + 22f);
        y = paragraph(page, "The unit logged 12,960 readings by the end of September, and lost "
                + "eleven of them to a single power fault during a storm. The fault was traced to "
                + "a loose connector, which the third revision of the housing replaces with a "
                + "sealed one. Battery use came in below the design figure, so the planned solar "
                + "panel can be one size smaller.", y);
        paragraph(page, "Readings from the pier were compared each week with the staff gauge "
                + "beside it. The difference stayed within the two centimeters the design calls "
                + "for, on calm days and in the two gales of the quarter alike.", y);

        page = new Page(pdf, Letter.PORTRAIT);
        y = heading(page, "Budget", 110f);
        y = row(page, bold, y + 12f, "Item", "Planned", "Spent");
        y = row(page, body, y, "Materials", "$84,000", "$81,250");
        y = row(page, body, y, "Field work", "$46,500", "$47,900");
        y = row(page, body, y, "Test equipment", "$22,000", "$19,400");
        y = row(page, body, y, "Total", "$152,500", "$148,550");

        y = heading(page, "Risks", y + 22f);
        y = paragraph(page, "Delivery of the sealed connectors is the main risk: the supplier "
                + "quotes ten weeks, which leaves two weeks of margin before the winter trial. A "
                + "second supplier has been asked for samples. Ice at the pier is the other risk; "
                + "the housing is rated for it, but it has not been tested in the field yet.", y);

        y = heading(page, "Next Quarter", y + 16f);
        y = paragraph(page, "The team will finish the calibration report, build three units with "
                + "the third revision of the housing, and start the winter trial on December 4. "
                + "The next report will give the first results of that trial.", y);

        y = heading(page, "About This Document", y + 16f);
        paragraph(page, "Quillhaven Instruments and its Larkmoor project are made up, and so is "
                + "every number here. This document was made with PDFjet for its tests and "
                + "examples, by util/MakeSamplePDF.java.", y);
    }

    /**
     * Writes the file.
     *
     * @param args not used.
     * @throws Exception if the PDF cannot be written.
     */
    public static void main(String[] args) throws Exception {
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        PDF pdf = new PDF(buf);
        pdf.setTitle("Quarterly Project Report");
        pdf.setSubject("A made-up report, for the tests and examples of PDFjet");
        new MakeSamplePDF(pdf).write(pdf);
        pdf.complete();

        // The bytes as Latin-1 characters, one for each, so the offsets of the
        // cross-reference table stay right.
        String text = new String(buf.toByteArray(), StandardCharsets.ISO_8859_1);
        StringBuilder date = new StringBuilder();
        for (byte b : "D:20261030120000Z".getBytes(StandardCharsets.US_ASCII)) {
            date.append(String.format("%02X", b));
        }
        String id = "0123456789abcdef0123456789abcdef";
        text = text.replaceFirst("/CreationDate <[0-9A-Fa-f]+>", "/CreationDate <" + date + ">");
        text = text.replaceFirst("/ID\\[<[0-9a-f]+><[0-9a-f]+>\\]", "/ID[<" + id + "><" + id + ">]");
        Files.write(Paths.get(OUTPUT), text.getBytes(StandardCharsets.ISO_8859_1));
        System.out.printf("%s => %d bytes%n", OUTPUT, text.length());
    }
}   // End of MakeSamplePDF.java
