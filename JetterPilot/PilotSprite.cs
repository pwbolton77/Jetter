using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;
using System.Diagnostics;

namespace JetterPilot
{
    public class PilotSprite : ClientSprite
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        /// <param name="acceleration"></param>
        public PilotSprite(KeeperTime ktime, IPilotControllerRequest controller_request, Vector position, double heading,
            double speed = 0.0, double acceleration = 0.0) :
            base(ktime, controller_request, position, heading, speed, acceleration)
        {
            fuel = _STARTING_FUEL;
            fuel_burn_rate = _BURN_RATE_PER_SEC;

            _state = SpriteStateType.Normal;
        }

        private const double _STARTING_FUEL = 1500.0;
        private const double _BURN_RATE_PER_SEC = 1.0;
        private double fuel = _STARTING_FUEL;
        private double fuel_burn_rate = _BURN_RATE_PER_SEC;

        public enum SpriteStateType
        {
            Invalid,
            Normal,
            Falling,
        }

        /// <summary>
        /// Current state of the sprite
        /// </summary>
        public SpriteStateType State
        {
            get { return _state; }
            private set
            {
                if (value != _state)
                {
                    _prev_state = _state;
                    _state = value;
                }
            }
        }
        private SpriteStateType _state = SpriteStateType.Invalid;


        /// <summary>
        /// Previous state of the sprite.
        /// </summary>
        public SpriteStateType PreviousState
        {
            get { return _prev_state; }
        }
        private SpriteStateType _prev_state = SpriteStateType.Invalid;



        // Custom events for this actor type
        public event StateChangeHandler NowFallingStateChange;

        public delegate void StateChangeHandler(object sender, SpriteStateType previous_state);

        /// <summary>
        /// Perform move processing (call once per turn).
        /// </summary>
        public override void Move(KeeperTime ktime)
        {
            switch (State)
            {
                case SpriteStateType.Normal: moveDuringNormalState(ktime); break;
                case SpriteStateType.Falling: moveDuringFallingState(ktime); break;
                default:
                    throw new InvalidOperationException("Unhandled Sprite State: " + State.ToString());
            }

            if (State != SpriteStateType.Invalid)
                OnMoved();
        }

        #region Normal Move Logic
        ///////////////////////////////////////////////
        // Normal Move logic

        /// <summary>
        /// Handle movement during normal state.
        /// </summary>
        private void moveDuringNormalState(KeeperTime ktime)
        {
            NewtonMove(ktime);

            double delta_fuel = ktime.EllapsedSeconds * _BURN_RATE_PER_SEC;
            // Trace.WriteLine(String.Format("ktime.EllapsedSeconds  {0}", ktime.EllapsedSeconds));

            fuel -= delta_fuel;

            if (fuel <= 0.0)
            {
                // If bogey ran out of fuel, start falling
                fuel = 0.0;
                State = SpriteStateType.Falling;
                _fall_seconds_remaining = _FALL_DURATION_SECS;
                Acceleration = 0.0;

                OnNowFallingStateChange();
            }
        }
        #endregion


        #region Falling Move Logic
        ///////////////////////////////////////////////
        // Falling move logic
        // Number of turns to fall when out of fuel.
        private const double _FALL_DURATION_SECS = 4.0;
        private double _fall_seconds_remaining = _FALL_DURATION_SECS;


        /// <summary>
        /// How close to fall completions.
        /// </summary>
        public double FallCountDownPercentComplete
        { get { return (_FALL_DURATION_SECS - _fall_seconds_remaining) / _FALL_DURATION_SECS; } }

        /// <summary>
        /// Handle movement during falling state.
        /// </summary>
        private void moveDuringFallingState(KeeperTime ktime)
        {
            double old_speed = Speed;
            Speed = old_speed * 0.995;

            NewtonMove(ktime);

            _fall_seconds_remaining -= ktime.EllapsedSeconds;
            if (_fall_seconds_remaining <= 0.0)
            {
                _fall_seconds_remaining = 0.0;
                // If bogey is done falling, remove from the game. 
                State = SpriteStateType.Invalid;
                Remove();
            }
        }
        #endregion


        #region event helper functions
        /// <summary>
        /// OnRemoved event helper.
        /// </summary>

        protected void OnNowFallingStateChange()
        {
            if (_state != SpriteStateType.Falling)
                throw new InvalidOperationException("Sprite State must be set properly prior to calling event handlers.");

            if (NowFallingStateChange != null)
                NowFallingStateChange(this, _prev_state);
        }
        #endregion


    }
}
