from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
main_vm_path = ROOT / "ViewModels" / "MainViewModel.cs"
backend_constants_path = ROOT / "Constants" / "BackendConstants.cs"
main_vm = main_vm_path.read_text(encoding="utf-8")
backend_constants = backend_constants_path.read_text(encoding="utf-8")
errors: list[str] = []

if "CreateLessonChatViewModelAsync" not in main_vm:
    errors.append("Lesson chat view model creation must be async so runtime lesson content can be awaited without blocking the UI thread.")

if "LoadRuntimeLessonScenarioForSubtopicAsync" not in main_vm:
    errors.append("Runtime lesson scenario loading must use an async helper.")

if "RuntimeLessonScenarioRequestTimeoutSeconds" not in backend_constants:
    errors.append("Runtime lesson scenario loading must use a named timeout constant.")

runtime_method_match = re.search(
    r"private\s+async\s+Task<LessonScenario>\s+LoadRuntimeLessonScenarioForSubtopicAsync\(.*?(?=\n    private |\n    \[RelayCommand|\Z)",
    main_vm,
    re.S,
)
if not runtime_method_match:
    errors.append("Could not find async runtime lesson scenario loader.")
else:
    runtime_method = runtime_method_match.group(0)
    for label, pattern in {
        "GetAwaiter().GetResult()": r"\.GetAwaiter\(\)\.GetResult\(\)",
        ".Result": r"\.Result\b",
        ".Wait()": r"\.Wait\s*\(",
    }.items():
        if re.search(pattern, runtime_method):
            errors.append(f"Runtime lesson scenario loading must not use blocking async call {label}.")

    if "await backendLessonContentClient.GetRuntimeScenarioAsync" not in runtime_method:
        errors.append("Runtime lesson scenario loading must await BackendLessonContentClient.GetRuntimeScenarioAsync.")
    if "Using packaged local lesson scenario fallback" not in runtime_method:
        errors.append("Runtime lesson scenario loading must keep packaged local content fallback logging.")
    if "Using backend runtime lesson scenario" not in runtime_method:
        errors.append("Runtime lesson scenario loading must log when backend runtime content is used.")

lesson_chat_creation_match = re.search(
    r"private\s+async\s+Task<LessonChatViewModel>\s+CreateLessonChatViewModelAsync\(.*?(?=\n    private |\n    \[RelayCommand|\Z)",
    main_vm,
    re.S,
)
if not lesson_chat_creation_match:
    errors.append("Could not find async lesson chat view model factory.")
else:
    lesson_chat_creation = lesson_chat_creation_match.group(0)
    for label, pattern in {
        "GetAwaiter().GetResult()": r"\.GetAwaiter\(\)\.GetResult\(\)",
        ".Result": r"\.Result\b",
        ".Wait()": r"\.Wait\s*\(",
    }.items():
        if re.search(pattern, lesson_chat_creation):
            errors.append(f"Lesson chat creation must not use blocking async call {label}.")
    if "await LoadRuntimeLessonScenarioForSubtopicAsync" not in lesson_chat_creation:
        errors.append("Lesson chat creation must await runtime lesson scenario loading.")

if re.search(r"GetRuntimeScenarioAsync\([^;]+?\.GetAwaiter\(\)\.GetResult\(\)", main_vm, re.S):
    errors.append("BackendLessonContentClient.GetRuntimeScenarioAsync must not be synchronously blocked with GetAwaiter().GetResult().")
if re.search(r"GetRuntimeScenarioAsync\([^;]+?\.Result\b", main_vm, re.S):
    errors.append("BackendLessonContentClient.GetRuntimeScenarioAsync must not be synchronously blocked with .Result.")
if re.search(r"GetRuntimeScenarioAsync\([^;]+?\.Wait\s*\(", main_vm, re.S):
    errors.append("BackendLessonContentClient.GetRuntimeScenarioAsync must not be synchronously blocked with .Wait().")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("Lesson runtime content async policy passed: lesson start awaits backend runtime content with timeout and local fallback, without blocking waits.")
