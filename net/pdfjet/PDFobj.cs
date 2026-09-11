/*
 * PDFobj.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Used to create Java or .NET objects that represent the objects in PDF document.
/// See the PDF specification for more information.
/// </summary>
public class PDFobj {
    internal int offset;           // The object offset
    internal int number;           // The object number
    internal List<String> dict;
    internal int streamOffset;
    internal byte[] stream;        // The compressed stream
    internal byte[] data;          // The decompressed data
    internal int gsNumber = -1;

    /// <summary>
    /// Used to create Java or .NET objects that represent the objects in PDF document.
    /// See the PDF specification for more information.
    /// Also see Example_19.
    /// </summary>
    internal PDFobj() {
        this.dict = new List<String>();
    }

    /// <summary>Returns the object number.</summary>
    public int GetNumber() {
        return this.number;
    }

    /// <summary>Returns the tokens of the object dictionary.</summary>
    public List<String> GetDict() {
        return this.dict;
    }

    /// <summary>Returns the decompressed stream data.</summary>
    public byte[] GetData() {
        return this.data;
    }

    internal void SetStreamAndData(byte[] buf, int length) {
        if (this.stream == null) {
            this.stream = new byte[length];
            Array.Copy(buf, streamOffset, stream, 0, length);
            String filter = GetValue("/Filter");
            if (filter.Equals("/FlateDecode")) {
                this.data = Decompressor.Inflate(stream);
            } else if (filter.Equals("/LZWDecode")) {
                this.data = Decompressor.ApplyPredictor(
                        Decompressor.LZWDecode(stream),
                        GetDecodeParm("/Predictor", 1),
                        GetDecodeParm("/Colors", 1),
                        GetDecodeParm("/BitsPerComponent", 8),
                        GetDecodeParm("/Columns", 1));
            } else {
                // Assume no compression for now.
                this.data = stream;
            }
        }
    }

    // Returns the integer value of the key in the /DecodeParms dictionary.
    private int GetDecodeParm(String key, int defaultValue) {
        String[] tokens = GetValue("/DecodeParms").Split(' ');
        for (int i = 0; i < tokens.Length - 1; i++) {
            if (tokens[i].Equals(key)) {
                return Int32.TryParse(tokens[i + 1], out int value) ? value : defaultValue;
            }
        }
        return defaultValue;
    }

    internal void SetStream(byte[] stream) {
        this.stream = stream;
    }

    internal void SetNumber(int number) {
        this.number = number;
    }

    /// <summary>
    /// Returns the parameter value given the specified key.
    /// </summary>
    /// <param name="key">the specified key.</param>
    /// <returns>the value.</returns>
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
                } else if (token.Equals("[")) {
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
                } else {
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
                } else {
                    numbers.Add(Int32.Parse(str));
                }
                break;
            }
        }
        return numbers;
    }

    /// <summary>Adds a content stream object number to the /Contents of this page.</summary>
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

    /// <summary>Returns the width and height from the /MediaBox of this page.</summary>
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
                } else {
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

    /// <summary>
    /// Returns the content object of this page, or null if it has no contents.
    /// The content of a page can be split into several streams, listed in an
    /// array that is either in the page dictionary or an object of its own.
    /// Together they are one content stream, so they are returned joined in
    /// a new object, which is not in the objects list.
    /// </summary>
    public PDFobj GetContentObject(List<PDFobj> objects) {
        List<Int32> numbers = GetObjectNumbers("/Contents");
        if (numbers.Count == 1) {
            PDFobj obj = objects[numbers[0] - 1];
            if (obj.stream != null) {
                return obj;
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = new List<Int32>();
            int i = obj.dict.IndexOf("[");
            while (i != -1 && i + 3 < obj.dict.Count && obj.dict[i + 3].Equals("R")) {
                numbers.Add(Int32.Parse(obj.dict[i + 1]));
                i += 3;
            }
        }
        if (numbers.Count == 0) {
            return null;
        }
        if (numbers.Count == 1) {
            return objects[numbers[0] - 1];
        }
        MemoryStream buf = new MemoryStream();
        foreach (int number in numbers) {
            byte[] bytes = objects[number - 1].data;
            if (bytes != null) {
                buf.Write(bytes, 0, bytes.Length);
                buf.WriteByte((byte) '\n');     // A stream can end in the middle of a line.
            }
        }
        PDFobj content = new PDFobj();
        content.data = buf.ToArray();
        return content;
    }

    /// <summary>Returns the resources object of this page.</summary>
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

    /// <summary>Adds a core font to the resources of this page and returns the font.</summary>
    public Font AddResource(int coreFont, List<PDFobj> objects) {
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
                } else if (Char.IsDigit(token[0])) {        // Indirect resources object
                    AddFontResource(objects[Int32.Parse(token) - 1], objects, font.fontID, obj.number);
                }
            }
        }

        return font;
    }

    private void AddFontResource(
            PDFobj obj, List<PDFobj> objects, String fontID, int number) {
        bool fonts = false;
        foreach (String token in obj.dict) {
            if (token.Equals("/Font")) {
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
                } else if (Char.IsDigit(token[0])) {
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
                } else {
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

    /// <summary>Adds an image to the resources of this page.</summary>
    public void AddResource(Image image, List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/XObject", this, objects, image.objNumber);
                } else {                        // Indirect resources object
                    AddResource("/XObject", objects[Int32.Parse(token) - 1], objects, image.objNumber);
                }
                return;
            }
        }
    }

    /// <summary>Adds a font to the resources of this page.</summary>
    public void AddResource(Font font, List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/Font", this, objects, font.objNumber);
                } else {                        // Indirect resources object
                    AddResource("/Font", objects[Int32.Parse(token) - 1], objects, font.objNumber);
                }
                return;
            }
        }
    }

    /// <summary>Adds a content stream to this page.</summary>
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
                } else {
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

    /// <summary>
    /// Adds new content object before the existing content objects.
    /// The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
    /// Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
    /// </summary>
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
                } else {
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

    /// <summary>Adds the graphics state to the resources of this page.</summary>
    public PDFobj SetGraphicsState(GraphicsState gs, List<PDFobj> objects) {
        PDFobj obj = null;
        int index = -1;
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    obj = this;
                    index = i + 2;
                } else {
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
            return this;
        }
        gsNumber = GetMaxGSNumber(obj);
        if (gsNumber == 0) {                        // No existing ExtGState dictionary
            obj.dict.Insert(index, "/ExtGState");   // Add ExtGState dictionary
            obj.dict.Insert(++index, "<<");
        } else {
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
        return this;
    }
}
}   // End of namespace PDFjet.NET
