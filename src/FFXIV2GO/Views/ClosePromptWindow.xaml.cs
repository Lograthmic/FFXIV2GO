using System.Windows;
using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.Views;

public enum CloseAction
{
    Cancel,
    Exit,
    MinimizeToTray
}

public partial class ClosePromptWindow : FluentWindow
{
    public CloseAction Result { get; private set; } = CloseAction.Cancel;

    public ClosePromptWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = CloseAction.Cancel;
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
        => RememberAndClose(CloseAction.Exit);

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => RememberAndClose(CloseAction.MinimizeToTray);

    /// <summary>勾选“不再询问”时记住本次选择，下次关闭主窗口直接按此操作执行。</summary>
    private void RememberAndClose(CloseAction action)
    {
        if (RememberCheckBox.IsChecked == true)
        {
            var config = AppConfig.Load();
            config.AskOnClose = false;
            config.CloseAction = action.ToString();
            config.Save();
        }

        Result = action;
        Close();
    }
}
