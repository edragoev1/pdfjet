package pdfjet

/**
 * bigtable.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"bufio"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
)

// BigTable represents a table for handling large amounts of data in PDF
type BigTable struct {
	pdf             *PDF
	f1              *Font
	f2              *Font
	pageSize        [2]float32
	y               float32
	yText           float32
	pages           []*Page
	page            *Page
	widths          []float32
	headerFields    []string
	alignment       map[int]alignment.Alignment
	vertLines       []float32
	bottomMargin    float32
	padding         float32
	language        string
	highlight       bool
	highlightColor  int32
	penColor        int32
	fileName        string
	delimiter       string
	numberOfColumns int
	startNewPage    bool
}

// NewBigTable creates a new BigTable instance
func NewBigTable(pdf *PDF, f1 *Font, f2 *Font, pageSize [2]float32) *BigTable {
	return &BigTable{
		pdf:            pdf,
		f1:             f1,
		f2:             f2,
		pageSize:       pageSize,
		pages:          make([]*Page, 0),
		bottomMargin:   20.0,
		padding:        2.0,
		language:       "en-US",
		highlight:      true,
		highlightColor: 0xF0F0F0,
		penColor:       0xB0B0B0,
		startNewPage:   true,
	}
}

// SetLocation sets the table location
func (bt *BigTable) SetLocation(x, y float32) {
	for i := 0; i <= bt.numberOfColumns; i++ {
		bt.vertLines[i] += x
	}
	bt.y = y
}

// SetNumberOfColumns sets the number of columns
func (bt *BigTable) SetNumberOfColumns(numberOfColumns int) {
	bt.numberOfColumns = numberOfColumns
}

// SetTextAlignment sets text alignment for a column
func (bt *BigTable) SetTextAlignment(column int, alignment alignment.Alignment) {
	bt.alignment[column] = alignment
}

// SetBottomMargin sets the bottom margin
func (bt *BigTable) SetBottomMargin(bottomMargin float32) {
	bt.bottomMargin = bottomMargin
}

// SetLanguage sets the language
func (bt *BigTable) SetLanguage(language string) {
	bt.language = language
}

// GetPages returns the generated pages
func (bt *BigTable) GetPages() []*Page {
	return bt.pages
}

func (bt *BigTable) drawTextAndLine(fields []string) error {
	if bt.page == nil { // First page
		bt.page = NewPageDetached(bt.pdf, bt.pageSize)
		bt.pages = append(bt.pages, bt.page)
		bt.page.SetPenWidth(0.0)
		bt.yText = bt.y + bt.f1.ascent
		bt.highlight = true
		bt.drawFieldsAndLine(bt.headerFields, bt.f1)
		bt.yText += bt.f1.descent + bt.f2.ascent
		bt.startNewPage = false
		return nil
	}

	if bt.startNewPage { // New page
		bt.page = NewPageDetached(bt.pdf, bt.pageSize)
		bt.pages = append(bt.pages, bt.page)
		bt.page.SetPenWidth(0.0)
		bt.yText = bt.y + bt.f1.ascent
		bt.highlight = true
		bt.drawFieldsAndLine(bt.headerFields, bt.f1)
		bt.yText += bt.f1.descent + bt.f2.ascent
		bt.startNewPage = false
	}

	bt.drawFieldsAndLine(fields, bt.f2)
	bt.yText += bt.f2.descent + bt.f2.ascent
	if bt.yText > (bt.page.height - bt.bottomMargin) {
		bt.drawTheVerticalLines()
		bt.startNewPage = true
	}

	return nil
}

func (bt *BigTable) drawFieldsAndLine(fields []string, font *Font) {
	if bt.highlight {
		bt.highlightRow(bt.page, font, bt.highlightColor)
		bt.highlight = false
	} else {
		bt.highlight = true
	}

	original := bt.page.GetPenColorRGB()
	bt.page.SetPenColor(bt.penColor)
	bt.page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
	bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
	bt.page.StrokePath()
	bt.page.SetPenColorRGB(original)
	bt.page.SetPenWidth(0.0)
	bt.page.SetBrushColor(color.Black)

	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[i]
		xText := bt.vertLines[i] + bt.padding
		if bt.alignment[i] == alignment.Right {
			xText = (bt.vertLines[i+1] - bt.padding) - font.StringWidth(font, font.size, text)
		}
		bt.page.DrawTextLine(font, text, xText, bt.yText)
	}
}

func (bt *BigTable) highlightRow(page *Page, font *Font, color int32) {
	original := page.GetBrushColorRGB()
	page.SetBrushColor(color)
	page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText+font.descent)
	page.LineTo(bt.vertLines[0], bt.yText+font.descent)
	page.FillPath()
	page.SetBrushColorRGB(original)
}

func (bt *BigTable) drawTheVerticalLines() {
	// bt.page.AddArtifactBMC()
	original := bt.page.GetPenColorRGB()
	bt.page.SetPenColor(bt.penColor)
	for i := 0; i <= bt.numberOfColumns; i++ {
		bt.page.DrawLine(
			bt.vertLines[i],
			bt.y,
			bt.vertLines[i],
			bt.yText-bt.f2.ascent)
	}
	bt.page.MoveTo(bt.vertLines[0], bt.yText-bt.f2.ascent)
	bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-bt.f2.ascent)
	bt.page.StrokePath()
	bt.page.SetPenColorRGB(original)
	// bt.page.AddEMC()
}

func (bt *BigTable) getAlignment(str string) alignment.Alignment {
	var buf strings.Builder
	if strings.HasPrefix(str, "(") && strings.HasSuffix(str, ")") {
		str = str[1 : len(str)-1]
	}
	for i := 0; i < len(str); i++ {
		ch := str[i]
		if ch != '.' && ch != ',' && ch != '\'' {
			buf.WriteByte(ch)
		}
	}
	if _, err := strconv.ParseFloat(buf.String(), 64); err == nil {
		return alignment.Right
	}
	return alignment.Left
}

// SetTableData sets the table data from file
func (bt *BigTable) SetTableData(fileName, delimiter string) error {
	bt.fileName = fileName
	bt.delimiter = delimiter
	bt.vertLines = make([]float32, bt.numberOfColumns+1)
	bt.headerFields = make([]string, bt.numberOfColumns)
	bt.widths = make([]float32, bt.numberOfColumns)
	bt.alignment = make(map[int]alignment.Alignment)

	file, err := os.Open(fileName)
	if err != nil {
		return err
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("failed to close file: " + err.Error())
		}
	}(file)

	scanner := bufio.NewScanner(file)
	rowNumber := 0
	for scanner.Scan() {
		line := scanner.Text()
		fields := strings.Split(line, bt.delimiter)
		if len(fields) < bt.numberOfColumns {
			continue
		}
		if rowNumber == 0 {
			for i := 0; i < bt.numberOfColumns; i++ {
				bt.headerFields[i] = fields[i]
			}
		}
		if rowNumber == 1 {
			for i := 0; i < bt.numberOfColumns; i++ {
				bt.alignment[i] = bt.getAlignment(fields[i])
			}
		}
		for i := 0; i < bt.numberOfColumns; i++ {
			field := fields[i]
			width := bt.f1.StringWidth(nil, bt.f1.size, field) + 2*bt.padding
			if width > bt.widths[i] {
				bt.widths[i] = width
			}
		}
		rowNumber++
	}

	if err := scanner.Err(); err != nil {
		return err
	}

	bt.vertLines[0] = 0.0
	vertLineX := float32(0.0)
	for i := 0; i < len(bt.widths); i++ {
		vertLineX += bt.widths[i]
		bt.vertLines[i+1] = vertLineX
	}

	return nil
}

// Complete finishes the table and writes all data
func (bt *BigTable) Complete() error {
	file, err := os.Open(bt.fileName)
	if err != nil {
		return err
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("failed to close file: " + err.Error())
		}
	}(file)

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		fields := strings.Split(line, bt.delimiter)
		if len(fields) < bt.numberOfColumns {
			continue
		}
		if err := bt.drawTextAndLine(fields); err != nil {
			return err
		}
	}

	if err := scanner.Err(); err != nil {
		return err
	}

	bt.drawTheVerticalLines()
	return nil
}
