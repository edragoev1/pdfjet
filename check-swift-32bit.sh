#!/bin/bash
# Compiles the Swift library and its examples for a target where Int is 32
# bits, as the Swift Package Index builds every release for wasm32 and
# watchOS (arm64_32): 9.1.9's 0xFFFF_FFFF in Page.swift did not compile
# there, found only after the tag. The target is 32-bit armv7 of the Swift
# SDK for Android, which has the Swift modules of armv7. SIL is emitted
# without linking: literal overflows are found there, after type checking.
# Crashes at run time on 32 bits are not.
#
# Needs the Swift SDK for Android of the toolchain's version, installed
# (swift sdk install ...) and set up with an NDK; or SWIFT_ANDROID_SDK set
# to its swift-android folder and ANDROID_NDK_HOME to an NDK, whose C
# headers are used as they are (CI: the NDK of the runner image).
set -eu
cd "$(dirname "$0")"
sdk="${SWIFT_ANDROID_SDK:-}"
sysroot=""
if [ -n "$sdk" ]; then
    sysroot="${ANDROID_NDK_HOME:?ANDROID_NDK_HOME is needed with SWIFT_ANDROID_SDK}/toolchains/llvm/prebuilt/linux-x86_64/sysroot"
else
    version=$(swift --version 2>&1 | sed -n 's/.*Swift version \([0-9.]*\).*/\1/p' | head -1)
    sdk="$HOME/.swiftpm/swift-sdks/swift-$version-RELEASE_android.artifactbundle/swift-android"
    sysroot="$sdk/ndk-sysroot"
fi
if [ ! -d "$sdk/swift-resources/usr/lib/swift-armv7" ] || [ ! -d "$sysroot/usr/include" ]; then
    echo "No Swift SDK for Android set up at $sdk: see the top of $0" >&2
    exit 1
fi
target=(-target armv7-unknown-linux-androideabi28
    -resource-dir "$sdk/swift-resources/usr/lib/swift-armv7" -sdk "$sysroot")
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

mapfile -t library < <(find Sources/PDFjet -name '*.swift' | sort)
swiftc -emit-sil -wmo -o /dev/null -module-name PDFjet "${target[@]}" "${library[@]}"
swiftc -emit-module -wmo -parse-as-library -module-name PDFjet \
    -emit-module-path "$work/PDFjet.swiftmodule" "${target[@]}" "${library[@]}"

failed=0
count=0
for example in Sources/Example_*; do
    count=$((count + 1))
    mapfile -t files < <(find "$example" -name '*.swift' | sort)
    if ! swiftc -emit-sil -wmo -o /dev/null -module-name "$(basename "$example")" \
            "${target[@]}" -I "$work" "${files[@]}"; then
        echo "FAILED: $example" >&2
        failed=$((failed + 1))
    fi
done
if [ $failed -ne 0 ]; then
    echo "$failed of $count examples do not compile where Int is 32 bits" >&2
    exit 1
fi
echo "The library and $count examples compile where Int is 32 bits (armv7)."
