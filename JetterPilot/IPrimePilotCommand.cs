using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterPilot
{
    interface IPrimePilotCommand
    {
        /// <summary>
        /// Enable/disable extended debug commands.
        /// </summary>
        bool DebugCommandsOn { get; set; }

        /// <summary>
        // Process a key command.
        /// </summary>
        /// <param name="key_cmd"></param>
        /// <returns>handled</returns>
        bool keyCommand(PilotKeyCommands key_cmd);
    }
}
