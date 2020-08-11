/**
 *  EmbeddedFile.swift
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
import Foundation


/**
 *  Used to embed file objects.
 *  The file objects must added to the PDF before drawing on the first page.
 *
 */
public class EmbeddedFile {

    var objNumber: Int = -1
    var fileName: String?


    public init(
            _ pdf: PDF,
            _ fileName: String,
            _ stream: InputStream,
            _ compress: Bool) throws {
        self.fileName = fileName

        var baos = [UInt8]()
        var buf = [UInt8](repeating: 0, count: 4096)
        stream.open()
        while stream.hasBytesAvailable {
            let count = stream.read(&buf, maxLength: buf.count)
            if count > 0 {
                baos.append(contentsOf: buf[0..<count])
            }
        }
        stream.close()

        if compress {
            buf = baos
            baos = [UInt8]()
            _ = LZWEncode(&baos, &buf)
        }

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /EmbeddedFile\n")
        if compress {
            // pdf.append("/Filter /FlateDecode\n")
            pdf.append("/Filter /LZWDecode\n")
        }
        pdf.append("/Length ")
        pdf.append(baos.count)
        pdf.append("\n")
        pdf.append(">>\n")
        pdf.append("stream\n")
        pdf.append(baos)
        pdf.append("\nendstream\n")
        pdf.endobj()

        pdf.newobj()
        pdf.append("<<\n")
        pdf.append("/Type /Filespec\n")
        pdf.append("/F (")
        pdf.append(fileName)
        pdf.append(")\n")
        pdf.append("/EF <</F ")
        pdf.append(pdf.getObjNumber() - 1)
        pdf.append(" 0 R>>\n")
        pdf.append(">>\n")
        pdf.endobj()

        self.objNumber = pdf.getObjNumber()
    }


    public func getFileName() -> String {
        return self.fileName!
    }

}   // End of EmbeddedFile.swift
