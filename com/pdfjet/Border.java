/**
 *  Border.java
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
package com.pdfjet;

/**
 *  Used to control the visibility of cell borders.
 *  See the Cell class for more information.
 *
 */
public class Border {
    /** The default constructor */
    public Border() {
    }

    /** Specifies no borders. */
    public static final int NONE   = 0x00000000;
    /** Specifies top border. */
    public static final int TOP    = 0x00010000;
    /** Specifies bottom border. */
    public static final int BOTTOM = 0x00020000;
    /** Specifies left border. */
    public static final int LEFT   = 0x00040000;
    /** Specifies right border. */
    public static final int RIGHT  = 0x00080000;
    /** Specifies all borders. */
    public static final int ALL    = 0x000F0000;
}
