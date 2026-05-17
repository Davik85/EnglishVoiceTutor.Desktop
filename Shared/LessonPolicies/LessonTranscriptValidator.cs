using System.Globalization;
using System.Text;

namespace EnglishVoiceTutor.Shared.LessonPolicies;

public enum LessonTranscriptValidationReason
{
    Valid,
    Empty,
    Placeholder,
    PunctuationOrNoiseOnly,
    NonLatinScript,
    MostlyNonLatinScript,
    TooShort,
    NoEnglishContent
}

public sealed record LessonTranscriptValidationResult(
    bool IsValid,
    LessonTranscriptValidationReason Reason,
    string NormalizedTranscript)
{
    public static LessonTranscriptValidationResult Valid(string normalizedTranscript) =>
        new(true, LessonTranscriptValidationReason.Valid, normalizedTranscript);

    public static LessonTranscriptValidationResult Invalid(LessonTranscriptValidationReason reason, string normalizedTranscript = "") =>
        new(false, reason, normalizedTranscript);
}

public static class LessonTranscriptValidator
{
    public const string VoiceMessagePlaceholder = "[Voice message]";
    public const string InvalidTranscriptUserMessage = "[Voice not recognized. Please try again in the study language.]";
    public const string RetryMessage = "Please try again in the study language.";
    public const string A1RetryMessage = "Please say it again in the study language.";
    public const int MinimumUsefulTranscriptLetters = 2;
    private const double MaximumNonLatinLetterRatio = 0.35;

    private static readonly HashSet<string> AllowedShortEnglishAnswers = new(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "i",
        "ok",
        "no",
        "yes",
        "hi"
    };

    public static LessonTranscriptValidationResult Validate(string? transcript, bool allowsOneLetterAnswer = false)
    {
        var normalized = NormalizeTranscript(transcript);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.Empty);
        }

        if (string.Equals(normalized, VoiceMessagePlaceholder, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, InvalidTranscriptUserMessage, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized.Trim('[', ']'), InvalidTranscriptUserMessage.Trim('[', ']'), StringComparison.OrdinalIgnoreCase))
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.Placeholder, normalized);
        }

        if (!HasLetterOrDigit(normalized))
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.PunctuationOrNoiseOnly, normalized);
        }

        if (ContainsBlockedScript(normalized))
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.NonLatinScript, normalized);
        }

        var letterCount = 0;
        var latinLetterCount = 0;
        var englishUsefulLetterCount = 0;
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (!RuneIsLetter(rune))
            {
                continue;
            }

            letterCount++;
            if (IsLatinLetter(rune))
            {
                latinLetterCount++;
                if (IsAsciiEnglishLetter(rune))
                {
                    englishUsefulLetterCount++;
                }
            }
        }

        if (letterCount == 0)
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.PunctuationOrNoiseOnly, normalized);
        }

        var nonLatinLetterCount = letterCount - latinLetterCount;
        if (nonLatinLetterCount > 0 && (double)nonLatinLetterCount / letterCount > MaximumNonLatinLetterRatio)
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.MostlyNonLatinScript, normalized);
        }

        if (englishUsefulLetterCount == 0)
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.NoEnglishContent, normalized);
        }

        var compactLetters = new string(normalized.Where(char.IsLetter).ToArray());
        if (!allowsOneLetterAnswer && compactLetters.Length < MinimumUsefulTranscriptLetters)
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.TooShort, normalized);
        }

        if (!allowsOneLetterAnswer && compactLetters.Length <= MinimumUsefulTranscriptLetters && !AllowedShortEnglishAnswers.Contains(compactLetters))
        {
            return LessonTranscriptValidationResult.Invalid(LessonTranscriptValidationReason.TooShort, normalized);
        }

        return LessonTranscriptValidationResult.Valid(normalized);
    }

    public static string GetRetryMessage(string selectedLevel)
    {
        return selectedLevel.TrimStart().StartsWith("A1", StringComparison.OrdinalIgnoreCase)
            ? A1RetryMessage
            : RetryMessage;
    }

    private static string NormalizeTranscript(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        return string.Join(' ', transcript.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool HasLetterOrDigit(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (RuneIsLetter(rune) || RuneIsDigit(rune))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsBlockedScript(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCyrillic(rune) || IsCjk(rune) || IsJapanese(rune) || IsKorean(rune))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RuneIsLetter(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter;
    }

    private static bool RuneIsDigit(Rune rune)
    {
        return Rune.GetUnicodeCategory(rune) == UnicodeCategory.DecimalDigitNumber;
    }

    private static bool IsAsciiEnglishLetter(Rune rune)
    {
        return rune.Value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsLatinLetter(Rune rune)
    {
        return rune.Value is >= 0x0041 and <= 0x007A
            or >= 0x00C0 and <= 0x024F
            or >= 0x1E00 and <= 0x1EFF;
    }

    private static bool IsCyrillic(Rune rune)
    {
        return rune.Value is >= 0x0400 and <= 0x052F
            or >= 0x2DE0 and <= 0x2DFF
            or >= 0xA640 and <= 0xA69F;
    }

    private static bool IsCjk(Rune rune)
    {
        return rune.Value is >= 0x3400 and <= 0x4DBF
            or >= 0x4E00 and <= 0x9FFF
            or >= 0xF900 and <= 0xFAFF;
    }

    private static bool IsJapanese(Rune rune)
    {
        return rune.Value is >= 0x3040 and <= 0x30FF;
    }

    private static bool IsKorean(Rune rune)
    {
        return rune.Value is >= 0xAC00 and <= 0xD7AF
            or >= 0x1100 and <= 0x11FF
            or >= 0x3130 and <= 0x318F;
    }
}
