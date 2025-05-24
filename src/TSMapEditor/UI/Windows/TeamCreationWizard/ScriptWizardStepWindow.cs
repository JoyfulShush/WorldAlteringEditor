using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TeamCreationWizard
{
    public class ScriptWindowEventArgs : EventArgs
    {
    }

    public class TeamTypeWizardStepEventArgs : EventArgs
    {
        public TeamTypeWizardStepEventArgs(List<TeamCreationWizardConfiguration> wizardConfigurations)
        {
            WizardConfigurations = wizardConfigurations;
        }

        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; }
    }

    public class ScriptWizardStepWindow : INItializableWindow
    {
        public ScriptWizardStepWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<ScriptWindowEventArgs> ScriptsWindowOpened;
        public event EventHandler<TeamTypeWizardStepEventArgs> TeamTypeWizardStepOpened;

        private EditorListBox lbDifficulties;
        private XNADropDown ddScripts;
        private EditorListBox lbScriptActions;
        private EditorButton btnOpenScripts;
        private EditorButton btnRefresh;
        private EditorButton btnNext;
        private EditorButton btnApplyScriptOtherDiffs;

        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; set; }
        private TeamCreationWizardConfiguration currentWizardConfiguration;        

        public override void Initialize()
        {
            Name = nameof(ScriptWizardStepWindow);
            base.Initialize();

            lbDifficulties = FindChild<EditorListBox>(nameof(lbDifficulties));
            ddScripts = FindChild<XNADropDown>(nameof(ddScripts));
            lbScriptActions = FindChild<EditorListBox>(nameof(lbScriptActions));
            btnOpenScripts = FindChild<EditorButton>(nameof(btnOpenScripts));
            btnRefresh = FindChild<EditorButton>(nameof(btnRefresh));
            btnNext = FindChild<EditorButton>(nameof(btnNext));
            btnApplyScriptOtherDiffs = FindChild<EditorButton>(nameof(btnApplyScriptOtherDiffs));

            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
            ddScripts.SelectedIndexChanged += DdScripts_SelectedIndexChanged;
            btnOpenScripts.LeftClick += BtnOpenScripts_LeftClick;
            btnRefresh.LeftClick += BtnRefresh_LeftClick;
            btnNext.LeftClick += BtnNext_LeftClick;
            btnApplyScriptOtherDiffs.LeftClick += BtnApplyScriptOtherDiffs_LeftClick;

            lbScriptActions.AllowRightClickUnselect = true;
            lbScriptActions.AllowKeyboardInput = false;
        }

        private void ResetForms()
        {
            currentWizardConfiguration = null;
            ClearScriptFields();
            LoadDifficulties();
            ListScripts();
            SelectScriptInDropdown();
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

        private void ClearScriptFields()
        {
            ddScripts.SelectedIndexChanged -= DdScripts_SelectedIndexChanged;

            ddScripts.Items.Clear();
            ddScripts.AddItem(Constants.NoneValue1);
            ddScripts.SelectedIndex = 0;

            lbScriptActions.Clear();
            lbScriptActions.ViewTop = 0;

            ddScripts.SelectedIndexChanged += DdScripts_SelectedIndexChanged;
        }

        private void ListScripts()
        {
            ddScripts.Items.Clear();

            ddScripts.AddItem(Constants.NoneValue1);
            foreach (var script in map.Scripts)
            {
                ddScripts.AddItem(new XNADropDownItem
                {
                    Text = script.Name,
                    Tag = script,
                    TextColor = script.EditorColor == null ? UISettings.ActiveSettings.AltColor : script.XNAColor
                });
            }
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

            UpdateScriptFields(currentWizardConfiguration.Script, false);
        }

        private void UpdateScriptFields(Script script, bool markScriptAsEdited)
        {
            ClearScriptFields();
            ListScripts();

            if (script == null)
            {                                
                SelectScriptInDropdown();
                return;
            }

            // Make sure script exists in the map - might have been deleted as we work
            if (!ValidateScriptInMapEntries(script))
            {
                currentWizardConfiguration.Script = null;                
                SelectScriptInDropdown();
                return;
            }

            SelectScriptInDropdown();

            for (int i = 0; i < script.Actions.Count; i++)
            {
                var actionEntry = script.Actions[i];
                lbScriptActions.AddItem(new XNAListBoxItem()
                {
                    Text = GetActionEntryText(i, actionEntry),
                    Tag = actionEntry
                });
            }

            if (markScriptAsEdited)
            {
                currentWizardConfiguration.EditedScript = true;
            }
        }

        private void SelectScriptInDropdown()
        {
            ddScripts.SelectedIndexChanged -= DdScripts_SelectedIndexChanged;

            if (!IsCurrentScriptExists())
            {
                ddScripts.SelectedIndex = 0;
            }
            else
            {                
                var scriptIndex = map.Scripts.FindIndex(script => script == currentWizardConfiguration.Script);
                if (scriptIndex < 0)
                {
                    ddScripts.SelectedIndex = 0;
                }
                else
                {
                    ddScripts.SelectedIndex = scriptIndex + 1;
                }
            }            
            
            ddScripts.SelectedIndexChanged += DdScripts_SelectedIndexChanged;
        }

        private string GetActionEntryText(int index, ScriptActionEntry entry)
        {
            ScriptAction action = GetScriptAction(entry.Action);
            if (action == null)
                return "#" + index + " - Unknown (" + entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";

            return "#" + index + " - " + action.Name + " (" + entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private ScriptAction GetScriptAction(int index)
        {
            return map.EditorConfig.ScriptActions.GetValueOrDefault(index);
        }

        private void DdScripts_SelectedIndexChanged(object sender , EventArgs e)
        {
            if (currentWizardConfiguration == null)
                return;

            // No script selected
            if (ddScripts.SelectedIndex <= 0)
            {
                currentWizardConfiguration.Script = null;
            }
            else
            {
                var script = map.Scripts.Find(script => script == ddScripts.SelectedItem.Tag);
                if (script != null) 
                {
                    currentWizardConfiguration.Script = script;
                }
                else
                {
                    currentWizardConfiguration.Script = null;
                }
            }

            UpdateScriptFields(currentWizardConfiguration.Script, true);
        }
        private bool IsCurrentScriptExists()
        {
            if (currentWizardConfiguration == null || currentWizardConfiguration.Script == null)
            {
                return false;
            }

            return true;
        }

        private void BtnOpenScripts_LeftClick(object sender, EventArgs e)
        {
            OpenScriptWindow();
        }

        private void BtnRefresh_LeftClick(object sender, EventArgs e)
        {            
            UpdateScriptFields(currentWizardConfiguration.Script, true);
        }

        private void BtnNext_LeftClick(object sender, EventArgs e)
        {            
            foreach (var wizardConfiguration in WizardConfigurations)
            {
                if (wizardConfiguration.Script == null)
                {
                    EditorMessageBox.Show(WindowManager, "Missing script", $"Difficulty {wizardConfiguration.Difficulty.ToString()} has no script attached. Please set a script to use.", MessageBoxButtons.OK);
                    return;
                }

                if (!ValidateScriptInMapEntries(wizardConfiguration.Script))
                {
                    EditorMessageBox.Show(WindowManager, "Invalid script", $"Difficulty {wizardConfiguration.Difficulty.ToString()} has a script '{wizardConfiguration.Script.Name}' that is not associated with the map. Was the script deleted? Please assign a valid script.", MessageBoxButtons.OK);
                    return;
                }
            }

            OpenTeamTypeWizardStepWindow();
        }

        private void BtnApplyScriptOtherDiffs_LeftClick(object sender, EventArgs e)
        {
            if (currentWizardConfiguration == null)
                return;

            if (WizardConfigurations.Count <= 1)
            {
                EditorMessageBox.Show(WindowManager, "No Difficulties", "There are no other difficulties to clone to. Aborting.", MessageBoxButtons.OK);
                return;
            }

            if (!currentWizardConfiguration.EditedScript)
            {
                EditorMessageBox.Show(WindowManager, "No Configuration to Clone", "This Script configuration is not edited, and cannot be cloned to other difficulties.", MessageBoxButtons.OK);
                return;
            }

            bool hasOtherEditedScriptConfigurations = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration &&
                wizardConfiguration.EditedScript == true);

            bool hasOtherUneditedScriptConfigurations = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration &&
                wizardConfiguration.EditedScript == false);

            if (hasOtherEditedScriptConfigurations)
            {
                string description = hasOtherUneditedScriptConfigurations ?
                    "There are other difficulties that have edited script configurations. Should this clone operation skip those difficulties?" + Environment.NewLine +
                    "Press 'Yes' to only clone to difficulties with unedited script configurations, or 'No' to clone to all other difficulties." :

                    "All other difficulties has edited script configurations. Are you sure you want to continue?";

                var result = EditorMessageBox.Show(WindowManager, "Existing Configurations Found", description, MessageBoxButtons.YesNo);
                result.YesClickedAction = _ =>
                {
                    if (hasOtherUneditedScriptConfigurations)
                    {
                        ApplyClone(true);
                    }
                    else
                    {
                        ApplyClone(false);
                    }
                };

                if (hasOtherUneditedScriptConfigurations)
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
            var script = currentWizardConfiguration.Script;
            foreach (var wizardConfiguration in WizardConfigurations)
            {
                if (wizardConfiguration == currentWizardConfiguration)
                    continue;

                if (skipEditedConfigurations && wizardConfiguration.EditedScript)
                    continue;

                wizardConfiguration.Script = script;
                wizardConfiguration.EditedScript = true;
            }            

            string relevantDifficultiesString = skipEditedConfigurations ?
                "to difficulties with unedited script configurations" :
                "to all other difficulties";
            EditorMessageBox.Show(WindowManager, "Script configuration applied successfully", $"Script '{script.Name}' was applied {relevantDifficultiesString} successfully.", MessageBoxButtons.OK);
        }

        private bool ValidateScriptInMapEntries(Script script)
        {
            var mapEntryScript = map.Scripts.Find(mapScript => mapScript == script);
            return mapEntryScript != null;
        }

        private void OpenScriptWindow()
        {
            ScriptsWindowOpened?.Invoke(this, new ScriptWindowEventArgs());
            PutOnBackground();
        }

        private void OpenTeamTypeWizardStepWindow()
        {
            TeamTypeWizardStepOpened?.Invoke(this, new TeamTypeWizardStepEventArgs(WizardConfigurations));
            Hide();
        }

        public void Open()
        {
            ResetForms();
            Show();
        }
    }
}

    
