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
    public class BasicBogeySprite : FlyingClientSprite
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        /// <param name="acceleration"></param>
        public BasicBogeySprite(KeeperTime ktime, IPilotControllerRequest controller_request, Vector position, double heading,
            double speed, double acceleration = 0.0, IClientSprite tracked_target = null) :
            base(ktime, controller_request, position, heading, speed, acceleration)
        {
            fuel = _BASIC_BOGEY_STARTING_FUEL;
            fuel_burn_rate = _BASIC_BOGEY_BURN_RATE_PER_SEC;
        }

        private const double _BASIC_BOGEY_STARTING_FUEL = 1000.0;    // @@@
        private const double _BASIC_BOGEY_BURN_RATE_PER_SEC = 1.0;

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

            double delta_fuel = ktime.EllapsedSeconds * fuel_burn_rate;
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


    }
}


