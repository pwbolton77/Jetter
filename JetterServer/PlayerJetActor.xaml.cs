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

namespace JetterServer
{
    /// <summary>
    /// Interaction logic for PlayerJetActor.xaml
    /// </summary>
    public partial class PlayerJetActor : UserControl
    {
        public PlayerJetActor(PlayerJetSprite sprite, int player_number)
        {
            InitializeComponent();

            _player_number = player_number;
            // Store the sprite/model passed via dependency injection to a local variable.
            _sprite = sprite;

            // Set the sprite height and width based on actor geometery.  Sprite height and width
            // is important for the (rough) first pass on collision detection. 
            SetSpriteHeightAndWidth();

            // Setup sprite secondary boundaries for collisions based on actor geometry.
            // This is needed for the detailed second pass on collisions.
            Vector extent = new Vector(_sprite.Width / 2.0, _sprite.Height / 2.0);
            double radius = _sprite.Width / 2.0;

            Vector center = Vector.Add(extent, _sprite.Position);

            // Attach observer/callbacks we need from sprite to actor. 
            _sprite.Moved += new MovedHandler(_Sprite_Moved);
            _sprite.Removed += new RemoveHandler(_Sprite_Removed);
            _sprite.Collision += new CollisionHandler(_Sprite_Collision);

            _sprite.CalmCollision += new CollisionHandler(_Sprite_CalmCollision);
            _sprite.SpinningCollision += new CollisionHandler(_Sprite_SpinningCollision);
            _sprite.PulsingCollision += new CollisionHandler(_Sprite_PulsingCollision);

            _sprite.NowSpinningStateChange += new PlayerJetSprite.StateChangeHandler(_PlayerJetSprite_NowSpinningStateChange);
            _sprite.NowPulsingStateChange += new PlayerJetSprite.StateChangeHandler(_PlayerJetSprite_NowPulsingStateChange);
            _sprite.NowCalmStateChange += new PlayerJetSprite.StateChangeHandler(_PlayerJetSprite_NowCalmStateChange);

            _sprite.Collision += new CollisionHandler(_PlayerJetSprite_Collision);
            _sprite.NonCollision += new CollisionHandler(_PlayerJetSprite_NonCollision);


            ////////////////////////////////////////////////////////////
            // Insert a transformation group into the *root* UIElment (above the first canvas) to control the 
            // actors roation and  position during animation.  Keep a reference to _roate and _translate to 
            // change the ship heading and position on each move (when the "Moved" callback happens).
            TransformGroup _transforms = new TransformGroup();

            _rotate.CenterX = 0.0;      // Set the center of rotation
            _rotate.CenterY = 0.0;
            _transforms.Children.Add(_rotate);        // Note: _rotate MUST be added before _translate or it will not rotate relative to ship center.

            _transforms.Children.Add(_translate);
            this.RenderTransform = _transforms;

            // Find the story boards in the xaml, so we can start/stop the animations when we want to.
            _spinning_story = (Storyboard)TryFindResource("PlayerJetSpinningKey");
            _pulsing_story = (Storyboard)TryFindResource("PlayerJetPulsingKey");

            // Set the color of the jet's body based on the player number.
            JetBodyPoly.Fill = createJetColor(_player_number);
        }



        /// <summary>
        /// Choice of colors for player (i.e. jet body color).
        /// </summary>
        private System.Windows.Media.Color[] _player_colors = new System.Windows.Media.Color [] 
        { 
            System.Windows.Media.Colors.Blue, 
            System.Windows.Media.Colors.LimeGreen, 
            System.Windows.Media.Colors.BlueViolet, 
            System.Windows.Media.Colors.Firebrick, 
            System.Windows.Media.Colors.Gold, 
        };

        /// <summary>
        /// Create a color for the body of the plane based on the player number.
        /// </summary>
        /// <param name="player_number"></param>
        /// <returns></returns>
        private SolidColorBrush createJetColor(int player_number)
        {
            int color_number = player_number % _player_colors.Length;
            return new SolidColorBrush(_player_colors[color_number]);
        }

        // Player number assiged by caller (i.e. controller). 
        private int _player_number;

        // Translate element in the root UIElement to control the actor position in the game stage (when the "Moved" callback happens).
        private TranslateTransform _translate = new TranslateTransform();

        // Rotation element in the root UIElement to control the actor/ship heading in the game stage (when the "Moved" callback happens).
        private RotateTransform _rotate = new RotateTransform();

        /// <summary>
        /// Property to get the PlayerJetSprite supporting the PlayerJetActor.
        /// </summary>
        private PlayerJetSprite _sprite;
        public PlayerJetSprite Sprite
        {
            get { return _sprite; }
        }

        // Storyboards in the xaml so can start/stop them later in code.
        private Storyboard _spinning_story;
        private Storyboard _pulsing_story;


        /// <summary>
        /// Set the height and width of the sprite by looking for an expected ScaleTransform in the
        /// top level Canvas.  If the ScaleTransform is present then we can the sprite width is computed by 
        /// multiplying the (unscaled) canvas.width times the ScaleTransform X value (and similar for sprite height).
        /// </summary>
        private void SetSpriteHeightAndWidth()
        {
            if (PlayerJetCanvas.RenderTransform is TransformGroup)
            {
                TransformGroup trans_group = (TransformGroup)PlayerJetCanvas.RenderTransform as TransformGroup;
                if (trans_group.Children.Count > 0 && trans_group.Children[0] is ScaleTransform)
                {
                    ScaleTransform scale_trans = trans_group.Children[0] as ScaleTransform;
                    _sprite.Width = PlayerJetCanvas.Width * scale_trans.ScaleX;
                    _sprite.Height = PlayerJetCanvas.Height * scale_trans.ScaleY;
                }
            }
        }


        /// <summary>
        /// Callback when the sprite is moved.
        /// </summary>
        /// <param name="sender"></param>
        void _Sprite_Moved(object sender)
        {
            _translate.X = _sprite.Position.X;
            _translate.Y = _sprite.Position.Y;
            _rotate.Angle = _sprite.Heading;    // Rotate the actor based on the sprites new heading.
        }

        /// <summary>
        /// Callback when the sprite is removed.
        /// </summary>
        /// <param name="sender"></param>
        void _Sprite_Removed(object sender)
        {
            this.Visibility = System.Windows.Visibility.Hidden;   // Hide this actor.
        }

        /// <summary>
        /// Callback when the sprite is now spinning.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prev_state"></param>
        void _PlayerJetSprite_NowSpinningStateChange(object sender, PlayerJetSprite.States prev_state)
        {
            PlayerJetInnerCircle.Fill = Brushes.Yellow;
            _spinning_story.Begin();
            _pulsing_story.Stop();
        }

        /// <summary>
        /// Callback when the sprite is now pulsing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prev_state"></param>
        void _PlayerJetSprite_NowPulsingStateChange(object sender, PlayerJetSprite.States prev_state)
        {
            PlayerJetInnerCircle.Fill = Brushes.Red;
            _spinning_story.Stop();
            _pulsing_story.Begin();
        }

        /// <summary>
        /// Callback when the sprite is now calm.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="prev_state"></param>
        void _PlayerJetSprite_NowCalmStateChange(object sender, PlayerJetSprite.States prev_state)
        {
            PlayerJetInnerCircle.Fill = Brushes.White;
            _spinning_story.Stop();
            _pulsing_story.Stop();
        }

        /// <summary>
        /// Callback when the sprite collides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _PlayerJetSprite_Collision(object sender, ISprite sprite)
        {
            OuterShieldCircle.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Callback when the does NOT collide this turn.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _PlayerJetSprite_NonCollision(object sender, ISprite sprite)
        {
            OuterShieldCircle.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Callback when a calm sprite collides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _Sprite_CalmCollision(object sender, ISprite sprite)
        {
        }

        /// <summary>
        /// Callback when a spinning sprite collides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _Sprite_SpinningCollision(object sender, ISprite other_sprite)
        {
        }

        /// <summary>
        /// Callback when a pulsing sprite collides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _Sprite_PulsingCollision(object sender, ISprite other_sprite)
        {
        }

        /// <summary>
        /// Callback when the sprite collides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sprite"></param>
        void _Sprite_Collision(object sender, ISprite other_sprite)
        {
        }
    }
}
