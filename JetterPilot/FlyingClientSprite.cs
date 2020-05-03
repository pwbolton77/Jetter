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
    public class FlyingClientSprite : ClientSprite
    {
        public FlyingClientSprite (KeeperTime ktime, IPilotControllerRequest controller_request,
            Vector position, double heading, double speed, double acceleration) :
            base(ktime, controller_request, position, heading, speed, acceleration)
        {
            _state = SpriteStateType.Normal;
        }


        protected const double _MAX_HEADING_CHANGE_PER_SEC = 20.0;
        protected double fuel = 0.0;
        protected double fuel_burn_rate = 1.0;


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
            protected set
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


        #region event helper functions
        /// <summary>
        /// OnRemoved event helper.
        /// </summary>

        protected void OnNowFallingStateChange()
        {
            if (State != SpriteStateType.Falling)
                throw new InvalidOperationException("Sprite State must be set properly prior to calling event handlers.");

            if (NowFallingStateChange != null)
                NowFallingStateChange(this, _prev_state);
        }
        #endregion

        /// <summary>
        /// Return the intercept heading of the target being tracked, limited by the max heading change per second.
        /// </summary>
        /// <param name="ktime"></param>
        /// <returns></returns>
        protected double interceptHeadingLimitedByMaxChange(KeeperTime ktime, 
            Vector target_position, Vector current_position, double max_heading_change_per_sec)
        {
            // Compute the direct heading that would get us straight to the tracked target.
            double new_direct_heading = MathHelper.headingFromVector(target_position - current_position);

            // Calculate the max change we can make based on ellapsed time.
            double max_heading_change = max_heading_change_per_sec * ktime.EllapsedSeconds;

            // Calculate the new heading based on direct heading, limited by the max heading change we can make.
            double old_heading = Heading;
            double new_limited_heading = MathHelper.headingLimitedByMaxChange(new_direct_heading, old_heading, max_heading_change);

            return new_limited_heading;
        }


        #region Target Tracking
        protected RemoveHandler _tracked_target_on_removed_handler_ref = null;
        protected IClientSprite _tracked_target = null;  // Target the missile is tracking.


        /// <summary>
        /// Start tracking the given target.
        /// </summary>
        /// <param name="tracked_target"></param>
        protected void startTrackingTarget(IClientSprite tracked_target)
        {
            // Check if we are already tracking a target.
            if (_tracked_target != null)
                throw new InvalidOperationException("startTrackingTarget: need to end tracking before tracking a new target.");

            if (tracked_target != null)
            {
                _tracked_target = tracked_target;
                _tracked_target_on_removed_handler_ref = new RemoveHandler(tracked_targeted_SpriteRemoveHandler);
                _tracked_target.Removed += _tracked_target_on_removed_handler_ref;
            }
        }

        /// <summary>
        /// End the tracking of the current target.
        /// </summary>
        protected void endTrackingTarget()
        {
            if (_tracked_target != null)
            {
                // Remove our handlers from the sprite we were tarcking.
                _tracked_target.Removed -= _tracked_target_on_removed_handler_ref;
                _tracked_target_on_removed_handler_ref = null;
                _tracked_target = null;
            }
        }

        /// <summary>
        /// Callback handler when a targeted sprite says its being removed from the game.
        /// </summary>
        /// <param name="sender"></param>
        void tracked_targeted_SpriteRemoveHandler(object sender)
        {
            IClientSprite sprite = sender as IClientSprite;
            endTrackingTarget();
        }

        #region Falling Move Logic
        ///////////////////////////////////////////////
        // Falling move logic
        // Number of turns to fall when out of fuel.
        protected const double _FALL_DURATION_SECS = 4.0;
        protected double _fall_seconds_remaining = _FALL_DURATION_SECS;


        /// <summary>
        /// How close to fall completions.
        /// </summary>
        public double FallCountDownPercentComplete
        { get { return (_FALL_DURATION_SECS - _fall_seconds_remaining) / _FALL_DURATION_SECS; } }

        /// <summary>
        /// Handle movement during falling state.
        /// </summary>
        protected virtual void moveDuringFallingState(KeeperTime ktime)
        {
            double old_speed = Speed;
            Speed = old_speed * 0.995;

            NewtonMove(ktime);

            _fall_seconds_remaining -= ktime.EllapsedSeconds;
            if (_fall_seconds_remaining <= 0.0)
            {
                _fall_seconds_remaining = 0.0;
                // If sprite is done falling, remove from the game. 
                State = SpriteStateType.Invalid;
                Remove();
            }
        }
        #endregion


        /// <summary>
        /// Detach our handlers from other objects.
        /// </summary>
        override protected void detachHandlersOnRemove()
        {
            endTrackingTarget();    // Detaches any handlers we have registred on the target we are tracking.
            base.detachHandlersOnRemove();
        }


        #endregion
    }
}
