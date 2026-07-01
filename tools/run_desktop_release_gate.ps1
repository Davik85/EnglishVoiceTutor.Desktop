param(
    [switch]$IncludeEfChecks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ==="
}

function Invoke-GateCommand {
    param(
        [string]$Title,
        [scriptblock]$Command
    )

    Write-Section $Title
    $global:LASTEXITCODE = 0
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Gate command failed with exit code $LASTEXITCODE: $Title"
    }
}

$repoRoot = (Get-Location).Path
$requiredPaths = @(
    "README.md",
    "EnglishVoiceTutor.Desktop.csproj",
    "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj",
    "tools/audit_lesson_content.ps1",
    "tools/audit_interface_localization.ps1",
    "tools/audit_desktop_backend_boundary.ps1"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Desktop release gate must be run from the repository root. Missing expected path: $relativePath"
    }
}

Invoke-GateCommand "Git status" {
    $status = git status --short
    if ($status) {
        $status | ForEach-Object { Write-Host $_ }
        throw "Git working tree must be clean before reporting the desktop release gate as passed."
    }

    Write-Host "Git working tree is clean."
}

Invoke-GateCommand "dotnet restore" {
    dotnet restore
}

Invoke-GateCommand "dotnet build" {
    dotnet build
}

Invoke-GateCommand "dotnet build -c Release" {
    dotnet build -c Release
}

Invoke-GateCommand "Backend dotnet build" {
    dotnet build "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj"
}

Invoke-GateCommand "Lesson content audit" {
    & "tools/audit_lesson_content.ps1"
}

Invoke-GateCommand "Interface localization audit" {
    & "tools/audit_interface_localization.ps1"
}

Invoke-GateCommand "Desktop backend boundary audit" {
    & "tools/audit_desktop_backend_boundary.ps1"
}

Invoke-GateCommand "Tutor prompt policy" {
    python "tools/test_tutor_prompt_policy.py"
}

Invoke-GateCommand "Lesson behavior CMS ownership policy" {
    python "tools/test_lesson_behavior_policy.py"
}

Invoke-GateCommand "Admin RBAC permission policy foundation" {
    python "tools/test_admin_rbac_permission_policy_foundation.py"
}

Invoke-GateCommand "Admin role assignment persistence foundation" {
    python "tools/test_admin_role_assignment_persistence_foundation.py"
}

Invoke-GateCommand "Admin UI role management policy" {
    python "tools/test_admin_ui_role_management_policy.py"
}

Invoke-GateCommand "Admin RBAC cutover validation pack" {
    python "tools/test_admin_rbac_cutover_validation_pack.py"
}

Invoke-GateCommand "Admin roles permissions policy" {
    python "tools/test_admin_roles_permissions_policy.py"
}

if ($IncludeEfChecks) {
    Invoke-GateCommand "EF migrations list" {
        dotnet ef migrations list --project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj" --startup-project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj"
    }

    Invoke-GateCommand "EF database update" {
        dotnet ef database update --project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj" --startup-project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj"
    }

    Invoke-GateCommand "EF pending model changes" {
        dotnet ef migrations has-pending-model-changes --project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj" --startup-project "backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj"
    }
}
else {
    Write-Section "EF checks"
    Write-Host "Skipped. Run with -IncludeEfChecks after schema-affecting backend changes to include: dotnet ef migrations list; dotnet ef database update; dotnet ef migrations has-pending-model-changes."
}

Write-Section "Desktop release gate"
Write-Host "Desktop release smoke gate automated checks passed."
