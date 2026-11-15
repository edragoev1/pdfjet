/*
 * Example_71.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PDFjet.NET;

/**
 * Example_71.cs
 * A monthly safety inspection checklist to print and take on a walk through
 * a building: who inspects, when and where, then for each area a row for
 * each item to check, with a box to tick when it is in order, a box when it
 * needs fixing, and room for a note; under the list, the problems found, the
 * date of the next inspection, and the signature.
 *
 * This is a form, not a finished document: the inspector fills it in. The
 * fields above and below the list are drawn as in the job application of
 * Example_72, a label above each box. The list is a Table: the items come
 * from an array, each area is a row that spans the four columns, and each
 * box to tick is a square, a Rect placed in its cell with SetDrawable. The
 * table goes on to as many pages as it needs, with its header row on each,
 * so a building with sixty items prints on two pages with the same code;
 * the fields under it follow the last row, or go to a page of their own
 * when they no longer fit.
 *
 * The document is PDF/UA-1: the title is a heading, the list is a table
 * that a screen reader reads by its column headers, and the boxes, of the
 * fields and to tick, are artifacts. The layout is that of the inspection
 * checklist template of PDFjet Forms. The building is made up.
 */
public class Example_71 {
    private const String SITE = "Pinecrest Community Center";

    // The areas to inspect, each its name and then its items
    private static readonly String[][] AREAS = {
        new String[] {"Exits and fire safety",
            "Exits and hallways are clear",
            "Exit signs are lit",
            "Fire extinguishers are in place and charged",
            "Smoke alarms were tested"},
        new String[] {"Work areas",
            "Floors are dry and free of trip hazards",
            "Electrical cords are in good shape",
            "Shelves and storage are stable",
            "The first aid kit is stocked"},
    };

    private const int BLUE = 0x1f58b0;
    private const int RED = 0xb00020;
    private const int RULE = 0xdde1e8;
    private const int BOX = 0x4d4d4d;
    private const int HEADER = 0xe3edf9;
    private const int AREA = 0xf1f3f6;

    // The grid: the margin, the width of the page between the margins, and
    // the gap between two fields of a row
    private const float LEFT = 48f;
    private const float WIDTH = 516f;
    private const float GAP = 12f;
    private const float HALF = (WIDTH - GAP) / 2f;

    // A box tall enough for a line of handwriting, and the height of each
    // line more
    private const float BOX_HEIGHT = 24f;
    private const float LINE_HEIGHT = 20f;

    // The side of a box to tick, about the height of the capital letters
    private const float TICK = 11f;

    // Room at the foot of each page for the footer, and the height of the
    // fields under the list
    private const float FOOTER_ROOM = 72f;   // the bottom margin, clear of "Page i of N"
    private const float FIELDS_HEIGHT = 170f;

    private Page page;
    private readonly Font labelFont;
    private readonly Font textFont;

    public Example_71() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_71.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Monthly safety inspection");
        pdf.SetAuthor(SITE);
        pdf.SetLanguage("en-US");

        textFont = new Font(pdf, IBMPlexSans.Regular);
        textFont.SetSize(11f);
        labelFont = new Font(pdf, IBMPlexSans.SemiBold);
        labelFont.SetSize(10f);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        italic.SetSize(10f);

        // The pages are kept in a list and added to the document at the end,
        // when the footer can say how many there are.
        List<Page> pages = new List<Page>();
        page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pages.Add(page);

        // The title, how to use the list, and a rule
        Heading("Monthly safety inspection", StructElem.H1, 20f, 62f);
        TextBlock intro = new TextBlock(textFont, "Walk through each area, tick what is in "
                + "order, and note anything that needs fixing.");
        intro.SetLineSpacing(1.15f);
        intro.SetLocation(LEFT, 74f);
        intro.SetWidth(WIDTH);
        float y = intro.DrawOn(page)[1] + 12f;
        Rule(y);

        new TextLine(italic, "Fields marked * are required.")
                .SetTextColor(RED)
                .SetLocation(LEFT, y + 20f)
                .DrawOn(page);

        // Where, who and when, a row of a half and two quarters. The list is
        // printed for one building, so its name is filled in already.
        y += 40f;
        float quarter = (HALF - GAP) / 2f;
        Field("Location *", LEFT, y, HALF, 1, SITE);
        Field("Inspected by *", LEFT + HALF + GAP, y, quarter, 1, null);
        y = Field("Date *", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The list: a header row, then each area and its items. The row of an
        // area is kept on the page of its first item.
        List<List<Cell>> rows = new List<List<Cell>>();
        List<int> areaRows = new List<int>();
        rows.Add(Row(new Cell(labelFont, "Item"), new Cell(labelFont, "OK"),
                new Cell(labelFont, "To fix"), new Cell(labelFont, "Notes")));
        foreach (String[] area in AREAS) {
            // The name of the area across the row; a cell that spans columns
            // is followed by empty ones that it covers
            Cell name = new Cell(labelFont, area[0]).SetColSpan(4).SetBackgroundColor(AREA);
            areaRows.Add(rows.Count);
            rows.Add(Row(name, new Cell(textFont), new Cell(textFont), new Cell(textFont)));
            for (int i = 1; i < area.Length; i++) {
                rows.Add(Row(new Cell(textFont, area[i]), Tick(textFont), Tick(textFont),
                        new Cell(textFont)));
            }
        }

        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(labelFont, Color.black, HEADER);
        table.SetCellPadding(5f, 6f);
        table.SetCellBorderWidth(0.5f);
        table.SetCellBorderColor(BOX);
        table.SetWidth(WIDTH);
        table.SetColumnWidthsInPercent(50f, 9f, 9f, 32f);
        table.SetTextAlignmentInColumn(1, Alignment.CENTER);
        table.SetTextAlignmentInColumn(2, Alignment.CENTER);
        foreach (int index in areaRows) {
            table.KeepRowWithNext(index);
        }
        // Under the fields on the first page, at the top of the next ones,
        // and clear of the footer on all of them
        table.SetLocation(LEFT, 48f);
        table.SetFirstPageTopMargin(y + 6f);
        table.SetBottomMargin(FOOTER_ROOM);
        float[] end = table.DrawOn(pdf, page, pages, Letter.PORTRAIT);
        page = pages[pages.Count - 1];

        // The fields under the last row. When they no longer fit on its page,
        // they go on a page of their own.
        y = end[1] + 18f;
        if (y + FIELDS_HEIGHT > page.GetHeight() - FOOTER_ROOM) {
            page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
            pages.Add(page);
            y = 48f;
        }
        y = Field("Problems found, and who will fix them", LEFT, y, WIDTH, 3, null);
        new CheckBox(textFont, "I confirm this inspection and sign below. *")
                .SetBorderColor(BOX)
                .SetLocation(LEFT, y)
                .DrawOn(page);
        y += 30f;
        Field("Inspector's signature *", LEFT, y, HALF, 1, null);
        Field("Date *", LEFT + HALF + GAP, y, quarter, 1, null);
        Field("Next inspection", LEFT + HALF + GAP + quarter + GAP, y, quarter, 1, null);

        // The footer, on every page, now that their number is known
        for (int i = 0; i < pages.Count; i++) {
            Page p = pages[i];
            TextLine footer = new TextLine(textFont, SITE + ", monthly safety inspection, page "
                    + (i + 1) + " of " + pages.Count);
            footer.SetFontSize(9f);
            footer.SetTextColor(Color.dimgray);
            footer.SetLocation(LEFT, p.GetHeight() - 36f);
            p.AddArtifactBMC();
            footer.DrawOn(p);
            p.AddEMC();
        }
        pdf.AddPages(pages);
        pdf.Complete();
    }

    // A cell with a box to tick: a square, placed in the cell with
    // SetDrawable and centered as the column is. A Rect is drawn as an
    // artifact; the column header says what a tick in it means.
    private static Cell Tick(Font font) {
        Rect box = new Rect(0f, 0f, TICK, TICK);
        box.SetBorderColor(BOX);
        box.SetBorderWidth(0.75f);
        return new Cell(font).SetDrawable(box);
    }

    // A row of the table, of the cells.
    private static List<Cell> Row(params Cell[] cells) {
        List<Cell> row = new List<Cell>();
        foreach (Cell cell in cells) {
            row.Add(cell);
        }
        return row;
    }

    // Draws a heading in blue, its baseline at y, and returns where the field
    // under it starts.
    private float Heading(String text, StructElem level, float size, float y) {
        TextLine heading = new TextLine(labelFont, text);
        heading.SetStructureType(level);
        heading.SetFontSize(size);
        heading.SetTextColor(BLUE);
        heading.SetLocation(LEFT, y);
        heading.DrawOn(page);
        return y + 16f;
    }

    // Draws the label of a field, its top at y.
    private void Label(String text, float x, float y) {
        TextLine label = new TextLine(labelFont, text);
        label.SetLocation(x, y + labelFont.GetAscent());
        label.DrawOn(page);
    }

    // Draws a field: its label at y and the box under it, of room for the
    // lines of handwriting, with the value in it when there is one. Returns
    // where the next row starts.
    private float Field(String text, float x, float y, float width, int lines, String value) {
        Label(text, x, y);
        float top = y + 16f;
        float height = BOX_HEIGHT + (lines - 1) * LINE_HEIGHT;
        Rect box = new Rect(x, top, width, height);
        box.SetBorderColor(BOX);
        box.SetBorderWidth(0.5f);
        box.DrawOn(page);
        if (value != null) {
            TextLine line = new TextLine(textFont, value);
            line.SetTextColor(BLUE);
            line.SetLocation(x + 6f, top + 16f);
            line.DrawOn(page);
        }
        return top + height + 12f;
    }

    // A thin line across the page, decoration only, so an artifact that a
    // screen reader skips.
    private void Rule(float y) {
        Line line = new Line(LEFT, y, LEFT + WIDTH, y);
        line.SetStrokeColor(RULE);
        page.AddArtifactBMC();
        line.DrawOn(page);
        page.AddEMC();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_71();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_71 => {time1 - time0,4} ms");
    }
}   // End of Example_71.cs
