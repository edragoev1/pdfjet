// main.go
//
// Example_09: XY Chart with trend line and data table

package main

import (
	"bufio"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example09 creates an XY chart with world communications data.
func Example09() error {
	pdf := pdfjet.NewPDFFile("Example_09.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(8.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	chart := pdfjet.NewChart(f1, f2)
	chartData, err := getData("data/world-communications.txt", "|")
	if err != nil {
		return err
	}
	chart.SetData(chartData)
	chart.SetLocation(70.0, 50.0)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("World View - Communications")
	chart.SetXAxisTitle("Cell phones per capita")
	chart.SetYAxisTitle("Internet users % of the population")
	addTrendLine(chart)
	chart.DrawOn(page)

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	err = addTableToChart(page, chart, f1, f2)
	if err != nil {
		return err
	}

	pdf.Complete()
	return nil
}

// addTrendLine calculates and adds a trend line to the chart.
func addTrendLine(chart *pdfjet.Chart) {
	points := chart.GetData()[0]

	m := chart.Slope(points)
	b := chart.Intercept(points, m)

	trendLine := make([]*pdfjet.Point, 0)

	x := 0.0
	y := m*float32(x) + b
	p1 := pdfjet.NewPoint(float32(x), y)
	p1.SetDrawPath()
	p1.SetStrokeColor(color.Blue)
	p1.SetShape(shape.Invisible)

	x = 1.5
	y = m*float32(x) + b
	p2 := pdfjet.NewPoint(float32(x), y)
	p2.SetShape(shape.Invisible)

	trendLine = append(trendLine, p1)
	trendLine = append(trendLine, p2)

	chartData := chart.GetData()
	chartData = append(chartData, trendLine)
	chart.SetData(chartData)
}

// addTableToChart creates and draws a table of the chart data.
func addTableToChart(page *pdfjet.Page, chart *pdfjet.Chart, f1, f2 *pdfjet.Font) error {
	table := pdfjet.NewTable()
	tableData := make([][]*pdfjet.Cell, 0)
	points := chart.GetData()[0]

	for i := 0; i < len(points); i++ {
		point := points[i]
		if point.GetShape() != shape.Circle {
			tableRow := make([]*pdfjet.Cell, 0)

			point.SetRadius(2.0)
			point.SetAlignment(alignment.Left)

			cell := pdfjet.NewCell(f2, "")
			cell.SetPoint(point)
			tableRow = append(tableRow, cell)

			cell = pdfjet.NewCell(f1, point.GetText())
			tableRow = append(tableRow, cell)

			cell = pdfjet.NewCell(f2, point.GetURIAction())
			tableRow = append(tableRow, cell)

			tableData = append(tableData, tableRow)
		}
	}

	table.SetData(tableData, 0)
	table.SetColumnWidths()
	table.SetCellBordersWidth(0.2)
	table.SetLocation(70.0, 360.0)
	table.SetColumnWidth(0, 9.0)
	table.DrawOn(page)

	return nil
}

// getData reads chart data from a file.
// format: country|population|...|cellphones|...|internet
func getData(fileName, delimiter string) ([][]*pdfjet.Point, error) {
	chartData := make([][]*pdfjet.Point, 0)
	points := make([]*pdfjet.Point, 0)

	file, err := os.Open(fileName)
	if err != nil {
		return nil, err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()

		var cols []string
		if delimiter == "|" {
			cols = strings.Split(line, "|")
		} else if delimiter == "\t" {
			cols = strings.Split(line, "\t")
		} else {
			return nil, nil
		}

		point := pdfjet.NewPoint(0, 0)

		if len(cols) < 8 {
			continue
		}

		populationStr := strings.TrimSpace(cols[1])
		populationStr = strings.ReplaceAll(populationStr, ",", "")
		population, err := strconv.ParseFloat(populationStr, 32)
		if err != nil {
			continue
		}

		countryName := strings.TrimSpace(cols[0])
		point.SetText(countryName)

		urlName := countryName
		urlName = strings.ReplaceAll(urlName, " ", "_")
		urlName = strings.ReplaceAll(urlName, "'", "_")
		urlName = strings.ReplaceAll(urlName, ",", "_")
		urlName = strings.ReplaceAll(urlName, "(", "_")
		urlName = strings.ReplaceAll(urlName, ")", "_")
		point.SetURIAction("http://pdfjet.com/country/" + urlName + ".txt")

		cellPhonesStr := strings.TrimSpace(cols[5])
		cellPhonesStr = strings.ReplaceAll(cellPhonesStr, ",", "")
		cellPhones, err := strconv.ParseFloat(cellPhonesStr, 32)
		if err != nil {
			continue
		}
		point.SetX(float32(cellPhones / population))

		internetStr := strings.TrimSpace(cols[7])
		internetStr = strings.ReplaceAll(internetStr, ",", "")
		internet, err := strconv.ParseFloat(internetStr, 32)
		if err != nil {
			continue
		}
		point.SetY(float32(internet / population * 100))

		point.SetRadius(2.0)

		if point.GetX() > 1.25 {
			point.SetShape(shape.RightArrow)
			point.SetStrokeColor(color.Black)
		}
		if point.GetY() > 80.0 {
			point.SetShape(shape.UpArrow)
			point.SetStrokeColor(color.Blue)
		}
		if point.GetText() == "France" {
			point.SetShape(shape.Multiply)
			point.SetStrokeColor(color.Black)
		}
		if point.GetText() == "Canada" {
			point.SetShape(shape.Box)
			point.SetStrokeColor(color.DarkOliveGreen)
		}
		if strings.HasPrefix(point.GetText(), "United States") {
			point.SetShape(shape.Star)
			point.SetStrokeColor(color.Red)
		}

		points = append(points, point)
	}

	if err := scanner.Err(); err != nil {
		return nil, err
	}

	chartData = append(chartData, points)
	return chartData, nil
}

func main() {
	start := time.Now()
	err := Example09()
	if err != nil {
		panic(err)
	}
	pdfjet.PrintDuration("Example_09", time.Since(start))
}
