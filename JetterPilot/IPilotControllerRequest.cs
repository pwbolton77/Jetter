using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetterCommon;

namespace JetterPilot
{
    /// <summary>
    /// Interface for requesting services from the controller.
    /// </summary>
    public interface IPilotControllerRequest
    {
        /// <summary>
        /// Add a sprite to the controller set of managed sprites (e.g. shoot a missile). 
        /// </summary>
        /// <param name="ktime"></param>
        /// <param name="sprite"></param>
        /// <returns>Controller id</returns>
        long AddSprite(KeeperTime ktime, IClientSprite sprite);


        /// <summary>
        /// Return a reference to a sprite given the controller id.
        /// </summary>
        /// <param name="controller_id"></param>
        /// <returns></returns>
        IClientSprite getSprite(long controller_id);
    }

}
