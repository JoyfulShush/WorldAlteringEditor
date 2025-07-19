﻿// Script for adding a map reveal trigger

// Using clauses.
// Unless you know what's in the WAE code-base, you want to always include
// these "standard usings".
using System;
using TSMapEditor;
using TSMapEditor.Models;
using TSMapEditor.CCEngine;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;
using TSMapEditor.UI.Windows;

namespace WAEScript
{
    public class AddMapRevealTrigger
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => "This script will create a new map reveal trigger. Continue?";

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            if (error == null)
                return $"Successfully created a map reveal trigger with name \"{mapRevealTriggerName}\". You can locate it in the Triggers window.";

            return error;
        }

        private string error;

        private const string mapRevealTriggerName = "Map Reveal Trigger";

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            var trigger = new Trigger(map.GetNewUniqueInternalId());
            trigger.Name = mapRevealTriggerName;
            trigger.HouseType = "Neutral";

            var timeElapsedCondition = new TriggerCondition();
            timeElapsedCondition.ConditionIndex = 13;
            timeElapsedCondition.Parameters[0] = "0";
            trigger.Conditions.Add(timeElapsedCondition);

            var mapRevealAction = new TriggerAction();
            mapRevealAction.ActionIndex = 16;
            mapRevealAction.Parameters[0] = "0";
            trigger.Actions.Add(mapRevealAction);

            map.AddTrigger(trigger);

            map.AddTag(new Tag()
            {
                ID = map.GetNewUniqueInternalId(),
                Name = trigger.Name + " (tag)",
                Trigger = trigger,
                Repeating = 0
            });
        }
    }
}