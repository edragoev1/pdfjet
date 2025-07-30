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
 * BigTable handles efficient rendering of large datasets to PDF with:
 * - Automatic pagination
 * - Customizable formatting (fonts, colors, alignment)
 * - Alternating row highlighting
 * - Accessibility tagging
 * - Stream-based processing (low memory footprint)
 */
type BigTable struct {
	pdf              *PDF       // Parent PDF document
	page             *Page      // Current working page
	pageSize         [2]float32 // Page dimensions [width, height]
	f1               *Font      // Primary font (typically for headers)
	f2               *Font      // Secondary font (for data rows)
	x                float32    // Current X position
	y                float32    // Current Y position (baseline)
	yText            float32    // Text baseline Y coordinate
	pages            []*Page    // All generated pages
	alignment        []int      // Column alignments (0=left, 1=right)
	vertLines        []float32  // X positions of vertical grid lines
	headerRow        []string   // Column headers
	bottomMargin     float32    // Bottom page margin (triggers new page)
	padding          float32    // Cell padding
	language         string     // Language tag for accessibility
	highlight        bool       // Row highlight toggle
	highlightColor   int32      // Background color for highlighted rows
	penColor         int32      // Color for grid lines
	fileName         string     // Source data file path
	delimiter        string
	widths           []float32 // Calculated column widths
	numberOfColumns  int       // Total column count
	columnsToDisplay []int     // Column indices to render (optional filter)
}

/**
 * NewBigTable initializes a table renderer with essential settings:
 * pdf: The parent PDF document
 * f1: Font for header row
 * f2: Font for data rows
 * pageSize: [width, height] of the PDF page
 */
func NewBigTable(pdf *PDF, f1, f2 *Font, pageSize [2]float32) *BigTable {
	table := new(BigTable)
	table.pdf = pdf
	table.pageSize = pageSize
	table.f1 = f1
	table.f2 = f2
	table.pages = make([]*Page, 0)
	table.bottomMargin = 15.0       // Default 15-unit bottom margin
	table.highlightColor = 0xF0F0F0 // Light gray highlight
	table.penColor = 0xB0B0B0       // Medium gray grid lines
	table.padding = 2.0             // 2-unit cell padding
	table.numberOfColumns = 10      // Default column count
	return table
}

/**
 * SetLocation positions the table on the page and adjusts vertical lines
 * x: Starting X coordinate
 * y: Starting Y coordinate
 */
func (table *BigTable) SetLocation(x, y float32) {
	// Adjust all vertical line positions relative to new X
	for i := 0; i < table.numberOfColumns; i++ {
		table.vertLines[i] += x
	}
	table.y = y // Set new baseline Y
}

/**
 * SetNumberOfColumns updates the expected column count.
 * Must be called before loading data.
 */
func (table *BigTable) SetNumberOfColumns(numberOfColumns int) {
	table.numberOfColumns = numberOfColumns
}

/**
 * SetColumnsToDisplay filters columns by index (zero-based).
 * If not set, all columns are displayed.
 */
func (table *BigTable) SetColumnsToDisplay(columnsToDisplay []int) {
	table.columnsToDisplay = columnsToDisplay
}

/**
 * SetTextAlignment defines per-column text alignment:
 * align: Slice where 0=left, 1=right alignment
 */
func (table *BigTable) SetTextAlignment(align []int) {
	table.alignment = align
}

/**
 * SetBottomMargin sets the minimum margin before page break.
 */
func (table *BigTable) SetBottomMargin(bottomMargin float32) {
	table.bottomMargin = bottomMargin
}

/**
 * SetLanguage sets the language tag for PDF accessibility.
 */
func (table *BigTable) SetLanguage(language string) {
	table.language = language
}

/**
 * GetPages returns all generated pages for multi-page tables.
 */
func (table *BigTable) GetPages() []*Page {
	return table.pages
}

/**
 * DrawRow renders a row, automatically handling:
 * - Header row detection
 * - Pagination
 * - Highlighting
 * row: Data cells to render
 * markerColor: Optional border color for special rows
 */
func (table *BigTable) DrawRow(row []string, markerColor int32) {
	if table.headerRow == nil {
		table.headerRow = row
		table.newPage(color.Black) // Draw header immediately
	} else {
		table.drawOn(row, markerColor) // Render data row
	}
}

/**
 * newPage creates a fresh page and renders the header row.
 * color: Text color for the header
 */
func (table *BigTable) newPage(color int32) {
	// Finalize current page if exists
	if table.page != nil {
		table.page.AddArtifactBMC() // Begin accessibility tag
		original := table.page.GetPenColor()
		table.page.SetPenColor(table.penColor)

		// Draw horizontal line below header
		table.page.DrawLine(
			float32(table.vertLines[0]),
			table.yText-table.f1.ascent,
			float32(table.vertLines[table.numberOfColumns]),
			table.yText-table.f1.ascent)

		// Draw vertical grid lines
		for i := 0; i <= table.numberOfColumns; i++ {
			table.page.DrawLine(
				table.vertLines[i],
				table.y,
				table.vertLines[i],
				table.yText-table.f1.ascent)
		}
		table.page.SetPenColorWithFloat32Array(original)
		table.page.AddEMC() // End accessibility tag
	}

	// Initialize new page
	table.page = NewPageDetached(table.pdf, table.pageSize)
	table.pages = append(table.pages, table.page)
	table.page.SetPenWidth(0.0)
	table.yText = table.y + table.f1.ascent // Set text baseline

	// Render header row
	table.page.AddArtifactBMC()
	table.highlightRow(table.page, table.highlightColor, table.f1)
	table.highlight = false // Reset highlight for data rows

	// Draw header grid lines
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f1.ascent,
		float32(table.vertLines[len(table.headerRow)]),
		table.yText-table.f1.ascent)
	table.page.SetPenColorWithFloat32Array(original)
	table.page.AddEMC()

	// Render header text
	rowText := getRowText(table.headerRow)
	table.page.AddBMC("P", table.language, rowText, rowText) // TODO:
	table.page.SetTextFont(table.f1)
	table.page.SetBrushColor(color)
	for i := 0; i < table.numberOfColumns; i++ {
		text := table.headerRow[i]
		xText1 := float32(table.vertLines[i])
		xText2 := float32(table.vertLines[i+1])

		table.page.BeginText()
		if table.alignment == nil || table.alignment[i] == 0 { // Left align
			table.page.SetTextLocation(
				(xText1 + table.padding), table.yText)
		} else if table.alignment[i] == 1 { // Right align
			table.page.SetTextLocation(
				(xText2-table.padding)-table.f1.StringWidth(nil, text), table.yText)
		}
		table.page.DrawText(text)
		table.page.EndText()
	}
	table.page.AddEMC()
	table.yText += (table.f2.ascent - table.f1.descent) // Move baseline down
}

/**
 * drawOn renders a data row with:
 * - Alternating highlights
 * - Cell alignment
 * - Pagination checks
 */
func (table *BigTable) drawOn(row []string, markerColor int32) {
	// Validate row length
	if len(row) > len(table.headerRow) {
		return // Silently skip malformed rows
	}

	// Highlight and draw grid lines
	table.page.AddArtifactBMC()
	if table.highlight {
		table.highlightRow(table.page, table.highlightColor, table.f2)
	}
	table.highlight = !table.highlight // Toggle for next row

	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f2.ascent,
		float32(table.vertLines[table.numberOfColumns]),
		table.yText-table.f2.ascent)
	table.page.SetPenColorWithFloat32Array(original)
	table.page.AddEMC()

	// Render cell content
	rowText := getRowText(row)
	table.page.AddBMC("P", table.language, rowText, rowText)
	table.page.SetPenWidth(0.0)
	table.page.SetTextFont(table.f2)
	table.page.SetBrushColor(color.Black)
	xText2 := float32(0.0)

	for i := 0; i < table.numberOfColumns; i++ {
		text := row[i]
		xText1 := float32(table.vertLines[i])
		xText2 = float32(table.vertLines[i+1])
		table.page.BeginText()
		if table.alignment == nil || table.alignment[i] == 0 { // Left align
			table.page.SetTextLocation(
				(xText1 + table.padding), table.yText)
		} else if table.alignment[i] == 1 { // Right align
			table.page.SetTextLocation(
				(xText2-table.padding)-table.f2.StringWidth(nil, text), table.yText)
		}
		table.page.DrawText(text)
		table.page.EndText()
	}
	table.page.AddEMC()

	// Draw special markers if requested
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
		table.page.SetPenColorWithFloat32Array(originalColor)
		table.page.SetPenWidth(0.0)
		table.page.AddEMC()
	}

	// Advance to next line and check pagination
	table.yText += table.f2.ascent - table.f2.descent
	if table.yText > table.page.height-table.bottomMargin {
		table.newPage(color.Black)
	}
}

/**
 * highlightRow fills a row's background with highlight color
 */
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

/**
 * getRowText concatenates cells for accessibility tagging
 */
func getRowText(row []string) string {
	var buf strings.Builder
	for _, field := range row {
		buf.WriteString(field)
		buf.WriteString(" ")
	}
	return buf.String()
}

/**
 * SetTableData analyzes the input file to:
 * 1. Calculate column widths
 * 2. Determine alignments (numeric=right)
 * 3. Precompute vertical line positions
 */
func (table *BigTable) SetTableData(fileName, delimiter string) {
	table.fileName = fileName
	table.delimiter = delimiter
	table.widths = make([]float32, 0)
	table.alignment = make([]int, 0)

	file, err := os.Open(fileName)
	if err != nil {
		fmt.Println(err)
		return
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	rowNumber := 0
	for scanner.Scan() {
		line := scanner.Text()
		fields := strings.Split(line, delimiter)
		for i := 0; i < table.numberOfColumns; i++ {
			width := table.f1.StringWidth(nil, fields[i])
			if rowNumber == 0 { // Header row
				table.widths = append(table.widths, width+2*table.padding)
			} else { // Data rows
				if i < table.numberOfColumns && (width+2*table.padding > table.widths[i]) {
					table.widths[i] = width + 2*table.padding
				}
			}
		}
		if rowNumber == 1 { // Determine alignment from first data row
			for _, field := range fields {
				table.alignment = append(table.alignment, table.getAlignment(field))
			}
		}
		rowNumber++
	}

	// Precompute vertical line positions
	table.vertLines = make([]float32, 0)
	table.vertLines = append(table.vertLines, table.x)
	vertLineX := table.x
	for i := 0; i < table.numberOfColumns; i++ {
		vertLineX += table.widths[i]
		table.vertLines = append(table.vertLines, vertLineX)
	}
}

/**
 * getAlignment detects numeric content for right alignment
 */
func (table *BigTable) getAlignment(str string) int {
	var buf strings.Builder
	// Clean numeric formatting
	if strings.HasPrefix(str, "(") && strings.HasSuffix(str, ")") {
		str = str[1 : len(str)-1]
	}
	for _, ch := range str {
		if ch != '.' && ch != ',' && ch != '\'' {
			buf.WriteRune(ch)
		}
	}
	// Test if numeric
	_, err := strconv.ParseFloat(str, 64)
	if err == nil {
		return 1 // Right-align numbers
	}
	return 0 // Left-align text
}

/**
 * Complete finalizes the table by:
 * 1. Streaming all data rows from file
 * 2. Drawing final grid lines
 */
func (table *BigTable) Complete() {
	file, err := os.Open(table.fileName)
	if err != nil {
		fmt.Println(err)
		return
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	firstRow := true

	for scanner.Scan() {
		if firstRow { // Skip header (already processed)
			firstRow = false
			continue
		}
		line := scanner.Text()
		fields := strings.Split(line, table.delimiter)
		row := make([]string, 0)
		for i := 0; i < table.numberOfColumns; i++ {
			row = append(row, fields[i])
		}
		table.DrawRow(row, color.Black)
	}

	// Draw final grid lines
	table.page.AddArtifactBMC()
	original := table.page.GetPenColor()
	table.page.SetPenColor(table.penColor)
	table.page.DrawLine(
		float32(table.vertLines[0]),
		table.yText-table.f2.ascent,
		float32(table.vertLines[table.numberOfColumns]),
		table.yText-table.f2.ascent)
	// Vertical lines
	for i := 0; i <= table.numberOfColumns; i++ {
		table.page.DrawLine(
			table.vertLines[i],
			table.y,
			table.vertLines[i],
			table.yText-table.f1.ascent)
	}
	table.page.SetPenColorWithFloat32Array(original)
	table.page.AddEMC()
}
