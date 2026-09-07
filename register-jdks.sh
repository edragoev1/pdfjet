#!/bin/bash

# List of JDKs and their priorities
declare -A jdks=(
  ["/opt/jdk8u504-b01"]=1008
  ["/opt/jdk-11.0.32.1+1"]=1011
  ["/opt/jdk-17.0.20.1+1"]=1017
  ["/opt/jdk-21.0.12.1+1"]=1021
  ["/opt/jdk-25.0.4.1+1"]=1025
)

# Tools to register
tools=("java" "javac" "jar" "javadoc")

# Loop over each JDK
for jdk_path in "${!jdks[@]}"; do
  priority=${jdks[$jdk_path]}
  echo "Registering JDK at $jdk_path with priority $priority"

  for tool in "${tools[@]}"; do
    tool_path="$jdk_path/bin/$tool"
    if [[ -x "$tool_path" ]]; then
      echo "  -> Registering $tool"
      sudo update-alternatives --install /usr/bin/$tool $tool $tool_path $priority
    else
      echo "  !! Skipping $tool: $tool_path not found or not executable"
    fi
  done
done

echo ""
echo "✅All tools registered. Use the following to switch:"
echo "  sudo update-alternatives --config java"
echo "  sudo update-alternatives --config javac"
echo "  sudo update-alternatives --config jar"
echo "  sudo update-alternatives --config javadoc"
