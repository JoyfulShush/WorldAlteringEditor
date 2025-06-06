using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RandomBasedTriggersCreatedEventArgs : EventArgs
    {
        public RandomBasedTriggersCreatedEventArgs(Trigger baseTrigger)
        {
            BaseTrigger = baseTrigger;
        }

        public Trigger BaseTrigger { get; }
    }

    public class AddRandomBasedTriggerWindow : INItializableWindow
    {
        public AddRandomBasedTriggerWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox tbName;
        private EditorNumberTextBox tbNumTriggers;
        private EditorNumberTextBox tbElapsedTime;
        private EditorNumberTextBox tbDelay;
        private XNACheckBox cbEveryDiff;
        private EditorButton btnApply;

        public event EventHandler<RandomBasedTriggersCreatedEventArgs> RandomBasedTriggersCreated;

        public override void Initialize()
        {
            Name = nameof(AddRandomBasedTriggerWindow);
            base.Initialize();

            tbName = FindChild<EditorTextBox>(nameof(tbName));
            tbName.AllowComma = false;

            tbNumTriggers = FindChild<EditorNumberTextBox>(nameof(tbNumTriggers));
            tbNumTriggers.AllowComma = false;
            tbNumTriggers.AllowDecimals = false;

            tbElapsedTime = FindChild<EditorNumberTextBox>(nameof(tbElapsedTime));
            tbElapsedTime.AllowComma = false;
            tbElapsedTime.AllowDecimals = false;

            tbDelay = FindChild<EditorNumberTextBox>(nameof(tbDelay));
            tbDelay.AllowComma = false;
            tbDelay.AllowDecimals = false;

            cbEveryDiff = FindChild<XNACheckBox>(nameof(cbEveryDiff));
            btnApply = FindChild<EditorButton>(nameof(btnApply));

            btnApply.LeftClick += BtnApply_LeftClick;
        }

        public void BtnApply_LeftClick(object sender, EventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            string name = tbName.Text;
            int elapsedTime = tbElapsedTime.Value;
            int count = tbNumTriggers.Value;
            int delay = tbDelay.Value;
            bool createForEveryDifficulty = cbEveryDiff.Checked;

            List<Trigger> baseTriggers = [];

            if (createForEveryDifficulty)
            {
                baseTriggers.Add(CreateRandomBasedTriggers(name, elapsedTime, count, delay, Difficulty.Hard));
                baseTriggers.Add(CreateRandomBasedTriggers(name, elapsedTime, count, delay, Difficulty.Medium));
                baseTriggers.Add(CreateRandomBasedTriggers(name, elapsedTime, count, delay, Difficulty.Easy));
            } else
            {
                baseTriggers.Add(CreateRandomBasedTriggers(name, elapsedTime, count, delay));
            }

            Hide();
            RandomBasedTriggersCreated?.Invoke(this, new RandomBasedTriggersCreatedEventArgs(baseTriggers[0]));
        }

        private Trigger CreateRandomBasedTriggers(string name, int elapsedTime, int count, int delay, Difficulty? difficulty = null)
        {
            if (difficulty != null)
            {
                name = $"{difficulty.ToString()[0]} {name}";
            }

            var baseTrigger = CreateBaseTrigger(name, elapsedTime, difficulty);
            var childTriggers = CreateChildTriggers(name, count, delay);
            AssociateTriggers(baseTrigger, childTriggers);

            return baseTrigger;
        }

        private bool Validate()
        {
            if (tbName.Text == string.Empty)
            {
                EditorMessageBox.Show(WindowManager, "Missing Trigger Name",
                    "Please enter a name for the triggers", MessageBoxButtons.OK);
                return false;
            }

            if (tbNumTriggers.Value < 2)
            {
                EditorMessageBox.Show(WindowManager, "Invalid Number of Triggers",
                    "Please enter a value of 2 or more", MessageBoxButtons.OK);
                return false;
            }

            if (tbElapsedTime.Value < 0)
            {
                EditorMessageBox.Show(WindowManager, "Invalid Elapsed Time",
                    "Please enter a value of 0 or more", MessageBoxButtons.OK);
                return false;
            }

            if (tbDelay.Value < 10)
            {
                EditorMessageBox.Show(WindowManager, "Invalid Random Delay",
                    "Please enter a value of 10 or more", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        private Trigger CreateBaseTrigger(string name, int elapsedTime, Difficulty? difficulty) 
        {
            string triggerName = $"{name} base";

            var baseTrigger = new Trigger(map.GetNewUniqueInternalId());
            baseTrigger.Name = triggerName;
            baseTrigger.HouseType = "Neutral";

            if (difficulty != null)
            {                
                baseTrigger.Hard = difficulty == Difficulty.Hard;
                baseTrigger.Normal = difficulty == Difficulty.Medium;
                baseTrigger.Easy = difficulty == Difficulty.Easy;

                int diffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == $"Difficulty {difficulty}");

                if (diffGlobalVariableIndex < 0)
                {
                    Logger.Log($"{nameof(AddRandomBasedTriggerWindow)}.{nameof(CreateBaseTrigger)}: {difficulty} difficulty global variable not found!");

                    EditorMessageBox.Show(WindowManager, "Invalid Difficulty",
                        $"The difficulty '{difficulty}' is not defined in the map's rules. Skipping Global Is Set event assignment to trigger", MessageBoxButtons.OK);
                } else
                {
                    var globalSetCondition = new TriggerCondition();
                    globalSetCondition.ConditionIndex = 27; // Global Is Set
                    globalSetCondition.Parameters[1] = diffGlobalVariableIndex.ToString();

                    baseTrigger.Conditions.Add(globalSetCondition);
                }
            }
            

            var elapsedTimeCondition = new TriggerCondition();
            elapsedTimeCondition.ConditionIndex = 13; // Elapsed Time
            elapsedTimeCondition.Parameters[1] = elapsedTime.ToString();

            baseTrigger.Conditions.Add(elapsedTimeCondition);

            map.Triggers.Add(baseTrigger);
            map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = baseTrigger.Name + " (tag)", Trigger = baseTrigger, Repeating = 2 });

            return baseTrigger;
        }

        private List<Trigger> CreateChildTriggers(string name, int count, int delay)
        {
            List<Trigger> triggers = [];

            for (int i = 0; i < count; i++)
            {
                var childTrigger = new Trigger(map.GetNewUniqueInternalId());
                childTrigger.Name = $"{name} {i + 1}";
                childTrigger.HouseType = "Neutral";
                childTrigger.Disabled = true;

                var randomDelayCondition = new TriggerCondition();
                randomDelayCondition.ConditionIndex = 51; // Random Delay
                randomDelayCondition.Parameters[1] = delay.ToString();

                childTrigger.Conditions.Add(randomDelayCondition);

                map.Triggers.Add(childTrigger);
                map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = childTrigger.Name + " (tag)", Trigger = childTrigger, Repeating = 2 });

                triggers.Add(childTrigger);
            }

            return triggers;
        }

        private void AssociateTriggers(Trigger baseTrigger, List<Trigger> childTriggers)
        {            
            foreach (var childTrigger in childTriggers)
            {
                // base trigger needs to enable each of the child triggers
                var enableTriggerAction = new TriggerAction();
                enableTriggerAction.ActionIndex = 53; // Enable Trigger
                enableTriggerAction.Parameters[0] = "2";
                enableTriggerAction.Parameters[1] = childTrigger.ID;

                baseTrigger.Actions.Add(enableTriggerAction);

                // each child trigger needs to disable itself and each other child trigger
                foreach (var siblingTrigger in childTriggers)
                {
                    var disableTriggerAction = new TriggerAction();
                    disableTriggerAction.ActionIndex = 54; // Disable Trigger
                    disableTriggerAction.Parameters[0] = "2";
                    disableTriggerAction.Parameters[1] = siblingTrigger.ID;

                    childTrigger.Actions.Add(disableTriggerAction);
                }
            }
        }

        public void Open() 
        {
            Show();
            ResetValues();
        }

        public void ResetValues()
        {
            tbName.Text = string.Empty;
            tbNumTriggers.Value = 3;
            tbElapsedTime.Value = 100;
            tbDelay.Value = 10;
            cbEveryDiff.Checked = false;
        }
    }
}
