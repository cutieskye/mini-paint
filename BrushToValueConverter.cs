using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mini_Paint
{
    class BrushToValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var comboBox = values[0] as ComboBox;
            var solidColorBrush = values[1] as SolidColorBrush;
            if (!(comboBox == null || solidColorBrush == null))
                foreach (TextBlock textBlock in comboBox.Items)
                    if (((SolidColorBrush)textBlock.Background).Color == solidColorBrush.Color)
                    {
                        comboBox.SelectedItem = textBlock;
                        return textBlock;
                    }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
