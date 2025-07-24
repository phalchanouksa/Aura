using System.ComponentModel;
using System.Windows;

namespace Aura
{
    public partial class ControlWindow : Window
    {
        public ControlWindow()
        {
            InitializeComponent();
            this.Closing += ControlWindow_Closing;
        }

        private void ControlWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}