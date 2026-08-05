using System.Windows;
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
    {
        Result = CloseAction.Exit;
        Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        Result = CloseAction.MinimizeToTray;
        Close();
    }
}
