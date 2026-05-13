# Lesson content audit tools

## Run the lesson content audit on Windows

From the repository root:

```powershell
py -3 tools/audit_lesson_content.py
```

or:

```powershell
python tools/audit_lesson_content.py
```

## Run before commit

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet build -c Release
py -3 tools/audit_lesson_content.py
```

The audit uses only the Python 3 standard library. It validates lesson JSON, expected lesson folders and files, taxonomy metadata, level profile fields, level-specific turn limits, Cyrillic-free content, obsolete per-level folders, generic copied phrases, and lightweight C# routing coverage.
