using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PDFjet.NET {
    /// <summary>A table for large amounts of data, read row by row from a delimited text file.</summary>
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
        private string fileName;
        private string delimiter;
        private int numberOfColumns;
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

        /// <summary>Sets the number of columns in this table.</summary>
        public BigTable SetNumberOfColumns(int numberOfColumns) {
            this.numberOfColumns = numberOfColumns;
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
            if (page == null) {
                NewPage();
                return;
            }
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
                String text = fields[i];
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
        /// </summary>
        /// <param name="fileName">the data file.</param>
        /// <param name="delimiter">the field delimiter.</param>
        /// <returns>this BigTable object.</returns>
        public BigTable SetTableData(string fileName, string delimiter) {
            this.fileName = fileName;
            this.delimiter = delimiter;
            this.vertLines = new float[this.numberOfColumns + 1];
            this.headerFields = new string[this.numberOfColumns];
            this.widths = new float[this.numberOfColumns];
            this.alignment = new Alignment[this.numberOfColumns];

            int rowNumber = 0;
            using (StreamReader reader = OpenDataFile()) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    // The quoted fields are read as RFC 4180 does, so a
                    // delimiter inside one is text and not a column break.
                    string[] fields = Util.Split(line, this.delimiter);
                    if (fields.Length < this.numberOfColumns) {
                        continue;
                    }
                    if (rowNumber == 0) {
                        for (int i = 0; i < this.numberOfColumns; i++) {
                            headerFields[i] = fields[i];
                        }
                    }
                    if (rowNumber == 1) {
                        for (int i = 0; i < this.numberOfColumns; i++) {
                            alignment[i] = GetAlignment(fields[i]);
                        }
                    }
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        string field = fields[i];
                        float width = f1.StringWidth(field) + 2 * this.padding;
                        if (width > widths[i]) {
                            this.widths[i] = width;
                        }
                    }
                    rowNumber++;
                }
            }
            this.dataRows = (rowNumber > 0) ? rowNumber - 1 : 0;     // Without the header

            SetVertLines();
            return this;
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

        // Opens the data file, which is read as UTF-8 only, as in the other ports,
        // after the byte order mark at its start, if there is one.
        private StreamReader OpenDataFile() {
            StreamReader reader = new StreamReader(this.fileName, new UTF8Encoding(false), false);
            if (reader.Peek() == '\uFEFF') {
                reader.Read();
            }
            return reader;
        }

        /// <summary>
        /// Draws the rows read from the data file, then the vertical lines, with a
        /// "Page i of N" footer on every page. The pages are added to the PDF as they
        /// are drawn, so the document does not hold them all.
        /// </summary>
        public void Complete() {
            this.pageCount = CountPages();
            using (StreamReader reader = OpenDataFile()) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] fields = Util.Split(line, this.delimiter);
                    if (fields.Length < this.numberOfColumns) {
                        continue;
                    }
                    this.DrawTextAndLine(fields);
                }
            }
            DrawTheVerticalLines();
            DrawFooter();
        }
    }
}
