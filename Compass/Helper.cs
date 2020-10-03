using UnityEngine;

namespace Compass
{
    public enum Orientation
    {
        Forward,
        Right,
        Backward,
        Left,

        ForwardRight,
        ForwardLeft,
        BackwardRight,
        BackwardLeft,

        ERROR
    }

    public static class Helper
    {
        public static readonly int[] angles = new int[] { 0, 90, 180 };  //The result of the Unity's method to calculate the angle is never greater than 180
        public static readonly int angleDiff = 20;  //Diff between angles ~90/4. Example of use: Forward = [-angleDiff, angleDiff]

        /// <summary>
        /// Returns the Direction(vector) between the TargetPosition and SourcePosition
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <param name="SourcePosition"></param>
        /// <returns></returns>
        public static Pipliz.Vector3Int GetDirection(Pipliz.Vector3Int TargetPosition, Pipliz.Vector3Int SourcePosition)
        {
            return TargetPosition - SourcePosition;
        }

        /// <summary>
        /// Return the angle (degrees º) between Target (Vector) and Source (Vector) WITHOUT considering the Y AXIS
        /// The result is between [-180, 180]
        /// Positive in a clockwise direction and negative in an anti-clockwise direction.
        /// </summary>
        /// <param name="TargetDirection"></param>
        /// <param name="SourceDirection"></param>
        /// <returns></returns>
        public static int GetAngle(Pipliz.Vector3Int TargetDirection, Pipliz.Vector3Int SourceDirection)
        {
            return (int)UnityEngine.Vector3.SignedAngle(new UnityEngine.Vector3(SourceDirection.x, SourceDirection.z), new UnityEngine.Vector3(TargetDirection.x, TargetDirection.z), Vector3.forward);
        }

        // Returns the Orientation <player> to
        /// <summary>
        /// Returns the relative orientation of the player to the target DIRECTION (vector)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="TargetDirection"></param>
        /// <returns></returns>
        public static Orientation GetOrientationToDirectionFromPlayer(Players.Player player, Pipliz.Vector3Int TargetDirection)
        {
            int angle = GetAngle(TargetDirection, new Pipliz.Vector3Int(player.Forward));

            /*
             * Fuzzy Logic over the angle using angleDiff to define the ranges
             * angleDiff = 20 ~90/4 in this way each direction is ~45º
             * 
             * [-20, 20] FORWARD
             * ]20, 70[ FORWARD + RIGHT
             * [70, 110] RIGHT
             * ]110, 160[ BACKWARD + RIGHT
             * [160, 180] & [-160, -180] BACKWARD
             * ]-160, -110[ BACKWARD + LEFT
             * [-110, -70] LEFT
             * ]-70, -20[ FORWARD + LEFT
             * 
             * IMPORTANT: When sending Left / Right you have to send the opposite one that has been calculate.
             * If you are looking to the right you have to turn left. When looking back NOT TRANSFORM
             */

            if (angle >= -angleDiff && angle <= angleDiff)  // [-20,20] = FORWARD
                return Orientation.Forward;

            if (angle > angles[0] + angleDiff && angle < angles[1] - angleDiff)         //]20, 70[ = FORWARD + RIGHT -> LEFT
                return Orientation.ForwardLeft;

            if (angle >= angles[1] - angleDiff && angle <= angles[1] + angleDiff)       //[70, 110] = RIGHT -> LEFT
                return Orientation.Left;

            if (angle > angles[1] + angleDiff && angle < angles[2] - angleDiff)         //]110, 160[ = BACKWARD + RIGHT
                return Orientation.BackwardRight;

            if (angle >= angles[2] - angleDiff || angle <= -angles[2] + angleDiff)      //[160, 180] && [-180, -160] = BACKWARD
                return Orientation.Backward;

            if (angle > -angles[2] + angleDiff && angle < -angles[1] - angleDiff)       //]-160, -110[ = BACKWARD + LEFT
                return Orientation.BackwardLeft;

            if (angle >= -angles[1] - angleDiff && angle <= -angles[1] + angleDiff)     //[-110, -70] = LEFT -> RIGHT
                return Orientation.Right;

            if (angle > -angles[1] + angleDiff && angle < angles[0] - angleDiff)        // ]-70, -20[ = FORWARD + LEFT -> RIGHT
                return Orientation.ForwardRight;

            return Orientation.ERROR;
        }

        /// <summary>
        /// Returns the relative orientation of the player to the target POSITION (location)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="TargetDirection"></param>
        /// <returns></returns>
        public static Orientation GetOrientationToPositionFromPlayer(Players.Player player, Pipliz.Vector3Int TargetPosition)
        {
            Pipliz.Vector3Int TargetDirection = GetDirection(TargetPosition, new Pipliz.Vector3Int(player.Position));

            return GetOrientationToDirectionFromPlayer(player, TargetDirection);
        }

    }
}
