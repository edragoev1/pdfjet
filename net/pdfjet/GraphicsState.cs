using System;


namespace PDFjet.NET {
public class GraphicsState {

    // Default values
    private float CA = 1f;
    private float ca = 1f;

    public void SetAlphaStroking(float CA) {
        if (CA >= 0f && CA <= 1f) {
            this.CA = CA;
        }
    }

    public float GetAlphaStroking() {
        return this.CA;
    }

    public void SetAlphaNonStroking(float ca) {
        if (ca >= 0f && ca <= 1f) {
            this.ca = ca;
        }
    }

    public float GetAlphaNonStroking() {
        return this.ca;
    }

}
}
