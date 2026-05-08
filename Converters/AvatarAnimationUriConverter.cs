using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace EnglishVoiceTutor.Desktop.Converters;

public sealed class AvatarAnimationUriConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Uri uri)
        {
            return null;
        }

        try
        {
            var resourceStreamInfo = Application.GetResourceStream(uri);
            resourceStreamInfo?.Stream.Dispose();

            return resourceStreamInfo is null
                ? null
                : uri;
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
