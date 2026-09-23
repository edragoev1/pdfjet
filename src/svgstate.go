// svgstate.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
)

// svgState is what an element of an SVG file draws with: the properties it
// inherits from its parent, changed by its own attributes, the rules of the
// style sheet for its classes and its style attribute, in that order, and the
// transform from its user space to that of the svg element.
type svgState struct {
	fill          int32 // color.Transparent is none
	stroke        int32
	fillCurrent   bool // currentColor: the color property, as it is where it is used
	strokeCurrent bool
	color         int32   // The color property
	strokeWidth   float32 // In the user space of the element
	evenOdd       bool    // fill-rule="evenodd"
	lineCap       capstyle.CapStyle
	lineJoin      joinstyle.JoinStyle
	fillOpacity   float32
	strokeOpacity float32
	opacity       float32 // The opacities of the element and its ancestors, multiplied
	ownOpacity    float32 // The opacity of the element itself, not inherited
	matrix        [6]float64
	hidden        bool // In <defs> and the like, or under display="none": not drawn
}

// newSVGState returns the state of the svg element before its attributes: an
// SVG fills black, strokes nothing, and a stroke is one unit wide.
func newSVGState() svgState {
	return svgState{
		fill:          color.Black,
		stroke:        color.Transparent,
		color:         color.Black,
		strokeWidth:   1.0,
		lineCap:       capstyle.Butt,
		lineJoin:      joinstyle.Miter,
		fillOpacity:   1.0,
		strokeOpacity: 1.0,
		opacity:       1.0,
		ownOpacity:    1.0,
		matrix:        svgIdentity,
	}
}

// svgRule is a rule of the style sheet of an SVG file for one class: .name
// followed by its declarations. Selectors of other kinds are left out.
type svgRule struct {
	class        string
	declarations [][2]string
}

// parseSVGStyleSheet returns the rules of the text of a <style> element that
// are for a class, in the order of the text.
func parseSVGStyleSheet(text string) []svgRule {
	// Comments are left out first; they may hold braces.
	for {
		start := strings.Index(text, "/*")
		if start < 0 {
			break
		}
		end := strings.Index(text[start+2:], "*/")
		if end < 0 {
			text = text[:start]
			break
		}
		text = text[:start] + " " + text[start+2+end+2:]
	}
	rules := make([]svgRule, 0)
	for _, block := range strings.Split(text, "}") {
		open := strings.Index(block, "{")
		if open < 0 {
			continue
		}
		declarations := parseSVGDeclarations(block[open+1:])
		for _, selector := range strings.Split(block[:open], ",") {
			selector = strings.TrimSpace(selector)
			if len(selector) > 1 && selector[0] == '.' && isSVGName(selector[1:]) {
				rules = append(rules, svgRule{selector[1:], declarations})
			}
		}
	}
	return rules
}

// isSVGName reports whether the text is a name of a class alone, without the
// combinators, the pseudo-classes and the like of other selectors.
func isSVGName(text string) bool {
	for _, ch := range text {
		if !(ch == '-' || ch == '_' || ch >= '0' && ch <= '9' ||
			ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch > 127) {
			return false
		}
	}
	return true
}

// parseSVGDeclarations returns the declarations of a style attribute or of a
// rule, name: value; each, without !important.
func parseSVGDeclarations(text string) [][2]string {
	declarations := make([][2]string, 0)
	for _, declaration := range strings.Split(text, ";") {
		colon := strings.Index(declaration, ":")
		if colon < 0 {
			continue
		}
		name := strings.TrimSpace(declaration[:colon])
		value := strings.TrimSpace(declaration[colon+1:])
		value = strings.TrimSpace(strings.TrimSuffix(value, "!important"))
		if name != "" && value != "" {
			declarations = append(declarations, [2]string{name, value})
		}
	}
	return declarations
}

// setProperty sets a property of the state, of a presentation attribute, a
// rule or a style attribute. Properties that PDFjet does not draw, and values
// it cannot read, are left as they are inherited; it returns an error only
// for a color that starts with # and is not hexadecimal, as it always has.
func (state *svgState) setProperty(colorMap map[string]int32, name, value string) error {
	value = strings.TrimSpace(value)
	if value == "inherit" {
		return nil
	}
	switch name {
	case "fill", "stroke":
		paint, current, ok, err := parseSVGPaint(colorMap, value)
		if err != nil {
			return fmt.Errorf("invalid SVG %s: %w", name, err)
		}
		if !ok {
			return nil
		}
		if name == "fill" {
			state.fill = paint
			state.fillCurrent = current
		} else {
			state.stroke = paint
			state.strokeCurrent = current
		}
	case "color":
		c, ok, err := parseSVGColor(colorMap, value)
		if err != nil {
			return fmt.Errorf("invalid SVG color: %w", err)
		}
		if ok {
			state.color = c
		}
	case "stroke-width":
		if strings.HasSuffix(value, "%") {
			return nil
		}
		width, err := parseSVGLength(value)
		if err == nil && width >= 0.0 {
			state.strokeWidth = width
		}
	case "fill-rule":
		if value == "evenodd" {
			state.evenOdd = true
		} else if value == "nonzero" {
			state.evenOdd = false
		}
	case "stroke-linecap":
		switch value {
		case "butt":
			state.lineCap = capstyle.Butt
		case "round":
			state.lineCap = capstyle.Round
		case "square":
			state.lineCap = capstyle.ProjectingSquare
		}
	case "stroke-linejoin":
		switch value {
		case "miter", "miter-clip", "arcs":
			state.lineJoin = joinstyle.Miter
		case "round":
			state.lineJoin = joinstyle.Round
		case "bevel":
			state.lineJoin = joinstyle.Bevel
		}
	case "opacity", "fill-opacity", "stroke-opacity":
		alpha, ok := parseSVGOpacity(value)
		if !ok {
			return nil
		}
		if name == "opacity" {
			state.ownOpacity = alpha
		} else if name == "fill-opacity" {
			state.fillOpacity = alpha
		} else {
			state.strokeOpacity = alpha
		}
	case "display":
		if value == "none" {
			state.hidden = true
		}
	}
	return nil
}

// parseSVGPaint reads the value of a fill or a stroke: none, currentColor, a
// color, or a url of a gradient or a pattern, which PDFjet does not draw, and
// the color after it, if any, which SVG draws when it cannot. ok is false for
// a value that leaves the paint as it is inherited.
func parseSVGPaint(colorMap map[string]int32, value string) (paint int32, current, ok bool, err error) {
	if strings.HasPrefix(value, "url(") {
		end := strings.Index(value, ")")
		if end < 0 {
			return 0, false, false, nil
		}
		value = strings.TrimSpace(value[end+1:])
		if value == "" {
			return 0, false, false, nil
		}
	}
	switch value {
	case "none":
		return color.Transparent, false, true, nil
	case "currentColor":
		return 0, true, true, nil
	}
	paint, ok, err = parseSVGColor(colorMap, value)
	return paint, false, ok, err
}

// parseSVGColor reads a color: #rgb, #rrggbb, rgb() of three numbers or
// percentages, or a name. ok is false for a color it does not know, and err
// is set for one that starts with # but is not hexadecimal.
func parseSVGColor(colorMap map[string]int32, value string) (int32, bool, error) {
	if strings.HasPrefix(value, "#") {
		hex := value[1:]
		if len(hex) == 3 {
			hex = string([]byte{hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]})
		}
		if len(hex) != 6 {
			return 0, false, nil
		}
		c, err := strconv.ParseInt(hex, 16, 32)
		if err != nil {
			return 0, false, fmt.Errorf("invalid color %q: %w", value, err)
		}
		return int32(c), true, nil
	}
	lower := strings.ToLower(value)
	if strings.HasPrefix(lower, "rgb(") || strings.HasPrefix(lower, "rgba(") {
		open := strings.Index(lower, "(")
		end := strings.Index(lower, ")")
		if end < open {
			return 0, false, nil
		}
		parts := strings.FieldsFunc(lower[open+1:end], func(ch rune) bool {
			return ch == ',' || ch == '/' || isSVGSpace(ch)
		})
		if len(parts) < 3 {
			return 0, false, nil
		}
		var rgb int32
		for _, part := range parts[:3] {
			scale := 1.0
			if strings.HasSuffix(part, "%") {
				part = part[:len(part)-1]
				scale = 2.55
			}
			number, err := strconv.ParseFloat(part, 64)
			if err != nil || math.IsNaN(number) || math.IsInf(number, 0) {
				return 0, false, nil
			}
			channel := math.Round(number * scale)
			rgb = rgb<<8 | int32(math.Max(0.0, math.Min(255.0, channel)))
		}
		return rgb, true, nil
	}
	if c, ok := colorMap[lower]; ok {
		return c, true, nil
	}
	return 0, false, nil
}

// parseSVGOpacity reads an opacity, a number or a percentage, and returns it
// between 0 and 1.
func parseSVGOpacity(value string) (float32, bool) {
	scale := 1.0
	if strings.HasSuffix(value, "%") {
		value = value[:len(value)-1]
		scale = 0.01
	}
	number, err := strconv.ParseFloat(strings.TrimSpace(value), 64)
	if err != nil || math.IsNaN(number) {
		return 0.0, false
	}
	return float32(math.Max(0.0, math.Min(1.0, number*scale))), true
}

// parseSVGLength reads a length in user units, which PDFjet draws as points,
// with or without a unit; an empty value is 0, as an omitted attribute.
func parseSVGLength(value string) (float32, error) {
	value = strings.TrimSpace(value)
	scale := float32(1.0)
	for _, unit := range svgUnits {
		if strings.HasSuffix(value, unit.suffix) {
			value = strings.TrimSpace(strings.TrimSuffix(value, unit.suffix))
			scale = unit.points
			break
		}
	}
	number, err := parseFloatLenient(value)
	if err == nil && (math.IsInf(float64(number), 0) || math.IsNaN(float64(number))) {
		return 0.0, fmt.Errorf("%q is not a number", value)
	}
	return number * scale, err
}

// parseSVGCoordinate reads a coordinate or a size of a shape, and returns 0
// for one it cannot read, a percentage among them.
func parseSVGCoordinate(value string) float64 {
	length, err := parseSVGLength(value)
	if err != nil || strings.HasSuffix(strings.TrimSpace(value), "%") {
		return 0.0
	}
	return float64(length)
}

// parseSVGNumbers reads a list of numbers, as the points of a polygon or the
// arguments of a transform are written: separated by white space or a comma,
// or by nothing where a sign or a second point starts the next one.
func parseSVGNumbers(text string) ([]float64, error) {
	numbers := make([]float64, 0)
	var buf strings.Builder
	flush := func() error {
		if buf.Len() == 0 {
			return nil
		}
		number, err := strconv.ParseFloat(buf.String(), 64)
		if err == nil && (math.IsInf(number, 0) || math.IsNaN(number)) {
			err = fmt.Errorf("%q is not a number", buf.String())
		}
		buf.Reset()
		if err != nil {
			return err
		}
		numbers = append(numbers, number)
		return nil
	}
	for _, ch := range text {
		if isSVGSpace(ch) || ch == ',' {
			if err := flush(); err != nil {
				return nil, err
			}
		} else if (ch == '-' || ch == '+') && !afterExponent(buf.String()) ||
			ch == '.' && strings.Contains(buf.String(), ".") {
			if err := flush(); err != nil {
				return nil, err
			}
			buf.WriteRune(ch)
		} else {
			buf.WriteRune(ch)
		}
	}
	if err := flush(); err != nil {
		return nil, err
	}
	return numbers, nil
}

// The matrices of transforms are [a b c d e f], which take x, y to
// a*x + c*y + e, b*x + d*y + f, as SVG writes them.
var svgIdentity = [6]float64{1, 0, 0, 1, 0, 0}

// svgMultiply returns the transform that is n, then m.
func svgMultiply(m, n [6]float64) [6]float64 {
	return [6]float64{
		m[0]*n[0] + m[2]*n[1],
		m[1]*n[0] + m[3]*n[1],
		m[0]*n[2] + m[2]*n[3],
		m[1]*n[2] + m[3]*n[3],
		m[0]*n[4] + m[2]*n[5] + m[4],
		m[1]*n[4] + m[3]*n[5] + m[5],
	}
}

// parseSVGTransform returns the matrix of a transform attribute, the list of
// its matrix, translate, scale, rotate, skewX and skewY, applied from the
// last to the first. ok is false for a list PDFjet cannot read, which is
// drawn as if the element had none.
func parseSVGTransform(text string) (matrix [6]float64, ok bool) {
	matrix = svgIdentity
	rest := strings.TrimSpace(text)
	for rest != "" {
		open := strings.Index(rest, "(")
		end := strings.Index(rest, ")")
		if open < 0 || end < open {
			return svgIdentity, false
		}
		name := strings.TrimSpace(rest[:open])
		args, err := parseSVGNumbers(rest[open+1 : end])
		if err != nil {
			return svgIdentity, false
		}
		rest = strings.TrimLeft(rest[end+1:], " \t\r\n,")
		var m [6]float64
		switch {
		case name == "matrix" && len(args) == 6:
			m = [6]float64{args[0], args[1], args[2], args[3], args[4], args[5]}
		case name == "translate" && len(args) == 1:
			m = [6]float64{1, 0, 0, 1, args[0], 0}
		case name == "translate" && len(args) == 2:
			m = [6]float64{1, 0, 0, 1, args[0], args[1]}
		case name == "scale" && len(args) == 1:
			m = [6]float64{args[0], 0, 0, args[0], 0, 0}
		case name == "scale" && len(args) == 2:
			m = [6]float64{args[0], 0, 0, args[1], 0, 0}
		case name == "rotate" && (len(args) == 1 || len(args) == 3):
			angle := args[0] * math.Pi / 180.0
			cos := math.Cos(angle)
			sin := math.Sin(angle)
			m = [6]float64{cos, sin, -sin, cos, 0, 0}
			if len(args) == 3 {
				m = svgMultiply([6]float64{1, 0, 0, 1, args[1], args[2]},
					svgMultiply(m, [6]float64{1, 0, 0, 1, -args[1], -args[2]}))
			}
		case (name == "skewX" || name == "skewY") && len(args) == 1 && math.Mod(math.Abs(args[0]), 180) == 90:
			return svgIdentity, false // A skew of 90 degrees flattens everything: not a transform
		case name == "skewX" && len(args) == 1:
			m = [6]float64{1, 0, math.Tan(args[0] * math.Pi / 180.0), 1, 0, 0}
		case name == "skewY" && len(args) == 1:
			m = [6]float64{1, math.Tan(args[0] * math.Pi / 180.0), 0, 1, 0, 0}
		default:
			return svgIdentity, false
		}
		matrix = svgMultiply(matrix, m)
	}
	return matrix, true
}
