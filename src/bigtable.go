package pdfjet

import (
	"bufio"
	"fmt"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
)

/**
 * Use this class if you have a lot of data.
 */
type BigTable struct {
	pdf            *PDF
	page           *Page
	pageSize       [2]float32
	f1             *Font
	f2             *Font
	x1             float32
	y1             float32
	yText          float32
	pages          []*Page
	align          []int
	vertLines      []float32
	headerRow      []string
	bottomMargin   float32
	spacing        float32
	padding        float32
	language       string
	highlightRow   bool
	highlightColor int32
	penColor       int32
}

func NewBigTable(pdf *PDF, f1, f2 *Font, pageSize [2]float32) *BigTable {
	table := new(BigTable)
	table.pdf = pdf
	table.pageSize = pageSize
	table.f1 = f1
	table.f2 = f2
	table.pages = make([]*Page, 0)
	table.bottomMargin = 15.0
	table.highlightColor = 0xF0F0F0
	table.penColor = 0xB0B0B0
	table.padding = 2.0
	return table
}

func (table *BigTable) SetLocation(x1, y1 float32) {
	table.x1 = x1
	table.y1 = y1
}

func (table *BigTable) SetTextAlignment(align []int) {
	table.align = align
}

func (table *BigTable) SetColumnSpacing(spacing float32) {
	table.spacing = spacing
}

func (table *BigTable) SetBottomMargin(bottomMargin float32) {
	table.bottomMargin = bottomMargin
}

func (table *BigTable) SetLanguage(language string) {
	table.language = language
}

func (table *BigTable) GetPages() []*Page {
	return table.pages
}

func (table *BigTable) SetColumnWidths(widths []float32) {
	table.vertLines = make([]float32, 0)
	table.vertLines = append(table.vertLines, table.x1)
	sumOfWidths := table.x1
	for _, width := range widths {
		sumOfWidths += width + table.spacing
		table.vertLines = append(table.vertLines, sumOfWidths)
	}
}

func (table *BigTable) DrawRow(row []string, markerColor int32) {
	if table.headerRow == nil {
		table.headerRow = row
		table.NewTablePage(row, color.Black)
	} else {
		table.DrawOn(row, markerColor)
	}
}

func (table *BigTable) NewTablePage(row []string, color int32) {
	if table.page != nil {
		table.page.AddArtifactBMC()
		original := table.page.GetPenColor()
		table.page.SetPenColor(table.penColor)
		table.page.DrawLine(float32(table.vertLines[0]), table.yText-table.f1.ascent,
			float32(table.vertLines[len(table.headerRow)]), table.yText-table.f1.ascent)
		// Draw the vertical lines
		for i := 0; i <= len(table.headerRow); i++ {
			table.page.DrawLine(table.vertLines[i], table.y1, table.vertLines[i], table.yText-table.f1.ascent)
		}
		table.page.SetPenColorRGB(original[0], original[1], original[2])
		table.page.AddEMC()
	}

	table.page = NewPageDetached(table.pdf, table.pageSize)
	table.pages = append(table.pages, table.page)
	table.page.SetPenWidth(0.0)
	table.yText = table.y1 + table.f1.ascent

	// Highlight row and draw horizontal line
	table.page.AddArtifactBMC()
	table.DrawHighlight(table.page, table.highlightColor, table.f1)
	table.highlightRow = false
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(float32(table.vertLines[0]), table.yText-table.f1.ascent,
		float32(table.vertLines[len(table.headerRow)]), table.yText-table.f1.ascent)
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()

	rowText := getRowText(table.headerRow)
	table.page.AddBMC("P", table.language, rowText, rowText)
	table.page.SetTextFont(table.f1)
	table.page.SetBrushColor(color)
	xText := float32(0.0)
	xText2 := float32(0.0)
	for i, text := range table.headerRow {
		xText = float32(table.vertLines[i])
		xText2 = float32(table.vertLines[i+1])
		table.page.BeginText()
		if table.align == nil || table.align[i] == 0 { // Align Left
			table.page.SetTextLocation((xText + table.padding), table.yText)
		} else if table.align[i] == 1 { // Align Right
			table.page.SetTextLocation((xText2-table.padding)-table.f1.StringWidth(nil, text), table.yText)
		}
		table.page.DrawText(text)
		table.page.EndText()
	}
	table.page.AddEMC()
	table.yText += table.f1.descent + table.f2.ascent
}

func (table *BigTable) DrawOn(row []string, markerColor int32) {
	if len(row) > len(table.headerRow) {
		// Prevent crashes when some data rows have extra fields!
		// The application should check for this and handle it the right way.
		return
	}

	// Highlight row and draw horizontal line
	table.page.AddArtifactBMC()
	if table.highlightRow {
		table.DrawHighlight(table.page, table.highlightColor, table.f2)
		table.highlightRow = false
	} else {
		table.highlightRow = true
	}
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(float32(table.vertLines[0]), table.yText-table.f2.ascent,
		float32(table.vertLines[len(table.headerRow)]), table.yText-table.f2.ascent)
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()

	rowText := getRowText(row)
	table.page.AddBMC("P", table.language, rowText, rowText)
	table.page.SetPenWidth(0.0)
	table.page.SetTextFont(table.f2)
	table.page.SetBrushColor(color.Black)
	xText := float32(0.0)
	xText2 := float32(0.0)
	for i, text := range row {
		xText = float32(table.vertLines[i])
		xText2 = float32(table.vertLines[i+1])
		table.page.BeginText()
		if table.align == nil || table.align[i] == 0 { // Align Left
			table.page.SetTextLocation((xText + table.padding), table.yText)
		} else if table.align[i] == 1 { // Align Right
			table.page.SetTextLocation((xText2-table.padding)-table.f2.StringWidth(nil, text), table.yText)
		}
		table.page.DrawText(text)
		table.page.EndText()
	}
	table.page.AddEMC()
	if markerColor != color.Black {
		table.page.AddArtifactBMC()
		originalColor := table.page.GetPenColor()
		table.page.SetPenColor(markerColor)
		table.page.SetPenWidth(3.0)
		table.page.DrawLine(table.vertLines[0]-2.0, table.yText-table.f2.ascent, table.vertLines[0]-2.0, table.yText+table.f2.descent)
		table.page.DrawLine(xText2+2.0, table.yText-table.f2.ascent, xText2+2.0, table.yText+table.f2.descent)
		table.page.SetPenColorRGB(originalColor[0], originalColor[1], originalColor[2])
		table.page.SetPenWidth(0.0)
		table.page.AddEMC()
	}
	table.yText += table.f2.descent + table.f2.ascent
	if table.yText+table.f2.descent > (table.page.height - table.bottomMargin) {
		table.NewTablePage(row, color.Black)
	}
}

func (table *BigTable) Complete() {
	table.page.AddArtifactBMC()
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(float32(table.vertLines[0]), table.yText-table.f2.ascent,
		float32(table.vertLines[len(table.vertLines)-1]), table.yText-table.f2.ascent)
	// Draw the vertical lines
	for i := 0; i <= len(table.headerRow); i++ {
		table.page.DrawLine(table.vertLines[i], table.y1, table.vertLines[i], table.yText-table.f1.ascent)
	}
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()
}

func (table *BigTable) DrawHighlight(page *Page, color int32, font *Font) {
	original := page.GetBrushColor()
	page.SetBrushColor(color)
	page.MoveTo(float32(table.vertLines[0]), table.yText-font.ascent)
	page.LineTo(float32(table.vertLines[len(table.headerRow)]), table.yText-font.ascent)
	page.LineTo(float32(table.vertLines[len(table.headerRow)]), table.yText+font.descent)
	page.LineTo(float32(table.vertLines[0]), table.yText+font.descent)
	page.FillPath()
	page.SetBrushColorRGB(original[0], original[1], original[2])
}

func getRowText(row []string) string {
	var buf strings.Builder
	for _, field := range row {
		buf.WriteString(field)
		buf.WriteString(" ")
	}
	return buf.String()
}

func (table *BigTable) GetColumnWidths(fileName string) []float32 {
	widths := make([]float32, 0)
	table.align = make([]int, 0)
	readFile, err := os.Open(fileName)
	if err != nil {
		fmt.Println(err)
	}
	fileScanner := bufio.NewScanner(readFile)
	fileScanner.Split(bufio.ScanLines)
	rowNumber := 0
	for fileScanner.Scan() {
		line := fileScanner.Text()
		fields := strings.Split(line, ",")
		for i := 0; i < len(fields); i++ {
			field := fields[i]
			width := table.f1.StringWidth(nil, field)
			if rowNumber == 0 { // Header Row
				widths = append(widths, width)
			} else {
				if i < len(widths) && width > widths[i] {
					widths[i] = width
				}
			}
		}
		if rowNumber == 1 { // First Data Row
			for _, field := range fields {
				table.align = append(table.align, table.getAlignment(field))
			}
		}
		rowNumber++
	}
	readFile.Close()
	return widths
}

func (table *BigTable) getAlignment(str string) int {
	var buf strings.Builder
	if strings.HasPrefix(str, "(") && strings.HasSuffix(str, ")") {
		str = str[1 : len(str)-1]
	}
	for _, ch := range str {
		if ch != '.' && ch != ',' && ch != '\'' {
			buf.WriteRune(ch)
		}
	}
	_, err := strconv.ParseFloat(str, 64)
	if err == nil {
		return 1 // Align Right
	}
	return 0 // Align Left
}
