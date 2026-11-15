/*
 * Example_77.java
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
 * A payment reminder, a letter to a customer whose invoice is past due: the
 * letterhead, the date, the address, the invoice as the subject, a friendly
 * paragraph, a small table of what was invoiced, credited and paid, the
 * amount still open, how to pay it, and the signature.
 * <p>
 * The letter is placed as the business letter of Example_59 is, each part
 * under the one before it, from where drawOn says the last one ended; the
 * table of the account is one more part, and the paragraphs after it move
 * down when it has more lines. The amount still open is computed from the
 * lines of the account, in BigDecimal, and is the same figure in the table
 * and in the text, as both come from one variable.
 * </p>
 * <p>
 * The document is PDF/UA-1: the subject is the heading of the letter, the
 * account a table that a screen reader reads by its column headers, and the
 * rule under the letterhead is an artifact. The layout is that of the
 * payment reminder template of PDFjet Forms. The people and the companies
 * are made up.
 * </p>
 *
 * @see Table
 * @see TextBlock
 */
public class Example_77 {
    // A line of the customer's account: an invoice is positive, a credit
    // note and a payment negative.
    static final class Entry {
        final LocalDate date;
        final String description;
        final BigDecimal amount;

        Entry(String date, String description, String amount) {
            this.date = LocalDate.parse(date);
            this.description = description;
            this.amount = new BigDecimal(amount);
        }
    }

    static final String SENDER = "Northwind Studio";
    static final String SENDER_ADDRESS = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com";

    static final LocalDate DATE = LocalDate.of(2026, 11, 3);
    static final String RECIPIENT = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4";
    static final String INVOICE = "2026-042";
    static final LocalDate DUE = LocalDate.of(2026, 10, 25);
    static final String CURRENCY = "CAD";
    static final String ACCOUNT = "01234-567-8901234";

    static final Entry[] ENTRIES = {
        new Entry("2026-09-25", "Invoice 2026-042, the annual report", "2495.61"),
        new Entry("2026-10-05", "Credit note CN-2026-007, the accessible edition", "-452.00"),
        new Entry("2026-10-20", "Your payment, thank you", "-964.41"),
    };

    static final String SIGNER = "Dana Whitfield\nNorthwind Studio";

    static final int RULE = 0xdde1e8;
    static final int HEADER = 0xe3edf9;

    static final float LEFT = 48f;
    static final float WIDTH = 516f;            // Letter, 612 wide, less the margins
    static final float GAP = 14f;               // Between the parts of the letter

    public Example_77() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_77.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Payment reminder, invoice " + INVOICE);
        pdf.setAuthor(SENDER);
        pdf.setLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.setSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.setSize(11f);

        // What is still open, from the lines of the account
        BigDecimal open = BigDecimal.ZERO;
        for (Entry entry : ENTRIES) {
            open = open.add(entry.amount);
        }

        Page page = new Page(pdf, Letter.PORTRAIT);
        DateTimeFormatter longDate = DateTimeFormatter.ofPattern("MMMM d, yyyy", Locale.US);
        DateTimeFormatter monthDay = DateTimeFormatter.ofPattern("MMMM d", Locale.US);

        // The letterhead, and a thin rule under it, decoration only
        new TextLine(semiBold, SENDER).setFontSize(16f).setLocation(LEFT, 64f).drawOn(page);
        float y = paragraph(page, regular, SENDER_ADDRESS, 80f) + GAP;
        Line rule = new Line(LEFT, y, LEFT + WIDTH, y);
        rule.setStrokeColor(RULE);
        page.addArtifactBMC();
        rule.drawOn(page);
        page.addEMC();

        // The date, the recipient, and the subject, the heading of the letter
        y = paragraph(page, regular, DATE.format(longDate), y + 2 * GAP);
        y = paragraph(page, regular, RECIPIENT, y + GAP);
        TextBlock subject = new TextBlock(semiBold, "Invoice " + INVOICE + ", due " + DUE.format(longDate));
        subject.setFontSize(12f);
        subject.setStructureType(StructElem.H1);
        subject.setLocation(LEFT, y + GAP);
        subject.setWidth(WIDTH);
        y = subject.drawOn(page)[1];

        y = paragraph(page, regular, "Dear Ms. Raman,", y + GAP);
        y = paragraph(page, regular, "This is a friendly reminder that invoice " + INVOICE + " of "
                + ENTRIES[0].date.format(monthDay) + ", for the layout of your annual report, was due on "
                + DUE.format(monthDay) + ". The amount still open is " + money(open) + " " + CURRENCY
                + ", after the credit note and the payment below.", y + GAP);

        // The account: what was invoiced, credited and paid, and what is open
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(row(semiBold, "Date", "Description", "Amount"));
        for (Entry entry : ENTRIES) {
            rows.add(row(regular, entry.date.toString(), entry.description, money(entry.amount)));
        }
        rows.add(row(semiBold, "", "Amount open, " + CURRENCY, money(open)));

        Table table = new Table();
        table.setTableData(rows, 1);
        table.setNumberOfFooterRows(1);
        table.setHeaderRowStyle(semiBold, Color.black, HEADER);
        table.setFooterRowStyle(semiBold, Color.black, HEADER);
        table.setCellPadding(4f, 3f);
        table.setCellBorderWidth(0.5f);
        table.setCellBorderColor(0x4d4d4d);
        table.setWidth(WIDTH);
        table.setColumnWidthsInPercent(18f, 62f, 20f);
        table.setTextAlignmentInColumn(2, Alignment.RIGHT);
        table.setLocation(LEFT, y + GAP);
        y = table.drawOn(page)[1];

        y = paragraph(page, regular, "If you have paid it in the last few days, thank you, and please "
                + "disregard this letter. If anything in the invoice is unclear, just reply to this "
                + "letter and I will be glad to help.", y + GAP);
        y = paragraph(page, regular, "You can pay by bank transfer to " + SENDER + ", account "
                + ACCOUNT + ", with the invoice number in the message.", y + GAP);

        // The closing, room to sign by hand, and the name under it
        y = paragraph(page, regular, "Kind regards,", y + GAP);
        paragraph(page, regular, SIGNER, y + 3 * GAP);

        pdf.complete();
    }

    // Draws the text wrapped to the width of the page, its top at y, and
    // returns the y where it ended, for the next part of the letter.
    private static float paragraph(Page page, Font font, String text, float y) throws Exception {
        TextBlock block = new TextBlock(font, text);
        block.setLineSpacing(1.25f);
        block.setLocation(LEFT, y);
        block.setWidth(WIDTH);
        return block.drawOn(page)[1];
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
    // 1,079.20, and a minus sign before a credit. The locale is given, or a
    // computer set to French writes 1 079,20.
    private static String money(BigDecimal amount) {
        return String.format(Locale.US, "%,.2f", amount);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_77();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_77 => %4d ms%n", time1 - time0);
    }
}   // End of Example_77.java
