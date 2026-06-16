package pdfjet

/**
 * attachmentattachment.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// FileAttachment describes file attachment object.
type FileAttachment struct {
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
	icon := "PushPin"
	attachment.icon = icon
	contents := "Right mouse click on the icon to save the attached attachment."
	attachment.contents = contents
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
func (attachment *FileAttachment) DrawOn(page *Page) [2]float32 {
	annotation := &Annotation{
		annotationType: AnnotationFileAttachment,
		x1:             attachment.x,
		y1:             attachment.y,
		x2:             attachment.x + attachment.h,
		y2:             attachment.y + attachment.h,
		vertices:       nil,
		fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
		transparency:   0.0,
		title:          "",
		contents:       "",
		uri:            "",
		key:            "",
		language:       "",
		actualText:     "",
		altDescription: ""}
	annotation.fileAttachment = attachment
	page.AddAnnotation(annotation)
	return [2]float32{attachment.x + attachment.h, attachment.y + attachment.h}
}
