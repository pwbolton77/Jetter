using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace JetterPilot
{
    public class PrimePilotSprite : PilotSprite, IPrimePilotCommand
    {
        public PrimePilotSprite(KeeperTime ktime, IPilotControllerRequest controller_request, Vector position, double heading,
            double speed = 0.0, double acceleration = 0.0) :
            base(ktime, controller_request, position, heading, speed, acceleration)
        {
            DebugCommandsOn = false;
        }


        #region IPrimePilotCommand
        /// <summary>
        /// Enable/disable extended debug commands.
        /// </summary>
        public bool DebugCommandsOn { get; set; }

        /// <summary>
        // Process a key command.
        /// </summary>
        /// <param name="command"></param>


        /// <summary>
        // Process a key command.
        /// </summary>
        /// <param name="key_cmd"></param>
        /// <returns>handled</returns>
        public bool keyCommand(PilotKeyCommands key_cmd)
        {
            bool handled = false;

            /////////////////////////////////
            // Let the debug attempt to handle the command first.
            if (DebugCommandsOn)
            {
                handled = true;
                switch (key_cmd)
                {
                    case PilotKeyCommands.RotateClockwiseKeyDown:
                        Heading = MathHelper.normalizeAngle(Heading + 5.0);
                        break;
                    case PilotKeyCommands.RotateClockwiseKeyUp:
                        break;

                    case PilotKeyCommands.RotateCounterClockwiseKeyDown:
                        Heading = MathHelper.normalizeAngle(Heading - 5.0);
                        break;
                    case PilotKeyCommands.RotateCounterClockwiseKeyUp:
                        break;

                    default:
                        handled = false;
                        break;
                }
            }


            /////////////////////////////////
            if (!handled)
            {
                handled = true;
                switch (key_cmd)
                {
                    //case PilotKeyCommands.RotateClockwiseKeyDown:
                    //    Heading = MathHelper.NormalizeAngle(Heading + 5.0);
                    //    break;
                    //case PilotKeyCommands.RotateClockwiseKeyUp:
                    //    break;

                    case PilotKeyCommands.FireMissileAtTrackedTargetKeyDown:
                        queFireMissleAtTrackedTarget();
                        break;
                    case PilotKeyCommands.FireMissileAtTrackedTargetKeyUp:
                        break;

                    case PilotKeyCommands.FireMissileUnTrackedKeyDown:
                        queFireMissleUnTracked();
                        break;
                    case PilotKeyCommands.FireMissileUnTrackedKeyUp:
                        break;

                    default:
                        handled = false;
                        break;
                }
            }

            return handled;
        }
        #endregion

        /// <summary>
        // Process a command to fire a missile at the tracked target.
        /// </summary>
        public void queFireMissleAtTrackedTarget()
        {
            if (_tracked_target != null)
                pilot_cmd_que.Enqueue(new XFireMissleTrackedPilotCmd(TrackedTarget.ControllerId));
        }

        /// <summary>
        // Process a command to fire a missile that is not tracking anything.
        /// </summary>
        public void queFireMissleUnTracked()
        {
            Trace.WriteLine("!!! Fired untracked missile !!!- NOT Implemented."); // @@@@
        }


        void execFireMissleAtTrackedTarget(KeeperTime ktime, XFireMissleTrackedPilotCmd command)
        {
            // If the command is active, and we have a valid bogey ...
            if (command.tracked_target_id != 0)
            {
                // Make a new sprite.
                AresMissileSprite sprite = new AresMissileSprite(ktime, _controller_request,
                    Position, Heading, Speed * 4.0, 0.0, _controller_request.getSprite (command.tracked_target_id));

                // Tell the controller to add it to the game.
                _controller_request.AddSprite(ktime, sprite);
            }
        }



        private IClientSprite _tracked_target = null;

        public IClientSprite TrackedTarget
        {
            get { return _tracked_target; }
            set { _tracked_target = value; }
        }


        /// <summary>
        /// Perform move processing (call once per turn).
        /// </summary>
        public override void Move(KeeperTime ktime)
        {
            execPilotQueCommands(ktime);    // Execute queued commands that need to executed in the Move/update loop.

            base.Move(ktime);
        }

        /// <summary>
        /// Execute any commands the pilot queued that must done during the Move/update loop.
        /// </summary>
        /// <param name="ktime"></param>
        void execPilotQueCommands(KeeperTime ktime)
        {

            // Loop through and get the queued pilot commands.
            XPilotCommand pilot_command = null;
            while (pilot_cmd_que.TryDequeue(out pilot_command))
            {
                switch (pilot_command.command)
                {
                    case XPilotCommandType.FireMissleTracked:
                        execFireMissleAtTrackedTarget(ktime, pilot_command as XFireMissleTrackedPilotCmd);
                        break;

                    default:
                        break;
                }
            }

        }

        // Construct a ConcurrentQueue for pilot commands that need to be executed during the Move/update loop.
        private ConcurrentQueue<XPilotCommand> pilot_cmd_que = new ConcurrentQueue<XPilotCommand>();

    }


    // @@@@@@@@@@@@@ The following XPilotCommandType needs to put the in the JetterComServiceLibrary.PilotCommands.cs file once controller identifiers are implemented.
    public enum XPilotCommandType
    {
        RudderNeutral,
        RudderRight,
        RudderLeft,
        ThrustNeutral,
        ThrustUp,
        ThrustDown,
        FireMissleTracked,
    };

    public class XPilotCommand
    {
        public XPilotCommandType command;  
    }

    public class XFireMissleTrackedPilotCmd : XPilotCommand
    {
        public XFireMissleTrackedPilotCmd(long tracked_target_id_)
        {
            command = XPilotCommandType.FireMissleTracked;  
            tracked_target_id = tracked_target_id_;
        }
        public long tracked_target_id = 0;
    }

}
