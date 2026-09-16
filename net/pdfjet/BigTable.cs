using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PDFjet.NET {
    /// <summary>
    /// A table for large amounts of data, read row by row from a delimited text file or
    /// from an IEnumerable. Each page is written as soon as it is full, so the memory
    /// stays flat however many rows there are.
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
        private float bottomMargin = 20.0f;
        private float padding = 2.0f;
        private bool highlightRow = true;
        private int highlightColor = 0xF0F0F0;
        private int penColor = 0xB0B0B0;
        private IEnumerable<string[]> rows;
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

        /// <summary>Sets the text alignment of the specified column.</summary>
        public BigTable SetTextAlignment(int column, Alignment alignment) {
            this.alignment[column] = alignment;
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
            DrawFieldsAndLine(headerFields, f1);
            this.yText += f1.descent + f2.ascent;
            startNewPage = false;
        }

        // Draws the footer of the page that was just finished, once. The page is
        // finished before the next one is created, so it is written complete.
        private void DrawFooter() {
            if (!footerDrawn) {
                page.AddFooter(new TextLine(f1, "Page " + pageNumber + " of " + pageCount));
                footerDrawn = true;
            }
        }

        private void DrawTextAndLine(string[] fields) {
            if (startNewPage) {
                NewPage();
            }

            DrawFieldsAndLine(fields, f2);
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

        private void DrawFieldsAndLine(string[] fields, Font font) {
            if (this.highlightRow) {
                HighlightRow(page, font, highlightColor);
                this.highlightRow = false;
            } else {
                this.highlightRow = true;
            }

        // Draw the line above the text.
            float[] original = page.GetPenColor();
            page.SetPenColor(penColor);
            page.MoveTo(vertLines[0], this.yText - font.ascent);
            page.LineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
            page.StrokePath();
            page.SetPenColor(original);
            page.SetBrushColor(Color.black);

            for (int i = 0; i < this.numberOfColumns; i++) {
                String text = fields[columns[i]];
                float xText = vertLines[i] + this.padding;
                if (alignment[i] == Alignment.RIGHT) {
                    xText = (vertLines[i + 1] - this.padding) - font.StringWidth(text);
                }
                page.DrawTextLine(font, text, xText, this.yText);
            }
        }

        private void HighlightRow(Page page, Font font, int color) {
            float[] original = page.GetBrushColor();
            page.SetBrushColor(color);
            page.MoveTo(vertLines[0], this.yText - font.ascent);
            page.LineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
            page.LineTo(vertLines[this.numberOfColumns], this.yText + font.descent);
            page.LineTo(vertLines[0], this.yText + font.descent);
            page.FillPath();
            page.SetBrushColor(original);
        }

        private void DrawTheVerticalLines() {
            float[] original = page.GetPenColor();
            page.SetPenColor(penColor);
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
        }

        // A number is right-aligned, as Table.RightAlignNumbers aligns it.
        private Alignment GetAlignment(string str) {
            return Table.IsNumber(str) ? Alignment.RIGHT : Alignment.LEFT;
        }

        /// <summary>
        /// Reads the data file to set the column widths, the column alignment and the header fields.
        /// The file is read as UTF-8. Its first line with a field for every column is the header, and the lines after it are the rows. A quoted field is read as
        /// RFC 4180 reads it, so a delimiter inside one is text.
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
            return SetTableData(header, ReadDataFile(fileName, delimiter, fieldsNeeded, true));
        }

        /// <summary>
        /// Sets the column widths, the column alignment and the header fields from rows that are
        /// not in a file: the results of a query, or a list of objects. The rows are enumerated
        /// twice, once here to measure the columns and once by Complete to draw them, and neither
        /// keeps them, so an IEnumerable that runs the query again, or maps the objects to fields
        /// as it goes, keeps the memory flat. A row without a field for every column is skipped,
        /// as a short line of a file is.
        /// </summary>
        /// <param name="header">the header fields, with a field for every column.</param>
        /// <param name="rows">the fields of each row, in the order they are drawn.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetTableData(string[] header, IEnumerable<string[]> rows) {
            if (header.Length < this.fieldsNeeded) {
                pdf.Fail(new ArgumentException("The header does not have a field for every column."));
            }
            this.rows = rows;
            this.vertLines = new float[this.numberOfColumns + 1];
            this.headerFields = (string[]) header.Clone();
            this.widths = new float[this.numberOfColumns];
            this.alignment = new Alignment[this.numberOfColumns];

            Measure(header);
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
                Measure(fields);
                rowNumber++;
            }
            this.dataRows = rowNumber;

            SetVertLines();
            return this;
        }

        // Widens the columns to fit the fields of a row.
        private void Measure(string[] fields) {
            for (int i = 0; i < this.numberOfColumns; i++) {
                float width = f1.StringWidth(fields[columns[i]]) + 2 * this.padding;
                if (width > widths[i]) {
                    this.widths[i] = width;
                }
            }
        }

        // Sets the x coordinates of the vertical lines from the location and the column widths.
        private void SetVertLines() {
            float vertLineX = this.x;
            this.vertLines[0] = vertLineX;
            for (int i = 0; i < widths.Length; i++) {
                vertLineX += this.widths[i];
                this.vertLines[i + 1] = vertLineX;
            }
        }

        // Yields the fields of the lines of the data file, which is read as UTF-8
        // only, as in the other ports, after the byte order mark at its start, if
        // there is one. The quoted fields are read as RFC 4180 does, so a delimiter
        // inside one is text and not a column break. With skipHeader, the rows
        // start after the header: the first line with at least fieldsNeeded fields.
        private static IEnumerable<string[]> ReadDataFile(
                string fileName, string delimiter, int fieldsNeeded, bool skipHeader) {
            using (StreamReader reader = new StreamReader(fileName, new UTF8Encoding(false), false)) {
                if (reader.Peek() == '\uFEFF') {
                    reader.Read();
                }
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] fields = Util.Split(line, delimiter);
                    if (skipHeader) {
                        skipHeader = fields.Length < fieldsNeeded;
                        continue;
                    }
                    yield return fields;
                }
            }
        }

        /// <summary>
        /// Draws the rows, then the vertical lines, with a "Page i of N" footer on every page.
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
