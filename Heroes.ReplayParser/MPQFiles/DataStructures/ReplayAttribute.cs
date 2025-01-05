using System;
using System.Linq;
using System.Text;

namespace Heroes.ReplayParser.MPQFiles.DataStructures
{
    public class ReplayAttribute
    {
        public int Header { get; set; }
        public ReplayAttributeEventType AttributeType { get; set; }
        public int PlayerId { get; set; }
        public byte[] Value { get; set; }

        public override string ToString()
        {
            return $"Player: {PlayerId}, AttributeType: {AttributeType}, Value: {Encoding.UTF8.GetString(Value.Reverse().ToArray())}";
        }
    }
}
