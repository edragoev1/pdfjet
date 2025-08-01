/**
 *  Slice.java
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
 * This class is used the from the Pie Chart.
 */
public class Slice {
    Float angle;
    int color;

    /**
     * Creates slice object to be used with the pie chart.
     *
     * @param percent the percent of the total.
     * @param color the slice color.
     */
    public Slice(Float percent, int color) {
        this.angle = percent*3.6f;
        this.color = color;
    }

    /**
     * Creates slice object to be used with the pie chart.
     *
     * @param percent the percent of the total.
     * @param color the slice color.
     */
    public Slice(String percent, int color) {
        Float value = Float.valueOf(
                percent.substring(0, percent.length() - 1));
        this.angle = value*3.6f;
        this.color = color;
    }
}
