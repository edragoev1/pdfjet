package pdfjet

/**
 * image.go
 *
Copyright 2023 Innovatics Inc.

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

import (
	"bufio"
	"io"
	"log"
	"math"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/device"
	"github.com/edragoev1/pdfjet/src/imagetype"
	"github.com/edragoev1/pdfjet/src/single"
)

// Image describes an image object.
// The image type can be one of the following:
//
//	imagetype.JPG, imagetype.PNG, imagetype.BMP or imagetype.PNG_STREAM
//
// Please see Example_03 and Example_24.
type Image struct {
	objNumber      int
	x              float32 // Position of the image on the page
	y              float32
	w              float32 // Image width
	h              float32 // Image height
	uri            *string
	key            *string
	xBox           float32
	yBox           float32
	degrees        int
	flipUpsideDown bool
	language       string
	altDescription string
	actualText     string
}

func NewImageFromFile(pdf *PDF, filePath string) *Image {
	var imageType int
	if strings.HasSuffix(strings.ToLower(filePath), ".png.stream") {
		imageType = imagetype.PNGStream
	} else if strings.HasSuffix(strings.ToLower(filePath), ".png") {
		imageType = imagetype.PNG
	} else if strings.HasSuffix(strings.ToLower(filePath), ".bmp") {
		imageType = imagetype.BMP
	} else if strings.HasSuffix(strings.ToLower(filePath), ".jpg") ||
		strings.HasSuffix(strings.ToLower(filePath), ".jpeg") {
		imageType = imagetype.JPG
	} else {
		log.Fatal("Invalid image file extension.")
	}
	file, err := os.Open(filePath)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	return NewImage(pdf, bufio.NewReader(file), imageType)
}

// NewImage the main constructor for the Image class.
// @param pdf the PDF to which we add this image.
// @param inputStream the input stream to read the image from.
// @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
func NewImage(pdf *PDF, reader io.Reader, imageType int) *Image {
	image := new(Image)
	image.altDescription = single.Space
	image.actualText = single.Space

	if imageType == imagetype.JPG {
		jpg := NewJPGImage(reader)
		data := jpg.GetData()
		image.w = float32(jpg.GetWidth())
		image.h = float32(jpg.GetHeight())
		if jpg.GetColorComponents() == 1 {
			image.addImageToPDF(pdf, data, nil, imageType, device.Gray, 8)
		} else if jpg.GetColorComponents() == 3 {
			image.addImageToPDF(pdf, data, nil, imageType, device.RGB, 8)
		} else if jpg.GetColorComponents() == 4 {
			image.addImageToPDF(pdf, data, nil, imageType, device.CMYK, 8)
		}
	} else if imageType == imagetype.PNG {
		png := NewPNGImage(reader)
		data := png.GetData()
		image.w = float32(png.GetWidth())
		image.h = float32(png.GetHeight())
		if png.GetColorType() == 0 {
			image.addImageToPDF(pdf, data, nil, imageType, device.Gray, png.GetBitDepth())
		} else {
			bitDepth := 8
			if png.GetBitDepth() == 16 {
				bitDepth = 16
			}
			image.addImageToPDF(pdf, data, png.GetAlpha(), imageType, device.RGB, bitDepth)
		}
	} else if imageType == imagetype.BMP {
		bmp := NewBMPImage(reader)
		data := bmp.GetData()
		image.w = float32(bmp.GetWidth())
		image.h = float32(bmp.GetHeight())
		image.addImageToPDF(pdf, data, nil, imageType, device.RGB, 8)
	} else if imageType == imagetype.PNGStream {
		image.addPNGStreamImage(pdf, reader)
	}

	return image
}

// NewImage2 adds this image to the existing PDF objects.
// @param objects the map to which we add this image.
// @param inputStream the input stream to read the image from.
// @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
func NewImage2(objects *[]*PDFobj, reader io.Reader, imageType int) *Image {
	image := new(Image)

	if imageType == imagetype.JPG {
		jpg := NewJPGImage(reader)
		data := jpg.GetData()
		image.w = float32(jpg.GetWidth())
		image.h = float32(jpg.GetHeight())
		if jpg.GetColorComponents() == 1 {
			image.addImageToObjects(objects, data, nil, imageType, device.Gray, 8)
		} else if jpg.GetColorComponents() == 3 {
			image.addImageToObjects(objects, data, nil, imageType, device.RGB, 8)
		} else if jpg.GetColorComponents() == 4 {
			image.addImageToObjects(objects, data, nil, imageType, device.CMYK, 8)
		}
	} else if imageType == imagetype.PNG {
		png := NewPNGImage(reader)
		data := png.GetData()
		image.w = png.GetWidth()
		image.h = png.GetHeight()
		if png.GetColorType() == 0 {
			image.addImageToObjects(objects, data, nil, imageType, device.Gray, png.GetBitDepth())
		} else {
			bitDepth := 8
			if png.GetBitDepth() == 16 {
				bitDepth = 16
			}
			image.addImageToObjects(objects, data, png.GetAlpha(), imageType, device.RGB, bitDepth)
		}
	} else if imageType == imagetype.BMP {
		bmp := NewBMPImage(reader)
		data := bmp.GetData()
		image.w = bmp.GetWidth()
		image.h = bmp.GetHeight()
		image.addImageToObjects(objects, data, nil, imageType, device.RGB, 8)
	}

	return image
}

// NewImageFromPDFobj constructs new image from an existing PDF object.
func NewImageFromPDFobj(pdf *PDF, obj *PDFobj) *Image {
	image := new(Image)
	image.altDescription = single.Space
	image.actualText = single.Space

	val, err := strconv.ParseFloat(obj.getValue("/Width"), 32)
	if err != nil {
		log.Fatal(err)
	}
	image.w = float32(val)

	val, err = strconv.ParseFloat(obj.getValue("/Height"), 32)
	if err != nil {
		log.Fatal(err)
	}
	image.h = float32(val)

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter ")
	pdf.appendString(obj.getValue("/Filter"))
	pdf.appendString("\n")
	pdf.appendString("/Width ")
	pdf.appendFloat32(image.w)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendFloat32(image.h)
	pdf.appendString("\n")
	colorSpace := obj.getValue("/ColorSpace")
	if colorSpace != "" {
		pdf.appendString("/ColorSpace ")
		pdf.appendString(colorSpace)
		pdf.appendString("\n")
	}
	pdf.appendString("/BitsPerComponent ")
	pdf.appendString(obj.getValue("/BitsPerComponent"))
	pdf.appendString("\n")
	decodeParms := obj.getValue("/DecodeParms")
	if decodeParms != "" {
		pdf.appendString("/DecodeParms ")
		pdf.appendString(decodeParms)
		pdf.appendString("\n")
	}
	imageMask := obj.getValue("/ImageMask")
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
	pdf.endobj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()

	return image
}

// SetLocation sets the location of this image on the page to (x, y).
//
// @param x the x coordinate of the top left corner of the image.
// @param y the y coordinate of the top left corner of the image.
func (image *Image) SetLocation(x, y float32) *Image {
	image.x = x
	image.y = y
	return image
}

// SetPosition sets the location of this image on the page to (x, y).
//
// @param x the x coordinate of the top left corner of the image.
// @param y the y coordinate of the top left corner of the image.
func (image *Image) SetPosition(x, y float32) {
	image.x = x
	image.y = y
}

// ScaleBy scales this image by the specified factor.
// @param factor the factor used to scale the image.
func (image *Image) ScaleBy(factor float32) *Image {
	image.w *= factor
	image.h *= factor
	return image
}

// ScaleByWidthAndHeight scales this image by the specified width and height factor.
// <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
//
// @param widthFactor the factor used to scale the width of the image
// @param heightFactor the factor used to scale the height of the image
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

// PlaceIn places this image in the specified box.
// @param box the specified box.
func (image *Image) PlaceIn(box *Box) {
	image.xBox = box.x
	image.yBox = box.y
}

// SetURIAction sets the URI for the "click box" action.
// @param uri the URI
func (image *Image) SetURIAction(uri *string) {
	image.uri = uri
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
func (image *Image) SetGoToAction(key *string) {
	image.key = key
}

// SetRotate sets the image rotation to the specified number of degrees.
// @param degrees the number of degrees.
func (image *Image) RotateClockwise(degrees int) {
	if degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270 {
		log.Fatal("The rotation angle must be 0, 90, 180 or 270")
	}
	image.degrees = degrees
}

// SetAltDescription sets the alternate description of this image.
// @param altDescription the alternate description of the image.
// @return this Image.
func (image *Image) SetAltDescription(altDescription string) *Image {
	image.altDescription = altDescription
	return image
}

// SetActualText sets the actual text for this image.
// @param actualText the actual text for the image.
// @return this Image.
func (image *Image) SetActualText(actualText string) *Image {
	image.actualText = actualText
	return image
}

// DrawOn draws this image on the specified page.
// @param page the page to draw this image on.
// @return x and y coordinates of the bottom right corner of this component.
func (image *Image) DrawOn(page *Page) [2]float32 {
	page.AddBMC("Span", image.language, image.actualText, image.altDescription)

	image.x += image.xBox
	image.y += image.yBox
	appendString(&page.buf, "q\n")

	if image.degrees == 0 {
		appendFloat32(&page.buf, image.w)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.h)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.x)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, page.height-(image.y+image.h))
		appendString(&page.buf, " cm\n")
	} else if image.degrees == 90 {
		appendFloat32(&page.buf, image.h)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.w)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.x)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, page.height-image.y)
		appendString(&page.buf, " cm\n")
		appendString(&page.buf, "0 -1 1 0 0 0 cm\n")
	} else if image.degrees == 180 {
		appendFloat32(&page.buf, image.w)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.h)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.x+image.w)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, page.height-image.y)
		appendString(&page.buf, " cm\n")
		appendString(&page.buf, "-1 0 0 -1 0 0 cm\n")
	} else if image.degrees == 270 {
		appendFloat32(&page.buf, image.h)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, 0.0)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.w)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, image.x+image.h)
		appendString(&page.buf, " ")
		appendFloat32(&page.buf, page.height-(image.y+image.w))
		appendString(&page.buf, " cm\n")
		appendString(&page.buf, "0 1 -1 0 0 0 cm\n")
	}

	if image.flipUpsideDown {
		appendString(&page.buf, "1 0 0 -1 0 0 cm\n")
	}

	appendString(&page.buf, "/Im")
	appendInteger(&page.buf, image.objNumber)
	appendString(&page.buf, " Do\n")
	appendString(&page.buf, "Q\n")

	page.AddEMC()

	if image.uri != nil || image.key != nil {
		page.AddAnnotation(NewAnnotation(
			image.uri,
			image.key, // The destination name
			image.x,
			image.y,
			image.x+image.w,
			image.y+image.h,
			image.language,
			image.actualText,
			image.altDescription))
	}

	return [2]float32{image.x + image.w, image.y + image.h}
}

// GetWidth returns the width of this image when drawn on the page.
// The scaling is taken into account.
// @return w - the width of this image.
func (image *Image) GetWidth() float32 {
	return image.w
}

// GetHeight returns the height of this image when drawn on the page.
// The scaling is taken into account.
// @return h - the height of this image.
func (image *Image) GetHeight() float32 {
	return image.h
}

func (image *Image) addSoftMask(pdf *PDF, data []byte, colorSpace string, bitsPerComponent int) {
	pdf.newobj()
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
	pdf.appendString("/Length ")
	pdf.appendInteger(len(data))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(data)
	pdf.appendString("\nendstream\n")
	pdf.endobj()
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addImageToPDF(
	pdf *PDF,
	data []byte,
	alpha []byte,
	imageType int,
	colorSpace string,
	bitsPerComponent int) {
	if alpha != nil {
		image.addSoftMask(pdf, alpha, device.Gray, bitsPerComponent)
	}
	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	if imageType == imagetype.JPG {
		pdf.appendString("/Filter /DCTDecode\n")
	} else if imageType == imagetype.PNG || imageType == imagetype.BMP {
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
	if colorSpace == device.CMYK {
		// If the image was created with Photoshop - invert the colors:
		pdf.appendString("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(data))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(data)
	pdf.appendString("\nendstream\n")
	pdf.endobj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addPNGStreamImage(pdf *PDF, reader io.Reader) {
	image.w = float32(getUint32(reader)) // Width
	image.h = float32(getUint32(reader)) // Height
	colorspace := getUint8(reader)       // Color Space
	alpha := getUint8(reader)            // Alpha

	if alpha != 0 {
		pdf.newobj()
		pdf.appendString("<<\n")
		pdf.appendString("/Type /XObject\n")
		pdf.appendString("/Subtype /Image\n")
		pdf.appendString("/Filter /FlateDecode\n")
		pdf.appendString("/Width ")
		pdf.appendFloat32(image.w)
		pdf.appendString("\n")
		pdf.appendString("/Height ")
		pdf.appendFloat32(image.h)
		pdf.appendString("\n")
		pdf.appendString("/ColorSpace /")
		pdf.appendString(device.Gray)
		pdf.appendString("\n")
		pdf.appendString("/BitsPerComponent 8\n")
		length := int(getUint32(reader))
		pdf.appendString("/Length ")
		pdf.appendInteger(length)
		pdf.appendString("\n")
		pdf.appendString(">>\n")
		pdf.appendString("stream\n")
		pdf.appendByteArray(getNBytes(reader, length))
		pdf.appendString("\nendstream\n")
		pdf.endobj()
		image.objNumber = pdf.getObjNumber()
	}

	pdf.newobj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter /FlateDecode\n")
	if alpha != 0 {
		pdf.appendString("/SMask ")
		pdf.appendInteger(image.objNumber)
		pdf.appendString(" 0 R\n")
	}
	pdf.appendString("/Width ")
	pdf.appendFloat32(image.w)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendFloat32(image.h)
	pdf.appendString("\n")
	pdf.appendString("/ColorSpace /")
	if colorspace == 1 {
		pdf.appendString(device.Gray)
	} else if colorspace == 3 || colorspace == 6 {
		pdf.appendString(device.RGB)
	}
	pdf.appendString("\n")
	pdf.appendString("/BitsPerComponent 8\n")
	length := int(getUint32(reader))
	pdf.appendString("/Length ")
	pdf.appendInteger(length)
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(getNBytes(reader, length))
	pdf.appendString("\nendstream\n")
	pdf.endobj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addSoftMaskToObjects(
	objects *[]*PDFobj,
	data []byte,
	colorSpace string,
	bitsPerComponent int) {
	obj := NewPDFobj()
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
	obj.SetStream(data)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	image.objNumber = obj.number
}

func (image *Image) addImageToObjects(
	objects *[]*PDFobj,
	data []byte,
	alpha []byte,
	imageType int,
	colorSpace string,
	bitsPerComponent int) {
	if alpha != nil {
		image.addSoftMaskToObjects(objects, alpha, device.Gray, bitsPerComponent)
	}

	obj := NewPDFobj()
	obj.dict = append(obj.dict, "<<")
	obj.dict = append(obj.dict, "/Type")
	obj.dict = append(obj.dict, "/XObject")
	obj.dict = append(obj.dict, "/Subtype")
	obj.dict = append(obj.dict, "/Image")
	if imageType == imagetype.JPG {
		obj.dict = append(obj.dict, "/Filter")
		obj.dict = append(obj.dict, "/DCTDecode")
	} else if imageType == imagetype.PNG || imageType == imagetype.BMP {
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
	if colorSpace == device.CMYK {
		// If the image was created with Photoshop - invert the colors:
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
	obj.SetStream(data)
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

// FlipUpsideDown flips the image upside down.
func (image *Image) FlipUpsideDown(flipUpsideDown bool) {
	image.flipUpsideDown = flipUpsideDown
}
