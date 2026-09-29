#!/bin/bash
# Gets the fonts and the data files the examples and the tests read, the
# folders fonts and data, from the repositories pdfjet-fonts and pdfjet-data at
# the commits fonts-and-data.txt pins. They are not in this repository, nor
# its submodules, so that the Go module proxy, which archives the tree at a
# tag, and SwiftPM, which checks out a package with its submodules, download
# the library alone.
#
# The build, run and test scripts run it, and it does nothing when the folders
# are at the pinned commits. A folder that is missing or empty is fetched, the
# pinned commit alone. A folder at another commit, as after a git pull that
# changed a pin, is moved to it, unless it has changes of its own, which are
# never overwritten, or has the pinned commit in its history, as when a newer
# one is being made. A folder that is not a git checkout is used as it is.

cd "$(dirname "$0")" || exit 1

while read -r dir url commit <&3; do
    case $dir in '' | '#'*) continue ;; esac
    commit=${commit%$'\r'}

    if [ -e "$dir/.git" ] && head=$(git -C "$dir" rev-parse -q --verify HEAD 2> /dev/null); then
        if [ "$head" = "$commit" ]; then
            continue
        fi
        if git -C "$dir" merge-base --is-ancestor "$commit" HEAD 2> /dev/null; then
            echo "Warning: $dir is at $head, a commit after" >&2
            echo "$commit, the one fonts-and-data.txt pins: using it as it is." >&2
            continue
        fi
        if [ -n "$(git -C "$dir" status --porcelain)" ]; then
            echo "$dir has changes of its own, and is at $head, not at" >&2
            echo "$commit, the commit fonts-and-data.txt pins. Commit or undo" >&2
            echo "the changes, or move $dir away, and run the script again." >&2
            exit 1
        fi
        echo "Moving $dir from $head to $commit, the commit fonts-and-data.txt pins"
        if ! git -C "$dir" cat-file -e "$commit^{commit}" 2> /dev/null; then
            if [ "$(git -C "$dir" rev-parse --is-shallow-repository)" = true ]; then
                git -C "$dir" fetch --depth 1 "$url" "$commit" || exit 1
            else
                git -C "$dir" fetch "$url" "$commit" || exit 1
            fi
        fi
        git -C "$dir" checkout -q --detach "$commit" || exit 1
        continue
    fi

    # Files but no checkout git can read, such as a copy, are used as they are.
    if [ -n "$(find "$dir" -mindepth 1 -maxdepth 1 ! -name .git 2> /dev/null | head -n 1)" ]; then
        continue
    fi

    # Missing, empty, or a fetch that was stopped: fetch the commit alone.
    echo "Getting $dir, $url at $commit"
    rm -rf "$dir"
    if ! { git init -q "$dir" &&
            git -C "$dir" remote add origin "$url" &&
            git -C "$dir" fetch --depth 1 origin "$commit" &&
            git -C "$dir" checkout -q --detach FETCH_HEAD; }; then
        rm -rf "$dir"
        echo "Could not get $dir from $url. It needs git and a connection to GitHub." >&2
        exit 1
    fi
done 3< fonts-and-data.txt
