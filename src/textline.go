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
	lineColor          [3]float32
	colorMap           map[string]int32
	scriptPosition     scriptposition.ScriptPosition
	verticalOffset     float32
	explicitOffset     bool // True after SetVerticalOffset
	uri, key           string
	language           string
	altDescription     string
	uriLanguage        string
	uriActualText      string
	uriAltDescription  string
	structureType      structelem.StructElem
}

// NewTextLine is constructor for creating text line objects.
// @param font the font to use.
// @param text the text.
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
// @param font the font to use.
func NewEmptyTextLine(font *Font) *TextLine {
	return NewTextLine(font, "")
}

// SetText sets the text.
// @param text the text.
// @return this TextLine.
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
// @param x the x coordinate of the text line.
// @param y the y coordinate of the text line.
// @return this TextLine.
func (textLine *TextLine) SetLocation(x, y float32) Drawable {
	textLine.x = x
	textLine.y = y
	return textLine
}

// SetFont sets the font to use for this text line.
// @param font the font to use.
// @return this TextLine.
func (textLine *TextLine) SetFont(font *Font) *TextLine {
	textLine.font = font
	return textLine
}

// GetFont gets the font to use for this text line.
// @return font the font to use.
func (textLine *TextLine) GetFont() *Font {
	return textLine.font
}

// SetFontSize sets the font size to use for this text line.
// @param fontSize the fontSize to use.
// @return this TextLine.
func (textLine *TextLine) SetFontSize(fontSize float32) *TextLine {
	textLine.fontSize = fontSize
	return textLine
}

// GetFontSize returns the font size.
func (textLine *TextLine) GetFontSize() float32 {
	return textLine.fontSize
}

// SetFallbackFont sets the fallback font.
// @param fallbackFont the fallback font.
// @return this TextLine.
func (textLine *TextLine) SetFallbackFont(fallbackFont *Font) *TextLine {
	textLine.fallbackFont = fallbackFont
	return textLine
}

// GetFallbackFont returns the fallback font.
// @return the fallback font.
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
	textLine.lineColor = [3]float32{r, g, b}
	return textLine
}

// SetDecorationColorRGB sets the color of the underline and strikeout lines from the
// red, green and blue components, from 0.0 to 1.0.
func (textLine *TextLine) SetDecorationColorRGB(c [3]float32) *TextLine {
	textLine.lineColor = c
	return textLine
}

// GetDecorationColor returns the color of the underline and strikeout lines.
func (textLine *TextLine) GetDecorationColor() [3]float32 {
	return textLine.lineColor
}

// GetTextColor returns the text color.
func (textLine *TextLine) GetTextColor() [3]float32 {
	return textLine.textColor
}

// GetDestinationX returns the x coordinate of the destination.
// @return the x coordinate of the destination.
func (textLine *TextLine) GetDestinationX() float32 {
	return textLine.x
}

// GetDestinationY returns the y coordinate of the destination.
// @return the y coordinate of the destination.
func (textLine *TextLine) GetDestinationY() float32 {
	return textLine.y - textLine.fontSize
}

// GetWidth returns the width of this TextLine.
// @return the width.
func (textLine *TextLine) GetWidth() float32 {
	return textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, textLine.text)
}

// GetStringWidth returns the width of the specified string.
func (textLine *TextLine) GetStringWidth(text string) float32 {
	return textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, text)
}

// GetHeight returns the height of this TextLine.
// @return the height.
func (textLine *TextLine) GetHeight() float32 {
	return textLine.font.GetBodyHeightAt(textLine.fontSize)
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI
// @return this TextLine.
func (textLine *TextLine) SetURIAction(uri string) *TextLine {
	textLine.uri = uri
	return textLine
}

// GetURIAction returns the action URI.
// @return the action URI.
func (textLine *TextLine) GetURIAction() string {
	return textLine.uri
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
// @return this TextLine.
func (textLine *TextLine) SetGoToAction(key string) *TextLine {
	textLine.key = key
	return textLine
}

// GetGoToAction returns the GoTo action string.
// @return the GoTo action string.
func (textLine *TextLine) GetGoToAction() string {
	return textLine.key
}

// SetUnderline sets the underline variable.
// If the value of the underline variable is 'true' - the text is underlined.
// @param underline the underline flag.
// @return this TextLine.
func (textLine *TextLine) SetUnderline(underline bool) *TextLine {
	textLine.underline = underline
	return textLine
}

// GetUnderline returns the underline flag.
// @return the underline flag.
func (textLine *TextLine) GetUnderline() bool {
	return textLine.underline
}

// SetStrikeout sets the strike variable.
// If the value of the strike variable is 'true' - a strike line is drawn through the text.
// @param strikeout the strikeout flag.
// @return this TextLine.
func (textLine *TextLine) SetStrikeout(strikeout bool) *TextLine {
	textLine.strikeout = strikeout
	return textLine
}

// GetStrikeout returns the strikeout flag.
// @return the strikeout flag.
func (textLine *TextLine) GetStrikeout() bool {
	return textLine.strikeout
}

// SetTextRotation sets the direction in which to draw the text.
// @param degrees the number of degrees.
// @return this TextLine.
func (textLine *TextLine) SetTextRotation(degrees int) *TextLine {
	textLine.degrees = degrees
	return textLine
}

// GetTextRotation returns the text direction.
// @return the text direction.
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
// @return the script position.
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
		return -textLine.font.GetBodyHeightAt(textLine.fontSize) / 2.0
	} else if textLine.scriptPosition == scriptposition.Subscript {
		return textLine.font.GetBodyHeightAt(textLine.fontSize) / 3.0
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
// @param altDescription the alternate description of the text line.
// @return this TextLine.
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
	line.lineColor = textLine.lineColor
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
	if page == nil || textLine.text == "" {
		return [2]float32{textLine.x, textLine.y}
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
	page.DrawStringUsingHighlightColors(
		textLine.font,
		textLine.fallbackFont,
		textLine.fontSize,
		textLine.text,
		textLine.x,
		textLine.y+verticalOffset,
		textLine.textColor,
		textLine.colorMap)
	page.AddEMC()

	radians := math.Pi * float64(textLine.degrees) / 180.0
	if textLine.underline {
		page.SetPenWidth(textLine.font.GetUnderlineThicknessAt(textLine.fontSize))
		page.SetPenColorRGB(textLine.lineColor)
		lineLength := textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, textLine.text)
		if textLine.isLastToken {
			lineLength -= textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, single.Space)
		}
		underlinePosition := float64(textLine.font.GetUnderlinePositionAt(textLine.fontSize))
		xAdjust := underlinePosition * math.Sin(radians)
		yAdjust := underlinePosition*math.Cos(radians) + float64(verticalOffset)
		x2 := float64(textLine.x) + float64(lineLength)*math.Cos(radians)
		y2 := float64(textLine.y) - float64(lineLength)*math.Sin(radians)
		page.AddBDC(textLine.structureType, textLine.language, "", "Underlined text: "+textLine.text)
		page.MoveTo(float32(float64(textLine.x)+xAdjust), float32(float64(textLine.y)+yAdjust))
		page.LineTo(float32(x2+xAdjust), float32(y2+yAdjust))
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.strikeout {
		page.SetPenWidth(textLine.font.GetUnderlineThicknessAt(textLine.fontSize))
		page.SetPenColorRGB(textLine.lineColor)
		lineLength := textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, textLine.text)
		if textLine.isLastToken {
			lineLength -= textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, single.Space)
		}
		bodyHeight := float64(textLine.font.GetBodyHeightAt(textLine.fontSize))
		xAdjust := (bodyHeight / 4.0) * math.Sin(radians)
		yAdjust := (bodyHeight/4.0)*math.Cos(radians) + float64(verticalOffset)
		x2 := float64(textLine.x) + float64(lineLength)*math.Cos(radians)
		y2 := float64(textLine.y) - float64(lineLength)*math.Sin(radians)
		page.AddBDC(textLine.structureType, textLine.language, "", "Strikethrough text: "+textLine.text)
		page.MoveTo(float32(float64(textLine.x)-xAdjust), float32(float64(textLine.y)-yAdjust))
		page.LineTo(float32(x2-xAdjust), float32(y2-yAdjust))
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.uri != "" || textLine.key != "" {
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             textLine.x,
			y1:             (textLine.y + verticalOffset) - textLine.font.GetAscentAt(textLine.fontSize),
			x2:             textLine.x + textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, textLine.text),
			y2:             (textLine.y + verticalOffset) + textLine.font.GetDescentAt(textLine.fontSize),
			vertices:       nil,
			transparency:   0.0,
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

	length := textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, textLine.text)
	xMax := math.Max(float64(textLine.x), float64(textLine.x)+float64(length)*math.Cos(radians))
	yMax := math.Max(
		float64(textLine.y+verticalOffset),
		float64(textLine.y+verticalOffset)-float64(length)*math.Sin(radians))

	return [2]float32{float32(xMax), float32(yMax)}
}

// Advance moves this text line down by the leading and returns the new y coordinate.
func (textLine *TextLine) Advance(leading float32) float32 {
	textLine.y += leading
	return textLine.y
}
