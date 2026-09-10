using System;

namespace PDFjet.NET {
public class GraphicsState {
    // Default values
    private float CA = 1f;
    private float ca = 1f;

    public GraphicsState SetAlphaStroking(float CA) {
        if (CA >= 0f && CA <= 1f) {
            this.CA = CA;
        }
        return this;
    }

    public float GetAlphaStroking() {
        return this.CA;
    }

    public GraphicsState SetAlphaNonStroking(float ca) {
        if (ca >= 0f && ca <= 1f) {
            this.ca = ca;
        }
        return this;
    }

    public float GetAlphaNonStroking() {
        return this.ca;
    }
}
}
