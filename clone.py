import os
import sys
import shutil
import re

def clone_project(source_dir, target_dir):
    if not os.path.exists(source_dir):
        print(f"Source directory {source_dir} does not exist.")
        sys.exit(1)
    if os.path.exists(target_dir):
        print(f"Target directory {target_dir} already exists.")
        sys.exit(1)

    shutil.copytree(source_dir, target_dir)
    print(f"Copied {source_dir} to {target_dir}")

    old_project_name = os.path.basename(os.path.normpath(source_dir))
    new_project_name = os.path.basename(os.path.normpath(target_dir))

    print(f"Renaming project from {old_project_name} to {new_project_name}")

    # Patterns to replace
    patterns = [
        (fr'\bnamespace\s+{re.escape(old_project_name)}', f'namespace {new_project_name}'),
        (fr'Debug\.Log\(\s*"\[{re.escape(old_project_name)}\]', f'Debug.Log("[{new_project_name}]'),
        (fr'Debug\.LogWarning\(\s*"\[{re.escape(old_project_name)}\]', f'Debug.LogWarning("[{new_project_name}]'),
        (fr'Debug\.LogError\(\s*"\[{re.escape(old_project_name)}\]', f'Debug.LogError("[{new_project_name}]'),
        (fr'\[{re.escape(old_project_name)}\]', f'[{new_project_name}]'),
        (r'(<AssemblyName>)\s*[^<]*\s*(</AssemblyName>)', rf'\1{new_project_name}\2'),
        (r'(<RootNamespace>)\s*[^<]*\s*(</RootNamespace>)', rf'\1{new_project_name}\2'),
        (r'(\[assembly:\s*AssemblyTitle\()\s*"[^"]*"', rf'\1 "{new_project_name}"'),
        (r'(\[assembly:\s*AssemblyProduct\()\s*"[^"]*"', rf'\1 "{new_project_name}"'),
        (r'(\bid:\s*)[^\s]+', rf'\1{new_project_name}'),
        (r'(\btitle:\s*)[^\n]+', rf'\1' + new_project_name),
        (r'(\bname:\s*)[^\n]+', rf'\1' + new_project_name),
    ]

    # Walk through all files
    for root, dirs, files in os.walk(target_dir):
        for filename in files:
            filepath = os.path.join(root, filename)

            # Rename csproj/sln file if necessary
            if filename.lower().startswith(old_project_name.lower()) and filename.endswith(('.csproj', '.sln')):
                new_filename = filename.replace(old_project_name, new_project_name)
                new_filepath = os.path.join(root, new_filename)
                os.rename(filepath, new_filepath)
                filepath = new_filepath
                print(f"Renamed file: {filename} -> {new_filename}")

            # Update text content ins
