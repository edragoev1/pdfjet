/**
 *  Annotation.cs
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


namespace PDFjet.NET {
/**
 *  Used to create PDF annotation objects.
 *
 *
 */
internal class Annotation {

    internal int objNumber;
    internal String uri = null;
    internal String key = null;
    internal float x1 = 0f;
    internal float y1 = 0f;
    internal float x2 = 0f;
    internal float y2 = 0f;

    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;

    internal FileAttachment fileAttachment = null;


    /**
     *  This class is used to create annotation objects.
     *
     *  @param uri the URI string.
     *  @param key the destination name.
     *  @param x1 the x coordinate of the top left corner.
     *  @param y1 the y coordinate of the top left corner.
     *  @param x2 the x coordinate of the bottom right corner.
     *  @param y2 the y coordinate of the bottom right corner.
     *
     */
    internal Annotation(
            String uri,
            String key,
            float x1,
            float y1,
            float x2,
            float y2,
            String language,
            String actualText,
            String altDescription) {
        this.uri = uri;
        this.key = key;
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.language = language;
        this.actualText = (actualText == null) ? uri : actualText;
        this.altDescription = (altDescription == null) ? uri : altDescription;
    }

}
}   // End of namespace PDFjet.NET
