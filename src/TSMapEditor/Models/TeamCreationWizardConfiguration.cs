using Microsoft.Xna.Framework;
using System;
using TSMapEditor.Initialization;
using TSMapEditor.Misc;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.Models
{
    public class TeamCreationWizardConfiguration
    {
        public TeamCreationWizardConfiguration(Map map, string name, Difficulty difficulty, HouseType houseType, string color)
        {
            this.map = map;
            Name = name;
            Difficulty = difficulty;
            HouseType = houseType;
            EditorColor = color;

            TaskForce = new TaskForce(name);
            TeamType = new TeamType(name);
        }

        private readonly Map map;
        public string Name { get; set; }
        public Difficulty Difficulty { get; set; }
        public HouseType HouseType { get; set; }
        public TaskForce TaskForce { get; set; }        
        public Script Script { get; set; }
        public TeamType TeamType { get; set; }
        
        public AITriggerType AITriggerType { get; set; }
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

        public void ProcessConfiguration()
        {
            CreateTaskForce();
            CreateTeamType();

            if (ShouldIncludeAITriggers)
            {
                CreateAITrigger();
            }
        }

        private void CreateTaskForce()
        {
            TaskForce.SetInternalID(map.GetNewUniqueInternalId());
            TaskForce.Name = Name;

            map.TaskForces.Add(TaskForce);
        }

        private void CreateTeamType()
        {
            TeamType.SetInternalID(map.GetNewUniqueInternalId());
            TeamType.Name = Name;
            TeamType.EditorColor = EditorColor;
            TeamType.HouseType = HouseType;
            TeamType.TaskForce = TaskForce;
            TeamType.Script = Script;

            map.TeamTypes.Add(TeamType);
        }

        private void CreateAITrigger()
        {
            AITriggerType.SetInternalID(map.GetNewUniqueInternalId());
            AITriggerType.Name = Name;
            AITriggerType.PrimaryTeam = TeamType;
            AITriggerType.OwnerName = HouseType.ININame;
            AITriggerType.Side = map.Rules.Sides.FindIndex(side => side == HouseType.Side);
            if (Difficulty == Difficulty.Easy)
            {
                AITriggerType.Easy = true;
                AITriggerType.Medium = false;
                AITriggerType.Hard = false;
            }
            else if (Difficulty == Difficulty.Medium)
            {
                AITriggerType.Easy = false;
                AITriggerType.Medium = true;
                AITriggerType.Hard = false;
            }
            else
            {
                AITriggerType.Easy = false;
                AITriggerType.Medium = false;
                AITriggerType.Hard = true;
            }
        }
    }
}
