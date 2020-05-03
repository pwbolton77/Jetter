using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JetterServer
{
    public delegate void CollisionHandler(object sender, ISprite other_sprite);
    public delegate void MovedHandler(object sender);
    public delegate void RemoveHandler(object sender);
    public interface ISprite
    {
        /// <summary>
        /// Register for notification that sprite has collided.
        /// </summary>
        event CollisionHandler Collision;

        /// <summary>
        /// Registry for notification that sprite has moved.
        /// </summary>
        event MovedHandler Moved;

        /// <summary>
        /// Registry for notification that sprite is removed from play.
        /// </summary>
        event RemoveHandler Removed;

        /// <summary>
        // Property incremented each time the sprite has collided.
        /// </summary>
        int Collisions { get; set; }

        /// <summary>
        /// The scaler speed of the sprite (the length of the Velocity vector).
        /// This can be NaN if velocity is not valid.
        /// </summary>
        double Speed { get; set; }

        /// <summary>
        /// Position is relative to top left.
        /// </summary>
        Vector Position { get; set; }

        /// <summary>
        /// Direction is a unit vector of the direction (Velocity is Direction scaled by Speed).
        /// </summary>
        Vector Direction { get; set; }

        /// <summary>
        /// Acceleration for increasing velocity per turn.
        /// </summary>
        double Acceleration { get; set; }

        /// <summary>
        // Velocity is the Direction vector multiplied by Speed.
        /// </summary>
        Vector Velocity { get; set; }

        /// <summary>
        /// Heading is the angle (in degrees) the sprite is facing.
        /// </summary>
        double Heading { get; set; }

        /// <summary>
        /// Heading velocity is the angular rate of change per move/turn in degrees.
        /// </summary>
        double HeadingVelocity { get; set; }

        /// <summary>
        /// Center is Position plus half the width/height of the sprite.
        /// </summary>
        Point Center { get; }

        /// <summary>
        /// True if the sprite is considered moving, rather than being a static object.
        /// This is a simple bool status that ignores speed/velocity attribute.
        /// </summary>
        bool IsMoving { get; set; }

        /// <summary>
        /// Width of the sprite.
        /// </summary>
        double Width { get; set; }

        /// <summary>
        /// Height of the sprite.
        /// </summary>
        double Height { get; set; }

        /// <summary>
        /// Compute the next position based on current velocity.
        /// </summary>
        void Move();

        /// <summary>
        /// Perform processing when removed from play (done with sprite for now - maybe recycle).
        /// </summary>
        void Remove();

        /// <summary>
        /// Do what needs doing when the sprite collides with another sprite.
        /// </summary>
        /// <param name="other_sprite"></param>
        void Collide(ISprite other_sprite);
    }
}
