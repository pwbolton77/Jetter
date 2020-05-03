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
using System.ComponentModel;
using JetterCommon;

namespace JetterPilot
{
    /// <summary>
    /// Interaction logic for PilotStage.xaml
    /// </summary>
    public partial class PilotStage : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="server_proxy"></param>
        public PilotStage(PilotController controller, ServerProxy server_proxy)
        {
            InitializeComponent();

            _controller = controller;
            _server_proxy = server_proxy;

            _view_translator = new RadarViewTranslator(
                RadarContainer.Width / 2.0, RadarContainer.Height / 2.0,  // radar screen canvas center x, y
                (RadarContainer.Width / 2.0) * 0.97    // radar canvas radial limit - use 97% of radius to allow for a little masking at the edges
                );                     // radar limit in world coord (i.e. kilometers) 


            // Use the view translate defaults for now.
            _pre_pilot_heading = _view_translator.RadarWorldHeading;
            _pre_pilot_visibility_range = _view_translator.RadarPilotVisibilityRange;
            _pre_pilot_radar_range = _view_translator.RadarWorldLimit;

            // Register handlers for Controller callbacks
            _controller.PropertyChanged += Controller_PropertyChangedHandler;
            _controller.AddedSprite += Controller_AddedSpriteHandler;
            _controller.TurnUpdateBegin += Controller_TurnUpdateBeginHandler;
            _controller.TurnUpdateEnd += Controller_TurnUpdateEndHandler;

            AttachServerHandlers();
        }

        private PilotController _controller = null;
        private ServerProxy _server_proxy = null;
        private RadarViewTranslator _view_translator = null;
        private PrimePilotSprite _prime_pilot_sprite = null;    // The prime sprite the point-of-view that this class is displaying

        private double _pre_pilot_heading = 0.0;
        private double _pre_pilot_radar_range = 100.0;
        private double _pre_pilot_visibility_range = 50.0;

        private ulong _frameCounter = 0;                         // Count the number of frames
        private Stopwatch _frame_stopwatch = new Stopwatch();    // Total time we animated

        private List<ClientActor> _bogey_actors = new List<ClientActor>();   // List of sprites for computing next move.

        private ClientActor _tracking_bogey = null;


        /// <summary>
        /// Bogey that is currently being tracked by the prime pilot.
        /// </summary>
        public ClientActor TrackingBogey
        {
            get { return _tracking_bogey; }

            private set
            {
                // If the value is different
                if (value != _tracking_bogey)
                {
                    _tracking_bogey = value;

                    // If prime pilot is valid
                    if (_prime_pilot_sprite != null)
                    {
                        // Tell the prime pilot sprite the new tracking target sprite (even if null).
                        if (_tracking_bogey != null)
                            _prime_pilot_sprite.TrackedTarget = _tracking_bogey.Sprite;
                        else
                            _prime_pilot_sprite.TrackedTarget = null; 
                    }
                }
            }
        }


        #region Dependency properties
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
           "Score", typeof(int), typeof(PilotStage), new PropertyMetadata(0));


        /// <summary>
        /// Heading is the angle (in degrees) the pilot is facing.
        /// </summary>
        public double PilotHeading
        {
            get { return (double)GetValue(PilotHeadingProperty); }
            private set { SetValue(PilotHeadingProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty PilotHeadingProperty = DependencyProperty.Register(
           "PilotHeading", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Heading is the angle (in degrees) the bogey is facing.
        /// </summary>
        public double BogeyHeading
        {
            get { return (double)GetValue(BogeyHeadingProperty); }
            private set { SetValue(BogeyHeadingProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty BogeyHeadingProperty = DependencyProperty.Register(
           "BogeyHeading", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));


        public double BogeyRange
        {
            get { return (double)GetValue(BogeyRangeProperty); }
            set { SetValue(BogeyRangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BogeyInterceptHeading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BogeyRangeProperty = DependencyProperty.Register(
            "BogeyRange", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));


        /// <summary>
        /// Range/distance between pilot and bogey. 
        /// </summary>
        public double BogeyInterceptHeading
        {
            get { return (double)GetValue(BogeyInterceptHeadingProperty); }
            set { SetValue(BogeyInterceptHeadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BogeyInterceptHeading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BogeyInterceptHeadingProperty = DependencyProperty.Register(
            "BogeyInterceptHeading", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));


        /// <summary>
        /// The North marker is part of the pilot radar display, on the outside rim of the radar screen.
        /// (Via XAML this property is bound to a rotation transform on the radar that show a (diamond) 
        /// marker at the correct angle to show the pilot north).
        /// </summary>
        public double NorthMarkerAngle
        {
            get { return (double)GetValue(NorthMarkerAngleProperty); }
            private set { SetValue(NorthMarkerAngleProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty NorthMarkerAngleProperty = DependencyProperty.Register(
           "NorthMarkerAngle", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        #region Map image properties.
        /// <summary>
        /// Property to control the map image xaml scaling. 
        /// </summary>
        public double MapScale
        {
            get { return (double)GetValue(MapScaleProperty); }
            private set { SetValue(MapScaleProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty MapScaleProperty = DependencyProperty.Register(
           "MapScale", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Property to control the map image xaml X translate transform.
        /// </summary>
        public double MapImgTransX
        {
            get { return (double)GetValue(MapImgTransXProperty); }
            private set { SetValue(MapImgTransXProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty MapImgTransXProperty = DependencyProperty.Register(
           "MapImgTransX", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Property to control the map image xaml Y translate transform.
        /// </summary>
        public double MapImgTransY
        {
            get { return (double)GetValue(MapImgTransYProperty); }
            private set { SetValue(MapImgTransYProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty MapImgTransYProperty = DependencyProperty.Register(
           "MapImgTransY", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));
        #endregion


        #region PilotVisibilityMarker
        /// <summary>
        /// Width of the pilot (eye) visibility marker ellipse on the radar screen. 
        /// </summary>
        public double PilotVisMarkerWidth
        {
            get { return (double)GetValue(PilotVisMarkerWidthProperty); }
            private set { SetValue(PilotVisMarkerWidthProperty, value); }
        }

        // Make this a dependent property to support XAML data binding to public properties of this class.
        public static readonly DependencyProperty PilotVisMarkerWidthProperty = DependencyProperty.Register(
           "PilotVisMarkerWidth", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Height of the pilot (eye) visibility marker ellipse on the radar screen. 
        /// </summary>
        public double PilotVisMarkerHeight
        {
            get { return (double)GetValue(PilotVisMarkerHeightProperty); }
            set { SetValue(PilotVisMarkerHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PilotVisMarkerHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PilotVisMarkerHeightProperty =
            DependencyProperty.Register("PilotVisMarkerHeight", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Left coord of the pilot (eye) visibility marker ellipse on the radar screen. 
        /// </summary>
        public double PilotVisMarkerLeft
        {
            get { return (double)GetValue(PilotVisMarkerLeftProperty); }
            set { SetValue(PilotVisMarkerLeftProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PilotVisMarkerLeft.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PilotVisMarkerLeftProperty =
            DependencyProperty.Register("PilotVisMarkerLeft", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));

        /// <summary>
        /// Top coord of the pilot (eye) visibility marker ellipse on the radar screen. 
        /// </summary>
        public double PilotVisMarkerTop
        {
            get { return (double)GetValue(PilotVisMarkerTopProperty); }
            set { SetValue(PilotVisMarkerTopProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PilotVisMarkerTop.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PilotVisMarkerTopProperty =
            DependencyProperty.Register("PilotVisMarkerTop", typeof(double), typeof(PilotStage), new PropertyMetadata(0.0));
        #endregion
        #endregion

        private ServerProxy.ServerRx_ServerStatusMessageEventHandler server_rx_status_msg_handler = null;
        /// <summary>
        /// Attach handlers to the server proxy to support server callbacks. 
        /// </summary>
        void AttachServerHandlers()
        {
            server_rx_status_msg_handler = new ServerProxy.ServerRx_ServerStatusMessageEventHandler(ServerRx_ServerStatusMessageHandler);
            _server_proxy.ServerRx_ServerStatusMessageEvent += server_rx_status_msg_handler;
        }

        /// <summary>
        /// Detach handler from the server proxy (i.e. cleanup when destroying pilot_stage.
        /// </summary>
        public void DetachServerHandlers()
        {
            _server_proxy.ServerRx_ServerStatusMessageEvent -= server_rx_status_msg_handler;
        }

        private const int _MESSAGE_LOG_MAX_SIZE = 50;
        FixedSizedQueue<string> _message_log = new FixedSizedQueue<string>(_MESSAGE_LOG_MAX_SIZE);  // Messages to pilot from server.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ServerRx_ServerStatusMessageHandler(object sender, ServerProxy.ServerRx_ServerStatusMessageArgs e)
        {
            // Trace.WriteLine(String.Format("!!! JetterPilot: PilotStage: {0}", e.message));

            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new ServerProxy.ServerRx_ServerStatusMessageEventHandler(ServerRx_ServerStatusMessageHandler),
                sender, new object[] { e });
                return;
            }

            // Marshalled to the correct thread so proceed
            _message_log.Enqueue(String.Format(DateTime.Now.ToString() + ": Server: " + e.message + Environment.NewLine));
            commMessagesTextBox.Clear();

            foreach (var msg in _message_log)
                commMessagesTextBox.AppendText(msg);
            commMessagesTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Callback handler for when the controller adds a sprited. 
        /// </summary>
        protected void Controller_AddedSpriteHandler(object sender, AddSpriteArgs e)
        {
            if (e.sprite is AresMissileSprite)
            {
                AresMissileSprite sprite = e.sprite as AresMissileSprite;
                // Make a new actor.
                AresMissileActor actor = new AresMissileActor (_view_translator, sprite);
                actor.Moved += Actor_MovedHandler;
                actor.Removed += Actor_RemovedHandler;

                _bogey_actors.Add(actor);                       // Add the actor to the list of bogeys the stage is tracking.

                RadarTargetInsertionCanvas.Children.Add(actor); // Add the actor to the stage.
                actor.Visibility = Visibility.Visible;          // Make the actor visible in the stage.
            }
            else if (e.sprite is BasicBogeySprite)
            {
                BasicBogeySprite sprite = e.sprite as BasicBogeySprite;
                // Make a new actor.
                BasicBogeyActor actor = new BasicBogeyActor(_view_translator, sprite);
                actor.Moved += Actor_MovedHandler;
                actor.Removed += Actor_RemovedHandler;

                _bogey_actors.Add(actor);                       // Add the actor to the list of bogeys the stage is tracking.

                RadarTargetInsertionCanvas.Children.Add(actor); // Add the actor to the stage.
                actor.Visibility = Visibility.Visible;          // Make the actor visible in the stage.
            }
            else if (e.sprite is PrimePilotSprite)
            {
                PrimePilotSprite sprite = e.sprite as PrimePilotSprite;
                _prime_pilot_sprite = sprite;

                _prime_pilot_sprite.DebugCommandsOn = true; // Turn on extended debug commands.

                // Attach observer/callbacks we need from sprite. 
                sprite.Moved += new MovedHandler(PrimePilotSprite_MovedHandler);
                sprite.Removed += new RemoveHandler(PrimePilotSprite_RemovedHandler);
            }
        }



        protected virtual void PrimePilotSprite_MovedHandler(object sender)
        {
            // @@@@ Update what the player sees for position.

            // Note: In regards to the view translator: Instead of updating here we are 
            // using Controller_TurnUpdateBeginHandler to update the _view_translator on 
            // the start of each update loop.
        }

        protected virtual void PrimePilotSprite_RemovedHandler(object sender)
        {
            _prime_pilot_sprite = null; // @@@@@@@@@@
            RadarSweepContainer.Children.Remove(RadarSweepBackingEllipse); // @@@@ Havent tried this yet.
        }

        /// <summary>
        /// Handler when actor is removed due to game logic.
        /// </summary>
        /// <param name="sender"></param>
        protected void Actor_RemovedHandler(object sender)
        {
            if (sender is ClientActor)
            {
                ClientActor actor = sender as ClientActor;

                if (actor == TrackingBogey)
                    TrackingBogey = null;

                _bogey_actors.Remove(actor);                        // Remove the actor from the list of bogeys the stage is tracking.
                RadarTargetInsertionCanvas.Children.Remove(actor);                   // Remove this actor from the parent stage
            }
        }

        /// <summary>
        /// Handler when actor is moved due to game logic.
        /// </summary>
        /// <param name="sender"></param>
        protected void Actor_MovedHandler(object sender)
        {
            if (sender is BasicBogeyActor)
            {
                ClientActor actor = sender as ClientActor;

                // If this actor is the bogey we are tracking ...
                if (actor == TrackingBogey)
                {
                    // If it off the radar scope stop tracking it.
                    if (!actor.IsRadarScopeVisible)
                    {
                        actor.RadarTrackingMarkVisibility = Visibility.Hidden;
                        TrackingBogey = null;

                        BogeyHeading = Double.NaN;
                        BogeyRange = Double.NaN;
                        BogeyInterceptHeading = Double.NaN;
                    }
                    else
                    {   // Update the bogey tracking display
                        IClientSprite sprite = actor.Sprite as IClientSprite;

                        BogeyHeading = sprite.Heading;

                        // Calculate intercept heading from pilot to bogey (by first calculating the delta distance).
                        Vector intercept_delta = Vector.Subtract(sprite.Position,
                            _view_translator.RadarWorldPosition);
                        BogeyRange = intercept_delta.Length;
                        BogeyInterceptHeading = MathHelper.headingFromVector(intercept_delta);
                    }
                }
            }
        }

        /// <summary>
        /// Callback handler for notification that controller turn update is about to begin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Controller_TurnUpdateBeginHandler(object sender, TurnUpdateBeginArgs e)
        {
            if (_prime_pilot_sprite != null)
            {
                // Set the view translator values that affect actors in this stage rendering (as per the prime pilots view state).
                _view_translator.RadarWorldHeading = _prime_pilot_sprite.Heading;
                _view_translator.RadarWorldPosition = _prime_pilot_sprite.Position;
            }

            // Update the radar view translator with new (player based) values.
            _view_translator.RadarWorldLimit = _pre_pilot_radar_range;
            _view_translator.RadarPilotVisibilityRange = _pre_pilot_visibility_range;
        }

        /// <summary>
        /// Callback handler for notification that controller turn update has just ended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Controller_TurnUpdateEndHandler(object sender, TurnUpdateEndArgs e)
        {
            // Update some properties to display in the player status panel.
            PilotHeading = _view_translator.RadarWorldHeading;
            turnNumberValueLabel.Content = e.turn.ToString();

            updateNorthMarker(_view_translator.RadarWorldHeading);
            updatePilotVisibilityMarker(_view_translator.RadarPilotVisibilityRange);

            updateMapImage();
        }


        /// <summary>
        /// Update the map shown underneath the radar.
        /// </summary>
        protected void updateMapImage()
        {
            double world_radar_range = _view_translator.RadarWorldLimit;
            double world_x = _view_translator.RadarWorldX;
            double world_y = _view_translator.RadarWorldY;

            double map_scale = _view_translator.MapWorldSize / (world_radar_range * 2.0);

            double trans_x = -world_x / _view_translator.MapWorldSize;
            double trans_y = world_y / _view_translator.MapWorldSize;

            // Update the related properties. 
            MapScale = map_scale;
            MapImgTransX = trans_x;
            MapImgTransY = trans_y;
        }


        /// <summary>
        /// Callback handler for when properties in the controller change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Controller_PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            PilotController controller = sender as PilotController;

            switch (e.PropertyName)
            {
                default:
                    break;
            }
        }


        /// <summary>
        /// Update the pilot's visibilty marker (ie. what he can see with naked eye).
        /// </summary>
        /// <param name="pilot_world_visibility_range"></param>
        void updatePilotVisibilityMarker(double pilot_world_visibility_range)
        {
            double stage_radius = pilot_world_visibility_range * _view_translator.StageScale;

            // If it too big then hide it.
            if (stage_radius >= _view_translator.StageRadiusLimit * 0.90)
            {
                PilotVisMarkerEllipse.Visibility = Visibility.Hidden;
                stage_radius = 0.0;
            }
            else
            {
                PilotVisMarkerEllipse.Visibility = Visibility.Visible;
            }

            PilotVisMarkerWidth = stage_radius * 2.0;
            PilotVisMarkerHeight = stage_radius * 2.0;
            PilotVisMarkerLeft = -stage_radius;
            PilotVisMarkerTop = -stage_radius;
        }

        /// <summary>
        /// Update the pilot's north marker on the radar screen.
        /// </summary>
        /// <param name="pilot_heading"></param>
        protected void updateNorthMarker(double pilot_heading)
        {
            NorthMarkerAngle = -pilot_heading;
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
        }

        /// <summary>
        /// Monitor and display render frame rates (assumes per frame callback from windows engine).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void monitorUIFrameRate()
        {
            if (_frameCounter++ == 0)
                _frame_stopwatch.Start(); // Starting timing.

            updateFrameRateDisplay(_frame_stopwatch.Elapsed, _frameCounter);
        }


        private void PilotStageUI_Loaded(object sender, RoutedEventArgs e)
        {
            // Make this user control focusable and set the focus here so we can get key up/down events.
            this.Focusable = true;
            this.Focus();
        }

        /// <summary>
        /// Get the preview version of key up events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PilotStageUI_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Trace.WriteLine(String.Format("PilotStageUI_PreviewKeyUp UPPP: {0}", e.Key));
        }

        /// <summary>
        /// Get the preview version of key down events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PilotStageUI_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Trace.WriteLine(String.Format("PilotStageUI_PreviewKeyDown: {0}", e.Key));
            PilotKeyCommands key_cmd = PilotKeyCommands.Invalid;

            switch (e.Key)
            {
                case Key.Left:
                case Key.NumPad4:
                    key_cmd = PilotKeyCommands.RotateCounterClockwiseKeyDown;
                    break;

                case Key.Right:
                case Key.NumPad6:
                    key_cmd = PilotKeyCommands.RotateClockwiseKeyDown;
                    break;

                case Key.Up:
                case Key.NumPad8:
                    break;

                case Key.Down:
                case Key.NumPad2:
                    break;

                case Key.Space:
                case Key.NumPad5:
                    //////////////////////////////////////////////////////
                    // Require control key to be pressed for fire commands.
                    // If control and shift, then fire untracked missile.
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        key_cmd = PilotKeyCommands.FireMissileUnTrackedKeyDown;

                    // If control the fire tracked missile.
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        key_cmd = PilotKeyCommands.FireMissileAtTrackedTargetKeyDown;
                    break;

                case Key.T:
                    key_cmd = PilotKeyCommands.RadarRangeDecreaseKeyDown;
                    break;

                case Key.R:
                    key_cmd = PilotKeyCommands.RadarRangeIncreaseKeyDown;
                    break;

                case Key.Tab:
                    key_cmd = PilotKeyCommands.RadarTrackNextBogeyKeyDown;
                    break;

                default:
                    break;
            }

            // If the key is valid, pass it on ...
            if (key_cmd != PilotKeyCommands.Invalid)
            {
                e.Handled = true;   // swallow the key press if it mapped to a command.
                pilotKeyCommand(key_cmd);
            }

            //if (e.Key == Key.Left)
            //    MessageBox.Show("Left Key Down");

            //if (e.Key == Key.Right)
            //    MessageBox.Show("Right Key Down");

        }

        /// <summary>
        /// Select the next bogey for pilot radar tracking.
        /// </summary>
        private void trackNextRadarBogey()
        {
            ClientActor old_target_bogey = TrackingBogey;
            ClientActor target_bogey = TrackingBogey;

            // Create a list of radar visible bogeys
            List<ClientActor> radar_bogeys = new List<ClientActor>();
            foreach (var actor in _bogey_actors)
            {
                if (actor.IsRadarScopeVisible)
                    radar_bogeys.Add(actor);
            }

            ////////////////////////////////////////
            // Pick the next target bogey 
            if (radar_bogeys.Count == 0)
                target_bogey = null;   // No bogeys, so null target 
            else if (radar_bogeys.Count == 1)
                target_bogey = radar_bogeys[0];    // Only one choice.
            else
            {
                // Find the next furthest based on where the current target distance.

                // Sort the list by distance
                radar_bogeys.Sort((a, b) => a.RadarDistance.CompareTo(b.RadarDistance));

                if (old_target_bogey == null)
                    target_bogey = radar_bogeys[0]; // If we don't have a current target, use the closest.
                else
                {
                    // There are at least 2 qualifing bogeys, and one is likely the  
                    // old target. Find the index of the old target.
                    int old_bogey_index = radar_bogeys.FindIndex(x => x == old_target_bogey);

                    if (old_bogey_index == -1)
                    {
                        // We didnt find the current target, so pick the first.
                        target_bogey = radar_bogeys[0];
                    }
                    else
                    {
                        // Found the current, so move to the next in the list.
                        int new_bogey_index = old_bogey_index + 1;

                        // Wrap to the first if we are at the end.
                        if (new_bogey_index == radar_bogeys.Count)
                            new_bogey_index = 0;

                        target_bogey = radar_bogeys[new_bogey_index];
                    }
                }

            }

            // Set the visibility on the old and new targets.
            if (target_bogey != old_target_bogey)
            {
                if (old_target_bogey != null)
                    old_target_bogey.RadarTrackingMarkVisibility = Visibility.Hidden;

                if (target_bogey != null)
                    target_bogey.RadarTrackingMarkVisibility = Visibility.Visible;
            }

            // Assign the new target to the class data member.
            TrackingBogey = target_bogey;
        }

        /// <summary>
        /// Process the commands that come in key presses.
        /// </summary>
        /// <param name="key_cmd"></param>
        public void pilotKeyCommand(PilotKeyCommands key_cmd)
        {
            bool handled = false;

            // Let the prime pilot sprite have first shot at the command.
            if (_prime_pilot_sprite != null)
            {
                handled = _prime_pilot_sprite.keyCommand(key_cmd);
            }

            if (!handled)
            {
                switch (key_cmd)
                {
                    case PilotKeyCommands.RadarRangeIncreaseKeyDown:
                        _pre_pilot_radar_range = _pre_pilot_radar_range * 1.20;
                        break;
                    case PilotKeyCommands.RadarRangeIncreaseKeyUp:
                        break;

                    case PilotKeyCommands.RadarRangeDecreaseKeyDown:
                        _pre_pilot_radar_range = _pre_pilot_radar_range * 1.0 / 1.20;
                        break;
                    case PilotKeyCommands.RadarRangeDecreaseKeyUp:
                        break;

                    case PilotKeyCommands.RadarTrackNextBogeyKeyDown:
                        trackNextRadarBogey();
                        break;

                    default:
                        break;
                }
            }
        }

    }


}


