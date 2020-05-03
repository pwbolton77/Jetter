using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterPilot
{
    public enum PilotKeyCommands
    {
        Invalid,

        MoveLeftKeyDown,
        MoveLeftKeyUp,

        MoveRightKeyDown,
        MoveRightKeyUp,

        MoveUpKeyDown,
        MoveUpKeyUp,

        MoveDownKeyDown,
        MoveDownKeyUp,

        NextTargetKeyDown,
        NextTargetKeyUp,

        FireBulletKeyDown,
        FireBulletKeyUp,

        RotateClockwiseKeyDown,
        RotateClockwiseKeyUp,

        RotateCounterClockwiseKeyDown,
        RotateCounterClockwiseKeyUp,

        MainThrusterKeyDown,
        MainThrusterKeyUp,

        DeadStopKeyDown,
        DeadStopKeyUp,

        RadarRangeIncreaseKeyDown,
        RadarRangeIncreaseKeyUp,

        RadarRangeDecreaseKeyDown,
        RadarRangeDecreaseKeyUp,

        RadarTrackNextBogeyKeyDown,
        RadarTrackNextBogeyKeyUp,

        FireMissileAtTrackedTargetKeyDown,
        FireMissileAtTrackedTargetKeyUp,

        FireMissileUnTrackedKeyDown,
        FireMissileUnTrackedKeyUp,
    }

    public interface IPilotKeyCommands
    {
        void pilotKeyCommand(PilotKeyCommands key_cmd);
    }
}
