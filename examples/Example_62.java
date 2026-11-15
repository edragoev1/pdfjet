/*
 * Example_62.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * The price list of a small design studio, on Letter paper: a centered
 * title with the date the prices start, then a section for each kind of
 * work, a heading and a table of the services and their prices, and a note
 * on taxes and quotes at the end.
 * <p>
 * A price list is a finished document that customers receive, not a form.
 * It is several tables, one under the other: each is drawn with
 * drawOn(pdf, page, pages, ...) starting where the one before it ended, and
 * goes on to the next page when it needs to, with its header row repeated.
 * A heading is not left alone at the foot of a page: when there is no room
 * for it and two rows under it, the section starts on a new page.
 * </p>
 * <p>
 * The document is PDF/UA-1: the title is the H1 and the sections H2, so a
 * screen reader can jump from one section to the next, and the rule and the
 * page numbers are artifacts. The layout is that of the price list template
 * of PDFjet Forms. The company and the prices are made up.
 * </p>
 *
 * @see Table
 */
public class Example_62 {
    // A kind of work: its heading, and its services with their prices, in
    // dollars before tax.
    static final class Section {
        final String title;
        final String[][] services;

        Section(String title, String[]... services) {
            this.title = title;
            this.services = services;
        }
    }

    static final String SELLER = "Northwind Studio";
    static final LocalDate PRICES_FROM = LocalDate.of(2026, 11, 1);

    static final Section[] SECTIONS = {
        new Section("Design",
                new String[] {"Logo and business card", "450.00"},
                new String[] {"Brochure, up to 8 pages", "900.00"},
                new String[] {"Poster or flyer", "250.00"},
                new String[] {"Design and layout of larger work, per day", "800.00"}),
        new Section("Print",
                new String[] {"Business cards, 250", "60.00"},
                new String[] {"Flyers, 500, color, both sides", "180.00"},
                new String[] {"Posters, 11 x 17 in, 25", "95.00"},
                new String[] {"Booklets up to 48 pages, saddle stitched, per copy", "6.00"}),
        new Section("Accessible PDFs",
                new String[] {"PDF/UA edition of a printed piece, per page", "25.00"},
                new String[] {"Fillable PDF form, per page", "60.00"}),
    };

    static final int BLUE = 0x1f58b0;
    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;
    static final int BORDER = 0x4d4d4d;

    static final float MARGIN = 48f;
    static final float TOP = 64f;           // where the next pages start
    static final float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_62() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_62.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Price list, " + SELLER);
        pdf.setAuthor(SELLER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        List<Page> pages = new ArrayList<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.add(page);
        float left = MARGIN;
        float right = page.getWidth() - MARGIN;
        float bottom = page.getHeight() - FOOTER_ROOM;

        // The title and the date the prices start, centered, and a rule
        TextLine title = new TextLine(semiBold, "Price list");
        title.setStructureType(StructElem.H1);
        title.setFontSize(26f);
        title.setTextColor(BLUE);
        title.setLocation((page.getWidth() - title.getWidth()) / 2f, 72f);
        title.drawOn(page);

        String from = PRICES_FROM.format(DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US));
        TextLine subtitle = new TextLine(regular, SELLER + " · Prices from " + from);
        subtitle.setFontSize(12f);
        subtitle.setLocation((page.getWidth() - subtitle.getWidth()) / 2f, 96f);
        subtitle.drawOn(page);

        Line rule = new Line(left, 116f, right, 116f);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The sections, each under the one before it
        float y = 116f;
        for (Section section : SECTIONS) {
            // The heading and the header row and two rows of the table, about
            // 90 points, or the section starts on a new page
            if (y + 90f > bottom) {
                page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
                pages.add(page);
                y = TOP - 36f;
            }
            TextLine heading = new TextLine(semiBold, section.title);
            heading.setStructureType(StructElem.H2);
            heading.setFontSize(14f);
            heading.setTextColor(BLUE);
            heading.setLocation(left, y + 36f);
            heading.drawOn(page);

            List<List<Cell>> rows = new ArrayList<List<Cell>>();
            rows.add(row(semiBold, "Service", "Price"));
            for (String[] service : section.services) {
                rows.add(row(regular, service[0], money(new BigDecimal(service[1]))));
            }
            Table table = new Table();
            table.setTableData(rows, 1);
            table.setHeaderRowStyle(semiBold, Color.black, HEADER);
            table.setCellPadding(4f, 3f);
            table.setCellBorderWidth(0.5f);
            table.setCellBorderColor(BORDER);
            table.setWidth(right - left);
            table.setColumnWidthsInPercent(70f, 30f);
            table.setTextAlignmentInColumn(1, Alignment.RIGHT);
            table.setLocation(left, TOP - 16f);
            table.setFirstPageTopMargin(y + 46f);
            table.setBottomMargin(FOOTER_ROOM);
            y = table.drawOn(pdf, page, pages, Letter.PORTRAIT)[1];
            page = pages.get(pages.size() - 1);
        }

        // The note on taxes and quotes, under the last table
        if (y + 40f > bottom) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.add(page);
            y = TOP - 20f;
        }
        new TextBlock(regular, "Prices in Canadian dollars, before tax. Ask us for a quote "
                + "for larger work.")
                .setLocation(left, y + 20f)
                .setWidth(right - left)
                .drawOn(page);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.size(); i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.size());
            footer.setFontSize(10f);
            footer.setLocation(right - footer.getWidth(), page.getHeight() - 52f);
            pages.get(i).addArtifactBMC();
            footer.drawOn(pages.get(i));
            pages.get(i).addEMC();
        }
        pdf.addPages(pages);
        pdf.complete();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> row(Font font, String... texts) {
        List<Cell> row = new ArrayList<Cell>();
        for (String text : texts) {
            row.add(new Cell(font, text));
        }
        return row;
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00, whatever the locale of the computer.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_62();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_62 => %4d ms%n", time1 - time0);
    }
}   // End of Example_62.java
