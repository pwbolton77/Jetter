using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace JetterCommServiceLibrary
{
    public enum PilotCommandType { 
        RudderNeutral, 
        RudderRight, 
        RudderLeft, 
        ThrustNeutral, 
        ThrustUp, 
        ThrustDown,
        FireMissleTracked,
    };

    [DataContract]
    public class PilotCommand
    {
        [DataMember]
        public PilotCommandType command;  
    }

    [DataContract]
    public class FireMissleTrackedPilotCmd : PilotCommand
    {
        // [DataMember]
        // public IClientSprite tracked_target = null;   // @@@ need to change this to a controller assigned identifier.
    }


}


