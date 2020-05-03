using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; //for Dispatcher
using JetterCommServiceLibrary;
using System.Diagnostics;
using JetterCommon;

namespace JetterServer
{
    /// <summary>
    /// Top level game crontroller class that puts game elements together and runs the main update loop.
    /// </summary>
    public class Controller : INotifyPropertyChanged, IControllerCallback
    {
        private System.Random _randNum = new System.Random();

        private bool _running = true;      // True if the game is running/playing.

        private JetterCommManager _comm_manager = null;
        private Dictionary<string /* pilot name */, PilotControllerInfo> pilot_dictionary = new Dictionary<string, PilotControllerInfo>();

        private Canvas _game_stage;         // The main game stage.
        private Rect _game_stage_limits;    // The x,y and width, height rectangle limits of the game stage.
        private List<ISprite> _active_sprites = new List<ISprite>();   // List of sprites for computing next move.

        /// <summary>
        /// Public event notification when controller properties change.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Method to support PropertyChanged events.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void onPropertyChanged(string propertyName)
        {
            // Callback to clients that registered event handlers for property changes.
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// The turn number tracked by the Controller update method.
        /// </summary>
        private long _turn_number = 0;
        public long TurnNumber
        {
            get { return _turn_number; }
            set
            {
                if (_turn_number != value)
                {
                    _turn_number = value;
                    onPropertyChanged("TurnNumber"); // Call event handlers because property changed.
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game_stage"></param>
        public Controller(Canvas game_stage, JetterCommManager comm_manager)
        {
            _game_stage = game_stage;
            _comm_manager = comm_manager;

            // Setup so the comm manager callback is to the Controller.
            _comm_manager.setControllerCallback(this /* IControllerCallback */);

            _game_stage_limits = new Rect(0, 0, _game_stage.Width, _game_stage.Height);
        }


        /// <summary>
        /// Update the game when it is next turn (based on timer ticks).
        /// </summary>
        /// <param name="turn_number"></param>
        public void UpdateTurn()
        {
            if (_running)
            {
                ++_turn_number;

                MoveActiveSprites();

                if ((_turn_number % 100) == 0)  // @@@ temp
                {
                    string msg = "Making message for turn: " + _turn_number.ToString();
                    Trace.WriteLine(msg);
                    _comm_manager.BroadcastServersStatusMessage(msg);
                }

                //MoveStandardSprites(); // Move all standard sprites and put them in the new grid-cell locations.

                //ProcessSpriteIntersections(); // Process all sprites in the _game_grid (for collisions).
                //ProcessNonCollisions();       // Process any sprites that need handling if they did not collide.

                /////////////////////////////////////////////////////
                //// Update some properties to display in the player status panel.
                //PlayerJetHeading = _player_ship_status.Heading;

                //if (IsPlayerColliding)
                //{
                //    // If the player did not collide with anything this turn, then
                //    // the player is no longer 'colliding'.
                //    if (!_player_ship_status.HasCollided)
                //        IsPlayerColliding = false;
                //}
                //else
                //{
                //    // If the player collided with at least one thing this turn,
                //    // then the player is now 'colliding'.  So count how many time
                //    // the player entered the 'colliding' state.
                //    if (_player_ship_status.HasCollided)
                //    {
                //        IsPlayerColliding = true;
                //        ++PlayerCollisionTurnCount;
                //    }
                //}
            }
        }


        /// <summary>
        /// Move all standard sprites and put them in the new grid-cell locations.
        /// </summary>
        public void MoveActiveSprites()
        {
            foreach (Sprite sprite in _active_sprites)
            {
                if (sprite.IsMoving)
                {
                    sprite.Move();
                }
            }
        }


        #region JetterCommManager.IControllerCallback
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Callback Dispatch Comments: PWB: Callbacks to the controller from the JetterCommManager MUST be delgated to the WPF thread
        // that handles the UI for the _game_stage - otherwise the WPF game_stage objects are not accessable 
        // the program will crash.  So we use a pattern that bundles up the arguments and has 
        // the WPF engine call a "dispatch method" with the arguments on the UI thread.
        // MSDN BeginInvoke exanple info: http://msdn.microsoft.com/en-us/library/ms741870.aspx
        // Also intresting: http://stackoverflow.com/questions/1207832/wpf-dispatcher-begininvoke-and-ui-background-threads 


        ///////////////////////////////////////////////////////////////////////
        public void PilotRequest(string from_pilot, PilotCommand pilot_command)
        {
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.

            // Bundle up the arguments.
            PilotRequest_DispatchDelegateArgs args = new PilotRequest_DispatchDelegateArgs(from_pilot, pilot_command);

            // Delegate the work such that the UI thread of the game_stage calls the XXX_Dispatch() method
            // to do the actual work.
            _game_stage.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new PilotRequest_DispatchDelegate(PilotRequest_Dispatch),
                this, new object[] { args });
        }

        private class PilotRequest_DispatchDelegateArgs : EventArgs
        {
            public PilotRequest_DispatchDelegateArgs(string from_pilot_, PilotCommand pilot_command_)
            {
                from_pilot = from_pilot_;
                pilot_command = pilot_command_;
            }
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.
            public string from_pilot;
            public PilotCommand pilot_command;
        }
        private delegate void PilotRequest_DispatchDelegate(object sender, PilotRequest_DispatchDelegateArgs e);

        private void PilotRequest_Dispatch(object sender, PilotRequest_DispatchDelegateArgs args)
        {
            string trace_msg = "Pilot request: from: " + args.from_pilot + "  cmd: " + args.pilot_command.command.ToString();
            Trace.WriteLine(trace_msg);

            PilotControllerInfo pilot_info = null;
            pilot_dictionary.TryGetValue(args.from_pilot, out pilot_info);
            if (pilot_info != null)
            {
                pilot_info.actor.Sprite.PilotRequest(args.pilot_command);
            }
            else
            {
                string msg = "!!! Controller could not find pilot: " + args.from_pilot;
                Trace.WriteLine(msg);
            }
        }

        ///////////////////////////////////////////////////////////////////////
        public void Join(string pilot)
        {
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.

            // Bundle up the arguments.
            JoinRequest_DispatchDelegateArgs args = new JoinRequest_DispatchDelegateArgs(pilot);

            // Delegate the work such that the UI thread of the game_stage calls the XXX_Dispatch() method
            // to do the actual work.
            _game_stage.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new JoinRequest_DispatchDelegate(JoinRequest_Dispatch),
                this, new object[] { args });
        }

        private class JoinRequest_DispatchDelegateArgs : EventArgs
        {
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.
            public JoinRequest_DispatchDelegateArgs(string pilot_)
            {
                pilot = pilot_;
            }
            public string pilot;
        }
        private delegate void JoinRequest_DispatchDelegate(object sender, JoinRequest_DispatchDelegateArgs e);

        private void JoinRequest_Dispatch(object sender, JoinRequest_DispatchDelegateArgs args)
        {
            string trace_msg = "Join request: from: " + args.pilot;
            Trace.WriteLine(trace_msg);
            AddPilot(args.pilot);
        }

        ///////////////////////////////////////////////////////////////////////
        public void Leave(string pilot)
        {
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.

            // Bundle up the arguments.
            LeaveRequest_DispatchDelegateArgs args = new LeaveRequest_DispatchDelegateArgs(pilot);

            // Delegate the work such that the UI thread of the game_stage calls the XXX_Dispatch() method
            // to do the actual work.
            _game_stage.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new LeaveRequest_DispatchDelegate(LeaveRequest_Dispatch),
                this, new object[] { args });
        }

        private class LeaveRequest_DispatchDelegateArgs : EventArgs
        {
            public LeaveRequest_DispatchDelegateArgs(string pilot_)
            {
                pilot = pilot_;
            }
            // NOTE: See "Callback Dispatch Comments" for explanation of the dispatch pattern and why it is needed.
            public string pilot;
        }
        private delegate void LeaveRequest_DispatchDelegate(object sender, LeaveRequest_DispatchDelegateArgs e);

        private void LeaveRequest_Dispatch(object sender, LeaveRequest_DispatchDelegateArgs args)
        {
            string trace_msg = "Leave request: from: " + args.pilot;
            Trace.WriteLine(trace_msg);

            PilotControllerInfo pilot_info = null;
            pilot_dictionary.TryGetValue(args.pilot, out pilot_info);
            if (pilot_info != null)
            {
                string msg = "Controller removing pilot: " + args.pilot;
                Trace.WriteLine(msg);

                PlayerJetRemoveFromStage(pilot_info.actor); // Remove from stage (and sprite update loop).
                pilot_dictionary.Remove(args.pilot);  // Removed from pilot dictionary.
            }
            else
            {
                string msg = "!!! Controller could not find pilot: " + args.pilot;
                Trace.WriteLine(msg);
            }
        }

        private class PilotControllerInfo
        {
            public PilotControllerInfo(PlayerJetActor player_jet_actor)
            {
                actor = player_jet_actor;
            }
            public PlayerJetActor actor;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool AddPilot(string pilot_name)
        {
            bool pilot_added = false;
            if (pilot_dictionary.ContainsKey(pilot_name))
            {
                string msg = "!!! Controller already has pilot: " + pilot_name;
                Trace.WriteLine(msg);
            }
            else
            {
                // Add pilot
                string msg = "Controller adding pilot: " + pilot_name;
                Trace.WriteLine(msg);

                int player_number = pilot_dictionary.Count;
                PlayerJetActor player_jet = PlayerJetAddToStage(pilot_name, player_number);
                PilotControllerInfo pilot_info = new PilotControllerInfo(player_jet);

                // Add pilot to dictionary of pilots.
                pilot_dictionary.Add(pilot_name, pilot_info);
            }
            return pilot_added;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pilot_name"></param>
        /// <returns></returns>
        PlayerJetActor PlayerJetAddToStage(string pilot_name, int player_number)
        {
            return PlayerJetAddToStage(pilot_name, player_number, new Vector (_game_stage_limits.Width/2, _game_stage_limits.Height/2), 
                 MathHelper.gravity_direction, 0.0 /* speed */, _game_stage_limits);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pilot_name"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        /// <param name="game_stage_limits"></param>
        /// <returns></returns>
        PlayerJetActor PlayerJetAddToStage(string pilot_name, int player_number, Vector position, Vector direction, double speed, Rect game_stage_limits)
        {
            //////////////////////////////////
            // Make a new sprite.
            PlayerJetSprite sprite = new PlayerJetSprite(position, direction, speed);
            sprite.Reset();                                 // Get the sprite in a default/known state.

            // Setup callbackks used for things like removal and scoring.
            sprite.Removed += new RemoveHandler(SpriteRemoveHandler);
            sprite.CalmCollision += new CollisionHandler(PilotCalmCollisionScoring);
            sprite.SpinningCollision += new CollisionHandler(PilotSpinningCollisionScoring);
            sprite.PulsingCollision += new CollisionHandler(PilotPulsingCollisionScoring);


            // Setup the ship event handling (e.g. for reporting ship status to player). 
            sprite.Collision += new CollisionHandler(PilotCollidedTracking);         // Calls PilotCollidedTracking method when ship collides.
            sprite.NonCollision += new CollisionHandler(PilotNonCollisionTracking);  // Calls PlayerNonCollisionTracking method (once per turn) when ship does NOT collide.


            //////////////////////////////////
            // Make a new actor.
            PlayerJetActor actor = new PlayerJetActor(sprite, player_number);   // Make a new actor and give it a reference to its sprite. 
            actor.Visibility = Visibility.Hidden;           
            _game_stage.Children.Add(actor);                // Add the actor to the stage.

            sprite.IsMoving = true;
            sprite.Position = position;                     // Set the position, direction, speed, etc.
            sprite.Direction = direction;
            sprite.Speed = speed;
            _active_sprites.Add(sprite);                    // Add the sprite to the list of moving sprites.
            sprite.Move();
            actor.Visibility = Visibility.Visible;          // Make the actor visible in the stage again.


            actor.Sprite.setBounceLimits(_game_stage_limits);
            return actor;
        }


        bool PlayerJetRemoveFromStage(PlayerJetActor player_jet_actor)
        {
            /// @@@ Pwb: requires more thought.  !! The sprite Remove event Handler will not be called.
            /// Most sprites will "die" due to the update loop.
            _active_sprites.Remove(player_jet_actor.Sprite);           // Add the sprite to the list of moving sprites.
            _game_stage.Children.Remove(player_jet_actor);

            bool removed = false;
            return removed;
        }

        #endregion
        void PilotCollidedTracking(object sender, ISprite sprite)
        {
            // ++Score;
        }

        void PilotNonCollisionTracking(object sender, ISprite _)
        {
        }

        void SpriteRemoveHandler(object sender)
        {
            ISprite sprite = sender as Sprite;
            _active_sprites.Remove(sprite);  // Remove the sprite from the list of moving sprites.
        }

        void PilotCalmCollisionScoring(object sender, ISprite sprite)
        {
            // ++Score;
        }

        void PilotSpinningCollisionScoring(object sender, ISprite sprite)
        {
            // ++Score;
        }

        void PilotPulsingCollisionScoring(object sender, ISprite sprite)
        {
            // ++Score;
        }

    }
}
