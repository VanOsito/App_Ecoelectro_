using System.Globalization;

namespace App.Converters
{
    public class ResiduoColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Collections.Generic.List<string> residuos && residuos != null)
            {
                // Morado para electrónicos generales
                if (residuos.Any(r => !string.IsNullOrEmpty(r) &&
                    (r.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Television", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Player", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Printer", StringComparison.OrdinalIgnoreCase))))
                    return Color.FromArgb("#805AD5");

                // Verde para baterías y componentes
                if (residuos.Any(r => !string.IsNullOrEmpty(r) &&
                    (r.Contains("Battery", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("PCB", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Keyboard", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Mouse", StringComparison.OrdinalIgnoreCase))))
                    return Color.FromArgb("#38A169");

                // Café para electrodomésticos grandes
                if (residuos.Any(r => !string.IsNullOrEmpty(r) &&
                    (r.Contains("Washing", StringComparison.OrdinalIgnoreCase) ||
                     r.Contains("Microwave", StringComparison.OrdinalIgnoreCase))))
                    return Color.FromArgb("#A0522D");
            }

            return Color.FromArgb("#805AD5"); // Morado por defecto
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}