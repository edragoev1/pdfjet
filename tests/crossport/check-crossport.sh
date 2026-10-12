#!/bin/bash
# Draws random shapes in the four ports, the same shapes from the same random
# numbers, and checks that every port writes the same content for each page as
# Go, byte for byte: the arcs, ellipses, donut charts, curved paths and rotated
# text whose paths are made of floating point arithmetic. A run like it against
# Go found the float paths of DonutChart and Arc apart (review of 11 October
# 2026, #25), which the examples' fixed inputs did not.
#
#     tests/crossport/check-crossport.sh [SEED [CASES]]
#
# A run without a seed takes one of its own and prints it, so that a failure
# can be run again. It works in build/check-crossport, and needs the four
# toolchains.

cd "$(dirname "$0")/../.." || exit 1
ROOT=$(pwd)
WORK=$ROOT/build/check-crossport
rm -rf "$WORK"
mkdir -p "$WORK"
SEED=${1:-$(od -An -N4 -tu4 /dev/urandom | tr -d ' ')}
CASES=${2:-5000}
echo "Drawing $CASES cases of seed $SEED in the four ports"

go vet ./tests/crossport/go || exit 1
go run ./tests/crossport/go "$SEED" "$CASES" "$WORK/go.txt" || exit 1

RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi
javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -d "$WORK/java" \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/internal/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    tests/crossport/java/CrossPort.java || exit 1
java -cp "$WORK/java" CrossPort "$SEED" "$CASES" "$WORK/java.txt" || exit 1

dotnet build tests/crossport/dotnet/CrossPort.csproj -c release -p:TreatWarningsAsErrors=true \
    -o "$WORK/dotnet" > "$WORK/dotnet.log" 2>&1 || { tail -n 20 "$WORK/dotnet.log"; exit 1; }
dotnet "$WORK/dotnet/CrossPort.dll" "$SEED" "$CASES" "$WORK/dotnet.txt" || exit 1

swift build --configuration release --product CrossPortSwift -Xswiftc -warnings-as-errors \
    > "$WORK/swift.log" 2>&1 || { tail -n 20 "$WORK/swift.log"; exit 1; }
"$(swift build --configuration release --show-bin-path)/CrossPortSwift" "$SEED" "$CASES" "$WORK/swift.txt" || exit 1

# The first case each port writes differently from Go, both ways.
status=0
for port in java dotnet swift; do
    if ! cmp -s "$WORK/go.txt" "$WORK/$port.txt"; then
        status=1
        python3 - "$WORK/go.txt" "$WORK/$port.txt" "$port" <<'PYEND'
import sys
def cases(path):
    found, name = {}, None
    for line in open(path, encoding='latin-1'):
        if line.startswith('case '):
            name = line.strip()
            found[name] = []
        else:
            found[name].append(line)
    return found
go, other = cases(sys.argv[1]), cases(sys.argv[2])
for name in go:
    if go[name] != other.get(name):
        print(f'{sys.argv[3]}: {name} differs from Go')
        print('--- go'); sys.stdout.write(''.join(go[name]))
        print(f'--- {sys.argv[3]}'); sys.stdout.write(''.join(other.get(name, ['(none)\n'])))
        break
PYEND
    fi
done
if [ $status -eq 0 ]; then
    echo "The $CASES cases of seed $SEED are the same in the four ports."
else
    echo "Run again with: tests/crossport/check-crossport.sh $SEED $CASES"
fi
exit $status
