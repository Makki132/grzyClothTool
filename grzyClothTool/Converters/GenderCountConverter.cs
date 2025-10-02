using grzyClothTool.Models.Drawable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace grzyClothTool.Converters
{
    /// <summary>
    /// Converter that counts male and female drawables in a collection and returns a formatted string
    /// </summary>
    public class GenderCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<object> items)
            {
                var drawables = items.OfType<GDrawable>().ToList();
                
                // Count males (Sex = 0) and females (Sex = 1)
                int maleCount = drawables.Count(d => d.Sex == Enums.SexType.male);
                int femaleCount = drawables.Count(d => d.Sex == Enums.SexType.female);
                
                return $"(M: {maleCount} / F: {femaleCount})";
            }
            
            return "(M: 0 / F: 0)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
