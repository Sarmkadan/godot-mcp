#!/usr/bin/env python3
"""
Simple build command script for the GodotMcp repository.

Running this script will invoke `dotnet test` to build the solution
and execute all unit tests. It is intended to be used as a quick
entry point for CI pipelines or local development.

If the `dotnet` CLI is not installed or the project fails to build,
the script will forward the error output and exit with a non‑zero
status code.
"""

import subprocess
import sys
from pathlib import Path

def main() -> None:
    # Ensure we are running from the repository root
    repo_root = Path(__file__).resolve().parent
    # Change to the repository root to let `dotnet` locate the solution file
    try:
        os.chdir(repo_root)
    except Exception:
        pass  # If changing directory fails, let dotnet handle the path

    # Run `dotnet test` and capture its output
    process = subprocess.run(
        ["dotnet", "test"],
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )

    # Print the combined output so the user sees both success and error messages
    print(process.stdout)

    # Propagate the exit code from `dotnet test`
    if process.returncode != 0:
        sys.exit(process.returncode)

if __name__ == "__main__":
    main()
