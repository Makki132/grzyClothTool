using grzyClothTool.Models.Drawable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Diagnostics;

namespace grzyClothTool.Converters
{
    /// <summary>
    /// Converter that counts male and female drawables in a collection and returns a formatted string
    /// </summary>
    public class GenderCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Debug.WriteLine($"GenderCountConverter - Input type: {value?.GetType().Name ?? "null"}");
                
                // Handle both IEnumerable<object> and ReadOnlyObservableCollection from CollectionViewGroup
                if (value is IEnumerable items)
                {
                    var drawables = items.OfType<GDrawable>().ToList();
                    Debug.WriteLine($"GenderCountConverter - Found {drawables.Count} drawables");
                    
                    // Count males (Sex = male = 1) and females (Sex = female = 0)
                    int maleCount = drawables.Count(d => d.Sex == Enums.SexType.male);
                    int femaleCount = drawables.Count(d => d.Sex == Enums.SexType.female);
                    
                    Debug.WriteLine($"GenderCountConverter - Males: {maleCount}, Females: {femaleCount}");
                    
                    return $"(M: {maleCount} / F: {femaleCount})";
                }
                
                Debug.WriteLine("GenderCountConverter - Value is not IEnumerable");
                return "(M: 0 / F: 0)";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenderCountConverter - Exception: {ex.Message}");
                return "(Error)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
