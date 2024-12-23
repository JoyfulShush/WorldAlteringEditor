using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TeamCreationWizard
{
    public class AITriggersWizardStepWindow : INItializableWindow
    {
        public AITriggersWizardStepWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorListBox lbDifficulties;

        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; set; }
        private TeamCreationWizardConfiguration currentWizardConfiguration;

        public override void Initialize()
        {
            Name = nameof(AITriggersWizardStepWindow);
            base.Initialize();

            lbDifficulties = FindChild<EditorListBox>(nameof(lbDifficulties));

            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
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

            //EditTeamType(currentWizardConfiguration.TeamType);
        }

        public void ResetForms()
        {
            currentWizardConfiguration = null;

            //ClearTeamTypeFields();
            LoadDifficulties();
        }

        public void Open()
        {
            ResetForms();
            Show();
        }
    }
}
