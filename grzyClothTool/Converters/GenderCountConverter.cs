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
    public class GenderCountConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // values[0] = ReadOnlyObservableCollection from CollectionViewGroup.Items
                // values[1] = IsExpanded (bool) - just used to trigger re-evaluation
                if (values != null && values.Length > 0 && values[0] is IEnumerable items)
                {
                    var drawables = items.OfType<GDrawable>().ToList();
                    
                    // Count males and females
                    int maleCount = drawables.Count(d => d.Sex == Enums.SexType.male);
                    int femaleCount = drawables.Count(d => d.Sex == Enums.SexType.female);
                    
                    Debug.WriteLine($"GenderCountConverter - Total: {drawables.Count}, Males: {maleCount}, Females: {femaleCount}");
                    
                    if (maleCount > 0 && femaleCount > 0)
                        return $"(M: {maleCount} / F: {femaleCount})";
                    if (maleCount > 0)
                        return $"(M: {maleCount})";
                    if (femaleCount > 0)
                        return $"(F: {femaleCount})";
                    
                    return $"(Count: {drawables.Count})";
                }
                
                return "(M: 0 / F: 0)";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenderCountConverter - Exception: {ex.Message}");
                return "(Error)";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
