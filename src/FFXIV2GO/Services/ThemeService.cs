using Wpf.Ui.Appearance;

namespace FFXIV2GO.Services;

/// <summary>
/// 主题切换：跟随系统 / 浅色 / 深色。
/// </summary>
public static class ThemeService
{
    public static void Apply(string theme) => Apply(theme, followSystem: true);

    public static void Apply(string theme, bool followSystem)
    {
        switch (AppConfig.NormalizeTheme(theme))
        {
            case "Light":
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
            case "Dark":
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            default:
                if (followSystem)
                    ApplicationThemeManager.ApplySystemTheme();
                else
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
        }
    }
}
