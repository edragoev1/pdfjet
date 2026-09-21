//
// textline.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

import (
	"math"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/single"
	"github.com/edragoev1/pdfjet/v9/src/scriptposition"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// TextLine is used to create text line objects.
type TextLine struct {
	text               string
	x, y               float32
	font, fallbackFont *Font
	fontSize           float32
	isLastToken        bool
	xOffset            float32
	underline          bool
	strikeout          bool
	degrees            int
	textColor          [3]float32
	decorationColor    [3]float32
	colorMap           map[string]int32
	scriptPosition     scriptposition.ScriptPosition
	verticalOffset     float32
	explicitOffset     bool // True after SetVerticalOffset
	uri, key           string
	destination        string
	language           string
	altDescription     string
	uriLanguage        string
	uriActualText      string
	uriAltDescription  string
	structureType      structelem.StructElem
}

// NewTextLine is constructor for creating text line objects.
//   - font: the font to use.
//   - text: the text.
func NewTextLine(font *Font, text string) *TextLine {
	textLine := new(TextLine)
	textLine.font = font
	textLine.fallbackFont = font
	textLine.fontSize = font.size
	textLine.text = text
	textLine.altDescription = text
	textLine.structureType = structelem.P
	return textLine
}

// NewEmptyTextLine is constructor for creating empty text line objects.
//   - font: the font to use.
func NewEmptyTextLine(font *Font) *TextLine {
	return NewTextLine(font, "")
}

// SetText sets the text.
//   - text: the text.
//
// Returns this TextLine.
func (textLine *TextLine) SetText(text string) *TextLine {
	textLine.text = text
	textLine.altDescription = text
	return textLine
}

// GetText returns the text.
func (textLine *TextLine) GetText() string {
	return textLine.text
}

// SetLocation sets the location where this text line will be drawn on the page.
//   - x: the x coordinate of the text line.
//   - y: the y coordinate of the text line.
//
// Returns this TextLine.
func (textLine *TextLine) SetLocation(x, y float32) Drawable {
	textLine.x = x
	textLine.y = y
	return textLine
}

// SetFont sets the font to use for this text line. The fallback font changes
// with it, unless a different fallback font was set.
//   - font: the font to use.
//
// Returns this TextLine.
func (textLine *TextLine) SetFont(font *Font) *TextLine {
	if textLine.fallbackFont == textLine.font {
		textLine.fallbackFont = font
	}
	textLine.font = font
	return textLine
}

// GetFont gets the font to use for this text line.
// Returns font the font to use.
func (textLine *TextLine) GetFont() *Font {
	return textLine.font
}

// SetFontSize sets the font size to use for this text line.
//   - fontSize: the fontSize to use.
//
// Returns this TextLine.
func (textLine *TextLine) SetFontSize(fontSize float32) *TextLine {
	textLine.fontSize = fontSize
	return textLine
}

// GetFontSize returns the font size.
func (textLine *TextLine) GetFontSize() float32 {
	return textLine.fontSize
}

// SetFallbackFont sets the fallback font.
//   - fallbackFont: the fallback font.
//
// Returns this TextLine.
func (textLine *TextLine) SetFallbackFont(fallbackFont *Font) *TextLine {
	textLine.fallbackFont = fallbackFont
	return textLine
}

// GetFallbackFont returns the fallback font.
// Returns the fallback font.
func (textLine *TextLine) GetFallbackFont() *Font {
	return textLine.fallbackFont
}

// SetTextColor sets the text color as a 0xRRGGBB value. color.Transparent leaves it unchanged.
func (textLine *TextLine) SetTextColor(c int32) *TextLine {
	if c == color.Transparent {
		return textLine
	}
	r := float32((c>>16)&0xff) / 255.0
	g := float32((c>>8)&0xff) / 255.0
	b := float32((c)&0xff) / 255.0
	textLine.textColor = [3]float32{r, g, b}
	return textLine
}

// SetTextColorRGB sets the text color from the red, green and blue components, from 0.0 to 1.0.
func (textLine *TextLine) SetTextColorRGB(c [3]float32) *TextLine {
	textLine.textColor = c
	return textLine
}

// SetDecorationColor sets the color of the underline and strikeout lines as a 0xRRGGBB value.
// color.Transparent leaves it unchanged.
func (textLine *TextLine) SetDecorationColor(c int32) *TextLine {
	if c == color.Transparent {
		return textLine
	}
	r := float32((c>>16)&0xff) / 255.0
	g := float32((c>>8)&0xff) / 255.0
	b := float32((c)&0xff) / 255.0
	textLine.decorationColor = [3]float32{r, g, b}
	return textLine
}

// SetDecorationColorRGB sets the color of the underline and strikeout lines from the
// red, green and blue components, from 0.0 to 1.0.
func (textLine *TextLine) SetDecorationColorRGB(c [3]float32) *TextLine {
	textLine.decorationColor = c
	return textLine
}

// GetDecorationColor returns the color of the underline and strikeout lines.
func (textLine *TextLine) GetDecorationColor() [3]float32 {
	return textLine.decorationColor
}

// GetTextColor returns the text color.
func (textLine *TextLine) GetTextColor() [3]float32 {
	return textLine.textColor
}

// destinationY returns the y coordinate of the destination of the line, a font
// size above the baseline, which DrawOn and Bookmark use.
func (textLine *TextLine) destinationY() float32 {
	return textLine.y - textLine.fontSize
}

// GetWidth returns the width of this TextLine.
// Returns the width.
func (textLine *TextLine) GetWidth() float32 {
	return textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, textLine.text)
}

// GetHeight returns the height of this TextLine.
// Returns the height.
func (textLine *TextLine) GetHeight() float32 {
	return textLine.font.GetBodyHeight(textLine.fontSize)
}

// GetAscent returns how far above its baseline this text line reaches: the
// ascent of the font at the font size of this text line.
func (textLine *TextLine) GetAscent() float32 {
	return textLine.font.GetAscent(textLine.fontSize)
}

// GetDescent returns how far below its baseline this text line reaches: the
// descent of the font at the font size of this text line.
func (textLine *TextLine) GetDescent() float32 {
	return textLine.font.GetDescent(textLine.fontSize)
}

// SetURIAction sets the URI for the "click text line" action.
//   - uri: the URI
//
// Returns this TextLine.
func (textLine *TextLine) SetURIAction(uri string) *TextLine {
	textLine.uri = uri
	return textLine
}

// GetURIAction returns the action URI.
// Returns the action URI.
func (textLine *TextLine) GetURIAction() string {
	return textLine.uri
}

// SetGoToAction sets the destination key for the action.
//   - key: the destination name.
//
// Returns this TextLine.
func (textLine *TextLine) SetGoToAction(key string) *TextLine {
	textLine.key = key
	return textLine
}

// SetDestination sets the name of a destination that DrawOn adds to the page, a
// font size above the baseline, so that a GoTo action with the name, which
// SetGoToAction sets, goes to this line.
//   - name: the destination name, or "" for none.
//
// Returns this TextLine.
func (textLine *TextLine) SetDestination(name string) *TextLine {
	textLine.destination = name
	return textLine
}

// GetDestination returns the name of the destination that DrawOn adds to the
// page, or "".
func (textLine *TextLine) GetDestination() string {
	return textLine.destination
}

// GetGoToAction returns the GoTo action string.
// Returns the GoTo action string.
func (textLine *TextLine) GetGoToAction() string {
	return textLine.key
}

// SetUnderline sets the underline variable.
// If the value of the underline variable is 'true' - the text is underlined.
//   - underline: the underline flag.
//
// Returns this TextLine.
func (textLine *TextLine) SetUnderline(underline bool) *TextLine {
	textLine.underline = underline
	return textLine
}

// GetUnderline returns the underline flag.
// Returns the underline flag.
func (textLine *TextLine) GetUnderline() bool {
	return textLine.underline
}

// SetStrikeout sets the strike variable.
// If the value of the strike variable is 'true' - a strike line is drawn through the text.
//   - strikeout: the strikeout flag.
//
// Returns this TextLine.
func (textLine *TextLine) SetStrikeout(strikeout bool) *TextLine {
	textLine.strikeout = strikeout
	return textLine
}

// GetStrikeout returns the strikeout flag.
// Returns the strikeout flag.
func (textLine *TextLine) GetStrikeout() bool {
	return textLine.strikeout
}

// SetTextRotation sets the rotation of the text. A positive angle turns
// clockwise, as every rotation in PDFjet turns, and a negative angle
// counterclockwise.
//   - degrees: the angle in degrees.
//
// Returns this TextLine.
func (textLine *TextLine) SetTextRotation(degrees int) *TextLine {
	textLine.degrees = degrees
	return textLine
}

// GetTextRotation returns the text direction.
// Returns the text direction.
func (textLine *TextLine) GetTextRotation() int {
	return textLine.degrees
}

// SetScriptPosition sets the script position: scriptposition.Normal, scriptposition.Subscript or
// scriptposition.Superscript. The offset of a superscript or subscript follows the font
// and font size of this text line when it is drawn.
func (textLine *TextLine) SetScriptPosition(scriptPosition scriptposition.ScriptPosition) *TextLine {
	textLine.scriptPosition = scriptPosition
	textLine.explicitOffset = false
	return textLine
}

// GetScriptPosition returns the script position.
// Returns the script position.
func (textLine *TextLine) GetScriptPosition() scriptposition.ScriptPosition {
	return textLine.scriptPosition
}

// SetVerticalOffset sets the vertical offset of the text, which replaces the
// offset of the script position until SetScriptPosition is called again.
func (textLine *TextLine) SetVerticalOffset(verticalOffset float32) *TextLine {
	textLine.verticalOffset = verticalOffset
	textLine.explicitOffset = true
	return textLine
}

// GetVerticalOffset returns the vertical text offset: the one set with
// SetVerticalOffset, or that of the script position at the font and font size of
// this text line.
func (textLine *TextLine) GetVerticalOffset() float32 {
	if textLine.explicitOffset {
		return textLine.verticalOffset
	}
	if textLine.scriptPosition == scriptposition.Superscript {
		return -textLine.font.GetBodyHeight(textLine.fontSize) / 2.0
	} else if textLine.scriptPosition == scriptposition.Subscript {
		return textLine.font.GetBodyHeight(textLine.fontSize) / 3.0
	}
	return 0.0
}

// SetLanguage sets the language of the text, for example "en-US".
func (textLine *TextLine) SetLanguage(language string) *TextLine {
	textLine.language = language
	return textLine
}

// GetLanguage returns the language of the text.
func (textLine *TextLine) GetLanguage() string {
	return textLine.language
}

// SetAltDescription sets the alternate description of this text line.
//   - altDescription: the alternate description of the text line.
//
// Returns this TextLine.
func (textLine *TextLine) SetAltDescription(altDescription string) *TextLine {
	textLine.altDescription = altDescription
	return textLine
}

// GetAltDescription gets the alternate description of this text line.
func (textLine *TextLine) GetAltDescription() string {
	return textLine.altDescription
}

// SetURILanguage sets the language of the link annotation.
func (textLine *TextLine) SetURILanguage(uriLanguage string) *TextLine {
	textLine.uriLanguage = uriLanguage
	return textLine
}

// SetURIAltDescription sets the alternate description of the link annotation.
func (textLine *TextLine) SetURIAltDescription(uriAltDescription string) *TextLine {
	textLine.uriAltDescription = uriAltDescription
	return textLine
}

// SetURIActualText sets the actual text of the link annotation.
func (textLine *TextLine) SetURIActualText(uriActualText string) *TextLine {
	textLine.uriActualText = uriActualText
	return textLine
}

// GetURILanguage returns the language of the link annotation.
func (textLine *TextLine) GetURILanguage() string {
	return textLine.uriLanguage
}

// GetURIAltDescription returns the alternate description of the link annotation.
func (textLine *TextLine) GetURIAltDescription() string {
	return textLine.uriAltDescription
}

// GetURIActualText returns the actual text of the link annotation.
func (textLine *TextLine) GetURIActualText() string {
	return textLine.uriActualText
}

// SetStructureType sets the structure element type of this text line, for
// example structelem.P or structelem.H1.
func (textLine *TextLine) SetStructureType(structureType structelem.StructElem) *TextLine {
	textLine.structureType = structureType
	return textLine
}

// SetHighlightColors sets the colors used to highlight words in the text.
func (textLine *TextLine) SetHighlightColors(colorMap map[string]int32) *TextLine {
	textLine.colorMap = colorMap
	return textLine
}

// GetHighlightColors returns the colors used to highlight words in the text.
func (textLine *TextLine) GetHighlightColors() map[string]int32 {
	return textLine.colorMap
}

// copyWithText returns a new text line with the text and every setting of this
// text line, for a part of its text wrapped onto a line of its own. An
// alternate description that was set is kept; otherwise the new text is its own.
func (textLine *TextLine) copyWithText(text string) *TextLine {
	line := NewTextLine(textLine.font, text)
	line.fallbackFont = textLine.fallbackFont
	line.fontSize = textLine.fontSize
	line.underline = textLine.underline
	line.strikeout = textLine.strikeout
	line.degrees = textLine.degrees
	line.textColor = textLine.textColor
	line.decorationColor = textLine.decorationColor
	line.colorMap = textLine.colorMap
	line.scriptPosition = textLine.scriptPosition
	line.verticalOffset = textLine.verticalOffset
	line.explicitOffset = textLine.explicitOffset
	line.uri = textLine.uri
	line.key = textLine.key
	line.language = textLine.language
	if textLine.altDescription != textLine.text {
		line.altDescription = textLine.altDescription
	}
	line.uriLanguage = textLine.uriLanguage
	line.uriActualText = textLine.uriActualText
	line.uriAltDescription = textLine.uriAltDescription
	line.structureType = textLine.structureType
	return line
}

// DrawOn draws this text line on the specified page and returns the x and y
// coordinates of its bottom right corner. It draws nothing when the page is
// nil or the text is empty.
func (textLine *TextLine) DrawOn(page *Page) [2]float32 {
	if textLine.text == "" {
		return [2]float32{textLine.x, textLine.y}
	}
	if page == nil {
		return textLine.corner(textLine.GetVerticalOffset()) // Measured, not drawn
	}
	if textLine.destination != "" {
		page.AddDestination(textLine.destination, textLine.destinationY())
	}

	verticalOffset := textLine.GetVerticalOffset()
	page.SetTextRotation(textLine.degrees)
	page.SetBrushColorRGB(textLine.textColor)
	// The text is drawn, so it is not given again as actual text, or as its own
	// alternate description: right to left text is drawn in visual order, and
	// would be read backwards.
	alt := textLine.altDescription
	if alt == textLine.text {
		alt = ""
	}
	page.AddBDC(textLine.structureType, textLine.language, "", alt)
	page.drawStringUsingHighlightColors(
		textLine.font,
		textLine.fallbackFont,
		textLine.fontSize,
		textLine.text,
		textLine.x,
		textLine.y+verticalOffset,
		textLine.textColor,
		textLine.colorMap)
	page.AddEMC()

	// The trigonometry turns counterclockwise, where the rotation turns clockwise.
	radians := math.Pi * float64(-textLine.degrees) / 180.0
	if textLine.underline {
		page.SetPenWidth(textLine.font.GetUnderlineThickness(textLine.fontSize))
		page.SetPenColorRGB(textLine.decorationColor)
		lineLength := textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, textLine.text)
		if textLine.isLastToken {
			lineLength -= textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, single.Space)
		}
		underlinePosition := float64(textLine.font.GetUnderlinePosition(textLine.fontSize))
		xAdjust := underlinePosition * math.Sin(radians)
		yAdjust := underlinePosition*math.Cos(radians) + float64(verticalOffset)
		x2 := float64(textLine.x) + float64(lineLength)*math.Cos(radians)
		y2 := float64(textLine.y) - float64(lineLength)*math.Sin(radians)
		// The line is decoration, and the text says what it is drawn under;
		// a description of its own is read after the text again.
		page.AddArtifactBMC()
		page.MoveTo(float32(float64(textLine.x)+xAdjust), float32(float64(textLine.y)+yAdjust))
		page.LineTo(float32(x2+xAdjust), float32(y2+yAdjust))
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.strikeout {
		page.SetPenWidth(textLine.font.GetUnderlineThickness(textLine.fontSize))
		page.SetPenColorRGB(textLine.decorationColor)
		lineLength := textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, textLine.text)
		if textLine.isLastToken {
			lineLength -= textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, single.Space)
		}
		bodyHeight := float64(textLine.font.GetBodyHeight(textLine.fontSize))
		xAdjust := (bodyHeight / 4.0) * math.Sin(radians)
		yAdjust := (bodyHeight/4.0)*math.Cos(radians) + float64(verticalOffset)
		x2 := float64(textLine.x) + float64(lineLength)*math.Cos(radians)
		y2 := float64(textLine.y) - float64(lineLength)*math.Sin(radians)
		page.AddArtifactBMC()
		page.MoveTo(float32(float64(textLine.x)-xAdjust), float32(float64(textLine.y)-yAdjust))
		page.LineTo(float32(x2-xAdjust), float32(y2-yAdjust))
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.uri != "" || textLine.key != "" {
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             textLine.x,
			y1:             (textLine.y + verticalOffset) - textLine.font.GetAscent(textLine.fontSize),
			x2:             textLine.x + textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, textLine.text),
			y2:             (textLine.y + verticalOffset) + textLine.font.GetDescent(textLine.fontSize),
			vertices:       nil,
			opacity:        0.0,
			title:          "",
			contents:       "",
			uri:            textLine.uri,
			key:            textLine.key, // The destination name
			language:       textLine.uriLanguage,
			actualText:     textLine.uriActualText,
			altDescription: textLine.uriAltDescription,
		})
	}

	page.SetTextRotation(0)

	return textLine.corner(verticalOffset)
}

// corner returns the right end of the baseline, or its lower end when the text
// is rotated.
func (textLine *TextLine) corner(verticalOffset float32) [2]float32 {
	// The trigonometry turns counterclockwise, where the rotation turns clockwise.
	radians := math.Pi * float64(-textLine.degrees) / 180.0
	length := textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, textLine.text)
	xMax := math.Max(float64(textLine.x), float64(textLine.x)+float64(length)*math.Cos(radians))
	yMax := math.Max(
		float64(textLine.y+verticalOffset),
		float64(textLine.y+verticalOffset)-float64(length)*math.Sin(radians))
	return [2]float32{float32(xMax), float32(yMax)}
}

// GetLocation returns the x coordinate of the start of the text and the y coordinate of its baseline.
func (textLine *TextLine) GetLocation() [2]float32 {
	return [2]float32{textLine.x, textLine.y}
}
