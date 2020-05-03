using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace JetterCommon
{
    /// <summary>
    /// Class that keeps track of game time and (waits) some minimum length of
    /// time before telling the controller to run the update loop.
    /// </summary>
    public class TimeKeeper
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="min_real_time_per_update">Minimum amount of real time to wait before calling for an update.</param>
        /// <param name="max_game_time_per_update">Maximum amount of game time to advance per update (helps with deubgging).</param>
        public TimeKeeper(TimeSpan min_real_time_per_update, TimeSpan max_game_time_per_update)
        {
            _rt_min_time_per_update = min_real_time_per_update;
            _max_game_time_per_update = max_game_time_per_update;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="max_real_time_updates_per_second"></param>
        public TimeKeeper(long max_real_time_updates_per_second, long min_game_time_updates_per_second)
        {
            int turn_time_in_millsecs = (int)((1000.0 / (double) max_real_time_updates_per_second) + 0.5);
            _rt_min_time_per_update = new TimeSpan(0 /* days */, 0 /* hours */, 0 /* min */, 0 /* sec */, turn_time_in_millsecs);

            turn_time_in_millsecs = (int)((1000.0 / (double) min_game_time_updates_per_second) + 0.5);
            _max_game_time_per_update =  new TimeSpan(0 /* days */, 0 /* hours */, 0 /* min */, 0 /* sec */, turn_time_in_millsecs);
        }

        /// <summary>
        /// Minimum amount of real time that must pass before the time keeper says
        /// an update loop execution is in order.
        /// </summary>
        public TimeSpan MinRealTimePerUpdate
        {
            get { return _rt_min_time_per_update; }
            private set { _rt_min_time_per_update = value; }
        }
        private TimeSpan _rt_min_time_per_update;

        /// <summary>
        /// Maximum amount of game time to advance per update.
        /// </summary>
        public TimeSpan MaxGameTimePerUpdate
        {
            get { return _max_game_time_per_update; }
            private set { _max_game_time_per_update = value; }
        }
        private TimeSpan _max_game_time_per_update = 
            new TimeSpan(0 /* days */, 0 /* hours */, 0 /* min */, 0 /* sec */, 20 /* millsecs */);

        /// <summary>
        /// Count of the number of times timeToUpdate method returned true.
        /// </summary>
        public long UpdateCounter
        {
            get { return _update_counter; }
            private set { _update_counter = value; }
        }
        private long _update_counter;


        /// <summary>
        /// The current game time.  Returns the game time that timeToUpdate last returned true.  This
        /// will not advance if the TimeKeeper is paused.
        /// </summary>
        public TimeSpan GameTime
        {
            get { return _game_time; }
            private set { _game_time = value; }
        }
        private TimeSpan _game_time = new TimeSpan(0 /* ticks */);

        /// <summary>
        /// The previous game time when the time keeper said it was time to update.  
        /// </summary>
        public TimeSpan PreviousGameTime
        {
            get { return _previous_game_time; }
            private set { _previous_game_time = value; }
        }
        private TimeSpan _previous_game_time = new TimeSpan(0 /* ticks */);

        private Stopwatch _rt_timer = new Stopwatch();    // The ellapsed (real) time the time keeper has been running. 


        /// <summary>
        /// Return true if enough time has passed that a turn update should occur.  The current game time and 
        /// update counter is also returned.
        /// </summary>
        /// <param name="game_time"></param>
        /// <param name="update_counter"></param>
        /// <returns></returns>
        public bool timeToUpdate(out KeeperTime ktime, out long update_counter)        
        {
            bool result = false;

            if (_rt_timer.IsRunning)
            {
                TimeSpan rt_update_check = _rt_timer.Elapsed;
                TimeSpan rt_delta = rt_update_check - _rt_last_update_stamp;
                if (rt_delta >= _rt_min_time_per_update)
                {
                    // Enough real time has passed, so update the game time.
                    result = true;
                    _rt_last_update_stamp = rt_update_check;
                    ++_update_counter;
                    _previous_game_time = _game_time;

                    // Scale the delta-real-time and update the game time.
                    TimeSpan _game_time_delta = new TimeSpan ((long) (rt_delta.Ticks * _game_time_scale));

                    long _game_time_delta_ticks = 
                        Math.Min(_game_time_delta.Ticks,  (long) (_max_game_time_per_update.Ticks * _game_time_scale + 0.5));

                    _game_time += new TimeSpan(_game_time_delta_ticks);
                }
            }

            update_counter = _update_counter;
            ktime = new KeeperTime(_game_time, _game_time - _previous_game_time);

            return result;
        }


        /// <summary>
        /// Return the last KeeperTime value calculated by timeToUpdate.
        /// </summary>
        public KeeperTime KeeperTime
        {
            get { return new KeeperTime(_game_time, _game_time - _previous_game_time); }
        }
        

        /// <summary>
        /// Get or set the scaling of real time to game time.  A scale
        /// factor of 2.0 will make the game go twice as fast.
        /// </summary>
        public double GameTimeScale
        {
            get { return _game_time_scale = 1.0; }
            set { _game_time_scale = value; }
        }
        private double _game_time_scale = 1.0;

        TimeSpan _rt_last_update_stamp = new TimeSpan(0 /* ticks */);

        /// <summary>
        /// Start the time keeper.
        /// </summary>
        public void Start()
        {
            _rt_timer.Start();
        }

        /// <summary>
        /// Stop the time keeper. 
        /// </summary>
        public void Stop()
        {
            _rt_timer.Stop();
        }

        /// <summary>
        /// Is the time keeper (stop watch) running.
        /// </summary>
        public bool IsRunning { get { return _rt_timer.IsRunning; } }

    }
}
