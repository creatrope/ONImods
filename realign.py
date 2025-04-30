import os
import re
import sys

def find_original_project_name(folder):
    # Tries to find the old project name from the .csproj file
    for root, dirs, files in os.walk(folder):
        for file in files:
            if file.endswith(".csproj"):
                file_path = os.path.join(root, file)
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()
                match = re.search(r"<AssemblyName>(.*?)</AssemblyName>", content, re.IGNORECASE)
                if match:
                    return match.group(1)
    return None

def realign_project(folder):
    if not os.path.isdir(folder):
        print(f"[ERROR] Folder '{folder}' does not exist.")
        sys.exit(1)

    new_name = os.path.basename(os.path.normpath(folder))
    old_name = find_original_project_name(folder)

    if not old_name:
        print(f"[ERROR] Could not detect original project name from AssemblyName in .csproj.")
        sys.exit(1)

    print(f"[INFO] Realigning project from '{old_name}' to '{new_name}'")
    
    updated_files = 0
    renamed_files = 0

    # Step 1: Replace content inside files
    for root, dirs, files in os.walk(folder):
        for file in files:
            if file.endswith((".cs", ".csproj", ".yaml")):
                file_path = os.path.join(root, file)
                print(f"[INFO] Processing file: {file_path}")
                with open(file_path, "r", encoding="utf-8") as f:
                    content = f.read()

                def replace_case_sensitive(match):
                    text = match.group(0)
                    if text.isupper():
                        return new_name.upper()
                    elif text.islower():
                        return new_name.lower()
                    elif text[0].isupper():
                        return new_name[0].upper() + new_name[1:]
                    else:
                        return new_name

                # Match any appearance, not strict \b
                pattern = re.compile(rf'{re.escape(old_name)}', re.IGNORECASE)
                new_content = pattern.sub(replace_case_sensitive, content)

                if content != new_content:
                    with open(file_path, "w", encoding="utf-8") as f:
                        f.write(new_content)
                    updated_files += 1
                    print(f"[UPDATE] Updated content in {file_path}")

    # Step 2: Rename files themselves if necessary
    for root, dirs, files in os.walk(folder, topdown=False):
        for file in files:
            if old_name in file:
                old_file_path = os.path.join(root, file)
                new_file_name = file.replace(old_name, new_name)
                new_file_path = os.path.join(root, new_file_name)
                os.rename(old_file_path, new_file_path)
                renamed_files += 1
                print(f"[RENAME] Renamed file {old_file_path} -> {new_file_path}")

    print(f"\n[SUMMARY]")
    print(f"Updated {updated_files} file contents.")
    print(f"Renamed {renamed_files} files.")
    print(f"[SUCCESS] Realign complete for '{new_name}'.")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python realign_project.py <project_folder>")
        sys.exit(1)

    project_folder = sys.argv[1]
    realign_project(project_folder)
