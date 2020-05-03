using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterPilot
{
    public delegate void ActorRemoveHandler(object sender);
    public delegate void ActorMoveHandler(object sender);

    public enum RadarBlipVisibilityState 
    {
        Hidden,
        Eyeshot,
        Blip,
    }

    public interface IClientActor
    {
        /// <summary>
        /// Registry for notification that actor/sprite is removed from play.
        /// </summary>
        event ActorRemoveHandler Removed;

        /// <summary>
        /// Registry for notification that actor/sprite is moved from play.
        /// </summary>
        event ActorMoveHandler Moved;

        /// <summary>
        /// Return the IClientSprite interface of the imbedded sprite.
        /// </summary>
        IClientSprite Sprite { get; }
    }
}
