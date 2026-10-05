#!/bin/bash
# Fails if the Go port has a fused multiply-add on arm64, which the Go spec
# lets the compiler make of x*y + z, rounding once, where amd64 and the
# Java, C# and Swift ports round twice; at a boundary it can break a line
# otherwise. Every float multiplication of the library is written in a
# conversion to its own type, float32(a*b), which rounds it and stops the
# fusing (5 October 2026). The compiler prints the arm64 code of every
# function of the library, its examples left out, and no fused instruction
# may be in it. Runs on any computer, no ARM needed.
#
#   ./check-no-fma.sh
cd "$(dirname "$0")" || exit 1
packages=$(GOWORK=off go list ./src/... | grep -v '/examples/')
# -a, so that the packages are compiled, and print their code, though built before
asm=$(GOWORK=off GOOS=linux GOARCH=arm64 CGO_ENABLED=0 go build -a -o /dev/null -gcflags=-S $packages 2>&1)
fused=$(printf '%s\n' "$asm" | grep -E '\s(FMADD|FMSUB|FNMADD|FNMSUB)[SD]\s' | grep -oE '\([^()]*\.go:[0-9]+\)' | sort | uniq -c)
if [ -n "$fused" ]; then
    echo "Fused multiply-adds on arm64, where a float multiplication needs float32(...) or float64(...):"
    echo "$fused"
    exit 1
fi
echo "No fused multiply-add on arm64 in the $(echo "$packages" | wc -l) packages of the Go port."
