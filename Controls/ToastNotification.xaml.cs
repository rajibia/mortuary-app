using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MortuaryApp.Controls;

public partial class ToastNotification : UserControl
{
    private readonly Action _onClose;

    public ToastNotification(string message, string icon, Color bg, Action onClose)
    {
        InitializeComponent();
        _onClose = onClose;
        IconText.Text = icon;
        MessageText.Text = message;
        ToastBorder.Background = new SolidColorBrush(bg);
        SlideTransform.X = 420;
        Loaded += (_, _) =>
        {
            var slideIn = new DoubleAnimation(420, 0, TimeSpan.FromMilliseconds(350))
            { DecelerationRatio = 0.9 };
            SlideTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
        };
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) => { timer.Stop(); Close(); };
        timer.Start();
    }

    public void Close()
    {
        var slideOut = new DoubleAnimation(0, 420, TimeSpan.FromMilliseconds(250))
        { AccelerationRatio = 0.9 };
        slideOut.Completed += (_, _) => _onClose();
        SlideTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
    }

    private void Close_Click(object sender, MouseButtonEventArgs e) => Close();
}