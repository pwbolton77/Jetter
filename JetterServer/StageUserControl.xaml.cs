using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.ComponentModel;
using JetterCommServiceLibrary;
using JetterCommon;

namespace JetterServer
{
    /// <summary>
    /// Interaction logic for StageUserControl.xaml
    /// </summary>
    public partial class StageUserControl : UserControl
    {
        public StageUserControl()
        {
            InitializeComponent();
            _comm_manager = JetterCommManager.getInstance();

            _controller = new Controller(GameStageCanvas, _comm_manager);
            _controller.PropertyChanged += ControllerPropertyChangedHandler;

            // Add an event handler to update canvas just before it is rendered.
            CompositionTarget.Rendering += renderHandler;

        }

        private long _turn_number = 0;

        private ulong _frameCounter = 0;                         // Count the number of frames
        private Stopwatch _frame_stopwatch = new Stopwatch();    // Total time we animated
        private GameTimeKeeper _time_keeper = new GameTimeKeeper(60 /* 20 turns per second equals 50 millsec turns */);
        public Controller _controller = null;
        private JetterCommManager _comm_manager = null; 

        private GameServer _temp_game_server = GameServer.getInstance(); // Temp code to play with GameServer concept in its own thread. 

        /// <summary>
        /// True if player has colliding with something (evaluated per turn).
        /// </summary>
        public bool IsPlayerColliding
        {
            get { return (bool)GetValue(IsPlayerCollidingProperty); }
            private set { SetValue(IsPlayerCollidingProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty IsPlayerCollidingProperty = DependencyProperty.Register(
           "IsPlayerColliding",
           typeof(bool),
           typeof(StageUserControl),
           new PropertyMetadata(false));


        /// <summary>
        /// Count of times (no more than one per turn) the player has collided with one or more objects. 
        /// This is the count of the number of transitions from 'non collide' to 'collide', but is 
        /// evaluated once per turn, and incremented base on the transition (not on number of objects it hit).
        /// </summary>
        public int PlayerCollisionTurnCount
        {
            get { return (int)GetValue(PlayerCollisionTurnCountProperty); }
            private set { SetValue(PlayerCollisionTurnCountProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty PlayerCollisionTurnCountProperty = DependencyProperty.Register(
           "PlayerCollisionTurnCount",
           typeof(int),
           typeof(StageUserControl),
           new PropertyMetadata(0));

        /// <summary>
        /// Heading is the angle (in degrees) the player ship is facing.
        /// </summary>
        public double PlayerShipHeading
        {
            get { return (double)GetValue(PlayerShipHeadingProperty); }
            private set { SetValue(PlayerShipHeadingProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty PlayerShipHeadingProperty = DependencyProperty.Register(
           "PlayerShipHeading",
           typeof(double),
           typeof(StageUserControl),
           new PropertyMetadata(0.0));

        /// <summary>
        /// The current score.
        /// </summary>
        public int Score
        {
            get { return (int)GetValue(ScoreProperty); }
            private set { SetValue(ScoreProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty ScoreProperty = DependencyProperty.Register(
           "Score",
           typeof(int),
           typeof(StageUserControl),
           new PropertyMetadata(0));


        /// <summary>
        /// Callback handler for when properties in the controller change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ControllerPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            //Controller controller = sender as Controller;

            //switch (e.PropertyName)
            //{
            //   case "IsPlayerColliding": IsPlayerColliding = controller.IsPlayerColliding; break;
            //   case "PlayerCollisionTurnCount": PlayerCollisionTurnCount = controller.PlayerCollisionTurnCount; break;
            //   case "PlayerShipHeading": PlayerShipHeading = controller.PlayerShipHeading ; break;
            //   case "Score": Score = controller.Score; break;
            //   default: break;
            //}
        }


        /// <summary>
        /// Update the frame rate display.
        /// </summary>
        /// <param name="frame_time_ellapsed"></param>
        /// <param name="frame_counter"></param>
        void updateFrameRateDisplay(TimeSpan frame_time_ellapsed, ulong frame_counter)
        {
            // Determine frame rate in fps (frames per second). 
            long frameRate = (long)(frame_counter / frame_time_ellapsed.TotalSeconds);
            if (frameRate > 0)
            {
                // Update elapsed time, number of frames, and frame rate.
                ellapsedTimeValueLabel.Content = _frame_stopwatch.Elapsed.ToString();
                frameCounterValueLabel.Content = _frameCounter.ToString();
                frameRateValueLabel.Content = frameRate.ToString();
            }
            turnNumberValueLabel.Content = _turn_number.ToString();

        }

        /// <summary>
        /// Render handler (per frame callback from windows).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void renderHandler(object sender, EventArgs e)
        {
            if (_frameCounter++ == 0)
                _frame_stopwatch.Start(); // Starting timing.

            updateFrameRateDisplay(_frame_stopwatch.Elapsed, _frameCounter);

            gameLoop();
        }


        /// <summary>
        /// Process "key down" events that the Window recieved.
        /// </summary>
        /// <param name="e"></param>
        public void Window_KeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.NumPad4:
                    break;

                case Key.Right:
                case Key.NumPad6:
                    break;

                case Key.Up:
                case Key.NumPad8:
                    break;

                case Key.Down:
                case Key.NumPad2:
                    break;

                case Key.Space:
                case Key.NumPad5:
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Process "key up" events that the Window recieved.
        /// </summary>
        /// <param name="e"></param>
        public void Window_KeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    break;

                case Key.Left:
                case Key.NumPad4:
                    break;

                case Key.Right:
                case Key.NumPad6:
                    break;

                case Key.Up:
                case Key.NumPad8:
                    break;

                case Key.Down:
                case Key.NumPad2:
                    break;

                case Key.Space:
                case Key.NumPad5:
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// The game update loop.
        /// </summary>
        void gameLoop()
        {
            tempCheckGameServerUpdate(); // Temp code to play with GameServer concept in its own thread. 

            bool new_turn = _time_keeper.computeTurnCount(out _turn_number);

            if (new_turn)
            {
               ///////////////////////////////////////
               // Update for the game logic
               _controller.UpdateTurn();
            }
        }

        /// <summary>
        /// Temp code to check for game server updates.  This is temp code to play with GameServer concept in its own thread. 
        /// </summary>
        void tempCheckGameServerUpdate()
        {
            // See if there is a new message from the game server.
            GameServer.ServerMessage server_msg = null;
            if (_temp_game_server.getMessage(ref server_msg))
                serverMessageValueLabel.Content = server_msg.Message;
        }

    }
}
