using System;
using System.Globalization;
using System.Windows.Data;

namespace EnglishVoiceTutor.Desktop.Converters;

public sealed class SelectionEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is null || values[1] is null)
        {
            return string.Empty;
        }

        return ReferenceEquals(values[0], values[1]) || values[0].Equals(values[1])
            ? "Selected"
            : string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
