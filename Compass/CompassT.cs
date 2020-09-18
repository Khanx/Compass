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

        public static readonly int[] angles = new int[] { 0, 90, 180, 270, 360 };
        public static readonly int angleDiff = 20;  //Diff between angles (Example: 0º = Forward, [0+angleDiff, 90-angleDiff] = Forward + Right)

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
        public static int getAngle(Pipliz.Vector3Int TargetDirection, Pipliz.Vector3Int SourceDirection)
        {
            return (int)UnityEngine.Vector3.Angle(new UnityEngine.Vector3(SourceDirection.x, SourceDirection.z), new UnityEngine.Vector3(TargetDirection.x, TargetDirection.z));
        }

        //Calculates the Direction(Vector) to Player Forward Right/Left
        public static Pipliz.Vector3Int GetDirectionRight_Left(Vector3 playerForward, bool right = true)
        {
            Vector3 testVector;
            // testVector will be the "local" player direction intended. It rotates as the player rotates

            if (right)
                testVector = -Vector3.Cross(playerForward, Vector3.up);
            else //left
                testVector = Vector3.Cross(playerForward, Vector3.up);

            float testVectorMax = Math.MaxMagnitude(testVector);
            // testVectorMax holds either x, y or z from testVector; whichever has the largest absolute value
            if (testVectorMax == testVector.x)
            {
                // so the largest part of the direction is the x-axis
                if (testVectorMax >= 0f)
                    return new Pipliz.Vector3Int(1, 0, 0);
                else
                    return new Pipliz.Vector3Int(-1, 0, 0);
            }
            else
            {
                if (testVectorMax >= 0)
                    return new Pipliz.Vector3Int(0, 0, 1);
                else
                    return new Pipliz.Vector3Int(0, 0, -1);
            }
        }

        //Returns the Orientation <player> to
        public static Orientation GetOrientationToDirectionFromPlayer(Players.Player player, Pipliz.Vector3Int TargetDirection)
        {
            int angle = getAngle(TargetDirection, new Pipliz.Vector3Int(player.Forward));

            if (angle < angles[0] + angleDiff)  // 0 - 20
                return Orientation.Forward;
            else if (angle >= angles[0] + angleDiff && angle < angles[1] - angleDiff)   // 20 - 70
            {
                Pipliz.Vector3Int l = GetDirectionRight_Left(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirectionRight_Left(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(TargetDirection, l) < Pipliz.Math.ManhattanDistance(TargetDirection, r))
                    return Orientation.ForwardLeft;
                else
                    return Orientation.ForwardRight;
            }
            else if (angle >= angles[1] - angleDiff && angle < angles[1] + angleDiff)   // 70 - 110
            {
                Pipliz.Vector3Int l = GetDirectionRight_Left(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirectionRight_Left(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(TargetDirection, l) < Pipliz.Math.ManhattanDistance(TargetDirection, r))
                    return Orientation.Left;
                else
                    return Orientation.Right;
            }
            else if (angle >= angles[1] + angleDiff && angle < angles[2] - angleDiff)   // 110 - 160
            {
                Pipliz.Vector3Int l = GetDirectionRight_Left(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirectionRight_Left(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(TargetDirection, l) < Pipliz.Math.ManhattanDistance(TargetDirection, r))
                    return Orientation.BackwardLeft;
                else
                    return Orientation.BackwardRight;
            }
            else if (angle >= angles[2] - angleDiff && angle < angles[2] + angleDiff)   // 160 - 200
                return Orientation.Backward;
            else if (angle >= angles[2] + angleDiff && angle < angles[3] - angleDiff)   // 200 - 250
            {
                Pipliz.Vector3Int l = GetDirectionRight_Left(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirectionRight_Left(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(TargetDirection, l) < Pipliz.Math.ManhattanDistance(TargetDirection, r))
                    return Orientation.BackwardLeft;
                else
                    return Orientation.BackwardRight;
            }
            else if (angle >= angles[3] - angleDiff)    // 250 - 360
            {
                Pipliz.Vector3Int l = GetDirectionRight_Left(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirectionRight_Left(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(TargetDirection, l) < Pipliz.Math.ManhattanDistance(TargetDirection, r))
                    return Orientation.Left;
                else
                    return Orientation.Right;
            }

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
            Z BETWEEN [80,99] = NORTH & IGNORE X
            Z BETWEEN [-80,-99] = SOUTH & IGNORE X

            X BETWEEM [80,99] = EAST & IGNORE Z
            X BETWEEM [-80,-99] = EAST & IGNORE Z
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
