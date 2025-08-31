// Script for assigning all infantry in the map to assume the Area Guard mission.

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
using Rampastring.XNAUI;

namespace WAEScript
{
    public class AssignAllGuardInfantryAreaGuard
    {
        /// <summary>
        /// Returns the description of this script.
        /// All scripts must contain this function.
        /// </summary>
        public string GetDescription() => "This script will assign all infantry units with mission Guard in the map to mission Area Guard. Continue?";

        /// <summary>
        /// Returns the message that is presented to the user if running this script succeeded.
        /// All scripts must contain this function.
        /// </summary>
        public string GetSuccessMessage()
        {
            if (error == null)
                return $"Successfully re-assigned {reassignedInfantryCount} infantry units from mission Guard to mission Area Guard.";

            return error;
        }

        private string error;
        private string guardMissionName = "Guard";
        private string areaGuardMissionName = "Area Guard";

        private int reassignedInfantryCount = 0;

        /// <summary>
        /// The function that actually does the magic.
        /// </summary>
        /// <param name="map">Map argument that allows us to access map data.</param>
        public void Perform(Map map)
        {
            map.DoForAllTechnos(techno =>
            {
                if (techno.WhatAmI() != RTTIType.Infantry)
                    return;

                Infantry infantry = (Infantry)techno;

                if (infantry.Mission != guardMissionName)
                    return;

                infantry.Mission = areaGuardMissionName;
                reassignedInfantryCount++;
            });
        }
    }
}