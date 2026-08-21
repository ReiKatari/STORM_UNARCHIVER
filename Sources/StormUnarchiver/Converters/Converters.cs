using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using StormUnarchiver.Models;
using Windows.UI;

namespace StormUnarchiver.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Success => new SolidColorBrush(ColorHelper.FromArgb(255, 74, 222, 128)),
                LogLevel.Warning => new SolidColorBrush(ColorHelper.FromArgb(255, 251, 191, 36)),
                LogLevel.Error => new SolidColorBrush(ColorHelper.FromArgb(255, 248, 113, 113)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(255, 148, 163, 184)),
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class BoolToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isSet = value is bool b && b;
        return isSet
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 226, 232, 240))  // StormText
            : new SolidColorBrush(ColorHelper.FromArgb(255, 100, 110, 130)); // Dim placeholder
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b && !b;
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (value is bool b && b) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Microsoft.UI.Xaml.Visibility v && v == Microsoft.UI.Xaml.Visibility.Visible;
}
