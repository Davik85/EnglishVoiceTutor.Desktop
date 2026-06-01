param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$EmailPrefix = "single-active-lesson-smoke",
    [string]$Password = "TestPassword123!"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$AuthRegisterPath = "/api/auth/register"
$HealthPath = "/api/health"
$DatabaseHealthPath = "/api/health/database"
$LessonSessionsPath = "/api/me/lesson-sessions"
$LessonHeartbeatPathTemplate = "/api/lesson-sessions/{0}/heartbeat"
$LessonMessagesPathTemplate = "/api/me/lesson-sessions/{0}/messages"
$ActiveLessonAbandonPath = "/api/lesson-sessions/active/abandon"
$ActiveLessonExistsCode = "active_lesson_exists"
$LessonSessionEndedElsewhereCode = "lesson_session_ended_elsewhere"
$HeartbeatFreshnessWaitSeconds = 130

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
    exit 1
}

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body
    )

    $invokeParams = @{
        Method = $Method
        Uri = $Url
        UseBasicParsing = $true
    }

    if ($Headers) {
        $invokeParams.Headers = $Headers
    }

    if ($null -ne $Body) {
        $invokeParams.Body = ($Body | ConvertTo-Json -Depth 10)
        $invokeParams.ContentType = $JsonContentType
    }

    $response = Invoke-WebRequest @invokeParams
    $parsedBody = $null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
        $parsedBody = $response.Content | ConvertFrom-Json
    }

    return [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Body = $parsedBody
    }
}

function Test-HeartbeatSchemaError {
    param([string]$Content)

    return -not [string]::IsNullOrWhiteSpace($Content) -and (
        $Content -match "LastHeartbeatAtUtc" -or
        $Content -match "42703" -or
        $Content -match "column .*LastHeartbeatAtUtc.* does not exist")
}

function Fail-HeartbeatSchemaMissing {
    param([string]$Context, [string]$Content)

    if (Test-HeartbeatSchemaError -Content $Content) {
        Fail "$Context failed because lesson_sessions.LastHeartbeatAtUtc is missing. Apply the heartbeat EF migration first: dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj"
    }
}

function Invoke-JsonExpectedStatus {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body,
        [int]$ExpectedStatus
    )

    try {
        $result = Invoke-Json -Method $Method -Url $Url -Headers $Headers -Body $Body
        if ($result.StatusCode -ne $ExpectedStatus) {
            $content = $null
            if ($null -ne $result.Body) {
                $content = $result.Body | ConvertTo-Json -Depth 10 -Compress
            }

            Fail-HeartbeatSchemaMissing -Context "$Method $Url" -Content $content
            Fail "Expected HTTP $ExpectedStatus but got $($result.StatusCode) for $Method $Url"
        }

        return $result
    }
    catch {
        $httpStatus = $null
        $content = $null

        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $httpStatus = [int]$_.Exception.Response.StatusCode.value__
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = [System.IO.StreamReader]::new($stream)
                    $content = $reader.ReadToEnd()
                }
            }
            catch {
                $content = $null
            }
        }

        if ($httpStatus -ne $ExpectedStatus) {
            Fail-HeartbeatSchemaMissing -Context "$Method $Url" -Content $content
            throw
        }

        $parsedBody = $null
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $parsedBody = $content | ConvertFrom-Json
        }

        return [pscustomobject]@{
            StatusCode = $httpStatus
            Body = $parsedBody
        }
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        Fail "$Message. Expected '$Expected', actual '$Actual'."
    }
}

function Assert-NotEmpty {
    param($Value, [string]$Message)
    if ([string]::IsNullOrWhiteSpace([string]$Value)) {
        Fail $Message
    }
}

function New-LessonStartBody {
    param([string]$Suffix)
    return @{
        lessonContentId = "smoke-single-active-$Suffix"
        studyLanguage = "English"
        topicId = "smoke-topic"
        topicTitle = "Smoke topic"
        subtopicId = "smoke-subtopic-$Suffix"
        subtopicTitle = "Smoke subtopic $Suffix"
        level = "A1"
        selectedContextId = $null
        selectedContextTitle = $null
        modeUsed = "text"
    }
}

try {
    Write-Host "Single active lesson guard smoke test" -ForegroundColor Yellow
    Write-Host "BaseUrl: $BaseUrl"

    $runId = [Guid]::NewGuid().ToString("N")
    $userOneEmail = "$EmailPrefix-user1-$runId@example.com"
    $userTwoEmail = "$EmailPrefix-user2-$runId@example.com"

    Write-Step "Verify backend process is reachable"
    Invoke-JsonExpectedStatus -Method "GET" -Url "$BaseUrl$HealthPath" -Headers $null -Body $null -ExpectedStatus 200 | Out-Null
    Write-Pass "Backend health endpoint is reachable"

    Write-Step "Verify database health before active lesson guard checks"
    $databaseHealth = Invoke-JsonExpectedStatus -Method "GET" -Url "$BaseUrl$DatabaseHealthPath" -Headers $null -Body $null -ExpectedStatus 200
    if ($databaseHealth.Body.status -ne "Healthy" -or $databaseHealth.Body.canConnect -ne $true) {
        Fail "Backend is reachable, but database health is not Healthy. Apply EF migrations and verify database connectivity before running this smoke test."
    }
    Write-Pass "Database health endpoint is healthy"

    Write-Step "Register two independent users"
    $userOneAuth = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$AuthRegisterPath" -Headers $null -Body @{ email = $userOneEmail; password = $Password } -ExpectedStatus 201
    $userTwoAuth = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$AuthRegisterPath" -Headers $null -Body @{ email = $userTwoEmail; password = $Password } -ExpectedStatus 201
    Assert-NotEmpty -Value $userOneAuth.Body.accessToken -Message "User one token must not be empty."
    Assert-NotEmpty -Value $userTwoAuth.Body.accessToken -Message "User two token must not be empty."
    $userOneHeaders = @{ Authorization = "Bearer $($userOneAuth.Body.accessToken)" }
    $userTwoHeaders = @{ Authorization = "Bearer $($userTwoAuth.Body.accessToken)" }
    Write-Pass "Both users registered"

    Write-Step "Start first lesson for user one"
    $firstLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "first") -ExpectedStatus 201
    Assert-NotEmpty -Value $firstLesson.Body.id -Message "First lesson session id must not be empty."
    Write-Pass "First lesson started"

    Write-Step "Send heartbeat for user one first lesson"
    $heartbeatResult = Invoke-JsonExpectedStatus -Method "POST" -Url ("$BaseUrl$LessonHeartbeatPathTemplate" -f $firstLesson.Body.id) -Headers $userOneHeaders -Body $null -ExpectedStatus 200
    Assert-NotEmpty -Value $heartbeatResult.Body.lastHeartbeatAtUtc -Message "Heartbeat timestamp must not be empty."
    Write-Pass "Heartbeat endpoint updated the active lesson"

    Write-Step "Verify second active lesson for same user is blocked while heartbeat is fresh"
    $blockedLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "second") -ExpectedStatus 409
    Assert-Equal -Expected $ActiveLessonExistsCode -Actual $blockedLesson.Body.error -Message "Blocked lesson error code"
    Assert-Equal -Expected $ActiveLessonExistsCode -Actual $blockedLesson.Body.code -Message "Blocked lesson machine-readable code"
    Assert-Equal -Expected $true -Actual $blockedLesson.Body.canEndOtherLesson -Message "Blocked lesson can be released by user"
    Write-Pass "Second active lesson was blocked with active_lesson_exists"

    Write-Step "Release user one's active lesson from the current authenticated account"
    $releaseResult = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$ActiveLessonAbandonPath" -Headers $userOneHeaders -Body $null -ExpectedStatus 200
    Assert-Equal -Expected $true -Actual $releaseResult.Body.released -Message "Active lesson release result"
    Assert-Equal -Expected "Abandoned" -Actual $releaseResult.Body.status -Message "Released lesson status"
    Write-Pass "Active lesson release endpoint abandoned user one's active lesson"

    Write-Step "Verify old released lesson heartbeat is rejected and cannot revive the session"
    $oldHeartbeat = Invoke-JsonExpectedStatus -Method "POST" -Url ("$BaseUrl$LessonHeartbeatPathTemplate" -f $firstLesson.Body.id) -Headers $userOneHeaders -Body $null -ExpectedStatus 409
    Assert-Equal -Expected $LessonSessionEndedElsewhereCode -Actual $oldHeartbeat.Body.error -Message "Old released heartbeat error code"
    Assert-Equal -Expected $LessonSessionEndedElsewhereCode -Actual $oldHeartbeat.Body.code -Message "Old released heartbeat machine-readable code"
    $oldHeartbeatRetry = Invoke-JsonExpectedStatus -Method "POST" -Url ("$BaseUrl$LessonHeartbeatPathTemplate" -f $firstLesson.Body.id) -Headers $userOneHeaders -Body $null -ExpectedStatus 409
    Assert-Equal -Expected $LessonSessionEndedElsewhereCode -Actual $oldHeartbeatRetry.Body.error -Message "Old released heartbeat retry error code"
    Write-Pass "Old released heartbeat is rejected and does not revive the abandoned session"

    Write-Step "Verify old released lesson-bound message creation is rejected"
    $oldMessageBody = @{ role = "user"; text = "Hello after release"; source = "typed"; turnNumber = 1; isValidLessonTurn = $true; studyLanguage = "English"; transcriptConfidence = $null; audioDurationMs = $null }
    $oldMessage = Invoke-JsonExpectedStatus -Method "POST" -Url ("$BaseUrl$LessonMessagesPathTemplate" -f $firstLesson.Body.id) -Headers $userOneHeaders -Body $oldMessageBody -ExpectedStatus 409
    Assert-Equal -Expected $LessonSessionEndedElsewhereCode -Actual $oldMessage.Body.error -Message "Old released lesson message error code"
    Write-Pass "Old released lesson-bound message creation is rejected"

    Write-Step "Verify user one can start after releasing the active lesson"
    $afterReleaseLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "after-release") -ExpectedStatus 201
    Assert-NotEmpty -Value $afterReleaseLesson.Body.id -Message "After-release lesson session id must not be empty."
    Write-Pass "Starting after active lesson release succeeds"

    Write-Step "Verify another user can start their own lesson"
    $otherUserLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userTwoHeaders -Body (New-LessonStartBody -Suffix "other-user") -ExpectedStatus 201
    Assert-NotEmpty -Value $otherUserLesson.Body.id -Message "Other user lesson session id must not be empty."
    Write-Pass "Another user can start a lesson independently"

    Write-Step "Verify user two remains blocked by their own active lesson until they release it"
    $userTwoBlocked = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userTwoHeaders -Body (New-LessonStartBody -Suffix "other-user-blocked") -ExpectedStatus 409
    Assert-Equal -Expected $ActiveLessonExistsCode -Actual $userTwoBlocked.Body.error -Message "Other user own active lesson error code"
    Write-Pass "Release by user one did not affect user two's active lesson"

    Write-Step "Release user two's active lesson for cleanup"
    $userTwoRelease = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$ActiveLessonAbandonPath" -Headers $userTwoHeaders -Body $null -ExpectedStatus 200
    Assert-Equal -Expected $true -Actual $userTwoRelease.Body.released -Message "User two active lesson release result"
    Write-Pass "User two active lesson cleanup release succeeded"

    Write-Step "Finish user one's after-release lesson"
    $finishBody = @{ validTurnCount = 0 }
    $finishResult = Invoke-JsonExpectedStatus -Method "PUT" -Url "$BaseUrl$LessonSessionsPath/$($afterReleaseLesson.Body.id)/finish" -Headers $userOneHeaders -Body $finishBody -ExpectedStatus 200
    Assert-Equal -Expected "Finished" -Actual $finishResult.Body.status -Message "Finished lesson status"
    Write-Pass "After-release lesson finished"

    Write-Step "Verify user one can start another lesson after finishing"
    $afterFinishLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "after-finish") -ExpectedStatus 201
    Assert-NotEmpty -Value $afterFinishLesson.Body.id -Message "After-finish lesson session id must not be empty."
    Write-Pass "Starting after finish succeeds"

    Write-Step "Wait for heartbeat freshness window to expire"
    Start-Sleep -Seconds $HeartbeatFreshnessWaitSeconds
    Write-Pass "Waited $HeartbeatFreshnessWaitSeconds seconds for stale heartbeat"

    Write-Step "Verify stale heartbeat no longer blocks user one"
    $afterStaleLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "after-stale-heartbeat") -ExpectedStatus 201
    Assert-NotEmpty -Value $afterStaleLesson.Body.id -Message "After-stale lesson session id must not be empty."
    Write-Pass "Stale heartbeat no longer blocks a new lesson"

    Write-Pass "Single active lesson guard smoke test passed."
}
catch {
    Fail-HeartbeatSchemaMissing -Context "single active lesson guard smoke test" -Content $_.Exception.Message
    Fail $_.Exception.Message
}
