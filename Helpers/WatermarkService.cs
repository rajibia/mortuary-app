using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace MortuaryApp.Helpers;

public static class WatermarkService
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(WatermarkService),
            new PropertyMetadata(null, OnTextChanged));

    public static string GetText(TextBox tb) => (string)tb.GetValue(TextProperty);
    public static void SetText(TextBox tb, string value) => tb.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        tb.Loaded += (_, _) =>
        {
            try
            {
                var layer = AdornerLayer.GetAdornerLayer(tb);
                if (layer == null) return;
                var adorner = new WatermarkAdorner(tb, e.NewValue as string);
                layer.Add(adorner);

                tb.TextChanged += (_, _) => adorner.InvalidateVisual();
            }
            catch { }
        };
    }
}

public class WatermarkAdorner : Adorner
{
    private readonly TextBlock _watermark;
    private readonly TextBox _tb;

    public WatermarkAdorner(TextBox tb, string? text) : base(tb)
    {
        _tb = tb;
        _watermark = new TextBlock
        {
            Text = text,
            IsHitTestVisible = false,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            FontSize = tb.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(tb.Padding.Left + 2, 0, 0, 0)
        };

        _watermark.SetBinding(VisibilityProperty,
            new Binding("Text")
            {
                Source = tb,
                Converter = new WatermarkVisibilityConverter(),
                FallbackValue = Visibility.Visible
            });
    }

    protected override Size MeasureOverride(Size constraint)
    {
        _watermark.Measure(constraint);
        return base.MeasureOverride(constraint);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _watermark.Arrange(new Rect(new Point(0, 0), finalSize));
        return base.ArrangeOverride(finalSize);
    }

    protected override Visual GetVisualChild(int index) => _watermark;
    protected override int VisualChildrenCount => 1;
}

public class WatermarkVisibilityConverter : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}
