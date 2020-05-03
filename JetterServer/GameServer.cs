using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace JetterServer
{
   /// <summary>
   /// Singleton class that updates the game model and generates game events for game observers using 
   /// an independant thread.
   /// </summary>
   public sealed class GameServer
   {
      private static GameServer theInstance = new GameServer();
      /// <summary>
      /// Gets the singleton GameServer instance.
      /// </summary>
      /// <returns></returns>
      public static GameServer getInstance() { return theInstance; }

      static private Thread _thread;
      static private readonly object _threadLock = new Object(); // Any plain old object will do make a lock (C# adds locks to objects as needed).
      static bool _stop_signaled = false;
      static private Random rnd = new Random();

      /// <summary>
      /// Private ctor to insure class is a singleton.
      /// </summary>
      private GameServer()
      {
         _thread = new Thread(threadEntry);
         _thread.Start();
      }

      /// <summary>
      /// Signal the GameServer thread to stop and wait for it terminate and join the caller.
      /// </summary>
      public static void stopAndWaitForJoin()
      {
         lock (_threadLock)
         { 
            _stop_signaled = true;   // Signal the thread to stop running.
         };
         _thread.Join();
      }

      /// <summary>
      /// Message class for events produced by the GameServer thread.
      /// </summary>
      public class ServerMessage
      {
         public ServerMessage(string msg)
         {
            Message = msg;
         }
         public string Message { get; set; }
      }

      /// <summary>
      /// Get the oldest message from the message queue.
      /// </summary>
      /// <param name="message"></param>
      /// <returns></returns>
      public bool getMessage(ref ServerMessage message)
      {
         message = null;

         lock (_threadLock)
         {
            if (_server_messages.Count > 0)
               message = _server_messages.Dequeue();
         }

         return message != null; 
      }

      static private int _message_id = 0; // Sequential id to identify messages as they are created.
      static private Queue<ServerMessage> _server_messages = new Queue<ServerMessage>();

      /// <summary>
      /// The thread entry/starting point.
      /// </summary>
      private static void threadEntry()
      {
         bool stopping = false;

         lock (_threadLock) 
         { 
            stopping = _stop_signaled; // Check if we have been signaled to stop the GameServer thread. 
         };

         while (!stopping)
         {
            bool make_messages = false;

            lock (_threadLock) 
            { 
               make_messages = (_server_messages.Count < 5); // Check if we need more messages.
            }

            // If we need to make new messages.
            if (make_messages)
            {
               int num_new_messages = rnd.Next(1, 5); // Randomly determine the number of new messages to make.

               // Make some new messages.
               for (int msg_count = 0; msg_count < num_new_messages; ++msg_count)
               {
                  ++_message_id; // Increment the sequential message id.

                  string msg = "Message: " + _message_id;
                  ServerMessage server_msg = new ServerMessage(msg);

                  // Now that we made it, we need to add it to the queue (after locking down the shared list).
                  lock (_threadLock)
                  {
                     _server_messages.Enqueue(server_msg);
                  }
               }
            }

            lock (_threadLock)
            {
               stopping = _stop_signaled; // Check if we have been signaled that we are done. 
            }

            if (!stopping)
            {
               // Wait a random amount of time before producing another messages.
               int sleep_time = rnd.Next(100, 1200);
               Thread.Sleep(sleep_time);
            }

         }

      }
   }
}
