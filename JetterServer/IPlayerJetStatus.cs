using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterServer
{
    public interface IPlayerJetStatus
    {
        /// <summary>
        /// True if sprite has collided one or more times (this turn).
        /// </summary>
        bool HasCollided
        { get; }

        /// <summary>
        /// Heading is the angle (in degrees) the sprite is facing.
        /// </summary>
        double Heading
        { get; }
    }
}
