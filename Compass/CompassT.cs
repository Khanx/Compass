using NetworkUI;
using NetworkUI.Items;
using Pipliz;
using System.Collections.Generic;
using UnityEngine;

namespace Compass
{
    [ModLoader.ModManager]
    public static class CompassT
    {
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, "Khanx.Compass.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, Shared.PlayerClickedData playerClickedData)
        {
            if (player == null || playerClickedData.TypeSelected != ItemTypes.IndexLookup.GetIndex("Khanx.Compass"))
                return;

            switch (playerClickedData.ClickType)
            {
                case Shared.PlayerClickedData.EClickType.Left:
                    Left_Click(player);
                    break;
                case Shared.PlayerClickedData.EClickType.Right:
                    Right_Click(player);
                    break;
            };
        }

        public static void Left_Click(Players.Player player)
        {
            if (player.Colonies.Length == 0)
            {
                Chatting.Chat.Send(player, "You do not have a colony.");
                return;
            }
            else if (player.Colonies.Length == 1)
            {
                Pipliz.Vector3Int colonyDirection = getColonyDirection(0, player);

                int angle = getAngle(colonyDirection, player);

                sendDirectionToPlayer(player, angle, colonyDirection);
            }
            else
                selectColonyUI(player);
        }

        public static Dictionary<NetworkID, int> last_Compass = new Dictionary<NetworkID, int>();

        public static void Right_Click(Players.Player player)
        {
            if (player.Colonies.Length == 0)
            {
                Chatting.Chat.Send(player, "You do not have a colony.");
                return;
            }
            else if (player.Colonies.Length == 1)
            {
                Pipliz.Vector3Int colonyDirection = getColonyDirection(0, player);

                int angle = getAngle(colonyDirection, player);

                sendDirectionToPlayer(player, angle, colonyDirection);
            }
            else
            {
                Pipliz.Vector3Int colonyDirection = getColonyDirection(last_Compass.GetValueOrDefault(player.ID, 0), player);

                int angle = getAngle(colonyDirection, player);

                sendDirectionToPlayer(player, angle, colonyDirection);
            }
        }

        public static void selectColonyUI(Players.Player player)
        {
            NetworkMenu menu = new NetworkMenu();
            menu.Identifier = "Compass";

            List<string> colonies = new List<string>();

            foreach (var col in player.Colonies)
                colonies.Add(col.Name);

            DropDown dropDown = new DropDown("Colony", "Khanx.Compass.Colony", colonies);
            //Default dropdown (ALWAYS INCLUDE OR GIVES ERROR)
            menu.LocalStorage.SetAs("Khanx.Compass.Colony", 0);

            ButtonCallback buttonCallback = new ButtonCallback("Khanx.Compass.Navigate", new LabelData("Navigate", UnityEngine.Color.black), -1, 25, ButtonCallback.EOnClickActions.ClosePopup);

            menu.Items.Add(dropDown);
            menu.Items.Add(buttonCallback);

            NetworkMenuManager.SendServerPopup(player, menu);
        }


        public static Pipliz.Vector3Int getColonyDirection(int colonyInt, Players.Player player)
        {
            return (player.Colonies[colonyInt].GetClosestBanner(new Pipliz.Vector3Int(player.Position)).Position - new Pipliz.Vector3Int(player.Position));
        }

        public static int getAngle(Pipliz.Vector3Int colonyDirection, Players.Player player)
        {
            //return angle;
            return (int)UnityEngine.Vector3.Angle(new UnityEngine.Vector3(player.Forward.x, player.Forward.z), new UnityEngine.Vector3(colonyDirection.x, colonyDirection.z));
        }

        public static Pipliz.Vector3Int GetDirection(Vector3 playerForward, bool right = true)
        {
            Vector3 testVector;
            // testVector will be the "local" player direction intended. It rotates as the player rotates

            if(right)
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

        public static readonly int[] angles = new int[] {0, 90, 180, 270, 360};
        public static readonly int angleDiff = 20;

        public static void sendDirectionToPlayer(Players.Player player, int angle, Pipliz.Vector3Int colonyDirection)
        {
            //Chatting.Chat.Send(data.Player, "Angle: " + angle);

            if (angle < angles[0] + angleDiff)  // 0 - 20
                Chatting.Chat.Send(player, "Forward");
            else if (angle >= angles[0] + angleDiff && angle < angles[1] - angleDiff)   // 20 - 70
            {
                Pipliz.Vector3Int l = GetDirection(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirection(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(colonyDirection, l) < Pipliz.Math.ManhattanDistance(colonyDirection, r))
                    Chatting.Chat.Send(player, "Forward & Left");
                else
                    Chatting.Chat.Send(player, "Forward & Right");
            }
            else if (angle >= angles[1] - angleDiff && angle < angles[1] + angleDiff)   // 70 - 110
            {
                Pipliz.Vector3Int l = GetDirection(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirection(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(colonyDirection, l) < Pipliz.Math.ManhattanDistance(colonyDirection, r))
                    Chatting.Chat.Send(player, "Left");
                else
                    Chatting.Chat.Send(player, "Right");
            }
            else if (angle >= angles[1] + angleDiff && angle < angles[2] - angleDiff)   // 110 - 160
            {
                Pipliz.Vector3Int l = GetDirection(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirection(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(colonyDirection, l) < Pipliz.Math.ManhattanDistance(colonyDirection, r))
                    Chatting.Chat.Send(player, "Backward & Left");
                else
                    Chatting.Chat.Send(player, "Backward & Right");
            }
            else if (angle >= angles[2] - angleDiff && angle < angles[2] + angleDiff)   // 160 - 200
                Chatting.Chat.Send(player, "Backward");
            else if (angle >= angles[2] + angleDiff && angle < angles[3] - angleDiff)   // 200 - 250
            {
                Pipliz.Vector3Int l = GetDirection(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirection(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(colonyDirection, l) < Pipliz.Math.ManhattanDistance(colonyDirection, r))
                    Chatting.Chat.Send(player, "Backward & Left");
                else
                    Chatting.Chat.Send(player, "Backward & Right");
            }
            else if (angle >= angles[3] - angleDiff)    // 250 - 360
            {
                Pipliz.Vector3Int l = GetDirection(player.Forward, false) * 10;
                Pipliz.Vector3Int r = GetDirection(player.Forward, true) * 10;

                if (Pipliz.Math.ManhattanDistance(colonyDirection, l) < Pipliz.Math.ManhattanDistance(colonyDirection, r))
                    Chatting.Chat.Send(player, "Left");
                else
                    Chatting.Chat.Send(player, "Right");
            }

        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerPushedNetworkUIButton, "Khanx.Compass.OnPlayerPushedNetworkUIButton")]
        public static void OnPlayerPushedNetworkUIButton(ButtonPressCallbackData data)
        {
            if (!data.ButtonIdentifier.Equals("Khanx.Compass.Navigate"))
                return;

            int colonyInt = data.Storage.GetAs<int>("Khanx.Compass.Colony");

            if (last_Compass.ContainsKey(data.Player.ID))
                last_Compass.Remove(data.Player.ID);

            last_Compass.Add(data.Player.ID, colonyInt);

            Pipliz.Vector3Int colonyDirection = getColonyDirection(colonyInt, data.Player);

            int angle = getAngle(colonyDirection, data.Player);

            sendDirectionToPlayer(data.Player, angle, colonyDirection);
        }
    }
}
