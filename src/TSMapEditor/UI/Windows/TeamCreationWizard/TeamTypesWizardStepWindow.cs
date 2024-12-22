using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TeamCreationWizard
{
    public class TeamTypesWizardStepWindow : INItializableWindow
    {
        public TeamTypesWizardStepWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TagEventArgs> TagOpened;

        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; set; }
        private TeamCreationWizardConfiguration currentWizardConfiguration;

        private EditorListBox lbDifficulties;
        private XNADropDown ddVeteranLevel;
        private EditorNumberTextBox tbPriority;
        private EditorNumberTextBox tbMax;
        private EditorNumberTextBox tbTechLevel;
        private XNADropDown ddMindControlDecision;
        private EditorNumberTextBox tbGroup;
        private EditorNumberTextBox tbWaypoint;
        private EditorPopUpSelector selTag;
        private EditorButton btnOpenTag;
        private EditorNumberTextBox tbTransportWaypoint;
        private EditorButton btnFinish;
        private EditorButton btnApplyTeamTypesOtherDiffs;
        private EditorPanel panelBooleans;

        private List<XNACheckBox> checkBoxes = new List<XNACheckBox>();
        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(TeamTypesWizardStepWindow);
            base.Initialize();

            lbDifficulties = FindChild<EditorListBox>(nameof(lbDifficulties));
            ddVeteranLevel = FindChild<XNADropDown>(nameof(ddVeteranLevel));
            tbPriority = FindChild<EditorNumberTextBox>(nameof(tbPriority));
            tbMax = FindChild<EditorNumberTextBox>(nameof(tbMax));
            tbTechLevel = FindChild<EditorNumberTextBox>(nameof(tbTechLevel));
            ddMindControlDecision = FindChild<XNADropDown>(nameof(ddMindControlDecision));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            tbWaypoint = FindChild<EditorNumberTextBox>(nameof(tbWaypoint));
            selTag = FindChild<EditorPopUpSelector>(nameof(selTag));
            btnOpenTag = FindChild<EditorButton>(nameof(btnOpenTag));
            tbTransportWaypoint = FindChild<EditorNumberTextBox>(nameof(tbTransportWaypoint));
            btnFinish = FindChild<EditorButton>(nameof(btnFinish));
            btnApplyTeamTypesOtherDiffs = FindChild<EditorButton>(nameof(btnApplyTeamTypesOtherDiffs));
            panelBooleans = FindChild<EditorPanel>(nameof(panelBooleans));

            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
            ddVeteranLevel.SelectedIndexChanged += DdVeteranLevel_SelectedIndexChanged;
            tbPriority.TextChanged += TbPriority_TextChanged;
            tbMax.TextChanged += TbMax_TextChanged;
            tbTechLevel.TextChanged += TbTechLevel_TextChanged;
            ddMindControlDecision.SelectedIndexChanged += DdMindControlDecision_SelectedIndexChanged;
            tbGroup.TextChanged += TbGroup_TextChanged;
            tbWaypoint.TextChanged += TbWaypoint_TextChanged;
            tbTransportWaypoint.TextChanged += TbTransportWaypoint_TextChanged;

            AddBooleanProperties(panelBooleans);
            checkBoxes.ForEach(chk => chk.CheckedChanged += FlagCheckBox_CheckedChanged);

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => currentWizardConfiguration.TeamType.Tag = w.SelectedObject, selectTagWindow);

            btnOpenTag.LeftClick += (s, e) => OpenTag();
            selTag.LeftClick += (s, e) => { if (IsCurrentTeamTypeExists()) selectTagWindow.Open(currentWizardConfiguration.TeamType.Tag); };

            btnFinish.LeftClick += BtnFinish_LeftClick;
            btnApplyTeamTypesOtherDiffs.LeftClick += BtnApplyTeamTypesOtherDiffs_LeftClick;

        }
        private void ClearTeamTypeFields()
        {
            ddVeteranLevel.SelectedIndex = 0;
            tbPriority.Text = string.Empty;
            tbMax.Text = string.Empty;
            tbTechLevel.Text = string.Empty;
            ddMindControlDecision.SelectedIndex = 0;
            tbGroup.Text = string.Empty;
            tbWaypoint.Text = string.Empty;
            selTag.Text = string.Empty;
            selTag.Tag = null;
            tbTransportWaypoint.Text = string.Empty;

            checkBoxes.ForEach(checkBoxes => checkBoxes.Checked = false);
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

            EditTeamType(currentWizardConfiguration.TeamType);
        }
        private void SelectionWindow_ApplyEffect<T>(Action<T> action, T window)
        {
            if (!IsCurrentTeamTypeExists())            
                return;
            

            action(window);
            EditTeamType(currentWizardConfiguration.TeamType);
        }

        private void EditTeamType(TeamType teamType)
        {
            if (teamType == null)
            {
                ClearTeamTypeFields();
                return;
            }
            
            ddVeteranLevel.SelectedIndex = teamType.VeteranLevel - 1;
            tbPriority.Value = teamType.Priority;
            tbMax.Value = teamType.Max;
            tbTechLevel.Value = teamType.TechLevel;
            tbGroup.Value = teamType.Group;
            tbWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(teamType.Waypoint);

            if (Constants.IsRA2YR)
            {
                ddMindControlDecision.SelectedIndex = teamType.MindControlDecision ?? -1;
                tbTransportWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(teamType.TransportWaypoint);
            }            

            if (teamType.Tag != null)
                selTag.Text = teamType.Tag.Name + " (" + teamType.Tag.ID + ")";
            else
                selTag.Text = string.Empty;

            checkBoxes.ForEach(chk => chk.Checked = teamType.IsFlagEnabled((string)chk.Tag));
        }

        private void DdVeteranLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.VeteranLevel = ddVeteranLevel.SelectedIndex + 1;
        }

        private void TbPriority_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.Priority = tbPriority.Value;
        }

        private void TbMax_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.Max = tbMax.Value;
        }

        private void TbTechLevel_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.TechLevel = tbTechLevel.Value;
        }

        private void DdMindControlDecision_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Constants.IsRA2YR && IsCurrentTeamTypeExists())
            {
                currentWizardConfiguration.TeamType.MindControlDecision = ddMindControlDecision.SelectedIndex;
            }
        }

        private void TbGroup_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.Group = tbGroup.Value;
        }

        private void TbWaypoint_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            currentWizardConfiguration.TeamType.Waypoint = Helpers.WaypointNumberToAlphabeticalString(tbWaypoint.Value);
        }

        private void TbTransportWaypoint_TextChanged(object sender, EventArgs e)
        {
            if (Constants.IsRA2YR && IsCurrentTeamTypeExists())
            {
               currentWizardConfiguration.TeamType.TransportWaypoint = Helpers.WaypointNumberToAlphabeticalString(tbTransportWaypoint.Value);
            }
        }        

        private void FlagCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            var checkBox = (XNACheckBox)sender;
            if (checkBox.Checked)
                currentWizardConfiguration.TeamType.EnableFlag((string)checkBox.Tag);
            else
                currentWizardConfiguration.TeamType.DisableFlag((string)checkBox.Tag);
        }

        private void BtnFinish_LeftClick(object sender, EventArgs e)
        {            
            bool ShouldIncludeAITriggers = WizardConfigurations[0].ShouldIncludeAITriggers;

            // check if AI Triggers should be created, if so move to next page
            // otherwise finish and trigger the logic
            if (ShouldIncludeAITriggers)
            {
                // TODO: create AI trigger window and trigger event to open it
            }
            else
            {
                foreach (var wizardConfiguration in WizardConfigurations)
                {
                    wizardConfiguration.ProcessConfiguration();
                }
            }

            Hide();
        }

        private void BtnApplyTeamTypesOtherDiffs_LeftClick(object sender, EventArgs e)
        {
            if (!IsCurrentTeamTypeExists())
                return;

            var teamType = currentWizardConfiguration.TeamType;

            foreach (var wizardConfiguration in WizardConfigurations)
            {
                if (currentWizardConfiguration == wizardConfiguration)
                    continue;

                wizardConfiguration.TeamType = teamType.Clone(wizardConfiguration.Name);
                wizardConfiguration.TeamType.Name = wizardConfiguration.Name;
            }

            EditorMessageBox.Show(WindowManager, "Clone successful", "Applied the current TeamType to the other difficulties successfully.", MessageBoxButtons.OK);
        }

        private void AddBooleanProperties(EditorPanel panelBooleans)
        {
            int currentColumnRight = 0;
            int currentColumnX = Constants.UIEmptySideSpace;
            XNACheckBox previousCheckBoxOnColumn = null;

            foreach (var teamTypeFlag in map.EditorConfig.TeamTypeFlags)
            {
                var checkBox = new XNACheckBox(WindowManager);
                checkBox.Tag = teamTypeFlag.Name;
                checkBox.Text = teamTypeFlag.Name;
                panelBooleans.AddChild(checkBox);
                checkBoxes.Add(checkBox);

                if (previousCheckBoxOnColumn == null)
                {
                    checkBox.Y = Constants.UIEmptyTopSpace;
                    checkBox.X = currentColumnX;
                }
                else
                {
                    checkBox.Y = previousCheckBoxOnColumn.Bottom + Constants.UIVerticalSpacing;
                    checkBox.X = currentColumnX;

                    // Start new column
                    if (checkBox.Bottom > panelBooleans.Height - Constants.UIEmptyBottomSpace)
                    {
                        currentColumnX = currentColumnRight + Constants.UIHorizontalSpacing * 2;
                        checkBox.Y = Constants.UIEmptyTopSpace;
                        checkBox.X = currentColumnX;
                        currentColumnRight = 0;
                    }
                }

                previousCheckBoxOnColumn = checkBox;
                currentColumnRight = Math.Max(currentColumnRight, checkBox.Right);
            }
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
        private bool IsCurrentTeamTypeExists()
        {
            if (currentWizardConfiguration == null || currentWizardConfiguration.TeamType == null)
            {
                return false;
            }

            return true;
        }

        private void OpenTag()
        {
            if (!IsCurrentTeamTypeExists())
                return;

            TagOpened?.Invoke(this, new TagEventArgs(currentWizardConfiguration.TeamType.Tag));
            PutOnBackground();
        }

        private void AdjustFinishButtonText()
        {
            bool shouldIncludeAITriggers = WizardConfigurations[0].ShouldIncludeAITriggers;
            btnFinish.Text = shouldIncludeAITriggers ? "Next" : "Finish";
        }

        public void ResetForms()
        {
            currentWizardConfiguration = null;

            ClearTeamTypeFields();
            LoadDifficulties();
            AdjustFinishButtonText();
        }

        public void Open()
        {
            ResetForms();
            Show();
        }
    }
}
