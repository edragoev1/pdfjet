/**
 *  State.cs
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
using System;


namespace PDFjet.NET {
class State {

    private float[] pen;
    private float[] brush;
    private float penWidth;
    private int lineCapStyle;
    private int lineJoinStyle;
    private String linePattern;


    public State(
            float[] pen,
            float[] brush,
            float penWidth,
            int lineCapStyle,
            int lineJoinStyle,
            String linePattern) {
        this.pen = new float[] { pen[0], pen[1], pen[2] };
        this.brush = new float[] { brush[0], brush[1], brush[2] };
        this.penWidth = penWidth;
        this.lineCapStyle = lineCapStyle;
        this.lineJoinStyle = lineJoinStyle;
        this.linePattern = linePattern;
    }


    public float[] GetPen() {
        return pen;
    }


    public float[] GetBrush() {
        return brush;
    }


    public float GetPenWidth() {
        return penWidth;
    }


    public int GetLineCapStyle() {
        return lineCapStyle;
    }


    public int GetLineJoinStyle() {
        return lineJoinStyle;
    }


    public String GetLinePattern() {
        return linePattern;
    }

}   // End of State.cs
}   // End of namespace PDFjet.NET
