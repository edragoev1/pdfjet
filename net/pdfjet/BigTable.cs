/*
 * BigTable.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
    /// <summary>
    /// A table for large amounts of data, read row by row from a delimited text file or
    /// from an IEnumerable. Each page is written as soon as it is full, so the memory
    /// stays flat however many rows there are. A PDF/UA document holds no more than
    /// that either: the table is tagged as a table, and the structure elements of a
    /// page are written with it. What is left is one cross-reference entry for each of
    /// them, which every object of a PDF has.
    /// </summary>
    public class BigTable {
        private readonly PDF pdf;
        private readonly Font f1;
        private readonly Font f2;
        private readonly PageSize pageSize;
        private float x;
        private float y;
        private float yText;
        private List<Page> pages;
        private Page page;
        private float[] widths;
        private string[] headerFields;
        private Alignment[] alignment;
        private float[] vertLines;
        // The width the text of each column has when the table is too wide
        // for the page and the last of its columns are cut back to fit it, or
        // null when the table fits: a column that is as wide as its widest
        // field holds every field of it whole and needs no measuring as it is
        // drawn.
        private float[] clipped;
        private float bottomMargin = 20.0f;
        private float padding = 2.0f;
        private bool highlightRow = true;
        // In a PDF/UA document the table is a Table element that the rows of
        // every page go on adding to; see DrawFieldsAndLine.
        private StructElement structElement = null;
        private float[] shadingColor = Rgb(0xF0F0F0);   // null for no shading
        private float[] borderColor = Rgb(0xB0B0B0);    // null for no lines
        private string footerText = "Page {page} of {pages}";
        private Font footerFont;                        // null for the header font
        private IEnumerable<string[]> rows;
        private bool checkLineBreaks;   // The rows are not read from a file, which has no line breaks left
        private int[] columns = new int[0];     // The fields drawn, in the order they are drawn
        private int numberOfColumns;            // The length of columns
        private int fieldsNeeded;               // The fields a row needs: the largest index plus 1
        private bool startNewPage = true;
        private int dataRows;           // The rows under the header, counted by SetTableData
        private int pageCount;          // The pages they take, counted by Complete
        private int pageNumber;         // The page being drawn
        private bool footerDrawn;

        /// <summary>
        /// Creates a table with the specified fonts and page size.
        /// </summary>
        /// <param name="pdf">the PDF.</param>
        /// <param name="f1">the header font.</param>
        /// <param name="f2">the body font.</param>
        /// <param name="pageSize">the page size, for example Letter.PORTRAIT.</param>
        public BigTable(PDF pdf, Font f1, Font f2, PageSize pageSize) {
            this.pdf = pdf;
            this.f1 = f1;
            this.f2 = f2;
            this.pageSize = pageSize;
            this.pages = new List<Page>();
        }

        /// <summary>Sets the location of the top left corner of this table.</summary>
        public BigTable SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
            if (this.vertLines != null) {
                SetVertLines();
            }
            return this;
        }

        /// <summary>
        /// Sets the number of columns in this table: the first fields of every row, in their
        /// order. It is the same as SetColumns(0, 1, ... numberOfColumns - 1).
        /// </summary>
        public BigTable SetNumberOfColumns(int numberOfColumns) {
            int[] columns = new int[Math.Max(numberOfColumns, 0)];
            for (int i = 0; i < columns.Length; i++) {
                columns[i] = i;
            }
            return SetColumns(columns);
        }

        /// <summary>
        /// Sets the fields of every row that the table draws, by their index from 0, in the order
        /// they are drawn, so SetColumns(3, 0, 11) draws the fourth field, then the first, then
        /// the twelfth. The indexes pick the header fields the same way. A row without a field for
        /// every index is skipped. Call it before SetTableData; SetTextAlignment counts the
        /// columns as they are drawn.
        /// </summary>
        /// <param name="columns">the indexes of the fields to draw.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetColumns(params int[] columns) {
            int fieldsNeeded = 0;
            foreach (int column in columns) {
                if (column < 0) {
                    pdf.Fail(new ArgumentException("A column index cannot be negative."));
                }
                fieldsNeeded = Math.Max(fieldsNeeded, column + 1);
            }
            this.columns = (int[]) columns.Clone();
            this.numberOfColumns = columns.Length;
            this.fieldsNeeded = fieldsNeeded;
            return this;
        }

        /// <summary>
        /// Sets the text alignment of the specified column, which is one of
        /// the columns of the table. Call it after SetTableData, which makes
        /// the columns: a column that the table does not have is refused.
        /// </summary>
        public BigTable SetTextAlignment(int column, Alignment alignment) {
            if (this.alignment == null || column < 0 || column >= this.alignment.Length) {
                pdf.Fail(new ArgumentException("The table has no column " + column
                        + ": set the alignment of a column after SetTableData."));
            }
            this.alignment[column] = alignment;
            return this;
        }

        /// <summary>
        /// Sets the color of every other row, starting with the header. Color.transparent turns
        /// the shading off.
        /// </summary>
        /// <param name="color">the color as a 0xRRGGBB value, for example Color.lightgray.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetShadingColor(int color) {
            this.shadingColor = (color == Color.transparent) ? null : Rgb(color);
            return this;
        }

        /// <summary>
        /// Sets the color of every other row, starting with the header, from its red, green and
        /// blue values from 0 to 1. null turns the shading off.
        /// </summary>
        /// <param name="color">the red, green and blue values.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetShadingColor(float[] color) {
            this.shadingColor = Util.CopyOf(color);
            return this;
        }

        /// <summary>
        /// Sets the color of the lines between the rows and the columns and around the table.
        /// Color.transparent leaves the lines out.
        /// </summary>
        /// <param name="color">the color as a 0xRRGGBB value, for example Color.gray.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetBorderColor(int color) {
            this.borderColor = (color == Color.transparent) ? null : Rgb(color);
            return this;
        }

        /// <summary>
        /// Sets the color of the lines between the rows and the columns and around the table from
        /// its red, green and blue values from 0 to 1. null leaves the lines out.
        /// </summary>
        /// <param name="color">the red, green and blue values.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetBorderColor(float[] color) {
            this.borderColor = Util.CopyOf(color);
            return this;
        }

        /// <summary>
        /// Sets the space between the text of a column and the lines on its left and right. It is
        /// 2 points by default.
        /// </summary>
        /// <param name="padding">the padding.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetPadding(float padding) {
            if (padding < 0f) {
                pdf.Fail(new ArgumentException("The padding cannot be negative."));
            }
            this.padding = padding;
            if (this.vertLines != null) {
                SetVertLines();
            }
            return this;
        }

        /// <summary>
        /// Sets the footer drawn at the bottom of every page, centered. In the text, {page} stands
        /// for the number of the page and {pages} for the number of pages. The footer is
        /// "Page {page} of {pages}" in the header font by default. A null or empty text leaves the
        /// footer out.
        /// </summary>
        /// <param name="text">the text, for example "Seite {page} von {pages}".</param>
        /// <param name="font">the font, or null for the header font.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetFooter(string text, Font font) {
            this.footerText = text;
            this.footerFont = font;
            return this;
        }

        /// <summary>Sets the bottom margin.</summary>
        public BigTable SetBottomMargin(float bottomMargin) {
            this.bottomMargin = bottomMargin;
            return this;
        }

        /// <summary>Returns the pages, which Complete has already added to the PDF.</summary>
        public List<Page> GetPages() {
            return pages;
        }

        // Creates the next page. It is added to the PDF right away, so the content
        // of the page before it is compressed and written, and its memory freed.
        private void NewPage() {
            page = new Page(pdf, pageSize);
            pages.Add(page);
            pageNumber++;
            footerDrawn = false;
            page.SetPenWidth(0f);
            this.yText = this.y + f1.ascent;
            this.highlightRow = true;
            // The header fields are the TH cells of the table the first time
            // they are drawn, and an artifact where they repeat on the next pages.
            StructElem? header = null;
            if (structElement == null) {
                structElement = page.AddStructElement(
                        page.structParent, StructElem.TABLE, null, true);
                header = StructElem.TH;
            }
            DrawFieldsAndLine(headerFields, f1, header);
            this.yText += f1.descent + f2.ascent;
            startNewPage = false;
        }

        // Draws the footer of the page that was just finished, once. The page is
        // finished before the next one is created, so it is written complete.
        private void DrawFooter() {
            if (!footerDrawn && !string.IsNullOrEmpty(footerText)) {
                string text = footerText
                        .Replace("{page}", pageNumber.ToString(CultureInfo.InvariantCulture))
                        .Replace("{pages}", pageCount.ToString(CultureInfo.InvariantCulture));
                // The page number repeats on every page, which makes it an artifact.
                page.AddArtifactBMC();
                page.AddFooter(new TextLine(footerFont ?? f1, text));
                page.AddEMC();
            }
            footerDrawn = true;
        }

        private void DrawTextAndLine(string[] fields) {
            if (startNewPage) {
                NewPage();
            }

            DrawFieldsAndLine(fields, f2, StructElem.TD);
            this.yText += f2.ascent + f2.descent;
            if (this.yText > (this.page.GetHeight() - this.bottomMargin)) {
                DrawTheVerticalLines();
                DrawFooter();
                startNewPage = true;
            }
        }

        // Counts the pages the rows take, as DrawTextAndLine breaks them, so that
        // the "Page i of N" footer of a page can be drawn before the next page is
        // created. The location and the bottom margin are set after SetTableData,
        // so the pages are counted when the table is drawn.
        private int CountPages() {
            float pageHeight = pageSize.GetHeight();
            float yTop = this.y + f1.ascent + f1.descent + f2.ascent;
            float yPos = yTop;
            int count = 1;
            bool newPage = false;
            for (int i = 0; i < this.dataRows; i++) {
                if (newPage) {
                    count++;
                    yPos = yTop;
                    newPage = false;
                }
                yPos += f2.ascent + f2.descent;
                if (yPos > (pageHeight - this.bottomMargin)) {
                    newPage = true;
                }
            }
            return count;
        }

        // Draws a row of the table. In a PDF/UA document the row is a TR element
        // and each field a TH or TD element that holds the text, and the shading
        // and the lines are artifacts. A row with no cell structure is drawn as
        // an artifact, which is what the header rows that repeat on the next
        // pages are.
        private void DrawFieldsAndLine(string[] fields, Font font, StructElem? cellStructure) {
            // The shading and the line above the text carry no meaning of their own.
            page.AddArtifactBMC();
            if (this.highlightRow) {
                if (shadingColor != null) {
                    HighlightRow(page, font, shadingColor);
                }
                this.highlightRow = false;
            } else {
                this.highlightRow = true;
            }

            // Draw the line above the text.
            if (borderColor != null) {
                float[] original = page.GetPenColor();
                page.SetPenColor(borderColor);
                page.MoveTo(vertLines[0], this.yText - font.ascent);
                page.LineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
                page.StrokePath();
                page.SetPenColor(original);
            }
            page.AddEMC();
            page.SetBrushColor(Color.black);

            bool tagged = structElement != null && cellStructure != null;
            StructElement parent = page.structParent;
            StructElement rowElement = null;
            if (tagged) {
                rowElement = page.AddStructElement(structElement, StructElem.TR, null);
            }
            for (int i = 0; i < this.numberOfColumns; i++) {
                String text = checkLineBreaks ? Util.LineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
                // A column the page was too narrow for holds as much of the
                // text as fits it, ending in " ..."; the cell of the structure
                // tree keeps the whole of the text, which is what a reader
                // reads. A column that was not cut is as wide as its widest
                // field, so every field of it is drawn whole and none is
                // measured here.
                String drawn = (clipped != null && clipped[i] < widths[i])
                        ? Fit(text, font, clipped[i]) : text;
                float xText = vertLines[i] + this.padding;
                if (alignment[i] == Alignment.RIGHT) {
                    xText = (vertLines[i + 1] - this.padding) - font.StringWidth(drawn);
                }
                if (tagged) {
                    // A cell holds its text and nothing else, so the cell
                    // element holds the marked content of the text: it needs
                    // no paragraph of its own, which would be another object
                    // for every cell.
                    page.structParent = rowElement;
                    page.AddBDC(cellStructure.Value, null, null, text,
                            CellAttributes(cellStructure.Value));
                } else {
                    page.AddArtifactBMC();
                }
                page.DrawTextLine(font, drawn, xText, this.yText);
                page.AddEMC();
            }
            page.structParent = parent;
        }

        // The attributes of a cell element: a header cell heads the column it is in.
        private static String CellAttributes(StructElem cellStructure) {
            return (cellStructure == StructElem.TH) ? "<</O /Table /Scope /Column>>" : null;
        }

        private void HighlightRow(Page page, Font font, float[] color) {
            float[] original = page.GetBrushColor();
            page.SetBrushColor(color);
            page.FillRectBetween(vertLines[0], this.yText - font.ascent,
                    vertLines[this.numberOfColumns], this.yText + font.descent);
            page.SetBrushColor(original);
        }

        private void DrawTheVerticalLines() {
            if (borderColor == null) {
                return;
            }
            // The lines of the table carry no meaning of their own.
            page.AddArtifactBMC();
            float[] original = page.GetPenColor();
            page.SetPenColor(borderColor);
            for (int i = 0; i <= this.numberOfColumns; i++) {
                page.DrawLine(
                    vertLines[i],
                    this.y,
                    vertLines[i],
                    this.yText - f2.ascent);
            }
            page.MoveTo(vertLines[0], this.yText - f2.ascent);
            page.LineTo(vertLines[this.numberOfColumns], this.yText - f2.ascent);
            page.StrokePath();
            page.SetPenColor(original);
            page.AddEMC();
        }

        private static float[] Rgb(int color) {
            return new float[] {
                ((color >> 16) & 0xff)/255f, ((color >> 8) & 0xff)/255f, (color & 0xff)/255f};
        }

        // A number is right-aligned, as Table.RightAlignNumbers aligns it.
        private Alignment GetAlignment(string str) {
            return Table.IsNumber(str) ? Alignment.RIGHT : Alignment.LEFT;
        }

        /// <summary>
        /// Reads the data file to set the column widths, the column alignment and the header fields.
        /// The file is read as UTF-8. Its first line with a field for every column is the header,
        /// and the lines after it are the rows. A quoted field is read as RFC 4180 reads it, so a
        /// delimiter inside one is text, and a line break inside one goes on to the next line of
        /// the file and is drawn as a space.
        /// </summary>
        /// <param name="fileName">the data file.</param>
        /// <param name="delimiter">the field delimiter.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetTableData(string fileName, string delimiter) {
            int fieldsNeeded = this.fieldsNeeded;
            string[] header = new string[0];
            foreach (string[] fields in ReadDataFile(fileName, delimiter, fieldsNeeded, false)) {
                if (fields.Length >= fieldsNeeded) {
                    header = fields;
                    break;
                }
            }
            return SetTableData(header, false, ReadDataFile(fileName, delimiter, fieldsNeeded, true));
        }

        /// <summary>
        /// Sets the column widths, the column alignment and the header fields from rows that are
        /// not in a file: the results of a query, or a list of objects. The rows are enumerated
        /// twice, once here to measure the columns and once by Complete to draw them, and neither
        /// keeps them, so an IEnumerable that runs the query again, or maps the objects to fields
        /// as it goes, keeps the memory flat. A row without a field for every column is skipped,
        /// as a short line of a file is, and a line break in a field is drawn as a space.
        /// </summary>
        /// <param name="header">the header fields, with a field for every column.</param>
        /// <param name="rows">the fields of each row, in the order they are drawn.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetTableData(string[] header, IEnumerable<string[]> rows) {
            return SetTableData(header, true, rows);
        }

        // With checkLineBreaks, each field is looked at for line breaks to draw as
        // spaces; the rows of a data file have them as spaces already.
        private BigTable SetTableData(string[] header, bool checkLineBreaks, IEnumerable<string[]> rows) {
            this.checkLineBreaks = checkLineBreaks;
            if (header.Length < this.fieldsNeeded) {
                pdf.Fail(new ArgumentException("The header does not have a field for every column."));
            }
            this.rows = rows;
            this.vertLines = new float[this.numberOfColumns + 1];
            this.headerFields = (string[]) header.Clone();
            this.widths = new float[this.numberOfColumns];
            this.alignment = new Alignment[this.numberOfColumns];

            Measure(header, f1);
            int rowNumber = 0;
            foreach (string[] fields in rows) {
                if (fields.Length < this.fieldsNeeded) {
                    continue;
                }
                if (rowNumber == 0) {   // Determine alignment from first data row
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        alignment[i] = GetAlignment(fields[columns[i]]);
                    }
                }
                Measure(fields, f2);
                rowNumber++;
            }
            this.dataRows = rowNumber;

            SetVertLines();
            return this;
        }

        // Widens the columns to fit the fields of a row. The widths are those of
        // the text, and SetVertLines adds the padding, so it can be set later.
        // Widens the columns to fit the fields of a row, measured in the font
        // the row is drawn with: the header font for the header and the body
        // font for a row under it.
        private void Measure(string[] fields, Font font) {
            for (int i = 0; i < this.numberOfColumns; i++) {
                String text = checkLineBreaks ? Util.LineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
                float width = font.StringWidth(text);
                if (width > widths[i]) {
                    this.widths[i] = width;
                }
            }
        }

        // Sets the x coordinates of the vertical lines from the location, the
        // column widths and the padding.
        // Puts each vertical line where the columns before it end, and cuts
        // the last of the columns back when they run past the right edge of
        // the page: a column as wide as its widest field is what the table
        // asks for, and a table wider than its page draws the last of its
        // columns off the edge, where they are lost. The columns are cut from
        // the last one back, so the table keeps the widths it asked for as
        // far as the page allows, and the fields of a column that was cut are
        // drawn with the end of their text replaced by " ...".
        private void SetVertLines() {
            float[] widths = (float[]) this.widths.Clone();
            // The table is drawn between the margin it starts at and the same
            // margin on the right of the page.
            float room = pageSize.GetWidth() - 2 * this.x - widths.Length * 2 * this.padding;
            float least = Math.Max(f1.StringWidth(ELLIPSIS), f2.StringWidth(ELLIPSIS));
            float total = 0f;
            foreach (float width in widths) {
                total += width;
            }
            this.clipped = null;
            for (int i = widths.Length - 1; i >= 0 && total > room; i--) {
                float cut = Math.Min(total - room, Math.Max(widths[i] - least, 0f));
                if (cut > 0f) {
                    widths[i] -= cut;
                    total -= cut;
                    if (this.clipped == null) {
                        this.clipped = (float[]) this.widths.Clone();
                    }
                    this.clipped[i] = widths[i];
                }
            }
            float vertLineX = this.x;
            this.vertLines[0] = vertLineX;
            for (int i = 0; i < widths.Length; i++) {
                vertLineX += widths[i] + 2 * this.padding;
                this.vertLines[i + 1] = vertLineX;
            }
        }

        // The mark that ends a field whose text was cut to fit its column,
        // which takes the place of the last four characters of what fits.
        private const string ELLIPSIS = " ...";

        // The text as much of it as fits the width, ending in the mark that
        // says it was cut. The mark takes the place of the last four
        // characters of what fits, rather than being added to them, so the
        // text stays inside the column; a character built from a surrogate
        // pair is not cut in half.
        private static string Fit(string text, Font font, float width) {
            if (font.StringWidth(text) <= width) {
                return text;
            }
            int end = 0;                    // The most characters that fit.
            int high = text.Length;
            while (end < high) {
                int middle = end + (high - end + 1) / 2;
                if (font.StringWidth(text.Substring(0, middle)) <= width) {
                    end = middle;
                } else {
                    high = middle - 1;
                }
            }
            end = Math.Max(0, end - ELLIPSIS.Length);
            while (end > 0 && font.StringWidth(text.Substring(0, end) + ELLIPSIS) > width) {
                end--;
            }
            if (end > 0 && Char.IsHighSurrogate(text[end - 1])) {
                end--;
            }
            return text.Substring(0, end) + ELLIPSIS;
        }

        // Yields the fields of the lines of the data file, which is read as UTF-8
        // only, as in the other ports, after the byte order mark at its start, if
        // there is one. The quoted fields are read as RFC 4180 does, so a delimiter
        // inside one is text and not a column break, and one with line breaks takes
        // the lines up to its closing quote. With skipHeader, the rows
        // start after the header: the first line with at least fieldsNeeded fields.
        private static IEnumerable<string[]> ReadDataFile(
                string fileName, string delimiter, int fieldsNeeded, bool skipHeader) {
            using (StreamReader reader = new StreamReader(fileName, new UTF8Encoding(false), false)) {
                if (reader.Peek() == '\uFEFF') {
                    reader.Read();
                }
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] fields = Util.ReadRecord(line, reader, delimiter);
                    if (skipHeader) {
                        skipHeader = fields.Length < fieldsNeeded;
                        continue;
                    }
                    yield return fields;
                }
            }
        }

        /// <summary>
        /// Draws the rows, then the vertical lines, with the footer on every page.
        /// The pages are added to the PDF as they are drawn, so the document does not hold them
        /// all. Call it after the location, the bottom margin and the table data have been set.
        /// </summary>
        public void Complete() {
            this.pageCount = CountPages();
            NewPage();
            foreach (string[] fields in rows) {
                if (fields.Length < this.fieldsNeeded) {
                    continue;
                }
                this.DrawTextAndLine(fields);
            }
            DrawTheVerticalLines();
            DrawFooter();
        }
    }
}
