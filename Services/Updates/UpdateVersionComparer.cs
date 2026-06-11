namespace EnglishVoiceTutor.Desktop.Services.Updates;

public static class UpdateVersionComparer
{
    public static int Compare(string installedVersion, string manifestVersion)
    {
        var installed = Parse(installedVersion);
        var manifest = Parse(manifestVersion);

        for (var index = 0; index < 3; index++)
        {
            var segmentComparison = installed.Core[index].CompareTo(manifest.Core[index]);
            if (segmentComparison != 0)
            {
                return Math.Sign(segmentComparison);
            }
        }

        return ComparePrerelease(installed.Prerelease, manifest.Prerelease);
    }

    public static string GetChannel(string version)
    {
        var prerelease = Parse(version).Prerelease;
        if (string.IsNullOrWhiteSpace(prerelease))
        {
            return "stable";
        }

        var firstToken = prerelease.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstToken) ? "prerelease" : firstToken;
    }

    private static ParsedVersion Parse(string version)
    {
        var normalized = (version ?? string.Empty).Trim();
        if (normalized.StartsWith('v'))
        {
            normalized = normalized[1..];
        }

        var buildMetadataIndex = normalized.IndexOf('+', StringComparison.Ordinal);
        if (buildMetadataIndex >= 0)
        {
            normalized = normalized[..buildMetadataIndex];
        }

        var prerelease = string.Empty;
        var prereleaseIndex = normalized.IndexOf('-', StringComparison.Ordinal);
        if (prereleaseIndex >= 0)
        {
            prerelease = normalized[(prereleaseIndex + 1)..];
            normalized = normalized[..prereleaseIndex];
        }

        var core = new[] { 0, 0, 0 };
        var segments = normalized.Split('.', StringSplitOptions.None);
        for (var index = 0; index < Math.Min(core.Length, segments.Length); index++)
        {
            core[index] = ReadLeadingNumber(segments[index]);
        }

        return new ParsedVersion(core, prerelease);
    }

    private static int ComparePrerelease(string installed, string manifest)
    {
        if (string.IsNullOrWhiteSpace(installed) && string.IsNullOrWhiteSpace(manifest))
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(installed))
        {
            return 1;
        }

        if (string.IsNullOrWhiteSpace(manifest))
        {
            return -1;
        }

        var installedTokens = installed.Split('.', StringSplitOptions.None);
        var manifestTokens = manifest.Split('.', StringSplitOptions.None);
        var count = Math.Max(installedTokens.Length, manifestTokens.Length);

        for (var index = 0; index < count; index++)
        {
            if (index >= installedTokens.Length)
            {
                return -1;
            }

            if (index >= manifestTokens.Length)
            {
                return 1;
            }

            var left = installedTokens[index];
            var right = manifestTokens[index];
            var leftNumeric = IsNumericToken(left);
            var rightNumeric = IsNumericToken(right);

            if (leftNumeric && rightNumeric)
            {
                var numericComparison = ParseNumericToken(left).CompareTo(ParseNumericToken(right));
                if (numericComparison != 0)
                {
                    return Math.Sign(numericComparison);
                }

                continue;
            }

            if (leftNumeric && !rightNumeric)
            {
                return -1;
            }

            if (!leftNumeric && rightNumeric)
            {
                return 1;
            }

            var alphaComparison = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            if (alphaComparison != 0)
            {
                return Math.Sign(alphaComparison);
            }
        }

        return 0;
    }

    private static int ReadLeadingNumber(string segment)
    {
        var token = new string((segment ?? string.Empty).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(token, out var number) ? number : 0;
    }

    private static long ParseNumericToken(string value) =>
        long.TryParse(value, out var number) ? number : long.MaxValue;

    private static bool IsNumericToken(string value) => !string.IsNullOrEmpty(value) && value.All(char.IsDigit);

    private sealed record ParsedVersion(int[] Core, string Prerelease);
}
