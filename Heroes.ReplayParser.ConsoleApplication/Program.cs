using System;
using System.IO;
using System.Linq;
using Heroes.ReplayParser;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check if a replay file is provided as an argument
            string replayFilePath;
            if (args.Length > 0 && File.Exists(args[0]))
            {
                replayFilePath = args[0];
                Console.WriteLine($"Using provided replay file: {replayFilePath}");
            }
            else
            {
                // Fallback to selecting a random replay file
                var heroesAccountsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Heroes of the Storm\Accounts");
                replayFilePath = Directory.GetFiles(heroesAccountsFolder, "*.StormReplay", SearchOption.AllDirectories).OrderBy(i => Guid.NewGuid()).First();
                Console.WriteLine($"No replay file provided. Using random replay: {replayFilePath}");
            }

            // Attempt to parse the replay
            try
            {
                var (replayParseResult, replay) = DataParser.ParseReplay(replayFilePath, deleteFile: false, ParseOptions.DefaultParsing);

                if (replayParseResult == DataParser.ReplayParseResult.Success)
                {
                    // Debug the replay build
                    Console.WriteLine($"Replay Build: {replay.ReplayBuild}");

                    // Debug the map name
                    Console.WriteLine($"Map: {replay.Map}");

                    // Debug player data
                    Console.WriteLine("Player Data:");
                    foreach (var player in replay.Players.OrderByDescending(i => i.IsWinner))
                    {
                        Console.WriteLine($"Name: {player.Name}");
                        Console.WriteLine($"Is Winner: {player.IsWinner}");
                        Console.WriteLine($"Hero: {player.Character}");
                        Console.WriteLine($"Character Level: {player.CharacterLevel}");
                        Console.WriteLine($"Talents: {string.Join(",", player.Talents.Select(i => $"{i.TalentID}:{i.TalentName}"))}");
                        Console.WriteLine("---");
                    }

                    Console.WriteLine("Press Any Key to Close");
                }
                else
                {
                    Console.WriteLine($"Failed to Parse Replay: {replayParseResult}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred while parsing the replay: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }



            Console.Read();
        }
    }
}
