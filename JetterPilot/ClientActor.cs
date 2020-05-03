using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace JetterPilot
{
    public abstract class ClientActor :  UserControl
    {
        /// Using a base class for WPF UserControl: http://stackoverflow.com/questions/887519/how-can-a-wpf-usercontrol-inherit-a-wpf-usercontrol
        public ClientActor(RadarViewTranslator view_translator, ClientSprite sprite)
        {
            _sprite = sprite;

            // Translator of world positions and headings to stage locations and angles.
            _view_translator = view_translator;

            // Attach observer/callbacks we need from sprite to actor. 
            _sprite.Moved += new MovedHandler(Sprite_MovedHandler);
            _sprite.Removed += new RemoveHandler(Sprite_RemovedHandler);

            ////////////////////////////////////////////////////////////
            // Insert a transformation group into the *root* UIElment (above the first canvas) to control the 
            // actors roation and  position during animation.  Keep a reference to _roate and _translate to 
            // change the heading and position on each move (when the "Moved" callback happens).
            TransformGroup _transforms = new TransformGroup();

            _rotate.CenterX = 0.0;      // Calculate the center of rotation as the center of the actor geometery.
            _rotate.CenterY = 0.0;
            _transforms.Children.Add(_rotate);        // Note: _rotate MUST be added before _translate or it will not rotate relative to actor center.

            _transforms.Children.Add(_translate);
            this.RenderTransform = _transforms;
        }

        /// <summary>
        /// Registry for notification that actor/sprite is removed from play.
        /// </summary>
        public event ActorRemoveHandler Removed;

        /// <summary>
        /// Registry for notification that actor/sprite is moved from play.
        /// </summary>
        public event ActorMoveHandler Moved;

        // Translator of world positions and headings to stage locations and angles.
        protected RadarViewTranslator _view_translator = null;

        // Translate element in the root UIElement to control the actor position in the game stage (when the "Moved" callback happens).
        protected TranslateTransform _translate = new TranslateTransform();

        // Rotation element in the root UIElement to control the actor heading in the game stage (when the "Moved" callback happens).
        protected RotateTransform _rotate = new RotateTransform();

        protected IClientSprite _sprite = null;

        #region event helper functions
        /// <summary>
        /// OnRemoved event helper.
        /// </summary>
        protected virtual void OnRemoved()
        {
            if (Removed != null)
                Removed(this);
        }

        /// <summary>
        /// OnMoved event helper.
        /// </summary>
        protected virtual void OnMoved()
        {
            if (Moved != null)
                Moved(this);
        }
        #endregion

        /// <summary>
        /// Return the IClientSprite interface of the imbedded sprite.
        /// </summary>
        public IClientSprite Sprite
        {
            get { return _sprite; }
            private set { Sprite = value; }
        }


        /// <summary>
        /// Callback when the sprite is removed.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void Sprite_RemovedHandler(object sender)
        {
            this.OnRemoved();
        }

        /// <summary>
        /// Callback when the sprite is moved.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void Sprite_MovedHandler(object sender)
        {
            Vector stage_loc = _view_translator.StageLocation(_sprite.Position);
            _translate.X = stage_loc.X;
            _translate.Y = stage_loc.Y;

            _rotate.Angle = _view_translator.StageAngle(_sprite.Heading);    // Rotate the actor based on the sprites new heading.

            _radar_distance_from_pilot = Vector.Subtract(_view_translator.RadarWorldPosition, _sprite.Position).Length;
            _is_radar_pilot_visible = (_radar_distance_from_pilot < _view_translator.RadarPilotVisibilityRange) ? true : false;
            _is_radar_scope_visible = (_radar_distance_from_pilot < _view_translator.RadarWorldLimit) ? true : false;

            // If the bogey is close enough that the pilot can see it by eye 
            if (_is_radar_pilot_visible)
                RadarBlipVisibility = RadarBlipVisibilityState.Eyeshot; // Display as visible target
            else
                RadarBlipVisibility = RadarBlipVisibilityState.Blip; // Display blip

            this.OnMoved();
        }

        /// <summary>
        /// Distance (in world coord) from the pilot/view_translator base on last update.
        /// </summary>
        public double RadarDistance
        {
            get { return _radar_distance_from_pilot; }
            set { _radar_distance_from_pilot = value; }
        }
        protected double _radar_distance_from_pilot = 0.0;

        /// <summary>
        /// True if the last move put it close enough that the pilot can see it by eye.  
        /// </summary>
        public bool IsRadarPilotVisible
        {
            get { return _is_radar_pilot_visible; }
            private set { _is_radar_pilot_visible = value; }
        }
        protected bool _is_radar_pilot_visible = false;

        /// <summary>
        /// True if the last move put it close enough that it is on the radar scope.
        /// </summary>
        public bool IsRadarScopeVisible
        {
            get { return _is_radar_scope_visible; }
            private set { _is_radar_scope_visible = value; }
        }
        protected bool _is_radar_scope_visible = false;


        /// <summary>
        /// Set/get the visibilitiy of the radar tracking mark. 
        /// </summary>
        public abstract Visibility RadarTrackingMarkVisibility
        {
            get; set;
        }

        /// <summary>
        /// Set/get the visibility status of the radar blip (i.e. show as blip, or as plane when in eyeshot).
        /// </summary>
        public abstract RadarBlipVisibilityState RadarBlipVisibility
        {
            get; set;
        }
    }
}
