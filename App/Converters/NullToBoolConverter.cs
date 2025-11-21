using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace App.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is string s) return !string.IsNullOrWhiteSpace(s);
            if (value is System.Collections.IEnumerable e)
            {
                var en = e.GetEnumerator();
                try { return en.MoveNext(); }
                finally { (en as IDisposable)?.Dispose(); }
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
