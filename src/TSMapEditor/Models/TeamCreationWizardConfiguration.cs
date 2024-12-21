using Microsoft.Xna.Framework;
using System;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class TeamCreationWizardConfiguration
    {
        public TeamCreationWizardConfiguration(string name, Difficulty difficulty, HouseType houseType, string color)
        {
            Name = name;
            Difficulty = difficulty;
            HouseType = houseType;
            EditorColor = color;

            TaskForce = new TaskForce(name);
            TeamType = new TeamType(name);
        }

        public string Name { get; set; }
        public Difficulty Difficulty { get; set; }
        public HouseType HouseType { get; set; }
        public TaskForce TaskForce { get; set; }        
        public Script Script { get; set; }
        public TeamType TeamType { get; set; }
        public bool ShouldIncludeAITriggers = false;

        public static NamedColor[] SupportedColors => NamedColors.GenericSupportedNamedColors;

        private string _editorColor;
        public string EditorColor
        {
            get => _editorColor;
            set
            {
                _editorColor = value;

                if (_editorColor != null)
                {
                    int index = Array.FindIndex(SupportedColors, c => c.Name == value);
                    if (index > -1)
                    {
                        EditorColorValue = SupportedColors[index].Value;
                    }
                    else
                    {
                        // Only allow assigning colors that actually exist in the color table
                        _editorColor = null;
                    }
                }
            }
        }

        private Color EditorColorValue;
        public Color GetXNAColor()
        {
            if (EditorColor != null)
                return EditorColorValue;

            return Helpers.GetHouseTypeUITextColor(HouseType);
        }
    }
}
