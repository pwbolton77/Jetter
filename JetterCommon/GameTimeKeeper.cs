using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace JetterCommon
{
   public class GameTimeKeeper
   {

      /// <summary>
      /// Instantiate a time keeper object that keeps track of the time since last called, 
      /// and converts that time into a turn count and "game time".  The ellapsed "game time" 
      /// is intended to NOT advance if game is being starved for processing time.  The turn count
      /// does not skip turns, even if starved for processing time.  So you would expect to see
      /// the game time be SLOWER than actual/realtime if this class does not get the computXXX()
      /// method called often enough.  Calling it more often then "turns_per_sec" is much perfered,
      /// and will just cause the computXXX() methods return "false" meaning the turn did not
      /// change. The intention is to help the game loop/update logic perform updates once and 
      /// only once per turn, and to keep track of virtual game time as a function of how many 
      /// turns have been processed.
      /// </summary>
      /// <param name="turns_per_sec"></param>
      public GameTimeKeeper(int turns_per_sec = 10)
      {
         _turns_per_sec = turns_per_sec;
         int _turn_time_in_millsecs = 1000 / _turns_per_sec;
         int _half_turn_time_in_millsecs = _turn_time_in_millsecs / 2;
         _turn_time_span = new TimeSpan(0 /* days */, 0 /* hours */, 0 /* min */, 0 /* sec */, _turn_time_in_millsecs);
         _half_turn_time_span = new TimeSpan(0 /* days */, 0 /* hours */, 0 /* min */, 0 /* sec */, _half_turn_time_in_millsecs);
         _turn_timer.Start();
         _running_time.Start();  // Start recording the length of actual time we have been running.
      }


      /// <summary>
      /// Compute the new turn counter.  Intended to be called from a timer tick of some sort.
      /// </summary>
      /// <param name="turn_count"></param>
      /// <returns></returns>
      public bool computeTurnCount(out long turn_count)
      {
         TimeSpan game_time;
         return computeGameTimeAndTurn(out game_time, out turn_count);
      }

      /// <summary>
      /// Compute the new game time.  Intended to be called from a timer tick of some sort.
      /// </summary>
      /// <param name="game_time"></param>
      /// <returns></returns>
      public bool computeGameTime(out TimeSpan game_time)
      {
         long turn_count;
         return computeGameTimeAndTurn(out game_time, out turn_count);
      }

      /// <summary>
      /// Compute the new game time and turn counter.  Intended to be called from a timer tick of some sort.
      /// </summary>
      /// <param name="game_time"></param>
      /// <param name="turn_count"></param>
      public bool computeGameTimeAndTurn(out TimeSpan game_time, out long turn_count)
      {
         TimeSpan _last_ellapsed = new TimeSpan(_turn_timer.Elapsed.Ticks + _last_ellapsed_remainder_time_span.Ticks);

         // See if enough ellapsed that we are in the next turn.
         bool new_turn = false;
         if (_last_ellapsed.Ticks >= _turn_time_span.Ticks)
         {
            new_turn = true;
            ++_turn_count;
            _game_time_base = new TimeSpan(_turn_count * _turn_time_span.Ticks);

            // Compute how much over the turn boundary we are
            _last_ellapsed_remainder_time_span = new TimeSpan(_last_ellapsed.Ticks - _turn_time_span.Ticks);

            // Trim the ellapsed overtime to no more than half a turn time.
            if (_last_ellapsed_remainder_time_span.Ticks > _half_turn_time_span.Ticks)
               _last_ellapsed_remainder_time_span = new TimeSpan(_half_turn_time_span.Ticks);

            // Compute the new game_time
            _game_time = new TimeSpan(_game_time_base.Ticks + _last_ellapsed_remainder_time_span.Ticks);

            // Restart the timer (reset the timer to 0 and starts it running again).
            _turn_timer.Restart();
         }
         else
         {
            ++_same_turn_count;
            // Compute the new game_time
            _game_time = new TimeSpan(_game_time_base.Ticks + _last_ellapsed.Ticks);
         }

         // Set return values.
         game_time = _game_time;
         turn_count = _turn_count;

         return new_turn;
      }

      /// <summary>
      /// The configured turns per second value.
      /// </summary>
      private int _turns_per_sec;
      public int ConfigTurnsPerSec
      {
         get { return _turns_per_sec; }
      }


      /// <summary>
      /// The actual turns per second that the GameTimeKeeper is running at.
      /// </summary>
      public double ActualTurnsPerSec
      {
         get { return 0.0; }
      }
      

      /// <summary>
      /// Return the number of type that GameTimeKeeper was called without
      /// advancing the turn count;
      /// </summary>
      private long _same_turn_count;
      public long SameTurnCount
      {
         get { return _same_turn_count; }
         private set { _same_turn_count = value; }
      }


      /// <summary>
      /// Return the actual ellapsed time time keeper has been running. 
      /// </summary>
      public TimeSpan RunningTime
      {
         get { return _running_time.Elapsed; }
      }


      /// <summary>
      /// Return the last computed turn count.
      /// </summary>
      public long TurnCount
      {
         get { return _turn_count; }
      }


      /// <summary>
      /// Return the ellapsed game time.  The ellapsed "game time" 
      /// is intended to NOT advance if game is being starved for processing time.
      /// </summary>
      public TimeSpan GameTime
      {
         get { return _game_time; }
      }
      
      private Stopwatch _turn_timer = new Stopwatch();
      private Stopwatch _running_time = new Stopwatch();    // The actual the ellapsed time the time keeper has been running. 
      private long _turn_count = 0;

      private TimeSpan _last_ellapsed_remainder_time_span = new TimeSpan(0);
      private TimeSpan _game_time_base = new TimeSpan(0);
      private TimeSpan _game_time = new TimeSpan(0);
      private TimeSpan _turn_time_span;
      private TimeSpan _half_turn_time_span;
   }
}
