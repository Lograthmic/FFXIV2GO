using CommunityToolkit.Mvvm.ComponentModel;

namespace FFXIV2GO.Services;

/// <summary>
/// 本地化感知 VM 基类：界面语言切换时自动触发全局属性刷新。
/// </summary>
public abstract class LocalizedViewModel : ObservableObject
{
    protected LocalizedViewModel()
    {
        LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(string.Empty);
    }
}
