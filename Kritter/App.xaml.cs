using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Kritter.Services;
using Kritter.Views;

namespace Kritter;

public partial class App : Application
{
    public static bool IsResumeMode { get; private set; }
    public static string? ResumePackagePath { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HandleResumeArgument(e.Args);

        var splash = new SplashWindow
        {
            Opacity = 0
        };

        splash.Show();
        var splashFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(2));
        splash.BeginAnimation(Window.OpacityProperty, splashFadeIn);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var main = new MainWindow
        {
            Opacity = 0
        };

        MainWindow = main;
        main.Show();

        var mainFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        main.BeginAnimation(Window.OpacityProperty, mainFade);

        splash.Close();
    }

    private static void HandleResumeArgument(string[] args)
    {
        var resumeInfo = ResumeService.GetResumeInfo();
        if (resumeInfo == null)
        {
            return;
        }

        // Fallback: if startup entry is blocked and app is launched manually,
        // continue from saved state anyway.

        IsResumeMode = true;
        ResumePackagePath = resumeInfo.PackagePath;
    }
}
