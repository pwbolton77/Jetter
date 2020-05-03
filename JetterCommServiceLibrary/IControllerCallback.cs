using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterCommServiceLibrary
{
    public interface IControllerCallback
    {
        void Join(string pilot);
        void Leave(string pilot);
        void PilotRequest(string from_pilot, PilotCommand pilot_command);
    }
}
