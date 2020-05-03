using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetterCommon;

namespace JetterPilot
{
    #region Args
    /// <summary>
    /// Arguments for notification that a sprite was added.
    /// </summary>
    public class AddSpriteArgs : EventArgs
    {
        public AddSpriteArgs(IClientSprite _sprite)
        {
            sprite = _sprite;
        }
        public IClientSprite sprite;
    }
    public delegate void AddSpriteHandler(object sender, AddSpriteArgs e);



    /// <summary>
    /// Arguments for notification that controller turn update is about to begin.
    /// </summary>
    public class TurnUpdateBeginArgs : EventArgs
    {
        public TurnUpdateBeginArgs(KeeperTime ktime_)
        {
            ktime = ktime_;
        }
        public KeeperTime ktime;
    }
    public delegate void TurnUpdateBeginHandler(object sender, TurnUpdateBeginArgs e);

    /// <summary>
    /// Arguments for notification that controller turn update has just ended.
    /// </summary>
    public class TurnUpdateEndArgs : EventArgs
    {
        public TurnUpdateEndArgs(long _turn)
        {
            turn = _turn;
        }
        public long turn;
    }
    public delegate void TurnUpdateEndHandler(object sender, TurnUpdateEndArgs e);

#endregion

    /// <summary>
    /// Interface for observing controller actions.
    /// </summary>
    public interface IPilotControllerObserve
    {
        /// <summary>
        /// Registry for notification that a sprite was added.
        /// </summary>
        event AddSpriteHandler AddedSprite;

        /// <summary>
        /// Registry for notification that controller turn update is about to begin.
        /// </summary>
        event TurnUpdateBeginHandler TurnUpdateBegin;

        /// <summary>
        /// Registry for notification that controller turn update has just ended.
        /// </summary>
        event TurnUpdateEndHandler TurnUpdateEnd;
    }

}
