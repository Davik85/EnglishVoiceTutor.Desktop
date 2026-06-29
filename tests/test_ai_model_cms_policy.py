from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    return (ROOT / path).read_text(encoding='utf-8')

def test_backend_runtime_uses_ai_model_settings_provider():
    assert 'IAiModelSettingsService' in read('backend/EnglishVoiceTutor.Api/Services/OpenAiOptionsProvider.cs')
    assert '_aiModelSettingsService.GetActiveSettings().SpeechToTextModel' in read('backend/EnglishVoiceTutor.Api/Services/AudioTranscriptionService.cs')
    assert '_aiModelSettingsService.GetActiveSettings().LessonChatTextToSpeechModel' in read('backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs')
    assert 'aiModelSettingsService.GetActiveSettings().RealtimeVoiceModel' in read('backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs')

def test_admin_ai_model_endpoints_are_bootstrap_admin_only():
    endpoints = read('backend/EnglishVoiceTutor.Api/Endpoints/AiModelSettingsAdminEndpoints.cs')
    assert endpoints.count('RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)') == 5
    assert 'OPENAI_API_KEY' not in endpoints

def test_ai_model_settings_validation_rejects_injection_and_uses_json_file_storage():
    service = read('backend/EnglishVoiceTutor.Api/Services/AiModelSettingsService.cs')
    assert 'site", "content", "ai-model-settings.json"' in service
    assert 'GeneratedRegex("^[A-Za-z0-9._:-]+$"' in service
    assert 'is required' in service
    assert 'fallback default model settings are being used' in service
    assert 'ApiKey' not in service

def test_desktop_does_not_hardcode_ai_model_ids():
    desktop_constants = read('Constants/BackendConstants.cs')
    forbidden = ['gpt-5.2', 'gpt-4o-mini-transcribe', 'gpt-4o-mini-tts', 'tts-1', 'gpt-realtime']
    for model in forbidden:
        assert model not in desktop_constants
