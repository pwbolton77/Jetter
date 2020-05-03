using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetterPilot.JetterCommServiceReference;
using System.ServiceModel;
using System.Diagnostics;

namespace JetterPilot
{
    public class ServerProxy : IJetterCommServiceCallback
    {
        private static ServerProxy theInstance = new ServerProxy();
        public static ServerProxy getInstance() { return theInstance; }
        private static JetterCommServiceClient _client = null;
        private static string _player_login_name;

        ///////////////////////////////////////////
        /////// IJetterCommServiceCallback
        #region IJetterCommServiceCallback
        void IJetterCommServiceCallback.Receive(string sender, string message)
        {
            Trace.Write("JetterCommCallbackClient: Receive: ");
            Trace.WriteLine(String.Format("sender: {0}  message: {1}", sender, message));
        }

        void IJetterCommServiceCallback.ReceiveWhisper(string sender, string message)
        {
            Trace.Write("JetterCommCallbackClient: ReceiveWhisper: ");
            Trace.WriteLine(String.Format("sender: {0}  message: {1}", sender, message));
        }

        void IJetterCommServiceCallback.CommPointEnter(CommPointInfo comm_point)
        {
            Trace.Write("JetterCommCallbackClient: CommPointEnter: ");
            Trace.WriteLine(String.Format("comm_point: {0}", comm_point.name));
        }

        void IJetterCommServiceCallback.CommPointLeave(string comm_point_name)
        {
            Trace.Write("JetterCommCallbackClient: CommPointLeave: ");
            Trace.WriteLine(String.Format("comm_point_name: {0}", comm_point_name));
        }

        //////////////////////////////////////////////////
        #region ServerStatusMessage
        public delegate void ServerRx_ServerStatusMessageEventHandler(object sender, ServerRx_ServerStatusMessageArgs e);
        public event ServerRx_ServerStatusMessageEventHandler ServerRx_ServerStatusMessageEvent;
        public class ServerRx_ServerStatusMessageArgs: EventArgs
        {
            public string message;
        }

        void IJetterCommServiceCallback.ReceiveServerStatusMessage(string message)
        {
            Trace.Write("!! JetterCommCallbackClient: ReceiveServerStatusMessage: ");
            Trace.WriteLine(String.Format("{0}", message));

            ServerRx_ServerStatusMessageArgs e = new ServerRx_ServerStatusMessageArgs ();
            e.message = message;
            OnServerRx_ServerStatusMessage_CallBackEvent(e);
        }

        void OnServerRx_ServerStatusMessage_CallBackEvent(ServerRx_ServerStatusMessageArgs e)
        {
            if (ServerRx_ServerStatusMessageEvent != null)
            {
                // Invokes the delegates. 
                ServerRx_ServerStatusMessageEvent(this, e);
            }
        }

        #endregion
        #endregion

        /// <summary>
        /// Begins an asynchronous join operation on the underlying <see cref="ChatProxy">ChatProxy</see>
        /// which will call the OnEndJoin() method on completion
        /// </summary>
        /// <param name="p">The <see cref="Common.Person">chatter</see> to try and join with</param>
        public bool Connect(string login_name)
        {
            bool connected = false;

            InstanceContext site = new InstanceContext(this);
            _client = new JetterCommServiceClient(site, "NetTcpBinding_IJetterCommService");
            CommPointInfo[] comm_point_info = _client.Join(login_name);

            if (comm_point_info == null || comm_point_info.Length == 0)
            {
                // Problems jointing
                Trace.WriteLine("!!!! Failed to connect/join to server.");
            }
            else
            {
                connected = true;
                _player_login_name = login_name;

                // Print communication points
                Trace.WriteLine("\nCommunication points:");
                foreach (CommPointInfo info in comm_point_info)
                    Trace.WriteLine(String.Format("   Point: {0}", info.name));
            }

            ////////////////////////////////////////
            //InstanceContext site = new InstanceContext(this);
            //proxy = new ChatProxy(site);
            //IAsyncResult iar = proxy.BeginJoin(p, new AsyncCallback(OnEndJoin), null);

            return connected;
        }

    }
}
