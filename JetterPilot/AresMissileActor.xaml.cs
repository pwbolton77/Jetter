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
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace JetterPilot
{
    /// <summary>
    /// Interaction logic for AresMissileActor.xaml
    /// </summary>
    public partial class AresMissileActor : ClientActor, IClientActor
    {
        public AresMissileActor(RadarViewTranslator view_translator, AresMissileSprite sprite)
            : base(view_translator, sprite)
        {
            InitializeComponent();

            _ares_missile_sprite = sprite;
            _ares_missile_sprite.NowFallingStateChange += new AresMissileSprite.StateChangeHandler(_Sprite_NowFalling);

            // Find the story boards in the xaml, so we can start/stop the animations when we want to.
            _spinning_falling_death_story = (Storyboard)TryFindResource("SpinningFallingDeathStoryKey");
        }

        private AresMissileSprite _ares_missile_sprite = null;

        // Storyboards in the xaml so can start/stop them later in code.
        private Storyboard _spinning_falling_death_story;


        /// <summary>
        /// Set/get the visibility of the radar tracking mark. 
        /// </summary>
        public override Visibility RadarTrackingMarkVisibility
        {
            get { return BogeyRadarTrackingMarkCanvas.Visibility; }
            set { BogeyRadarTrackingMarkCanvas.Visibility = value; }
        }

        /// <summary>
        /// Set/get the visibility status of the radar blip (i.e. show as blip, or as plane when in eyeshot).
        /// </summary>
        public override RadarBlipVisibilityState RadarBlipVisibility
        {
            get { return _radar_blip_visibility_state; }

            set
            {
                if (value == RadarBlipVisibilityState.Eyeshot)
                {
                    BogeyBlipCanvas.Visibility = Visibility.Hidden;
                    BogeyEyeshotVisCanvas.Visibility = Visibility.Visible;
                }
                else if (value == RadarBlipVisibilityState.Blip)
                {
                    BogeyEyeshotVisCanvas.Visibility = Visibility.Hidden;
                    BogeyBlipCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    BogeyBlipCanvas.Visibility = Visibility.Hidden;
                    BogeyEyeshotVisCanvas.Visibility = Visibility.Hidden;
                }
            }
        }
        protected RadarBlipVisibilityState _radar_blip_visibility_state = RadarBlipVisibilityState.Hidden;


        void _Sprite_NowFalling(object sender, AresMissileSprite.SpriteStateType previous_state)
        {
            _spinning_falling_death_story.Begin();
        }



    }
}
