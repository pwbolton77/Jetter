using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommServiceLibrary;

namespace JetterServer
{
    public class PlayerJetSprite : Sprite, IPlayerPilotCommand, IPlayerJetStatus
    {
        // Setup the movement play profile of the ship.
        public class PlayProfile
        {
            private const double DEFAULT_ACCELERATION = 0.15;
            private const double DEFAULT_MAX_SPEED = 1.5;
            private const double DEFAULT_HEADING_VELOCITY = 3.5;   // degrees of rotation per turn
            public PlayProfile()
            {
                Acceleration = DEFAULT_ACCELERATION;
                MaxSpeed = DEFAULT_MAX_SPEED;
                HeadingVelocity = DEFAULT_HEADING_VELOCITY;     // degrees of rotation per turn
            }
            public double HeadingVelocity { get; set; }
            public double Acceleration { get; set; }
            public double MaxSpeed { get; set; }
        }

        private PlayProfile _play_profile = new PlayProfile();
        public PlayProfile Profile
        {
            get { return _play_profile; }
            set { _play_profile = value; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        public PlayerJetSprite(Vector position, Vector direction, double speed)
            : base(position, direction, speed)
        {
        }


        /// <summary>
        /// True if the sprite is configured to bounce off the (initalized) position limits.
        /// </summary>
        private bool _is_bounce_off_position_limits_enabled = false;
        public bool IsBounceOffPositionLimitsEnabled
        {
            get { return _is_bounce_off_position_limits_enabled; }
            private set { _is_bounce_off_position_limits_enabled = value; }
        }


        private bool _initialized_bounce_limits = false;   // True if setBounceLimits() has been called to initialize bounce limits.

        /// <summary>
        /// Setup bounce limits so sprite will bounce off (the game stage) boundary.  The limits must account for
        /// the (visual) width and height of the sprite.
        /// </summary>
        /// <param name="stage_limits"></param>
        /// <param name="sprite_width"></param>
        /// <param name="sprite_height"></param>
        /// <param name="enable_bounce"></param>
        /// <returns>True if bounds are valid based on sprite width/height. </returns>
        public bool setBounceLimits(Rect stage_limits, double sprite_width, double sprite_height, bool enable_bounce = true)
        {
            bool result = false;
            if ((stage_limits.Width - sprite_width >= 0.0) && (stage_limits.Height - sprite_height >= 0.0))
            {
                PositionLimits = new Rect(stage_limits.X, stage_limits.Y, stage_limits.Width - sprite_width, stage_limits.Height - sprite_height);
                _initialized_bounce_limits = true;

                if (enable_bounce)
                    enableBounceOffPositionLimits();
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Setup bounce limits so sprite will bounce off (the game stage) boundary.  The limits must account for
        /// the (visual) width and height of the sprite. This method assumes the sprite width/height has already 
        /// been set.
        /// </summary>
        /// <param name="stage_limits"></param>
        /// <param name="enable_bounce"></param>
        /// <returns>True if bounds are valid based on sprite width/height. </returns>
        public bool setBounceLimits(Rect stage_limits, bool enable_bounce = true)
        {
            return setBounceLimits(stage_limits, Width, Height, enable_bounce);
        }

        /// <summary>
        /// Enable "bounce off position limits".  The method setBounceLimits() (and sprite width/height if used) must 
        /// be called first to initialize limits.
        /// </summary>
        /// <returns>True if it was sucessfully enable.</returns>
        public bool enableBounceOffPositionLimits()
        {
            if (_initialized_bounce_limits)
                IsBounceOffPositionLimitsEnabled = true;
            return IsBounceOffPositionLimitsEnabled;
        }

        /// <summary>
        /// Disable "bounce off position limits".
        /// </summary>
        public void disableBounceOffPositionLimits()
        {
            IsBounceOffPositionLimitsEnabled = false;
        }



        /// <summary>
        /// Positon limits (e.g. for bouncing off limits).
        /// </summary>
        public Rect PositionLimits { get; private set; }

        public enum BoundaryNames
        {
            OuterCircle,
            InnerCircle
        }
        public enum States
        {
            Invalid,
            Calm,
            Spinning,
            Pulsing,
        }

        private States _state = States.Calm;
        public States State
        {
            get { return _state; }
            set { _state = value; }
        }

        //custom events for this model type
        // public event EventHandler DescendingCollision;

        public delegate void StateChangeHandler(object sender, States previous_state);
        public event StateChangeHandler NowCalmStateChange;
        public event StateChangeHandler NowSpinningStateChange;
        public event StateChangeHandler NowPulsingStateChange;

        public event CollisionHandler CalmCollision;
        public event CollisionHandler SpinningCollision;
        public event CollisionHandler PulsingCollision;


        /// <summary>
        /// Calculate the new position (based on position and velocity of sprite).
        /// </summary>
        /// <returns></returns>
        protected override Vector CalculateNewPosition()
        {
            Vector newPosition = Vector.Add(Velocity, Position);
            const double damp_bounce = 0.20;  // Damp the reverse direction.  0.20 means 20%.

            if (this.IsBounceOffPositionLimitsEnabled)
            {
                Point pos = (Point)newPosition;
                if (!PositionLimits.Contains(pos))
                {
                    if (pos.X < PositionLimits.Left)
                    {
                        // Deal with X too small
                        pos.X = PositionLimits.Left;
                        Velocity = new Vector(-Velocity.X * damp_bounce, Velocity.Y);
                    }
                    else if (pos.X > PositionLimits.Right)
                    {
                        // Deal with X too large 
                        pos.X = PositionLimits.Right;
                        Velocity = new Vector(-Velocity.X * damp_bounce, Velocity.Y);
                    }

                    if (pos.Y < PositionLimits.Top)
                    {
                        // Deal with Y too small
                        pos.Y = PositionLimits.Top;
                        Velocity = new Vector(Velocity.X, -Velocity.Y * damp_bounce);
                    }
                    else if (pos.Y > PositionLimits.Bottom)
                    {
                        // Deal with Y too large 
                        pos.Y = PositionLimits.Bottom;
                        Velocity = new Vector(Velocity.X, -Velocity.Y * damp_bounce);
                    }

                    newPosition = (Vector)pos;
                }
            }

            return newPosition;
        }



        /// <summary>
        /// Calculate the new velocity based on acceleration.
        /// </summary>
        /// <returns></returns>
        protected override Vector CalculateNewVelocity()
        {
            Vector newVelocity = base.CalculateNewVelocity();

            if (newVelocity.Length > Profile.MaxSpeed)
            {
                newVelocity.Normalize();
                newVelocity = Vector.Multiply(newVelocity, Profile.MaxSpeed);
            }
            return newVelocity;
        }

        /// <summary>
        /// True if the sprite is attached to player commands (i.e. is not expected to 
        /// act on its own intelligence. 
        /// </summary>
        private bool _is_attach_focus;
        public bool IsAttachToPlayer
        {
            get { return _is_attach_focus; }
            set { _is_attach_focus = value; }
        }


        /// <summary>
        /// Called when the player starts directing commands to this sprite. 
        /// </summary>
        public void AttachPlayer()
        {
            IsAttachToPlayer = true;
        }

        /// <summary>
        /// Called when the player stops directing commands to this sprite. 
        /// </summary>
        public void DetachPlayer()
        {
            IsAttachToPlayer = false;
        }


        public override void Collide(ISprite other)
        {
            if (this.State == States.Calm)
            {
                OnCalmCollision(other);
                OnNowSpinningStateChange();   // Now change state to spinning
            }
            else if (this.State == States.Spinning)
            {
                OnSpinningCollision(other);
                OnNowPulsingStateChange();   // Now change state to pulsing 
            }
            else if (this.State == States.Pulsing)
            {
                OnPulsingCollision(other);
                OnNowCalmStateChange();   // Now change state to calm 
            }

            base.Collide(other);    // Tell the base class of collision.
        }

        protected void OnCalmCollision(ISprite s)
        {
            this.Collisions++;
            if (CalmCollision != null)
            {
                CalmCollision(this, s);
            }
        }

        protected void OnSpinningCollision(ISprite s)
        {
            this.Collisions++;
            if (SpinningCollision != null)
            {
                SpinningCollision(this, s);
            }
        }
        protected void OnPulsingCollision(ISprite s)
        {
            this.Collisions++;
            if (PulsingCollision != null)
            {
                PulsingCollision(this, s);
            }
        }
        protected void OnNowCalmStateChange()
        {
            States prev_state = this.State;
            this.State = States.Calm;
            if (NowCalmStateChange != null)
                NowCalmStateChange(this, prev_state);
        }
        protected void OnNowSpinningStateChange()
        {
            States prev_state = this.State;
            this.State = States.Spinning;
            if (NowSpinningStateChange != null)
                NowSpinningStateChange(this, prev_state);
        }
        protected void OnNowPulsingStateChange()
        {
            States prev_state = this.State;
            this.State = States.Pulsing;
            if (NowPulsingStateChange != null)
                NowPulsingStateChange(this, prev_state);
        }

        public void PilotRequest(JetterCommServiceLibrary.PilotCommand pilot_command)
        {
            // Temp pilot commands @@@@@@@@@@@@@@
            switch (pilot_command.command)
            {
                case PilotCommandType.RudderNeutral:
                    break;
                case PilotCommandType.RudderLeft:
                    Position = new Vector(Position.X - 2, Position.Y);
                    break;
                case PilotCommandType.RudderRight:
                    Position = new Vector(Position.X + 2, Position.Y);
                    break;

                case PilotCommandType.ThrustNeutral:
                    break;
                case PilotCommandType.ThrustUp:
                    Position = new Vector(Position.X, Position.Y - 2);
                    break;
                case PilotCommandType.ThrustDown:
                    // Velocity = MathHelper.stop_velocity;
                    Position = new Vector(Position.X, Position.Y + 2);
                    break;

                default:
                    break;
            }
        }
    }
}
