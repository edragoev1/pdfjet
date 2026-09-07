rm -rf .build

for i in $(seq 1 50);
do
    # The Swift port has no Example_30 - it demonstrates encryption, which the
    # Swift port does not support. See the "Port differences" section in README.md.
    if [ $i -eq 30 ]; then
        continue
    fi
    if [ $i -lt 10 ]; then
        swift run --configuration release Example_0$i
    else
        swift run --configuration release Example_$i
    fi
done
