swift build -c release -Xswiftc -g
sudo perf record -g -- .build/release/Example_43
sudo perf report
