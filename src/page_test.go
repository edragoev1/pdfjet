package pdfjet

import "testing"

func TestPage_lineTo(t *testing.T) {
	type fields struct {
		pdf           *PDF
		buf           []byte
		pageObj       *PDFobj
		objNumber     int
		tm            [4]float32
		tm0           []byte
		tm1           []byte
		tm2           []byte
		tm3           []byte
		renderingMode int
		width         float32
		height        float32
		contents      []int
		annots        []*Annotation
		destinations  []*Destination
		cropBox       []float32
		bleedBox      []float32
		trimBox       []float32
		artBox        []float32
		structures    []*StructElem
		pen           [3]float32
		brush         [3]float32
		penCMYK       [4]float32
		brushCMYK     [4]float32
		penWidth      float32
		lineCapStyle  int
		lineJoinStyle int
		linePattern   string
		font          *Font
		savedStates   []*State
		mcid          int
		savedHeight   float32
	}
	type args struct {
		x float32
		y float32
	}
	tests := []struct {
		name   string
		fields fields
		args   args
	}{
		// TODO: Add test cases.
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			page := &Page{
				pdf:           tt.fields.pdf,
				buf:           tt.fields.buf,
				pageObj:       tt.fields.pageObj,
				objNumber:     tt.fields.objNumber,
				tm:            tt.fields.tm,
				tm0:           tt.fields.tm0,
				tm1:           tt.fields.tm1,
				tm2:           tt.fields.tm2,
				tm3:           tt.fields.tm3,
				renderingMode: tt.fields.renderingMode,
				width:         tt.fields.width,
				height:        tt.fields.height,
				contents:      tt.fields.contents,
				annots:        tt.fields.annots,
				destinations:  tt.fields.destinations,
				cropBox:       tt.fields.cropBox,
				bleedBox:      tt.fields.bleedBox,
				trimBox:       tt.fields.trimBox,
				artBox:        tt.fields.artBox,
				structures:    tt.fields.structures,
				pen:           tt.fields.pen,
				brush:         tt.fields.brush,
				penCMYK:       tt.fields.penCMYK,
				brushCMYK:     tt.fields.brushCMYK,
				penWidth:      tt.fields.penWidth,
				lineCapStyle:  tt.fields.lineCapStyle,
				lineJoinStyle: tt.fields.lineJoinStyle,
				linePattern:   tt.fields.linePattern,
				font:          tt.fields.font,
				savedStates:   tt.fields.savedStates,
				mcid:          tt.fields.mcid,
				savedHeight:   tt.fields.savedHeight,
			}
			page.LineTo(tt.args.x, tt.args.y)
		})
	}
}
