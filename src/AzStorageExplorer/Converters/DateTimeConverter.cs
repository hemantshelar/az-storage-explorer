using Microsoft.UI.Xaml.Data;

namespace AzStorageExplorer.Converters;

public class DateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.LocalDateTime.ToString("g");
        }
        if (value is DateTime dateTime)
        {
            return dateTime.ToString("g");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

