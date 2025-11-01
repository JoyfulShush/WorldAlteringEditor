using Rampastring.Tools;
using System;
using System.IO;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A trigger tag. Tags are responsible for activating map triggers.
    /// </summary>
    public class Tag : IIDContainer
    {
        public const int REPEAT_TYPE_MAX = 2;

        public string GetInternalID() => ID;
        public void SetInternalID(string id) => ID = id;

        public string ID { get; set; }
        public int Repeating { get; set; }
        public string Name { get; set; }
        public Trigger Trigger { get; set; }

        public void WriteToIniSection(IniSection iniSection)
        {
            iniSection.SetStringValue(ID, $"{Repeating},{Name},{Trigger.ID}");
        }

        public string GetDisplayString() => Name + " (" + ID + ")";

        public void Serialize(MemoryStream memoryStream)
        {
            byte[] bytes;

            bytes = System.Text.Encoding.UTF8.GetBytes(Name);
            memoryStream.Write(BitConverter.GetBytes(bytes.Length));
            memoryStream.Write(bytes);

            memoryStream.Write(BitConverter.GetBytes(Repeating));
        }

        public void Deserialize(MemoryStream memoryStream)
        {
            byte[] buffer = new byte[4];
            byte[] stringBytes;
            int length;
            
            memoryStream.Read(buffer, 0, buffer.Length);
            length = BitConverter.ToInt32(buffer, 0);
            stringBytes = new byte[length];
            memoryStream.Read(stringBytes, 0, stringBytes.Length);
            Name = System.Text.Encoding.UTF8.GetString(stringBytes);
            
            memoryStream.Read(buffer, 0, buffer.Length);
            Repeating = BitConverter.ToInt32(buffer, 0);
        }
    }
}
