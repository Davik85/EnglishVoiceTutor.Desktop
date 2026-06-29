using System.Reflection;

namespace EnglishVoiceTutor.Desktop.Services.Updates;

public enum DesktopDistributionChannel
{
    Direct,
    Store
}

public static class DesktopDistributionChannelProvider
{
    public const string DirectChannelName = "Direct";
    public const string StoreChannelName = "Store";
    public const string MetadataName = "DesktopDistributionChannel";

    public static DesktopDistributionChannel CurrentChannel { get; } = ResolveCurrentChannel();

    public static bool IsDirect => CurrentChannel == DesktopDistributionChannel.Direct;

    public static bool IsStore => CurrentChannel == DesktopDistributionChannel.Store;

    public static string CurrentChannelName => CurrentChannel.ToString();

    private static DesktopDistributionChannel ResolveCurrentChannel()
    {
        var configuredChannel = typeof(DesktopDistributionChannelProvider)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, MetadataName, StringComparison.Ordinal))
            ?.Value;

        return Parse(configuredChannel);
    }

    private static DesktopDistributionChannel Parse(string? configuredChannel)
    {
        if (string.IsNullOrWhiteSpace(configuredChannel) || string.Equals(configuredChannel.Trim(), DirectChannelName, StringComparison.OrdinalIgnoreCase))
        {
            return DesktopDistributionChannel.Direct;
        }

        if (string.Equals(configuredChannel.Trim(), StoreChannelName, StringComparison.OrdinalIgnoreCase))
        {
            return DesktopDistributionChannel.Store;
        }

        throw new InvalidOperationException($"Unsupported desktop distribution channel '{configuredChannel}'. Use '{DirectChannelName}' or '{StoreChannelName}'.");
    }
}
