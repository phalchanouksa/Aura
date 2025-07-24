using System;
using System.Globalization;
using System.Windows.Data;

namespace Aura
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? parameter : Binding.DoNothing;
        }
    }

    // The enum for focus modes is also in this file
    public enum FocusMode
    {
        Window,
        Cursor,
        ReadingLine
    }
    public enum EffectMode
    {
        Off,
        Dim,
        Blur
    }
}