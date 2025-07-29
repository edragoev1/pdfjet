using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/**
 * Use this class if you have a lot of data.
 */
namespace PDFjet.NET {
public class BigTable {
    private PDF pdf;
    private Page page;
    private float[] pageSize;
    private Font f1;
    private Font f2;
    private float x1;
    private float y1;
    private float yText;
    private List<Page> pages;
    private List<int> align;
    private List<float> vertLines;
    private List<String> headerRow;
    private float bottomMargin = 15f;
    private float spacing;
    private float padding = 2f;
    private String language = "en-US";
    private bool highlightRow = true;
    private int highlightColor = 0xF0F0F0;
    private int penColor = 0xB0B0B0;

    public BigTable(PDF pdf, Font f1, Font f2, float[] pageSize) {
        this.pdf = pdf;
        this.f1 = f1;
        this.f2 = f2;
        this.pageSize = pageSize;
        this.pages = new List<Page>();
    }

    public void SetLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
    }

    public void SetTextAlignment(List<int> align) {
        this.align = align;
    }

    public void SetColumnSpacing(float spacing) {
        this.spacing = spacing;
    }

    public void SetBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
    }

    public void SetLanguage(String language) {
        this.language = language;
    }

    public List<Page> GetPages() {
        return pages;
    }

    public void SetColumnWidths(List<float> widths) {
        vertLines = new List<float>();
        vertLines.Add(x1);
        float sumOfWidths = x1;
        foreach (float width in widths) {
            sumOfWidths += width + spacing;
            vertLines.Add(sumOfWidths);
        }
    }

    public void DrawRow(List<String> row, int markerColor) {
        if (headerRow == null) {
            headerRow = row;
            NewPage(Color.black);
        } else {
            DrawOn(row, markerColor);
        }
    }

    private void NewPage(int color) {
        float[] original;
        if (page != null) {
            page.AddArtifactBMC();
            original = page.GetPenColor();
            page.SetPenColor(penColor);
            page.DrawLine(vertLines[0], yText - f1.ascent, vertLines[headerRow.Count], yText - f1.ascent);
            // Draw the vertical lines
            for (int i = 0; i <= headerRow.Count; i++) {
                page.DrawLine(vertLines[i], y1, vertLines[i], yText - f1.ascent);
            }
            page.SetPenColor(original);
            page.AddEMC();
        }

        page = new Page(pdf, pageSize, Page.DETACHED);
        pages.Add(page);
        page.SetPenWidth(0f);
        yText = y1 + f1.ascent;

        // Highlight row and draw horizontal line
        page.AddArtifactBMC();
        DrawHighlight(page, highlightColor, f1);
        highlightRow = false;
        original = page.GetPenColor();
        page.SetPenColor(penColor);
        page.DrawLine(vertLines[0], yText - f1.ascent, vertLines[headerRow.Count], yText - f1.ascent);
        page.SetPenColor(original);
        page.AddEMC();

        String rowText = GetRowText(headerRow);
        page.AddBMC(StructElem.P, language, rowText, rowText);
        page.SetTextFont(f1);
        page.SetBrushColor(color);
        float xText = 0f;
        float xText2 = 0f;
        for (int i = 0; i < headerRow.Count; i++) {
            String text = headerRow[i];
            xText = vertLines[i];
            xText2 = vertLines[i + 1];
            page.BeginText();
            if (align == null || align[i] == 0) {   // Align Left
                page.SetTextLocation((xText + padding), yText);
            } else if (align[i] == 1) {             // Align Right
                page.SetTextLocation((xText2 - padding) - f1.StringWidth(text), yText);
            }
            page.DrawText(text);
            page.EndText();
        }
        page.AddEMC();
        yText += f1.descent + f2.ascent;
    }

    private void DrawOn(List<String> row, int markerColor) {
        if (row.Count > headerRow.Count) {
            // Prevent crashes when some data rows have extra fields!
            // The application should check for this and handle it the right way.
            return;
        }

        // Highlight row and draw horizontal line
        page.AddArtifactBMC();
        float[] original;
        if (highlightRow) {
            DrawHighlight(page, highlightColor, f2);
            highlightRow = false;
        } else {
            highlightRow = true;
        }
        original = page.GetPenColor();
        page.SetPenColor(penColor);
        page.DrawLine(vertLines[0], yText - f2.ascent, vertLines[headerRow.Count], yText - f2.ascent);
        page.SetPenColor(original);
        page.AddEMC();

        String rowText = GetRowText(row);
        page.AddBMC(StructElem.P, language, rowText, rowText);
        page.SetPenWidth(0f);
        page.SetTextFont(f2);
        page.SetBrushColor(Color.black);
        float xText = 0f;
        float xText2 = 0f;
        for (int i = 0; i < row.Count; i++) {
            String text = row[i];
            xText = vertLines[i];
            xText2 = vertLines[i + 1];
            page.BeginText();
            if (align == null || align[i] == 0) {   // Align Left
                page.SetTextLocation((xText + padding), yText);
            } else if (align[i] == 1) {             // Align Right
                page.SetTextLocation((xText2 - padding) - f2.StringWidth(text), yText);
            }
            page.DrawText(text);
            page.EndText();
        }
        page.AddEMC();
        if (markerColor != Color.black) {
            page.AddArtifactBMC();
            float[] originalColor = page.GetPenColor();
            page.SetPenColor(markerColor);
            page.SetPenWidth(3f);
            page.DrawLine(vertLines[0] - 2f, yText - f2.ascent, vertLines[0] - 2f, yText + f2.descent);
            page.DrawLine(xText2 + 2f, yText - f2.ascent, xText2 + 2f, yText + f2.descent);
            page.SetPenColor(originalColor);
            page.SetPenWidth(0f);
            page.AddEMC();
        }
        yText += f2.descent + f2.ascent;
        if (yText + f2.descent > (page.height - bottomMargin)) {
            NewPage(Color.black);
        }
    }

    public void Complete() {
        page.AddArtifactBMC();
        float[]original = page.GetPenColor();
        page.SetPenColor(penColor);
        page.DrawLine(vertLines[0], yText - f2.ascent, vertLines[headerRow.Count], yText - f2.ascent);
        // Draw the vertical lines
        for (int i = 0; i <= headerRow.Count; i++) {
            page.DrawLine(vertLines[i], y1, vertLines[i], yText - f1.ascent);
        }
        page.SetPenColor(original);
        page.AddEMC();
    }

    private void DrawHighlight(Page page, int color, Font font) {
        float[] original = page.GetBrushColor();
        page.SetBrushColor(color);
        page.MoveTo(vertLines[0], yText - font.ascent);
        page.LineTo(vertLines[headerRow.Count], yText - font.ascent);
        page.LineTo(vertLines[headerRow.Count], yText + font.descent);
        page.LineTo(vertLines[0], yText + font.descent);
        page.FillPath();
        page.SetBrushColor(original);
    }

    private String GetRowText(List<String> row) {
        StringBuilder buf = new StringBuilder();
        foreach (String field in row) {
            buf.Append(field);
            buf.Append(" ");
        }
        return buf.ToString();
    }

    public List<float> getColumnWidths(String fileName) {
        StreamReader reader = new StreamReader(fileName);
        List<float> widths = new List<float>();
        align = new List<int>();
        int rowNumber = 0;
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            String[] fields = System.Text.RegularExpressions.Regex.Split(line, @"\,");
            for (int i = 0; i < fields.Length; i++) {
                String field = fields[i];
                float width = f1.StringWidth(null, field);
                if (rowNumber == 0) {   // Header Row
                    widths.Add(width);
                } else {
                    if (i < widths.Count && width > widths[i]) {
                        widths[i] = width;
                    }
                }
            }
            if (rowNumber == 1) {       // First Data Row
                foreach (String field in fields) {
                    align.Add(GetAlignment(field));
                }
            }
            rowNumber++;
        }
        reader.Close();
        return widths;
    }

    private int GetAlignment(String str) {
        StringBuilder buf = new StringBuilder();
        if (str.StartsWith("(") && str.EndsWith(")")) {
            str = str.Substring(1, str.Length - 1);
        }
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (ch != '.' && ch != ',' && ch != '\'') {
                buf.Append(ch);
            }
        }
        try {
            Double.Parse(buf.ToString());
            return 1;   // Align Right
        } catch (Exception) {
        }
        return 0;       // Align Left
    }
}
}
