using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetterCommon;

namespace JetterPilot
{
    /// <summary>
    /// Helper class to hold the current radar settings, and translate world positions and headings to stage locations and angles so 
    /// actors can accuratly be placed on the radar screen. 
    /// </summary>
    public class RadarViewTranslator
    {
        /// <summary>
        /// Constructor for the RadarViewTranslator.
        /// </summary>
        /// <param name="stage_center_x">The stage/canvas point to used as the center of radar screen. </param>
        /// <param name="stage_center_y"></param>
        /// <param name="stage_radius_limit">The radial extent/limit of the stage/canvas from the center point used for the radar screen.
        /// Used for scaling world coord to canvas location. </param>
        /// <param name="radar_world_limit">The world radial value that would be at the limit of radar/canvas. Used for 
        /// scaling world coord to stage/canvas location. </param>
        public RadarViewTranslator(double stage_center_x, double stage_center_y, double stage_radius_limit)
        {
            _stage_center_x = stage_center_x;
            _stage_center_y = stage_center_y;
            _stage_radius_limit = stage_radius_limit;
        }

        private double _stage_center_x = 1.0;
        private double _stage_center_y = 1.0;
        private double _stage_radius_limit = 1.0;


        //=================================================
        /* Nominal values for speed and distance, pre upgrades
            250 mph -   Saunter speed 
            310 mph -   Max speed
            350 mph -   Afterburner max speed 

            1000 miles - Map width and height
            10 miles -   Radar range limit (144 secs at 250 mph)
            3 miles -   Pilot visibility (10.8 secs at 250)
        */

        private double _map_world_size = 350.0;   // Miles
        private double _radar_world_limit = 10.0;   // Miles
        private double _radar_pilot_visibility_range = 3.0;    // Miles
        private Vector _radar_world_position = new Vector(0.0, 0.0);    // Coord miles from center of map
        private double _radar_world_heading = 0.0;  // 0.0 is facing north


        /// <summary>
        /// Return the map size (in miles), with the assumption of a square image.
        /// </summary>
        public double MapWorldSize 
        {
            get { return _map_world_size; }
        }


        /// <summary>
        /// The radial extent/limit of the stage/canvas from the center point used for the radar screen.
        /// Used for scaling world coord to canvas location.
        /// </summary>
        public double StageRadiusLimit
        {
            get { return _stage_radius_limit; }
        }

        /// <summary>
        /// The world radial value that would be at the limit of radar/canvas. Used for 
        /// scaling world coord to stage/canvas location.
        /// </summary>
        public double RadarWorldLimit 
        {
            get { return _radar_world_limit; }
            set { _radar_world_limit = value; }
        }
        

        /// <summary>
        /// Get/set the radar/pilot heading in the world.
        /// </summary>
        public double RadarWorldHeading
        {
            get { return _radar_world_heading; }
            set { _radar_world_heading = value; }
        }


        /// <summary>
        /// Get/set the radar/pilot y position in world coordinates.
        /// </summary>
        public Vector RadarWorldPosition
        {
            get { return _radar_world_position; }
            set { _radar_world_position = value; }
        }

        /// <summary>
        /// Get/set the radar/pilot x position in world coordinates.
        /// </summary>
        public double RadarWorldX
        {
            get { return _radar_world_position.X; }
            set { _radar_world_position.X = value; }
        }

        /// <summary>
        /// Get/set the radar/pilot y position in world coordinates.
        /// </summary>
        public double RadarWorldY
        {
            get { return _radar_world_position.Y; }
            set { _radar_world_position.Y = value; }
        }

        /// <summary>
        /// Pilot visibility range on the radar.
        /// </summary>
        public double RadarPilotVisibilityRange
        {
            get { return _radar_pilot_visibility_range; }
            set { _radar_pilot_visibility_range = value; }
        }


        /// <summary>
        /// Calculate the location on the stage base on the (bogey) world position
        /// and the radar current settings.
        /// </summary>
        /// <param name="world_position"></param>
        /// <returns></returns>
        public Vector StageLocation(Vector world_position)
        {
            // Given the world (pilot) heading and location as the new base, and the bogey 
            // world coordinates, find the bogey location as a x,y delta with the heading 
            // as the y-axis.
            Vector world_delta = MathHelper.rebaseWorldBogeyCoord(
                _radar_world_heading, _radar_world_position, world_position);

            // Scale the vector so it relative to the stage (i.e. if the current radar limit
            // is 100 km, and the bogey is 50km away it will be half way out on the radar screen).
            double scale = _stage_radius_limit / _radar_world_limit;
            Vector stage_delta = Vector.Multiply(scale, world_delta);

            // Move the value to the center of radar sceen.
            double stage_x = stage_delta.X + _stage_center_x;
            double stage_y = _stage_center_y - stage_delta.Y;

            return new Vector(stage_x, stage_y);
        }

        /// <summary>
        /// Return the scale for calculating radar stage distance given world distance. 
        //  I.e. multiple a world distance time this scale to get the distance in stage coords.
        /// </summary>
        public double StageScale
        {
            get
            {
                double scale = _stage_radius_limit / _radar_world_limit;
                return scale;
            }
        }


        /// <summary>
        /// Calculate the stage angle (so we set the actor's rotate transform) based on the
        /// radars heading/facing and the bogey's heading.
        /// </summary>
        /// <param name="bogey_heading"></param>
        /// <returns></returns>
        public double StageAngle(double bogey_heading)
        {
            double world_degrees = bogey_heading - _radar_world_heading;
            double stage_angle = (world_degrees + 540.0) % 360.0;

            return stage_angle;
        }

    }
}
