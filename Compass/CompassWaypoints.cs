using Pipliz;
using System.Collections.Generic;


namespace Compass
{
    public class CompassWaypoints
    {
        public Vector3Int playerDeath { get; set; }
        public Vector3Int colonistDeath { get; set; }
        public List<WayPoint> waypoints { get; set; }
        
        public CompassWaypoints(Vector3Int playerDeath, Vector3Int colonistDeath, List<WayPoint> waypoints)
        {
            this.playerDeath = playerDeath;
            this.colonistDeath = colonistDeath;
            this.waypoints = waypoints;
        }
    }
}
