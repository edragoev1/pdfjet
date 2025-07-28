/**
 *  Field.cs
 *
©2025 PDFjet Software

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

/**
 *  Please see Example_45
 */
namespace PDFjet.NET {
public class Field {
    internal float x;
    internal String[] values;
    internal String[] altDescription;
    internal String[] actualText;
    internal bool format;

    public Field(float x, String[] values) : this(x, values, false) {
    }

    public Field(float x, String[] values, bool format) {
        this.x = x;
        this.values = values;
        this.format = format;
        if (values != null) {
            altDescription = new String[values.Length];
            actualText     = new String[values.Length];
            for (int i = 0; i < values.Length; i++) {
                this.altDescription[i] = values[i];
                this.actualText[i]     = values[i];
            }
        }
    }

    public Field SetAltDescription(String altDescription) {
        this.altDescription[0] = altDescription;
        return this;
    }

    public Field SetActualText(String actualText) {
        this.actualText[0] = actualText;
        return this;
    }
}   // End of Field.cs
}   // End of namespace PDFjet.NET
