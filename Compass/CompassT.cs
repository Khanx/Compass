using NetworkUI;
using NetworkUI.Items;
using Pipliz;
using System.Collections.Generic;
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
        //PlayerID - Colony of the LAST direction
        public static Dictionary<NetworkID, CompassLastAction> last_Compass_Action = new Dictionary<NetworkID, CompassLastAction>();

        public static readonly int[] angles = new int[] { 0, 90, 180 };  //The result of the Unity's method to calculate the angle is never greater than 180
        public static readonly int angleDiff = 20;  //Diff between angles ~90/4. Example of use: Forward = [-angleDiff, angleDiff]

        //Item Interaction
        //Left CLick = Show UI
        //Right Click = Last option selected in the UI. Default: Direction
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

        //Last option selected in the UI. Default: Direction
        public static void RepeatLastAction(Players.Player player)
        {
            CompassLastAction last_action = last_Compass_Action.GetValueOrDefault(player.ID, new CompassLastAction(CompassAction.CardinalDirection));

            switch (last_action.action)
            {
                case CompassAction.CardinalDirection:
                    sendCardinalDirectionToPlayer(player);
                    break;

                case CompassAction.ColonyDirection:
                    Orientation orientation = GetOrientationToPositionFromPlayer(player, last_action.position);

                    sendOrientationToPlayer(player, orientation);
                    break;
            }
        }

        //Shows the UI
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
                sendCardinalDirectionToPlayer(data.Player);

                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.CardinalDirection));
                return;
            }

            if (data.ButtonIdentifier.Equals("Khanx.Compass.ColonyDirection"))
            {
                int colonyInt = data.Storage.GetAs<int>("Khanx.Compass.Colony");

                Pipliz.Vector3Int colonyPosition = getColonyPosition(colonyInt, data.Player);
                Orientation orientation = GetOrientationToPositionFromPlayer(data.Player, colonyPosition);

                sendOrientationToPlayer(data.Player, orientation);


                if (last_Compass_Action.ContainsKey(data.Player.ID))
                    last_Compass_Action.Remove(data.Player.ID);

                last_Compass_Action.Add(data.Player.ID, new CompassLastAction(CompassAction.ColonyDirection, colonyPosition));
                return;
            }
        }

        //Returns the Position of the Colony <player>.Colonies[<colonyInt>] (comes from the UI)
        public static Pipliz.Vector3Int getColonyPosition(int colonyInt, Players.Player player)
        {
            return (player.Colonies[colonyInt].GetClosestBanner(new Pipliz.Vector3Int(player.Position)).Position);
        }

        //Returns the Direction(vector) between the <TargetPosition> and <SourcePosition>
        public static Pipliz.Vector3Int getDirection(Pipliz.Vector3Int TargetPosition, Pipliz.Vector3Int SourcePosition)
        {
            return TargetPosition - SourcePosition;
        }

        //Return the angle (degrees º) between Target(Vector) and Source(Vector) WITHOUT considering the Y AXIS
        // The result is between [-180, 180]
        // Positive in a clockwise direction and negative in an anti-clockwise direction.
        public static int getAngle(Pipliz.Vector3Int TargetDirection, Pipliz.Vector3Int SourceDirection)
        {
            return (int)UnityEngine.Vector3.SignedAngle(new UnityEngine.Vector3(SourceDirection.x, SourceDirection.z), new UnityEngine.Vector3(TargetDirection.x, TargetDirection.z), Vector3.forward);
        }

        //Returns the Orientation <player> to
        public static Orientation GetOrientationToDirectionFromPlayer(Players.Player player, Pipliz.Vector3Int TargetDirection)
        {
            int angle = getAngle(TargetDirection, new Pipliz.Vector3Int(player.Forward));

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

        public static Orientation GetOrientationToPositionFromPlayer(Players.Player player, Pipliz.Vector3Int TargetPosition)
        {
            Pipliz.Vector3Int TargetDirection = getDirection(TargetPosition, new Pipliz.Vector3Int(player.Position));

            return GetOrientationToDirectionFromPlayer(player, TargetDirection);
        }

        //Sends to the player the cardinal direction he is looking at
        public static void sendCardinalDirectionToPlayer(Players.Player player)
        {
            int x = (int)(player.Forward.x * 100);
            int z = (int)(player.Forward.z * 100);

            /*
             * Z AXIS:
             * [80, 100] NORTH
             * [0, 79] NORTH + X
             * [-79, 0] SOUTH + X
             * [100, 80] SOUTH
             * 
             * X AXIS:
             * [80, 100] EAST
             * [0, 79] EAST + X
             * [-79, 0] WEST + X
             * [100, 80] WEST
             * 
             * IF pure NORTH/SOUTH/EAST/WEST appears it is not need to check the other axis
             */

            if (z > 100 - angleDiff)
            {
                Chatting.Chat.Send(player, "North");
                return;
            }
            else if (-z > 100 - angleDiff)
            {
                Chatting.Chat.Send(player, "South");
                return;
            }
            else if (x > 100 - angleDiff)
            {
                Chatting.Chat.Send(player, "East");
                return;
            }
            else if (-x > 100 - angleDiff)
            {
                Chatting.Chat.Send(player, "West");
                return;
            }

            if (z > 0) //NORTH
            {
                if (x > 0) //East
                {
                    Chatting.Chat.Send(player, "North & East");
                    return;
                }
                else //West
                {
                    Chatting.Chat.Send(player, "North & West");
                    return;
                }
            }
            else //South
            {
                if (x > 0) //East
                {
                    Chatting.Chat.Send(player, "South & East");
                    return;
                }
                else //West
                {
                    Chatting.Chat.Send(player, "South & West");
                    return;
                }
            }
        }

        //Sends the Orientation X to the player
        public static void sendOrientationToPlayer(Players.Player player, Orientation orientation)
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
