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
using System.Diagnostics;
using System.Windows.Threading;
using JetterCommon;

namespace JetterPilot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServerProxy _server_proxy = ServerProxy.getInstance();  // Get the server proxy (which is a singleton).
        private PilotStage _pilot_stage = null;         // The stage (i.e. main user control) for the pilot/player.
        private bool _connected_to_server = false;      // True if we are currenly connected to the server.

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += delegate { initialiseGUI(); };
            this.SignInControl.LoginButtonClick += new RoutedEventHandler(SignInControl_LoginButtonClick);
        }

        private PilotController _controller = new PilotController();
        private void initialiseGUI()
        {
            const bool temp_skip_login = true;    // @@@ Temp - skip login while working on radar screen

            if (temp_skip_login)
            {
                this.SignInControl.Visibility = Visibility.Hidden;  // @@ temp
                startGame();
            }
            else
            {
                this.SignInControl.Visibility = Visibility.Visible;
            }
        }


        /// <summary>
        /// Start the game.
        /// </summary>
        private void startGame()
        {
            _pilot_stage = new PilotStage(_controller, _server_proxy);

            _controller.initPilotController();

            placeholder.Content = _pilot_stage;

            // Add an event handler to update canvas just before it is rendered.
            CompositionTarget.Rendering += renderHandler;

            // @@@ Should send a new message to "enable server callbacks" to this client after 
            // @@@ all the events are hooked up.  Then probably send a "syncData" message to the server.
        }

        /// <summary>
        /// Render handler (per frame callback from windows).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void renderHandler(object sender, EventArgs e)
        {
            _controller.renderHandler();    // Let the controller deal with game updates. 

            _pilot_stage.monitorUIFrameRate();   // Just to print out some render frame rates.
        }

        /// <summary>
        /// Uses the data entered on the <see cref="SignInControl">SignInControl</see>
        /// to initialise certain UI elements. And also creates a new 
        /// <see cref="Proxy_Singleton">ProxySingleton</see> and subscribes to its
        /// ProxyEvent/ProxyCallBackEvent events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignInControl_LoginButtonClick(object sender, RoutedEventArgs e)
        {
            //lstChatters.Items.Clear();
            //currPerson = this.SignInControl.CurrentPerson;
            ////connect to proxy, and subscribe to its events
            //ProxySingleton.Connect(currPerson);
            //ProxySingleton.ProxyEvent += new Proxy_Singleton.ProxyEventHandler(ProxySingleton_ProxyEvent);
            ////one event subscription for the embedded ChatControl
            //ProxySingleton.ProxyCallBackEvent += new Proxy_Singleton.ProxyCallBackEventHandler(this.ChatControl.ProxySingleton_ProxyCallBackEvent);
            ////one event subscription for this window
            //ProxySingleton.ProxyCallBackEvent += new Proxy_Singleton.ProxyCallBackEventHandler(this.ProxySingleton_ProxyCallBackEvent);
            ////set the UI elements using signin data
            //this.photoBig.Source = new BitmapImage(new Uri(currPerson.ImageURL));

            // MessageBox.Show(this.SignInControl.LoginName);

            // ProxySingleton.ProxyCallBackEvent += new Proxy_Singleton.ProxyCallBackEventHandler(this.ChatControl.ProxySingleton_ProxyCallBackEvent);

            if (!_connected_to_server)
            {
                _connected_to_server = _server_proxy.Connect(this.SignInControl.LoginName);
                if (_connected_to_server)
                {
                    this.SignInControl.Visibility = Visibility.Hidden;
                    startGame();
                }
                else
                {
                    MessageBox.Show(String.Format("Failed to connect to server with login name: {0}", this.SignInControl.LoginName),
                        "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


            //this.MainBorder.Visibility = Visibility.Visible;
            //this.mnuBorder.Visibility = Visibility.Visible;
            //this.lblInstructions.Content = "You can click on the gridview below to launch a chat window. When the window is opened you may\r\n" +
            //                            "either chat using either the Whisper button which will ONLY chat to the person you selected in\r\n" +
            //                            "the gridview, or you can use the Say button, which will allow ALL connected people to see the message.\r\n";
            //this.txtPerson.Content = "Hello " + currPerson.Name;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

        }
        private void TestMathHelper() 
        {
            double pilot_heading_angle = 60.0;
            double pilot_w_x = 1.0;
            double pilot_w_y = 2.0;
            double bogey_w_x = 3.0;
            double bogey_w_y = 5.0;
            Vector delta = new Vector(bogey_w_x - pilot_w_x, bogey_w_y - pilot_w_y);


            Vector pilot_heading_vec = MathHelper.createVectFromHeading(pilot_heading_angle);

            double x_cross = Vector.CrossProduct(delta, pilot_heading_vec); // Confirmed - The cross product of normalized parallel vectors is 0.0
            double y_dot = Vector.Multiply(pilot_heading_vec, delta);       // Confirmed - The dot product of normalized parallel vectors is 1.0 

            Console.WriteLine("x_cross= {0},  y_dot= {1}", x_cross, y_dot);

            Vector pilot_loc = new Vector(pilot_w_x, pilot_w_y);
            Vector bogey_loc = new Vector(bogey_w_x,bogey_w_y);
            Vector world_delta = MathHelper.rebaseWorldBogeyCoord(pilot_heading_angle, pilot_loc, bogey_loc);


            double boggy_heading_angle = 65.0;
            Vector bogey_heading_vec = MathHelper.rebaseWorldBogeyHeadingVector(pilot_heading_angle, boggy_heading_angle);
        }


        private void TestMathHelper2()
        {
            checkDeltaHeading(0, 10);   // expect -10
            checkDeltaHeading(10, 0);   // expect 10

            checkDeltaHeading(10, 20);  // expect -10
            checkDeltaHeading(30, 20);  // expect 10

            checkDeltaHeading(10, 350);  // expect 20
            checkDeltaHeading(350, 10);  // expect -20

            
            checkDeltaHeading(175, 350);  // expect -175 
            checkDeltaHeading(350, 175);  // expect 175 

            checkDeltaHeading(165, 350);  // expect 175 
            checkDeltaHeading(350, 165);  // expect -175 
        }

        static private void checkDeltaHeading(double newh, double oldh)
        {
            double delta = MathHelper.headingDeltaSigned(newh, oldh);
            Trace.WriteLine(String.Format("newh: {0}  oldh: {1}  delta: {2}", newh, oldh, delta));
        }

        private void TestMathHelper3()
        {
            const double max_heading_change = 19.0;
            computeMaxTurnDeltaHeading(0, 10, max_heading_change);   // expect direct -10
            computeMaxTurnDeltaHeading(10, 0, max_heading_change);   // expect direct 10

            computeMaxTurnDeltaHeading(10, 20, max_heading_change);  // expect direct -10
            computeMaxTurnDeltaHeading(30, 20, max_heading_change);  // expect direct 10

            computeMaxTurnDeltaHeading(10, 350, max_heading_change);  // expect direct 20
            computeMaxTurnDeltaHeading(350, 10, max_heading_change);  // expect direct -20

            
            computeMaxTurnDeltaHeading(175, 350, max_heading_change);  // expect direct -175 
            computeMaxTurnDeltaHeading(350, 175, max_heading_change);  // expect direct 175 

            computeMaxTurnDeltaHeading(165, 350, max_heading_change);  // expect direct 175 
            computeMaxTurnDeltaHeading(350, 165, max_heading_change);  // expect direct -175 
        }

        static private void computeMaxTurnDeltaHeading(double new_direct_heading, double old_heading, 
            double max_heading_change)
        {
            double direct_delta = MathHelper.headingDeltaSigned(new_direct_heading, old_heading);
            double max_delta = Math.Min(max_heading_change, direct_delta); 
            max_delta = Math.Max(-max_heading_change, max_delta);

            double new_limited_heading = MathHelper.normalizeAngle(old_heading + max_delta);

            Trace.WriteLine(String.Format("new_direct_heading: {0}  old_heading: {1}  max_heading_change: {2}", 
                new_direct_heading, old_heading, max_heading_change));

            Trace.WriteLine(String.Format("direct_delta: {0}  max_delta: {1}  new_limited_heading: {2}", 
                direct_delta, max_delta, new_limited_heading));
        }



    }
}
