using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;

namespace JetterPilot
{
    public delegate void MovedHandler(object sender);
    public delegate void RemoveHandler(object sender);
    public interface IClientSprite
    {
        /// <summary>
        /// Registry for notification that sprite has moved.
        /// </summary>
        event MovedHandler Moved;

        /// <summary>
        /// Registry for notification that sprite is removed from play.
        /// </summary>
        event RemoveHandler Removed;

        /// <summary>
        /// Update the position and movement parameters of the sprite.
        /// </summary>
        /// <param name="newPosition"></param>
        void SetMovement(Vector new_position, double heading,
            double speed = 0.0, double acceleration = 0.0);

        /// <summary>
        /// Position is relative to top left.
        /// </summary>
        Vector Position { get; set; }

        /// <summary>
        /// Heading is the angle (in degrees) the sprite is moving, with 0
        /// being headed in the positive Y direction,
        /// and going towards the negative X as the angle increases (nominaly 
        /// clockwise).
        /// </summary>
        double Heading { get; set; }

        /// <summary>
        /// The scaler speed of the sprite (the length of the Velocity vector).
        /// This can be NaN if velocity is not valid.
        /// </summary>
        double Speed { get; set; }

        /// <summary>
        /// Acceleration for increasing speed per turn.
        /// </summary>
        double Acceleration { get; set; }

        /// <summary>
        /// Compute the next position based on current velocity.
        /// </summary>
        void Move(KeeperTime ktime);

        /// <summary>
        /// Interact with another sprite.
        /// </summary>
        /// <param name="other_sprite"></param>
        /// <param name="is_primary_interation"> True if this is the primary/first interaction betweeen the two for this pass 
        /// through the update loop. </param>
        void Interact(KeeperTime ktime, IClientSprite other_sprite, bool is_primary_interation);

        /// <summary>
        /// Return true if the sprite has been removed from the 
        /// game (i.e. just after the Removed event handlers are invoked).
        /// </summary>
        bool IsRemoved { get; }

        /// <summary>
        /// Velocity vector (based on heading multiplied by speed).
        /// </summary>
        Vector Velocity { get; }

        /// <summary>
        /// Direction property (unit size as calculated from heading and speed).
        /// </summary>
        Vector Direction { get; }

        /// <summary>
        /// Get/Set the controller id of the sprite.
        /// </summary>
        long ControllerId { get; set; }
    }
}
