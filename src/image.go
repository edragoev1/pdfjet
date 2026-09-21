// image.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"io"
	"math"
	"os"
	"strconv"

	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/internal/device"
	"github.com/edragoev1/pdfjet/v9/src/internal/imagetype"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Image describes an image object.
// The image type can be one of the following:
//
//	imagetype.JPG, imagetype.PNG or imagetype.BMP
//
// Please see Example_03 and Example_24.
type Image struct {
	objNumber      int
	pdf            *PDF    // The PDF the image was added to, or nil for an image of an existing PDF
	x              float32 // Position of the image on the page
	y              float32
	w              float32 // Image width
	h              float32 // Image height
	uri            string
	key            string
	degrees        int
	flipUpsideDown bool
	invertedInks   bool // A CMYK JPEG that Adobe software wrote, with its inks inverted
	language       string
	altDescription string
	actualText     string
}

// NewImageFromFile creates an image from the PNG, BMP or JPEG file at the specified path.
// It panics if the extension is not supported or the file cannot be opened.
func NewImageFromFile(pdf *PDF, filePath string) *Image {
	file, err := os.Open(filePath)
	if err != nil {
		panic(err)
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("Error closing file: " + err.Error())
		}
	}(file)
	return NewImage(pdf, bufio.NewReader(file))
}

// NewImage the main constructor for the Image class.
//   - pdf: the PDF to which we add this image.
//   - inputStream: the input stream to read the image from.
func NewImage(pdf *PDF, reader io.Reader) *Image {
	buf := content.GetFromStream(reader)
	imageType := imageTypeOf(buf)
	reader = bytes.NewReader(buf)
	image := new(Image)
	image.pdf = pdf

	switch imageType {
	case imagetype.JPG:
		jpg, err := newJPGImage(reader)
		if err != nil {
			panic(err)
		}
		data := jpg.getData()
		image.w = jpg.getWidth()
		image.h = jpg.getHeight()
		if jpg.getColorComponents() == 1 {
			image.addImageToPDF(pdf, data, nil, imageType, device.Gray, 8)
		} else if jpg.getColorComponents() == 3 {
			image.addImageToPDF(pdf, data, nil, imageType, device.RGB, 8)
		} else if jpg.getColorComponents() == 4 {
			image.invertedInks = jpg.isAdobe()
			image.addImageToPDF(pdf, data, nil, imageType, device.CMYK, 8)
		}
	case imagetype.PNG:
		png := newPNGImage(reader)
		data := png.GetData()
		image.w = png.GetWidth()
		image.h = png.GetHeight()
		if png.GetColorType() == 0 {
			image.addImageToPDF(pdf, data, nil, imageType, device.Gray, png.GetBitDepth())
		} else if png.GetColorType() == 4 {
			image.addImageToPDF(pdf, data, png.GetAlpha(), imageType, device.Gray, 8)
		} else {
			bitDepth := 8
			if png.GetBitDepth() == 16 {
				bitDepth = 16
			}
			image.addImageToPDF(pdf, data, png.GetAlpha(), imageType, device.RGB, bitDepth)
		}
		image.setPhysicalSize(png)
	case imagetype.BMP:
		bmp := newBMPImage(reader)
		data := bmp.getData()
		image.w = bmp.getWidth()
		image.h = bmp.getHeight()
		image.addImageToPDF(pdf, data, nil, imageType, device.RGB, 8)
	}

	return image
}

// NewImageForObjects adds this image to the existing PDF objects.
//   - objects: the map to which we add this image.
//   - inputStream: the input stream to read the image from.
func NewImageForObjects(objects *[]*PDFobj, reader io.Reader) *Image {
	buf := content.GetFromStream(reader)
	imageType := imageTypeOf(buf)
	reader = bytes.NewReader(buf)
	image := new(Image)

	switch imageType {
	case imagetype.JPG:
		jpg, err := newJPGImage(reader)
		if err != nil {
			panic(err)
		}
		data := jpg.getData()
		image.w = jpg.getWidth()
		image.h = jpg.getHeight()
		if jpg.getColorComponents() == 1 {
			image.addImageToObjects(objects, data, nil, imageType, device.Gray, 8)
		} else if jpg.getColorComponents() == 3 {
			image.addImageToObjects(objects, data, nil, imageType, device.RGB, 8)
		} else if jpg.getColorComponents() == 4 {
			image.invertedInks = jpg.isAdobe()
			image.addImageToObjects(objects, data, nil, imageType, device.CMYK, 8)
		}
	case imagetype.PNG:
		png := newPNGImage(reader)
		data := png.GetData()
		image.w = png.GetWidth()
		image.h = png.GetHeight()
		if png.GetColorType() == 0 {
			image.addImageToObjects(objects, data, nil, imageType, device.Gray, png.GetBitDepth())
		} else if png.GetColorType() == 4 {
			image.addImageToObjects(objects, data, png.GetAlpha(), imageType, device.Gray, 8)
		} else {
			bitDepth := 8
			if png.GetBitDepth() == 16 {
				bitDepth = 16
			}
			image.addImageToObjects(objects, data, png.GetAlpha(), imageType, device.RGB, bitDepth)
		}
		image.setPhysicalSize(png)
	case imagetype.BMP:
		bmp := newBMPImage(reader)
		data := bmp.getData()
		image.w = bmp.getWidth()
		image.h = bmp.getHeight()
		image.addImageToObjects(objects, data, nil, imageType, device.RGB, 8)
	}

	return image
}

// NewImageFromPDFobj constructs new image from an existing PDF object.
func NewImageFromPDFobj(pdf *PDF, obj *PDFobj) *Image {
	image := new(Image)
	image.pdf = pdf

	val, err := strconv.ParseFloat(obj.GetValue("/Width"), 32)
	if err != nil {
		panic(err)
	}
	image.w = float32(val)

	val, err = strconv.ParseFloat(obj.GetValue("/Height"), 32)
	if err != nil {
		panic(err)
	}
	image.h = float32(val)

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter ")
	pdf.appendString(obj.GetValue("/Filter"))
	pdf.appendString("\n")
	pdf.appendString("/Width ")
	pdf.appendFloat32(image.w)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendFloat32(image.h)
	pdf.appendString("\n")
	colorSpace := obj.GetValue("/ColorSpace")
	if colorSpace != "" {
		pdf.appendString("/ColorSpace ")
		pdf.appendString(colorSpace)
		pdf.appendString("\n")
	}
	pdf.appendString("/BitsPerComponent ")
	pdf.appendString(obj.GetValue("/BitsPerComponent"))
	pdf.appendString("\n")
	decodeParms := obj.GetValue("/DecodeParms")
	if decodeParms != "" {
		pdf.appendString("/DecodeParms ")
		pdf.appendString(decodeParms)
		pdf.appendString("\n")
	}
	imageMask := obj.GetValue("/ImageMask")
	if imageMask != "" {
		pdf.appendString("/ImageMask ")
		pdf.appendString(imageMask)
		pdf.appendString("\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(obj.stream))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(obj.stream)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()

	return image
}

// setPhysicalSize draws the image at the size its pHYs chunk asks for, when it
// has one. The width and the height are the pixels of the image until here,
// which is what the image object of the PDF is written with, and are the size
// it is drawn at from here on.
func (image *Image) setPhysicalSize(png *pngImage) {
	if png.physicalWidth > 0.0 && png.physicalHeight > 0.0 {
		image.w = png.physicalWidth
		image.h = png.physicalHeight
	}
}

// SetLocation sets the location of this image on the page to (x, y).
//
//   - x: the x coordinate of the top left corner of the image.
//   - y: the y coordinate of the top left corner of the image.
func (image *Image) SetLocation(x, y float32) Drawable {
	image.x = x
	image.y = y
	return image
}

// ScaleBy scales this image by the specified factor.
//   - factor: the factor used to scale the image.
func (image *Image) ScaleBy(factor float32) *Image {
	image.w *= factor
	image.h *= factor
	return image
}

// ScaleByWidthAndHeight scales this image by the specified width and height factor.
//
// Author: Pieter Libin, pieter@emweb.be
//
//   - widthFactor: the factor used to scale the width of the image
//   - heightFactor: the factor used to scale the height of the image
func (image *Image) ScaleByWidthAndHeight(widthFactor, heightFactor float32) *Image {
	image.w *= widthFactor
	image.h *= heightFactor
	return image
}

// ResizeWidth resizes the image to the specified width.
func (image *Image) ResizeWidth(width float32) *Image {
	factor := width / image.GetWidth()
	return image.ScaleByWidthAndHeight(factor, factor)
}

// ResizeHeight resizes the image to the specified height.
func (image *Image) ResizeHeight(height float32) *Image {
	factor := height / image.GetHeight()
	return image.ScaleByWidthAndHeight(factor, factor)
}

// SetURIAction sets the URI for the "click box" action.
//   - uri: the URI
func (image *Image) SetURIAction(uri string) *Image {
	image.uri = uri
	return image
}

// SetGoToAction sets the destination key for the action.
//   - key: the destination name.
func (image *Image) SetGoToAction(key string) *Image {
	image.key = key
	return image
}

// SetRotation rotates this image clockwise by 0, 90, 180 or 270 degrees, as every
// rotation in PDFjet turns. It panics on any other angle.
func (image *Image) SetRotation(degrees int) *Image {
	if degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270 {
		panic("The rotation angle must be 0, 90, 180 or 270")
	}
	image.degrees = degrees
	return image
}

// SetAltDescription sets the alternate description of this image.
//   - altDescription: the alternate description of the image.
//
// Returns this Image.
func (image *Image) SetAltDescription(altDescription string) *Image {
	image.altDescription = altDescription
	return image
}

// SetActualText sets the actual text for this image.
//   - actualText: the actual text for the image.
//
// Returns this Image.
func (image *Image) SetActualText(actualText string) *Image {
	image.actualText = actualText
	return image
}

// SetLanguage sets the language of this image, for example "en-US".
func (image *Image) SetLanguage(language string) *Image {
	image.language = language
	return image
}

// DrawOn draws this image on the specified page.
//   - page: the page to draw this image on.
//
// Returns x and y coordinates of the bottom right corner of this component.
func (image *Image) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{image.x + image.w, image.y + image.h} // Measured, not drawn
	}
	if image.pdf != nil && page.pdf != image.pdf {
		page.pdf.fail("The image belongs to another PDF.")
		return [2]float32{image.x + image.w, image.y + image.h}
	}
	if image.w == 0 || image.h == 0 {
		return [2]float32{image.x + image.w, image.y + image.h} // A zero size image paints nothing.
	}
	page.AddBDC(structelem.Figure, image.language, image.actualText, image.altDescription)
	page.SaveGraphicsState()

	switch image.degrees {
	case 0:
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(image.x)
		page.appendString(" ")
		page.appendFloat32(page.height - (image.y + image.h))
		page.appendString(" cm\n")
	case 90:
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(image.x)
		page.appendString(" ")
		page.appendFloat32(page.height - image.y)
		page.appendString(" cm\n")
		page.appendString("0 -1 1 0 0 0 cm\n")
	case 180:
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(image.x + image.w)
		page.appendString(" ")
		page.appendFloat32(page.height - image.y)
		page.appendString(" cm\n")
		page.appendString("-1 0 0 -1 0 0 cm\n")
	case 270:
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(image.x + image.h)
		page.appendString(" ")
		page.appendFloat32(page.height - (image.y + image.w))
		page.appendString(" cm\n")
		page.appendString("0 1 -1 0 0 0 cm\n")
	}

	if image.flipUpsideDown {
		page.appendString("1 0 0 -1 0 1 cm\n")
	}

	page.appendString("/Im")
	page.appendInteger(image.objNumber)
	page.appendString(" Do\n")

	page.RestoreGraphicsState()

	page.AddEMC()

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
		})
	}

	return [2]float32{image.x + image.w, image.y + image.h}
}

// GetWidth returns the width of this image when drawn on the page.
// The scaling is taken into account.
// Returns w - the width of this image.
func (image *Image) GetWidth() float32 {
	return image.w
}

// GetHeight returns the height of this image when drawn on the page.
// The scaling is taken into account.
// Returns h - the height of this image.
func (image *Image) GetHeight() float32 {
	return image.h
}

func (image *Image) addSoftMask(pdf *PDF, data []byte, colorSpace string, bitsPerComponent int) {
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Width ")
	pdf.appendInteger(int(image.w))
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendInteger(int(image.h))
	pdf.appendString("\n")
	pdf.appendString("/ColorSpace /")
	pdf.appendString(colorSpace)
	pdf.appendString("\n")
	pdf.appendString("/BitsPerComponent ")
	pdf.appendInteger(bitsPerComponent)
	pdf.appendString("\n")

	buf := data
	if pdf.encryption != nil {
		buf = pdf.encryption.encrypt(data)
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addImageToPDF(
	pdf *PDF,
	data []byte,
	alpha []byte,
	imageType imagetype.ImageType,
	colorSpace string,
	bitsPerComponent int) {
	if alpha != nil {
		image.addSoftMask(pdf, alpha, device.Gray, bitsPerComponent)
	}
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	switch imageType {
	case imagetype.JPG:
		pdf.appendString("/Filter /DCTDecode\n")
	case imagetype.PNG, imagetype.BMP:
		pdf.appendString("/Filter /FlateDecode\n")
		if alpha != nil {
			pdf.appendString("/SMask ")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" 0 R\n")
		}
	}
	pdf.appendString("/Width ")
	pdf.appendInteger(int(image.w))
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendInteger(int(image.h))
	pdf.appendString("\n")
	pdf.appendString("/ColorSpace /")
	pdf.appendString(colorSpace)
	pdf.appendString("\n")
	pdf.appendString("/BitsPerComponent ")
	pdf.appendInteger(bitsPerComponent)
	pdf.appendString("\n")
	if colorSpace == device.CMYK && image.invertedInks {
		// Adobe software, Photoshop among them, stores the inks inverted.
		pdf.appendString("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
	}

	buf := data
	if pdf.encryption != nil {
		buf = pdf.encryption.encrypt(data)
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addSoftMaskToObjects(
	objects *[]*PDFobj,
	data []byte,
	colorSpace string,
	bitsPerComponent int) {
	obj := newPDFobj()
	obj.dict = append(obj.dict, "<<")
	obj.dict = append(obj.dict, "/Type")
	obj.dict = append(obj.dict, "/XObject")
	obj.dict = append(obj.dict, "/Subtype")
	obj.dict = append(obj.dict, "/Image")
	obj.dict = append(obj.dict, "/Filter")
	obj.dict = append(obj.dict, "/FlateDecode")
	obj.dict = append(obj.dict, "/Width")
	obj.dict = append(obj.dict, strconv.Itoa(int(image.w)))
	obj.dict = append(obj.dict, "/Height")
	obj.dict = append(obj.dict, strconv.Itoa(int(image.h)))
	obj.dict = append(obj.dict, "/ColorSpace")
	obj.dict = append(obj.dict, "/"+colorSpace)
	obj.dict = append(obj.dict, "/BitsPerComponent")
	obj.dict = append(obj.dict, strconv.Itoa(bitsPerComponent))
	obj.dict = append(obj.dict, "/Length")
	obj.dict = append(obj.dict, strconv.Itoa(len(data)))
	obj.dict = append(obj.dict, ">>")
	obj.setStream(data)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	image.objNumber = obj.number
}

func (image *Image) addImageToObjects(
	objects *[]*PDFobj,
	data []byte,
	alpha []byte,
	imageType imagetype.ImageType,
	colorSpace string,
	bitsPerComponent int) {
	if alpha != nil {
		image.addSoftMaskToObjects(objects, alpha, device.Gray, bitsPerComponent)
	}

	obj := newPDFobj()
	obj.dict = append(obj.dict, "<<")
	obj.dict = append(obj.dict, "/Type")
	obj.dict = append(obj.dict, "/XObject")
	obj.dict = append(obj.dict, "/Subtype")
	obj.dict = append(obj.dict, "/Image")
	switch imageType {
	case imagetype.JPG:
		obj.dict = append(obj.dict, "/Filter")
		obj.dict = append(obj.dict, "/DCTDecode")
	case imagetype.PNG, imagetype.BMP:
		obj.dict = append(obj.dict, "/Filter")
		obj.dict = append(obj.dict, "/FlateDecode")
		if alpha != nil {
			obj.dict = append(obj.dict, "/SMask")
			obj.dict = append(obj.dict, strconv.Itoa(image.objNumber))
			obj.dict = append(obj.dict, "0")
			obj.dict = append(obj.dict, "R")
		}
	}
	obj.dict = append(obj.dict, "/Width")
	obj.dict = append(obj.dict, strconv.Itoa(int(image.w)))
	obj.dict = append(obj.dict, "/Height")
	obj.dict = append(obj.dict, strconv.Itoa(int(image.h)))
	obj.dict = append(obj.dict, "/ColorSpace")
	obj.dict = append(obj.dict, "/"+colorSpace)
	obj.dict = append(obj.dict, "/BitsPerComponent")
	obj.dict = append(obj.dict, strconv.Itoa(bitsPerComponent))
	if colorSpace == device.CMYK && image.invertedInks {
		// Adobe software, Photoshop among them, stores the inks inverted.
		obj.dict = append(obj.dict, "/Decode")
		obj.dict = append(obj.dict, "[")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "]")
	}
	obj.dict = append(obj.dict, "/Length")
	obj.dict = append(obj.dict, strconv.Itoa(len(data)))
	obj.dict = append(obj.dict, ">>")
	obj.setStream(data)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	image.objNumber = obj.number
}

// ResizeToFit resizes an image so it would fit on a page.
func (image *Image) ResizeToFit(page *Page, keepAspectRatio bool) {
	if keepAspectRatio {
		image.ScaleBy(float32(math.Min(
			float64((page.GetWidth()-image.x)/image.w),
			float64((page.GetHeight()-image.y)/image.h))))
	} else {
		image.ScaleByWidthAndHeight((page.GetWidth()-image.x)/image.w, (page.GetHeight()-image.y)/image.h)
	}
}

// SetFlipUpsideDown sets whether this image is drawn upside down.
func (image *Image) SetFlipUpsideDown(flipUpsideDown bool) *Image {
	image.flipUpsideDown = flipUpsideDown
	return image
}

// imageTypeOf returns the type of the image from its first bytes, and panics
// when the image is not a PNG, JPEG or BMP file.
func imageTypeOf(buf []byte) imagetype.ImageType {
	if len(buf) >= 4 && buf[0] == 0x89 && buf[1] == 'P' && buf[2] == 'N' && buf[3] == 'G' {
		return imagetype.PNG
	}
	if len(buf) >= 2 && buf[0] == 0xFF && buf[1] == 0xD8 {
		return imagetype.JPG
	}
	if len(buf) >= 2 && buf[0] == 'B' && buf[1] == 'M' {
		return imagetype.BMP
	}
	panic("The image is not a PNG, JPEG or BMP file.")
}
