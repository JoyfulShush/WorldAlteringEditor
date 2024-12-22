using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TeamCreationWizard
{
    public class TaskForcesWizardStepEventArgs : EventArgs
    {
        public TaskForcesWizardStepEventArgs(List<TeamCreationWizardConfiguration> wizardConfigurations)
        {
            WizardConfigurations = wizardConfigurations;
        }
        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; }
    }

    public class GeneralSettingsWizardStepWindow : INItializableWindow
    {
        public GeneralSettingsWizardStepWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TaskForcesWizardStepEventArgs> TaskForceWizardStepOpened;

        private EditorTextBox tbTeamName;
        private XNACheckBox chkTeamNameAsPrefix;
        private XNACheckBox chkTeamNameShorten;
        private XNACheckBox chkCreateEasy;
        private XNACheckBox chkCreateMedium;
        private XNACheckBox chkCreateHard;
        private XNADropDown ddHouse;
        private XNADropDown ddColor;
        private XNACheckBox chkAddAITriggers;
        private EditorButton btnNext;

        public List<TeamCreationWizardConfiguration> WizardConfigurations;

        public override void Initialize()
        {
            Name = nameof(GeneralSettingsWizardStepWindow);
            base.Initialize();

            tbTeamName = FindChild<EditorTextBox>(nameof(tbTeamName));
            chkTeamNameAsPrefix = FindChild<XNACheckBox>(nameof(chkTeamNameAsPrefix));
            chkTeamNameShorten = FindChild<XNACheckBox>(nameof(chkTeamNameShorten));
            chkCreateEasy = FindChild<XNACheckBox>(nameof(chkCreateEasy));
            chkCreateMedium = FindChild<XNACheckBox>(nameof(chkCreateMedium));
            chkCreateHard = FindChild<XNACheckBox>(nameof(chkCreateHard));
            ddHouse = FindChild<XNADropDown>(nameof(ddHouse));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            chkAddAITriggers = FindChild<XNACheckBox>(nameof(chkAddAITriggers));
            btnNext = FindChild<EditorButton>(nameof(btnNext));
            
            ListColors();
            ListHouses();
            ResetForm();

            btnNext.LeftClick += BtnNext_LeftClick;
        }

        private void ListHouses()
        {
            ddHouse.Items.Clear();
            map.GetHouseTypes().ForEach(ht => ddHouse.AddItem(ht.ININame, Helpers.GetHouseTypeUITextColor(ht)));
        }

        private void ListColors()
        {
            foreach (var supportedColor in TeamCreationWizardConfiguration.SupportedColors)
            {
                ddColor.AddItem(supportedColor.Name, supportedColor.Value);
            }
        }

        private void ResetForm()
        {
            tbTeamName.Text = string.Empty;
            chkTeamNameAsPrefix.Checked = true;
            chkTeamNameShorten.Checked = true;
            chkCreateEasy.Checked = true;
            chkCreateMedium.Checked = true;
            chkCreateHard.Checked = true;
            ddHouse.SelectedIndex = 0;
            ddColor.SelectedIndex = 0;
        }

        private void BtnNext_LeftClick(object sender, EventArgs e)
        {
            // Gather information
            string teamName = tbTeamName.Text;
            bool createEasyDiff = chkCreateEasy.Checked;
            bool createMediumDiff = chkCreateMedium.Checked;
            bool createHardDiff = chkCreateHard.Checked;
            bool prefix = chkTeamNameAsPrefix.Checked;
            bool shorten = chkTeamNameShorten.Checked;
            string houseName = ddHouse.SelectedItem.Text;
            string editorColor = ddColor.SelectedItem.Text;
            bool shouldIncludeAITriggers = chkAddAITriggers.Checked;

            if (string.IsNullOrEmpty(teamName))
            {                
                EditorMessageBox.Show(WindowManager, "Empty team name", "Please enter a name for your team.", MessageBoxButtons.OK);
                return;
            }

            List<Difficulty> difficlutiesList = [];

            if (createHardDiff)
            {
                difficlutiesList.Add(Difficulty.Hard);
            }            

            if (createMediumDiff)
            {
                difficlutiesList.Add(Difficulty.Medium);
            }

            if (createEasyDiff)
            {
                difficlutiesList.Add(Difficulty.Easy);
            }

            if (difficlutiesList.Count == 0)
            {
                EditorMessageBox.Show(WindowManager, "No difficulties selected", "Please select at least one difficulty.", MessageBoxButtons.OK);
                return;
            }

            HouseType houseType = map.GetHouseTypes().Find(ht => ht.ININame == houseName);
            if (houseType == null)
            {
                EditorMessageBox.Show(WindowManager, "House not found", $"house {houseName} was not found in the system. Please choose a valid house.", MessageBoxButtons.OK);
                return;
            }

            int index = Array.FindIndex(TeamCreationWizardConfiguration.SupportedColors, c => c.Name == editorColor);
            if (index <= -1) 
            {
                EditorMessageBox.Show(WindowManager, "Color not found", $"color {editorColor} was not found in the system. Please choose a valid color.", MessageBoxButtons.OK);
                return;
            }

            List <TeamCreationWizardConfiguration> wizardConfigurations = [];
            foreach (var difficulty in difficlutiesList)
            {
                string difficultyName = difficulty.ToString();

                if (shorten)
                {
                    difficultyName = difficultyName[0].ToString();
                }

                string finalTeamName;
                if (prefix)
                {
                    finalTeamName = difficultyName + " " + teamName;
                }
                else
                {
                    finalTeamName = teamName + " " + difficultyName;
                }

                var teamCreationWizardConfiguration = new TeamCreationWizardConfiguration(map, finalTeamName, difficulty, houseType, editorColor);
                teamCreationWizardConfiguration.ShouldIncludeAITriggers = shouldIncludeAITriggers;
                wizardConfigurations.Add(teamCreationWizardConfiguration);
            }

            WizardConfigurations = wizardConfigurations;

            OpenTaskForceWizardStep();
        }

        private void OpenTaskForceWizardStep()
        {
            if (WizardConfigurations == null)
            {
                return;
            }

            TaskForceWizardStepOpened?.Invoke(this, new TaskForcesWizardStepEventArgs(WizardConfigurations));
            Hide();
        }

        public void Open() {
            ResetForm();
            Show();
        }
    }
}
