using System.Globalization;
using System.Text;

namespace EnglishVoiceTutor.Shared.UserProfiles;

public static class UserDisplayNamePolicy
{
    public static bool TryNormalize(string? value, out string? normalized)
    {
        normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            normalized = null;
            return true;
        }

        if (!string.Equals(value, normalized, StringComparison.Ordinal)) return false;

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is not (
                UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter)) return false;
        }

        return true;
    }
}
