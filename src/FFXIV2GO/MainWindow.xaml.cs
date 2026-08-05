using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FFXIV2GO.Services;
using FFXIV2GO.ViewModels;
using FFXIV2GO.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Wpf.Ui.Controls;

namespace FFXIV2GO;

public partial class MainWindow : FluentWindow
{
    private TaskbarIcon? _trayIcon;
    private bool _allowExit;
    private bool _closePromptPending;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CreateTrayIcon();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_allowExit)
        {
            _trayIcon?.Dispose();
            return;
        }

        // 窗口未显示或已隐藏（如最小化到托盘期间系统关闭/退出），不弹对话框，直接允许退出。
        if (!IsVisible)
        {
            _allowExit = true;
            return;
        }

        // 已记住关闭行为（“不再询问”），直接按记录的操作执行。
        var config = AppConfig.Load();
        if (!config.AskOnClose)
        {
            if (config.CloseAction == CloseAction.Exit.ToString())
            {
                _allowExit = true;
                _trayIcon?.Dispose();
                return;
            }

            e.Cancel = true;
            Hide();
            return;
        }

        e.Cancel = true;

        // 不能在 OnClosing（WM_CLOSE 处理期间）同步弹模态框：
        // 窗口处于“关闭中”状态，ShowDialog/Close/Owner 会抛异常。
        // 延迟到关闭流程结束后再弹出。
        if (_closePromptPending)
        {
            return;
        }

        _closePromptPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            if (_allowExit || !IsVisible)
            {
                _closePromptPending = false;
                return;
            }

            var dialog = new ClosePromptWindow { Owner = this };
            dialog.ShowDialog();
            _closePromptPending = false;

            switch (dialog.Result)
            {
                case CloseAction.Exit:
                    _allowExit = true;
                    Close();
                    break;
                case CloseAction.MinimizeToTray:
                    Hide();
                    break;
            }
        });
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "FFXIV2GO",
            Visibility = Visibility.Visible
        };

        if (TryLoadAppIcon() is { } appIcon)
        {
            _trayIcon.Icon = appIcon;
        }
        else
        {
            _trayIcon.IconSource = CreateTrayIconSource();
        }

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        var menu = new System.Windows.Controls.ContextMenu();
        var show = new System.Windows.Controls.MenuItem { Header = LocalizationService.Instance["Tray.Show"] };
        show.Click += (_, _) => ShowMainWindow();
        var exit = new System.Windows.Controls.MenuItem { Header = LocalizationService.Instance["Tray.Exit"] };
        exit.Click += (_, _) =>
        {
            _allowExit = true;
            _trayIcon?.Dispose();
            Application.Current.Shutdown();
        };
        menu.Items.Add(show);
        menu.Items.Add(exit);
        _trayIcon.ContextMenu = menu;
    }

    private void ShowMainWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
    }

    /// <summary>从 exe 内嵌图标提取 32px 图标供托盘使用；失败返回 null（回退到代码生成的占位图标）。</summary>
    private static System.Drawing.Icon? TryLoadAppIcon()
    {
        try
        {
            if (Environment.ProcessPath is not { } path ||
                System.Drawing.Icon.ExtractAssociatedIcon(path) is not { } extracted)
            {
                return null;
            }

            return new System.Drawing.Icon(extracted, 32, 32);
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource CreateTrayIconSource()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
        brush.Freeze();
        var glyph = new FormattedText(
            "F",
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            19,
            Brushes.White,
            1.0)
        {
            TextAlignment = TextAlignment.Center
        };

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(brush, null, new Rect(0, 0, 32, 32), 7, 7);
            dc.DrawText(glyph, new Point((32 - glyph.Width) / 2, (32 - glyph.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string key } ||
            DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.Navigate(key);
    }
}
