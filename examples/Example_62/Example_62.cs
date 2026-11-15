/*
 * Example_62.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_62.cs
 * The price list of a small design studio, on Letter paper: a centered
 * title with the date the prices start, then a section for each kind of
 * work, a heading and a table of the services and their prices, and a note
 * on taxes and quotes at the end.
 *
 * A price list is a finished document that customers receive, not a form.
 * It is several tables, one under the other: each is drawn with
 * DrawOn(pdf, page, pages, ...) starting where the one before it ended, and
 * goes on to the next page when it needs to, with its header row repeated.
 * A heading is not left alone at the foot of a page: when there is no room
 * for it and two rows under it, the section starts on a new page.
 *
 * The document is PDF/UA-1: the title is the H1 and the sections H2, so a
 * screen reader can jump from one section to the next, and the rule and the
 * page numbers are artifacts. The layout is that of the price list template
 * of PDFjet Forms. The company and the prices are made up.
 */
public class Example_62 {
    // A kind of work: its heading, and its services with their prices, in
    // dollars before tax.
    private sealed class Section {
        internal readonly String title;
        internal readonly String[][] services;

        internal Section(String title, params String[][] services) {
            this.title = title;
            this.services = services;
        }
    }

    private const String SELLER = "Northwind Studio";
    private static readonly DateTime PRICES_FROM = new DateTime(2026, 11, 1);

    private static readonly Section[] SECTIONS = {
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

    private const int BLUE = 0x1f58b0;
    private const int RULE = 0xdde1e8;
    private const int HEADER = 0xe3edf9;
    private const int BORDER = 0x4d4d4d;

    private const float MARGIN = 48f;
    private const float TOP = 64f;           // where the next pages start
    private const float FOOTER_ROOM = 80f;   // the bottom margin, clear of "Page i of N"

    public Example_62() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_62.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Price list, " + SELLER);
        pdf.SetAuthor(SELLER);
        pdf.SetLanguage("en-CA");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        regular.SetSize(11f);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        semiBold.SetSize(11f);

        List<Page> pages = new List<Page>();
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);
        float left = MARGIN;
        float right = page.GetWidth() - MARGIN;
        float bottom = page.GetHeight() - FOOTER_ROOM;

        // The title and the date the prices start, centered, and a rule
        TextLine title = new TextLine(semiBold, "Price list");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(26f);
        title.SetTextColor(BLUE);
        title.SetLocation((page.GetWidth() - title.GetWidth()) / 2f, 72f);
        title.DrawOn(page);

        String from = PRICES_FROM.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        TextLine subtitle = new TextLine(regular, SELLER + " · Prices from " + from);
        subtitle.SetFontSize(12f);
        subtitle.SetLocation((page.GetWidth() - subtitle.GetWidth()) / 2f, 96f);
        subtitle.DrawOn(page);

        Line rule = new Line(left, 116f, right, 116f);
        rule.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        rule.DrawOn(page);
        page.AddEMC();

        // The sections, each under the one before it
        float y = 116f;
        foreach (Section section in SECTIONS) {
            // The heading and the header row and two rows of the table, about
            // 90 points, or the section starts on a new page
            if (y + 90f > bottom) {
                page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
                pages.Add(page);
                y = TOP - 36f;
            }
            TextLine heading = new TextLine(semiBold, section.title);
            heading.SetStructureType(StructElem.H2);
            heading.SetFontSize(14f);
            heading.SetTextColor(BLUE);
            heading.SetLocation(left, y + 36f);
            heading.DrawOn(page);

            List<List<Cell>> rows = new List<List<Cell>>();
            rows.Add(Row(semiBold, "Service", "Price"));
            foreach (String[] service in section.services) {
                rows.Add(Row(regular, service[0], Money(decimal.Parse(service[1], CultureInfo.InvariantCulture))));
            }
            Table table = new Table();
            table.SetTableData(rows, 1);
            table.SetHeaderRowStyle(semiBold, Color.black, HEADER);
            table.SetCellPadding(4f, 3f);
            table.SetCellBorderWidth(0.5f);
            table.SetCellBorderColor(BORDER);
            table.SetWidth(right - left);
            table.SetColumnWidthsInPercent(70f, 30f);
            table.SetTextAlignmentInColumn(1, Alignment.RIGHT);
            table.SetLocation(left, TOP - 16f);
            table.SetFirstPageTopMargin(y + 46f);
            table.SetBottomMargin(FOOTER_ROOM);
            y = table.DrawOn(pdf, page, pages, Letter.PORTRAIT)[1];
            page = pages[pages.Count - 1];
        }

        // The note on taxes and quotes, under the last table
        if (y + 40f > bottom) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = TOP - 20f;
        }
        new TextBlock(regular, "Prices in Canadian dollars, before tax. Ask us for a quote "
                + "for larger work.")
                .SetLocation(left, y + 20f)
                .SetWidth(right - left)
                .DrawOn(page);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            TextLine footer = new TextLine(regular, "Page " + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(10f);
            footer.SetLocation(right - footer.GetWidth(), page.GetHeight() - 52f);
            pages[i].AddArtifactBMC();
            footer.DrawOn(pages[i]);
            pages[i].AddEMC();
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    // A row of the table, a cell in the font for each text.
    private static List<Cell> Row(Font font, params String[] texts) {
        List<Cell> row = new List<Cell>();
        foreach (String text in texts) {
            row.Add(new Cell(font, text));
        }
        return row;
    }

    // The amount with two decimals and commas between the thousands, as
    // 1,200.00, whatever the culture of the computer.
    private static String Money(decimal amount) {
        return amount.ToString("N2", CultureInfo.InvariantCulture);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_62();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_62 => {time1 - time0,4} ms");
    }
}   // End of Example_62.cs
