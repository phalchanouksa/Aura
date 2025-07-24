using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.IO;
using System.Drawing;

namespace Aura
{
    public partial class App : Application
    {
        private TaskbarIcon tb;
        private ControlWindow controlWindow;
        private MainWindow overlayWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create both windows
            overlayWindow = new MainWindow();
            controlWindow = new ControlWindow();

            // IMPORTANT: Link the ControlWindow's DataContext to the overlay window.
            // This is how the slider will control the overlay's opacity.
            controlWindow.DataContext = overlayWindow;

            // Initialize the TaskbarIcon
            tb = new TaskbarIcon();
            tb.Icon = new Icon(new MemoryStream(Aura.Properties.Resources.icon));
            tb.ToolTipText = "Aura";

            // Show the control panel on left-click
            tb.TrayLeftMouseDown += (s, args) =>
            {
                controlWindow.Show();
                controlWindow.Activate();
            };

            // Create a context menu for the tray icon
            tb.ContextMenu = new System.Windows.Controls.ContextMenu();
            var exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit Aura" };
            exitMenuItem.Click += (s, args) => Shutdown();
            tb.ContextMenu.Items.Add(exitMenuItem);
        }

        protected override void OnExit(ExitEventArgs e)
        {

            // --- Save Settings ---
            Aura.Properties.Settings.Default.IsOverlayVisible = overlayWindow.IsOverlayVisible;
            Aura.Properties.Settings.Default.DimOpacity = overlayWindow.DimOpacity;
            Aura.Properties.Settings.Default.DimColorR = overlayWindow.DimColorR;
            Aura.Properties.Settings.Default.DimColorG = overlayWindow.DimColorG;
            Aura.Properties.Settings.Default.DimColorB = overlayWindow.DimColorB;

            Aura.Properties.Settings.Default.Save();
            tb?.Dispose(); // Clean up the icon when the app closes
            base.OnExit(e);
        }
    }
}