import subprocess
import os
import re

cwd = os.path.dirname(os.path.abspath(__file__)) if __file__ else "."
res = subprocess.run(["dotnet", "build"], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="ignore")

lines = res.stdout.split("\n")
for line in lines:
    if "error CS" in line:
        # Extract filename, line, error code and message
        match = re.search(r"\\([^\\]+\.cs)\((\d+),(\d+)\): error (CS\d+): (.*)", line)
        if match:
            filename = match.group(1)
            line_pos = match.group(2)
            col_pos = match.group(3)
            err_code = match.group(4)
            msg = match.group(5)
            print(f"{filename}:{line_pos} [{err_code}] -> {msg}")
        else:
            print("RAW ERROR:", line[:120])

print(f"Build finished with exit code: {res.returncode}")


