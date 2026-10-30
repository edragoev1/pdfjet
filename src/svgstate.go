// svgstate.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"fmt"
	"math"
	"slices"
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
	fillAlpha     float32 // The alpha of the fill color, as #f008 or rgba() has one
	strokeAlpha   float32
	colorAlpha    float32
	strokeWidth   float32 // In the user space of the element
	evenOdd       bool    // fill-rule="evenodd"
	clipEvenOdd   bool    // clip-rule="evenodd"
	lineCap       capstyle.CapStyle
	lineJoin      joinstyle.JoinStyle
	fillOpacity   float32
	strokeOpacity float32
	opacity       float32 // The opacities of the element and its ancestors, multiplied
	ownOpacity    float32 // The opacity of the element itself, not inherited
	matrix        [6]float64
	hidden        bool      // In <defs> and the like, or under display="none": not drawn
	invisible     bool      // visibility: not painted, but its children may be
	dashArray     []float32 // stroke-dasharray, in the user space; nil is a solid line
	dashOffset    float32
	strokeFirst   bool    // paint-order: the stroke drawn before the fill
	fontSize      float32 // In the user space; medium, as a browser has it
	fontWeight    int     // normal 400, bold 700
	italic        bool    // A font-style of italic or oblique
	fontFamily    int     // The family of the bundled fonts its names stand for
	textAnchor    int     // 0 start, 1 middle, 2 end
	baseline      int     // dominant-baseline: 0 the alphabetic, 1 the middle, 2 the hanging
	markerStart   string  // The ids of the markers, inherited; "" for none
	markerMid     string
	markerEnd     string
	clipPath      string           // The id of the clip path of the element itself, not inherited
	clips         []*svgPath       // The clip paths of the element and of its ancestors; never changed in place
	paints        map[string]int32 // The colors of the gradients, by id, shared by every state
}

// newSVGState returns the state of the svg element before its attributes: an
// SVG fills black, strokes nothing, and a stroke is one unit wide.
func newSVGState() svgState {
	return svgState{
		fill:          color.Black,
		stroke:        color.Transparent,
		color:         color.Black,
		fillAlpha:     1.0,
		strokeAlpha:   1.0,
		colorAlpha:    1.0,
		strokeWidth:   1.0,
		lineCap:       capstyle.Butt,
		lineJoin:      joinstyle.Miter,
		fillOpacity:   1.0,
		strokeOpacity: 1.0,
		opacity:       1.0,
		ownOpacity:    1.0,
		matrix:        svgIdentity,
		fontSize:      16.0,
		fontWeight:    400,
		fontFamily:    bundledSans,
	}
}

// svgRule is a rule of the style sheet of an SVG file for one class: .name
// followed by its declarations. Selectors of other kinds are left out.
type svgRule struct {
	class        string
	declarations [][2]string
}

// svgStyleSheet holds the rules of the <style> elements of an SVG file, in
// their order, and where the rules of each class are among them, so that an
// element finds the rules of its classes without going through all of them.
type svgStyleSheet struct {
	rules   []svgRule
	byClass map[string][]int
	// The declarations its rules have applied to elements, maxSVGApplied at
	// most
	applied int
}

func newSVGStyleSheet() *svgStyleSheet {
	return &svgStyleSheet{byClass: make(map[string][]int)}
}

// add adds the rules of a <style> element after those before it.
func (sheet *svgStyleSheet) add(rules []svgRule) {
	for _, rule := range rules {
		sheet.byClass[rule.class] = append(sheet.byClass[rule.class], len(sheet.rules))
		sheet.rules = append(sheet.rules, rule)
	}
}

// forClasses returns the rules for any of the classes, in the order of the
// style sheet, each rule once.
func (sheet *svgStyleSheet) forClasses(classes []string) ([]svgRule, error) {
	indexes := make([]int, 0)
	// A set, not the classes before each: one element of N classes was N x N
	seen := make(map[string]bool, len(classes))
	for _, class := range classes {
		if seen[class] {
			continue // A class named twice has its rules once.
		}
		seen[class] = true
		indexes = append(indexes, sheet.byClass[class]...)
	}
	slices.Sort(indexes)
	rules := make([]svgRule, len(indexes))
	for i, index := range indexes {
		rules[i] = sheet.rules[index]
		sheet.applied += len(rules[i].declarations)
		if sheet.applied > maxSVGApplied {
			return nil, fmt.Errorf("The style sheet of the SVG applies more than %d declarations.", maxSVGApplied)
		}
	}
	return rules, nil
}

// maxSVGApplied is the most declarations the rules of a style sheet apply
// to elements: N copies of a rule for a class and N elements of it were N x
// N declarations, 20 s for 416 KB (review of 17 October 2026).
const maxSVGApplied = 1000000

// parseSVGStyleSheet returns the rules of the text of a <style> element that
// are for a class, in the order of the text.
func parseSVGStyleSheet(text string) []svgRule {
	// Comments are left out first; they may hold braces. A comment that is
	// not closed runs to the end of the text.
	var uncommented strings.Builder
	for {
		start := strings.Index(text, "/*")
		if start < 0 {
			uncommented.WriteString(text)
			break
		}
		uncommented.WriteString(text[:start])
		end := strings.Index(text[start+2:], "*/")
		if end < 0 {
			break
		}
		uncommented.WriteString(" ")
		text = text[start+2+end+2:]
	}
	text = uncommented.String()
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
		paint, alpha, current, ok, err := parseSVGPaint(colorMap, state.paints, value)
		if err != nil {
			return fmt.Errorf("invalid SVG %s: %w", name, err)
		}
		if !ok {
			return nil
		}
		if name == "fill" {
			state.fill = paint
			state.fillAlpha = alpha
			state.fillCurrent = current
		} else {
			state.stroke = paint
			state.strokeAlpha = alpha
			state.strokeCurrent = current
		}
	case "color":
		c, alpha, ok, err := parseSVGColor(colorMap, value)
		if err != nil {
			return fmt.Errorf("invalid SVG color: %w", err)
		}
		if ok {
			state.color = c
			state.colorAlpha = alpha
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
	case "clip-rule":
		if value == "evenodd" {
			state.clipEvenOdd = true
		} else if value == "nonzero" {
			state.clipEvenOdd = false
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
	case "visibility":
		if value == "hidden" || value == "collapse" {
			state.invisible = true
		} else if value == "visible" {
			state.invisible = false
		}
	case "stroke-dasharray":
		state.dashArray = parseSVGDashArray(value)
	case "stroke-dashoffset":
		if strings.HasSuffix(value, "%") {
			return nil
		}
		if offset, err := parseSVGLength(value); err == nil {
			state.dashOffset = offset
		}
	case "paint-order":
		// The stroke is drawn first when it comes before the fill, and the
		// markers, which PDFjet draws last, are left out.
		order := strings.Fields(value)
		stroke := slices.Index(order, "stroke")
		fill := slices.Index(order, "fill")
		state.strokeFirst = stroke >= 0 && (fill < 0 || stroke < fill)
	case "font-size":
		if size, ok := parseSVGFontSize(value, state.fontSize); ok {
			state.fontSize = size
		}
	case "font-weight":
		if weight, ok := parseSVGFontWeight(value, state.fontWeight); ok {
			state.fontWeight = weight
		}
	case "font-style":
		if value == "italic" || strings.HasPrefix(value, "oblique") {
			state.italic = true
		} else if value == "normal" {
			state.italic = false
		}
	case "font-family":
		state.fontFamily = bundledFamilyOf(value)
	case "font":
		state.setFont(value)
	case "text-anchor":
		switch value {
		case "start":
			state.textAnchor = 0
		case "middle":
			state.textAnchor = 1
		case "end":
			state.textAnchor = 2
		}
	case "dominant-baseline", "alignment-baseline":
		switch value {
		case "middle", "central":
			state.baseline = 1
		case "hanging", "text-before-edge":
			state.baseline = 2
		case "auto", "alphabetic", "baseline":
			state.baseline = 0
		}
	case "marker-start":
		state.markerStart = svgURLId(value)
	case "marker-mid":
		state.markerMid = svgURLId(value)
	case "marker-end":
		state.markerEnd = svgURLId(value)
	case "marker":
		state.markerStart = svgURLId(value)
		state.markerMid = state.markerStart
		state.markerEnd = state.markerStart
	case "clip-path":
		state.clipPath = svgURLId(value)
	}
	return nil
}

// drawnFillAlpha returns the alpha the fill is drawn with: its opacities and
// the alpha of its color.
func (state *svgState) drawnFillAlpha() float32 {
	alpha := state.fillAlpha
	if state.fillCurrent {
		alpha = state.colorAlpha
	}
	return float32(float32(state.fillOpacity*state.opacity) * alpha)
}

// drawnStrokeAlpha returns the alpha the stroke is drawn with.
func (state *svgState) drawnStrokeAlpha() float32 {
	alpha := state.strokeAlpha
	if state.strokeCurrent {
		alpha = state.colorAlpha
	}
	return float32(float32(state.strokeOpacity*state.opacity) * alpha)
}

// svgURLId returns the id of a reference, url(#id), with or without quotes,
// and "" for none or for a value of another kind.
func svgURLId(value string) string {
	value = strings.TrimSpace(value)
	if !strings.HasPrefix(value, "url(") {
		return ""
	}
	end := strings.Index(value, ")")
	if end < 0 {
		return ""
	}
	id := strings.TrimSpace(value[4:end])
	if len(id) >= 2 && (id[0] == '"' || id[0] == '\'') && id[len(id)-1] == id[0] {
		id = strings.TrimSpace(id[1 : len(id)-1])
	}
	if len(id) < 2 || id[0] != '#' {
		return ""
	}
	return id[1:]
}

// parseSVGDashArray reads a stroke-dasharray: the lengths of the dashes and
// the gaps, as many again when they are an odd number. It returns nil, a
// solid line, for none, for a length below zero or one it cannot read, and
// for lengths that are all zero.
func parseSVGDashArray(value string) []float32 {
	if value == "none" {
		return nil
	}
	lengths := make([]float32, 0)
	for _, field := range strings.Fields(strings.ReplaceAll(value, ",", " ")) {
		if strings.HasSuffix(field, "%") {
			return nil
		}
		length, err := parseSVGLength(field)
		if err != nil || length < 0.0 || math.IsNaN(float64(length)) || math.IsInf(float64(length), 0) {
			return nil
		}
		lengths = append(lengths, length)
	}
	sum := float32(0.0)
	for _, length := range lengths {
		sum += length
	}
	if len(lengths) == 0 || sum == 0.0 {
		return nil
	}
	count := len(lengths)
	dashes := make([]float32, count)
	if count%2 != 0 {
		dashes = make([]float32, 2*count)
	}
	for i := range dashes {
		dashes[i] = lengths[i%count]
	}
	return dashes
}

// svgFontSizes are the font sizes of the keywords, as a browser has them.
var svgFontSizes = map[string]float32{
	"xx-small": 9.0, "x-small": 10.0, "small": 13.0, "medium": 16.0,
	"large": 18.0, "x-large": 24.0, "xx-large": 32.0,
}

// parseSVGFontSize reads a font-size: a length, a number of em or a
// percentage of the font size of the parent, or a keyword. ok is false for a
// size it cannot read or one below zero.
func parseSVGFontSize(value string, parentSize float32) (float32, bool) {
	if size, ok := svgFontSizes[value]; ok {
		return size, true
	}
	var size float32
	if strings.HasSuffix(value, "em") && !strings.HasSuffix(value, "rem") {
		factor, ok := parseSVGFactor(value[:len(value)-2])
		if !ok {
			return 0.0, false
		}
		size = float32(factor * parentSize)
	} else if strings.HasSuffix(value, "%") {
		factor, ok := parseSVGFactor(value[:len(value)-1])
		if !ok {
			return 0.0, false
		}
		size = float32(factor*parentSize) / 100.0
	} else {
		length, err := parseSVGLength(value)
		if err != nil {
			return 0.0, false
		}
		size = length
	}
	if size < 0.0 || math.IsNaN(float64(size)) || math.IsInf(float64(size), 0) {
		return 0.0, false
	}
	return size, true
}

// parseSVGFactor reads a number; ok is false for one it cannot read.
func parseSVGFactor(text string) (float32, bool) {
	number, err := parseSVGFloat(strings.TrimSpace(text), 32)
	if err != nil {
		return 0.0, false
	}
	return float32(number), true
}

// parseSVGFontWeight reads a font-weight, from 1 to 1000: normal is 400 and
// bold 700, and bolder and lighter are those of CSS, from the weight of the
// parent. ok is false for a weight it cannot read.
func parseSVGFontWeight(value string, parentWeight int) (int, bool) {
	switch value {
	case "normal":
		return 400, true
	case "bold":
		return 700, true
	case "bolder":
		if parentWeight < 350 {
			return 400, true
		} else if parentWeight < 550 {
			return 700, true
		}
		return 900, true
	case "lighter":
		if parentWeight < 550 {
			return 100, true
		} else if parentWeight < 750 {
			return 400, true
		}
		return 700, true
	}
	weight, ok := parseSVGFactor(value)
	if !ok || weight < 1.0 || weight > 1000.0 {
		return 0, false
	}
	return int(math.Floor(float64(weight) + 0.5)), true // As Java's Math.round
}

// setFont sets the size, the weight, the style and the family of the font
// shorthand, such as "bold 10px sans-serif": the words before the size, then
// the size, which may have a line height after a slash, then the family. A
// shorthand without a weight is normal.
func (state *svgState) setFont(value string) {
	weight := 400
	italic := false
	fields := strings.Fields(value)
	for i, field := range fields {
		size := field
		slash := strings.Index(field, "/")
		if slash >= 0 {
			size = field[:slash]
		}
		// A size has a unit or is a keyword; a number alone is a weight.
		if size != "" && (size[len(size)-1] < '0' || size[len(size)-1] > '9') {
			if fontSize, ok := parseSVGFontSize(size, state.fontSize); ok {
				state.fontSize = fontSize
				state.fontWeight = weight
				state.italic = italic
				// The family after the size, and a line height after a
				// slash written apart from it
				from := i + 1
				if slash < 0 && from < len(fields) && strings.HasPrefix(fields[from], "/") {
					if fields[from] == "/" {
						from += 2
					} else {
						from++
					}
				}
				family := ""
				if from < len(fields) {
					family = strings.Join(fields[from:], " ")
				}
				state.fontFamily = bundledFamilyOf(family)
				return
			}
		}
		if field == "italic" || field == "oblique" {
			italic = true
			continue
		}
		if w, ok := parseSVGFontWeight(field, state.fontWeight); ok {
			weight = w
		}
	}
}

// parseSVGPaint reads the value of a fill or a stroke: none, currentColor, a
// color, or a url of a gradient, which PDFjet draws in the mean color of its
// stops, its color in the paints. A url of anything else, a pattern or an
// element that is not there, is the color after it, which SVG draws when it
// cannot draw the url, or none, as a browser draws it. ok is false for a
// value that leaves the paint as it is inherited. none and currentColor are
// keywords of CSS, in any case.
func parseSVGPaint(colorMap, paints map[string]int32, value string) (paint int32, alpha float32, current, ok bool, err error) {
	if strings.HasPrefix(value, "url(") {
		end := strings.Index(value, ")")
		if end < 0 {
			return 0, 1.0, false, false, nil
		}
		if id := svgURLId(value[:end+1]); id != "" {
			if gradient, found := paints[id]; found {
				return gradient, 1.0, false, true, nil
			}
		}
		value = strings.TrimSpace(value[end+1:])
		if value == "" {
			value = "none"
		}
	}
	switch strings.ToLower(value) {
	case "none":
		return color.Transparent, 1.0, false, true, nil
	case "currentcolor":
		return 0, 1.0, true, true, nil
	}
	paint, alpha, ok, err = parseSVGColor(colorMap, value)
	return paint, alpha, false, ok, err
}

// parseSVGColor reads a color and its alpha: #rgb, #rgba, #rrggbb, #rrggbbaa,
// rgb() of three numbers or percentages, hsl(), with an alpha after them in
// either, or a name. ok is false for a color it does not know, and err is set
// for one that starts with # but is not hexadecimal.
func parseSVGColor(colorMap map[string]int32, value string) (int32, float32, bool, error) {
	if strings.HasPrefix(value, "#") {
		hex := value[1:]
		if len(hex) == 3 || len(hex) == 4 {
			doubled := make([]byte, 0, 8)
			for i := 0; i < len(hex); i++ {
				doubled = append(doubled, hex[i], hex[i])
			}
			hex = string(doubled)
		}
		if len(hex) != 6 && len(hex) != 8 {
			return 0, 1.0, false, nil
		}
		c, err := strconv.ParseInt(hex[:6], 16, 32)
		if err != nil {
			return 0, 1.0, false, fmt.Errorf("invalid color %q: %w", value, err)
		}
		alpha := float32(1.0)
		if len(hex) == 8 {
			a, err := strconv.ParseInt(hex[6:], 16, 32)
			if err != nil {
				return 0, 1.0, false, fmt.Errorf("invalid color %q: %w", value, err)
			}
			alpha = float32(max(a, 0)) / 255.0
		}
		return int32(c), alpha, true, nil
	}
	lower := strings.ToLower(value)
	hsl := strings.HasPrefix(lower, "hsl(") || strings.HasPrefix(lower, "hsla(")
	if hsl || strings.HasPrefix(lower, "rgb(") || strings.HasPrefix(lower, "rgba(") {
		open := strings.Index(lower, "(")
		end := strings.Index(lower, ")")
		if end < open {
			return 0, 1.0, false, nil
		}
		parts := strings.FieldsFunc(lower[open+1:end], func(ch rune) bool {
			return ch == ',' || ch == '/' || isSVGSpace(ch)
		})
		if len(parts) < 3 {
			return 0, 1.0, false, nil
		}
		alpha := float32(1.0)
		if len(parts) > 3 {
			if opacity, ok := parseSVGOpacity(parts[3]); ok {
				alpha = opacity
			}
		}
		if hsl {
			rgb, ok := parseSVGHSL(parts)
			return rgb, alpha, ok, nil
		}
		var rgb int32
		for _, part := range parts[:3] {
			scale := 1.0
			if strings.HasSuffix(part, "%") {
				part = part[:len(part)-1]
				scale = 2.55
			}
			number, err := parseSVGFloat(part, 64)
			if err != nil || math.IsNaN(number) || math.IsInf(number, 0) {
				return 0, 1.0, false, nil
			}
			channel := math.Round(float64(number * scale))
			rgb = rgb<<8 | int32(math.Max(0.0, math.Min(255.0, channel)))
		}
		return rgb, alpha, true, nil
	}
	if c, ok := colorMap[lower]; ok {
		return c, 1.0, true, nil
	}
	return 0, 1.0, false, nil
}

// parseSVGHSL returns the color of the hue, the saturation and the lightness
// of hsl(), by the formula of CSS Color 4; ok is false for one it cannot read.
func parseSVGHSL(parts []string) (int32, bool) {
	hue, ok := parseSVGHue(parts[0])
	saturation, ok2 := parseSVGPercentage(parts[1])
	lightness, ok3 := parseSVGPercentage(parts[2])
	if !ok || !ok2 || !ok3 {
		return 0, false
	}
	a := float64(saturation * math.Min(lightness, 1.0-lightness))
	var rgb int32
	for _, n := range [3]float64{0.0, 8.0, 4.0} {
		k := math.Mod(n+hue/30.0, 12.0)
		channel := lightness - float64(a*math.Max(-1.0, math.Min(math.Min(k-3.0, 9.0-k), 1.0)))
		rgb = rgb<<8 | int32(math.Max(0.0, math.Min(255.0, math.Round(float64(channel*255.0)))))
	}
	return rgb, true
}

// parseSVGHue reads a hue, in degrees unless it has a unit, and returns it
// from 0 to 360.
func parseSVGHue(text string) (float64, bool) {
	scale := 1.0
	for _, unit := range []struct {
		suffix string
		scale  float64
	}{{"deg", 1.0}, {"grad", 0.9}, {"rad", 57.29577951308232}, {"turn", 360.0}} {
		if strings.HasSuffix(text, unit.suffix) {
			text = strings.TrimSuffix(text, unit.suffix)
			scale = unit.scale
			break
		}
	}
	number, err := parseSVGFloat(text, 64)
	if err != nil || math.IsNaN(number) || math.IsInf(number, 0) {
		return 0.0, false
	}
	hue := math.Mod(float64(number*scale), 360.0)
	if hue < 0.0 {
		hue += 360.0
	}
	return hue, true
}

// parseSVGPercentage reads a saturation or a lightness, with or without its
// percent sign, and returns it between 0 and 1.
func parseSVGPercentage(text string) (float64, bool) {
	number, err := parseSVGFloat(strings.TrimSuffix(text, "%"), 64)
	if err != nil || math.IsNaN(number) || math.IsInf(number, 0) {
		return 0.0, false
	}
	return math.Max(0.0, math.Min(1.0, number/100.0)), true
}

// parseSVGOpacity reads an opacity, a number or a percentage, and returns it
// between 0 and 1.
func parseSVGOpacity(value string) (float32, bool) {
	scale := 1.0
	if strings.HasSuffix(value, "%") {
		value = value[:len(value)-1]
		scale = 0.01
	}
	number, err := parseSVGFloat(strings.TrimSpace(value), 64)
	if err != nil || math.IsNaN(number) {
		return 0.0, false
	}
	return float32(math.Max(0.0, math.Min(1.0, float64(number*scale)))), true
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
	return float32(number * scale), err
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
		number, err := parseSVGFloat(buf.String(), 64)
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
		float64(m[0]*n[0]) + float64(m[2]*n[1]),
		float64(m[1]*n[0]) + float64(m[3]*n[1]),
		float64(m[0]*n[2]) + float64(m[2]*n[3]),
		float64(m[1]*n[2]) + float64(m[3]*n[3]),
		float64(m[0]*n[4]) + float64(m[2]*n[5]) + m[4],
		float64(m[1]*n[4]) + float64(m[3]*n[5]) + m[5],
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
			angle := float64(float64(args[0]*math.Pi) / 180.0)
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
			m = [6]float64{1, 0, math.Tan(float64(float64(args[0]*math.Pi) / 180.0)), 1, 0, 0}
		case name == "skewY" && len(args) == 1:
			m = [6]float64{1, math.Tan(float64(float64(args[0]*math.Pi) / 180.0)), 0, 1, 0, 0}
		default:
			return svgIdentity, false
		}
		matrix = svgMultiply(matrix, m)
	}
	return matrix, true
}

// parseSVGFloat reads a number of an attribute as strconv.ParseFloat does,
// with inf, infinity and nan in any case, but only one written in decimal:
// a hexadecimal number, as 0x1p3, or one with underscores, as 1_000, is none
// of SVG's, and C# did not read them (review of 17 October 2026).
func parseSVGFloat(text string, bitSize int) (float64, error) {
	digits := strings.TrimLeft(text, "+-")
	switch strings.ToLower(digits) {
	case "inf", "infinity", "nan":
	default:
		if strings.Trim(text, "0123456789.eE+-") != "" {
			return 0, fmt.Errorf("%q is not a number", text)
		}
	}
	return strconv.ParseFloat(text, bitSize)
}
