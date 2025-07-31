if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-swift.sh 33"
    exit 1
fi

rm -rf .build

swift run --configuration release Example_$1
# swift run --configuration debug Example_$1

# mupdf Example_$1.pdf
evince Example_$1.pdf
