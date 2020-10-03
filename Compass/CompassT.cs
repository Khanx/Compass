using NetworkUI;
using NetworkUI.Items;
using Pipliz;
using System.Collections.Generic;

namespace Compass
{
    public enum CompassAction
    {
        CardinalDirection,
        ColonyDirection,
        //PlayerDeath,    //FUTURE WORK
        //ColonistDeath,  //FUTURE WORK
        //WorldMarker     //FUTURE WORK
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

                case CompassAction.ColonyDirection:
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

            ButtonCallback CardinalButtonCallback = new ButtonCallback("Khanx.Compass.CardinalDirection", new LabelData("Cardinal Direction", UnityEngine.Color.white), -1, 25, ButtonCallback.EOnClickActions.ClosePopup);
            menu.Items.Add(CardinalButtonCallback);

            if (player.Colonies.Length > 0)
            {
                EmptySpace Cardinal2Colony = new EmptySpace(25);
                List<string> colonies = new List<string>();

                foreach (var col in player.Colonies)
                    colonies.Add(col.Name);

                DropDown ColonyDropDown = new DropDown("Colony", "Khanx.Compass.Colony", colonies);
                //Default dropdown (ALWAYS INCLUDE OR GIVES ERROR)
                menu.LocalStorage.SetAs("Khanx.Compass.Colony", 0);

                ButtonCallback ColonyButtonCallback = new ButtonCallback("Khanx.Compass.ColonyDirection", new LabelData("Navigate", UnityEngine.Color.white), -1, 25, ButtonCallback.EOnClickActions.ClosePopup);

                menu.Items.Add(Cardinal2Colony);
                menu.Items.Add(ColonyDropDown);
                menu.Items.Add(ColonyButtonCallback);
            }

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

            if (data.ButtonIdentifier.Equals("Khanx.Compass.ColonyDirection"))
            {
                int colonyInt = data.Storage.GetAs<int>("Khanx.Compass.Colony");

                Pipliz.Vector3Int colonyPosition = GetColonyPosition(colonyInt, data.Player);
                Orientation orientation = Helper.GetOrientationToPositionFromPlayer(data.Player, colonyPosition);

                SendOrientationToPlayer(data.Player, orientation);


                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.ColonyDirection, colonyPosition));
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
