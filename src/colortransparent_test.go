package pdfjet

import (
	"fmt"
	"reflect"
	"sort"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// TestEveryColorSetterLeavesTheColorForTransparent checks that each setter of
// a 0xRRGGBB color leaves the color as it was for color.Transparent, -1, which
// they drew white, its low 24 bits, before v9.0.3: the object set to red, then
// to transparent, is the object set to red.
func TestEveryColorSetterLeavesTheColorForTransparent(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	cases := []struct {
		name string
		make func() any
		set  func(any, int32)
	}{
		{"Arc.SetStrokeColor", func() any { return NewArc() }, func(o any, c int32) { o.(*Arc).SetStrokeColor(c) }},
		{"Arc.SetFillColor", func() any { return NewArc() }, func(o any, c int32) { o.(*Arc).SetFillColor(c) }},
		{"Chart.SetGridLineColor", func() any { return NewChart(font, font) }, func(o any, c int32) { o.(*Chart).SetGridLineColor(c) }},
		{"BarChart.SetGridLineColor", func() any { return NewBarChart(font, font) }, func(o any, c int32) { o.(*BarChart).SetGridLineColor(c) }},
		{"CheckBox.SetBorderColor", func() any { return NewCheckBox(font, "A") }, func(o any, c int32) { o.(*CheckBox).SetBorderColor(c) }},
		{"CheckBox.SetCheckmarkColor", func() any { return NewCheckBox(font, "A") }, func(o any, c int32) { o.(*CheckBox).SetCheckmarkColor(c) }},
		{"BaseAnnotation.SetFillColor", func() any { return NewSquareAnnotation() }, func(o any, c int32) { o.(*SquareAnnotation).SetFillColor(c) }},
		{"Container.SetBorderColor", func() any { return NewContainer(100, 50) }, func(o any, c int32) { o.(*Container).SetBorderColor(c) }},
		{"Page.SetPenColor", func() any { return testNewPage() }, func(o any, c int32) { o.(*Page).SetPenColor(c) }},
		{"Page.SetBrushColor", func() any { return testNewPage() }, func(o any, c int32) { o.(*Page).SetBrushColor(c) }},
		{"Markup.SetLinkColor", func() any { return NewMarkup(font, font, font, font, font) }, func(o any, c int32) { o.(*Markup).SetLinkColor(c) }},
		{"Form.SetLabelColor", func() any { return NewForm(nil) }, func(o any, c int32) { o.(*Form).SetLabelColor(c) }},
		{"Form.SetValueColor", func() any { return NewForm(nil) }, func(o any, c int32) { o.(*Form).SetValueColor(c) }},
		{"Line.SetStrokeColor", func() any { return NewLine(0, 0, 10, 10) }, func(o any, c int32) { o.(*Line).SetStrokeColor(c) }},
		{"Point.SetStrokeColor", func() any { return NewPoint(1, 1) }, func(o any, c int32) { o.(*Point).SetStrokeColor(c) }},
		{"Point.SetFillColor", func() any { return NewPoint(1, 1) }, func(o any, c int32) { o.(*Point).SetFillColor(c) }},
		{"Paragraph.SetTextColor", func() any { return NewParagraph().Add(NewTextLine(font, "A")) }, func(o any, c int32) { o.(*Paragraph).SetTextColor(c) }},
		{"Stamp.SetStrokeColor", func() any { return NewStamp(pdf) }, func(o any, c int32) { o.(*Stamp).SetStrokeColor(c) }},
		{"Stamp.SetFillColor", func() any { return NewStamp(pdf) }, func(o any, c int32) { o.(*Stamp).SetFillColor(c) }},
		{"Table.SetCellBorderColor", func() any {
			table := NewTable()
			table.SetTableData([][]*Cell{{NewCell(font, "A")}}, 0)
			return table
		}, func(o any, c int32) { o.(*Table).SetCellBorderColor(c) }},
		{"Path.SetStrokeColor", func() any { return NewPath() }, func(o any, c int32) { o.(*Path).SetStrokeColor(c) }},
		{"Rect.SetFillColor", func() any { return NewRect(0, 0, 10, 10) }, func(o any, c int32) { o.(*Rect).SetFillColor(c) }},
		{"Series.SetStrokeColor", func() any { return newSeries("A") }, func(o any, c int32) { o.(*Series).SetStrokeColor(c) }},
	}
	for _, c := range cases {
		o := c.make()
		c.set(o, color.Red)
		red := dump(reflect.ValueOf(o), 4)
		c.set(o, color.Transparent)
		if after := dump(reflect.ValueOf(o), 4); after != red {
			t.Errorf("%s: color.Transparent changed it", c.name)
		}
	}
}

// dump writes the value, its unexported fields too, following pointers that
// many levels down, so that a color kept in a cell of a table, or a line of a
// paragraph, is seen: fmt prints a pointer as its address only.
func dump(v reflect.Value, depth int) string {
	switch v.Kind() {
	case reflect.Pointer, reflect.Interface:
		if v.IsNil() {
			return "nil"
		}
		if depth == 0 {
			return "&"
		}
		return "&" + dump(v.Elem(), depth-1)
	case reflect.Struct:
		var b strings.Builder
		b.WriteString("{")
		for i := 0; i < v.NumField(); i++ {
			b.WriteString(v.Type().Field(i).Name + ":" + dump(v.Field(i), depth) + " ")
		}
		return b.String() + "}"
	case reflect.Slice, reflect.Array:
		var b strings.Builder
		b.WriteString("[")
		for i := 0; i < v.Len() && i < 64; i++ {
			b.WriteString(dump(v.Index(i), depth) + " ")
		}
		return b.String() + "]"
	case reflect.Map:
		keys := make([]string, 0, v.Len())
		for _, k := range v.MapKeys() {
			keys = append(keys, fmt.Sprint(k)+"="+dump(v.MapIndex(k), depth))
		}
		sort.Strings(keys)
		return "map" + fmt.Sprint(keys)
	case reflect.Func, reflect.Chan, reflect.UnsafePointer:
		return fmt.Sprint(v.Kind())
	case reflect.Bool:
		return fmt.Sprint(v.Bool())
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		return fmt.Sprint(v.Int())
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64, reflect.Uintptr:
		return fmt.Sprint(v.Uint())
	case reflect.Float32, reflect.Float64:
		return fmt.Sprint(v.Float())
	case reflect.String:
		return fmt.Sprintf("%q", v.String())
	}
	return v.Kind().String()
}
