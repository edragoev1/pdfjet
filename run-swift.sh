#!/bin/sh
# swift run --configuration release Example_$1
swift run --configuration debug Example_$1

# evince Example_$1.pdf
mupdf Example_$1.pdf
