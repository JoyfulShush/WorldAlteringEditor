using System;
using System.Globalization;
using System.IO;

namespace TSMapEditor.Models
{
    public class TriggerAction : ICloneable
    {
        public const int PARAM_COUNT = 7;
        public const int INI_VALUE_COUNT = PARAM_COUNT + 1;

        public TriggerAction()
        {
            for (int i = 0; i < Parameters.Length - 1; i++)
                Parameters[i] = "0";

            Parameters[PARAM_COUNT - 1] = "A";
        }

        public int ActionIndex { get; set; }
        public string[] Parameters { get; private set; } = new string[PARAM_COUNT];

        public string ParamToString(int index)
        {
            if (string.IsNullOrWhiteSpace(Parameters[index]))
                return "0";

            return Parameters[index];
        }

        public object Clone() => DoClone();

        public TriggerAction DoClone()
        {
            TriggerAction clone = (TriggerAction)MemberwiseClone();
            clone.Parameters = new string[Parameters.Length];
            Array.Copy(Parameters, clone.Parameters, Parameters.Length);

            return clone;
        }

        public static TriggerAction ParseFromArray(string[] array, int startIndex)
        {
            if (startIndex + INI_VALUE_COUNT > array.Length)
                return null;

            int actionIndex = int.Parse(array[startIndex], CultureInfo.InvariantCulture);

            var triggerAction = new TriggerAction();
            triggerAction.ActionIndex = actionIndex;
            for (int i = 0; i < PARAM_COUNT; i++)
                triggerAction.Parameters[i] = array[startIndex + 1 + i];

            return triggerAction;
        }

        public void Serialize(MemoryStream memoryStream)
        {
            memoryStream.Write(BitConverter.GetBytes(ActionIndex));

            foreach (var parameter in Parameters)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(parameter);
                memoryStream.Write(BitConverter.GetBytes(bytes.Length));
                memoryStream.Write(bytes);
            }
        }

        public void Deserialize(MemoryStream memoryStream)
        {
            byte[] buffer = new byte[4];

            memoryStream.Read(buffer, 0, 4);
            ActionIndex = BitConverter.ToInt32(buffer);

            for (int i = 0; i < Parameters.Length; i++)
            {
                memoryStream.Read(buffer, 0, 4);
                int stringLength = BitConverter.ToInt32(buffer);

                byte[] stringBytes = new byte[stringLength];
                memoryStream.Read(stringBytes, 0, stringLength);

                Parameters[i] = System.Text.Encoding.UTF8.GetString(stringBytes);
            }
        }
    }
}
