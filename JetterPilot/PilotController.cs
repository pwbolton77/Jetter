using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; //for Dispatcher
using System.Diagnostics;
using JetterCommon;


#region Comments
/*
Summary:
- The controller provides an event for AddedSprite, but NOT for sprite removal or moving.
- The Sprite provides an event for Removed and Moved.
- Actors, when constructed, add to the sprited Removed and Moved.  The actor then chains the sprites callback to its event Removed.
- The Stage registers with the actor Removed event.
- The AresMissileSprite class adds other custom "state handling" events like NowFallingStateChangeonly, but no other "standard" events.



=============================

-   PilotState:PilotStageUI_PreviewKeyDown() calls pilotKeyCommand().
-   PilotStage:pilotKeyCommand() calls PrimePilotSprite.keyCommand().
-   PrimePilotSprite:fireMissleAtTrackedTarget()
-   fireMissleAtTrackedTarget() queues a command to executed on the PrimePilotSprite:Move() update loop.
-   PrimePilotSprite:Move() calls execQueCommand_fireMissleAtTrackedTarget().
-   execQueCommand_fireMissleAtTrackedTarget() creates a new AresMissleSprite() and calls PilotController:AddSprite() using the IPilotControllerRequest interface.

-   PilotController:AddSprite()
	-   sprite.Removed -> PilotController:SpriteRemoveHandler added
	-   PilotController adds new sprite to _active_sprites
	-   sprite.Move(ktime) is called.
	-   PilotController:onAddedSprite() is called envoking AddedSprite registered handlers.
		-  PilotContoller.AddedSprite -> PilotStage:Controller_AddedSpriteHandler()
			-  PilotStage:Controller_AddedSpriteHandler creates a new AresMissileActor
				-  AresMissileActor constructor base ClientActor:_sprite.Moved -> AresMissleActor.Sprite_MovedHandler is added.
				-  AresMissileActor constructor base ClientActor:_sprite.Removed -> AresMissleActor.Sprite_RemovedHandler is added.
			-  PilotStage:Controller_AddedSpriteHandler: AresMissileActor actor.Moved -> PilotStage:Actor_MovedHandler is added.
			-  PilotStage:Controller_AddedSpriteHandler: AresMissileActor actor.Removed -> PilotStage:Actor_RemovedHandler is added.
			-  PilotStage:Controller_AddedSpriteHandler: AresMissileActor is added to PilotStage:_bogey_actors list.
			-  PilotStage:Controller_AddedSpriteHandler: AresMissileActor is added to stage children (RadarTargetInsertionCavas.Children.Add()


-  AresMissileSprite determines it is to be removed (e.g. out of fuel) during Move().
-  AresMissileSprite:moveDuringFallingState() is called.
-  AresMissileSprite:moveDuringFallingState calls ClientSprite:Remove().
-  ClientSprite:Remove calls CLientSprite:onRemoved().
-  ClientSprite:onRemoved() -> envokes handlers registered on ClientSprite:Remove event.
	-  PilotController:SpriteRemoveHandler is called
		-  PilotController:SpriteRemoveHandler removes the sprite from PilotContoller:_active_sprites list.
	-  AresMissileActor:Sprite_RemovedHandler calls ClientActor:OnRemoved().
		-  PilotStage:Actor_RemovedHandler is called by ClientActor:OnRemoved
		-  PilotStage:Actor_RemovedHandler clears PilotStage:TrackingBogey
*/		
#endregion

namespace JetterPilot
{
    public class PilotController : INotifyPropertyChanged, IPilotKeyCommands, IPilotControllerObserve, IPilotControllerRequest 
    {
        private System.Random _randNum = new System.Random();

        // List of sprites for computing next move.
        private Dictionary<long /* controller id */, IClientSprite> _active_sprites = new Dictionary<long, IClientSprite>();

        // private Dictionary<string /* pilot name */, PilotControllerInfo> pilot_dictionary = new Dictionary<string, PilotControllerInfo>();

        /// <summary>
        /// Public event notification when controller properties change.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Public event for notification that a sprite was added.
        /// </summary>
        public event AddSpriteHandler AddedSprite;

        /// <summary>
        /// Public event for notification that controller turn update is about to begin.
        /// </summary>
        public event TurnUpdateBeginHandler TurnUpdateBegin;

        /// <summary>
        /// Public event for notification that controller turn update has just ended.
        /// </summary>
        public event TurnUpdateEndHandler TurnUpdateEnd;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game_stage"></param>
        public PilotController()
        {
        }

        public void initPilotController()
        {
            PrimePilot_Drop(_time_keeper.KeeperTime,
                new Vector(0.0, 0.0) /* bogey_position - world coords */,
                0.0 /* heading */, MathHelper.mphToMpsec(500.0) /* speed */);  // @@@@

            // @@@ Bogey Drop
            BasicBogey_Drop(_time_keeper.KeeperTime,
                new Vector(-0.0, 8.0) /* bogey_position - world coords */,
                92.0 /* bogey_heading */, MathHelper.mphToMpsec(0.0) /* bogey_speed */);

            BasicBogey_Drop(_time_keeper.KeeperTime,
                new Vector(-5.0, 3.0) /* bogey_position - world coords */,
                91.0 /* bogey_heading */, MathHelper.mphToMpsec(200.0) /* bogey_speed */);

            BasicBogey_Drop(_time_keeper.KeeperTime,
                new Vector(-2.0, 0.0) /* bogey_position - world coords */,
                90.0 /* bogey_heading */, MathHelper.mphToMpsec(200.0)/* bogey_speed */);

            _time_keeper.Start();
        }

        private TimeKeeper _time_keeper = new TimeKeeper(30L /* max_updates_per_second */, 5L /* min_game_time_updates_per_second */);

        /// <summary>
        /// Method to support PropertyChanged events.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void onPropertyChanged(string propertyName)
        {
            // Callback to clients that registered event handlers for property changes.
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// Method to support AddedSprite events.
        /// </summary>
        protected void onAddedSprite(IClientSprite sprite)
        {
            // Callback to clients that registered event handlers
            if (this.AddedSprite != null)
                this.AddedSprite(this, new AddSpriteArgs(sprite));
        }

        /// <summary>
        /// Method to support TurnUpdateBegin events.
        /// </summary>
        /// <param name="_turn_number"></param>
        protected void onTurnUpdateBegin(KeeperTime ktime)
        {
            // Callback to clients that registered event handlers
            if (this.TurnUpdateBegin != null)
                this.TurnUpdateBegin(this, new TurnUpdateBeginArgs(ktime));
        }

        /// <summary>
        /// Method to support TurnUpdateEnd events.
        /// </summary>
        /// <param name="_turn_number"></param>
        protected void onTurnUpdateEnd(long _turn_number)
        {
            // Callback to clients that registered event handlers
            if (this.TurnUpdateEnd != null)
                this.TurnUpdateEnd(this, new TurnUpdateEndArgs(_turn_number));
        }

        /// <summary>
        /// The turn number tracked by the Controller update method.
        /// </summary>
        private long _turn_number = 0;
        public long TurnNumber
        {
            get { return _turn_number; }
            set
            {
                if (_turn_number != value)
                {
                    _turn_number = value;
                    onPropertyChanged("TurnNumber"); // Call event handlers because property changed.
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        public void renderHandler()
        {
            KeeperTime ktime;
            long update_counter;

            // @@ _time_keeper.GameTimeScale = 4.0;   // @@@@
            bool new_turn = _time_keeper.timeToUpdate(out ktime, out update_counter);

            if (new_turn)
            {
                ///////////////////////////////////////
                // Update for the game logic
                UpdateTurn(ktime);
            }
        }




        /// <summary>
        /// Update the game when it is next turn (based on timer ticks).
        /// </summary>
        /// <param name="ktime"></param>
        public void UpdateTurn(KeeperTime ktime)
        {
            ++_turn_number;
            onTurnUpdateBegin(ktime);

            // Temp list of sprites for computing next move.
            // (When sprites are removed, they are removed from the master list
            // of active sprites).
            List<IClientSprite> _temp_sprites = _active_sprites.Values.ToList();

            MoveActiveSprites(ktime, _temp_sprites); // Move sprites.
            InteractSprites(ktime, _temp_sprites);  // Have the sprites interact with each other.

            onTurnUpdateEnd(_turn_number);
        }


        /// <summary>
        /// Move all standard sprites and put them in the new grid-cell locations.
        /// </summary>
        private void MoveActiveSprites(KeeperTime ktime, List<IClientSprite> sprites)
        {
            foreach (var sprite in sprites)
            {
                sprite.Move(ktime);
            }
        }


        /// <summary>
        /// Have all standard sprites interact.
        /// </summary>
        private void InteractSprites(KeeperTime ktime, List<IClientSprite> sprites)
        {
            for (var sprite_index = 0; sprite_index < sprites.Count - 1; ++sprite_index)
            {
                for (var other_index = sprite_index + 1; other_index < sprites.Count; ++other_index)
                {
                    IClientSprite sprite1 = sprites[sprite_index];
                    IClientSprite sprite2 = sprites[other_index];
                    bool interact = interactCheck(ktime, sprite1, sprite2);
                    if (interact == true)
                    {
                        IClientSprite primary_sprite = null;
                        IClientSprite secondary_sprite = null;
                        prioritizeSpriteInteraction(sprite1, sprite2, out primary_sprite, out secondary_sprite);

                        primary_sprite.Interact(ktime, secondary_sprite, true  /* is_primary_interation */);
                        secondary_sprite.Interact(ktime, primary_sprite, false /* is_primary_interation */);
                    }
                }
            }
        }

        private bool interactCheck(KeeperTime ktime, IClientSprite sprite, IClientSprite other_sprite)
        {
            // @@@ Temp interactCheck - do logic here yet.
            return true;
        }

        private void prioritizeSpriteInteraction(IClientSprite sprite1, IClientSprite sprite2,
            out IClientSprite primary_sprite, out IClientSprite secondary_sprite)
        {
            // @@@ Temp prioritizeSpriteInteraction  - do logic here yet.
            primary_sprite = sprite1;
            secondary_sprite = sprite2;
        }


        /// <summary>
        /// Drop a new sprite into the game.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <param name="speed"></param>
        void BasicBogey_Drop(KeeperTime ktime, Vector position, double heading, double speed)
        {
            //////////////////////////////////
            // Make a new sprite.
            BasicBogeySprite sprite = new BasicBogeySprite(ktime, (IPilotControllerRequest) this,
                position, heading, speed);

            AddSprite(ktime, sprite); // Add a sprite to the controller. 
        }

        void PrimePilot_Drop(KeeperTime ktime, Vector position, double heading, double speed)
        {
            //////////////////////////////////
            // Make a new sprite.
            PilotSprite sprite = new PrimePilotSprite(ktime, (IPilotControllerRequest) this, position, heading, speed);

            AddSprite(ktime, sprite); // Add a sprite to the controller. 
        }

        /// <summary>
        /// Add a sprite to the controller. 
        /// </summary>
        /// <param name="ktime"></param>
        /// <param name="sprite"></param>
        /// <returns>controler id for the sprite</returns>
        public long AddSprite(KeeperTime ktime, IClientSprite sprite)
        {
            long controller_id = createControllerId();  // Assign the sprite a controller id.
            sprite.ControllerId = controller_id;  // Assign the sprite a controller id.

            _active_sprites.Add(controller_id, sprite);   // Add the sprite to the list of moving sprites.

            sprite.Removed += new RemoveHandler(SpriteRemoveHandler);

            sprite.Move(ktime);

            // Inform observers a new sprite was made (so they can create elements that display the sprite.)
            onAddedSprite(sprite);  // Support the AddedSprite event.

            return controller_id;
        }


        /// <summary>
        /// Return a reference to a sprite given the controller id.
        /// </summary>
        /// <param name="controller_id"></param>
        /// <returns></returns>
        public IClientSprite getSprite(long controller_id)
        { 
            IClientSprite result = null;

            if (! _active_sprites.TryGetValue(controller_id, out result))
                result = null;

            return result;
        }

        /// <summary>
        /// Create a new unique controller id.
        /// </summary>
        /// <returns></returns>
        long createControllerId()
        {
            return ++_controller_id_count;  // 
        }
        private long _controller_id_count = 0;  // For creating controller ids.

        /// <summary>
        /// Callback handler when a sprite says its being removed from the game.
        /// </summary>
        /// <param name="sender"></param>
        void SpriteRemoveHandler(object sender)
        {
            IClientSprite sprite = sender as IClientSprite;
            _active_sprites.Remove(sprite.ControllerId);  // Remove the sprite from the list of moving sprites.
        }



        /// <summary>
        /// Handle pilot commands that require the controller to do something.
        /// </summary>
        /// <param name="key_cmd"></param>
        public void pilotKeyCommand(PilotKeyCommands key_cmd)
        {
            switch (key_cmd)
            {
                case PilotKeyCommands.RotateClockwiseKeyDown:
                    break;

                case PilotKeyCommands.RotateClockwiseKeyUp:
                    break;

                case PilotKeyCommands.RotateCounterClockwiseKeyDown:
                    break;

                case PilotKeyCommands.RotateCounterClockwiseKeyUp:
                    break;

                default:
                    break;
            }
        }


    }
}
