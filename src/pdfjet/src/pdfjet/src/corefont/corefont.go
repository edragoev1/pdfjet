package corefont

// CoreFont structure.
type CoreFont struct {
	Name               string
	Notice             string
	BBoxLLx            int16
	BBoxLLy            int16
	BBoxURx            int16
	BBoxURy            int16
	UnderlinePosition  int16
	UnderlineThickness int16
	Metrics            [][]int
}
