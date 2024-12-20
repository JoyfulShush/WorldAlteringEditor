﻿using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectStringWindow : SelectObjectWindow<CsfString>
    {
        public SelectStringWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorDescriptionPanel panelContent;

        public override void Initialize()
        {
            Name = nameof(SelectStringWindow);
            base.Initialize();

            lbObjectList.AllowMultiLineItems = false;
            panelContent = FindChild<EditorDescriptionPanel>(nameof(panelContent));
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                panelContent.Text = string.Empty;
                return;
            }

            SelectedObject = (CsfString)lbObjectList.SelectedItem.Tag;
            panelContent.Text = SelectedObject.Value;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();
            panelContent.Text = string.Empty;

            foreach (CsfString csf in map.StringTable.GetStringEnumerator())
            {
                string preview;

                // Add ellipsis ("...") to string content preview if it's too long.
                const int maxLength = 56;
                if (csf.ID.Length > maxLength)
                    preview = csf.ID[..maxLength];
                else
                    preview = (csf.ID.Length + csf.Value.Length) > maxLength ? csf.Value[..(maxLength - csf.ID.Length)] + "..." : csf.Value;

                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"({csf.ID}) {preview}", Tag = csf });
                if (csf == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
