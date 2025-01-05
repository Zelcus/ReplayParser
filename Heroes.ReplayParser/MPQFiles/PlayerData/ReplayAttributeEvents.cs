using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Heroes.ReplayParser.MPQFiles.DataStructures;

namespace Heroes.ReplayParser.MPQFiles
{
    public static class ReplayAttributeEvents
    {
        public const string FileName = "replay.attributes.events";

        public static void Parse(Replay replay, byte[] buffer)
        {
            const int headerSize = 5;

            var attributes = new ReplayAttribute[BitConverter.ToInt32(buffer, headerSize)];

            const int initialOffset = 4 + headerSize;

            for (var i = 0; i < attributes.Length; i++)
            {
                var currentOffset = initialOffset + (i * 13);

                var attribute = new ReplayAttribute
                {
                    Header = BitConverter.ToInt32(buffer, currentOffset),
                    AttributeType = (ReplayAttributeEventType)BitConverter.ToInt32(buffer, currentOffset + 4),
                    PlayerId = buffer[currentOffset + 8],
                    Value = new byte[4]
                };

                Array.Copy(buffer, currentOffset + 9, attribute.Value, 0, 4);

                attributes[i] = attribute;
            }

            ApplyAttributes(replay, attributes.OrderBy(i => i.AttributeType).ToArray());

            /* var stringList = attributes.OrderBy(i => i.AttributeType);
            Console.WriteLine(stringList.Count()); */
        }

        /// <summary>
        /// Applies the set of attributes to a replay.
        /// </summary>
        /// <param name="replay">Replay to apply the attributes to.</param>
        private static void ApplyAttributes(Replay replay, ReplayAttribute[] Attributes)
        {
            // I'm not entirely sure this is the right encoding here. Might be unicode...
            var encoding = Encoding.UTF8;

            var attributes1 = new List<ReplayAttribute>();
            var attributes2 = new List<ReplayAttribute>();
            var attributes3 = new List<ReplayAttribute>();
            var attributes4 = new List<ReplayAttribute>();
            var attributesffa = new List<ReplayAttribute>();

            // The 'PlayerID' in attributes does not seem to match any existing player array
            // It almost matches the 'Replay.Player' array, except for games with less than 10 players
            var replayPlayersWithOpenSlotsIndex = 1;

            foreach (var attribute in Attributes)
                switch (attribute.AttributeType)
                {
                    case ReplayAttributeEventType.PlayerTypeAttribute:
                        {
                            var type = encoding.GetString(attribute.Value.Reverse().ToArray()).ToLower();

                            if (type == "comp" || type == "humn")
                                replay.PlayersWithOpenSlots[attribute.PlayerId - 1] = replay.Players[attribute.PlayerId - replayPlayersWithOpenSlotsIndex];

                            switch (type)
                            {
                                case "comp":
                                    replay.PlayersWithOpenSlots[attribute.PlayerId - 1].PlayerType = PlayerType.Computer;
                                    break;
                                case "humn":
                                    replay.PlayersWithOpenSlots[attribute.PlayerId - 1].PlayerType = PlayerType.Human;
                                    break;
                                // Less than 10 players in a Custom game
                                case "open":
                                    replayPlayersWithOpenSlotsIndex++;
                                    break;
                                default:
                                    throw new Exception("Unexpected value for PlayerType");
                            }

                            break;
                        }

                    case ReplayAttributeEventType.TeamSizeAttribute:
                        {
                            // This fixes issues with reversing the string before encoding. Without this, you get "\01v1"
                            replay.TeamSize = new string(encoding.GetString(attribute.Value, 0, 3).Reverse().ToArray());
                            break;
                        }

                    case ReplayAttributeEventType.DifficultyLevelAttribute:
                        {
                            var diffLevel = encoding.GetString(attribute.Value.Reverse().ToArray());
                            var player = replay.PlayersWithOpenSlots[attribute.PlayerId - 1];

                            if (player != null)
                                switch (diffLevel)
                                {
                                    case "VyEy":
                                        player.Difficulty = Difficulty.Beginner;
                                        break;
                                    case "Easy":
                                        player.Difficulty = Difficulty.Recruit;
                                        break;
                                    case "Medi":
                                        player.Difficulty = Difficulty.Adept;
                                        break;
                                    case "HdVH":
                                        player.Difficulty = Difficulty.Veteran;
                                        break;
                                    case "VyHd":
                                        player.Difficulty = Difficulty.Elite;
                                        break;
                                }

                            break;
                        }

                    case ReplayAttributeEventType.GameSpeedAttribute:
                        {
                            var speed = encoding.GetString(attribute.Value.Reverse().ToArray()).ToLower();

                            switch (speed)
                            {
                                case "slor":
                                    replay.GameSpeed = GameSpeed.Slower;
                                    break;
                                case "slow":
                                    replay.GameSpeed = GameSpeed.Slow;
                                    break;
                                case "norm":
                                    replay.GameSpeed = GameSpeed.Normal;
                                    break;
                                case "fast":
                                    replay.GameSpeed = GameSpeed.Fast;
                                    break;
                                case "fasr":
                                    replay.GameSpeed = GameSpeed.Faster;
                                    break;

                                    // Otherwise, Game Speed will remain "Unknown"
                            }

                            break;
                        }

                    case ReplayAttributeEventType.PlayerTeam1v1Attribute:
                        {
                            attributes1.Add(attribute);
                            break;
                        }

                    case ReplayAttributeEventType.PlayerTeam2v2Attribute:
                        {
                            attributes2.Add(attribute);
                            break;
                        }

                    case ReplayAttributeEventType.PlayerTeam3v3Attribute:
                        {
                            attributes3.Add(attribute);
                            break;
                        }

                    case ReplayAttributeEventType.PlayerTeam4v4Attribute:
                        {
                            attributes4.Add(attribute);
                            break;
                        }

                    case ReplayAttributeEventType.PlayerTeamFFAAttribute:
                        {
                            attributesffa.Add(attribute);
                            break;
                        }


                    case ReplayAttributeEventType.GameTypeAttribute:
                        {
                            switch (encoding.GetString(attribute.Value.Reverse().ToArray()).ToLower().Trim('\0'))
                            {
                                case "priv":
                                    replay.GameMode = GameMode.Custom;
                                    break;
                                case "amm":
                                    if (replay.ReplayBuild < 33684)
                                        replay.GameMode = GameMode.QuickMatch;
                                    break;
                                default:
                                    throw new Exception("Unexpected Game Type");
                            }

                            break;
                        }

                    case ReplayAttributeEventType.HeroAttributeId:
                        {
                            if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                            {
                                replay.PlayersWithOpenSlots[attribute.PlayerId - 1].IsAutoSelect = encoding.GetString(attribute.Value.Reverse().ToArray()) == "Rand";
                                replay.PlayersWithOpenSlots[attribute.PlayerId - 1].HeroAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                            }
                            break;
                        }

                    case ReplayAttributeEventType.SkinAndSkinTintAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].IsAutoSelect = encoding.GetString(attribute.Value.Reverse().ToArray()) == "Rand";
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].SkinAndSkinTintAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        }
                        break;

                    case ReplayAttributeEventType.MountAndMountTintAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].MountAndMountTintAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        }
                        break;

                    case ReplayAttributeEventType.BannerAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].BannerAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        }
                        break;

                    case ReplayAttributeEventType.SprayAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].SprayAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        }
                        break;

                    case ReplayAttributeEventType.VoiceLineAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].VoiceLineAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        }
                        break;

                    case ReplayAttributeEventType.AnnouncerAttributeId:
                        if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] != null)
                        {
                            replay.PlayersWithOpenSlots[attribute.PlayerId - 1].AnnouncerPackAttributeId = encoding.GetString(attribute.Value.Reverse().ToArray());
                        }
                        break;

                    case ReplayAttributeEventType.CharacterLevel:
                        {
                            if (replay.PlayersWithOpenSlots[attribute.PlayerId - 1] == null)
                                break;

                            var characterLevel = int.Parse(encoding.GetString(attribute.Value.Reverse().ToArray()));
                            var player = replay.PlayersWithOpenSlots[attribute.PlayerId - 1];
                            player.CharacterLevel = characterLevel;

                            if (player.IsAutoSelect && player.CharacterLevel > 1)
                                player.IsAutoSelect = false;
                            break;
                        }

                    case ReplayAttributeEventType.LobbyMode:
                        {
                            if (replay.ReplayBuild < 43905 && replay.GameMode != GameMode.Custom)
                                switch (encoding.GetString(attribute.Value.Reverse().ToArray()).ToLower().Trim('\0'))
                                {
                                    case "stan":
                                        replay.GameMode = GameMode.QuickMatch;
                                        break;
                                    case "drft":
                                        replay.GameMode = GameMode.HeroLeague;
                                        break;
                                }
                        }
                        break;

                    case ReplayAttributeEventType.ReadyMode:
                        if (replay.ReplayBuild < 43905 && replay.GameMode == GameMode.HeroLeague && encoding.GetString(attribute.Value.Reverse().ToArray()).ToLower().Trim('\0') == "fcfs")
                            replay.GameMode = GameMode.TeamLeague;
                        break;

                    case (ReplayAttributeEventType)4011: // What is this? Draft order?
                        break;
                    case (ReplayAttributeEventType)4016: // What is this? Always '1' in Hero League
                                                         // if (replay.GameMode == GameMode.HeroLeague && int.Parse(encoding.GetString(attribute.Value.Reverse().ToArray())) != 1)
                                                         // Console.WriteLine("WAAT!?");
                        break;
                    case (ReplayAttributeEventType)4017: // What is this? Always '5' in Hero League
                                                         // if (replay.GameMode == GameMode.HeroLeague && int.Parse(encoding.GetString(attribute.Value.Reverse().ToArray())) != 5)
                                                         // Console.WriteLine("WAAT!?");
                        break;

                    case ReplayAttributeEventType.DraftBanMode:
                        // Options: No Ban (""), One Ban ("1ban"), Two Ban ("2ban"), Mid Ban ("Mban"), Three Ban ("3ban")
                        break;

                    case ReplayAttributeEventType.DraftTeam1BanChooserSlot:
                    case ReplayAttributeEventType.DraftTeam2BanChooserSlot:
                        // For Ranked Play, this is always "Hmmr" -> Highest MMR
                        break;

                    case ReplayAttributeEventType.DraftTeam1Ban1LockedIn:
                    case ReplayAttributeEventType.DraftTeam1Ban2LockedIn:
                    case ReplayAttributeEventType.DraftTeam2Ban1LockedIn:
                    case ReplayAttributeEventType.DraftTeam2Ban2LockedIn:
                        // So far I've only seen an empty string here
                        break;

                    case ReplayAttributeEventType.DraftTeam1Ban1:
                    case ReplayAttributeEventType.DraftTeam1Ban2:
                    case ReplayAttributeEventType.DraftTeam1Ban3:
                    case ReplayAttributeEventType.DraftTeam2Ban1:
                    case ReplayAttributeEventType.DraftTeam2Ban2:
                    case ReplayAttributeEventType.DraftTeam2Ban3:
                        var draftTeamBanValue = encoding.GetString(attribute.Value.Reverse().ToArray()).Trim('\0');
                        if (draftTeamBanValue != "")
                            switch (attribute.AttributeType)
                            {
                                case ReplayAttributeEventType.DraftTeam1Ban1:
                                    replay.TeamHeroBans[0][0] = draftTeamBanValue;
                                    break;
                                case ReplayAttributeEventType.DraftTeam1Ban2:
                                    replay.TeamHeroBans[0][1] = draftTeamBanValue;
                                    break;
                                case ReplayAttributeEventType.DraftTeam1Ban3:
                                    replay.TeamHeroBans[0][2] = draftTeamBanValue;
                                    break;
                                case ReplayAttributeEventType.DraftTeam2Ban1:
                                    replay.TeamHeroBans[1][0] = draftTeamBanValue;
                                    break;
                                case ReplayAttributeEventType.DraftTeam2Ban2:
                                    replay.TeamHeroBans[1][1] = draftTeamBanValue;
                                    break;
                                case ReplayAttributeEventType.DraftTeam2Ban3:
                                    replay.TeamHeroBans[1][2] = draftTeamBanValue;
                                    break;
                            }
                        break;
                }

            List<ReplayAttribute> currentList = null;

            if (replay.TeamSize.Equals("1v1"))
                currentList = attributes1;
            else if (replay.TeamSize.Equals("2v2"))
                currentList = attributes2;
            else if (replay.TeamSize.Equals("3v3"))
                currentList = attributes3;
            else if (replay.TeamSize.Equals("4v4"))
                currentList = attributes4;
            else if (replay.TeamSize.Equals("FFA"))
                currentList = attributesffa;

            /* Team is parsed in ReplayDetails.cs, this is unnecessary
            if (currentList != null)
                foreach (var att in currentList)
                    // Reverse the values then parse, you don't notice the effects of this until theres 10+ teams o.o
                    replay.PlayersWithOpenSlots[att.PlayerId - 1].Team = int.Parse(encoding.GetString(att.Value.Reverse().ToArray()).Trim('\0', 'T')); */
        }

        private class ReplayAttribute
        {
            public int Header { get; set; }
            public ReplayAttributeEventType AttributeType { get; set; }
            public int PlayerId { get; set; }
            public byte[] Value { get; set; }

            public override string ToString()
            {
                return "Player: " + PlayerId + ", AttributeType: " + AttributeType.ToString() + ", Value: " + Encoding.UTF8.GetString(Value.Reverse().ToArray());
            }
        }
    }
}
