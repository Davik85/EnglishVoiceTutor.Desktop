param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$EmailPrefix = "single-active-lesson-smoke",
    [string]$Password = "TestPassword123!"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$AuthRegisterPath = "/api/auth/register"
$LessonSessionsPath = "/api/me/lesson-sessions"
$ActiveLessonExistsCode = "active_lesson_exists"

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

    Write-Step "Verify second active lesson for same user is blocked"
    $blockedLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "second") -ExpectedStatus 409
    Assert-Equal -Expected $ActiveLessonExistsCode -Actual $blockedLesson.Body.error -Message "Blocked lesson error code"
    Assert-Equal -Expected $ActiveLessonExistsCode -Actual $blockedLesson.Body.code -Message "Blocked lesson machine-readable code"
    Write-Pass "Second active lesson was blocked with active_lesson_exists"

    Write-Step "Verify another user can start their own lesson"
    $otherUserLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userTwoHeaders -Body (New-LessonStartBody -Suffix "other-user") -ExpectedStatus 201
    Assert-NotEmpty -Value $otherUserLesson.Body.id -Message "Other user lesson session id must not be empty."
    Write-Pass "Another user can start a lesson independently"

    Write-Step "Finish user one's first lesson"
    $finishBody = @{ validTurnCount = 0 }
    $finishResult = Invoke-JsonExpectedStatus -Method "PUT" -Url "$BaseUrl$LessonSessionsPath/$($firstLesson.Body.id)/finish" -Headers $userOneHeaders -Body $finishBody -ExpectedStatus 200
    Assert-Equal -Expected "Finished" -Actual $finishResult.Body.status -Message "Finished lesson status"
    Write-Pass "First lesson finished"

    Write-Step "Verify user one can start another lesson after finishing"
    $afterFinishLesson = Invoke-JsonExpectedStatus -Method "POST" -Url "$BaseUrl$LessonSessionsPath" -Headers $userOneHeaders -Body (New-LessonStartBody -Suffix "after-finish") -ExpectedStatus 201
    Assert-NotEmpty -Value $afterFinishLesson.Body.id -Message "After-finish lesson session id must not be empty."
    Write-Pass "Starting after finish succeeds"

    Write-Pass "Single active lesson guard smoke test passed. Stale active lessons are covered by the backend 12-hour stale policy constant and should be validated with database time travel in integration environments."
}
catch {
    Fail $_.Exception.Message
}
