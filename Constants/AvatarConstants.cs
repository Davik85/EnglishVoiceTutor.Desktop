using System;
using System.IO;
using System.Windows;
using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Constants;

public static class AvatarConstants
{
    private const string PackUriPrefix = "pack://application:,,,/";
    private const string AvatarAssetRootPath = "Assets/Avatars";
    private const string AvatarFilePrefix = "avatar-";
    private const string AvatarFileExtension = ".gif";

    public const string ElenaAvatarId = TutorAvatarOptions.DefaultAvatarId;

    public const string IdleStateName = "idle";
    public const string ListeningStateName = "listening";
    public const string TranscribingStateName = "transcribing";
    public const string ThinkingStateName = "thinking";
    public const string SpeakingStateName = "speaking";

    public const string IdleAnimationPath = "Assets/Avatars/avatar-idle.gif";
    public const string ListeningAnimationPath = "Assets/Avatars/avatar-listening.gif";
    public const string TranscribingAnimationPath = "Assets/Avatars/avatar-transcribing.gif";
    public const string ThinkingAnimationPath = "Assets/Avatars/avatar-thinking.gif";
    public const string SpeakingAnimationPath = "Assets/Avatars/avatar-speaking.gif";
    public const string FallbackImagePath = "Assets/Avatars/avatar-fallback.png";

    public const string IdleDisplayText = "Idle";
    public const string ListeningDisplayText = "Listening";
    public const string TranscribingDisplayText = "Transcribing";
    public const string ThinkingDisplayText = "Thinking";
    public const string SpeakingDisplayText = "Speaking";

    public static string GetAnimationPath(AvatarState state, string? avatarId)
    {
        var normalizedAvatarId = TutorAvatarOptions.GetById(avatarId).Id;
        var stateName = GetStateName(state);

        var selectedAvatarPath = BuildNestedAvatarPath(normalizedAvatarId, stateName);
        if (ResourceExists(selectedAvatarPath))
        {
            return selectedAvatarPath;
        }

        var elenaPath = BuildNestedAvatarPath(ElenaAvatarId, stateName);
        if (ResourceExists(elenaPath))
        {
            return elenaPath;
        }

        var legacyPath = GetLegacyAnimationPath(state);
        if (ResourceExists(legacyPath))
        {
            return legacyPath;
        }

        return legacyPath;
    }

    public static string GetDisplayText(AvatarState state)
    {
        return state switch
        {
            AvatarState.Listening => ListeningDisplayText,
            AvatarState.Transcribing => TranscribingDisplayText,
            AvatarState.Thinking => ThinkingDisplayText,
            AvatarState.Speaking => SpeakingDisplayText,
            _ => IdleDisplayText
        };
    }

    public static Uri ToPackUri(string assetPath)
    {
        return new Uri($"{PackUriPrefix}{assetPath}", UriKind.Absolute);
    }

    private static string GetStateName(AvatarState state)
    {
        return state switch
        {
            AvatarState.Listening => ListeningStateName,
            AvatarState.Transcribing => TranscribingStateName,
            AvatarState.Thinking => ThinkingStateName,
            AvatarState.Speaking => SpeakingStateName,
            _ => IdleStateName
        };
    }

    private static string BuildNestedAvatarPath(string avatarId, string stateName)
    {
        return $"{AvatarAssetRootPath}/{avatarId}/{AvatarFilePrefix}{stateName}{AvatarFileExtension}";
    }

    private static string GetLegacyAnimationPath(AvatarState state)
    {
        return state switch
        {
            AvatarState.Listening => ListeningAnimationPath,
            AvatarState.Transcribing => TranscribingAnimationPath,
            AvatarState.Thinking => ThinkingAnimationPath,
            AvatarState.Speaking => SpeakingAnimationPath,
            _ => IdleAnimationPath
        };
    }

    private static bool ResourceExists(string assetPath)
    {
        try
        {
            var uri = ToPackUri(assetPath);
            using var resourceStream = Application.GetResourceStream(uri)?.Stream;
            return resourceStream is not null;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
