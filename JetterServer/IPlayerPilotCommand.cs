using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetterCommServiceLibrary;

namespace JetterServer
{
    public interface IPlayerPilotCommand
    {
        void PilotRequest(PilotCommand pilot_command);
    }
}
