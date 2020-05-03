using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace JetterCommServiceLibrary
{
    #region Public enums/event args
    /// <summary>
    /// A simple enumeration for dealing with the chat message types
    /// </summary>
    public enum MessageType { Receive, UserEnter, UserLeave, ReceiveWhisper, RecieveServerStatusMessage };

    /// <summary>
    /// This class is used when carrying out any of the 4 chat callback actions
    /// such as Receive, ReceiveWhisper, UserEnter, UserLeave <see cref="IChatCallback">
    /// IChatCallback</see> for more details
    /// </summary>
    public class ChatEventArgs : EventArgs
    {
        public MessageType msgType;
        public string person;
        public string message;
        // @@@@ For ReceiveServerStatusMessage: Not sure if I should create another Args class yet. // @@@
    }
    #endregion

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class JetterCommService : IJetterCommService
    {
        #region Instance fields
        //callback interface for clients
        IJetterCommCallbackService callback = null;
        private JetterCommManager.ChatEventHandler myEventHandler = null;
        //current person 
        private string person;
        private JetterCommManager _comm_manager = JetterCommManager.getInstance();
        #endregion

        public CommPointInfo[] Join(string pilot_name)
        {
            //create a new ChatEventHandler delegate, pointing to the MyEventHandler() method
            myEventHandler = new JetterCommManager.ChatEventHandler(MyEventHandler);

            CommPointInfo[] person_list = null;
            bool userAdded = _comm_manager.Join(pilot_name, myEventHandler, out person_list);

            if (userAdded)
            {
                this.person = pilot_name;
                callback = OperationContext.Current.GetCallbackChannel<IJetterCommCallbackService>();
            }
            return person_list;
        }

        public void Say(string msg)
        {
            ChatEventArgs e = new ChatEventArgs();
            e.msgType = MessageType.Receive;
            e.person = this.person;
            e.message = msg;
            _comm_manager.BroadcastMessage(e);
        }

        public void Whisper(string to, string msg)
        {
            _comm_manager.Whisper(this.person, to, msg);
        }

        public void Leave()
        {
            if (this.person == null)
                return;

            _comm_manager.Leave(this.person);
            this.person = null;
        }

        /// <summary>
        /// This method is called when ever one of the chatters
        /// ChatEventHandler delegates is invoked. When this method
        /// is called it will examine the events ChatEventArgs to see
        /// what type of message is being broadcast, and will then
        /// call the correspoding method on the clients callback interface
        /// </summary>
        /// <param name="sender">the sender, which is not used</param>
        /// <param name="e">The ChatEventArgs</param>
        private void MyEventHandler(object sender, ChatEventArgs e)
        {
            try
            {
                switch (e.msgType)
                {
                    case MessageType.Receive:
                        callback.Receive(e.person, e.message);
                        break;
                    case MessageType.ReceiveWhisper:
                        callback.ReceiveWhisper(e.person, e.message);
                        break;
                    case MessageType.UserEnter:
                        callback.CommPointEnter(new CommPointInfo(e.person));  // @@@ Temp - we should use the actual comm point that was just added to the list.
                        break;
                    case MessageType.UserLeave:
                        callback.CommPointLeave(e.person);
                        break;
                    case MessageType.RecieveServerStatusMessage:
                        callback.ReceiveServerStatusMessage(e.message);
                        break;
                }
            }
            catch
            {
                Leave();
            }
        }


        public void PilotRequest(PilotCommand pilot_command)
        {
            _comm_manager.PilotRequest(this.person, pilot_command);
        }
    }
}
