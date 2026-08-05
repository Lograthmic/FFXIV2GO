using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace FFXIV2GO.Services;

/// <summary>
/// 多语言服务：RESX 资源（Strings.resx 英文默认 + Strings.zh-CN.resx）。
/// 支持界面用 {Binding Source={x:Static loc:LocalizationService.Instance}, Path=[Key]} 绑定。
/// 切换语言后触发全局刷新。
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private readonly ResourceManager _resources =
        new("FFXIV2GO.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.InvariantCulture;

    public string CurrentLanguage { get; private set; } = "System";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetLanguage(string language)
    {
        CurrentLanguage = AppConfig.NormalizeLanguage(language);
        _culture = new CultureInfo(AppConfig.ResolveCulture(CurrentLanguage));
        CultureInfo.CurrentUICulture = _culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public string this[string key]
    {
        get
        {
            var value = _resources.GetString(key, _culture);
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }

    public string Get(string key) => this[key];
}
