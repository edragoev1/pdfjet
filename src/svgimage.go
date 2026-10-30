// svgimage.go
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
	"bytes"
	"fmt"
	"io"
	"math"
	"os"
	"slices"
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
	"github.com/edragoev1/pdfjet/v9/src/xmlparser"
)

// SVGImage is used to draw svgParser images on a page.
type SVGImage struct {
	x, y, w, h          float32
	viewBox             string
	preserveAspectRatio string
	paths               []*svgPath
	uri                 string
	key                 string
	language            string
	altDescription      string
	actualText          string
	font                *Font
	boldFont            *Font
	clipShapes          []*svgPath // The clip paths of the image
	inUnits             bool       // A size in a unit such as mm, and no viewBox
	switches            []*svgSwitch
}

// maxSVGReused is the most elements drawn again, by use elements and as
// markers: a use of a group of uses, and so on, draws ten to the power of the
// number of groups, as an XML of entities in entities does.
const maxSVGReused = 1000000

// maxSVGOperations is the most path operations drawn again, by use elements
// and as markers, and written for the clip paths of what is drawn: a use of a
// path of N operations was N operations for each element maxSVGReused counts,
// 506 MB for a file of 200 KB, and a clip path of N operations is read for
// every element it clips and written for every path in it, 800 MB of page for
// 320 KB (review of 30 October 2026). The operations of a file drawn once are
// not counted: they grow with the file.
const maxSVGOperations = 4000000

// maxSVGDepth is how deep the elements being drawn may nest, by groups, use
// elements and markers, a state on the stack for each: a chain of use elements
// is flat in the file, so the limit of the XML parser does not see it, and
// each level is a call. The number is the same in the four ports and leaves
// every one of them room on a stack of 512 KB, where a level takes 7.8 KB of a debug
// build of Swift (review of 30 October 2026).
const maxSVGDepth = 48

// svgReader holds what is needed while an SVG file is read: the elements by
// their ids, the states of the elements being drawn, and the elements drawn
// again by a use element or as a marker, which are not drawn inside
// themselves.
type svgReader struct {
	image    *SVGImage
	colorMap map[string]int32
	rules    *svgStyleSheet
	stack    []svgState
	root     bool
	ids      map[string]*xmlparser.XMLNode
	reusing  map[*xmlparser.XMLNode]bool
	reused   int64
	useSize  [2]string // The width and the height of the use element of the element drawn next
	counted  int64     // The operations maxSVGOperations limits
	marker   []float64 // The viewport of the marker drawn next, in its user space, or nil
	switches map[*xmlparser.XMLNode]*svgSwitch
	clipping bool // A text of a clip path is read, for each element it clips
}

// NewSVGImageFromFile reads and parses an svgParser image from a file.
// The file is fully read before parsing, so no file handle lifecycle is involved.
func NewSVGImageFromFile(filePath string) (*SVGImage, error) {
	data, err := os.ReadFile(filePath)
	if err != nil {
		return nil, fmt.Errorf("reading SVG %q: %w", filePath, err)
	}
	return NewSVGImage(bytes.NewReader(data))
}

// svgTemplates are the elements whose content is not drawn where it is, but
// used by other elements, which PDFjet does not draw.
var svgTemplates = map[string]bool{
	"defs": true, "clipPath": true, "mask": true, "pattern": true, "symbol": true,
	"marker": true, "linearGradient": true, "radialGradient": true,
}

// NewSVGImage parses an svgParser image from a reader, for embedding in a PDF document.
// It draws the <path>, <rect>, <circle>, <ellipse>, <line>, <polyline> and
// <polygon> elements, in groups or not, with their transforms, and the fill,
// stroke, stroke-width, stroke-dasharray, stroke-dashoffset, fill-rule,
// stroke-linecap, stroke-linejoin, stroke-miterlimit, opacity, fill-opacity,
// stroke-opacity, visibility, paint-order, clip-path and markers they have or
// inherit, as attributes, in a style attribute or in rules for their classes in a <style> element that
// comes before them. It draws the elements a <use> element names, a <symbol>
// at the size of the <use>, a nested <svg> at its x, y and viewBox, the first
// child of a <switch> that it can draw and whose systemLanguage has the language
// of the PDF, which is known when the image is drawn, and <text>
// and <tspan> elements with their x, y, dx, dy, font-size, font-weight,
// text-anchor and dominant-baseline, in the font set with SetFont and
// SetBoldFont, or else in the fonts PDFjet carries, embedded: IBM Plex Sans
// for sans-serif, Helvetica, Arial and the like, IBM Plex Serif for serif,
// Times and the like, Cascadia Mono for monospace, Courier and the like, in
// the face of the weight CSS matches to the font-weight, upright or italic, as
// the font-family, font-weight and font-style of the text ask. A gradient is
// drawn in the mean color of its stops. The width, height, viewBox and
// preserveAspectRatio of the <svg> element give the size of the image, and
// what is drawn outside it is cut off, as a browser does, and so is what a
// <symbol>, a <marker> or a nested <svg> draws outside its viewport, unless its
// overflow is visible or auto. A <clipPath> clips with its shapes or, when it has none, with
// its <text> elements, in those fonts, by the text rendering mode 7 of a PDF.
// A text cannot be in one path with other shapes, as a text object holds no
// path and clip paths do not add up, so the texts of a <clipPath> that has
// shapes too are left out, and the glyphs of a text clip by the nonzero rule,
// whatever its clip-rule. The shapes of a <clipPath> of several are one path,
// their union, by the nonzero rule, so a clip-rule of evenodd is drawn only in a
// <clipPath> of one shape: clip paths in a PDF only intersect and a path has one
// rule, so even-odd shapes in one union would have to be cut where they cross,
// and a soft mask, which PDF/A-1 forbids, is not used for it. It returns an error
// for a file that nests what it draws more than 48 deep, or that draws more
// than 4,000,000 path operations again, by use elements, markers and clip paths.
func NewSVGImage(reader io.Reader) (*SVGImage, error) {
	image := new(SVGImage)
	image.paths = make([]*svgPath, 0)

	document, err := xmlparser.ParseReaderSkippingDoctype(reader)
	if err != nil {
		return nil, fmt.Errorf("parsing SVG: %w", err)
	}
	r := &svgReader{
		image:    image,
		colorMap: svgColorMap,
		rules:    newSVGStyleSheet(),
		root:     true,
		ids:      make(map[string]*xmlparser.XMLNode),
		reusing:  make(map[*xmlparser.XMLNode]bool),
		switches: make(map[*xmlparser.XMLNode]*svgSwitch),
	}
	// The first element of an id, from the top down, is the one it names.
	elements := []*xmlparser.XMLNode{document}
	for i := 0; i < len(elements); i++ {
		element := elements[i]
		if id, ok := element.Attribute("id"); ok && id != "" {
			if _, found := r.ids[id]; !found {
				r.ids[id] = element
			}
		}
		elements = append(elements, element.Children()...)
	}
	svg := newSVGState()
	svg.paints, err = r.gradientColors()
	if err != nil {
		return nil, err
	}
	r.stack = []svgState{svg}
	if err := r.draw(document, false); err != nil {
		return nil, err
	}

	if err := image.processPaths(image.paths); err != nil {
		return nil, err
	}
	return image, nil
}

// gradientColors returns the colors the gradients are drawn in, by their ids:
// the mean of the colors of their stops, or of the stops of the gradient they
// name with href when they have none. A gradient without stops is none, and
// is left out.
func (r *svgReader) gradientColors() (map[string]int32, error) {
	colors := make(map[string]int32)
	read := make(map[*xmlparser.XMLNode]*svgStops)
	for id, gradient := range r.ids {
		// Up to ten gradients along the hrefs, which may go round in a circle.
		for i := 0; i < 10 && gradient != nil && isSVGGradient(gradient); i++ {
			stops := read[gradient]
			if stops == nil {
				var err error
				stops, err = r.gradientStops(gradient)
				if err != nil {
					return nil, err
				}
				read[gradient] = stops
			}
			if stops.found {
				colors[id] = stops.color
				break
			}
			gradient = stops.next
		}
	}
	return colors, nil
}

// svgStops is what a gradient itself says of its color: the mean of its stops,
// if it has any, and the gradient its href names.
type svgStops struct {
	color int32
	found bool
	next  *xmlparser.XMLNode
}

// gradientStops reads the gradient, once however many gradients name it: N
// gradients that name one of N stops were N x N stops read, 50 s for 3.4 MB
// (review of 30 October 2026).
func (r *svgReader) gradientStops(gradient *xmlparser.XMLNode) (*svgStops, error) {
	var red, green, blue int64
	count := int64(0)
	for _, stop := range gradient.Children() {
		if stop.LocalName() != "stop" {
			continue
		}
		state := newSVGState()
		state.fill = color.Black // stop-color is black unless given
		if c, ok := stop.Attribute("stop-color"); ok {
			if err := state.setProperty(r.colorMap, "fill", c); err != nil {
				return nil, err
			}
		}
		style, _ := stop.Attribute("style")
		for _, declaration := range parseSVGDeclarations(style) {
			if declaration[0] == "stop-color" {
				if err := state.setProperty(r.colorMap, "fill", declaration[1]); err != nil {
					return nil, err
				}
			}
		}
		c := int64(state.fill)
		if state.fill == color.Transparent {
			c = int64(color.Black)
		}
		red += (c >> 16) & 0xFF
		green += (c >> 8) & 0xFF
		blue += c & 0xFF
		count++
	}
	stops := new(svgStops)
	if count > 0 {
		stops.color = int32((red+count/2)/count<<16 | (green+count/2)/count<<8 | (blue+count/2)/count)
		stops.found = true
	}
	stops.next = r.ids[svgHref(gradient)]
	return stops, nil
}

// isSVGGradient reports whether the element is a gradient.
func isSVGGradient(node *xmlparser.XMLNode) bool {
	name := node.LocalName()
	return name == "linearGradient" || name == "radialGradient"
}

// svgHref returns the id that the href of an element names, #id, with or
// without the xlink prefix, or "".
func svgHref(node *xmlparser.XMLNode) string {
	href := ""
	found := false
	for _, attr := range node.Attributes() {
		name := attr.Name
		localName := name
		if colon := strings.IndexByte(name, ':'); colon != -1 {
			localName = name[colon+1:]
		}
		if name == "href" || !found && localName == "href" {
			href = attr.Value
			found = true
		}
	}
	href = strings.TrimSpace(href)
	if len(href) > 1 && href[0] == '#' {
		return href[1:]
	}
	return ""
}

// svgAttributes returns the attributes of no namespace of the element, and
// their order: xlink:href, xml:space and the namespaces' own declarations,
// xmlns:..., are not read.
func svgAttributes(node *xmlparser.XMLNode) (map[string]string, []string) {
	attributes := make(map[string]string)
	order := make([]string, 0)
	for _, attr := range node.Attributes() {
		if !strings.Contains(attr.Name, ":") {
			attributes[attr.Name] = attr.Value
			order = append(order, attr.Name)
		}
	}
	return attributes, order
}

// svgAttribute returns the value of the attribute of the element, or "".
func svgAttribute(node *xmlparser.XMLNode, name string) string {
	value, _ := node.Attribute(name)
	return value
}

// countReused counts an element drawn again, and fails when there are too
// many.
func (r *svgReader) countReused() error {
	r.reused++
	if r.reused > maxSVGReused {
		return fmt.Errorf("The SVG draws more than %d elements again, with use elements or markers.", maxSVGReused)
	}
	return nil
}

// countOperations counts operations drawn again or written again, and fails
// when there are too many.
func (r *svgReader) countOperations(count int) error {
	r.counted += int64(count)
	if r.counted > maxSVGOperations {
		return fmt.Errorf("The SVG draws more than %d path operations again, with use elements, "+
			"markers or clip paths.", maxSVGOperations)
	}
	return nil
}

// svgClipOperations returns the operations written for the clip paths of one
// path, and of a run of a text.
func svgClipOperations(clips []*svgPath) int {
	count := 0
	for _, clip := range clips {
		count += len(clip.operations) + clip.glyphs
	}
	return count
}

// errSVGDepth is the error of elements nested too deep.
func errSVGDepth() error {
	return fmt.Errorf("The SVG nests elements, use elements and markers more than %d deep.", maxSVGDepth)
}

// draw draws the element, then the ones in it, in the order of the document;
// the rules of a <style> element apply from its end. An element that is used,
// by a use element or as a marker, is drawn even where it is a template, as a
// symbol or a marker is.
func (r *svgReader) draw(node *xmlparser.XMLNode, used bool) error {
	if len(r.stack) > maxSVGDepth {
		return errSVGDepth()
	}
	image := r.image
	name := node.LocalName()
	if len(r.reusing) > 0 {
		if err := r.countReused(); err != nil {
			return err
		}
	}
	attributes, order := svgAttributes(node)
	state, err := elementState(r.stack[len(r.stack)-1], r.colorMap, r.rules, attributes, order)
	if err != nil {
		return err
	}
	if svgTemplates[name] && !used {
		state.hidden = true
	}
	if state.clipPath != "" && !state.hidden {
		if err := r.clip(&state); err != nil {
			return err
		}
	}
	size := r.useSize
	r.useSize = [2]string{}
	marker := r.marker
	r.marker = nil
	if name == "svg" && r.root {
		r.root = false
		image.w = parseLength(attributes["width"])
		image.h = parseLength(attributes["height"])
		// viewbox in lower case when there is no viewBox, as an HTML
		// page's parser reads an inline svg, whose files keep it
		if viewBox, ok := attributes["viewBox"]; ok {
			image.viewBox = viewBox
		} else {
			image.viewBox = attributes["viewbox"]
		}
		image.preserveAspectRatio = attributes["preserveAspectRatio"]
		image.inUnits = image.viewBox == "" && image.w > 0.0 && image.h > 0.0 &&
			(svgHasUnit(attributes["width"]) || svgHasUnit(attributes["height"]))
		state.viewport = [2]float64{float64(image.w), float64(image.h)}
		if image.inUnits {
			state.viewport = [2]float64{float64(image.w / svgPixel), float64(image.h / svgPixel)}
		}
		if box, err := parseSVGNumbers(image.viewBox); err == nil && len(box) == 4 {
			state.viewport = [2]float64{box[2], box[3]}
		}
	} else if name == "svg" || name == "symbol" && used {
		if err := r.enterViewport(&state, attributes, size); err != nil {
			return err
		}
	} else if marker != nil {
		if state.overflow == "" {
			state.overflow = "hidden"
		}
		if !state.hidden && state.clipsOverflow() {
			if err := r.clipToViewport(&state, marker[0], marker[1], marker[2], marker[3]); err != nil {
				return err
			}
		}
	}
	r.stack = append(r.stack, state)
	defer func() { r.stack = r.stack[:len(r.stack)-1] }()

	if name == "text" {
		if !state.hidden {
			return r.addText(node, state, attributes)
		}
		return nil
	}
	if name == "use" {
		target := r.ids[svgHref(node)]
		if target != nil && !state.hidden && !r.reusing[target] {
			// The element is drawn as a child of the use element, moved by
			// its x and y.
			parent := state
			parent.matrix = svgMultiply(state.matrix, [6]float64{
				1, 0, 0, 1, parseSVGCoordinate(attributes["x"]), parseSVGCoordinate(attributes["y"])})
			r.stack = append(r.stack, parent)
			r.reusing[target] = true
			r.useSize = [2]string{attributes["width"], attributes["height"]}
			err := r.draw(target, true)
			delete(r.reusing, target)
			r.stack = r.stack[:len(r.stack)-1]
			if err != nil {
				return err
			}
		}
		return nil
	}
	operations, err := shapeOperations(name, attributes)
	if err != nil {
		return err
	}
	count := 0
	if len(r.reusing) > 0 {
		count = len(operations)
	}
	if !state.hidden && len(operations) > 0 {
		count += svgClipOperations(state.clips) + len(state.dashArray)
	}
	if err := r.countOperations(count); err != nil {
		return err
	}
	if name == "line" {
		state.fill = color.Transparent // A line has no inside to fill
		state.fillCurrent = false
	}
	var vertices [][3]float64
	if !state.hidden && !state.invisible && len(operations) > 0 &&
		(state.markerStart != "" || state.markerMid != "" || state.markerEnd != "") &&
		(name == "path" || name == "line" || name == "polyline" || name == "polygon") {
		vertices = svgVertices(operations) // Before addPath moves the points
	}
	image.addPath(&state, operations)
	if vertices != nil {
		if err := r.drawMarkers(&state, vertices); err != nil {
			return err
		}
	}

	if name == "switch" {
		return r.drawSwitch(node)
	}
	for _, child := range node.Children() {
		if err := r.draw(child, false); err != nil {
			return err
		}
	}
	if name == "style" {
		r.rules.add(parseSVGStyleSheet(node.Text()))
	}
	return nil
}

// svgDrawn are the elements PDFjet draws, and a switch can choose.
var svgDrawn = map[string]bool{
	"a": true, "circle": true, "ellipse": true, "g": true, "line": true, "path": true, "polygon": true,
	"polyline": true, "rect": true, "svg": true, "switch": true, "text": true, "use": true,
}

// isSVGSwitchable reports whether a switch can draw the element: one PDFjet
// draws, which asks for no extension, as PDFjet has none.
func isSVGSwitchable(node *xmlparser.XMLNode) bool {
	_, extensions := node.Attribute("requiredExtensions")
	return svgDrawn[node.LocalName()] && !extensions
}

// svgSwitch is a switch element: the children it can draw, up to the first
// with no systemLanguage, which is drawn when none before it is, and the
// languages of each, nil for no systemLanguage. A PDF has no reader's language,
// so the language is that of the PDF, and an image is read before it is known:
// each child is read, and the one that is drawn is chosen in DrawOn.
type svgSwitch struct {
	children  []*xmlparser.XMLNode
	languages [][]string
	chosen    int // The child drawn in the language of the PDF, or -1
}

// svgChoice is the child of a switch that an element is in.
type svgChoice struct {
	of    *svgSwitch
	child int
}

// newSVGSwitch returns the switch of the element.
func newSVGSwitch(node *xmlparser.XMLNode) *svgSwitch {
	choice := new(svgSwitch)
	for _, child := range node.Children() {
		if !isSVGSwitchable(child) {
			continue
		}
		choice.children = append(choice.children, child)
		value, ok := child.Attribute("systemLanguage")
		if !ok {
			choice.languages = append(choice.languages, nil)
			break // Nothing after it can be drawn
		}
		choice.languages = append(choice.languages, svgLanguages(value))
	}
	return choice
}

// svgLanguages returns the primary subtags of the languages of a
// systemLanguage, a list split by commas. That of an empty one is not nil, and
// has none: it matches no language, as SVG says.
func svgLanguages(value string) []string {
	languages := make([]string, 0)
	for _, language := range strings.Split(value, ",") {
		if strings.TrimSpace(language) != "" {
			languages = append(languages, svgPrimaryLanguage(language))
		}
	}
	return languages
}

// svgPrimaryLanguage returns the primary subtag of a language, en-US's en, in
// lower case, and en for no language.
func svgPrimaryLanguage(language string) string {
	language = strings.TrimSpace(language)
	if dash := strings.IndexByte(language, '-'); dash != -1 {
		language = language[:dash]
	}
	if language == "" {
		return "en"
	}
	return strings.ToLower(language)
}

// choose chooses the first child that has the language, or no systemLanguage.
func (choice *svgSwitch) choose(language string) {
	choice.chosen = -1
	for i, languages := range choice.languages {
		if languages == nil || slices.Contains(languages, language) {
			choice.chosen = i
			return
		}
	}
}

// drawSwitch reads the children of the switch that may be drawn. A switch that
// is drawn again by a use element is the same switch, so the number of switches
// is that of the file.
func (r *svgReader) drawSwitch(node *xmlparser.XMLNode) error {
	choice := r.switches[node]
	if choice == nil {
		choice = newSVGSwitch(node)
		r.switches[node] = choice
		r.image.switches = append(r.image.switches, choice)
	}
	top := len(r.stack) - 1
	choices := r.stack[top].choices
	for i, child := range choice.children {
		inChild := make([]svgChoice, 0, len(choices)+1)
		inChild = append(inChild, choices...)
		r.stack[top].choices = append(inChild, svgChoice{choice, i})
		if err := r.draw(child, false); err != nil {
			return err
		}
	}
	return nil
}

// chosenPaths returns the paths drawn in the language of the PDF, those in a
// child of a switch that is not chosen left out.
func (image *SVGImage) chosenPaths(language string) []*svgPath {
	if len(image.switches) == 0 {
		return image.paths
	}
	primary := svgPrimaryLanguage(language)
	for _, choice := range image.switches {
		choice.choose(primary)
	}
	paths := make([]*svgPath, 0, len(image.paths))
	for _, path := range image.paths {
		if path.isChosen() {
			paths = append(paths, path)
		}
	}
	return paths
}

// elementState returns the state of an element whose parent has the given
// one: its presentation attributes, in their order, then the rules of the
// style sheet for its classes, in the order of the style sheet, then its
// style attribute, and its transform.
func elementState(parent svgState, colorMap map[string]int32, rules *svgStyleSheet,
	attributes map[string]string, order []string) (svgState, error) {
	state := parent
	state.ownOpacity = 1.0
	state.clipPath = ""
	state.overflow = ""
	for _, name := range order {
		if err := state.setProperty(colorMap, name, attributes[name]); err != nil {
			return state, err
		}
	}
	ofClasses, err := rules.forClasses(strings.Fields(attributes["class"]))
	if err != nil {
		return state, err
	}
	for _, rule := range ofClasses {
		for _, declaration := range rule.declarations {
			if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
				return state, err
			}
		}
	}
	for _, declaration := range parseSVGDeclarations(attributes["style"]) {
		if err := state.setProperty(colorMap, declaration[0], declaration[1]); err != nil {
			return state, err
		}
	}
	if state.overflow == "inherit" {
		state.overflow = parent.overflow
		if state.overflow == "" {
			state.overflow = "visible" // What an element has unless given
		}
	}
	state.opacity = float32(state.opacity * state.ownOpacity)
	if transform, ok := attributes["transform"]; ok {
		if matrix, ok := parseSVGTransform(transform); ok {
			state.matrix = svgMultiply(parent.matrix, matrix)
		}
	}
	return state, nil
}

// svgKappa is how far the control points of the cubic curve that draws a
// quarter of a circle are from its ends, for a radius of 1.
const svgKappa = 0.5522847498307936

// svgBuilder builds the PDF path operations of a shape.
type svgBuilder struct {
	operations []*svgPathOp
	x0, y0     float64 // The start of the subpath
}

func (b *svgBuilder) moveTo(x, y float64) {
	b.x0 = x
	b.y0 = y
	b.operations = append(b.operations, newSVGPathOpXY('M', float32(x), float32(y)))
}

func (b *svgBuilder) lineTo(x, y float64) {
	b.operations = append(b.operations, newSVGPathOpXY('L', float32(x), float32(y)))
}

func (b *svgBuilder) curveTo(x1, y1, x2, y2, x, y float64) {
	op := newSVGPathOp('C')
	op.setCubicPoints(float32(x1), float32(y1), float32(x2), float32(y2), float32(x), float32(y))
	b.operations = append(b.operations, op)
}

func (b *svgBuilder) closePath() {
	b.operations = append(b.operations, newSVGPathOpXY('Z', float32(b.x0), float32(b.y0)))
}

// ellipse adds the ellipse as four cubic curves, from its right end.
func (b *svgBuilder) ellipse(cx, cy, rx, ry float64) {
	kx := float64(svgKappa * rx)
	ky := float64(svgKappa * ry)
	b.moveTo(cx+rx, cy)
	b.curveTo(cx+rx, cy+ky, cx+kx, cy+ry, cx, cy+ry)
	b.curveTo(cx-kx, cy+ry, cx-rx, cy+ky, cx-rx, cy)
	b.curveTo(cx-rx, cy-ky, cx-kx, cy-ry, cx, cy-ry)
	b.curveTo(cx+kx, cy-ry, cx+rx, cy-ky, cx+rx, cy)
	b.closePath()
}

// shapeOperations returns the PDF path operations of an element that draws a
// path or a basic shape, in its user space, and none for another element or
// a shape of no size. It returns an error for path data that is not numbers.
func shapeOperations(name string, attributes map[string]string) ([]*svgPathOp, error) {
	number := func(name string) float64 {
		return parseSVGCoordinate(attributes[name])
	}
	b := new(svgBuilder)
	switch name {
	case "path":
		data := attributes["d"]
		operations, err := toPDF(newSVGParser().getOperations(data))
		if err != nil {
			return nil, fmt.Errorf("invalid path data %q: %w", data, err)
		}
		// Path data starts with a moveto; data that does not is in error
		// from its start, and SVG 1.1 section 8.3.2 draws it up to there.
		if len(operations) > 0 && operations[0].cmd != 'M' {
			return nil, nil
		}
		return operations, nil
	case "rect":
		x, y, w, h := number("x"), number("y"), number("width"), number("height")
		if w <= 0.0 || h <= 0.0 {
			return nil, nil
		}
		// A radius that is not given is the other one, and neither is more
		// than half the side.
		rx, rxSet := radius(attributes["rx"])
		ry, rySet := radius(attributes["ry"])
		if !rxSet {
			rx = ry
		}
		if !rySet {
			ry = rx
		}
		rx = math.Min(rx, float64(w/2.0))
		ry = math.Min(ry, float64(h/2.0))
		if rx <= 0.0 || ry <= 0.0 {
			b.moveTo(x, y)
			b.lineTo(x+w, y)
			b.lineTo(x+w, y+h)
			b.lineTo(x, y+h)
			b.closePath()
			return b.operations, nil
		}
		kx := float64(svgKappa * rx)
		ky := float64(svgKappa * ry)
		b.moveTo(x+rx, y)
		b.lineTo(x+w-rx, y)
		b.curveTo(x+w-rx+kx, y, x+w, y+ry-ky, x+w, y+ry)
		b.lineTo(x+w, y+h-ry)
		b.curveTo(x+w, y+h-ry+ky, x+w-rx+kx, y+h, x+w-rx, y+h)
		b.lineTo(x+rx, y+h)
		b.curveTo(x+rx-kx, y+h, x, y+h-ry+ky, x, y+h-ry)
		b.lineTo(x, y+ry)
		b.curveTo(x, y+ry-ky, x+rx-kx, y, x+rx, y)
		b.closePath()
	case "circle":
		r := number("r")
		if r <= 0.0 {
			return nil, nil
		}
		b.ellipse(number("cx"), number("cy"), r, r)
	case "ellipse":
		rx, ry := number("rx"), number("ry")
		if rx <= 0.0 || ry <= 0.0 {
			return nil, nil
		}
		b.ellipse(number("cx"), number("cy"), rx, ry)
	case "line":
		b.moveTo(number("x1"), number("y1"))
		b.lineTo(number("x2"), number("y2"))
	case "polyline", "polygon":
		points, err := parseSVGNumbers(attributes["points"])
		if err != nil || len(points) < 4 {
			return nil, nil // Drawn as far as it can be read, which is not a line
		}
		b.moveTo(points[0], points[1])
		for i := 2; i+1 < len(points); i += 2 {
			b.lineTo(points[i], points[i+1])
		}
		if name == "polygon" {
			b.closePath()
		}
	}
	return b.operations, nil
}

// radius reads the rx or the ry of a rectangle; ok is false when it is not
// given, is auto or is not a length of zero or more.
func radius(value string) (float64, bool) {
	value = strings.TrimSpace(value)
	if value == "" || value == "auto" {
		return 0.0, false
	}
	length, err := parseSVGLength(value)
	if err != nil || length < 0.0 || strings.HasSuffix(value, "%") {
		return 0.0, false
	}
	return float64(length), true
}

// addPath adds the operations of a path or a shape to the image, in the space
// of the svg element, with what the state draws them with, unless they draw
// nothing: a shape in <defs> or of no size, or with neither a fill nor a stroke.
func (image *SVGImage) addPath(state *svgState, operations []*svgPathOp) {
	m := state.matrix
	det := float64(m[0]*m[3]) - float64(m[1]*m[2])
	if state.hidden || state.invisible || len(operations) == 0 || det == 0.0 {
		return
	}
	path := new(svgPath)
	path.operations = operations
	path.fill = state.fill
	if state.fillCurrent {
		path.fill = state.color
	}
	path.stroke = state.stroke
	if state.strokeCurrent {
		path.stroke = state.color
	}
	path.fillAlpha = state.drawnFillAlpha()
	path.strokeAlpha = state.drawnStrokeAlpha()
	if path.fillAlpha == 0.0 {
		path.fill = color.Transparent
	}
	if path.strokeAlpha == 0.0 || state.strokeWidth == 0.0 {
		path.stroke = color.Transparent
	}
	if path.fill == color.Transparent && path.stroke == color.Transparent {
		return
	}
	scale := math.Sqrt(math.Abs(det))
	path.strokeWidth = float32(float64(float64(state.strokeWidth) * scale))
	path.evenOdd = state.evenOdd
	path.lineCap = state.lineCap
	path.lineJoin = state.lineJoin
	path.miterLimit = state.miterLimit
	path.strokeFirst = state.strokeFirst
	if state.dashArray != nil {
		path.dashArray = make([]float32, len(state.dashArray))
		for i, length := range state.dashArray {
			path.dashArray[i] = float32(float64(float64(length) * scale))
		}
		path.dashOffset = float32(float64(float64(state.dashOffset) * scale))
	}
	path.clips = state.clips
	path.choices = state.choices
	svgTransformAll(m, operations)
	image.paths = append(image.paths, path)
}

// svgTransformAll moves the points of the operations by the matrix.
func svgTransformAll(m [6]float64, operations []*svgPathOp) {
	if m == svgIdentity {
		return
	}
	for _, op := range operations {
		op.x, op.y = svgTransform(m, op.x, op.y)
		if op.cmd == 'C' {
			op.x1, op.y1 = svgTransform(m, op.x1, op.y1)
			op.x2, op.y2 = svgTransform(m, op.x2, op.y2)
		}
	}
}

// clip sets the clip paths of the element: those of its ancestors and the one
// its clip-path names, the shapes of the clipPath element in the user space
// of the element. A use in it names a shape, moved by its x and y. A clip path
// of one shape keeps the clip-rule of the shape; several are one path whose
// nonzero rule draws their union, each turned the same way round, as opposite
// ways would cancel out where they overlap. A clip path with no shape clips
// with its texts, which are read again, as their matrices are, for each element
// it clips. A clip path that is not there, or one in objectBoundingBox units,
// which PDFjet does not draw, is left out; one with neither a shape nor a glyph
// hides the element, as SVG clips all of it away.
func (r *svgReader) clip(state *svgState) error {
	clipPath := r.ids[state.clipPath]
	if clipPath == nil || clipPath.LocalName() != "clipPath" ||
		strings.TrimSpace(svgAttribute(clipPath, "clipPathUnits")) == "objectBoundingBox" {
		return nil
	}
	base := newSVGState()
	base.matrix = state.matrix
	attributes, order := svgAttributes(clipPath)
	base, err := elementState(base, r.colorMap, r.rules, attributes, order)
	if err != nil {
		return err
	}
	base.hidden = false // A clipPath element is not displayed anyway
	clip := new(svgPath)
	clip.operations = make([]*svgPathOp, 0)
	shapes := make([][]*svgPathOp, 0)
	texts := make([]*svgPath, 0)
	read := 0 // What is read of the texts
	for _, child := range clipPath.Children() {
		attributes, order := svgAttributes(child)
		shape, err := elementState(base, r.colorMap, r.rules, attributes, order)
		if err != nil {
			return err
		}
		node := child
		name := child.LocalName()
		if name == "use" {
			target := r.ids[svgHref(child)]
			if target == nil {
				continue
			}
			shape.matrix = svgMultiply(shape.matrix, [6]float64{
				1, 0, 0, 1, parseSVGCoordinate(attributes["x"]), parseSVGCoordinate(attributes["y"])})
			attributes, order = svgAttributes(target)
			shape, err = elementState(shape, r.colorMap, r.rules, attributes, order)
			if err != nil {
				return err
			}
			node = target
			name = target.LocalName()
		}
		if shape.hidden || shape.invisible {
			continue
		}
		if name == "text" {
			r.clipping = true
			text, err := r.readText(node, shape, attributes)
			r.clipping = false
			if err != nil {
				return err
			}
			read += text.operations()
			m := shape.matrix
			if text.clips() && float64(m[0]*m[3])-float64(m[1]*m[2]) != 0.0 {
				part := new(svgPath)
				part.text = text
				texts = append(texts, part)
			}
			continue
		}
		operations, err := shapeOperations(name, attributes)
		if err != nil {
			return err
		}
		if len(operations) == 0 {
			continue
		}
		svgTransformAll(shape.matrix, operations)
		shapes = append(shapes, operations)
		clip.evenOdd = shape.clipEvenOdd
	}
	for _, operations := range shapes {
		if len(shapes) > 1 {
			clip.evenOdd = false
			if svgArea(operations) < 0.0 {
				operations = svgReversed(operations)
			}
		}
		clip.operations = append(clip.operations, operations...)
	}
	if err := r.countOperations(len(clipPath.Children()) + len(clip.operations) + read); err != nil {
		return err
	}
	if len(shapes) == 0 {
		clip.parts = texts
		clip.glyphs = read
		r.image.clipShapes = append(r.image.clipShapes, texts...)
	}
	if len(clip.operations) == 0 && len(clip.parts) == 0 {
		state.hidden = true
		return nil
	}
	r.addClip(state, clip)
	return nil
}

// addClip adds the clip path to those of the element.
func (r *svgReader) addClip(state *svgState, clip *svgPath) {
	r.image.clipShapes = append(r.image.clipShapes, clip)
	clips := make([]*svgPath, 0, len(state.clips)+1)
	clips = append(clips, state.clips...)
	state.clips = append(clips, clip)
}

// svgArea returns twice the area of the path, its sign the way it goes round,
// by the points of its lines and curves, and closed.
func svgArea(operations []*svgPathOp) float64 {
	area := 0.0
	var x0, y0, x, y float64
	line := func(px, py float64) {
		area += float64(x*py) - float64(px*y)
		x = px
		y = py
	}
	for _, op := range operations {
		if op.cmd == 'M' {
			line(x0, y0)
			x0 = float64(op.x)
			y0 = float64(op.y)
			x = x0
			y = y0
			continue
		}
		if op.cmd == 'C' {
			line(float64(op.x1), float64(op.y1))
			line(float64(op.x2), float64(op.y2))
		}
		line(float64(op.x), float64(op.y))
	}
	line(x0, y0)
	return area
}

// svgReversed returns the path going the other way round, each subpath from
// its end. Only a clip path needs this, whose subpaths close of themselves.
func svgReversed(operations []*svgPathOp) []*svgPathOp {
	reversed := make([]*svgPathOp, 0, len(operations))
	start := 0
	for end := 1; end <= len(operations); end++ {
		if end < len(operations) && operations[end].cmd != 'M' {
			continue
		}
		last := operations[end-1]
		reversed = append(reversed, newSVGPathOpXY('M', last.x, last.y))
		for i := end - 1; i > start; i-- {
			op := operations[i]
			before := operations[i-1]
			if op.cmd == 'C' {
				curve := newSVGPathOp('C')
				curve.setCubicPoints(op.x2, op.y2, op.x1, op.y1, before.x, before.y)
				reversed = append(reversed, curve)
			} else {
				reversed = append(reversed, newSVGPathOpXY('L', before.x, before.y))
			}
		}
		start = end
	}
	return reversed
}

// enterViewport sets the user space of a nested svg element, or of a symbol a use
// element draws: moved by its x and y, and its viewBox fitted to its width
// and height, which the use element may give, and which are those of the
// viewport it is in unless given; a percentage is of that viewport. A
// viewBox or a size of nothing draws nothing. What is drawn is clipped to the
// viewport, as hidden is the overflow of these elements unless given or
// inherit, the value of the parent; a viewport of no known size clips nothing.
func (r *svgReader) enterViewport(state *svgState, attributes map[string]string, size [2]string) error {
	if state.overflow == "" {
		state.overflow = "hidden"
	}
	width := attributes["width"]
	if size[0] != "" {
		width = size[0]
	}
	height := attributes["height"]
	if size[1] != "" {
		height = size[1]
	}
	w := svgViewportLength(width, state.viewport[0], state.viewport[0])
	h := svgViewportLength(height, state.viewport[1], state.viewport[1])
	x := svgViewportLength(attributes["x"], state.viewport[0], 0.0)
	y := svgViewportLength(attributes["y"], state.viewport[1], 0.0)
	m := [6]float64{1, 0, 0, 1, x, y}
	box, err := parseSVGNumbers(attributes["viewBox"])
	fitted := err == nil && len(box) == 4
	// A viewport of no known size, of an svg element with neither a size nor
	// a viewBox, leaves the viewBox as it is.
	if fitted && w == 0.0 && width == "" {
		w = box[2]
	}
	if fitted && h == 0.0 && height == "" {
		h = box[3]
	}
	if fitted && (box[2] <= 0.0 || box[3] <= 0.0) || width != "" && w <= 0.0 || height != "" && h <= 0.0 {
		state.hidden = true
		return nil
	}
	if !state.hidden && w > 0.0 && h > 0.0 && state.clipsOverflow() {
		if err := r.clipToViewport(state, x, y, w, h); err != nil {
			return err
		}
	}
	if !fitted {
		state.viewport = [2]float64{w, h}
		state.matrix = svgMultiply(state.matrix, m)
		return nil
	}
	sx, sy, tx, ty := svgFit(box, w, h, attributes["preserveAspectRatio"])
	m = svgMultiply(m, [6]float64{sx, 0, 0, sy, tx, ty})
	m = svgMultiply(m, [6]float64{1, 0, 0, 1, -box[0], -box[1]})
	state.viewport = [2]float64{box[2], box[3]}
	state.matrix = svgMultiply(state.matrix, m)
	return nil
}

// svgFit returns the scales and the move that fit a viewBox of a size to a
// viewport of the width and the height, as its preserveAspectRatio says.
func svgFit(box []float64, w, h float64, aspect string) (sx, sy, tx, ty float64) {
	sx = w / box[2]
	sy = h / box[3]
	fields := strings.Fields(aspect)
	if len(fields) > 0 && fields[0] == "defer" {
		fields = fields[1:]
	}
	align := "xMidYMid"
	if len(fields) > 0 {
		align = fields[0]
	}
	if align != "none" {
		sx = math.Min(sx, sy)
		if len(fields) > 1 && fields[1] == "slice" {
			sx = math.Max(w/box[2], h/box[3])
		}
		sy = sx
		if len(align) != 8 {
			align = "xMidYMid"
		}
		tx = float64(float64(alignOffset(align[1:4], 1.0)) * (w - float64(box[2]*sx)))
		ty = float64(float64(alignOffset(align[5:8], 1.0)) * (h - float64(box[3]*sy)))
	}
	return sx, sy, tx, ty
}

// clipToViewport clips the element to the rectangle, in the user space it is
// in before it enters its viewport.
func (r *svgReader) clipToViewport(state *svgState, x, y, w, h float64) error {
	b := new(svgBuilder)
	b.moveTo(x, y)
	b.lineTo(x+w, y)
	b.lineTo(x+w, y+h)
	b.lineTo(x, y+h)
	b.closePath()
	svgTransformAll(state.matrix, b.operations)
	clip := new(svgPath)
	clip.operations = b.operations
	r.addClip(state, clip)
	return r.countOperations(len(clip.operations))
}

// svgViewportLength reads an x, a y, a width or a height of a viewport, which
// may be a percentage of the size of the one it is in, the given one if it has
// none.
func svgViewportLength(value string, whole, given float64) float64 {
	value = strings.TrimSpace(value)
	if value == "" {
		return given
	}
	if strings.HasSuffix(value, "%") {
		number, err := parseSVGFloat(strings.TrimSuffix(value, "%"), 64)
		if err != nil || math.IsNaN(number) || math.IsInf(number, 0) {
			return given
		}
		return float64(number*whole) / 100.0
	}
	return parseSVGCoordinate(value)
}

// svgVertices returns the vertices of the path, where its markers are drawn,
// in its user space: x, y and the angle of the path there, in degrees, which
// is halfway between the directions in and out of a vertex in the middle.
func svgVertices(operations []*svgPathOp) [][3]float64 {
	vertices := make([][6]float64, 0) // x, y, in x, in y, out x, out y
	x := 0.0
	y := 0.0
	start := 0 // The vertex where the subpath starts
	for _, op := range operations {
		var in, out [2]float64
		if op.cmd == 'M' {
			start = len(vertices)
			vertices = append(vertices, [6]float64{float64(op.x), float64(op.y), 0, 0, 0, 0})
			x = float64(op.x)
			y = float64(op.y)
			continue
		} else if op.cmd == 'C' {
			out = svgDirection(x, y, float64(op.x1), float64(op.y1), float64(op.x2), float64(op.y2),
				float64(op.x), float64(op.y))
			in = svgDirection(float64(op.x), float64(op.y), float64(op.x2), float64(op.y2),
				float64(op.x1), float64(op.y1), x, y)
			in = [2]float64{-in[0], -in[1]}
		} else {
			// A line, or Z, the line back to the start of the subpath.
			out = [2]float64{float64(op.x) - x, float64(op.y) - y}
			in = out
		}
		if len(vertices) == 0 {
			continue
		}
		last := &vertices[len(vertices)-1]
		if last[4] == 0.0 && last[5] == 0.0 {
			last[4] = out[0]
			last[5] = out[1]
		}
		vertices = append(vertices, [6]float64{float64(op.x), float64(op.y), in[0], in[1], 0, 0})
		if op.cmd == 'Z' {
			// The vertex that closes the subpath goes on as its start does.
			closing := &vertices[len(vertices)-1]
			closing[4] = vertices[start][4]
			closing[5] = vertices[start][5]
		}
		x = float64(op.x)
		y = float64(op.y)
	}
	result := make([][3]float64, 0, len(vertices))
	for _, v := range vertices {
		inLength := math.Hypot(v[2], v[3])
		outLength := math.Hypot(v[4], v[5])
		dx := 0.0
		dy := 0.0
		if inLength > 0.0 {
			dx += v[2] / inLength
			dy += v[3] / inLength
		}
		if outLength > 0.0 {
			dx += v[4] / outLength
			dy += v[5] / outLength
		}
		if dx == 0.0 && dy == 0.0 && inLength > 0.0 {
			dx = v[2] // A path that turns back: the direction in
			dy = v[3]
		}
		result = append(result, [3]float64{v[0], v[1], svgDegrees(math.Atan2(dy, dx))})
	}
	return result
}

// svgDegrees and svgRadians convert an angle as Java's Math.toDegrees and
// Math.toRadians do, by one multiplication.
func svgDegrees(radians float64) float64 {
	return float64(radians * 57.29577951308232)
}

func svgRadians(degrees float64) float64 {
	return float64(degrees * 0.017453292519943295)
}

// svgDirection returns the direction from x, y to the first of the points
// that is not there, as a curve leaves its start.
func svgDirection(x, y float64, points ...float64) [2]float64 {
	for i := 0; i+1 < len(points); i += 2 {
		if points[i] != x || points[i+1] != y {
			return [2]float64{points[i] - x, points[i+1] - y}
		}
	}
	return [2]float64{0, 0}
}

// drawMarkers draws the markers of a path at its vertices: the start marker
// at the first, the end marker at the last and the mid marker at the others.
func (r *svgReader) drawMarkers(state *svgState, vertices [][3]float64) error {
	last := len(vertices) - 1
	for i := 0; i <= last; i++ {
		if i == 0 {
			if err := r.drawMarker(state.markerStart, state, vertices[i], true); err != nil {
				return err
			}
		}
		if i > 0 && i < last {
			if err := r.drawMarker(state.markerMid, state, vertices[i], false); err != nil {
				return err
			}
		}
		if i == last {
			if err := r.drawMarker(state.markerEnd, state, vertices[i], false); err != nil {
				return err
			}
		}
	}
	return nil
}

// drawMarker draws the marker at the vertex: its refX, refY there, turned as
// its orient says, and its viewBox fitted to its markerWidth and
// markerHeight, which are in units of the stroke width unless its
// markerUnits are userSpaceOnUse. A size of nothing draws nothing. What is
// drawn is clipped to that viewport, as hidden is the overflow of a marker
// unless given, and the viewport is where the viewBox fitted to it leaves it.
func (r *svgReader) drawMarker(id string, state *svgState, vertex [3]float64, start bool) error {
	if id == "" {
		return nil
	}
	marker := r.ids[id]
	if marker == nil || marker.LocalName() != "marker" || r.reusing[marker] {
		return nil
	}
	orient := strings.TrimSpace(svgAttribute(marker, "orient"))
	angle := 0.0
	if orient == "auto" {
		angle = vertex[2]
	} else if orient == "auto-start-reverse" {
		angle = vertex[2]
		if start {
			angle = vertex[2] + 180.0
		}
	} else {
		orient = strings.TrimSuffix(orient, "deg")
		number, err := parseSVGFloat(strings.TrimSpace(orient), 64)
		if err == nil && !math.IsNaN(number) && !math.IsInf(number, 0) {
			angle = number
		}
	}
	units := 1.0
	if strings.TrimSpace(svgAttribute(marker, "markerUnits")) != "userSpaceOnUse" {
		units = float64(state.strokeWidth)
	}
	width := svgMarkerSize(svgAttribute(marker, "markerWidth"))
	height := svgMarkerSize(svgAttribute(marker, "markerHeight"))
	if width == 0.0 || height == 0.0 {
		return nil
	}
	sx := 1.0
	sy := 1.0
	viewport := []float64{0, 0, width, height}
	if box, err := parseSVGNumbers(svgAttribute(marker, "viewBox")); err == nil && len(box) == 4 {
		if box[2] <= 0.0 || box[3] <= 0.0 {
			return nil // A viewBox of no size draws nothing
		}
		var tx, ty float64
		sx, sy, tx, ty = svgFit(box, width, height, svgAttribute(marker, "preserveAspectRatio"))
		viewport = []float64{box[0] - tx/sx, box[1] - ty/sy, width / sx, height / sy}
	}
	radians := svgRadians(angle)
	cos := math.Cos(radians)
	sin := math.Sin(radians)
	m := svgMultiply(state.matrix, [6]float64{cos, sin, -sin, cos, vertex[0], vertex[1]})
	m = svgMultiply(m, [6]float64{float64(units * sx), 0, 0, float64(units * sy), 0, 0})
	m = svgMultiply(m, [6]float64{
		1, 0, 0, 1, -parseSVGCoordinate(svgAttribute(marker, "refX")),
		-parseSVGCoordinate(svgAttribute(marker, "refY"))})
	// The marker inherits from the svg element, not from the path.
	base := newSVGState()
	base.paints = state.paints
	base.clips = state.clips
	base.choices = state.choices
	base.matrix = m
	r.stack = append(r.stack, base)
	r.reusing[marker] = true
	r.marker = viewport
	err := r.draw(marker, true)
	delete(r.reusing, marker)
	r.stack = r.stack[:len(r.stack)-1]
	return err
}

// svgMarkerSize reads the markerWidth or the markerHeight: 3 unless given.
func svgMarkerSize(value string) float64 {
	if strings.TrimSpace(value) == "" {
		return 3.0
	}
	size := parseSVGCoordinate(value)
	if size > 0.0 {
		return size
	}
	return 0.0
}

// addText adds a text element, with its tspan elements, as the runs of its
// characters.
func (r *svgReader) addText(node *xmlparser.XMLNode, state svgState, attributes map[string]string) error {
	text, err := r.readText(node, state, attributes)
	if err != nil {
		return err
	}
	empty := true
	count := 0
	for _, run := range text.runs {
		if run.text != "" {
			empty = false
		}
		count += svgClipOperations(state.clips)
		if len(r.reusing) > 0 {
			count += 1 + utf8.RuneCountInString(run.text)
		}
	}
	if err := r.countOperations(count); err != nil {
		return err
	}
	m := state.matrix
	if empty || float64(m[0]*m[3])-float64(m[1]*m[2]) == 0.0 {
		return nil
	}
	path := new(svgPath)
	path.operations = make([]*svgPathOp, 0)
	path.text = text
	path.clips = state.clips
	path.choices = state.choices
	r.image.paths = append(r.image.paths, path)
	return nil
}

// readText returns the text of a text element, as its runs.
func (r *svgReader) readText(node *xmlparser.XMLNode, state svgState, attributes map[string]string) (*svgText, error) {
	text := new(svgText)
	text.matrix = state.matrix
	pending := [4]float64{math.NaN(), math.NaN(), 0, 0}
	if err := r.addRuns(text, node, state, attributes, &pending, len(r.stack)); err != nil {
		return nil, err
	}
	text.collapseSpaces()
	return text, nil
}

// addRuns adds the runs of a text or a tspan element. Its x, y, dx and dy
// apply to its first character, which may be in a tspan in it; they are kept
// in pending until then. A tspan counts as a level of the depth of the elements.
func (r *svgReader) addRuns(text *svgText, node *xmlparser.XMLNode, state svgState,
	attributes map[string]string, pending *[4]float64, depth int) error {
	if depth > maxSVGDepth {
		return errSVGDepth()
	}
	if r.clipping {
		if err := r.countOperations(1); err != nil {
			return err
		}
	}
	x := svgTextLength(attributes["x"], state.fontSize)
	y := svgTextLength(attributes["y"], state.fontSize)
	dx := svgTextLength(attributes["dx"], state.fontSize)
	dy := svgTextLength(attributes["dy"], state.fontSize)
	if !math.IsNaN(x) {
		pending[0] = x
		pending[2] = 0.0
	}
	if !math.IsNaN(y) {
		pending[1] = y
		pending[3] = 0.0
	}
	if !math.IsNaN(dx) {
		pending[2] += dx
	}
	if !math.IsNaN(dy) {
		pending[3] += dy
	}
	for _, item := range node.Content() {
		switch item := item.(type) {
		case string:
			run := &svgTextRun{
				text:        item,
				x:           pending[0],
				y:           pending[1],
				dx:          pending[2],
				dy:          pending[3],
				fontSize:    state.fontSize,
				weight:      state.fontWeight,
				italic:      state.italic,
				family:      state.fontFamily,
				anchor:      state.textAnchor,
				baseline:    state.baseline,
				fill:        state.fill,
				stroke:      state.stroke,
				fillAlpha:   state.drawnFillAlpha(),
				strokeAlpha: state.drawnStrokeAlpha(),
				strokeWidth: state.strokeWidth,
				strokeFirst: state.strokeFirst,
				lineJoin:    state.lineJoin,
				miterLimit:  state.miterLimit,
			}
			if state.fillCurrent {
				run.fill = state.color
			}
			if state.strokeCurrent {
				run.stroke = state.color
			}
			if state.invisible {
				// It keeps its place in the text
				run.fill = color.Transparent
				run.stroke = color.Transparent
				run.invisible = true
			}
			*pending = [4]float64{math.NaN(), math.NaN(), 0, 0}
			text.runs = append(text.runs, run)
		case *xmlparser.XMLNode:
			name := item.LocalName()
			if name != "tspan" && name != "a" {
				continue // A title or a desc is not drawn
			}
			childAttributes, childOrder := svgAttributes(item)
			childState, err := elementState(state, r.colorMap, r.rules, childAttributes, childOrder)
			if err != nil {
				return err
			}
			childState.matrix = state.matrix // A tspan has no transform
			if !childState.hidden {
				if err := r.addRuns(text, item, childState, childAttributes, pending, depth+1); err != nil {
					return err
				}
			}
		}
	}
	return nil
}

// svgTextLength reads the first length of an x, y, dx or dy of a text, which
// may be in em of its font size, and returns NaN for none.
func svgTextLength(value string, fontSize float32) float64 {
	fields := strings.Fields(strings.ReplaceAll(value, ",", " "))
	if len(fields) == 0 {
		return math.NaN()
	}
	first := fields[0]
	var length float64
	if strings.HasSuffix(first, "em") {
		number, err := parseSVGFloat(first[:len(first)-2], 64)
		if err != nil {
			return math.NaN()
		}
		length = float64(number * float64(fontSize))
	} else if strings.HasSuffix(first, "%") {
		return math.NaN()
	} else {
		points, err := parseSVGLength(first)
		if err != nil {
			return math.NaN()
		}
		length = float64(points)
	}
	if math.IsInf(length, 0) {
		return math.NaN()
	}
	return length
}

// svgTransform returns the point that the matrix takes the point to.
func svgTransform(m [6]float64, x, y float32) (float32, float32) {
	return float32(float64(m[0]*float64(x)) + float64(m[2]*float64(y)) + m[4]),
		float32(float64(m[1]*float64(x)) + float64(m[3]*float64(y)) + m[5])
}

// svgUnits are the units of a length of an SVG file, in points. A number
// without a unit is in the user unit of the file, which PDFjet draws as a
// point, and so is a number in px.
var svgUnits = []struct {
	suffix string
	points float32
}{
	{"px", 1.0},
	{"pt", 1.0},
	{"pc", 12.0},
	{"in", 72.0},
	{"mm", 72.0 / 25.4},
	{"cm", 72.0 / 2.54},
}

// svgPixel is a user unit of an svg element whose size is in a unit such as mm
// and that has no viewBox, in points. Such a size is the size of its pixels, 96
// in an inch, and what is in it is drawn in those.
const svgPixel = 0.75

// svgHasUnit reports whether the length has a unit that is not a pixel.
func svgHasUnit(value string) bool {
	value = strings.TrimSpace(value)
	for _, unit := range svgUnits {
		if unit.suffix != "px" && strings.HasSuffix(value, unit.suffix) {
			return true
		}
	}
	return false
}

// parseLength returns the width or the height of the svg element in points,
// and 0 for a length that PDFjet cannot read, a percentage among them, which
// leaves the size to the viewBox.
func parseLength(value string) float32 {
	value = strings.TrimSpace(value)
	scale := float32(1.0)
	for _, unit := range svgUnits {
		if strings.HasSuffix(value, unit.suffix) {
			value = strings.TrimSpace(strings.TrimSuffix(value, unit.suffix))
			scale = unit.points
			break
		}
	}
	number, err := parseSVGFloat(value, 32)
	if err != nil || math.IsInf(number, 0) || math.IsNaN(number) {
		return 0.0
	}
	return float32(float32(number) * scale)
}

// parseFloatLenient parses a numeric attribute value, treating an empty
// value as 0 — consistent with how an omitted attribute is handled.
func parseFloatLenient(value string) (float32, error) {
	value = strings.TrimSpace(value)
	if value == "" {
		return 0.0, nil
	}
	v, err := parseSVGFloat(value, 32)
	if err != nil {
		return 0.0, fmt.Errorf("%q: %w", value, err)
	}
	return float32(v), nil
}

func (image *SVGImage) processPaths(paths []*svgPath) error {
	if image.viewBox == "" && !image.inUnits {
		return nil
	}
	var box [4]float32
	if image.inUnits {
		box[2] = image.w / svgPixel
		box[3] = image.h / svgPixel
	} else {
		list, err := parseSVGNumbers(image.viewBox)
		if err != nil || len(list) != 4 {
			return fmt.Errorf("Invalid SVG viewBox %q: four numbers are needed.", image.viewBox)
		}
		for i := range box {
			box[i] = float32(list[i])
		}
		if box[2] <= 0 || box[3] <= 0 {
			return fmt.Errorf("Invalid SVG viewBox %q: its width and height have to be above zero.", image.viewBox)
		}
	}
	// A size the file does not give, or one in a unit PDFjet cannot read, is
	// the other one in the proportions of the viewBox, or the size of the
	// viewBox when both are missing, where scaling it by a width of zero would
	// draw every path at the origin.
	if image.w == 0.0 && image.h == 0.0 {
		image.w = box[2]
		image.h = box[3]
	} else if image.w == 0.0 {
		image.w = float32(image.h*box[2]) / box[3]
	} else if image.h == 0.0 {
		image.h = float32(image.w*box[3]) / box[2]
	}

	// The viewBox is scaled to the size, and, unless preserveAspectRatio is
	// none, by the same factor across and down, as large as it fits (meet) or
	// as small as it covers (slice), and placed as it says, in the middle by
	// default.
	sx := image.w / box[2]
	sy := image.h / box[3]
	var tx, ty float32
	fields := strings.Fields(image.preserveAspectRatio)
	if len(fields) > 0 && fields[0] == "defer" {
		fields = fields[1:]
	}
	align := "xMidYMid"
	if len(fields) > 0 {
		align = fields[0]
	}
	uniform := align != "none" && sx != sy
	if uniform {
		scale := float32(math.Min(float64(sx), float64(sy)))
		if len(fields) > 1 && fields[1] == "slice" {
			scale = float32(math.Max(float64(sx), float64(sy)))
		}
		sx = scale
		sy = scale
		if len(align) != 8 {
			align = "xMidYMid"
		}
		tx = alignOffset(align[1:4], image.w-float32(box[2]*scale))
		ty = alignOffset(align[5:8], image.h-float32(box[3]*scale))
	}
	strokeScale := float32(math.Sqrt(float64(float64(sx) * float64(sy))))
	all := append(append(make([]*svgPath, 0, len(paths)+len(image.clipShapes)), paths...), image.clipShapes...)
	for _, path := range all {
		path.strokeWidth = float32(path.strokeWidth * strokeScale)
		if path.dashArray != nil {
			for i := range path.dashArray {
				path.dashArray[i] = float32(path.dashArray[i] * strokeScale)
			}
			path.dashOffset = float32(path.dashOffset * strokeScale)
		}
		if path.text != nil {
			m := path.text.matrix
			ax := float64(image.w / box[2])
			ay := float64(image.h / box[3])
			ox := 0.0
			oy := 0.0
			if uniform {
				ax = float64(sx)
				ay = float64(sy)
				ox = float64(tx)
				oy = float64(ty)
			}
			path.text.matrix = [6]float64{
				float64(m[0] * ax), float64(m[1] * ay), float64(m[2] * ax), float64(m[3] * ay),
				float64((m[4]-float64(box[0]))*ax) + ox, float64((m[5]-float64(box[1]))*ay) + oy}
		}
		for _, op := range path.operations {
			if uniform {
				op.x = float32((op.x-box[0])*sx) + tx
				op.y = float32((op.y-box[1])*sy) + ty
				op.x1 = float32((op.x1-box[0])*sx) + tx
				op.y1 = float32((op.y1-box[1])*sy) + ty
				op.x2 = float32((op.x2-box[0])*sx) + tx
				op.y2 = float32((op.y2-box[1])*sy) + ty
			} else {
				op.x = float32((op.x-box[0])*image.w) / box[2]
				op.y = float32((op.y-box[1])*image.h) / box[3]
				op.x1 = float32((op.x1-box[0])*image.w) / box[2]
				op.y1 = float32((op.y1-box[1])*image.h) / box[3]
				op.x2 = float32((op.x2-box[0])*image.w) / box[2]
				op.y2 = float32((op.y2-box[1])*image.h) / box[3]
			}
		}
	}
	return nil
}

// alignOffset returns how far the viewBox is moved across or down in the
// space left over by it: none for Min, half for Mid and all for Max.
func alignOffset(align string, space float32) float32 {
	switch align {
	case "Min":
		return 0.0
	case "Max":
		return space
	}
	return float32(space / 2.0)
}

// ScaleBy scales this svgParser image by the specified factor.
func (image *SVGImage) ScaleBy(factor float32) *SVGImage {
	all := append(append(make([]*svgPath, 0, len(image.paths)+len(image.clipShapes)), image.paths...), image.clipShapes...)
	for _, path := range all {
		path.strokeWidth = float32(path.strokeWidth * factor)
		if path.dashArray != nil {
			for i := range path.dashArray {
				path.dashArray[i] = float32(path.dashArray[i] * factor)
			}
			path.dashOffset = float32(path.dashOffset * factor)
		}
		if path.text != nil {
			m := path.text.matrix
			f := float64(factor)
			path.text.matrix = [6]float64{
				float64(m[0] * f), float64(m[1] * f), float64(m[2] * f),
				float64(m[3] * f), float64(m[4] * f), float64(m[5] * f)}
		}
		for _, op := range path.operations {
			op.x1 = float32(op.x1 * factor)
			op.y1 = float32(op.y1 * factor)
			op.x2 = float32(op.x2 * factor)
			op.y2 = float32(op.y2 * factor)
			op.x = float32(op.x * factor)
			op.y = float32(op.y * factor)
		}
	}
	image.w = float32(image.w * factor)
	image.h = float32(image.h * factor)
	return image
}

// SetLocation sets the location of the top left corner of this image on the page.
func (image *SVGImage) SetLocation(x, y float32) Drawable {
	image.x = x
	image.y = y
	return image
}

// SetURIAction sets the URI for the "click box" action.
func (image *SVGImage) SetURIAction(uri string) *SVGImage {
	image.uri = uri
	return image
}

// SetGoToAction sets the destination key for the action.
func (image *SVGImage) SetGoToAction(key string) *SVGImage {
	image.key = key
	return image
}

// SetAltDescription sets the alternate description of this image.
func (image *SVGImage) SetAltDescription(altDescription string) *SVGImage {
	image.altDescription = altDescription
	return image
}

// SetActualText sets the actual text of this image.
func (image *SVGImage) SetActualText(actualText string) *SVGImage {
	image.actualText = actualText
	return image
}

// SetLanguage sets the language of this image, for example "en-US".
func (image *SVGImage) SetLanguage(language string) *SVGImage {
	image.language = language
	return image
}

// SetFont sets the font of the text of the image, in place of the fonts
// PDFjet carries: for the text of a language they do not have, such as
// Chinese, or for a font of one's own.
func (image *SVGImage) SetFont(font *Font) *SVGImage {
	image.font = font
	return image
}

// SetBoldFont sets the font of the bold text of the image, whose font-weight
// is bold or 600 or more, when a font is set with SetFont; unless set, that
// font draws the bold text too.
func (image *SVGImage) SetBoldFont(font *Font) *SVGImage {
	image.boldFont = font
	return image
}

// fontOf returns the font of a run: the one set, the bold one for a weight of
// 600 or more when it is set, or else the face of the fonts PDFjet carries of
// its family, weight and style, which the Go port always has, embedded in it.
func (image *SVGImage) fontOf(run *svgTextRun, pdf *PDF) *Font {
	if image.font != nil {
		if run.weight >= 600 && image.boldFont != nil {
			return image.boldFont
		}
		return image.font
	}
	return bundledFont(pdf, run.family, run.weight, run.italic)
}

// GetWidth returns the width of this svgParser image.
func (image *SVGImage) GetWidth() float32 {
	return image.w
}

// GetHeight returns the height of this svgParser image.
func (image *SVGImage) GetHeight() float32 {
	return image.h
}

// drawPath draws a path, filled and then stroked, or stroked first when its
// paint-order says. An opacity, a line cap and a line join are set in a
// graphics state of the path's own.
func (image *SVGImage) drawPath(path *svgPath, page *Page) {
	fill := path.fill != color.Transparent
	stroke := path.stroke != color.Transparent
	if len(path.operations) == 0 || !fill && !stroke || !image.isWritable(path, page) {
		return
	}
	// PDF/A-1 has no transparency, so there the path is drawn opaque, as an
	// annotation is (review of 12 October 2026, R9).
	alpha := !page.pdf.isPDFA1() && (fill && path.fillAlpha < 1.0 || stroke && path.strokeAlpha < 1.0)
	lineCap := stroke && path.lineCap != capstyle.Butt
	lineJoin := stroke && path.lineJoin != joinstyle.Miter
	// A limit of 4 is set around the whole image, in DrawOn
	miter := stroke && path.lineJoin == joinstyle.Miter && path.miterLimit != svgMiterLimit
	dash := stroke && path.dashArray != nil
	state := alpha || lineCap || lineJoin || miter || dash || path.clips != nil
	if state {
		page.SaveGraphicsState()
		image.clip(path.clips, page)
		if dash {
			page.appendByte('[')
			for i, length := range path.dashArray {
				if i > 0 {
					page.appendByte(' ')
				}
				page.appendFloat32(length)
			}
			page.appendString("] ")
			page.appendFloat32(svgDashPhase(path))
			page.appendString(" d\n")
		}
		if alpha {
			gs := NewGraphicsState()
			gs.SetAlphaNonStroking(path.fillAlpha)
			gs.SetAlphaStroking(path.strokeAlpha)
			page.SetGraphicsState(gs)
		}
		if lineCap {
			page.SetLineCapStyle(path.lineCap)
		}
		if lineJoin {
			page.SetLineJoinStyle(path.lineJoin)
		}
		if miter {
			page.appendFloat32(path.miterLimit)
			page.appendString(" M\n")
		}
	}

	for pass := 0; pass < 2; pass++ {
		strokePass := (pass == 0) == path.strokeFirst
		if !strokePass && fill {
			page.SetBrushColor(path.fill)
			for _, op := range path.operations {
				if op.cmd == 'M' {
					page.MoveTo(op.x+image.x, op.y+image.y)
				} else if op.cmd == 'L' {
					page.LineTo(op.x+image.x, op.y+image.y)
				} else if op.cmd == 'C' {
					page.CurveTo(
						op.x1+image.x, op.y1+image.y,
						op.x2+image.x, op.y2+image.y,
						op.x+image.x, op.y+image.y)
				}
			}
			if path.evenOdd {
				page.appendString("f*\n")
			} else {
				page.FillPath()
			}
		}

		if strokePass && stroke {
			page.SetPenColor(path.stroke)
			page.SetPenWidth(path.strokeWidth)
			// Z closes a subpath, and the path is stroked once, all of its
			// subpaths together, as SVG strokes it.
			for _, op := range path.operations {
				if op.cmd == 'M' {
					page.MoveTo(op.x+image.x, op.y+image.y)
				} else if op.cmd == 'L' {
					page.LineTo(op.x+image.x, op.y+image.y)
				} else if op.cmd == 'C' {
					page.CurveTo(
						op.x1+image.x, op.y1+image.y,
						op.x2+image.x, op.y2+image.y,
						op.x+image.x, op.y+image.y)
				} else if op.cmd == 'Z' {
					page.appendString("h\n")
				}
			}
			page.StrokePath()
		}
	}

	if state {
		page.RestoreGraphicsState()
	}
}

// clip clips what is drawn next to the clip paths, each in turn.
func (image *SVGImage) clip(clips []*svgPath, page *Page) {
	for _, clip := range clips {
		if len(clip.parts) > 0 {
			image.clipToText(clip, page)
			continue
		}
		for _, op := range clip.operations {
			if op.cmd == 'M' {
				page.MoveTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'L' {
				page.LineTo(op.x+image.x, op.y+image.y)
			} else if op.cmd == 'C' {
				page.CurveTo(op.x1+image.x, op.y1+image.y, op.x2+image.x, op.y2+image.y, op.x+image.x, op.y+image.y)
			} else if op.cmd == 'Z' {
				page.appendString("h\n")
			}
		}
		if clip.evenOdd {
			page.appendString("W*\nn\n")
		} else {
			page.ClipPath()
		}
	}
}

// clipToText clips what is drawn next to the glyphs of the texts of the clip
// path, their union, as they are in one text object.
func (image *SVGImage) clipToText(clip *svgPath, page *Page) {
	page.beginSVGClipText()
	for _, part := range clip.parts {
		for _, run := range part.text.runs {
			if run.clips() {
				page.addSVGClipText(run.font, run.fontSize, run.drawn(), image.textMatrix(part.text, run, page))
			}
		}
	}
	page.endText()
}

// textMatrix returns the text matrix that takes the glyphs of the run, whose y
// goes up, to where the run is on the page.
func (image *SVGImage) textMatrix(text *svgText, run *svgTextRun, page *Page) [6]float32 {
	m := text.matrix
	px := float64(run.px)
	py := float64(run.py)
	return [6]float32{
		float32(m[0]),
		float32(-m[1]),
		float32(-m[2]),
		float32(m[3]),
		float32(float64(m[0]*px)+float64(m[2]*py)+m[4]) + image.x,
		page.height - (float32(float64(m[1]*px)+float64(m[3]*py)+m[5]) + image.y)}
}

// drawText draws a text: each run filled and then stroked, or stroked first
// when its paint-order says.
func (image *SVGImage) drawText(text *svgText, clips []*svgPath, page *Page) {
	m := text.matrix
	scale := math.Sqrt(math.Abs(float64(m[0]*m[3]) - float64(m[1]*m[2])))
	for _, run := range text.runs {
		str := run.drawn()
		if str == "" || !(run.fontSize > 0.0) {
			continue
		}
		tm := image.textMatrix(text, run, page)
		writable := image.clipsAreWritable(clips, page)
		for _, value := range tm {
			writable = writable && fastfloat.IsWritable(value)
		}
		strokeWidth := float32(float64(float64(run.strokeWidth) * scale))
		fill := run.fill != color.Transparent && run.fillAlpha > 0.0
		stroke := run.stroke != color.Transparent && run.strokeAlpha > 0.0 &&
			strokeWidth > 0.0 && fastfloat.IsWritable(strokeWidth)
		if !writable || !fill && !stroke {
			continue
		}
		alpha := !page.pdf.isPDFA1() && (fill && run.fillAlpha < 1.0 || stroke && run.strokeAlpha < 1.0)
		lineJoin := stroke && run.lineJoin != joinstyle.Miter
		miter := stroke && run.lineJoin == joinstyle.Miter && run.miterLimit != svgMiterLimit
		state := alpha || lineJoin || miter || clips != nil
		if state {
			page.SaveGraphicsState()
			image.clip(clips, page)
			if alpha {
				gs := NewGraphicsState()
				gs.SetAlphaNonStroking(run.fillAlpha)
				gs.SetAlphaStroking(run.strokeAlpha)
				page.SetGraphicsState(gs)
			}
			if lineJoin {
				page.SetLineJoinStyle(run.lineJoin)
			}
			if miter {
				page.appendFloat32(run.miterLimit)
				page.appendString(" M\n")
			}
		}
		for pass := 0; pass < 2; pass++ {
			strokePass := (pass == 0) == run.strokeFirst
			if strokePass && stroke {
				page.SetPenColor(run.stroke)
				page.SetPenWidth(strokeWidth)
				page.drawSVGText(run.font, run.fontSize, str, tm, 1)
			} else if !strokePass && fill {
				page.SetBrushColor(run.fill)
				page.drawSVGText(run.font, run.fontSize, str, tm, 0)
			}
		}
		if state {
			page.RestoreGraphicsState()
		}
	}
}

// strokesMiters reports whether a path or a run of a text is stroked with
// miter joins and the limit of SVG unless given.
func (image *SVGImage) strokesMiters(paths []*svgPath) bool {
	for _, path := range paths {
		if path.text != nil {
			for _, run := range path.text.runs {
				if run.stroke != color.Transparent && run.lineJoin == joinstyle.Miter && run.miterLimit == svgMiterLimit {
					return true
				}
			}
		} else if path.stroke != color.Transparent && path.lineJoin == joinstyle.Miter && path.miterLimit == svgMiterLimit {
			return true
		}
	}
	return false
}

// overflows reports whether something of the image is drawn outside its
// size, which is cut off then: a point of a path, wider by half its stroke,
// or the box of a run of a text, from its ascent to its descent.
func (image *SVGImage) overflows(paths []*svgPath, page *Page) bool {
	if image.w <= 0.0 || image.h <= 0.0 {
		return false
	}
	for _, path := range paths {
		if path.text != nil {
			m := path.text.matrix
			for _, run := range path.text.runs {
				corners := [4][2]float32{
					{run.px, run.py - run.fontSize},
					{run.px + run.width, run.py - run.fontSize},
					{run.px, run.py + float32(0.3*run.fontSize)},
					{run.px + run.width, run.py + float32(0.3*run.fontSize)}}
				for _, corner := range corners {
					x, y := svgTransform(m, corner[0], corner[1])
					if image.outside(x, y, 0.0) {
						return true
					}
				}
			}
			continue
		}
		if !image.isWritable(path, page) {
			continue // Not drawn
		}
		margin := float32(0.0)
		if path.stroke != color.Transparent {
			margin = float32(path.strokeWidth / 2.0)
		}
		for _, op := range path.operations {
			if image.outside(op.x, op.y, margin) || op.cmd == 'C' &&
				(image.outside(op.x1, op.y1, margin) || image.outside(op.x2, op.y2, margin)) {
				return true
			}
		}
	}
	return false
}

// outside reports whether a point is outside the size of the image, by more
// than a hundredth of a point and the margin.
func (image *SVGImage) outside(px, py, margin float32) bool {
	e := 0.01 + margin
	return px < -e || py < -e || px > image.w+e || py > image.h+e
}

// svgDashPhase returns the dash offset as a PDF writes it, which is not below
// zero: an offset below zero starts as far before the pattern, the same place
// as that far from the end of the pattern (review of 30 October 2026).
func svgDashPhase(path *svgPath) float32 {
	if path.dashOffset >= 0.0 {
		return path.dashOffset
	}
	sum := 0.0
	for _, length := range path.dashArray {
		sum += float64(length)
	}
	phase := math.Mod(float64(path.dashOffset), sum)
	if phase < 0.0 {
		phase += sum
	}
	return float32(math.Abs(phase))
}

// isWritable reports whether a PDF can hold the points, the stroke width, the
// dashes and the clip paths of the path where it is drawn: a transform or a
// scale such as scale(1e30) can take them out of its range, and such a path is
// not drawn.
func (image *SVGImage) isWritable(path *svgPath, page *Page) bool {
	if !fastfloat.IsWritable(path.strokeWidth) {
		return false
	}
	if path.stroke != color.Transparent && path.dashArray != nil {
		for _, length := range path.dashArray {
			if !fastfloat.IsWritable(length) {
				return false
			}
		}
		if !fastfloat.IsWritable(svgDashPhase(path)) {
			return false
		}
	}
	return image.areWritable(path.operations, page) && image.clipsAreWritable(path.clips, page)
}

// clipsAreWritable reports whether a PDF can hold the clip paths.
func (image *SVGImage) clipsAreWritable(clips []*svgPath, page *Page) bool {
	for _, clip := range clips {
		if !image.areWritable(clip.operations, page) {
			return false
		}
		for _, part := range clip.parts {
			for _, run := range part.text.runs {
				for _, value := range image.textMatrix(part.text, run, page) {
					if run.clips() && !fastfloat.IsWritable(value) {
						return false
					}
				}
			}
		}
	}
	return true
}

// areWritable reports whether a PDF can hold the points of the operations.
func (image *SVGImage) areWritable(operations []*svgPathOp, page *Page) bool {
	point := func(x, y float32) bool {
		x += image.x
		y += image.y
		return fastfloat.IsWritable(x) && fastfloat.IsWritable(y) && fastfloat.IsWritable(page.height-y)
	}
	for _, op := range operations {
		if !point(op.x, op.y) || op.cmd == 'C' && (!point(op.x1, op.y1) || !point(op.x2, op.y2)) {
			return false
		}
	}
	return true
}

// DrawOn draws this svgParser image on the specified page.
func (image *SVGImage) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{image.x + image.w, image.y + image.h} // Measured, not drawn
	}
	// A linked image is a Figure in the Link its annotation joins
	var link *structElement
	if image.uri != "" || image.key != "" {
		link = page.beginLink()
	}
	page.AddBDC(structelem.Figure, image.language, image.actualText, image.altDescription)
	paths := image.chosenPaths(page.pdf.language)
	for _, drawn := range [][]*svgPath{paths, image.clipShapes} {
		for _, path := range drawn {
			if path.text != nil {
				for _, run := range path.text.runs {
					run.font = image.fontOf(run, page.pdf)
				}
				path.text.place()
			}
		}
	}
	// The miter limit, set once: a stroked map has as many paths as a PDF
	// would hold, and the few with another limit set their own.
	miter := image.strokesMiters(paths)
	if miter {
		page.SaveGraphicsState()
		page.appendFloat32(svgMiterLimit)
		page.appendString(" M\n")
	}
	clip := image.overflows(paths, page)
	if clip {
		page.SaveGraphicsState()
		page.ClipRect(image.x, image.y, image.w, image.h)
	}
	for _, path := range paths {
		if path.text != nil {
			image.drawText(path.text, path.clips, page)
		} else {
			image.drawPath(path, page)
		}
	}
	if clip {
		page.RestoreGraphicsState()
	}
	if miter {
		page.RestoreGraphicsState()
	}
	page.SetFigureBoundingBox(image.x, image.y, image.w, image.h)
	page.AddEMC()
	page.endLink(link)
	if image.uri != "" || image.key != "" {
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             image.x,
			y1:             image.y,
			x2:             image.x + image.w,
			y2:             image.y + image.h,
			vertices:       nil,
			opacity:        0.0,
			title:          "",
			contents:       "",
			uri:            image.uri,
			key:            image.key, // The destination name
			language:       image.language,
			actualText:     image.actualText,
			altDescription: image.altDescription,
			linkElement:    link,
		})
	}
	return [2]float32{image.x + image.w, image.y + image.h}
}
