using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace JetterCommServiceLibrary
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IJetterCommCallbackService))]
    public interface IJetterCommService
    {
        [OperationContract(IsOneWay = true, IsInitiating = false, IsTerminating = false)]
        void PilotRequest(PilotCommand pilot_command);

        [OperationContract(IsOneWay = true, IsInitiating = false, IsTerminating = false)]
        void Say(string msg);

        [OperationContract(IsOneWay = true, IsInitiating = false, IsTerminating = false)]
        void Whisper(string to, string msg);

        [OperationContract(IsOneWay = false, IsInitiating = true, IsTerminating = false)]
        CommPointInfo[] Join(string pilot_name);

        [OperationContract(IsOneWay = true, IsInitiating = false, IsTerminating = true)]
        void Leave();
    }

    public interface IJetterCommCallbackService
    {
        [OperationContract(IsOneWay = true)]
        void Receive(string sender, string message);

        [OperationContract(IsOneWay = true)]
        void ReceiveWhisper(string sender, string message);

        [OperationContract(IsOneWay = true)]
        void ReceiveServerStatusMessage(string message);

        [OperationContract(IsOneWay = true)]
        void CommPointEnter(CommPointInfo comm_point);

        [OperationContract(IsOneWay = true)]
        void CommPointLeave(string comm_point_name);
    }
}
