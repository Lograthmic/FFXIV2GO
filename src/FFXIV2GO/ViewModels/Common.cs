using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.ViewModels;

public sealed class NavItem : LocalizedViewModel
{
    public required string Key { get; init; }
    public required string TitleKey { get; init; }
    public required SymbolRegular Symbol { get; init; }
    public string Title => LocalizationService.Instance[TitleKey];
}

public sealed class DashboardCard : LocalizedViewModel
{
    public required string TitleKey { get; init; }
    public required string DescKey { get; init; }
    public required SymbolRegular Symbol { get; init; }
    public required string TargetKey { get; init; }

    /// <summary>标题右侧是否显示状态指示图标。</summary>
    public bool ShowStatus { get; init; }

    /// <summary>状态指示三态（Ok=绿对号，Fail=红错号，Neutral=灰点点）。</summary>
    public StatusState Status { get; init; } = StatusState.Fail;

    public string Title => LocalizationService.Instance[TitleKey];
    public string Desc => LocalizationService.Instance[DescKey];
}

public sealed class OptionItem : LocalizedViewModel
{
    public required string Value { get; init; }
    public required string LabelKey { get; init; }
    public string Label => LocalizationService.Instance[LabelKey];
}

public sealed class AppSelectItem
{
    public AppEntry Entry { get; }
    public string Name => Entry.Name;
    public bool IsSelected { get; set; }

    public AppSelectItem(AppEntry entry)
    {
        Entry = entry;
    }
}

/// <summary>设置页自动启动应用项。</summary>
public sealed class AutoLaunchAppItem
{
    public required string Name { get; init; }
    public bool IsSelected { get; set; }
}
