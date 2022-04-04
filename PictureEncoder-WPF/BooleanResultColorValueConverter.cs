using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PictureEncoder_WPF
{
    internal class BooleanResultColorValueConverter : IValueConverter
    {
        private static readonly SolidColorBrush trueBursh = new() { Color = Color.FromRgb(0, 255, 0) };
        private static readonly SolidColorBrush falseBursh = new() { Color = Color.FromRgb(255, 0, 0) };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool) { return new ArgumentException("Argument must a bool type", nameof(value)); }
            var v = (bool)value;
            return v ? trueBursh : falseBursh;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
