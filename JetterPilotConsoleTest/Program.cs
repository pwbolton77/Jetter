using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetterPilotConsoleTest.JetterCommServiceReference;
using System.ServiceModel;

namespace JetterPilotConsoleTest
{
    public class JetterCommCallbackClient : IJetterCommServiceCallback
    {
        public void Receive(string sender, string message)
        {
            Console.Write("JetterCommCallbackClient: Receive: ");
            Console.WriteLine("sender: {0}  message: {1}", sender, message);
        }

        public void ReceiveWhisper(string sender, string message)
        {
            Console.Write("JetterCommCallbackClient: ReceiveWhisper: ");
            Console.WriteLine("sender: {0}  message: {1}", sender, message);
        }

        public void CommPointEnter(CommPointInfo comm_point)
        {
            Console.Write("JetterCommCallbackClient: CommPointEnter: ");
            Console.WriteLine("comm_point: {0}", comm_point.name);
        }

        public void CommPointLeave(string comm_point_name)
        {
            Console.Write("JetterCommCallbackClient: CommPointLeave: ");
            Console.WriteLine("comm_point_name: {0}", comm_point_name);
        }

        public void ReceiveServerStatusMessage(string message)
        {
            Console.Write("!! JetterCommCallbackClient: ReceiveServerStatusMessage: ");
            Console.WriteLine("{0}", message);
        }
    }

    class Program
    {
        private static JetterCommServiceClient _client = null;
        private static string _player_login_name;
        static void Main(string[] args)
        {
            Console.WriteLine("Press <Enter> to attempt to Join ...");
            Console.ReadLine();

            //////////////////////////////////////////////
            // Get connected.
            //JetterCommCallbackClient callback_client = new JetterCommCallbackClient ();
            //InstanceContext site = new InstanceContext(callback_client);
            //_client = new JetterCommServiceClient(site, "NetTcpBinding_IJetterCommService");

            //CommPointInfo[] comm_point_info = _client.Join("JetterPilotConsoleTest");

            //// Print communication points
            //Console.WriteLine("\nCommunication points:");
            //foreach (CommPointInfo info in comm_point_info)
            //    Console.WriteLine("   Point: {0}", info.name);

            bool login_okay = PlayerLogin();

            /////////////////////////////////////////////
            // Take player commands
            if (login_okay)
            {
                PrintHelp();
                PlayerCommandLoop();
            }

            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();

            // Disconnect from server.
            if (_client != null)
                _client.Leave();
        }

        /// <summary>
        /// Validate the player name that was entered.
        /// </summary>
        /// <param name="login_name"></param>
        /// <param name="error_msg"></param>
        /// <returns></returns>
        static bool ValidatePlayerLoginName(string login_name, out string error_msg)
        {
            bool valid = false;
            error_msg = "";

            while (true)
            {
                if (login_name.Length == 0)
                {
                    error_msg = "Zero length";
                    break;  // invalid
                }

                if (!login_name.All(char.IsLetterOrDigit))
                { 
                    error_msg = "Only letters and digits allowed.";
                    break;  // invalid
                }

                valid = true;
                break;
            }

            return valid;
        }

        /// <summary>
        /// Get the player name and attempt to login to the server.
        /// </summary>
        /// <returns></returns>
        static bool PlayerLogin()
        {
            bool login_okay = false;
            bool valid_player_name = false;
            string login_name = "";

            bool done = false;
            while (!done && !valid_player_name )
            {
                Console.Write("Enter login name: ");
                login_name = Console.ReadLine();

                if (login_name.Length == 0)
                {
                    done = true;    // Take a zero length name to mean "don't attempt login".
                }
                else
                {
                    string error_message;
                    valid_player_name = ValidatePlayerLoginName(login_name, out error_message);
                    if (valid_player_name)
                    {
                        done = true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid login name: {0}", error_message);
                    }
                }
    
            }
                
            if (valid_player_name)
            {
                ///////////////////////////////////////////////
                // Attempt to login ...
                JetterCommCallbackClient callback_client = new JetterCommCallbackClient();
                InstanceContext site = new InstanceContext(callback_client);
                _client = new JetterCommServiceClient(site, "NetTcpBinding_IJetterCommService");

                CommPointInfo[] comm_point_info = _client.Join(login_name);

                if (comm_point_info == null || comm_point_info.Length == 0)
                {
                    // Problems jointing
                    Console.WriteLine("!!!! Failed to connect/join to server.");
                }
                else
                {
                    _player_login_name = login_name;
                    login_okay = true;

                    // Print communication points
                    Console.WriteLine("\nCommunication points:");
                    foreach (CommPointInfo info in comm_point_info)
                        Console.WriteLine("   Point: {0}", info.name);
                }
            }

            return login_okay;
        }

        /// <summary>
        /// Run the player command loop.
        /// </summary>
        static void PlayerCommandLoop()
        {
            bool done = false;
            while (!done)
            {
                ConsoleKeyInfo player_key = Console.ReadKey(true /* do not display the key pressed */);

                switch (player_key.Key)
                {
                    case ConsoleKey.Escape:
                        done = true;
                        break;

                    case ConsoleKey.H:
                        PrintHelp();
                        break;

                    case ConsoleKey.LeftArrow:
                        SendPlayerCommand(PilotCommandType.RudderLeft);
                        break;
                    case ConsoleKey.RightArrow:
                        SendPlayerCommand(PilotCommandType.RudderRight);
                        break;
                    case ConsoleKey.UpArrow:
                        SendPlayerCommand(PilotCommandType.ThrustUp);
                        break;
                    case ConsoleKey.DownArrow:
                        SendPlayerCommand(PilotCommandType.ThrustDown);
                        break;

                    default:
                        Console.WriteLine("Unknown command.  Press 'H' for list of commands.");
                        break;
                }
            }
        }

        /// <summary>
        /// Send player commands to server.
        /// </summary>
        /// <param name="cmd"></param>
        static void SendPlayerCommand(PilotCommandType cmd)
        {
            PilotCommand pcmd = new PilotCommand();
            pcmd.command = cmd;
            _client.PilotRequest(pcmd);
        }

        /// <summary>
        /// Print help on the console.
        /// </summary>
        static void PrintHelp()
        {
            Console.WriteLine(
                "\n" +
                "Player commands:\n" +
                "\tEsc - Exit\n" +
                "\tH - Help\n" +
                "\tRightArrow - ?? \n" +
                "\tLeftArrow - ?? \n" +
                "\tUpArrow - ?? \n" +
                "\tDownArrow - ?? \n" +
                "\n"
                );
        }


    }
}
