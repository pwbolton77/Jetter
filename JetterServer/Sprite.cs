using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;

namespace JetterServer
{
    public abstract class Sprite : ISprite
    {
        public event CollisionHandler Collision;
        public event CollisionHandler NonCollision;
        public event MovedHandler Moved;
        public event RemoveHandler Removed;

        /// <summary>
        // Construct with position (and invalid velocity).
        /// </summary>
        /// <param name="position"></param>
        /// <param name="is_moving"></param>
        public Sprite(Vector position, bool is_moving = true)
        {
            _position = position;
            _velocity.Normalize();  // Note: _velocity will be set to NaN.
            _is_moving = is_moving;
        }

        /// <summary>
        /// Construct with position, direction, and speed. (Velocity is
        /// set from direction and speed.)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        /// <param name="is_moving"></param>
        public Sprite(Vector position, Vector direction, double speed, bool is_moving = true)
        {
            _position = position;
            direction.Normalize();  // Note: This will NOT crash if direction vector is NaN.
            _velocity = Vector.Multiply(direction, speed);
            _is_moving = is_moving;
        }

        /// <summary>
        /// Construct from position and velocity.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="is_moving"></param>
        public Sprite(Vector position, Vector velocity, bool is_moving = true)
        {
            _position = position;
            _velocity = velocity;
            _is_moving = is_moving;
        }

        /// <summary>
        /// True if velocity is valid (check for NaN or "not a number").
        /// </summary>
        public bool IsValidVelocity
        {
            get { return !double.IsNaN(_velocity.X) && !double.IsNaN(_velocity.Y); }
        }

        /// <summary>
        /// True if the position is valid (check for NaN or "not a number").
        /// </summary>
        public bool IsValidPosition
        {
            get { return !double.IsNaN(_position.X) && !double.IsNaN(_position.Y); }
        }


        /// <summary>
        /// Width property.
        /// </summary>
        private double _width = 0.0;
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Height property.
        /// </summary>
        private double _height = 0.0;
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Center is Position plus half the width/height of the sprite.
        /// </summary>
        public Point Center
        {
            get
            { return new Point(_position.X + (_width / 2), _position.Y + (_height / 2)); }
        }


        /// <summary>
        /// Acumulated count of the number collision incurred by the sprite.
        /// </summary>
        private int _collisions = 0;
        public int Collisions
        {
            get { return _collisions; }
            set { _collisions = value; }
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
        /// Heading is the angle (in degrees) the sprite is facing, with 0
        /// being headed in the positive Y direction (nominaly down),
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
        /// Heading velocity is the angular rate of change per move/turn in degrees.
        /// </summary>
        private double _heading_velocity;
        public double HeadingVelocity
        {
            get { return _heading_velocity; }
            set { _heading_velocity = (value % 360.0 + 360.0) % 360.0; }
        }

        /// <summary>
        /// Velocity vector property.
        /// </summary>
        private Vector _velocity = new Vector(0.0, 0.0);
        public Vector Velocity
        {
            get
            {
                return _velocity;
            }
            set { _velocity = value; }
        }

        /// <summary>
        /// Direction property (as calculated from velocity vector).
        /// </summary>
        public Vector Direction
        {
            get
            {
                Vector norm = _velocity;
                norm.Normalize();
                return norm;
            }
            set
            {
                value.Normalize();
                double len = value.Length;
                _velocity = Vector.Multiply(value, len);
            }
        }

        /// <summary>
        /// Speed property (as calculated from velocity vector).
        /// This can be NaN if velocity is not valid.
        /// </summary>
        public double Speed
        {
            get { return _velocity.Length; }
            set
            {
                _velocity.Normalize();
                _velocity = Vector.Multiply(_velocity, value);
            }
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
        /// IsMoving property.  Note: "is moving" is a simple bool that is independent of velocity.
        /// </summary>
        private bool _is_moving = true;
        public bool IsMoving
        {
            get { return _is_moving; }
            set { _is_moving = value; }
        }

        /// <summary>
        /// Sprite has collided with another sprite.  Perform collide processing. 
        /// </summary>
        /// <param name="s"></param>
        public virtual void Collide(ISprite other_sprite)
        {
            HasCollided = true;
            OnCollision(other_sprite);
        }

        /// <summary>
        /// Sprite has not collided with anything (call once per turn).  Perform non-collide processing.
        /// </summary>
        public virtual void NonCollide()
        {
            if (!this.HasCollided)
                this.OnNonCollision();
        }

        /// <summary>
        /// True if sprite has collided one or more times (this turn).
        /// </summary>
        private bool _has_collided;
        public bool HasCollided
        {
            get { return _has_collided; }
            set { _has_collided = value; }
        }

        /// <summary>
        /// Perform move processing (call once per turn).
        /// </summary>
        public virtual void Move()
        {
            HasCollided = false;
            if (this.IsMoving)
            {
                Vector newPosition = CalculateNewPosition();
                double newHeading = CalculateNewHeading();
                Vector newVelocity = CalculateNewVelocity();

                UpdatePosition(newPosition, newVelocity, newHeading);

                this.OnMoved();
            }
        }

        /// <summary>
        /// Perform processing when removed from play (done with sprite for now - maybe recycle).
        /// </summary>
        public virtual void Remove()
        {
            this.OnRemoved();
        }


        /// <summary>
        /// Reset to reasonable defaults (not moving, invalid velocity, invalid position, etc.)
        /// </summary>
        public virtual void Reset()
        {
            _velocity = new Vector(0.0, 0.0);
            _velocity.Normalize();  // Note: _velocity will be set to NaN.

            _position = new Vector();
            _position.Normalize();  // Note: _position will be set to NaN.

            HasCollided = false;
            IsMoving = false;
        }

        /// <summary>
        /// Calculate the new position (based on position and velocity of sprite).
        /// </summary>
        /// <returns></returns>
        protected virtual Vector CalculateNewPosition()
        {
            Vector newPosition = Vector.Add(Velocity, Position);
            return newPosition;
        }

        /// <summary>
        /// Calculate the new heading (based on heading and heading velocity).
        /// </summary>
        /// <returns></returns>
        protected virtual double CalculateNewHeading()
        {
            double newHeading = Heading + HeadingVelocity;
            return newHeading;
        }

        /// <summary>
        /// Calculate the new velocity based on acceleration.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector CalculateNewVelocity()
        {
            Vector newVelocity = Velocity + MathHelper.createVectFromHeading(Heading, Acceleration);
            return newVelocity;
        }


        /// <summary>
        /// Update the position of the sprite.
        /// </summary>
        /// <param name="newPosition"></param>
        public void UpdatePosition(Vector newPosition, Vector newVelocity, double newHeading)
        {
            this.Velocity = newVelocity;
            this.Heading = newHeading;
            this.Position = newPosition;
        }


        #region event helper functions

        /// <summary>
        /// OnCollision event helper.
        /// </summary>
        /// <param name="s"></param>
        protected void OnCollision(ISprite s)
        {
            this.Collisions++;

            if (Collision != null)
                Collision(this, s);
        }

        /// <summary>
        /// OnNonCollision event helper.
        /// </summary>
        protected void OnNonCollision()
        {
            if (NonCollision != null)
                NonCollision(this, null);
        }

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
            return string.Format("p:{0}, t:{1}, m:{2}", this.Position.ToString(), base.ToString(), this.IsMoving);
        }
    }
}
