package pdfjet

/**
 * attachmentattachment.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
