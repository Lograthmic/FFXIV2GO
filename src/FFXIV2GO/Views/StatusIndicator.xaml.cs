using System.Windows;
using System.Windows.Controls;
using FFXIV2GO.Services;

namespace FFXIV2GO.Views;

/// <summary>
/// 状态指示图标：Ok=绿色对号，Fail=红色错号，Neutral=灰色点点点。
/// </summary>
public partial class StatusIndicator : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(StatusState), typeof(StatusIndicator),
            new PropertyMetadata(StatusState.Fail));

    public StatusState Status
    {
        get => (StatusState)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public StatusIndicator()
    {
        InitializeComponent();
    }
}
