using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
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
        private XNADropDown ddConditionType;
        private XNADropDown ddComparator;
        private EditorNumberTextBox tbQuantity;
        private EditorPopUpSelector selComparisonObjectType;
        private EditorNumberTextBox tbInitial;
        private EditorNumberTextBox tbMinimum;
        private EditorNumberTextBox tbMaximum;
        private EditorButton btnFinish;
        private EditorButton btnApplyAITriggersOtherDiffs;

        private SelectTechnoTypeWindow selectTechnoTypeWindow;
        public List<TeamCreationWizardConfiguration> WizardConfigurations { get; set; }
        private TeamCreationWizardConfiguration currentWizardConfiguration;

        public override void Initialize()
        {
            Name = nameof(AITriggersWizardStepWindow);
            base.Initialize();

            lbDifficulties = FindChild<EditorListBox>(nameof(lbDifficulties));
            ddConditionType = FindChild<XNADropDown>(nameof(ddConditionType));
            ddComparator = FindChild<XNADropDown>(nameof(ddComparator));
            tbQuantity = FindChild<EditorNumberTextBox>(nameof(tbQuantity));
            selComparisonObjectType = FindChild<EditorPopUpSelector>(nameof(selComparisonObjectType));
            tbInitial = FindChild<EditorNumberTextBox>(nameof(tbInitial));
            tbMinimum = FindChild<EditorNumberTextBox>(nameof(tbMinimum));
            tbMaximum = FindChild<EditorNumberTextBox>(nameof(tbMaximum));
            btnFinish = FindChild<EditorButton>(nameof(btnFinish));
            btnApplyAITriggersOtherDiffs = FindChild<EditorButton>(nameof(btnApplyAITriggersOtherDiffs));            

            lbDifficulties.SelectedIndexChanged += LbDifficulties_SelectedIndexChanged;
            ddConditionType.SelectedIndexChanged += DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged += DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged += TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick += SelComparisonObjectType_LeftClick;
            tbInitial.TextChanged += TbInitial_TextChanged;
            tbMinimum.TextChanged += TbMinimum_TextChanged;
            tbMaximum.TextChanged += TbMaximum_TextChanged;

            selectTechnoTypeWindow = new SelectTechnoTypeWindow(WindowManager, map);
            selectTechnoTypeWindow.IncludeNone = true;
            var technoTypeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTechnoTypeWindow);
            technoTypeDarkeningPanel.Hidden += TechnoTypeDarkeningPanel_Hidden;

            btnFinish.LeftClick += BtnFinish_LeftClick;
            btnApplyAITriggersOtherDiffs.LeftClick += BtnApplyAITriggersOtherDiffs_LeftClick;
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
            
            EditAITrigger(currentWizardConfiguration.AITriggerType, false);
        }

        private void EditAITrigger(AITriggerType aiTriggerType, bool markAITriggerAsEdited)
        {                        
            if (aiTriggerType== null)
            {
                ClearAITriggerFields();
                return;
            }

            ddConditionType.SelectedIndexChanged -= DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged -= DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged -= TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick -= SelComparisonObjectType_LeftClick;
            tbInitial.TextChanged -= TbInitial_TextChanged;
            tbMinimum.TextChanged -= TbMinimum_TextChanged;
            tbMaximum.TextChanged -= TbMaximum_TextChanged;

            ddConditionType.SelectedIndex = ((int)aiTriggerType.ConditionType + 1);
            ddComparator.SelectedIndex = (int)aiTriggerType.Comparator.ComparatorOperator;
            tbQuantity.Value = aiTriggerType.Comparator.Quantity;
            selComparisonObjectType.Text = aiTriggerType.ConditionObject != null ? $"{aiTriggerType.ConditionObject.GetEditorDisplayName()} ({aiTriggerType.ConditionObject.ININame})" : string.Empty;
            selComparisonObjectType.Tag = aiTriggerType.ConditionObject;
            tbInitial.DoubleValue = aiTriggerType.InitialWeight;
            tbMinimum.DoubleValue = aiTriggerType.MinimumWeight;
            tbMaximum.DoubleValue = aiTriggerType.MaximumWeight;

            ddConditionType.SelectedIndexChanged += DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged += DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged += TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick += SelComparisonObjectType_LeftClick;
            tbInitial.TextChanged += TbInitial_TextChanged;
            tbMinimum.TextChanged += TbMinimum_TextChanged;
            tbMaximum.TextChanged += TbMaximum_TextChanged;

            if (markAITriggerAsEdited)
            {
                currentWizardConfiguration.EditedAITriggers = true;
            }
        }

        private void ClearAITriggerFields()
        {
            ddConditionType.SelectedIndexChanged -= DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged -= DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged -= TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick -= SelComparisonObjectType_LeftClick;
            tbInitial.TextChanged -= TbInitial_TextChanged;
            tbMinimum.TextChanged -= TbMinimum_TextChanged;
            tbMaximum.TextChanged -= TbMaximum_TextChanged;

            ddConditionType.SelectedIndex = -1;
            ddComparator.SelectedIndex = -1;
            tbQuantity.Text = string.Empty;
            selComparisonObjectType.Text = string.Empty;
            selComparisonObjectType.Tag = null;
            tbInitial.Text = string.Empty;
            tbMinimum.Text = string.Empty;
            tbMaximum.Text = string.Empty;

            ddConditionType.SelectedIndexChanged += DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged += DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged += TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick += SelComparisonObjectType_LeftClick;
            tbInitial.TextChanged += TbInitial_TextChanged;
            tbMinimum.TextChanged += TbMinimum_TextChanged;
            tbMaximum.TextChanged += TbMaximum_TextChanged;
        }

        private void DdConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.ConditionType = (AITriggerConditionType)(ddConditionType.SelectedIndex - 1);
            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void DdComparator_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.Comparator = new AITriggerComparator(
                (AITriggerComparatorOperator)ddComparator.SelectedIndex, 
                currentWizardConfiguration.AITriggerType.Comparator.Quantity
            );

            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void TbQuantity_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.Comparator = new AITriggerComparator(
                currentWizardConfiguration.AITriggerType.Comparator.ComparatorOperator, 
                tbQuantity.Value
            );

            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void SelComparisonObjectType_LeftClick(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            selectTechnoTypeWindow.Open(currentWizardConfiguration.AITriggerType.ConditionObject);            
        }

        private void TechnoTypeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.ConditionObject = selectTechnoTypeWindow.SelectedObject;

            EditAITrigger(currentWizardConfiguration.AITriggerType, true);
        }

        private void TbInitial_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.InitialWeight = tbInitial.DoubleValue;
            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void TbMinimum_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.MinimumWeight = tbMinimum.DoubleValue;
            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void TbMaximum_TextChanged(object sender, EventArgs e)
        {
            if (!IsCurrentAITriggerExists())
                return;

            currentWizardConfiguration.AITriggerType.MaximumWeight = tbMaximum.DoubleValue;
            currentWizardConfiguration.EditedAITriggers = true;
        }

        private void BtnFinish_LeftClick(object sender, EventArgs e)
        {            
            foreach (var wizardConfiguration in WizardConfigurations)
            {
                wizardConfiguration.ProcessConfiguration();
            }

            EditorMessageBox.Show(WindowManager, "Wizard Completed!", WizardConfigurations[0].GetFinishMessageText(), MessageBoxButtons.OK);
            Hide();
        }

        private void BtnApplyAITriggersOtherDiffs_LeftClick(object sender, EventArgs e)
        {
            if (currentWizardConfiguration == null)
                return;

            if (WizardConfigurations.Count <= 1)
            {
                EditorMessageBox.Show(WindowManager, "No Difficulties", "There are no other difficulties to clone to. Aborting.", MessageBoxButtons.OK);
                return;
            }

            if (!currentWizardConfiguration.EditedAITriggers)
            {
                EditorMessageBox.Show(WindowManager, "No Configuration to Clone", "This AI Trigger is not edited, and cannot be cloned to other difficulties.", MessageBoxButtons.OK);
                return;
            }

            bool hasOtherEditedAITriggers = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration &&
                wizardConfiguration.EditedAITriggers == true);

            bool hasOtherUneditedAITriggers = WizardConfigurations.Exists(wizardConfiguration =>
                wizardConfiguration != currentWizardConfiguration &&
                wizardConfiguration.EditedAITriggers == false);

            if (hasOtherEditedAITriggers)
            {
                string description = hasOtherUneditedAITriggers ?
                    "There are other difficulties that have edited AITriggers. Should this clone operation skip those difficulties?" + Environment.NewLine +
                    "Press 'Yes' to only clone to difficulties with unedited AITriggers, or 'No' to clone to all other difficulties." :

                    "All other difficulties has edited AITriggers. Are you sure you want to continue?";

                var result = EditorMessageBox.Show(WindowManager, "Existing Configurations Found", description, MessageBoxButtons.YesNo);
                result.YesClickedAction = _ =>
                {
                    if (hasOtherUneditedAITriggers)
                    {
                        ApplyClone(true);
                    }
                    else
                    {
                        ApplyClone(false);
                    }
                };

                if (hasOtherUneditedAITriggers)
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
            var aiTriggerType = currentWizardConfiguration.AITriggerType;

            foreach (var wizardConfiguration in WizardConfigurations)
            {
                if (currentWizardConfiguration == wizardConfiguration)
                    continue;


                if (skipEditedConfigurations && wizardConfiguration.EditedAITriggers)
                    continue;

                wizardConfiguration.AITriggerType = aiTriggerType.Clone(wizardConfiguration.FullName);
                wizardConfiguration.AITriggerType.Name = wizardConfiguration.FullName;
                wizardConfiguration.EditedAITriggers = true;
            }

            string relevantDifficultiesString = skipEditedConfigurations ?
                "to difficulties with unedited AITriggers" :
                "to all other difficulties";
            EditorMessageBox.Show(WindowManager, "AITrigger applied successfully", $"AITrigger was applied {relevantDifficultiesString} successfully.", MessageBoxButtons.OK);
        }

        private bool IsCurrentAITriggerExists()
        {
            if (currentWizardConfiguration == null || currentWizardConfiguration.AITriggerType == null)
            {
                return false;
            }

            return true;
        }

        public void ResetForms()
        {
            currentWizardConfiguration = null;

            ClearAITriggerFields();
            LoadDifficulties();
        }

        public void Open()
        {
            ResetForms();
            Show();
        }
    }
}
