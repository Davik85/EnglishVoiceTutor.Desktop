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
    assert endpoints.count('RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)') == 6
    assert 'AdminAiModelSettingsProviderTestRoute' in endpoints
    assert 'OPENAI_API_KEY' not in endpoints

def test_ai_model_settings_validation_rejects_injection_and_uses_json_file_storage():
    service = read('backend/EnglishVoiceTutor.Api/Services/AiModelSettingsService.cs')
    assert 'site", "content", "ai-model-settings.json"' in service
    assert 'GeneratedRegex("^[A-Za-z0-9._:-]+$"' in service
    assert 'is required' in service
    assert 'fallback default model settings are being used' in service
    assert 'Format validation does not prove provider access' in service
    assert 'ApiKey' not in service

def test_admin_ui_explains_validate_is_format_only_and_has_provider_test():
    html = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html')
    js = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js')
    assert 'Validate format' in html
    assert 'Format validation does not prove provider access. Use Test provider access before publishing a new model.' in html
    assert 'Test provider access' in html
    assert 'aiModelSettingsProviderTest' in js
    assert 'Draft was not published' in js


def test_provider_access_result_is_safe_and_uses_draft_request():
    models = read('backend/EnglishVoiceTutor.Api/Models/AiModelProviderTestModels.cs')
    service = read('backend/EnglishVoiceTutor.Api/Services/AiModelProviderAccessTestService.cs')
    for field in ['RoleId', 'RoleLabel', 'ModelId', 'SyntaxValid', 'ProviderTested', 'ProviderOk', 'SafeCategory', 'SafeMessage', 'StatusCode', 'DurationMs', 'ProviderErrorType', 'ProviderErrorCode', 'ProviderErrorParam', 'SanitizedProviderMessage']:
        assert field in models
    for category in ['unavailable_or_not_found', 'unauthorized_or_forbidden', 'rate_limited', 'quota_or_billing', 'invalid_request', 'provider_error', 'timeout', 'unknown']:
        assert category in models
    assert 'TestDraftAsync(AiModelSettings draft' in service
    assert 'SaveDraftAsync' not in service and 'PublishAsync' not in service
    assert 'ReadAsStringAsync' in service
    assert 'minimal_responses_text' in service
    assert 'current_provider_test_shape' in service
    assert 'minimal_structured_output' in service
    assert 'lesson_chat_runtime_shape_without_user_content' in service
    assert 'Safe diagnostic lesson input. No user lesson content is included.' in service
    assert 'Authorization' in service
    assert 'ApiKey' not in models


def test_runtime_failure_logs_safe_model_diagnostics():
    chat = read('backend/EnglishVoiceTutor.Api/Services/OpenAiLessonChatService.cs')
    assert 'modelRole=lesson_tutor_chat' in chat
    assert 'configuredModelId={ConfiguredModelId}' in chat
    assert 'providerStatusCode={ProviderStatusCode}' in chat
    assert 'safeCategory={SafeCategory}' in chat
    assert 'providerErrorType={ProviderErrorType}' in chat
    assert 'providerErrorCode={ProviderErrorCode}' in chat
    assert 'providerErrorParam={ProviderErrorParam}' in chat
    assert 'sanitizedProviderMessage={SanitizedProviderMessage}' in chat
    assert 'OpenAiProviderRequestException' in chat
    assert 'httpRequest.Headers.Authorization' in chat
    assert 'requestJson' in chat
    assert 'requestJson' not in chat[chat.rindex('private void LogProviderCallFailure'):]


def test_desktop_does_not_hardcode_ai_model_ids():
    desktop_constants = read('Constants/BackendConstants.cs')
    forbidden = ['gpt-5.2', 'gpt-4o-mini-transcribe', 'gpt-4o-mini-tts', 'tts-1', 'gpt-realtime']
    for model in forbidden:
        assert model not in desktop_constants
