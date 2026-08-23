using System;
using System.Collections.Generic;
using System.IO;

namespace PDFjet.NET {
    public class BigTable {
        private readonly PDF pdf;
        private readonly Font f1;
        private readonly Font f2;
        private float[] pageSize;
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
        private string language = "en-US";
        private bool highlightRow = true;
        private int highlightColor = 0xF0F0F0;
        private int penColor = 0xB0B0B0;
        private string fileName;
        private string delimiter;
        private int numberOfColumns;
        private bool startNewPage = true;

        public BigTable(PDF pdf, Font f1, Font f2, float[] pageSize) {
            this.pdf = pdf;
            this.f1 = f1;
            this.f2 = f2;
            this.pageSize = pageSize;
            this.pages = new List<Page>();
        }

        public void SetLocation(float x, float y) {
            for (int i = 0; i <= this.numberOfColumns; i++) {
                this.vertLines[i] += x;
            }
            this.y = y;
        }

        public void SetNumberOfColumns(int numberOfColumns) {
            this.numberOfColumns = numberOfColumns;
        }

        public void SetTextAlignment(int column, Alignment alignment) {
            this.alignment[column] = alignment;
        }

        public void SetBottomMargin(float bottomMargin) {
            this.bottomMargin = bottomMargin;
        }

        public void SetLanguage(string language) {
            this.language = language;
        }

        public List<Page> GetPages() {
            return pages;
        }

        private void DrawTextAndLine(string[] fields) {
            if (page == null) {
                page = new Page(pdf, pageSize, Page.DETACHED);
                pages.Add(page);
                page.SetPenWidth(0f);
                this.yText = this.y + f1.ascent;
                this.highlightRow = true;
                DrawFieldsAndLine(headerFields, f1);
                this.yText += f1.descent + f2.ascent;
                startNewPage = false;
                return;
            }
            if (startNewPage) {
                page = new Page(pdf, pageSize, Page.DETACHED);
                pages.Add(page);
                page.SetPenWidth(0f);
                this.yText = this.y + f1.ascent;
                this.highlightRow = true;
                DrawFieldsAndLine(headerFields, f1);
                this.yText += f1.descent + f2.ascent;
                startNewPage = false;
            }

            DrawFieldsAndLine(fields, f2);
            this.yText += f2.ascent + f2.descent;
            if (this.yText > (this.page.GetHeight() - this.bottomMargin)) {
                DrawTheVerticalLines();
                startNewPage = true;
            }
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

        private Alignment GetAlignment(string str) {
            System.Text.StringBuilder buf = new System.Text.StringBuilder();
            if (str.StartsWith("(") && str.EndsWith(")")) {
                str = str.Substring(1, str.Length - 2);
            }
            for (int i = 0; i < str.Length; i++) {
                char ch = str[i];
                if (ch != '.' && ch != ',' && ch != '\'') {
                    buf.Append(ch);
                }
            }
            try {
                double.Parse(buf.ToString());
                return Alignment.RIGHT;
            } catch (FormatException) {
                return Alignment.LEFT;
            }
        }

        public void SetTableData(string fileName, string delimiter) {
            this.fileName = fileName;
            this.delimiter = delimiter;
            this.vertLines = new float[this.numberOfColumns + 1];
            this.headerFields = new string[this.numberOfColumns];
            this.widths = new float[this.numberOfColumns];
            this.alignment = new Alignment[this.numberOfColumns];

            int rowNumber = 0;
            using (StreamReader reader = new StreamReader(fileName)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] fields = line.Split(new string[] { this.delimiter }, StringSplitOptions.None);
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

            this.vertLines[0] = 0.0f;
            float vertLineX = 0.0f;
            for (int i = 0; i < widths.Length; i++) {
                vertLineX += this.widths[i];
                this.vertLines[i + 1] = vertLineX;
            }
        }

        public void Complete() {
            using (StreamReader reader = new StreamReader(this.fileName)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] fields = line.Split(new string[] { this.delimiter }, StringSplitOptions.None);
                    if (fields.Length < this.numberOfColumns) {
                        continue;
                    }
                    this.DrawTextAndLine(fields);
                }
            }
            DrawTheVerticalLines();
        }
    }
}
