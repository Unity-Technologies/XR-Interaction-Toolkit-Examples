namespace Oculus.Platform.Samples.SimplePlatformSample
{
    using UnityEngine;
    using UnityEngine.UI;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Oculus.Platform;
    using Oculus.Platform.Models;

    public class DataEntry : MonoBehaviour
    {

        public Text dataOutput;

        void Start()
        {
            Core.Initialize();
            checkEntitlement();
        }

        // Update is called once per frame
        void Update()
        {
            string currentText = GetComponent<InputField>().text;

            if (Input.GetKey(KeyCode.Return))
            {
                if (currentText != "")
                {
                    SubmitCommand(currentText);
                }

                GetComponent<InputField>().text = "";
            }

            // Handle all messages being returned
            Request.RunCallbacks();
        }

        private void SubmitCommand(string command)
        {
            string[] commandParams = command.Split(' ');

            if (commandParams.Length > 0)
            {
                switch (commandParams[0])
                {
                    case "m":
                        getLoggedInUser();
                        break;
                    case "u":
                        if (commandParams.Length > 1)
                        {
                            getUser(commandParams[1]);
                        }
                        break;
                    case "d":
                        getLoggedInFriends();
                        break;
                    case "n":
                        getUserNonce();
                        break;
                    case "e":
                        checkEntitlement();
                        break;
                    case "a":
                        if (commandParams.Length > 1)
                        {
                            getAchievementDefinition(commandParams[1]);
                        }
                        break;
                    case "b":
                        if (commandParams.Length > 1)
                        {
                            getAchievementProgress(commandParams[1]);
                        }
                        break;
                    case "3":
                        if (commandParams.Length > 1)
                        {
                            unlockAchievement(commandParams[1]);
                        }
                        break;
                    case "4":
                        if (commandParams.Length > 2)
                        {
                            addCountAchievement(commandParams[1], commandParams[2]);
                        }
                        break;
                    case "5":
                        if (commandParams.Length > 2)
                        {
                            addFieldsAchievement(commandParams[1], commandParams[2]);
                        }
                        break;
                    case "1":
                        if (commandParams.Length > 2)
                        {
                            writeLeaderboardEntry(commandParams[1], commandParams[2]);
                        }
                        break;
                    case "2":
                        if (commandParams.Length > 1)
                        {
                            getLeaderboardEntries(commandParams[1]);
                        }
                        break;
                    default:
                        printOutputLine("Invalid Command");
                        break;
                }
            }
        }

        void getLeaderboardEntries(string leaderboardName)
        {
            Leaderboards.GetEntries(leaderboardName, 10, LeaderboardFilterType.None, LeaderboardStartAt.Top).OnComplete(leaderboardGetCallback);
        }

        void writeLeaderboardEntry(string leaderboardName, string value)
        {
            byte[] extraData = new byte[] { 0x54, 0x65, 0x73, 0x74 };

            Leaderboards.WriteEntry(leaderboardName, Convert.ToInt32(value), extraData, false).OnComplete(leaderboardWriteCallback);
        }

        void addFieldsAchievement(string achievementName, string fields)
        {
            Achievements.AddFields(achievementName, fields).OnComplete(achievementFieldsCallback);
        }

        void addCountAchievement(string achievementName, string count)
        {
            Achievements.AddCount(achievementName, Convert.ToUInt64(count)).OnComplete(achievementCountCallback);
        }

        void unlockAchievement(string achievementName)
        {
            Achievements.Unlock(achievementName).OnComplete(achievementUnlockCallback);
        }

        void getAchievementProgress(string achievementName)
        {
            string[] Names = new string[1];
            Names[0] = achievementName;

            Achievements.GetProgressByName(Names).OnComplete(achievementProgressCallback);
        }

        void getAchievementDefinition(string achievementName)
        {
            string[] Names = new string[1];
            Names[0] = achievementName;

            Achievements.GetDefinitionsByName(Names).OnComplete(achievementDefinitionCallback);
        }

        void checkEntitlement()
        {
            Entitlements.IsUserEntitledToApplication().OnComplete(getEntitlementCallback);
        }

        void getUserNonce()
        {
            printOutputLine("Trying to get user nonce");

            Users.GetUserProof().OnComplete(userProofCallback);
        }

        void getLoggedInUser()
        {
            printOutputLine("Trying to get currently logged in user");
            Users.GetLoggedInUser().OnComplete(getUserCallback);
        }

        void getUser(string userID)
        {
            printOutputLine("Trying to get user " + userID);
            Users.Get(Convert.ToUInt64(userID)).OnComplete(getUserCallback);
        }

        void getLoggedInFriends()
        {
            printOutputLine("Trying to get friends of logged in user");
            Users.GetLoggedInUserFriends().OnComplete(getFriendsCallback);
        }

        void printOutputLine(String newLine)
        {
            dataOutput.text = "> " + newLine + System.Environment.NewLine + dataOutput.text;
        }

        void outputUserArray(UserList users)
        {
            foreach (User user in users)
            {
                printOutputLine("User: " + user.ID + " " + user.OculusID + " " + user.Presence);
            }
        }


        // Callbacks
        void userProofCallback(Message<UserProof> msg)
        {
            if (!msg.IsError)
            {
                printOutputLine("Received user nonce generation success");
                UserProof userNonce = msg.Data;
                printOutputLine("Nonce: " + userNonce.Value);
            }
            else
            {
                printOutputLine("Received user nonce generation error");
                Error error = msg.GetError();
                printOutputLine("Error: " + error.Message);
            }

        }

        void getEntitlementCallback(Message msg)
        {
            if (!msg.IsError)
            {
                printOutputLine("You are entitled to use this app.");
            }
            else
            {
                printOutputLine("You are NOT entitled to use this app.");
            }
        }

        void leaderboardGetCallback(Message<LeaderboardEntryList> msg)
        {
            if (!msg.IsError)
            {
                printOutputLine("Leaderboard entry get success.");
                var entries = msg.Data;

                foreach (var entry in entries)
                {
                    printOutputLine(entry.Rank + ". " + entry.User.OculusID + " " + entry.Score + " " + entry.Timestamp);
                }
            }
            else
            {
                printOutputLine("Received leaderboard get error");
                Error error = msg.GetError();
                printOutputLine("Error: " + error.Message);
            }
        }

       void leaderboardWriteCallback(Message msg)
        {
            if (!msg.IsError)
            {
                printOutputLine("Leaderboard entry write success.");
                var didUpdate = (Message<bool>)msg;

                if (didUpdate.Data)
                {
                    printOutputLine("Score updated.");
                }
                else
                {
                    printOutputLine("Score NOT updated.");
                }
            }
            else
            {
                printOutputLine("Received leaderboard write error");
                Error error = msg.GetError();
                printOutputLine("Error: " + error.Message);
            }
        }

       void achievementFieldsCallback(Message msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Achievement fields added.");
           }
           else
           {
               printOutputLine("Received achievement fields add error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void achievementCountCallback(Message msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Achievement count added.");
           }
           else
           {
               printOutputLine("Received achievement count add error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void achievementUnlockCallback(Message msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Achievement unlocked");
           }
           else
           {
               printOutputLine("Received achievement unlock error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void achievementProgressCallback(Message<AchievementProgressList> msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Received achievement progress success");
               AchievementProgressList progressList = msg.GetAchievementProgressList();

               foreach (var progress in progressList)
               {
                   if (progress.IsUnlocked)
                   {
                       printOutputLine("Achievement Unlocked");
                   }
                   else
                   {
                       printOutputLine("Achievement Locked");
                   }
                   printOutputLine("Current Bitfield: " + progress.Bitfield.ToString());
                   printOutputLine("Current Count: " + progress.Count.ToString());
               }
           }
           else
           {
               printOutputLine("Received achievement progress error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void achievementDefinitionCallback(Message<AchievementDefinitionList> msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Received achievement definitions success");
               AchievementDefinitionList definitionList = msg.GetAchievementDefinitions();

               foreach (var definition in definitionList)
               {
                   switch (definition.Type)
                   {
                       case AchievementType.Simple:
                           printOutputLine("Achievement Type: Simple");
                           break;
                       case AchievementType.Bitfield:
                           printOutputLine("Achievement Type: Bitfield");
                           printOutputLine("Bitfield Length: " + definition.BitfieldLength.ToString());
                           printOutputLine("Target: " + definition.Target.ToString());
                           break;
                       case AchievementType.Count:
                           printOutputLine("Achievement Type: Count");
                           printOutputLine("Target: " + definition.Target.ToString());
                           break;
                       case AchievementType.Unknown:
                       default:
                           printOutputLine("Achievement Type: Unknown");
                           break;
                   }
               }
           }
           else
           {
               printOutputLine("Received achievement definitions error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void getUserCallback(Message<User> msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Received get user success");
               User user = msg.Data;
               printOutputLine("User: " + user.ID + " " + user.OculusID + " " + user.Presence);
           }
           else
           {
               printOutputLine("Received get user error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

       void getFriendsCallback(Message<UserList> msg)
       {
           if (!msg.IsError)
           {
               printOutputLine("Received get friends success");
               UserList users = msg.Data;
               outputUserArray(users);
           }
           else
           {
               printOutputLine("Received get friends error");
               Error error = msg.GetError();
               printOutputLine("Error: " + error.Message);
           }
       }

    }
}
