/**
 *  Image.swift
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
import Foundation


///
/// Used to create image objects and draw them on a page.
/// The image type can be one of the following:
/// ImageType.JPG, ImageType.PNG, ImageType.BMP or ImageType.PNG_STREAM
///
/// Please see Example_03 and Example_24.
///
public class Image : Drawable {

    var objNumber: Int?

    var x: Float = 0.0      // Position of the image on the page
    var y: Float = 0.0
    var w: Float?           // Image width
    var h: Float?           // Image height

    var uri: String?
    var key: String?

    private var xBox: Float?
    private var yBox: Float?

    private var degrees = 0
    private var flipUpsideDown = false

    private var language: String?
    private var altDescription: String = Single.space
    private var actualText: String = Single.space


    enum StreamError: Error {
        case read
        case write
    }


    ///
    /// The main constructor for the Image class.
    ///
    /// @param pdf the PDF to which we add this image.
    /// @param stream the input stream to read the image from.
    /// @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
    ///
    public init(
            _ pdf: PDF,
            _ stream: InputStream,
            _ imageType: ImageType) throws {
        stream.open()
        if imageType == ImageType.JPG {
            let jpg = JPGImage(stream)
            w = Float(jpg.getWidth())
            h = Float(jpg.getHeight())
            if jpg.getColorComponents() == 1 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceGray", 8)
            }
            else if jpg.getColorComponents() == 3 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceRGB", 8)
            }
            else if jpg.getColorComponents() == 4 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceCMYK", 8)
            }
        }
        else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            w = Float(png.getWidth()!)
            h = Float(png.getHeight()!)
            if png.getColorType() == 0 {
                addImage(pdf, png.getData(), [UInt8](), imageType, "DeviceGray", png.getBitDepth())
            }
            else {
                if png.getBitDepth() == 16 {
                    addImage(pdf, png.getData(), [UInt8](), imageType, "DeviceRGB", 16)
                }
                else {
                    addImage(pdf, png.getData(), png.getAlpha(), imageType, "DeviceRGB", 8)
                }
            }
        }
        else if imageType == ImageType.BMP {
            let bmp = BMPImage(stream)
            w = Float(bmp.getWidth())
            h = Float(bmp.getHeight())
            addImage(pdf, bmp.getData(), [UInt8](), imageType, "DeviceRGB", 8)
        }
        else if imageType == ImageType.PNG_STREAM {
            try addImage(pdf, stream)
        }
        stream.close()
    }


    ///
    /// Constructor used to attach images to existing PDF.
    ///
    /// @param objects the map to which we add this image.
    /// @param stream the input stream to read the image from.
    /// @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
    ///
    public init(
            _ objects: inout [PDFobj],
            _ stream: InputStream,
            _ imageType: ImageType) throws {
        stream.open()
        var data: [UInt8]
        var alpha = [UInt8]()
        if imageType == ImageType.JPG {
            let jpg = JPGImage(stream)
            data = jpg.getData()
            w = Float(jpg.getWidth())
            h = Float(jpg.getHeight())
            if jpg.getColorComponents() == 1 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceGray", 8)
            }
            else if jpg.getColorComponents() == 3 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
            }
            else if jpg.getColorComponents() == 4 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceCMYK", 8)
            }
        }
        else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            data = png.getData()
            alpha = png.getAlpha()
            w = Float(png.getWidth()!)
            h = Float(png.getHeight()!)
            if png.getColorType() == 0 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceGray", png.getBitDepth())
            }
            else {
                if png.getBitDepth() == 16 {
                    addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 16)
                }
                else {
                    addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
                }
            }
        }
        else if imageType == ImageType.BMP {
            let bmp = BMPImage(stream)
            data = bmp.getData()
            w = Float(bmp.getWidth())
            h = Float(bmp.getHeight())
            addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
        }

        stream.close()
    }


    public init(_ pdf: PDF, _ obj: PDFobj) throws {
        w = Float(obj.getValue("/Width"))
        h = Float(obj.getValue("/Height"))
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter ")
        pdf.append(obj.getValue("/Filter"))
        pdf.append("\n")
        pdf.append("/Width ")
        pdf.append(w!)
        pdf.append("\n")
        pdf.append("/Height ")
        pdf.append(h!)
        pdf.append("\n")
        let colorSpace = obj.getValue("/ColorSpace")
        if colorSpace != "" {
            pdf.append("/ColorSpace ")
            pdf.append(colorSpace)
            pdf.append("\n")
        }
        pdf.append("/BitsPerComponent ")
        pdf.append(obj.getValue("/BitsPerComponent"))
        pdf.append("\n")
        let decodeParms = obj.getValue("/DecodeParms")
        if decodeParms != "" {
            pdf.append("/DecodeParms ")
            pdf.append(decodeParms)
            pdf.append("\n")
        }
        let imageMask = obj.getValue("/ImageMask")
        if imageMask != "" {
            pdf.append("/ImageMask ")
            pdf.append(imageMask)
            pdf.append("\n")
        }
        pdf.append("/Length ")
        pdf.append(obj.stream!.count)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(obj.stream!, 0, obj.stream!.count)
        pdf.append("\nendstream\n")
        pdf.endobj()
        pdf.images.append(self)
        objNumber = pdf.getObjNumber()
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    ///
    /// Sets the location of this image on the page to (x, y).
    ///
    /// @param x the x coordinate of the top left corner of the image.
    /// @param y the y coordinate of the top left corner of the image.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Image {
        self.x = x
        self.y = y
        return self
    }


    ///
    /// Scales this image by the specified factor.
    ///
    /// @param factor the factor used to scale the image.
    ///
    @discardableResult
    public func scaleBy(_ factor: Float) -> Image {
        return self.scaleBy(factor, factor)
    }


    ///
    /// Scales this image by the specified width and height factor.
    /// <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
    ///
    /// @param widthFactor the factor used to scale the width of the image
    /// @param heightFactor the factor used to scale the height of the image
    ///
    @discardableResult
    public func scaleBy(_ widthFactor: Float, _ heightFactor: Float) -> Image {
        self.w! = (self.w! * widthFactor).rounded()
        self.h! = (self.h! * heightFactor).rounded()
        return self
    }


    public func resizeWidth(_ width: Float) -> Image {
        let factor = width / getWidth()
        return self.scaleBy(factor, factor)
    }


    public func resizeHeight(_ height: Float) -> Image {
        let factor = height / getHeight()
        return self.scaleBy(factor, factor)
    }


    ///
    /// Places this image in the specified box.
    ///
    /// @param box the specified box.
    ///
    public func placeIn(_ box: Box) -> Image {
        self.xBox = box.x
        self.yBox = box.y
        return self
    }


    ///
    /// Sets the URI for the "click box" action.
    ///
    /// @param uri the URI
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> Image {
        self.uri = uri
        return self
    }


    ///
    /// Sets the destination key for the action.
    ///
    /// @param key the destination name.
    ///
    @discardableResult
    public func setGoToAction(_ key: String) -> Image {
        self.key = key
        return self
    }


    ///
    /// Sets the rotate90 flag.
    /// When the flag is true the image is rotated 90 degrees clockwise.
    ///
    /// @param rotate90 the flag.
    ///
    @discardableResult
    public func setRotateCW90(_ rotate90: Bool) -> Image {
        if rotate90 {
            self.degrees = 90
        }
        else {
            self.degrees = 0
        }
        return self
    }


    ///
    /// Sets the image rotation to the specified number of degrees.
    ///
    /// @param degrees the number of degrees.
    ///
    @discardableResult
    public func setRotate(_ degrees: Int) -> Image {
        self.degrees = degrees
        return self
    }


    ///
    /// Sets the alternate description of this image.
    ///
    /// @param altDescription the alternate description of the image.
    /// @return this Image.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Image {
        self.altDescription = altDescription
        return self
    }


    ///
    /// Sets the actual text for this image.
    ///
    /// @param actualText the actual text for the image.
    /// @return this Image.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Image {
        self.actualText = actualText
        return self
    }


    ///
    /// Draws this image on the specified page.
    ///
    /// @param page the page to draw this image on.
    /// @return x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {

        page!.addBMC(StructElem.P, language, actualText, altDescription)

        if xBox != nil {
            x += xBox!
        }
        if yBox != nil {
            y += yBox!
        }
        page!.append("q\n")

        if degrees == 0 {
            page!.append(w!)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(h!)
            page!.append(" ")
            page!.append(x.rounded())
            page!.append(" ")
            page!.append((page!.height - (y + h!)).rounded())
            page!.append(" cm\n")
        }
        else if degrees == 90 {
            page!.append(h!)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(w!)
            page!.append(" ")
            page!.append(x.rounded())
            page!.append(" ")
            page!.append((page!.height - y).rounded())
            page!.append(" cm\n")
            page!.append("0 -1 1 0 0 0 cm\n")
        }
        else if degrees == 180 {
            page!.append(w!)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(h!)
            page!.append(" ")
            page!.append((x + w!).rounded())
            page!.append(" ")
            page!.append((page!.height - y).rounded())
            page!.append(" cm\n")
            page!.append("-1 0 0 -1 0 0 cm\n")
        }
        else if degrees == 270 {
            page!.append(h!)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(0.0)
            page!.append(" ")
            page!.append(w!)
            page!.append(" ")
            page!.append((x + h!).rounded())
            page!.append(" ")
            page!.append((page!.height - (y + w!)).rounded())
            page!.append(" cm\n")
            page!.append("0 1 -1 0 0 0 cm\n")
        }

        if flipUpsideDown {
            page!.append("1 0 0 -1 0 0 cm\n")
        }

        page!.append("/Im")
        page!.append(objNumber!)
        page!.append(" Do\n")
        page!.append("Q\n")

        page!.addEMC()

        if uri != nil || key != nil {
            page!.addAnnotation(Annotation(
                    uri,
                    key,    // The destination name
                    x.rounded(),
                    y.rounded(),
                    (x + w!).rounded(),
                    (y + h!).rounded(),
                    language,
                    actualText,
                    altDescription))
        }

        return [x + w!, y + h!]
    }


    ///
    /// Returns the width of this image when drawn on the page.
    /// The scaling is take into account.
    ///
    /// @return w - the width of this image.
    ///
    public func getWidth() -> Float {
        return self.w!
    }


    ///
    /// Returns the height of this image when drawn on the page.
    /// The scaling is take into account.
    ///
    /// @return h - the height of this image.
    ///
    public func getHeight() -> Float {
        return self.h!
    }


    private func addSoftMask(
            _ pdf: PDF,
            _ alpha: [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) {
        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter /LZWDecode\n")
        // pdf.append("/Filter /FlateDecode\n")
        pdf.append("/Width ")
        pdf.append(Int(w!))
        pdf.append("\n")
        pdf.append("/Height ")
        pdf.append(Int(h!))
        pdf.append("\n")
        pdf.append("/ColorSpace /")
        pdf.append(colorSpace)
        pdf.append("\n")
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append("\n")
        pdf.append("/Length ")
        pdf.append(alpha.count)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(alpha, 0, alpha.count)
        pdf.append("\nendstream\n")
        pdf.endobj()
        objNumber = pdf.getObjNumber()
    }


    private func addImage(
            _ pdf: PDF,
            _ data: [UInt8],
            _ alpha: [UInt8],
            _ imageType: ImageType,
            _ colorSpace: String,
            _ bitsPerComponent: Int)  {

        if alpha.count > 0 {
            addSoftMask(pdf, alpha, "DeviceGray", bitsPerComponent)
        }

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        if imageType == ImageType.JPG {
            pdf.append("/Filter /DCTDecode\n")
        }
        else {
            pdf.append("/Filter /LZWDecode\n")
            // pdf.append("/Filter /FlateDecode\n")
            if alpha.count > 0 {
                pdf.append("/SMask ")
                pdf.append(objNumber!)
                pdf.append(" 0 R\n")
            }
        }
        pdf.append("/Width ")
        pdf.append(Int(w!))
        pdf.append("\n")
        pdf.append("/Height ")
        pdf.append(Int(h!))
        pdf.append("\n")
        pdf.append("/ColorSpace /")
        pdf.append(colorSpace)
        pdf.append("\n")
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append("\n")
        if colorSpace == "DeviceCMYK" {
            // If the image was created with Photoshop - invert the colors:
            pdf.append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
        }
        pdf.append("/Length ")
        pdf.append(data.count)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(data, 0, data.count)
        pdf.append("\nendstream\n")
        pdf.endobj()
        pdf.images.append(self)
        self.objNumber = pdf.getObjNumber()
    }


    // Used for .png.stream images!
    private func addImage(_ pdf: PDF, _ stream: InputStream) throws {

        self.w = Float(try getUInt32(stream)!)  // Width
        self.h = Float(try getUInt32(stream)!)  // Height
        let color = try getUInt8(stream)!       // Color Space
        let alpha = try getUInt8(stream)!       // Alpha

        if alpha != 0 {
            pdf.newobj()
            pdf.append("<<\n")
            pdf.append("/Type /XObject\n")
            pdf.append("/Subtype /Image\n")
            pdf.append("/Filter /FlateDecode\n")
            // pdf.append("/Filter /LZWDecode\n")
            pdf.append("/Width ")
            pdf.append(Int(w!))
            pdf.append("\n")
            pdf.append("/Height ")
            pdf.append(Int(h!))
            pdf.append("\n")
            pdf.append("/ColorSpace /DeviceGray\n")
            pdf.append("/BitsPerComponent 8\n")
            let length = Int(try getUInt32(stream)!)
            pdf.append("/Length ")
            pdf.append(length)
            pdf.append("\n")
            pdf.append(">>\n")
            pdf.append("stream\n")
            var buffer = [UInt8](repeating: 0, count: length)
            let count = stream.read(&buffer, maxLength: length)
            pdf.append(buffer, 0, count)
            pdf.append("\nendstream\n")
            pdf.endobj()
            objNumber = pdf.getObjNumber()
        }

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter /FlateDecode\n")
        // pdf.append("/Filter /LZWDecode\n")
        if alpha != 0 {
            pdf.append("/SMask ")
            pdf.append(objNumber!)
            pdf.append(" 0 R\n")
        }
        pdf.append("/Width ")
        pdf.append(Int(w!))
        pdf.append("\n")
        pdf.append("/Height ")
        pdf.append(Int(h!))
        pdf.append("\n")
        pdf.append("/ColorSpace /")
        if color == 1 {
            pdf.append("DeviceGray")
        }
        else if color == 3 || color == 6 {
            pdf.append("DeviceRGB")
        }
        pdf.append("\n")
        pdf.append("/BitsPerComponent 8\n")
        pdf.append("/Length ")
        pdf.append(try getUInt32(stream)!)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let count = stream.read(&buffer, maxLength: buffer.count)
            if count > 0 {
                pdf.append(buffer, 0, count)
            }
        }
        pdf.append("\nendstream\n")
        pdf.endobj()
        pdf.images.append(self)
        objNumber = pdf.getObjNumber()
    }


    private func getUInt8(_ stream: InputStream) throws -> UInt8? {
        var buffer = [UInt8](repeating: 0, count: 1)
        if stream.read(&buffer, maxLength: 1) == 1 {
            return buffer[0]
        }
        throw StreamError.read
    }


    private func getUInt32(_ stream: InputStream) throws -> UInt32? {
        var buffer = [UInt8](repeating: 0, count: 4)
        if stream.read(&buffer, maxLength: 4) == 4 {
            var value = UInt32(buffer[0]) << 24
            value |= UInt32(buffer[1]) << 16
            value |= UInt32(buffer[2]) <<  8
            value |= UInt32(buffer[3])
            return value
        }
        throw StreamError.read
    }


    private func addSoftMask2(
            _ objects: inout [PDFobj],
            _ data: inout [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) {

        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/XObject")
        obj.dict.append("/Subtype")
        obj.dict.append("/Image")
        obj.dict.append("/Filter")
        // obj.dict.append("/FlateDecode")
        obj.dict.append("/LZWDecode")
        obj.dict.append("/Width")
        obj.dict.append(String(Int(w!)))
        obj.dict.append("/Height")
        obj.dict.append(String(Int(h!)))
        obj.dict.append("/ColorSpace")
        obj.dict.append("/" + colorSpace)
        obj.dict.append("/BitsPerComponent")
        obj.dict.append(String(bitsPerComponent))
        obj.dict.append("/Length")
        obj.dict.append(String(data.count))
        obj.dict.append(">>")
        obj.setStream(&data)
        obj.number = objects.count + 1
        objects.append(obj)

        objNumber = obj.number
    }


    func addImageToObjects(
            _ objects: inout [PDFobj],
            _ data: inout [UInt8],
            _ alpha: inout [UInt8],
            _ imageType: ImageType,
            _ colorSpace: String,
            _ bitsPerComponent: Int) {

        if !alpha.isEmpty {
            addSoftMask2(&objects, &alpha, "DeviceGray", bitsPerComponent)
        }

        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/XObject")
        obj.dict.append("/Subtype")
        obj.dict.append("/Image")
        if imageType == ImageType.JPG {
            obj.dict.append("/Filter")
            obj.dict.append("/DCTDecode")
        }
        else if imageType == ImageType.PNG ||
                imageType == ImageType.BMP {
            obj.dict.append("/Filter")
            // obj.dict.append("/FlateDecode")
            obj.dict.append("/LZWDecode")
            if !alpha.isEmpty {
                obj.dict.append("/SMask")
                obj.dict.append(String(objNumber!))
                obj.dict.append("0")
                obj.dict.append("R")
            }
        }
        obj.dict.append("/Width")
        obj.dict.append(String(Int(w!)))
        obj.dict.append("/Height")
        obj.dict.append(String(Int(h!)))
        obj.dict.append("/ColorSpace")
        obj.dict.append("/" + colorSpace)
        obj.dict.append("/BitsPerComponent")
        obj.dict.append(String(bitsPerComponent))
        if colorSpace == "DeviceCMYK" {
            // If the image was created with Photoshop - invert the colors:
            obj.dict.append("/Decode")
            obj.dict.append("[")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("]")
        }
        obj.dict.append("/Length")
        obj.dict.append(String(data.count))
        obj.dict.append(">>")
        obj.setStream(&data)
        obj.number = objects.count + 1
        objects.append(obj)

        objNumber = obj.number
    }


    public func resizeToFit(_ page: Page, keepAspectRatio: Bool) {
        if keepAspectRatio {
            self.scaleBy(min((page.width - self.x)/self.w!, (page.height - self.y)/self.h!))
        }
        else {
            self.scaleBy((page.width - self.x)/self.w!, (page.height - self.y)/self.h!)
        }
    }


    public func flipUpsideDown(_ flipUpsideDown: Bool) {
        self.flipUpsideDown = flipUpsideDown
    }

}   // End of Image.swift
