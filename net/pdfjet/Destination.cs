/**
 *  Destination.cs
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
 *  Used to create PDF destination objects.
 */
namespace PDFjet.NET {
public class Destination {
    public String name;
    public int pageObjNumber;
    public float xPosition;
    public float yPosition;

    /**
     *  This constructor is used to create destination objects.
     *
     *  @param name the name of this destination object.
     *  @param xPosition the x coordinate of the top left corner.
     *  @param yPosition the y coordinate of the top left corner.
     *
     */
    public Destination(String name, float xPosition, float yPosition) {
        this.name = name;
        this.xPosition = xPosition;
        this.yPosition = yPosition;
    }

    /**
     *  This constructor is used to create destination objects.
     *
     *  @param name the name of this destination object.
     *  @param xPosition the x coordinate of the top left corner.
     *  @param yPosition the y coordinate of the top left corner.
     *
     */
    public Destination(String name, double xPosition, double yPosition) {
        this.name = name;
        this.xPosition = (float) xPosition;
        this.yPosition = (float) yPosition;
    }

    /**
     *  This constructor is used to create destination objects.
     *
     *  @param name the name of this destination object.
     *  @param yPosition the y coordinate of the top left corner.
     *
     */
    public Destination(String name, float yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = yPosition;
    }

    /**
     *  This constructor is used to create destination objects.
     *
     *  @param name the name of this destination object.
     *  @param yPosition the y coordinate of the top left corner.
     *
     */
    public Destination(String name, double yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = (float) yPosition;
    }

    internal void SetPageObjNumber(int pageObjNumber) {
        this.pageObjNumber = pageObjNumber;
    }
}
}   // End of namespace PDFjet.NET
