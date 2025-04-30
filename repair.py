import os
import sys
import re

def repair_project(target_dir):
    project_name = os.path.basename(os.path.normpath(target_dir))
    print(f"Repairing project to match directory name: {project_name}")

    # Patterns to replace
    patterns = [
        # Assembly info
        (r'(\[assembly:\s*AssemblyTitle\()\s*"[^"]*"', rf'\1 "{project_name}"'),
        (r'(\[assembly:\s*AssemblyProduct\()\s*"[^"]*"', rf'\1 "{project_name}"'),
        # csproj file project name
        (r'(<AssemblyName>)\s*[^<]*\s*(</AssemblyName>)', rf'\1{project_name}\2'),
        (r'(<RootNamespace>)\s*[^<]*\s*(</RootNamespace>)', rf'\1{project_name}\2'),
        (r'(<ProjectGuid>[^<]*</ProjectGuid>)', r'\1'),  # Leave the GUID untouched
        # Namespace declarations
        (r'\bnamespace\s+\w+', f'namespace {project_name}'),
        # Debug.Log messages with old project name in brackets
        (r'Debug\.Log\(\s*"\[\w+\]', f'Debug.Log("[{project_name}]'),
        (r'Debug\.LogWarning\(\s*"\[\w+\]', f'Debug.LogWarning("[{project_name}]'),
        (r'Debug\.LogError\(\s*"\[\w+\]', f'Debug.LogError("[{project_name}]'),
    ]

    for root, dirs, files in os.walk(target_dir):
        for file in files:
            if file.endswith(".cs") or file.endswith(".csproj") or file.endswith(".sln") or file.endswith(".txt"):
                filepath = os.path.join(root, file)
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()

                original_content = content
                for pattern, replacement in patterns:
                    content = re.sub(pattern, replacement, content)

                if content != original_content:
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"Updated: {filepath}")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python repair.py <target_directory>")
        sys.exit(1)

    target_directory = sys.argv[1]
    repair_project(target_directory)
