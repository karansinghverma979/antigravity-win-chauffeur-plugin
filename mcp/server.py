#!/usr/bin/env python3
"""
Win-Chauffeur MCP Dispatch Bridge
Transparent stdio bridge launching the high-velocity native C# .NET 9 win-chauffeur engine.
"""

import sys
import os
import subprocess

def main():
    plugin_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    bin_path = os.path.join(plugin_root, "bin", "win-chauffeur.exe")

    if not os.path.exists(bin_path):
        sys.stderr.write(f"[win-chauffeur] Binary not found at: {bin_path}\n")
        sys.exit(1)

    try:
        proc = subprocess.Popen(
            [bin_path, "--mcp"],
            stdin=sys.stdin,
            stdout=sys.stdout,
            stderr=sys.stderr
        )
        sys.exit(proc.wait())
    except KeyboardInterrupt:
        if proc.poll() is None:
            proc.terminate()
        sys.exit(0)
    except Exception as e:
        sys.stderr.write(f"[win-chauffeur] Dispatch bridge error: {e}\n")
        sys.exit(1)

if __name__ == "__main__":
    main()
