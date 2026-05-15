import os
import shutil
import subprocess
from pathlib import Path

def main():
    print("Building fModLoader (Main App)...")
    subprocess.run([
        "pyinstaller",
        "--noconfirm",
        "--onedir",
        "--windowed",
        "--name", "fModLoader",
        "--add-data", "icons;icons",
        "main.py"
    ], check=True)

    print("Building fModLoader CLI...")
    subprocess.run([
        "pyinstaller",
        "--noconfirm",
        "--onedir",
        "--console",
        "--name", "fModLoader_CLI",
        "--distpath", "dist/cli_temp",
        "make_modcompat.py"
    ], check=True)

    print("Merging CLI into Main App directory...")
    # The CLI executable will be at dist/cli_temp/fModLoader_CLI/fModLoader_CLI.exe
    cli_exe = Path("dist/cli_temp/fModLoader_CLI/fModLoader_CLI.exe")
    main_dir = Path("dist/fModLoader")
    
    if cli_exe.exists():
        shutil.copy2(cli_exe, main_dir / "fModLoader_CLI.exe")
        print("Copied CLI exe to main dist folder.")
    else:
        print("CLI exe not found!")

    print("Creating required application directories...")
    directories = ["plugins", "bugfix", "fonts", "mods", "diagnostics"]
    for d in directories:
        dir_path = main_dir / d
        dir_path.mkdir(exist_ok=True)
        # Create a dummy file so git/installer tracks them if needed
        (dir_path / ".keep").touch()
        print(f"Created: {d}")

    print("Cleaning up temporary CLI build...")
    if Path("dist/cli_temp").exists():
        shutil.rmtree("dist/cli_temp")

    print("Build complete! The 'dist/fModLoader' folder is ready for Inno Setup.")

if __name__ == "__main__":
    main()
