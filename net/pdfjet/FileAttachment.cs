/**
 *  FileAttachment.cs
 *
Copyright 2020 Innovatics Inc.

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
 *  Used to attach file objects.
 *
 */
public class FileAttachment : IDrawable {

    internal int objNumber = -1;
    internal PDF pdf = null;
    internal EmbeddedFile embeddedFile = null;
    internal String icon = "PushPin";
    internal String title = "";
    internal String contents = "Right mouse click or double click on the icon to save the attached file.";
    internal float x = 0f;
    internal float y = 0f;
    internal float h = 24f;


    public FileAttachment(PDF pdf, EmbeddedFile file) {
        this.pdf = pdf;
        this.embeddedFile = file;
    }


    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }


    public void SetIconPushPin() {
        this.icon = "PushPin";
    }


    public void SetIconPaperclip() {
        this.icon = "Paperclip";
    }


    public void SetIconSize(float height) {
        this.h = height;
    }


    public void SetTitle(String title) {
        this.title = title;
    }


    public void SetDescription(String description) {
        this.contents = description;
    }


    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public float[] DrawOn(Page page) {
        Annotation annotation = new Annotation(
                null,
                null,
                x,
                y,
                x + h,
                y + h,
                null,
                null,
                null);
        annotation.fileAttachment = this;
        page.AddAnnotation(annotation);
        return new float[] {this.x + this.h, this.y + this.h};
    }

}   // End of FileAttachment.cs
}   // End of namespace PDFjet.NET
