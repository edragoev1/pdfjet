package pdfjet

/**
 * attachmentattachment.go
 *
Copyright 2022 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// FileAttachment describes file attachment object.
type FileAttachment struct {
	objNumber    int
	pdf          *PDF
	embeddedFile *EmbeddedFile
	icon         string
	title        string
	contents     string
	x            float32
	y            float32
	h            float32
}

// NewFileAttachment constructs file attachment objects.
func NewFileAttachment(pdf *PDF, embeddedFile *EmbeddedFile) *FileAttachment {
	attachment := new(FileAttachment)
	attachment.pdf = pdf
	attachment.embeddedFile = embeddedFile
	attachment.icon = "PushPin"
	attachment.contents = "Right mouse click or double click on the icon to save the attached attachment."
	attachment.h = 24.0
	return attachment
}

// SetLocation sets the location.
func (attachment *FileAttachment) SetLocation(x, y float32) {
	attachment.x = x
	attachment.y = y
}

// SetIconPushPin sets the push pin icon.
func (attachment *FileAttachment) SetIconPushPin() {
	attachment.icon = "PushPin"
}

// SetIconPaperclip sets the paper clip icon.
func (attachment *FileAttachment) SetIconPaperclip() {
	attachment.icon = "Paperclip"
}

// SetIconSize sets the icon size.
func (attachment *FileAttachment) SetIconSize(height float32) {
	attachment.h = height
}

// SetTitle sets the title.
func (attachment *FileAttachment) SetTitle(title string) {
	attachment.title = title
}

// SetDescription sets the description.
func (attachment *FileAttachment) SetDescription(description string) {
	attachment.contents = description
}

// DrawOn draws this component on the page.
func (attachment *FileAttachment) DrawOn(page *Page) {
	page.AddAnnotation(NewAnnotation(
		nil,
		nil,
		attachment.x,
		page.height-attachment.y,
		attachment.x+attachment.h,
		page.height-(attachment.y+attachment.h),
		"",
		"",
		""))
}
