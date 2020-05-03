using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetterCommon
{
    public class KeeperTime
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="game_time"></param>
        /// <param name="ellapsed_game_time"></param>
        public KeeperTime(TimeSpan game_time, TimeSpan ellapsed_game_time)
        {
            _game_time = game_time;
            _ellapsed_game_time = ellapsed_game_time;
        }

        /// <summary>
        /// Return the current game time.
        /// </summary>
        public TimeSpan GameTime { get { return _game_time; } } 

        /// <summary>
        /// Return the amount of ellapsed time since the last TimeKeeper update.
        /// </summary>
        public TimeSpan EllapsedGameTime { get { return _ellapsed_game_time; } }

        /// <summary>
        /// Return the amount of ellapsed time as seconds since the last TimeKeeper update.
        /// </summary>
        public double EllapsedSeconds { get { return _ellapsed_game_time.TotalSeconds; } }

        /// <summary>
        /// True if the game time is zero (i.e. presumably the game was not started
        /// or was restarted).
        /// </summary>
        public bool IsZero { get { return (_game_time.Ticks == 0);} }

        protected TimeSpan _game_time = new TimeSpan(0);
        protected TimeSpan _ellapsed_game_time = new TimeSpan(0);
    }
}
