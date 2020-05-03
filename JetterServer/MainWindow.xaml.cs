using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using System.Configuration;
using System.Diagnostics;

namespace JetterServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceHost host;
        private GameServer _game_server = GameServer.getInstance();
        private StageUserControl _game_stage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Uri uri = new Uri(ConfigurationManager.AppSettings["addr"]);
            host = new ServiceHost(typeof(JetterCommServiceLibrary.JetterCommService), uri);

            host.Open();
            Trace.WriteLine("===========================================");
            Trace.Write("Chat service listen on endpoint: ");
            Trace.WriteLine(uri.ToString());

            _game_stage = new StageUserControl();
            placeholder.Content = _game_stage;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Shutdown the GameServer singleton
            GameServer.stopAndWaitForJoin();

            host.Abort();
            host.Close();
        }



        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_game_stage != null)
            {
                _game_stage.Window_KeyDown(e);
            }

            if (e.Key == Key.Left)
                Trace.WriteLine("Left Key Down");

            if (e.Key == Key.Right)
                Trace.WriteLine("Right Key Down");
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (_game_stage != null)
                _game_stage.Window_KeyUp(e);

            if (e.Key == Key.Left)
                Trace.WriteLine("Left Key Up");

            if (e.Key == Key.Right)
                Trace.WriteLine("Right Key Up");
        }

    }
}
