using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JetterCommon
{
    public class MathHelper
    {
        public static Vector gravity_direction = MathHelper.createVectFromAngle(0);
        public static Vector down_direction = gravity_direction;
        public static Vector up_direction = -down_direction;
        public static Vector right_direction = MathHelper.createVectFromAngle(90);
        public static Vector left_direction = -right_direction;
        public static Vector stop_velocity = stop_velocity = new Vector(0.0, 0.0);

        static public Vector createVectFromAngle(double angleInDegrees, double length)
        {
            double x = Math.Sin(convertToRadians(180 - angleInDegrees)) * length;
            double y = Math.Cos(convertToRadians(180 - angleInDegrees)) * length;
            return new Vector(x, y);
        }
        static public Vector createVectFromAngle(double angleInDegrees)
        {
            double x = Math.Sin(convertToRadians(180 - angleInDegrees));
            double y = Math.Cos(convertToRadians(180 - angleInDegrees));
            return new Vector(x, y);
        }

        /// <summary>
        /// Create a unit vector based on an heading given in degrees.
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        static public Vector createVectFromHeading(double angleInDegrees)
        {
            double x = Math.Sin(convertToRadians(180 - angleInDegrees));
            double y = -Math.Cos(convertToRadians(180 - angleInDegrees));
            return new Vector(x, y);
        }

        /// <summary>
        /// Create a (scaled) vector based on an heading given in degrees.
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static public Vector createVectFromHeading(double angleInDegrees, double length)
        {
            double x = Math.Sin(convertToRadians(180 - angleInDegrees)) * length;
            double y = -Math.Cos(convertToRadians(180 - angleInDegrees)) * length;
            return new Vector(x, y);
        }


        /// <summary>
        /// Convert degrees to radians.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        static public double convertToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        /// <summary>
        /// Convert radians to degrees.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        static public double convertToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }

        /// <summary>
        /// Normalize the angle so it is between 0.0 and 360.0 degrees (even 
        /// if the angle is large negative number).
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        static public double normalizeAngle(double angle)
        {
            double new_angle = angle % 360.0;
            new_angle = (new_angle + 360.0) % 360.0;

            return new_angle;
        }

        /// <summary>
        /// Calculate the heading given a (delta) vector (e.g. delta = bogey_position - pilot_position).
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        static public double headingFromVector(Vector delta)
        {
            // Examples follow ...
            //double temp000 = MathHelper.ConvertToDegrees(Math.Atan2(0 /* X */, 1 /* Y */));
            //double temp090 = MathHelper.ConvertToDegrees(Math.Atan2(1 /* X */, 0 /* Y */));
            //double temp180 = MathHelper.ConvertToDegrees(Math.Atan2(0 /* X */, -1 /* Y */));
            //double temp270 = MathHelper.ConvertToDegrees(Math.Atan2(-1 /* X */, 0/* Y */));
            //double tempNaN = MathHelper.ConvertToDegrees(Math.Atan2(0 /* X */, 0/* Y */));

            return normalizeAngle(  convertToDegrees(Math.Atan2(delta.X, delta.Y)) );
        }

        /// <summary>
        /// Given the (pilot) heading and location as the new base, and the bogey world coordinates,
        /// find the bogey coordinates as a x,y delta with the heading as the y-axis.  (Its still world coordinates, it just
        /// uses the base/pilot location and heading as the frame of reference.)
        /// </summary>
        /// <param name="base_heading_angle"></param>
        /// <param name="base_loc"></param>
        /// <param name="bogey_loc"></param>
        /// <returns></returns>
        public static Vector rebaseWorldBogeyCoord(double base_heading_angle, Vector base_loc, Vector bogey_loc)
        {
            Vector delta = Vector.Subtract(bogey_loc, base_loc);
            Vector base_heading_vec = createVectFromHeading(base_heading_angle);

            double x_cross = Vector.CrossProduct(delta, base_heading_vec);  // The cross product of normalized parallel vectors is 0.0
            double y_dot = Vector.Multiply(base_heading_vec, delta);        // The dot product of normalized parallel vectors is 1.0 
            return new Vector(x_cross, y_dot);
        }

        /// <summary>
        /// Using the (pilot) heading as the new reference, and given the bogey heading (old coord system), find the vector 
        /// that describes the bogey's heading as a unit vector in the new coordinate system.
        /// </summary>
        /// <param name="base_heading_angle"></param>
        /// <param name="bogey_heading_angle"></param>
        /// <returns></returns>
        public static Vector rebaseWorldBogeyHeadingVector(double base_heading_angle, double bogey_heading_angle)
        {
            Vector base_vec = createVectFromHeading(base_heading_angle);
            Vector bogey_vec = createVectFromHeading(bogey_heading_angle);

            double x_cross = Vector.CrossProduct(base_vec, bogey_vec);  // The cross product of normalized parallel vectors is 0.0
            double y_dot = Vector.Multiply(base_vec, bogey_vec);        // The dot product of normalized parallel vectors is 1.0 

            return new Vector(x_cross, y_dot);
        }

        /// <summary>
        /// Return the signed difference in heading between old and new values.
        /// </summary>
        /// <param name="new_heading"></param>
        /// <param name="old_heading"></param>
        /// <returns></returns>
        public static double headingDeltaSigned(double new_heading, double old_heading)
        {
            double delta_heading = MathHelper.normalizeAngle((360.0 + new_heading) - old_heading);

            if (delta_heading > 180)
                delta_heading = - (360 - delta_heading) ;

            //double delta_heading = new_heading - old_heading;

            //if (delta_heading <= -180.0)
            //    delta_heading = delta_heading + 360.0;
            //else if (delta_heading >= 180.0)
            //    delta_heading = delta_heading - 180;

            return delta_heading;
        }

        /// <summary>
        /// Return the new heading limited by the maximum amount the heading can change.
        /// </summary>
        /// <param name="new_direct_heading"></param>
        /// <param name="old_heading"></param>
        /// <param name="max_heading_change"></param>
        /// <returns></returns>
        public static double headingLimitedByMaxChange(double new_direct_heading, double old_heading, double max_heading_change)
        {
            double direct_delta = headingDeltaSigned(new_direct_heading, old_heading);
            double max_delta = Math.Min(max_heading_change, direct_delta);
            max_delta = Math.Max(-max_heading_change, max_delta);

            double new_limited_heading = MathHelper.normalizeAngle(old_heading + max_delta);
            return new_limited_heading;
        }


        /// <summary>
        /// Return miles per second given miles per hour.
        /// </summary>
        /// <param name="mph"></param>
        /// <returns></returns>
        public static double mphToMpsec(double mph)
            { return mph / 3600; }

        #region Vector Test
        // Temp vector test 
        static public void vectorTest()
        {
            Vector v1 = new Vector(2.0, 4.0);
            double v1len = v1.Length;
            Vector nv1 = v1;
            nv1.Normalize();
            double nv1len = nv1.Length;   // Confirmed - The length of a normalized vector is 1.0 (it is never negative).

            Vector v2 = new Vector(1.0, 0.0);
            Vector v3 = new Vector(0.0, 3.0);
            double cross1 = Vector.CrossProduct(v2, v3); // Confirmed - The cross product of normalized perpendicular vectors is 1.0 (or -1.0).
            // Confirmed - The cross product of non-normalized perpendicular vectors is the 1.0 * v1.length * v2.length 
            double dot1 = Vector.Multiply(v2, v3);       // Confirmed - The dot product of normalized perpendicular vectors is 0.0 

            Vector v4 = new Vector(1.0, 0.0);
            Vector v5 = new Vector(3.0, 0.0);
            double cross2 = Vector.CrossProduct(v4, v5); // Confirmed - The cross product of normalized parallel vectors is 0.0
            double dot2 = Vector.Multiply(v4, v5);       // Confirmed - The dot product of normalized parallel vectors is 1.0 
            // Confirmed - The dot product of parallel vectors is 1.0 * v4.length * v5.length
        }
        #endregion
    }
}
