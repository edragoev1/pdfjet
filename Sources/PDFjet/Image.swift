/**
 * Image.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create image objects and draw them on a page.
/// The image type can be one of the following:
/// ImageType.JPG, ImageType.PNG or ImageType.BMP
///
/// Please see Example_03 and Example_24.
///
public class Image : Drawable {
    var objNumber: Int?
    // The identity of the PDF the image was added to, or nil for an image of an existing PDF.
    var pdfIdentity: UUID?

    var x: Float = 0.0      // Position of the image on the page
    var y: Float = 0.0
    var w: Float?           // Image width
    var h: Float?           // Image height

    var uri: String?
    var key: String?

    private var degrees = 0
    private var flipUpsideDown = false
    // True for a CMYK JPEG that Adobe software wrote, with its inks inverted.
    private var invertedInks = false

    private var language: String?
    private var altDescription: String?
    private var actualText: String?

    enum StreamError: Error {
        case read
        case write
    }

    enum ImageError: Error {
    case format(String)
        case rotation(String)
    }

    ///
    /// The main constructor for the Image class.
    ///
    /// - Parameter pdf: the PDF to which we add this image.
    /// - Parameter filePath: the path to the image file.
    ///
    public convenience init(_ pdf: PDF, _ filePath: String) throws {
        guard let stream = InputStream(fileAtPath: filePath),
                FileManager.default.fileExists(atPath: filePath) else {
            throw PDFjetError(message: "File not found: " + filePath)
        }
        try self.init(pdf, stream)
    }

    ///
    /// The main constructor for the Image class.
    ///
    /// - Parameter pdf: the PDF to which we add this image.
    /// - Parameter stream: the input stream to read the image from.
    ///
    public convenience init(_ pdf: PDF, _ stream: InputStream) throws {
        try self.init(pdf, try Content.getFromStream(stream))
    }

    private init(_ pdf: PDF, _ bytes: [UInt8]) throws {
        self.pdfIdentity = pdf.identity
        let imageType = try Image.typeOf(bytes)
        let stream = InputStream(data: Data(bytes))
        if imageType == ImageType.JPG {
            let jpg = try JPGImage(stream)
            w = Float(jpg.getWidth())
            h = Float(jpg.getHeight())
            if jpg.getColorComponents() == 1 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceGray", 8)
            } else if jpg.getColorComponents() == 3 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceRGB", 8)
            } else if jpg.getColorComponents() == 4 {
                invertedInks = jpg.isAdobe()
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceCMYK", 8)
            }
        } else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            w = Float(png.getWidth())
            h = Float(png.getHeight())
            if png.getColorType() == 0 {
                addImage(pdf, png.getData(), [UInt8](), imageType, "DeviceGray", png.getBitDepth())
            } else if png.getColorType() == 4 {
                addImage(pdf, png.getData(), png.getAlpha() ?? [UInt8](), imageType, "DeviceGray", 8)
            } else {
                if png.getBitDepth() == 16 {
                    addImage(pdf, png.getData(), [UInt8](), imageType, "DeviceRGB", 16)
                } else {
                    addImage(pdf, png.getData(), png.getAlpha() ?? [UInt8](), imageType, "DeviceRGB", 8)
                }
            }
            setPhysicalSize(png)
        } else if imageType == ImageType.BMP {
            let bmp = try BMPImage(stream)
            w = Float(bmp.getWidth())
            h = Float(bmp.getHeight())
            addImage(pdf, bmp.getData(), [UInt8](), imageType, "DeviceRGB", 8)
        }
    }

    ///
    /// Constructor used to attach images to existing PDF.
    ///
    /// - Parameter objects: the map to which we add this image.
    /// - Parameter stream: the input stream to read the image from.
    ///
    public convenience init(_ objects: inout [PDFobj], _ stream: InputStream) throws {
        try self.init(&objects, try Content.getFromStream(stream))
    }

    private init(_ objects: inout [PDFobj], _ bytes: [UInt8]) throws {
        let imageType = try Image.typeOf(bytes)
        let stream = InputStream(data: Data(bytes))
        var data: [UInt8]
        var alpha = [UInt8]()
        if imageType == ImageType.JPG {
            let jpg = try JPGImage(stream)
            data = jpg.getData()
            w = Float(jpg.getWidth())
            h = Float(jpg.getHeight())
            if jpg.getColorComponents() == 1 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceGray", 8)
            } else if jpg.getColorComponents() == 3 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
            } else if jpg.getColorComponents() == 4 {
                invertedInks = jpg.isAdobe()
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceCMYK", 8)
            }
        } else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            data = png.getData()
            alpha = png.getAlpha() ?? [UInt8]()
            var noAlpha = [UInt8]()
            w = Float(png.getWidth())
            h = Float(png.getHeight())
            if png.getColorType() == 0 {
                addImageToObjects(&objects, &data, &noAlpha, imageType, "DeviceGray", png.getBitDepth())
            } else if png.getColorType() == 4 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceGray", 8)
            } else {
                if png.getBitDepth() == 16 {
                    addImageToObjects(&objects, &data, &noAlpha, imageType, "DeviceRGB", 16)
                } else {
                    addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
                }
            }
            setPhysicalSize(png)
        } else if imageType == ImageType.BMP {
            let bmp = try BMPImage(stream)
            data = bmp.getData()
            w = Float(bmp.getWidth())
            h = Float(bmp.getHeight())
            addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
        }
    }

    /// Creates an image from an image object read from an existing PDF.
    public init(_ pdf: PDF, _ obj: PDFobj) throws {
        self.pdfIdentity = pdf.identity
        w = Float(obj.getValue("/Width"))
        h = Float(obj.getValue("/Height"))
        pdf.newObj()
        pdf.append(Token.beginDictionary)
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
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(obj.stream!, 0, obj.stream!.count)
        pdf.append(Token.endStream)
        pdf.endObj()
        pdf.images.append(self)
        objNumber = pdf.getObjNumber()
    }

    ///
    // Draws the image at the size its pHYs chunk asks for, when it has one.
    // The width and the height are the pixels of the image until here, which
    // is what the image object of the PDF is written with, and are the size it
    // is drawn at from here on.
    private func setPhysicalSize(_ png: PNGImage) {
        if png.getPhysicalWidth() > 0.0 && png.getPhysicalHeight() > 0.0 {
            self.w = png.getPhysicalWidth()
            self.h = png.getPhysicalHeight()
        }
    }

    /// Sets the location of this image on the page to (x, y).
    ///
    /// - Parameter x: the x coordinate of the top left corner of the image.
    /// - Parameter y: the y coordinate of the top left corner of the image.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Scales this image by the specified factor.
    ///
    /// - Parameter factor: the factor used to scale the image.
    ///
    @discardableResult
    public func scaleBy(_ factor: Float) -> Image {
        return self.scaleBy(factor, factor)
    }

    ///
    /// Scales this image by the specified width and height factor.
    ///
    /// *Author:* **Pieter Libin**, pieter@emweb.be
    ///
    /// - Parameter widthFactor: the factor used to scale the width of the image
    /// - Parameter heightFactor: the factor used to scale the height of the image
    ///
    @discardableResult
    public func scaleBy(_ widthFactor: Float, _ heightFactor: Float) -> Image {
        self.w! *= widthFactor
        self.h! *= heightFactor
        return self
    }

    /// Scales this image proportionally to the specified width.
    @discardableResult
    public func resizeWidth(_ width: Float) -> Image {
        let factor = width / getWidth()
        return self.scaleBy(factor, factor)
    }

    /// Scales this image proportionally to the specified height.
    @discardableResult
    public func resizeHeight(_ height: Float) -> Image {
        let factor = height / getHeight()
        return self.scaleBy(factor, factor)
    }

    ///
    /// Sets the URI for the "click box" action.
    ///
    /// - Parameter uri: the URI
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> Image {
        self.uri = uri
        return self
    }

    ///
    /// Sets the destination key for the action.
    ///
    /// - Parameter key: the destination name.
    ///
    @discardableResult
    public func setGoToAction(_ key: String) -> Image {
        self.key = key
        return self
    }

    /// Rotates this image clockwise by 0, 90, 180 or 270 degrees, as every rotation in PDFjet turns.
    @discardableResult
    public func setRotation(_ degrees: Int) throws -> Image {
        if degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270 {
            throw ImageError.rotation("The rotation angle must be 0, 90, 180 or 270")
        }
        self.degrees = degrees
        return self
    }

    ///
    /// Sets the alternate description of this image.
    ///
    /// - Parameter altDescription: the alternate description of the image.
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Image {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this image.
    ///
    /// - Parameter actualText: the actual text for the image.
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Image {
        self.actualText = actualText
        return self
    }

    ///
    /// Sets the language of this image.
    ///
    /// - Parameter language: the language, for example "en-US".
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setLanguage(_ language: String) -> Image {
        self.language = language
        return self
    }

    ///
    /// Draws this image on the specified page.
    ///
    /// - Parameter page: the page to draw this image on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [x + w!, y + h!]     // Measured, not drawn
        }
        if let identity = pdfIdentity, identity != page.pdf.identity {
            page.pdf.fail("The image belongs to another PDF.")
            return [x + w!, y + h!]
        }
        if w! == 0.0 || h! == 0.0 {
            return [x + w!, y + h!]     // A zero size image paints nothing.
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription)
        page.saveGraphicsState()

        if degrees == 0 {
            page.append(w!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(h!)
            page.append(Token.space)
            page.append(x)
            page.append(Token.space)
            page.append(page.height - (y + h!))
            page.append(" cm\n")
        } else if degrees == 90 {
            page.append(h!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(w!)
            page.append(Token.space)
            page.append(x)
            page.append(Token.space)
            page.append(page.height - y)
            page.append(" cm\n")
            page.append("0 -1 1 0 0 0 cm\n")
        } else if degrees == 180 {
            page.append(w!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(h!)
            page.append(Token.space)
            page.append(x + w!)
            page.append(Token.space)
            page.append(page.height - y)
            page.append(" cm\n")
            page.append("-1 0 0 -1 0 0 cm\n")
        } else if degrees == 270 {
            page.append(h!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(w!)
            page.append(Token.space)
            page.append(x + h!)
            page.append(Token.space)
            page.append(page.height - (y + w!))
            page.append(" cm\n")
            page.append("0 1 -1 0 0 0 cm\n")
        }

        if flipUpsideDown {
            page.append("1 0 0 -1 0 1 cm\n")
        }

        page.append("/Im")
        page.append(objNumber!)
        page.append(" Do\n")

        page.restoreGraphicsState()

        page.addEMC()

        if uri != nil || key != nil {
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w!,
                    y + h!,
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Opacity
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    key,    // The destination name
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
    /// - Returns: w - the width of this image.
    ///
    public func getWidth() -> Float {
        return self.w!
    }

    ///
    /// Returns the height of this image when drawn on the page.
    /// The scaling is take into account.
    ///
    /// - Returns: h - the height of this image.
    ///
    public func getHeight() -> Float {
        return self.h!
    }

    private func addSoftMask(
            _ pdf: PDF,
            _ alpha: [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) {
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter /FlateDecode\n")
        pdf.append("/Width ")
        pdf.append(Int(w!))
        pdf.append(Token.newline)
        pdf.append("/Height ")
        pdf.append(Int(h!))
        pdf.append(Token.newline)
        pdf.append("/ColorSpace /")
        pdf.append(colorSpace)
        pdf.append(Token.newline)
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append(Token.newline)
        let buf = pdf.encrypted(alpha)
        pdf.append("/Length ")
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf, 0, buf.count)
        pdf.append(Token.endStream)
        pdf.endObj()
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

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        if imageType == ImageType.JPG {
            pdf.append("/Filter /DCTDecode\n")
        } else {
            pdf.append("/Filter /FlateDecode\n")
            if alpha.count > 0 {
                pdf.append("/SMask ")
                pdf.append(objNumber!)
                pdf.append(" 0 R\n")
            }
        }
        pdf.append("/Width ")
        pdf.append(Int(w!))
        pdf.append(Token.newline)
        pdf.append("/Height ")
        pdf.append(Int(h!))
        pdf.append(Token.newline)
        pdf.append("/ColorSpace /")
        pdf.append(colorSpace)
        pdf.append(Token.newline)
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append(Token.newline)
        if colorSpace == "DeviceCMYK" && invertedInks {
            // Adobe software, Photoshop among them, stores the inks inverted.
            pdf.append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
        }
        let buf = pdf.encrypted(data)
        pdf.append("/Length ")
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf, 0, buf.count)
        pdf.append(Token.endStream)
        pdf.endObj()
        pdf.images.append(self)
        self.objNumber = pdf.getObjNumber()
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
        obj.dict.append("/FlateDecode")
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
        } else if imageType == ImageType.PNG ||
                imageType == ImageType.BMP {
            obj.dict.append("/Filter")
            obj.dict.append("/FlateDecode")
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
        if colorSpace == "DeviceCMYK" && invertedInks {
            // Adobe software, Photoshop among them, stores the inks inverted.
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

    /// Scales this image to fit between its location and the bottom right corner of the page.
    public func resizeToFit(_ page: Page, keepAspectRatio: Bool) {
        if keepAspectRatio {
            self.scaleBy(min((page.width - self.x)/self.w!, (page.height - self.y)/self.h!))
        } else {
            self.scaleBy((page.width - self.x)/self.w!, (page.height - self.y)/self.h!)
        }
    }

    /// Sets whether this image is drawn upside down.
    @discardableResult
    public func setFlipUpsideDown(_ flipUpsideDown: Bool) -> Image {
        self.flipUpsideDown = flipUpsideDown
        return self
    }

    /// Returns the type of the image from its first bytes.
    static func typeOf(_ bytes: [UInt8]) throws -> ImageType {
        if bytes.count >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 {
            return ImageType.PNG
        }
        if bytes.count >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8 {
            return ImageType.JPG
        }
        if bytes.count >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D {
            return ImageType.BMP
        }
        throw ImageError.format("The image is not a PNG, JPEG or BMP file.")
    }
}   // End of Image.swift
