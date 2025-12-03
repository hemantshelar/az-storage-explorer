using Microsoft.UI.Xaml.Data;

namespace AzStorageExplorer.Converters;

public class BlobIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isDirectory)
        {
            return isDirectory ? "\uE8B7" : "\uE8A5"; // Folder or File icon
        }
        return "\uE8A5"; // Default file icon
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

