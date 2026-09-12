rm -rf .build

# Builds the library and all the examples at once, then runs the examples.
# -warnings-as-errors fails the build on any warning.
swift build --configuration release -Xswiftc -warnings-as-errors
bin=$(swift build --configuration release --show-bin-path)

for i in $(seq 1 51);
do
    if [ $i -lt 10 ]; then
        "$bin/Example_0$i"
    else
        "$bin/Example_$i"
    fi
done
