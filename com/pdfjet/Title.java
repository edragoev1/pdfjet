/**
 *  Title.java
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
 * Please see Example_51 and Example_52
 *
 */
public class Title implements Drawable {
    public TextLine prefix;
    public TextLine textLine;


    public Title(Font font, String title, float x, float y) {
        this.prefix = new TextLine(font);
        this.prefix.setLocation(x, y);
        this.textLine = new TextLine(font, title);
        this.textLine.setLocation(x, y);
    }


    public Title setPrefix(String text) {
        prefix.setText(text);
        return this;
    }


    public Title setOffset(float offset) {
        textLine.setLocation(textLine.x + offset, textLine.y);
        return this;
    }

    public void setPosition(float x, float y) {
        prefix.setLocation(x, y);
        textLine.setLocation(x, y);
    }

    public void setPosition(double x, double y) {
        setPosition(x, y);
    }


    public Title setLocation(float x, float y) {
        textLine.setLocation(x, y);
        return this;
    }

    public Title setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }


    public float[] drawOn(Page page) throws Exception {
        if (!prefix.equals("")) {
            prefix.drawOn(page);
        }
        return textLine.drawOn(page);
    }

}
