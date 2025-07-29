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
	pdf              *PDF
	page             *Page
	pageSize         [2]float32
	f1               *Font
	f2               *Font
	x                float32
	y                float32
	yText            float32
	pages            []*Page
	alignment        []int
	vertLines        []float32
	headerRow        []string
	bottomMargin     float32
	padding          float32
	language         string
	highlight        bool
	highlightColor   int32
	penColor         int32
	fileName         string
	widths           []float32
	numberOfColumns  int
	columnsToDisplay []int
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
	table.numberOfColumns = 10
	return table
}

func (table *BigTable) SetLocation(x, y float32) {
	for i := 0; i < table.numberOfColumns; i++ {
		table.vertLines[i] += x
	}
	table.y = y
}

func (table *BigTable) SetNumberOfColumns(numberOfColumns int) {
	table.numberOfColumns = numberOfColumns
}

func (table *BigTable) SetColumnsToDisplay(columnsToDisplay []int) {
	table.columnsToDisplay = columnsToDisplay
}

func (table *BigTable) SetTextAlignment(align []int) {
	table.alignment = align
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

func (table *BigTable) DrawRow(row []string, markerColor int32) {
	if table.headerRow == nil {
		table.headerRow = row
		table.newPage(color.Black) // Draws the header row automatically
	} else {
		table.drawOn(row, markerColor)
	}
}

func (table *BigTable) newPage(color int32) {
	if table.page != nil {
		table.page.AddArtifactBMC()
		original := table.page.GetPenColor()
		table.page.SetPenColor(table.penColor)
		table.page.DrawLine(
			float32(table.vertLines[0]),
			table.yText-table.f1.ascent,
			float32(table.vertLines[table.numberOfColumns]),
			table.yText-table.f1.ascent)
		// Draw the vertical lines
		for i := 0; i <= table.numberOfColumns; i++ {
			table.page.DrawLine(
				table.vertLines[i],
				table.y,
				table.vertLines[i],
				table.yText-table.f1.ascent)
		}
		table.page.SetPenColorRGB(original[0], original[1], original[2])
		table.page.AddEMC()
	}

	table.page = NewPageDetached(table.pdf, table.pageSize)
	table.pages = append(table.pages, table.page)
	table.page.SetPenWidth(0.0)
	table.yText = table.y + table.f1.ascent

	table.page.AddArtifactBMC()
	// Highlight the row
	table.highlightRow(table.page, table.highlightColor, table.f1)
	table.highlight = false
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	//Draw the horizontal line
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f1.ascent,
		float32(table.vertLines[len(table.headerRow)]),
		table.yText-table.f1.ascent)
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()

	rowText := getRowText(table.headerRow)
	table.page.AddBMC("P", table.language, rowText, rowText)
	table.page.SetTextFont(table.f1)
	table.page.SetBrushColor(color)
	for i := 0; i < table.numberOfColumns; i++ {
		text := table.headerRow[i]
		xText := float32(table.vertLines[i])
		xText2 := float32(table.vertLines[i+1])
		table.page.BeginText()
		if table.alignment == nil || table.alignment[i] == 0 { // Align Left
			table.page.SetTextLocation(
				(xText + table.padding),
				table.yText)
		} else if table.alignment[i] == 1 { // Align Right
			table.page.SetTextLocation(
				(xText2-table.padding)-table.f1.StringWidth(nil, text),
				table.yText)
		}
		table.page.DrawText(text)
		table.page.EndText()
	}
	table.page.AddEMC()
	table.yText += (table.f2.ascent - table.f1.descent)
}

func (table *BigTable) drawOn(row []string, markerColor int32) {
	if len(row) > len(table.headerRow) {
		// Prevent crashes when some data rows have extra fields!
		// The application should check for this and handle it the right way.
		return
	}

	// Highlight row and draw horizontal line
	table.page.AddArtifactBMC()
	if table.highlight {
		table.highlightRow(table.page, table.highlightColor, table.f2)
		table.highlight = false
	} else {
		table.highlight = true
	}
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f2.ascent,
		float32(table.vertLines[table.numberOfColumns]),
		table.yText-table.f2.ascent)
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()

	rowText := getRowText(row)
	table.page.AddBMC("P", table.language, rowText, rowText)
	table.page.SetPenWidth(0.0)
	table.page.SetTextFont(table.f2)
	table.page.SetBrushColor(color.Black)
	xText2 := float32(0.0)
	for i := 0; i < table.numberOfColumns; i++ {
		text := row[i]
		xText := float32(table.vertLines[i])
		xText2 = float32(table.vertLines[i+1])
		table.page.BeginText()
		if table.alignment == nil || table.alignment[i] == 0 { // Align Left
			table.page.SetTextLocation(
				(xText + table.padding),
				table.yText)
		} else if table.alignment[i] == 1 { // Align Right
			table.page.SetTextLocation(
				(xText2-table.padding)-table.f2.StringWidth(nil, text),
				table.yText)
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
		table.page.DrawLine(
			table.vertLines[0]-table.padding,
			table.yText-table.f2.ascent,
			table.vertLines[0]-table.padding,
			table.yText-table.f2.descent)
		table.page.DrawLine(
			xText2+table.padding,
			table.yText-table.f2.ascent,
			xText2+table.padding,
			table.yText-table.f2.descent)
		table.page.SetPenColorRGB(originalColor[0], originalColor[1], originalColor[2])
		table.page.SetPenWidth(0.0)
		table.page.AddEMC()
	}
	table.yText += table.f2.ascent - table.f2.descent
	if table.yText+table.f2.descent > table.page.height-table.bottomMargin {
		table.newPage(color.Black)
	}
}

func (table *BigTable) highlightRow(page *Page, color int32, font *Font) {
	original := page.GetBrushColor()
	page.SetBrushColor(color)
	page.MoveTo(float32(table.vertLines[0]), table.yText-font.ascent)
	page.LineTo(float32(table.vertLines[table.numberOfColumns]), table.yText-font.ascent)
	page.LineTo(float32(table.vertLines[table.numberOfColumns]), table.yText-font.descent)
	page.LineTo(float32(table.vertLines[0]), table.yText-font.descent)
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

func (table *BigTable) SetTableData(fileName string, delimiter rune) {
	table.fileName = fileName
	table.widths = make([]float32, 0)
	table.alignment = make([]int, 0)
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
		for i := 0; i < table.numberOfColumns; i++ {
			width := table.f1.StringWidth(nil, fields[i])
			if rowNumber == 0 { // Header Row
				table.widths = append(table.widths, width+2*table.padding)
			} else {
				if (i < len(table.widths)) && (width+2*table.padding > table.widths[i]) {
					table.widths[i] = width + 2*table.padding
				}
			}
		}
		if rowNumber == 1 { // First Data Row
			for _, field := range fields {
				table.alignment = append(table.alignment, table.getAlignment(field))
			}
		}
		rowNumber++
	}
	readFile.Close()

	table.vertLines = make([]float32, 0)
	table.vertLines = append(table.vertLines, table.x)
	vertLineX := table.x
	for i := 0; i < table.numberOfColumns; i++ {
		vertLineX += table.widths[i]
		table.vertLines = append(table.vertLines, vertLineX)
	}
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

func (table *BigTable) Complete() {
	readFile, err := os.Open(table.fileName)
	if err != nil {
		fmt.Println(err)
	}
	fileScanner := bufio.NewScanner(readFile)
	fileScanner.Split(bufio.ScanLines)
	for fileScanner.Scan() {
		line := fileScanner.Text()
		fields := strings.Split(line, ",")
		row := make([]string, 0)
		for i := 0; i < table.numberOfColumns; i++ {
			row = append(row, fields[i])
		}
		table.DrawRow(row, color.Black)
	}
	readFile.Close()

	table.page.AddArtifactBMC()
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f2.ascent,
		float32(table.vertLines[table.numberOfColumns]),
		table.yText-table.f2.ascent)
	// Draw the vertical lines
	for i := 0; i <= table.numberOfColumns; i++ {
		table.page.DrawLine(
			table.vertLines[i],
			table.y,
			table.vertLines[i],
			table.yText-table.f1.ascent)
	}
	table.page.SetPenColorRGB(original[0], original[1], original[2])
	table.page.AddEMC()
}
