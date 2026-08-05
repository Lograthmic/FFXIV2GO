using System.Windows;
using FFXIV2GO.Services;
using FFXIV2GO.ViewModels;
using Wpf.Ui.Controls;

namespace FFXIV2GO.Views;

public partial class AppSelectWindow : FluentWindow
{
    private readonly List<string> _atLeastOne;
    private readonly List<AppSelectItem> _items;

    public IReadOnlyList<AppEntry> Selected { get; private set; } = [];

    public AppSelectWindow(AppManifest manifest)
    {
        InitializeComponent();

        _atLeastOne = manifest.AtLeastOne ?? [];
        _items = manifest.Apps.Select(a => new AppSelectItem(a)).ToList();
        AppList.ItemsSource = _items;

        RequiredHint.Text = _atLeastOne.Count > 0
            ? string.Format(LocalizationService.Instance["AppSelect.RequiredHint"], string.Join(", ", _atLeastOne))
            : string.Empty;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        foreach (var group in _atLeastOne)
        {
            if (!_items.Any(i => string.Equals(i.Entry.Group, group, StringComparison.Ordinal) && i.IsSelected))
            {
                System.Windows.MessageBox.Show(
                    string.Format(LocalizationService.Instance["AppSelect.RequiredMessage"], group),
                    "FFXIV2GO", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
        }

        Selected = _items.Where(i => i.IsSelected).Select(i => i.Entry).ToList();
        DialogResult = true;
    }
}
