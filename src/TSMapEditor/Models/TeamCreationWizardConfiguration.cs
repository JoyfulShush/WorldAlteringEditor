using Microsoft.Xna.Framework;
using System;
using TSMapEditor.Misc;

namespace TSMapEditor.Models
{
    public class TeamCreationWizardConfiguration
    {
        public TeamCreationWizardConfiguration(Map map, string name, string fullName, Difficulty difficulty, HouseType houseType, string color)
        {
            this.map = map;
            Name = name;
            FullName = fullName;
            Difficulty = difficulty;
            HouseType = houseType;
            EditorColor = color;

            TaskForce = new TaskForce(fullName);
            TeamType = new TeamType(fullName);
            AITriggerType = new AITriggerType(fullName);
        }

        private readonly Map map;
        public string Name { get; set; }
        // name with difficulty parsing
        public string FullName { get; set; }
        public Difficulty Difficulty { get; set; }
        public HouseType HouseType { get; set; }
        public TaskForce TaskForce { get; set; }
        public bool EditedTaskForce = false;
        public Script Script { get; set; }
        public bool EditedScript = false;
        public TeamType TeamType { get; set; }
        public bool EditedTeamType = false;
        
        public AITriggerType AITriggerType { get; set; }
        public bool EditedAITriggers = false;
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
            TaskForce.Name = FullName;

            map.TaskForces.Add(TaskForce);
        }

        private void CreateTeamType()
        {
            TeamType.SetInternalID(map.GetNewUniqueInternalId());
            TeamType.Name = FullName;
            TeamType.EditorColor = EditorColor;
            TeamType.HouseType = HouseType;
            TeamType.TaskForce = TaskForce;
            TeamType.Script = Script;

            map.TeamTypes.Add(TeamType);
        }

        private void CreateAITrigger()
        {
            AITriggerType.SetInternalID(map.GetNewUniqueInternalId());
            AITriggerType.Name = FullName;
            AITriggerType.PrimaryTeam = TeamType;
            AITriggerType.OwnerName = HouseType.ININame;
            AITriggerType.Side = map.Rules.Sides.FindIndex(side => side == HouseType.Side) + 1;
            AITriggerType.Easy = Difficulty == Difficulty.Easy;
            AITriggerType.Medium = Difficulty == Difficulty.Medium;
            AITriggerType.Hard = Difficulty == Difficulty.Hard;

            map.AITriggerTypes.Add(AITriggerType);
        }

        public string GetFinishMessageText()
        {
            return $"Successfully created TaskForces{(ShouldIncludeAITriggers ? ", TeamTypes and AITriggers" : " and TeamTypes")} for team '{Name}'! You can find them in their respective menus.";
        }
    }
}
