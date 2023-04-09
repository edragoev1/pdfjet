/**
 *  PDFobj.cs
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
using System;
using System.Collections.Generic;
using System.Text;


namespace PDFjet.NET {
/**
 *  Used to create Java or .NET objects that represent the objects in PDF document.
 *  See the PDF specification for more information.
 *
 */
public class PDFobj {

    internal int offset;           // The object offset
    internal int number;           // The object number
    internal List<String> dict;
    internal int streamOffset;
    internal byte[] stream;        // The compressed stream
    internal byte[] data;          // The decompressed data
    internal int gsNumber = -1;


    /**
     *  Used to create Java or .NET objects that represent the objects in PDF document.
     *  See the PDF specification for more information.
     *  Also see Example_19.
     */
    internal PDFobj() {
        this.dict = new List<String>();
    }


    public int GetNumber() {
        return this.number;
    }


    public List<String> GetDict() {
        return this.dict;
    }


    public byte[] GetData() {
        return this.data;
    }


    internal void SetStreamAndData(byte[] buf, int length) {
        if (this.stream == null) {
            this.stream = new byte[length];
            Array.Copy(buf, streamOffset, stream, 0, length);
            if (GetValue("/Filter").Equals("/FlateDecode")) {
                this.data = Decompressor.inflate(stream);
            }
            else {
                // Assume no compression for now.
                // In the future we may handle LZW decompression ...
                this.data = stream;
            }
        }
    }


    internal void SetStream(byte[] stream) {
        this.stream = stream;
    }


    internal void SetNumber(int number) {
        this.number = number;
    }


    /**
     *  Returns the parameter value given the specified key.
     *
     *  @param key the specified key.
     *
     *  @return the value.
     */
    public String GetValue(String key) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals(key)) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append("<< ");
                    i += 2;
                    while (!dict[i].Equals(">>")) {
                        buffer.Append(dict[i]);
                        buffer.Append(" ");
                        i += 1;
                    }
                    buffer.Append(">>");
                    return buffer.ToString();
                }
                else if (token.Equals("[")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append("[ ");
                    i += 2;
                    while (!dict[i].Equals("]")) {
                        buffer.Append(dict[i]);
                        buffer.Append(" ");
                        i += 1;
                    }
                    buffer.Append("]");
                    return buffer.ToString();
                }
                else {
                    return token;
                }
            }
        }
        return "";
    }


    internal List<Int32> GetObjectNumbers(String key) {
        List<Int32> numbers = new List<Int32>();
        for (int i = 0; i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals(key)) {
                String str = dict[++i];
                if (str.Equals("[")) {
                    while (true) {
                        str = dict[++i];
                        if (str.Equals("]")) {
                            break;
                        }
                        numbers.Add(Int32.Parse(str));
                        ++i;    // 0
                        ++i;    // R
                    }
                }
                else {
                    numbers.Add(Int32.Parse(str));
                }
                break;
            }
        }
        return numbers;
    }


    public void AddContentObject(int number) {
        int index = -1;
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                String str = dict[++i];
                if (str.Equals("[")) {
                    while (true) {
                        str = dict[++i];
                        if (str.Equals("]")) {
                            index = i;
                            break;
                        }
                        ++i;    // 0
                        ++i;    // R
                    }
                }
                break;
            }
        }
        dict.Insert(index, "R");
        dict.Insert(index, "0");
        dict.Insert(index, number.ToString());
    }


    public float[] GetPageSize() {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/MediaBox")) {
                return new float[] {
                        Convert.ToSingle(dict[i + 4]),
                        Convert.ToSingle(dict[i + 5]) };
            }
        }
        return Letter.PORTRAIT;
    }


    internal int GetLength(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals("/Length")) {
                int number = Int32.Parse(dict[i + 1]);
                if (dict[i + 2].Equals("0") &&
                        dict[i + 3].Equals("R")) {
                    return GetLength(objects, number);
                }
                else {
                    return number;
                }
            }
        }
        return 0;
    }


    internal int GetLength(List<PDFobj> objects, int number) {
        foreach (PDFobj obj in objects) {
            if (obj.number == number) {
                return Int32.Parse(obj.dict[3]);
            }
        }
        return 0;
    }


    public PDFobj GetContentsObject(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                if (dict[i + 1].Equals("[")) {
                    String token = dict[i + 2];
                    return objects[Int32.Parse(token) - 1];
                }
                else {
                    String token = dict[i + 1];
                    return objects[Int32.Parse(token) - 1];
                }
            }
        }
        return null;
    }

    public PDFobj GetResourcesObject(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    return this;
                }
                return objects[Int32.Parse(token) - 1];
            }
        }
        return null;
    }

    public Font AddResource(CoreFont coreFont, List<PDFobj> objects) {
        Font font = new Font(coreFont);
        font.fontID = font.name.Replace('-', '_').ToUpper();

        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Type1");
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + font.name);
        if (!font.name.Equals("Symbol") && !font.name.Equals("ZapfDingbats")) {
            obj.dict.Add("/Encoding");
            obj.dict.Add("/WinAnsiEncoding");
        }
        obj.dict.Add(">>");
        obj.number = objects.Count + 1;
        objects.Add(obj);

        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[++i];
                if (token.Equals("<<")) {                   // Direct resources object
                    AddFontResource(this, objects, font.fontID, obj.number);
                }
                else if (Char.IsDigit(token[0])) {          // Indirect resources object
                    AddFontResource(objects[Int32.Parse(token) - 1], objects, font.fontID, obj.number);
                }
            }
        }

        return font;
    }


    private void AddFontResource(
            PDFobj obj, List<PDFobj> objects, String fontID, int number) {

        bool fonts = false;
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Font")) {
                fonts = true;
                break;
            }
        }
        if (!fonts) {
            for (int i = 0; i < obj.dict.Count; i++) {
                if (obj.dict[i].Equals("/Resources")) {
                    obj.dict.Insert(i + 2, "/Font");
                    obj.dict.Insert(i + 3, "<<");
                    obj.dict.Insert(i + 4, ">>");
                    break;
                }
            }
        }

        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Font")) {
                String token = obj.dict[i + 1];
                if (token.Equals("<<")) {
                    obj.dict.Insert(i + 2, "/" + fontID);
                    obj.dict.Insert(i + 3, number.ToString());
                    obj.dict.Insert(i + 4, "0");
                    obj.dict.Insert(i + 5, "R");
                    return;
                }
                else if (Char.IsDigit(token[0])) {
                    PDFobj o2 = objects[Int32.Parse(token) - 1];
                    for (int j = 0; j < o2.dict.Count; j++) {
                        if (o2.dict[j].Equals("<<")) {
                            o2.dict.Insert(j + 1, "/" + fontID);
                            o2.dict.Insert(j + 2, number.ToString());
                            o2.dict.Insert(j + 3, "0");
                            o2.dict.Insert(j + 4, "R");
                            return;
                        }
                    }
                }
            }
        }
    }


    private void InsertNewObject(
            List<String> dict, String[] list, String type) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals(type)) {
                dict.InsertRange(i + 2, list);
                return;
            }
        }
        if (dict[3].Equals("<<")) {
            dict.InsertRange(4, list);
            return;
        }
    }


    private void AddResource(
            String type, PDFobj obj, List<PDFobj> objects, Int32 objNumber) {
        String tag = type.Equals("/Font") ? "/F" : "/Im";
        String number = objNumber.ToString();
        String[] list = {tag + number, number, "0", "R"};
        for (int i = 0; i < obj.dict.Count; i++) {
            String token = obj.dict[i];
            if (token.Equals(type)) {
                token = obj.dict[i + 1];
                if (token.Equals("<<")) {
                    InsertNewObject(obj.dict, list, type);
                }
                else {
                    InsertNewObject(objects[Int32.Parse(token) - 1].dict, list, type);
                }
                return;
            }
        }

        // Handle the case where the page originally does not have any font resources.
        String[] array = {type, "<<", tag + number, number, "0", "R", ">>"};
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Resources")) {
                obj.dict.InsertRange(i + 2, array);
                return;
            }
        }
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("<<")) {
                obj.dict.InsertRange(i + 1, array);
                return;
            }
        }
    }


    public void AddResource(Image image, List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/XObject", this, objects, image.objNumber);
                }
                else {                          // Indirect resources object
                    AddResource("/XObject", objects[Int32.Parse(token) - 1], objects, image.objNumber);
                }
                return;
            }
        }
    }


    public void AddResource(Font font, List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/Font", this, objects, font.objNumber);
                }
                else {                          // Indirect resources object
                    AddResource("/Font", objects[Int32.Parse(token) - 1], objects, font.objNumber);
                }
                return;
            }
        }
    }


    public void AddContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);

        String objNumber = obj.number.ToString();
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                i += 1;
                String token = dict[i];
                if (token.Equals("[")) {
                    // Array of content objects
                    while (true) {
                        i += 1;
                        token = dict[i];
                        if (token.Equals("]")) {
                            dict.Insert(i, "R");
                            dict.Insert(i, "0");
                            dict.Insert(i, objNumber);
                            return;
                        }
                        i += 2;     // Skip the 0 and R
                    }
                }
                else {
                    // Single content object
                    PDFobj obj2 = objects[Int32.Parse(token) - 1];
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.Count; j++) {
                            if (obj2.dict[j].Equals("]")) {
                                obj2.dict.Insert(j, "R");
                                obj2.dict.Insert(j, "0");
                                obj2.dict.Insert(j, objNumber);
                                return;
                            }
                        }
                    }
                    dict.Insert(i, "[");
                    dict.Insert(i + 4, "]");
                    dict.Insert(i + 4, "R");
                    dict.Insert(i + 4, "0");
                    dict.Insert(i + 4, objNumber);
                    return;
                }
            }
        }
    }


    /**
     * Adds new content object before the existing content objects.
     * The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
     * Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
     *
     * @param content
     * @param objects
     */
    public void AddPrefixContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);

        String objNumber = obj.number.ToString();
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                i += 1;
                String token = dict[i];
                if (token.Equals("[")) {
                    // Array of content object streams
                    i += 1;
                    dict.Insert(i, "R");
                    dict.Insert(i, "0");
                    dict.Insert(i, objNumber);
                    return;
                }
                else {
                    // Single content object
                    PDFobj obj2 = objects[Int32.Parse(token) - 1];
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.Count; j++) {
                            if (obj2.dict[j].Equals("[")) {
                                j += 1;
                                obj2.dict.Insert(j, "R");
                                obj2.dict.Insert(j, "0");
                                obj2.dict.Insert(j, objNumber);
                                return;
                            }
                        }
                    }
                    dict.Insert(i, "[");
                    dict.Insert(i + 4, "]");
                    i += 1;
                    dict.Insert(i, "R");
                    dict.Insert(i, "0");
                    dict.Insert(i, objNumber);
                    return;
                }
            }
        }
    }


    private int GetMaxGSNumber(PDFobj obj) {
        List<Int32> numbers = new List<Int32>();
        foreach (String token in obj.dict) {
            if (token.StartsWith("/GS")) {
                numbers.Add(Int32.Parse(token.Substring(3)));
            }
        }
        if (numbers.Count == 0) {
            return 0;
        }
        int maxGSNumber = -1;
        foreach (Int32 number in numbers) {
            if (number > maxGSNumber) {
                maxGSNumber = number;
            }
        }
        return maxGSNumber;
    }


    public void SetGraphicsState(GraphicsState gs, List<PDFobj> objects) {
        PDFobj obj = null;
        int index = -1;
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    obj = this;
                    index = i + 2;
                }
                else {
                    obj = objects[Int32.Parse(token) - 1];
                    for (int j = 0; j < obj.dict.Count; j++) {
                        if (obj.dict[j].Equals("<<")) {
                            index = j + 1;
                            break;
                        }
                    }
                }
                break;
            }
        }
        if (obj == null || index == -1) {
            return;
        }
        gsNumber = GetMaxGSNumber(obj);
        if (gsNumber == 0) {                        // No existing ExtGState dictionary
            obj.dict.Insert(index, "/ExtGState");   // Add ExtGState dictionary
            obj.dict.Insert(++index, "<<");
        }
        else {
            while (index < obj.dict.Count) {
                String token = obj.dict[index];
                if (token.Equals("/ExtGState")) {
                    index += 1;
                    break;
                }
                index += 1;
            }
        }
        obj.dict.Insert(++index, "/GS" + (gsNumber + 1).ToString());
        obj.dict.Insert(++index, "<<");
        obj.dict.Insert(++index, "/CA");
        obj.dict.Insert(++index, gs.GetAlphaStroking().ToString());
        obj.dict.Insert(++index, "/ca");
        obj.dict.Insert(++index, gs.GetAlphaNonStroking().ToString());
        obj.dict.Insert(++index, ">>");
        if (gsNumber == 0) {
            obj.dict.Insert(++index, ">>");
        }

        StringBuilder buf = new StringBuilder();
        buf.Append("q\n");
        buf.Append("/GS" + (gsNumber + 1).ToString() + " gs\n");
        AddPrefixContent(Encoding.ASCII.GetBytes(buf.ToString()), objects);
    }

}
}   // End of namespace PDFjet.NET
