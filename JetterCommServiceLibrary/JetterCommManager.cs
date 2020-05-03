using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace JetterCommServiceLibrary
{
    public sealed class JetterCommManager
    {
        //thread sync lock object
        private static Object syncObj = new Object();
        //delegate used for BroadcastEvent
        public delegate void ChatEventHandler(object sender, ChatEventArgs e);
        public static event ChatEventHandler ChatEvent;
        //holds a list of chatters, and a delegate to allow the BroadcastEvent to work
        //out which chatter delegate to invoke
        static Dictionary<CommPointInfo, ChatEventHandler> chatters = new Dictionary<CommPointInfo, ChatEventHandler>();

        // Singleton ChatServer
        private static JetterCommManager _theInstance = new JetterCommManager();
        public static JetterCommManager getInstance() { return _theInstance; }
        private JetterCommManager()
        { }


        private IControllerCallback _controller_callback = null;
        public void setControllerCallback(IControllerCallback controller_callback)
        {
            _controller_callback = controller_callback;
        }

        /// <summary>
        /// Takes a <see cref="Common.Person">Person</see> and allows them
        /// to join the chat room, if there is not already a chatter with
        /// the same name
        /// </summary>
        /// <param name="person"><see cref="Common.Person">Person</see> joining</param>
        /// <return
        public bool Join(string name, ChatEventHandler chat_event_handler, out CommPointInfo[] person_list)
        {
            bool userAdded = false;
            person_list = null;

            CommPointInfo person = new CommPointInfo(name);

            //carry out a critical section that checks to see if the new chatter
            //name is already in use, if its not allow the new chatter to be
            //added to the list of chatters, using the person as the key, and the
            //ChatEventHandler delegate as the value, for later invocation
            lock (syncObj)
            {
                if (!checkIfPersonExists(person.name) && person != null)
                {
                    chatters.Add(person, chat_event_handler);
                    userAdded = true;
                }
            }

            //if the new chatter could be successfully added, get a callback instance
            //create a new message, and broadcast it to all other chatters, and then 
            //return the list of al chatters such that connected clients may show a
            //list of all the chatters
            if (userAdded)
            {
                ChatEventArgs e = new ChatEventArgs();
                e.msgType = MessageType.UserEnter;
                e.person = person.name;
                BroadcastMessage(e);

                //add this newly joined chatters ChatEventHandler delegate, to the global
                //multicast delegate for invocation
                ChatEvent += chat_event_handler;
                //carry out a critical section that copy all chatters to a new list
                lock (syncObj)
                {
                    person_list = new CommPointInfo[chatters.Count];
                    chatters.Keys.CopyTo(person_list, 0);
                }
                // Tell the controller we have a new pilot that joined.
                _controller_callback.Join(person.name);
            }

            return userAdded;
        }

        /// <summary>
        /// A request has been made by a client to leave the chat room,
        /// so remove the <see cref="Common.Person">Person </see>from
        /// the internal list of chatters, and unwire the chatters
        /// delegate from the multicast delegate, so that it no longer
        /// gets invokes by globally broadcasted methods
        /// </summary>
        public void Leave(string person)
        {
            if (person == null)
                return;

            bool userRemoved = false;
            //get the chatters ChatEventHandler delegate
            ChatEventHandler chatterToRemove = null;

            //carry out a critical section, that removes the chatter from the
            //internal list of chatters
            lock (syncObj)
            {
                chatterToRemove = getPersonHandler(person);
                if (chatterToRemove != null)
                {
                    userRemoved = true; // Found the user that needs to be removed.
                    CommPointInfo comm_point = getPerson(person);
                    chatters.Remove(comm_point);
                }
            }

            if (userRemoved)
            {
                //unwire the chatters delegate from the multicast delegate, so that 
                //it no longer gets invokes by globally broadcasted methods
                ChatEvent -= chatterToRemove;
                ChatEventArgs e = new ChatEventArgs();
                e.msgType = MessageType.UserLeave;
                e.person = person;
                //broadcast this leave message to all other remaining connected
                //chatters
                BroadcastMessage(e);

                // Tell the controller we have a pilot that left.
                _controller_callback.Leave(person);
            }
        }

        /// <summary>
        ///loop through all connected chatters and invoke their 
        ///ChatEventHandler delegate asynchronously, which will firstly call
        ///the MyEventHandler() method and will allow a asynch callback to call
        ///the EndAsync() method on completion of the initial call
        /// </summary>
        /// <param name="e">The ChatEventArgs to use to send to all connected chatters</param>
        public void BroadcastMessage(ChatEventArgs e)
        {
            ChatEventHandler temp = ChatEvent;

            //loop through all connected chatters and invoke their 
            //ChatEventHandler delegate asynchronously, which will firstly call
            //the MyEventHandler() method and will allow a asynch callback to call
            //the EndAsync() method on completion of the initial call
            if (temp != null)
            {
                foreach (ChatEventHandler handler in temp.GetInvocationList())
                {
                    handler.BeginInvoke(this, e, new AsyncCallback(EndAsync), null);
                }
            }
        }

        /// <summary>
        /// Is called as a callback from the asynchronous call, so simply get the
        /// delegate and do an EndInvoke on it, to signal the asynchronous call is
        /// now completed
        /// </summary>
        /// <param name="ar">The asnch result</param>
        private void EndAsync(IAsyncResult ar)
        {
            ChatEventHandler d = null;

            try
            {
                //get the standard System.Runtime.Remoting.Messaging.AsyncResult,and then
                //cast it to the correct delegate type, and do an end invoke
                System.Runtime.Remoting.Messaging.AsyncResult asres = (System.Runtime.Remoting.Messaging.AsyncResult)ar;
                d = ((ChatEventHandler)asres.AsyncDelegate);
                d.EndInvoke(ar);
            }
            catch
            {
                ChatEvent -= d;
            }
        }

        public void BroadcastServersStatusMessage(string msg)
        {
            ChatEventArgs e = new ChatEventArgs();
            e.msgType = MessageType.RecieveServerStatusMessage;
            e.person = null;
            e.message = msg;
            BroadcastMessage(e);
        }

        /// <summary>
        /// Broadcasts the input msg parameter to all the <see cref="Common.Person">
        /// Person</see> whos name matches the to input parameter
        /// by looking up the person from the internal list of chatters
        /// and invoking their ChatEventHandler delegate asynchronously.
        /// Where the MyEventHandler() method is called at the start of the
        /// asynch call, and the EndAsync() method at the end of the asynch call
        /// </summary>
        /// <param name="to">The persons name to send the message to</param>
        /// <param name="msg">The message to broadcast to all chatters</param>
        public void Whisper(string from_person, string to, string msg)
        {
            ChatEventArgs e = new ChatEventArgs();
            e.msgType = MessageType.ReceiveWhisper;
            e.person = from_person;
            e.message = msg;
            try
            {
                ChatEventHandler chatterTo;
                //carry out a critical section, that attempts to find the 
                //correct Person in the list of chatters.
                //if a person match is found, the matched chatters 
                //ChatEventHandler delegate is invoked asynchronously
                lock (syncObj)
                {
                    chatterTo = getPersonHandler(to);
                    if (chatterTo == null)
                    {
                        throw new KeyNotFoundException("The person whos name is " + to +
                                                        " could not be found");
                    }
                }
                //do a async invoke on the chatter (call the MyEventHandler() method, and the
                //EndAsync() method at the end of the asynch call
                chatterTo.BeginInvoke(this, e, new AsyncCallback(EndAsync), null);
            }
            catch (KeyNotFoundException)
            {
            }
        }

        #region PilotRequests
        public void PilotRequest(string from_pilot, PilotCommand pilot_command)
        {
            if (_controller_callback != null)
                _controller_callback.PilotRequest(from_pilot, pilot_command);
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Searches the intenal list of chatters for a particular person, and returns
        /// true if the person could be found
        /// </summary>
        /// <param name="name">the name of the <see cref="Common.Person">Person</see> to find</param>
        /// <returns>True if the <see cref="Common.Person">Person</see> was found in the
        /// internal list of chatters</returns>
        private bool checkIfPersonExists(string name)
        {
            foreach (CommPointInfo p in chatters.Keys)
            {
                if (p.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Searches the intenal list of chatters for a particular person, and returns
        /// the individual chatters ChatEventHandler delegate in order that it can be
        /// invoked
        /// </summary>
        /// <param name="name">the name of the <see cref="Common.Person">Person</see> to find</param>
        /// <returns>The True ChatEventHandler delegate for the <see cref="Common.Person">Person</see> who matched
        /// the name input parameter</returns>
        private ChatEventHandler getPersonHandler(string name)
        {
            foreach (CommPointInfo p in chatters.Keys)
            {
                if (p.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    ChatEventHandler chatTo = null;
                    chatters.TryGetValue(p, out chatTo);
                    return chatTo;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches the intenal list of chatters for a particular person, and returns
        /// the actual <see cref="Common.Person">Person</see> whos name matches
        /// the name input parameter
        /// </summary>
        /// <param name="name">the name of the <see cref="Common.Person">Person</see> to find</param>
        /// <returns>The actual <see cref="Common.Person">Person</see> whos name matches
        /// the name input parameter</returns>
        private CommPointInfo getPerson(string name)
        {
            foreach (CommPointInfo p in chatters.Keys)
            {
                if (p.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }
        #endregion
    }
}
