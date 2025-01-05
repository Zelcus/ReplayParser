using System;

namespace Heroes.ReplayParser.MPQFiles.DataStructures
{
    public class GameEvent
    {
        public GameEventType EventType { get; set; }
        public Player Player { get; set; } = null;
        public bool IsGlobal { get; set; } = false;
        public int TicksElapsed { get; set; }
        public TimeSpan TimeSpan => new TimeSpan(0, 0, (int)(TicksElapsed / 16.0));
        public TrackerEventStructure Data { get; set; } = null;

        public override string ToString()
        {
            return Data?.ToString();
        }
    }
}