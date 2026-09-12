package pdfjet

// TextParameters holds configuration for text rendering
type TextParameters struct {
	font     *Font
	fontSize float32
	x        float32
	y        float32
	text     string
}

// NewTextParameters creates a new TextParameters with default values
func NewTextParameters() *TextParameters {
	return &TextParameters{
		fontSize: 12.0, // Default font size
		x:        0.0,  // Default X position
		y:        0.0,  // Default Y position
	}
}

// SetFont sets the font and returns the receiver for chaining
func (tp *TextParameters) SetFont(font *Font) *TextParameters {
	tp.font = font
	return tp
}

// SetFontSize sets the font size and returns the receiver for chaining
func (tp *TextParameters) SetFontSize(fontSize float32) *TextParameters {
	tp.fontSize = fontSize
	return tp
}

// SetTextLocation sets the X, Y location and returns the receiver for chaining
func (tp *TextParameters) SetTextLocation(x, y float32) *TextParameters {
	tp.x = x
	tp.y = y
	return tp
}

// SetText sets the text content and returns the receiver for chaining
func (tp *TextParameters) SetText(text string) *TextParameters {
	tp.text = text
	return tp
}
