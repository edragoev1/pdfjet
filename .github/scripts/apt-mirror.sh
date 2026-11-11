#!/bin/bash
# Points apt at Ubuntu's own archive when the runner's Azure mirror of Ubuntu
# does not resolve: from 14:59 on 11 November 2026 every job that installed a
# package failed on "Temporary failure resolving 'azure.archive.ubuntu.com'".
# Run before apt-get update; it changes nothing while the mirror resolves.
if ! getent hosts azure.archive.ubuntu.com > /dev/null; then
    echo "::warning::azure.archive.ubuntu.com does not resolve: using archive.ubuntu.com"
    echo "http://archive.ubuntu.com/ubuntu/" | sudo tee /etc/apt/apt-mirrors.txt > /dev/null
fi
