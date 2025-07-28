using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32; // Add this namespace

namespace Aura
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer timer;
        private IntPtr _windowHandle;
        private bool _isOverlayVisible = false;
        private bool _runOnStartup; // New field for startup setting

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;

        public event PropertyChangedEventHandler PropertyChanged;

        public SolidColorBrush DimBrush { get; set; }

        public bool IsOverlayVisible
        {
            get { return _isOverlayVisible; }
            set
            {
                if (_isOverlayVisible != value)
                {
                    _isOverlayVisible = value;
                    this.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOverlayVisible)));
                }
            }
        }

        // --- New Property for Run on Startup ---
        public bool RunOnStartup
        {
            get { return _runOnStartup; }
            set
            {
                if (_runOnStartup != value)
                {
                    _runOnStartup = value;
                    SetStartup(_runOnStartup); // Call method to update registry
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunOnStartup)));
                }
            }
        }
        // --- End of New Property ---

        // --- Existing Properties for Customization ---
        public double DimOpacity
        {
            get { return DimBrush.Opacity; }
            set
            {
                if (DimBrush.Opacity != value)
                {
                    DimBrush.Opacity = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DimOpacity)));
                }
            }
        }

        public byte DimColorR
        {
            get { return DimBrush.Color.R; }
            set { UpdateDimColor(r: value); }
        }

        public byte DimColorG
        {
            get { return DimBrush.Color.G; }
            set { UpdateDimColor(g: value); }
        }

        public byte DimColorB
        {
            get { return DimBrush.Color.B; }
            set { UpdateDimColor(b: value); }
        }

        private void UpdateDimColor(byte? r = null, byte? g = null, byte? b = null)
        {
            Color newColor = Color.FromRgb(r ?? DimBrush.Color.R, g ?? DimBrush.Color.G, b ?? DimBrush.Color.B);
            if (DimBrush.Color != newColor)
            {
                DimBrush.Color = newColor;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); // Update all color properties
            }
        }
        // --- End of Existing Properties ---

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the brush
            DimBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0)) { Opacity = 0.5 };
            this.Background = DimBrush;

            this.DataContext = this;

            // --- Load Saved Settings ---
            DimOpacity = Aura.Properties.Settings.Default.DimOpacity;
            UpdateDimColor(
                Aura.Properties.Settings.Default.DimColorR,
                Aura.Properties.Settings.Default.DimColorG,
                Aura.Properties.Settings.Default.DimColorB
            );
            // Load RunOnStartup setting
            _runOnStartup = Aura.Properties.Settings.Default.RunOnStartup; // Load into private field first

            IsOverlayVisible = Aura.Properties.Settings.Default.IsOverlayVisible;
            // Must be set last to show the window if it was enabled
            // Redundant: IsOverlayVisible = Properties.Settings.Default.IsOverlayVisible; // This line can be removed

            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
            SetWindowLong(_windowHandle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        // --- New method to manage startup via Registry ---
        private void SetStartup(bool enable)
        {
            // The key for Run programs at startup for the current user
            const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(RunKey, true);

            if (startupKey == null)
            {
                // This should generally not happen, but handle it if the key isn't accessible
                MessageBox.Show("Could not open Windows startup registry key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string appName = "Aura"; // Your application's name
            string appPath = Process.GetCurrentProcess().MainModule.FileName; // Path to your executable

            if (enable)
            {
                // Add the application to startup
                startupKey.SetValue(appName, appPath);
            }
            else
            {
                // Remove the application from startup
                if (startupKey.GetValue(appName) != null)
                {
                    startupKey.DeleteValue(appName);
                }
            }

            startupKey.Close();
        }
        // --- End of new method ---

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsOverlayVisible) return;

            IntPtr activeWindowHandle = Win32Helper.GetForegroundWindow();
            if (activeWindowHandle == IntPtr.Zero || activeWindowHandle == _windowHandle) return;

            // --- NEW: Check for and ignore the Windows shell process ---
            try
            {
                Win32Helper.GetWindowThreadProcessId(activeWindowHandle, out uint processId);
                if (processId > 0)
                {
                    Process p = Process.GetProcessById((int)processId);
                    // If the active window is the shell, a context menu, or the start menu, do nothing.
                    if (p.ProcessName.ToLower() == "explorer")
                    {
                        return;
                    }
                }
            }
            catch
            {
                // Ignore errors getting process info, just proceed as normal.
            }

            // --- Z-Order logic now only runs for application windows ---
            Win32Helper.SetWindowPos(activeWindowHandle, Win32Helper.HWND_TOP, 0, 0, 0, 0, Win32Helper.SWP_NOMOVE | Win32Helper.SWP_NOSIZE);
            Win32Helper.SetWindowPos(_windowHandle, activeWindowHandle, 0, 0, 0, 0, Win32Helper.SWP_NOMOVE | Win32Helper.SWP_NOSIZE);
        }
    }
}