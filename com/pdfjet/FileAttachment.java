/**
 *  FileAttachment.java
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
package com.pdfjet;

/**
 *  Used to attach file objects.
 *
 */
public class FileAttachment implements Drawable {

    protected int objNumber = -1;
    protected PDF pdf;
    protected EmbeddedFile embeddedFile;
    protected String icon = "PushPin";
    protected String title = "";
    protected String contents = "Right mouse click or double click on the icon to save the attached file.";
    protected float x = 0f;
    protected float y = 0f;
    protected float h = 24f;


    public FileAttachment(PDF pdf, EmbeddedFile file) {
        this.pdf = pdf;
        this.embeddedFile = file;
    }

    public void setPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void setPosition(double x, double y) {
        setPosition((float) x, (float) y);
    }

    public void setLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void setLocation(double x, double y) {
        setLocation((float) x, (float) y);
    }


    public void setIconPushPin() {
        this.icon = "PushPin";
    }


    public void setIconPaperclip() {
        this.icon = "Paperclip";
    }


    public void setIconSize(float height) {
        this.h = height;
    }


    public void setTitle(String title) {
        this.title = title;
    }


    public void setDescription(String description) {
        this.contents = description;
    }


    public float[] drawOn(Page page) throws Exception {
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
        page.addAnnotation(annotation);
        return new float[] {this.x + this.h, this.y + this.h};
    }

}   // End of FileAttachment.java
