using System.Windows;
using FFXIV2GO.ViewModels;
using Wpf.Ui.Controls;

namespace FFXIV2GO.Views;

public partial class FirstRunWindow : FluentWindow
{
    public string SelectedLanguage { get; private set; } = "System";

    public FirstRunWindow()
    {
        InitializeComponent();

        LanguageCombo.ItemsSource = new[]
        {
            new OptionItem { Value = "System", LabelKey = "Settings.Language.System" },
            new OptionItem { Value = "zh-CN", LabelKey = "Settings.Language.zh-CN" },
            new OptionItem { Value = "en", LabelKey = "Settings.Language.en" }
        };
        LanguageCombo.DisplayMemberPath = "Label";
        LanguageCombo.SelectedValuePath = "Value";
        LanguageCombo.SelectedValue = "System";
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = LanguageCombo.SelectedValue as string ?? "System";
        DialogResult = true;
    }
}
