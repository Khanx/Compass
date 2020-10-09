using colonyserver.Assets.UIGeneration;
using NetworkUI;
using NetworkUI.Items;
using Pipliz;
using System.Collections.Generic;
using static colonyshared.NetworkUI.UIGeneration.WorldMarkerSettings;

namespace Compass
{
    public enum CompassAction
    {
        CardinalDirection,
        ColonyDirection,
        PlayerDeath,
        ColonistDeath,
        WayPoint
    }

    public struct CompassLastAction
    {
        public CompassAction action;
        public Pipliz.Vector3Int position;

        public CompassLastAction(CompassAction action)
        {
            this.action = action;
            this.position = new Pipliz.Vector3Int();
        }

        public CompassLastAction(CompassAction action, Pipliz.Vector3Int position)
        {
            this.action = action;
            this.position = position;
        }
    }

    [ModLoader.ModManager]
    public static class CompassT
    {
        /// <summary>
        /// PlayerID - Last action selected
        /// </summary>
        public static Dictionary<NetworkID, CompassLastAction> last_Compass_Action = new Dictionary<NetworkID, CompassLastAction>();

        /// <summary>
        /// Interaction with the Compass (item)
        /// Left Click = Show UI
        /// Right Click = Last option selected, default: Cardinal Direction
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerClickedData"></param>
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, "Khanx.Compass.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, Shared.PlayerClickedData playerClickedData)
        {
            if (player == null || playerClickedData.TypeSelected != ItemTypes.IndexLookup.GetIndex("Khanx.Compass"))
                return;

            switch (playerClickedData.ClickType)
            {
                case Shared.PlayerClickedData.EClickType.Left:
                    CompassUI(player);
                    break;
                case Shared.PlayerClickedData.EClickType.Right:
                    RepeatLastAction(player);
                    break;
            };
        }

        /// <summary>
        /// Repeat the last action selected in the UI. Default Cardinal Direction
        /// </summary>
        /// <param name="player"></param>
        public static void RepeatLastAction(Players.Player player)
        {
            CompassLastAction last_action = last_Compass_Action.GetValueOrDefault(player.ID, new CompassLastAction(CompassAction.CardinalDirection));

            switch (last_action.action)
            {
                case CompassAction.CardinalDirection:
                    SendCardinalDirectionToPlayer(player);
                    break;

                case CompassAction.PlayerDeath:
                case CompassAction.ColonistDeath:
                case CompassAction.ColonyDirection:
                case CompassAction.WayPoint:
                    Orientation orientation = Helper.GetOrientationToPositionFromPlayer(player, last_action.position);

                    SendOrientationToPlayer(player, orientation);
                    break;
            }
        }

        /// <summary>
        /// Shows the UI
        /// </summary>
        /// <param name="player"></param>
        public static void CompassUI(Players.Player player)
        {
            NetworkMenu menu = new NetworkMenu();
            menu.Identifier = "Compass";
            menu.LocalStorage.SetAs("header", "Compass");
            menu.Width = 450;
            menu.Height = 400;

            ButtonCallback CardinalButtonCallback = new ButtonCallback("Khanx.Compass.CardinalDirection",
                                                                       new LabelData("Cardinal direction", UnityEngine.Color.white),
                                                                       200,
                                                                       30,
                                                                       ButtonCallback.EOnClickActions.ClosePopup);

            List<string> colonies = new List<string>();

            if (player.Colonies.Length > 0)
                foreach (var col in player.Colonies)
                    colonies.Add(col.Name);
            else
                colonies.Add("-");

            Label ColonyLabel = new Label(new LabelData("Colony:", UnityEngine.Color.white));
            DropDownNoLabel ColonyDropDown = new DropDownNoLabel("Khanx.Compass.Colony", colonies);
            ColonyDropDown.Width = 300;
            //Default dropdown (ALWAYS INCLUDE OR GIVES ERROR)
            menu.LocalStorage.SetAs("Khanx.Compass.Colony", 0);

            HorizontalRow ColonySelector = new HorizontalRow(new List<(IItem, int)> { (ColonyLabel, 75), (ColonyDropDown, 325) });


            ButtonCallback ColonyButtonCallback = new ButtonCallback("Khanx.Compass.ColonyDirection",
                                                                     new LabelData("Find colony", (player.Colonies.Length > 0) ? UnityEngine.Color.white : UnityEngine.Color.black),
                                                                     200,
                                                                     30,
                                                                     (player.Colonies.Length > 0) ? ButtonCallback.EOnClickActions.ClosePopup : ButtonCallback.EOnClickActions.None);


            CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(player.ID, null);


            ButtonCallback PlayerDeathButtonCallback = new ButtonCallback("Khanx.Compass.PlayerDeath",
                                                                            new LabelData("Player Death", UnityEngine.Color.white),
                                                                            200,
                                                                            30,
                                                                            ButtonCallback.EOnClickActions.ClosePopup);

            if (compassWaypoints == null || compassWaypoints.playerDeath == Vector3Int.invalidPos)
                PlayerDeathButtonCallback.Enabled = false;


            ButtonCallback ColonistDeathButtonCallback = new ButtonCallback("Khanx.Compass.ColonistDeath",
                                                                    new LabelData("Colonist Death", UnityEngine.Color.white),
                                                                    200,
                                                                    30,
                                                                    ButtonCallback.EOnClickActions.ClosePopup); ;

            if (compassWaypoints == null || compassWaypoints.colonistDeath == Vector3Int.invalidPos)
                ColonistDeathButtonCallback.Enabled = false;


            List<string> waypoints = new List<string>();

            if (compassWaypoints != null && compassWaypoints.waypoints != null && compassWaypoints.waypoints.Count != 0)
                foreach (var w in compassWaypoints.waypoints)
                    waypoints.Add(w.name);
            else
                waypoints.Add("-");

            Label WaypointLabel = new Label(new LabelData("Waypoint:", UnityEngine.Color.white));
            DropDownNoLabel WaypointDropDown = new DropDownNoLabel("Khanx.Compass.Waypoint", waypoints);
            WaypointDropDown.Width = 300;
            //Default dropdown (ALWAYS INCLUDE OR GIVES ERROR)
            menu.LocalStorage.SetAs("Khanx.Compass.Waypoint", 0);

            HorizontalRow WaypointSelector = new HorizontalRow(new List<(IItem, int)> { (WaypointLabel, 75), (WaypointDropDown, 325) });

            ButtonCallback WaypointButtonCallback = new ButtonCallback("Khanx.Compass.WaypointDirection",
                                                                     new LabelData("Find waypoint", (compassWaypoints != null && compassWaypoints.waypoints != null && compassWaypoints.waypoints.Count != 0) ? UnityEngine.Color.white : UnityEngine.Color.black),
                                                                     200,
                                                                     30,
                                                                     (compassWaypoints != null && compassWaypoints.waypoints != null && compassWaypoints.waypoints.Count != 0) ? ButtonCallback.EOnClickActions.ClosePopup : ButtonCallback.EOnClickActions.None);

            ButtonCallback WayPointAdd = new ButtonCallback("Khanx.Compass.WaypointUIAdd",
                                                                     new LabelData("Add waypoint", UnityEngine.Color.white),
                                                                     200,
                                                                     30,
                                                                     ButtonCallback.EOnClickActions.ClosePopup);

            ButtonCallback WayPointRemove = new ButtonCallback("Khanx.Compass.WaypointRemove",
                                                                     new LabelData("Remove waypoint", (compassWaypoints != null && compassWaypoints.waypoints != null && compassWaypoints.waypoints.Count != 0) ? UnityEngine.Color.white : UnityEngine.Color.black),
                                                                     200,
                                                                     30,
                                                                     (compassWaypoints != null && compassWaypoints.waypoints != null && compassWaypoints.waypoints.Count != 0) ? ButtonCallback.EOnClickActions.ClosePopup : ButtonCallback.EOnClickActions.None);

            HorizontalRow WaypointManage = new HorizontalRow(new List<(IItem, int)> { (WayPointAdd, 200), (WayPointRemove, 200) });


            menu.Items.Add(new HorizontalRow(new List<(IItem, int)> { (new EmptySpace(), 100), (CardinalButtonCallback, 200) }));
            menu.Items.Add(new HorizontalRow(new List<(IItem, int)> { (new EmptySpace(), 100), (PlayerDeathButtonCallback, 200) }));
            menu.Items.Add(new HorizontalRow(new List<(IItem, int)> { (new EmptySpace(), 100), (ColonistDeathButtonCallback, 200) }));

            menu.Items.Add(new EmptySpace(10));
            menu.Items.Add(new Line(UnityEngine.Color.white, 1, 410, 0, 0));
            menu.Items.Add(new EmptySpace(10));
            menu.Items.Add(ColonySelector);
            menu.Items.Add(new HorizontalRow(new List<(IItem, int)> { (new EmptySpace(), 100), (ColonyButtonCallback, 200) }));

            menu.Items.Add(new EmptySpace(10));
            menu.Items.Add(new Line(UnityEngine.Color.white, 1, 410, 0, 0));
            menu.Items.Add(new EmptySpace(10));
            menu.Items.Add(WaypointSelector);
            menu.Items.Add(new HorizontalRow(new List<(IItem, int)> { (new EmptySpace(), 100), (WaypointButtonCallback, 200) }));
            menu.Items.Add(WaypointManage);


            NetworkMenuManager.SendServerPopup(player, menu);
        }

        //Button behavior
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerPushedNetworkUIButton, "Khanx.Compass.OnPlayerPushedNetworkUIButton")]
        public static void OnPlayerPushedNetworkUIButton(ButtonPressCallbackData data)
        {
            if (data.ButtonIdentifier.Equals("Khanx.Compass.CardinalDirection"))
            {
                SendCardinalDirectionToPlayer(data.Player);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.CardinalDirection));
                return;
            }
            
            if (data.ButtonIdentifier.Equals("Khanx.Compass.PlayerDeath"))
            {
                CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(data.Player.ID, null);

                if (compassWaypoints == null)
                    return;

                Orientation orientation = Helper.GetOrientationToPositionFromPlayer(data.Player, compassWaypoints.playerDeath);

                SendOrientationToPlayer(data.Player, orientation);

                UIManager.AddorUpdateWorldMarker("Khanx.Compass.Goal" + data.Player.Name,
                    (data.Player.Name.Substring(data.Player.Name.Length-1).Equals("s")) ? data.Player.Name+"' tomb" : data.Player.Name + "'s tomb",
                    compassWaypoints.playerDeath,
                    "Khanx.Compass",
                    ToggleType.ItemSelected,
                    "Khanx.Compass",
                    data.Player);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.PlayerDeath, compassWaypoints.playerDeath));
                return;
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.ColonistDeath"))
            {
                CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(data.Player.ID, null);

                if (compassWaypoints == null)
                    return;

                Orientation orientation = Helper.GetOrientationToPositionFromPlayer(data.Player, compassWaypoints.colonistDeath);

                SendOrientationToPlayer(data.Player, orientation);

                UIManager.AddorUpdateWorldMarker("Khanx.Compass.Goal" + data.Player.Name,
                    "Colonist Death",
                    compassWaypoints.colonistDeath,
                    "Khanx.Compass",
                    ToggleType.ItemSelected,
                    "Khanx.Compass",
                     data.Player);


                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.ColonistDeath, compassWaypoints.colonistDeath));
                return;
            }
            
            if (data.ButtonIdentifier.Equals("Khanx.Compass.ColonyDirection"))
            {
                int colonyInt = data.Storage.GetAs<int>("Khanx.Compass.Colony");

                Pipliz.Vector3Int colonyPosition = GetColonyPosition(colonyInt, data.Player);
                Orientation orientation = Helper.GetOrientationToPositionFromPlayer(data.Player, colonyPosition);

                SendOrientationToPlayer(data.Player, orientation);

                UIManager.AddorUpdateWorldMarker("Khanx.Compass.Goal" + data.Player.Name,
                    data.Player.Colonies[colonyInt].Name,
                    colonyPosition,
                    "Khanx.Compass",
                    ToggleType.ItemSelected,
                    "Khanx.Compass",
                     data.Player);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.ColonyDirection, colonyPosition));
                return;
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.WaypointDirection"))
            {
                int waypointInt = data.Storage.GetAs<int>("Khanx.Compass.Waypoint");

                CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(data.Player.ID, null);

                if (compassWaypoints == null || compassWaypoints.waypoints == null && compassWaypoints.waypoints.Count == 0)
                {
                    Chatting.Chat.Send(data.Player, "Error: WaypointSelection, contact with the author of the mod");
                    return;
                }

                Orientation orientation = Helper.GetOrientationToPositionFromPlayer(data.Player, compassWaypoints.waypoints[waypointInt].position);

                SendOrientationToPlayer(data.Player, orientation);

                UIManager.AddorUpdateWorldMarker("Khanx.Compass.Goal" + data.Player.Name,
                    compassWaypoints.waypoints[waypointInt].name,
                    compassWaypoints.waypoints[waypointInt].position,
                    "Khanx.Compass",
                    ToggleType.ItemSelected,
                    "Khanx.Compass",
                     data.Player);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.WayPoint, compassWaypoints.waypoints[waypointInt].position));
                return;
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.WaypointUIAdd"))
            {
                NetworkMenu menu = new NetworkMenu();
                menu.Identifier = "Compass";
                menu.LocalStorage.SetAs("header", "Compass");
                InputField waypointName = new InputField("Khanx.Compass.waypointName");

                ButtonCallback WayPointAdd = new ButtonCallback("Khanx.Compass.WaypointAdd",
                                                                new LabelData("Add waypoint", UnityEngine.Color.white),
                                                                200,
                                                                30,
                                                                ButtonCallback.EOnClickActions.ClosePopup);

                ButtonCallback WayPointRemove = new ButtonCallback("Khanx.Compass.WaypointCancel",
                                                                        new LabelData("Cancel", UnityEngine.Color.white),
                                                                        200,
                                                                        30,
                                                                        ButtonCallback.EOnClickActions.ClosePopup);

                HorizontalRow WaypointManage = new HorizontalRow(new List<(IItem, int)> { (WayPointAdd, 200), (WayPointRemove, 200) });

                menu.Items.Add(waypointName);
                menu.Items.Add(WaypointManage);
                NetworkMenuManager.SendServerPopup(data.Player, menu);
                return;
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.WaypointAdd"))
            {
                string waypointName = data.Storage.GetAs<string>("Khanx.Compass.waypointName");

                if(waypointName.Equals(""))
                {
                    Chatting.Chat.Send(data.Player, "The waypoint has NOT been added, the name was not suitable");
                    return;
                }

                CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(data.Player.ID, null);

                if (compassWaypoints == null || compassWaypoints.waypoints == null)
                {
                    compassWaypoints = new CompassWaypoints(Vector3Int.invalidPos, Vector3Int.invalidPos, new List<WayPoint>() { new WayPoint(waypointName, new Vector3Int(data.Player.Position)) });
                    CompassManager.Waypoints.Add(data.Player.ID, compassWaypoints);
                }
                else
                    compassWaypoints.waypoints.Add(new WayPoint(waypointName, new Vector3Int(data.Player.Position)));

                Chatting.Chat.Send(data.Player, "Waypoint " + waypointName + " has been added");
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.WaypointRemove"))
            {
                int waypointInt = data.Storage.GetAs<int>("Khanx.Compass.Waypoint");

                CompassWaypoints compassWaypoints = CompassManager.Waypoints.GetValueOrDefault(data.Player.ID, null);

                //This should not happen
                if (compassWaypoints == null || compassWaypoints.waypoints == null && compassWaypoints.waypoints.Count == 0)
                {
                    Chatting.Chat.Send(data.Player, "Error: WaypointRemoving, contact with the author of the mod");
                    return;
                }

                Chatting.Chat.Send(data.Player, "Waypoint "+ compassWaypoints.waypoints[waypointInt].name +" has been removed");

                compassWaypoints.waypoints.RemoveAt(waypointInt);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                UIManager.RemoveMarker("Khanx.Compass.Goal" + data.Player.Name, data.Player);

                return;
            }
        }

        /// <summary>
        /// Returns the Position of the Colony <player>.Colonies[<colonyInt>] (comes from the UI)
        /// </summary>
        /// <param name="colonyInt"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static Pipliz.Vector3Int GetColonyPosition(int colonyInt, Players.Player player)
        {
            return (player.Colonies[colonyInt].GetClosestBanner(new Pipliz.Vector3Int(player.Position)).Position);
        }

        /// <summary>
        /// Sends to the player the cardinal direction which he is looking at
        /// </summary>
        /// <param name="player"></param>
        private static void SendCardinalDirectionToPlayer(Players.Player player)
        {
            //Calculate the orientation to the NORTH and based on the orientation it says the cardinal direction

            Orientation orientation = Helper.GetOrientationToDirectionFromPlayer(player, new Vector3Int(0, 0, 5));

            switch (orientation)
            {
                case Orientation.Forward:
                    Chatting.Chat.Send(player, "North");
                    break;
                case Orientation.Right:
                    Chatting.Chat.Send(player, "West");
                    break;
                case Orientation.Backward:
                    Chatting.Chat.Send(player, "South");
                    break;
                case Orientation.Left:
                    Chatting.Chat.Send(player, "East");
                    break;
                case Orientation.ForwardRight:
                    Chatting.Chat.Send(player, "North & West");
                    break;
                case Orientation.ForwardLeft:
                    Chatting.Chat.Send(player, "North & East");
                    break;
                case Orientation.BackwardRight:
                    Chatting.Chat.Send(player, "South & East");
                    break;
                case Orientation.BackwardLeft:
                    Chatting.Chat.Send(player, "South & West");
                    break;
                case Orientation.ERROR:
                    Chatting.Chat.Send(player, "Something went wrong, please contant with the author of the mod");
                    break;
            }
        }

        /// <summary>
        /// Sends the Orientation X to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="orientation"></param>
        private static void SendOrientationToPlayer(Players.Player player, Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Forward:
                    Chatting.Chat.Send(player, "Forward");
                    break;
                case Orientation.Right:
                    Chatting.Chat.Send(player, "Right");
                    break;
                case Orientation.Backward:
                    Chatting.Chat.Send(player, "Backward");
                    break;
                case Orientation.Left:
                    Chatting.Chat.Send(player, "Left");
                    break;
                case Orientation.ForwardRight:
                    Chatting.Chat.Send(player, "Forward & Right");
                    break;
                case Orientation.ForwardLeft:
                    Chatting.Chat.Send(player, "Forward & Left");
                    break;
                case Orientation.BackwardRight:
                    Chatting.Chat.Send(player, "Backward & Right");
                    break;
                case Orientation.BackwardLeft:
                    Chatting.Chat.Send(player, "Backward & Left");
                    break;

                case Orientation.ERROR:
                    Chatting.Chat.Send(player, "Something went wrong, please contact with the author of this mod");
                    break;
            }
        }
    }
}
