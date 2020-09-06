#!/bin/sh
# swift run --configuration release Example_$1
swift run --configuration debug Example_$1

# mupdf-gl Example_$1.pdf
evince Example_$1.pdf
