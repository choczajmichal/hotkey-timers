using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;
using Brush = System.Windows.Media.Brush;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Timerki;
public sealed class MiniWindow : Window
{
    private readonly StackPanel _row = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(10,7,10,7) };
    private readonly Border[] _cells = new Border[3];
    private readonly Ellipse[] _dots = new Ellipse[3];
    private readonly TextBlock[] _texts = new TextBlock[3];
    private readonly Settings _settings;
    private bool _repositioned;
    private bool _dragged;
    private bool _dragging;
    private System.Windows.Point _dragStart;
    private double _startLeft, _startTop;
    private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(79, 220, 118));
    private static readonly Brush Red = new SolidColorBrush(Color.FromRgb(247, 73, 73));
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newValue);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    private const int GWL_EXSTYLE = -20, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80;
    private const uint SWP_NOMOVE=0x2, SWP_NOSIZE=0x1, SWP_NOACTIVATE=0x10, SWP_NOZORDER=0x4, SWP_FRAMECHANGED=0x20;
    public MiniWindow(Settings settings)
    {
        _settings = settings;
        Title = "Timerki – mini";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Topmost = settings.AlwaysOnTop;
        var baseBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(235, 25, 30, 40)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(82, 90, 104)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9), Child = _row
        };
        Content = baseBorder;
        for (int i=0;i<3;i++)
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            _dots[i] = new Ellipse { Width = 9, Height = 9, Fill = Green, Margin = new Thickness(0,0,6,0), VerticalAlignment = VerticalAlignment.Center };
            _texts[i] = new TextBlock { Text = "10:00", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.SemiBold, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center };
            line.Children.Add(_dots[i]); line.Children.Add(_texts[i]);
            _cells[i] = new Border { Child = line, Margin = new Thickness(i==0?0:12,0,0,0), Visibility = Visibility.Collapsed };
            _row.Children.Add(_cells[i]);
        }
        Loaded += (_,_) => { if (!_repositioned) { PositionInitial(); _repositioned=true; } ApplySettings(); };
        baseBorder.MouseLeftButtonDown += (s,e) => { if (_settings.ClickThrough) return; _dragStart = PointToScreen(e.GetPosition(this)); _startLeft=Left; _startTop=Top; _dragged=false; _dragging = true; baseBorder.CaptureMouse(); };
        baseBorder.MouseMove += (s,e) =>
        {
            if (!_dragging || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
            var p = PointToScreen(e.GetPosition(this));
            var dx = (p.X-_dragStart.X) / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1);
            var dy = (p.Y-_dragStart.Y) / (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M22 ?? 1);
            if (Math.Abs(dx)+Math.Abs(dy) > 2) _dragged=true;
            Left = _startLeft + dx; Top = _startTop + dy;
        };
        baseBorder.MouseLeftButtonUp += (s,e) =>
        {
            if (!_dragging) return;
            _dragging=false; baseBorder.ReleaseMouseCapture();
            if (_dragged) { _settings.MiniLeft = Left; _settings.MiniTop = Top; _settings.Save(); }
        };
    }
    private void PositionInitial()
    {
        var area = SystemParameters.WorkArea;
        Left = _settings.MiniLeft ?? Math.Max(area.Left, area.Right - ActualWidth - 12);
        Top = _settings.MiniTop ?? Math.Max(area.Top, area.Bottom - ActualHeight - 8);
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right-ActualWidth));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom-ActualHeight));
    }
    public void ApplySettings()
    {
        Topmost = _settings.AlwaysOnTop;
        if (!IsLoaded) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style |= WS_EX_TOOLWINDOW;
        style = _settings.ClickThrough ? (style | WS_EX_TRANSPARENT) : (style & ~WS_EX_TRANSPARENT);
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
        SetWindowPos(hwnd, IntPtr.Zero, 0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
    }
    public void Update(TimerModel[] timers, bool blink)
    {
        bool visible=false;
        for(int i=0;i<3;i++)
        {
            bool active=timers[i].Active;
            _cells[i].Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            if (!active) continue;
            visible=true;
            _texts[i].Text = timers[i].Display;
            _dots[i].Fill = timers[i].Done && blink ? Red : Green;
        }
        if (visible && !IsVisible) Show();
        else if (!visible && IsVisible) Hide();
    }
}
