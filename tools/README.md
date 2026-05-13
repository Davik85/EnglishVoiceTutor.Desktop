# Lesson content audit tools

## Run the lesson content audit on Windows

From the repository root, use the supported Windows PowerShell audit command:

```powershell
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
```

If your PowerShell execution policy already allows local scripts, you can use the shorter form:

```powershell
.\tools\audit_lesson_content.ps1
```

Python is not required for the Windows audit workflow. The older Python audit remains optional duplicate tooling only if Python is already installed:

```powershell
python tools\audit_lesson_content.py
```

## Run before commit

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet build -c Release
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
```

The PowerShell audit uses only built-in PowerShell/.NET functionality. It validates lesson JSON, expected lesson folders and files, taxonomy metadata, level profile fields, level-specific turn limits, Cyrillic-free content, obsolete per-level folders, generic copied phrases, lesson-type safety/content expectations, and lightweight C# routing coverage.
