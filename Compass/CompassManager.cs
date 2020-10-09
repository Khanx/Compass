using Pipliz;
using System.Collections.Generic;
using Newtonsoft;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Compass
{
    public class WayPoint
    {
        public string name;
        public Pipliz.Vector3Int position;

        public WayPoint(string name, Vector3Int position)
        {
            this.name = name;
            this.position = position;
        }
    }

    [ModLoader.ModManager]
    public static class CompassManager
    {

        public static Dictionary<NetworkID, CompassWaypoints> Waypoints = new Dictionary<NetworkID, CompassWaypoints>();

        public static List<WayPoint> list = new List<WayPoint>();

        public static string waypointsFile;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "Khanx.Compass.Load")]
        public static void LoadWaypoints()
        {
            waypointsFile = "./gamedata/savegames/" + ServerManager.WorldName + "/waypoints.json";

            if (!File.Exists(waypointsFile))
                return;

            Waypoints = JsonConvert.DeserializeObject<Dictionary<NetworkID, CompassWaypoints>>(File.ReadAllText(waypointsFile));
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "Khanx.Compass.AutoSave")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "Khanx.Compass.Save")]
        public static void SaveWaypoints()
        {
            if (File.Exists(waypointsFile))
                File.Delete(waypointsFile);

            if (Waypoints.Count(w => w.Value.waypoints.Count > 0) == 0)
                return;

            string json = JsonConvert.SerializeObject(Waypoints.Where(v => v.Value.waypoints.Count > 0).ToDictionary(k => k.Key, v => v.Value));

            File.WriteAllText(waypointsFile, json);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerDeath, "Khanx.Compass.OnPlayerDeath")]
        public static void OnPlayerDeath(Players.Player player)
        {
            CompassWaypoints compassWaypoints = Waypoints.GetValueOrDefault(player.ID, null);

            if (compassWaypoints == null)
            {
                compassWaypoints = new CompassWaypoints(new Vector3Int(player.Position), Vector3Int.invalidPos, new List<WayPoint>());
                Waypoints.Add(player.ID, compassWaypoints);
            }
            else
                compassWaypoints.playerDeath = new Vector3Int(player.Position);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCDied, "Khanx.Compass.OnNPCDied")]
        public static void OnNPCDied(NPC.NPCBase colonist)
        {
            Colony colony = colonist.Colony;

            if (colony == null)
                return;

            foreach (var owner in colony.Owners)
            {
                if (owner.ConnectionState != Players.EConnectionState.Connected)
                    continue;

                CompassWaypoints compassMarkers = Waypoints.GetValueOrDefault(owner.ID, null);

                if (compassMarkers == null)
                {
                    compassMarkers = new CompassWaypoints(Vector3Int.invalidPos, colonist.Position, new List<WayPoint>());
                    Waypoints.Add(owner.ID, compassMarkers);
                }
                else
                    compassMarkers.colonistDeath = colonist.Position;
            }
        }

    }
}
