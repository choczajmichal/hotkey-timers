using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
namespace Timerki;
public sealed class MainWindow : Window
{
    private readonly Settings _settings = Settings.Load();
    private readonly TimerModel[] _timers;
    private readonly MiniWindow _mini;
    private readonly DispatcherTimer _tick;
    private readonly Forms.NotifyIcon _tray;
    private GlobalKeyboard? _hook;
    private readonly TextBlock[] _statuses = new TextBlock[3];
    private readonly TextBox[] _durations = new TextBox[3];
    private readonly Button[] _hotkeyButtons = new Button[3];
    private int _capturingHotkey = -1;
    private readonly TextBlock[] _audioNames = new TextBlock[3];
    private readonly CheckBox _alwaysOnTop = new() { Content = "Always on Top (tylko miniaturki)", Margin = new Thickness(0,5,0,5) };
    private readonly CheckBox _clickThrough = new() { Content = "Przepuszczaj kliknięcia przez miniaturki (Click-through)", Margin = new Thickness(0,5,0,5) };
    private readonly CheckBox _autoStart = new() { Content = "Uruchamiaj z systemem Windows", Margin = new Thickness(0,5,0,5) };
    private bool _closing;
    private bool _settingUp = true;
    public MainWindow()
    {
        Title = "Timerki – 3 niezależne liczniki";
        Width = 710; Height=570; MinWidth=680; MinHeight=530;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(29,34,44));
        Foreground = Brushes.White;
        _timers = Enumerable.Range(0,3).Select(i => new TimerModel(_settings.Durations[i])).ToArray();
        _mini = new MiniWindow(_settings);
        BuildUi();
        _tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "Timerki", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Otwórz ustawienia", null, (_,_) => Dispatcher.Invoke(Restore));
        menu.Items.Add("Zakończ aplikację", null, (_,_) => Dispatcher.Invoke(Exit));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_,_) => Dispatcher.Invoke(Restore);
        try { _hook = new GlobalKeyboard(); _hook.KeyPressed += OnGlobalKey; }
        catch (Exception e) { System.Windows.MessageBox.Show(e.Message,"Globalne skróty",MessageBoxButton.OK,MessageBoxImage.Warning); }
        PreviewKeyDown += CaptureHotkey;
        Deactivated += (_,_) => CancelHotkeyCapture();
        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _tick.Tick += (_,_) => RefreshTimers();
        _tick.Start();
        StateChanged += (_,_) => { if (WindowState == WindowState.Minimized) Hide(); };
        Closing += (_,e) => { if (!_closing) { e.Cancel=true; Hide(); } };
        _settingUp = false;
        RefreshTimers();
    }
    private void BuildUi()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20) };
        var stack = new StackPanel(); scroll.Content = stack; Content = scroll;
        stack.Children.Add(new TextBlock { Text = "TIMERY", FontSize=25, FontWeight=FontWeights.Bold, Margin=new Thickness(0,0,0,5) });
        stack.Children.Add(new TextBlock { Text = "Klawisz uruchamia lub restartuje licznik. Klawisz trafia też normalnie do gry.", Foreground=Brushes.LightGray, Margin=new Thickness(0,0,0,20) });
        for (int i=0;i<3;i++)
        {
            int idx=i;
            var section = new Border { Background = new SolidColorBrush(Color.FromRgb(44,51,65)), CornerRadius=new CornerRadius(8), Padding=new Thickness(12), Margin=new Thickness(0,0,0,12) };
            var content = new StackPanel(); section.Child=content; stack.Children.Add(section);
            var row1 = new DockPanel(); content.Children.Add(row1);
            var name = new TextBlock { Text=$"Timer {i+1}", FontSize=17, FontWeight=FontWeights.Bold, VerticalAlignment=VerticalAlignment.Center };
            DockPanel.SetDock(name,Dock.Left); row1.Children.Add(name);
            _statuses[i] = new TextBlock { Text="Nieaktywny", Foreground=Brushes.LightGreen, HorizontalAlignment=System.Windows.HorizontalAlignment.Right, FontFamily=new FontFamily("Consolas"), FontSize=16 };
            DockPanel.SetDock(_statuses[i],Dock.Right); row1.Children.Add(_statuses[i]);
            var row2 = new WrapPanel { Margin=new Thickness(0,10,0,0), VerticalAlignment=VerticalAlignment.Center }; content.Children.Add(row2);
            row2.Children.Add(Label("Czas (sekundy):"));
            _durations[i] = new TextBox { Text=_settings.Durations[i].ToString(), Width=72, Margin=new Thickness(6,0,20,0), Padding=new Thickness(5) };
            row2.Children.Add(_durations[i]);
            row2.Children.Add(Label("Skrót:"));
            _hotkeyButtons[i] = Button($"{(Key)_settings.Hotkeys[i]}", (_,_) => BeginHotkeyCapture(idx));
            _hotkeyButtons[i].MinWidth = 105;
            _hotkeyButtons[i].Margin = new Thickness(7,0,20,0);
            row2.Children.Add(_hotkeyButtons[i]);
            var start = Button("Start / Reset", (_,_) => Start(idx)); row2.Children.Add(start);
            row2.Children.Add(Button("Stop", (_,_) => { _timers[idx].Stop(); RefreshTimers(); }));
            var row3 = new WrapPanel { Margin = new Thickness(0,10,0,0) }; content.Children.Add(row3);
            row3.Children.Add(Button("Wybierz dźwięk WAV", (_,_) => SelectAudio(idx)));
            _audioNames[i] = new TextBlock { Text=AudioDescription(i), Foreground=Brushes.LightGray, VerticalAlignment=VerticalAlignment.Center, Margin=new Thickness(10,0,0,0), MaxWidth=350, TextTrimming=TextTrimming.CharacterEllipsis };
            row3.Children.Add(_audioNames[i]);
            _durations[i].LostFocus += (_,_) => SaveUi();
        }
        stack.Children.Add(new TextBlock { Text="MINIATUROWE LICZNIKI", FontSize=17, FontWeight=FontWeights.Bold, Margin=new Thickness(0,8,0,9) });
        _alwaysOnTop.IsChecked = _settings.AlwaysOnTop;
        _clickThrough.IsChecked = _settings.ClickThrough;
        _autoStart.IsChecked = _settings.AutoStart;
        stack.Children.Add(_alwaysOnTop); stack.Children.Add(_clickThrough); stack.Children.Add(_autoStart);
        _alwaysOnTop.Checked += (_,_)=>SaveUi(); _alwaysOnTop.Unchecked += (_,_)=>SaveUi();
        _clickThrough.Checked += (_,_)=>SaveUi(); _clickThrough.Unchecked += (_,_)=>SaveUi();
        _autoStart.Checked += (_,_)=>SaveUi(); _autoStart.Unchecked += (_,_)=>SaveUi();
        stack.Children.Add(new TextBlock { Text="Miniaturki można przeciągnąć myszą. Aby przesunąć je po włączeniu Click-through, wyłącz tę opcję na chwilę. Zamknięcie okna (X) ukrywa je do zasobnika.", TextWrapping=TextWrapping.Wrap, Foreground=Brushes.LightGray, Margin=new Thickness(0,12,0,10) });
        stack.Children.Add(Button("Zakończ aplikację", (_,_)=>Exit()));
    }
    private static TextBlock Label(string text) => new() { Text=text, VerticalAlignment=VerticalAlignment.Center };
    private static Button Button(string text, RoutedEventHandler click)
    {
        var b = new Button { Content=text, Margin=new Thickness(0,0,8,0), Padding=new Thickness(9,5,9,5), MinHeight=30 };
        b.Click+=click; return b;
    }
    private string AudioDescription(int idx) => string.IsNullOrWhiteSpace(_settings.AudioFiles[idx]) ? "Domyślnie: komunikat głosowy (Windows SAPI)" : Path.GetFileName(_settings.AudioFiles[idx]);
    private void SelectAudio(int index)
    {
        var dialog = new OpenFileDialog { Title=$"Wybierz dźwięk Timera {index+1}", Filter="Pliki WAV (*.wav)|*.wav" };
        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                _settings.AudioFiles[index] = Settings.ImportAudio(index, dialog.FileName);
                _audioNames[index].Text = AudioDescription(index);
                _settings.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Nie udało się skopiować pliku WAV do folderu Sounds: {ex.Message}", "Timerki", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    private void OnGlobalKey(Key key)
    {
        if (_capturingHotkey >= 0) return;
        for(int i=0;i<3;i++) if ((Key)_settings.Hotkeys[i] == key) { Start(i); break; }
    }
    private void Start(int idx)
    {
        SaveUi();
        _timers[idx].Start(_settings.Durations[idx]);
        RefreshTimers();
    }
    private void BeginHotkeyCapture(int index)
    {
        CancelHotkeyCapture();
        _capturingHotkey = index;
        _hotkeyButtons[index].Content = "Naciśnij klawisz...";
        _hotkeyButtons[index].Focus();
    }
    private void CancelHotkeyCapture()
    {
        if (_capturingHotkey < 0) return;
        int index = _capturingHotkey;
        _capturingHotkey = -1;
        _hotkeyButtons[index].Content = ((Key)_settings.Hotkeys[index]).ToString();
    }
    private void CaptureHotkey(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_capturingHotkey < 0) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { CancelHotkeyCapture(); return; }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;
        // Global hook currently supports single keys; disallow multi-key combinations.
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (key == Key.None || key == Key.Tab) return;
        if (Enumerable.Range(0,3).Any(j => j != _capturingHotkey && _settings.Hotkeys[j] == (int)key))
        {
            System.Windows.MessageBox.Show("Ten klawisz jest już przypisany do innego timera.", "Powtórzony skrót", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        int index = _capturingHotkey;
        _settings.Hotkeys[index] = (int)key;
        CancelHotkeyCapture();
        _settings.Save();
    }
    private void SaveUi()
    {
        if (_settingUp) return;
        for(int i=0;i<3;i++)
        {
            if (!int.TryParse(_durations[i].Text,out int duration)) duration=_settings.Durations[i];
            _settings.Durations[i]=Math.Clamp(duration,1,86400);
            _durations[i].Text=_settings.Durations[i].ToString();

        }
        _settings.AlwaysOnTop=_alwaysOnTop.IsChecked==true;
        _settings.ClickThrough=_clickThrough.IsChecked==true;
        bool startup=_autoStart.IsChecked==true;
        if (startup!=_settings.AutoStart) SetAutoStart(startup);
        _settings.AutoStart=startup;
        _settings.Save();
        _mini.ApplySettings();
    }
    private static void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enabled) key?.SetValue("Timerki", $"\"{Environment.ProcessPath}\"");
            else key?.DeleteValue("Timerki", false);
        }
        catch (Exception e) { System.Windows.MessageBox.Show($"Nie można zmienić autostartu: {e.Message}"); }
    }
    private void RefreshTimers()
    {
        for (int i=0;i<3;i++)
        {
            if (_timers[i].Update()) PlayFinished(i);
            _statuses[i].Text = !_timers[i].Active ? "Nieaktywny" : _timers[i].Done ? "00:00  •  KONIEC" : _timers[i].Display;
        }
        _mini.Update(_timers, (DateTime.UtcNow.Millisecond / 500) == 1);
    }
    private void PlayFinished(int idx)
    {
        var path = Settings.ResolveAudioPath(_settings.AudioFiles[idx]);
        // Run audio separately so that the countdown UI and hook remain responsive.
        _ = Task.Run(() =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    using var player = new SoundPlayer(path);
                    player.PlaySync();
                    return;
                }
                // Built-in Windows COM SAPI; no external speech package required.
                var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (type != null)
                {
                    dynamic? voice = Activator.CreateInstance(type);
                    if (voice != null) { voice.Speak($"Timer {idx+1} zakończony"); System.Runtime.InteropServices.Marshal.FinalReleaseComObject(voice); return; }
                }
            }
            catch { /* sound fallback */ }
            SystemSounds.Exclamation.Play();
        });
    }
    private void Restore()
    {
        Show(); WindowState=WindowState.Normal; Activate();
    }
    private void Exit()
    {
        _closing=true;
        _tick.Stop();
        _hook?.Dispose();
        _tray.Visible=false;
        _tray.Dispose();
        _mini.Close();
        Close();
        System.Windows.Application.Current.Shutdown();
    }
}
