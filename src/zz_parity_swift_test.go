package pdfjet

import (
	"fmt"
	"math"
	"os"
	"strings"
	"testing"
)

type zzRng struct{ s uint64 }

func (r *zzRng) next() uint64 {
	r.s ^= r.s >> 12
	r.s ^= r.s << 25
	r.s ^= r.s >> 27
	return r.s * 0x2545F4914F6CDD1D
}

func (r *zzRng) f(lo, hi float32) float32 {
	u := float32(r.next()>>40) / 16777216.0
	return lo + float32(u*(hi-lo))
}

func (r *zzRng) n(k uint64) int { return int(r.next() % k) }

func zzb(sb *strings.Builder, v ...float32) {
	for _, x := range v {
		fmt.Fprintf(sb, "%08x ", math.Float32bits(x))
	}
}

func TestZZParitySwift(t *testing.T) {
	r := &zzRng{0x9E3779B97F4A7C15}
	var sb strings.Builder
	for i := 0; i < 100000; i++ {
		xc, yc, rad := r.f(0, 600), r.f(0, 600), r.f(0, 200)
		a1 := r.f(-90, 270)
		a2 := a1 + r.f(0, 90)
		p0 := getPoint(xc, yc, rad, a1)
		p3 := getPoint(xc, yc, rad, a2)
		cp := getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
		fmt.Fprintf(&sb, "seg %d ", i)
		zzb(&sb, xc, yc, rad, a1, a2)
		for _, p := range cp {
			zzb(&sb, p[0], p[1])
		}
		sb.WriteString("\n")
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	for i := 0; i < 300; i++ {
		page := NewPage(pdf, testLetterPortrait())
		chart := NewDonutChart(font, font).SetRadii(r.f(0, 200), r.f(0, 200))
		n := 1 + r.n(6)
		vals := []float32{}
		for j := 0; j < n; j++ {
			v := r.f(0, 1000)
			vals = append(vals, v)
			chart.AddSlice(NewSlice(v, 0x123456, fmt.Sprintf("L%d", j)))
		}
		x, y := r.f(0, 300), r.f(0, 300)
		chart.SetLocation(x, y).DrawOn(page)
		fmt.Fprintf(&sb, "donut %d ", i)
		zzb(&sb, vals...)
		zzb(&sb, x, y)
		sb.WriteString("\n" + testContent(page) + "\n")
	}
	for i := 0; i < 2000; i++ {
		page := NewPage(pdf, testLetterPortrait())
		arc := NewArc().SetRadiusX(r.f(0, 200)).SetRadiusY(r.f(0, 200)).
			SetStartAngle(r.f(-180, 180)).SetSweep(r.f(-360, 360))
		if r.n(2) == 0 {
			arc.SetStartPointToEndOf(NewLine(r.f(0, 600), r.f(0, 600), r.f(0, 600), r.f(0, 600)))
		}
		arc.SetStrokeColor(0x000000).SetLocation(r.f(0, 600), r.f(0, 600)).DrawOn(page)
		fmt.Fprintf(&sb, "arc %d ", i)
		zzb(&sb, arc.cx, arc.cy, arc.startAngle)
		sb.WriteString("\n" + testContent(page) + "\n")
	}
	os.WriteFile(os.Getenv("ZZ_OUT"), []byte(sb.String()), 0o644)
}
