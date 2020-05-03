using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;

namespace JetterPilot
{
    public abstract class ClientSprite : IClientSprite
    {
        public event MovedHandler Moved;
        public event RemoveHandler Removed;

        /// <summary>
        /// Construct with position, direction, and speed. (Velocity is
        /// set from direction and speed.)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        public ClientSprite(KeeperTime ktime, IPilotControllerRequest controller_request, Vector position, double heading,
            double speed = 0.0, double acceleration = 0.0)
        {
            _controller_request = controller_request;
            SetMovement(position, heading, speed, acceleration);
        }

        protected IPilotControllerRequest _controller_request = null; // Interface for making requests of the controller (like adding missile sprites).

        /// <summary>
        /// Update the position and movement parameters of the sprite.
        /// </summary>
        /// <param name="newPosition"></param>
        public void SetMovement(Vector new_position, double heading,
            double speed = 0.0, double acceleration = 0.0)
        {
            Position = new_position;
            Heading = heading;
            Acceleration = acceleration;
            Speed = speed;
        }

        /// <summary>
        /// Position property.
        /// </summary>
        private Vector _position = new Vector(0.0, 0.0);
        public Vector Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Heading is the angle (in degrees) the sprite is moving, with 0
        /// being headed in the positive Y direction,
        /// and going towards the negative X as the angle increases (nominaly 
        /// clockwise).
        /// </summary>
        private double _heading = 0.0;
        public double Heading
        {
            get { return _heading; }
            set { _heading = (value % 360.0 + 360.0) % 360.0; }
        }

        /// <summary>
        /// Speed property
        /// </summary>
        private double _speed = 0.0;
        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
            }
        }

        /// <summary>
        /// Acceleration for increasing velocity per turn.
        /// </summary>
        private double _acceleration = 0.0;
        public double Acceleration
        {
            get { return _acceleration; }
            set { _acceleration = value; }
        }

        /// <summary>
        /// True if the position is valid (check for NaN or "not a number").
        /// </summary>
        public bool IsValidPosition
        {
            get { return !double.IsNaN(_position.X) && !double.IsNaN(_position.Y); }
        }



        /// <summary>
        /// Perform move processing (call once per turn).
        /// </summary>
        public virtual void Move(KeeperTime ktime)
        {
            NewtonMove(ktime);
            this.OnMoved();     // Invoke registred event handlers
        }

        /// <summary>
        /// Move the object based on newtonian physics. 
        /// </summary>
        protected void NewtonMove(KeeperTime ktime)
        {
            double accel_added_speed = _acceleration * ktime.EllapsedSeconds;

            _speed = _speed + accel_added_speed;

            // Get velocity vector based on heading and speed.
            Vector new_velocity = MathHelper.createVectFromHeading(_heading, _speed);

            // Add velocity vector times ellapsed time to get position. 
            Vector velocity_added_position = new_velocity * ktime.EllapsedSeconds;
            _position = Vector.Add(velocity_added_position, _position);
        }

        /// <summary>
        /// Interact with another sprite.
        /// </summary>
        /// <param name="other_sprite"></param>
        /// <param name="is_primary_interation"> True if this is the primary/first interaction betweeen the two for this pass 
        /// through the update loop. </param>
        public virtual void Interact(KeeperTime ktime, IClientSprite other_sprite, bool is_primary_interation)
        {
        }


        /// <summary>
        /// Perform processing when removed from play (done with sprite for now - maybe recycle).
        /// </summary>
        protected virtual void Remove()
        {  
            this.OnRemoved();   // Event handler helper
            _is_removed = true; // Indicate that sprite has been removed from the game.
            detachHandlersOnRemove();
        }

        /// <summary>
        /// Detach our handlers from other objects.
        /// </summary>
        virtual protected void detachHandlersOnRemove()
        {
        }

        /// <summary>
        /// Return true if the sprite has been removed from the 
        /// game (i.e. just after the Removed event handlers are invoked).
        /// </summary>
        public bool IsRemoved 
        {
            get { return _is_removed; }
            protected set { _is_removed = value; }
        }
        private bool _is_removed = false;
        

        /// <summary>
        /// Velocity vector (based on heading multiplied by speed).
        /// </summary>
        public Vector Velocity
        {
            get
            {
                Vector direction = MathHelper.createVectFromHeading(_heading);
                return (Vector.Multiply(direction, _speed));
            }
        }

        /// <summary>
        /// Direction property (unit size as calculated from heading and speed).
        /// </summary>
        public Vector Direction
        {
            get
            {
                Vector direction = MathHelper.createVectFromHeading(_heading);
                return direction;
            }
        }

        /// <summary>
        /// Get/Set the controller id of the sprite.
        /// </summary>
        public long ControllerId
        {
            get { return _controller_id; }
            set { _controller_id = value; }
        }
        private long _controller_id;
        

        #region event helper functions
        /// <summary>
        /// OnMoved event helper.
        /// </summary>
        protected void OnMoved()
        {
            if (Moved != null)
                Moved(this);
        }

        /// <summary>
        /// OnRemoved event helper.
        /// </summary>
        protected void OnRemoved()
        {
            if (Removed != null)
                Removed(this);
        }
        #endregion


        /// <summary>
        /// Return formatted string for position, type and is_moving.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("p:{0}, t:{1}, m:{2}", this.Position.ToString(), base.ToString());
        }
    }
}
