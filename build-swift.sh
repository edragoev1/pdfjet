rm -rf .build

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        swift run --configuration release Example_0$i
    else
        swift run --configuration release Example_$i
    fi
done
