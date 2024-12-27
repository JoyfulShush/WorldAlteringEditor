using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TeamCreationWizard
{    
    public class ScriptWizardStepEventArgs : EventArgs
    {
        public ScriptWizardStepEventArgs(List<TeamCreationWizardConfiguration> wizardConfigurations)
        {
            WizardConfigurations = wizardConfigurations;
        }
        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; }
    }

    public class TaskForceWizardStepWindow : INItializableWindow
    {
        public TaskForceWizardStepWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<ScriptWizardStepEventArgs> ScriptsWizardStepOpened;

        private EditorListBox lbDifficulties;                
        private EditorNumberTextBox tbGroup;        
        private EditorListBox lbUnitEntries;
        private XNALabel lblCost;
        private EditorButton btnAddUnit;
        private EditorButton btnDeleteUnit;        
        private EditorNumberTextBox tbUnitCount;
        private EditorSuggestionTextBox tbSearchUnit;        
        private EditorListBox lbUnitType;
        private EditorButton btnCloneOtherDiffs;
        private EditorButton btnNext;

        private XNAContextMenu unitListContextMenu;

        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; set; }
        private TeamCreationWizardConfiguration currentWizardConfiguration;

        public override void Initialize()
        {
            Name = nameof(TaskForceWizardStepWindow);
            base.Initialize();

            lbDifficulties = FindChild<EditorListBox>(nameof(lbDifficulties));                        
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            lbUnitEntries = FindChild<EditorListBox>(nameof(lbUnitEntries));
            lblCost = FindChild<XNALabel>(nameof(lblCost));
            btnAddUnit = FindChild<EditorButton>(nameof(btnAddUnit));
            btnDeleteUnit = FindChild<EditorButton>(nameof(btnDeleteUnit));            
            tbUnitCount = FindChild<EditorNumberTextBox>(nameof(tbUnitCount));
            tbSearchUnit = FindChild<EditorSuggestionTextBox>(nameof(tbSearchUnit));            
            lbUnitType = FindChild<EditorListBox>(nameof(lbUnitType));
            btnCloneOtherDiffs = FindChild<EditorButton>(nameof(btnCloneOtherDiffs));
            btnNext = FindChild<EditorButton>(nameof(btnNext));

            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
            btnAddUnit.LeftClick += BtnAddUnit_LeftClick;
            btnDeleteUnit.LeftClick += BtnDeleteUnit_LeftClick;
            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;
            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;
            tbSearchUnit.TextChanged += TbSearchUnit_TextChanged;
            tbSearchUnit.EnterPressed += TbSearchUnit_EnterPressed;
            tbGroup.TextChanged += TbGroup_TextChanged;
            btnCloneOtherDiffs.LeftClick += BtnCloneOtherDiffs_LeftClick;
            btnNext.LeftClick += BtnNext_LeftClick;

            unitListContextMenu = new XNAContextMenu(WindowManager);
            unitListContextMenu.Name = nameof(unitListContextMenu);
            unitListContextMenu.Width = 150;
            unitListContextMenu.AddItem("Move Up", UnitListContextMenu_MoveUp, () => IsCurrentTaskForceExists() && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex > 0);
            unitListContextMenu.AddItem("Move Down", UnitListContextMenu_MoveDown, () => IsCurrentTaskForceExists() && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex < lbUnitEntries.Items.Count - 1);
            unitListContextMenu.AddItem("Clone Unit Entry", UnitListContextMenu_CloneEntry, () => IsCurrentTaskForceExists() && lbUnitEntries.SelectedItem != null && currentWizardConfiguration.TaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Insert New Unit Here", UnitListContextMenu_Insert, () => IsCurrentTaskForceExists() && lbUnitEntries.SelectedItem != null && currentWizardConfiguration.TaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Delete Unit Entry", UnitListContextMenu_Delete, () => IsCurrentTaskForceExists() && lbUnitEntries.SelectedItem != null);
            AddChild(unitListContextMenu);
            lbUnitEntries.AllowRightClickUnselect = false;
            lbUnitEntries.RightClick += (s, e) => { if (IsCurrentTaskForceExists()) { lbUnitEntries.SelectedIndex = lbUnitEntries.HoveredIndex; unitListContextMenu.Open(GetCursorPoint()); } };

            ListUnits();
        }

        public void ResetForms()
        {
            currentWizardConfiguration = null;

            ClearTaskForceFields();
            LoadDifficulties();
        }

        private void LoadDifficulties()
        {
            lbDifficulties.SelectedIndexChanged -= LbDifficulties_SelectedIndexChanged;
            lbDifficulties.Clear();
            lbDifficulties.SelectedIndex = -1;            
            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
            
            foreach (var wizardConfiguration in WizardConfigurations)
            {
                lbDifficulties.AddItem(wizardConfiguration.Difficulty.ToString());
            }
            lbDifficulties.SelectedIndex = 0;
        }

        private void ClearTaskForceFields()
        {            
            tbGroup.Text = "-1";
            lbUnitEntries.Clear();
            lblCost.Text = "0$";
            tbUnitCount.Text = string.Empty;
        }

        private void ListUnits()
        {
            var gameObjectTypeList = new List<GameObjectType>();
            gameObjectTypeList.AddRange(map.Rules.AircraftTypes);
            gameObjectTypeList.AddRange(map.Rules.InfantryTypes);
            gameObjectTypeList.AddRange(map.Rules.UnitTypes);
            gameObjectTypeList = gameObjectTypeList.OrderBy(g => g.ININame).ToList();

            foreach (GameObjectType objectType in gameObjectTypeList)
            {
                lbUnitType.AddItem(new XNAListBoxItem() { Text = objectType.ININame + " (" + objectType.GetEditorDisplayName() + ")", Tag = objectType });
            }
        }

        private void BtnDeleteUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;
            taskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(taskForce, true);
        }

        private void BtnAddUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (!IsCurrentTaskForceExists())
                return;

            var taskForce = currentWizardConfiguration.TaskForce;

            if (!taskForce.HasFreeTechnoSlot())
                return;

            taskForce.AddTechnoEntry(
                new TaskForceTechnoEntry()
                {
                    Count = 1,
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag
                });

            EditTaskForce(taskForce, true);
            lbUnitEntries.SelectedIndex = lbUnitEntries.Items.Count - 1;
        }

        private void EditTaskForce(TaskForce taskForce, bool markTaskForceEdited)
        {
            RefreshTaskForceCost();

            if (taskForce == null)
            {
                ClearTaskForceFields();
                return;
            }

            tbSearchUnit.Text = tbSearchUnit.Suggestion;
            tbGroup.Value = taskForce.Group;
            lbUnitEntries.SelectedIndexChanged -= LbUnitEntries_SelectedIndexChanged;
            lbUnitEntries.Clear();

            for (int i = 0; i < taskForce.TechnoTypes.Length; i++)
            {
                var taskForceTechno = taskForce.TechnoTypes[i];
                if (taskForceTechno == null)
                    break;

                lbUnitEntries.AddItem(GetUnitEntryText(taskForceTechno));
            }

            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;

            if (lbUnitEntries.SelectedItem == null && lbUnitEntries.Items.Count > 0)
            {
                lbUnitEntries.SelectedIndex = 0;
            }
            else
            {
                LbUnitEntries_SelectedIndexChanged(this, EventArgs.Empty);
            }

            if (markTaskForceEdited)
            {
                currentWizardConfiguration.EditedTaskForce = true;
            }
        }

        private void RefreshTaskForceCost()
        {
            if (!IsCurrentTaskForceExists())
            {
                lblCost.Text = string.Empty;
                return;
            }

            var taskForce = currentWizardConfiguration.TaskForce;

            int cost = 0;
            foreach (var technoEntry in taskForce.TechnoTypes)
            {
                if (technoEntry != null)
                    cost += technoEntry.TechnoType.Cost * technoEntry.Count;
            }

            lblCost.Text = cost.ToString(CultureInfo.InvariantCulture) + "$";
        }

        private bool IsCurrentTaskForceExists()
        {
            if (currentWizardConfiguration == null || currentWizardConfiguration.TaskForce == null)
            {
                return false;
            }

            return true;
        }

        private void LbUnitEntries_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                tbUnitCount.Text = string.Empty;
                return;
            }

            var taskForce = currentWizardConfiguration.TaskForce;

            var taskForceTechno = taskForce.TechnoTypes[lbUnitEntries.SelectedIndex];

            lbUnitType.SelectedIndexChanged -= LbUnitType_SelectedIndexChanged;
            lbUnitType.SelectedIndex = lbUnitType.Items.FindIndex(u => ((TechnoType)u.Tag) == taskForceTechno.TechnoType);
            lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;

            tbUnitCount.TextChanged -= TbUnitCount_TextChanged;
            tbUnitCount.Value = taskForceTechno.Count;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;
        }

        private string GetUnitEntryText(TaskForceTechnoEntry taskForceTechno)
        {
            return $"{taskForceTechno.Count} {taskForceTechno.TechnoType.ININame} ({taskForceTechno.TechnoType.GetEditorDisplayName()})";
        }

        private void LbUnitType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbUnitType.SelectedItem == null)
                return;

            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForce = currentWizardConfiguration.TaskForce;

            var taskForceTechno = taskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.TechnoType = (TechnoType)lbUnitType.SelectedItem.Tag;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void TbUnitCount_TextChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForce = currentWizardConfiguration.TaskForce;

            var taskForceTechno = taskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.Count = tbUnitCount.Value;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void TbSearchUnit_EnterPressed(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            FindNextMatchingUnit();
        }

        private void TbSearchUnit_TextChanged(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            lbUnitType.SelectedIndex = -1;
            FindNextMatchingUnit();
        }

        private void FindNextMatchingUnit()
        {
            for (int i = lbUnitType.SelectedIndex + 1; i < lbUnitType.Items.Count; i++)
            {
                var gameObjectType = (TechnoType)lbUnitType.Items[i].Tag;

                if (gameObjectType.ININame.ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()) ||
                    gameObjectType.GetEditorDisplayName().ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()))
                {
                    lbUnitType.SelectedIndex = i;
                    lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
                    break;
                }
            }
        }

        private void UnitListContextMenu_MoveUp()
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex <= 0)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;

            int viewTop = lbUnitEntries.ViewTop;
            taskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex - 1, lbUnitEntries.SelectedIndex);
            EditTaskForce(taskForce, true);
            lbUnitEntries.SelectedIndex--;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_MoveDown()
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex >= lbUnitEntries.Items.Count - 1)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;

            int viewTop = lbUnitEntries.ViewTop;
            taskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex, lbUnitEntries.SelectedIndex + 1);
            EditTaskForce(taskForce, true);
            lbUnitEntries.SelectedIndex++;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_CloneEntry()
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;
            if (!taskForce.HasFreeTechnoSlot())
                return;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex + 1;

            var clonedEntry = taskForce.TechnoTypes[lbUnitEntries.SelectedIndex].Clone();
            taskForce.InsertTechnoEntry(newIndex, clonedEntry);
            EditTaskForce(taskForce, true);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Insert()
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex;

            taskForce.InsertTechnoEntry(lbUnitEntries.SelectedIndex,
                new TaskForceTechnoEntry()
                {
                    Count = 1,
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag
                });

            EditTaskForce(taskForce, true);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Delete()
        {
            if (!IsCurrentTaskForceExists() || lbUnitEntries.SelectedItem == null)
                return;

            var taskForce = currentWizardConfiguration.TaskForce;

            int viewTop = lbUnitEntries.ViewTop;
            taskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(taskForce, true);
            lbUnitEntries.ViewTop = viewTop;
        }

        private void LbDifficulties_SelectedIndexChanged(object sender, EventArgs e)
        {
            string difficulty = lbDifficulties.SelectedItem.Text;
            if (string.IsNullOrEmpty(difficulty))
                return;

            var selectedWizardConfiguration = WizardConfigurations.Find(wc => wc.Difficulty.ToString() == difficulty);
            if (selectedWizardConfiguration == null)
            {
                EditorMessageBox.Show(WindowManager, "Failure finding wizard conf", "Could not find an appropiate wizard configuration for difficulty " + difficulty, MessageBoxButtons.OK);
                return;
            }

            currentWizardConfiguration = selectedWizardConfiguration;

            EditTaskForce(currentWizardConfiguration.TaskForce, false);
        }

        private void TbGroup_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTaskForceExists())
                return;
            
            currentWizardConfiguration.TaskForce.Group = tbGroup.Value;
        }
        private void BtnCloneOtherDiffs_LeftClick(object sender, EventArgs e)
        {
            if (!IsCurrentTaskForceExists())
                return;

            if (WizardConfigurations.Count <= 1)
            {
                EditorMessageBox.Show(WindowManager, "No Difficulties", "There are no other difficulties to clone to. Aborting.", MessageBoxButtons.OK);
                return;
            }

            if (!currentWizardConfiguration.EditedTaskForce)
            {
                EditorMessageBox.Show(WindowManager, "No Configuration to Clone", "This TaskForce is not edited, and cannot be cloned to other difficulties.", MessageBoxButtons.OK);
                return;
            }                
            
            bool hasOtherEditedTaskForces = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration && 
                wizardConfiguration.EditedTaskForce == true);

            bool hasOtherUneditedTaskForces = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration &&
                wizardConfiguration.EditedTaskForce == false);

            if (hasOtherEditedTaskForces)
            {
                string description = hasOtherUneditedTaskForces ?
                    "There are other difficulties that have edited TaskForces. Should this clone operation skip those difficulties?" + Environment.NewLine +
                    "Press 'Yes' to only clone to difficulties with unedited TaskForces, or 'No' to clone to all other difficulties." :

                    "All other difficulties has edited TaskForces. Are you sure you want to continue?";

                var result = EditorMessageBox.Show(WindowManager, "Existing Configurations Found", description, MessageBoxButtons.YesNo);
                result.YesClickedAction = _ =>
                {
                    if (hasOtherUneditedTaskForces)
                    {
                        ApplyClone(true);
                    }
                    else
                    {
                        ApplyClone(false);
                    }
                };
                
                if (hasOtherUneditedTaskForces)
                {
                    result.NoClickedAction = _ => ApplyClone(false);
                }
            }
            else
            {
                ApplyClone(true);
            }
        }

        private void ApplyClone(bool skipEditedConfigurations)
        {
            var taskForce = currentWizardConfiguration.TaskForce;

            foreach (var wizardConfiguration in WizardConfigurations)
            {
                if (currentWizardConfiguration == wizardConfiguration)
                    continue;

                if (skipEditedConfigurations && wizardConfiguration.EditedTaskForce)
                    continue;

                wizardConfiguration.TaskForce = taskForce.Clone(wizardConfiguration.FullName);
                wizardConfiguration.TaskForce.Name = wizardConfiguration.FullName;
                wizardConfiguration.EditedTaskForce = true;
            }

            string relevantDifficultiesString = skipEditedConfigurations ?
                "to difficulties with unedited TaskForces" :
                "to all other difficulties";
            EditorMessageBox.Show(WindowManager, "Application successful", $"Applied the current TaskForce {relevantDifficultiesString} successfully.", MessageBoxButtons.OK);
        }

        private void BtnNext_LeftClick(object sender, EventArgs e)
        {            
            foreach (var wizardConfiguration in WizardConfigurations)
            {                
                var technoTypes = wizardConfiguration.TaskForce.TechnoTypes;
                int actualTechnos = 0;

                foreach (var technoType in technoTypes)
                {
                    if (technoType != null)
                        actualTechnos++;
                }

                if (actualTechnos == 0)
                {
                    EditorMessageBox.Show(WindowManager, "Task force with no entries", $"Task force for difficulty {wizardConfiguration.Difficulty.ToString()} has no units. Please edit the configuration to include at least one unit.", MessageBoxButtons.OK);
                    return;
                }
            }

            // Open Script step window and hide this one, passing the wizardConfigurations to it
            OpenScriptWizardStep();
        }

        private void OpenScriptWizardStep()
        {
            ScriptsWizardStepOpened?.Invoke(this, new ScriptWizardStepEventArgs(WizardConfigurations));
            Hide();
        }

        public void Open()
        {
            ResetForms();
            Show();
        }
    }
}
