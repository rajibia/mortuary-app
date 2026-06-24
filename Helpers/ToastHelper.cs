using System.Windows.Media;
using MortuaryApp.Controls;

namespace MortuaryApp.Helpers;

public static class ToastHelper
{
    public static void ShowToast(this System.Windows.Window window, string message, string icon, Color bg)
    {
        if (window is MainWindow main)
        {
            main.ShowToast(message, icon, bg);
        }
    }

    public static void ShowSuccess(string message) => ShowOnMain("✓ " + message, "✅", Color.FromRgb(22, 163, 74));
    public static void ShowError(string message) => ShowOnMain("✕ " + message, "❌", Color.FromRgb(220, 38, 38));
    public static void ShowWarning(string message) => ShowOnMain("⚠ " + message, "⚠️", Color.FromRgb(217, 119, 6));
    public static void ShowInfo(string message) => ShowOnMain("ℹ " + message, "ℹ️", Color.FromRgb(37, 99, 235));

    private static void ShowOnMain(string message, string icon, Color bg)
    {
        var main = System.Windows.Application.Current.MainWindow as MainWindow;
        main?.ShowToast(message, icon, bg);
    }
}