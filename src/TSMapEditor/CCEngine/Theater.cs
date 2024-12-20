using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Extensions;
using System.Linq;

namespace TSMapEditor.CCEngine
{
    public class LATGround
    {
        public LATGround(string displayName, TileSet groundTileSet, TileSet transitionTileSet, TileSet baseTileSet, IEnumerable<int> connectToTileSetIndices)
        {
            DisplayName = displayName;
            GroundTileSet = groundTileSet;
            TransitionTileSet = transitionTileSet;
            BaseTileSet = baseTileSet;

            if (connectToTileSetIndices != null)
                ConnectToTileSetIndices.AddRange(connectToTileSetIndices);
        }

        public string DisplayName { get; }
        public TileSet GroundTileSet { get; }
        public TileSet TransitionTileSet { get; }
        public TileSet BaseTileSet { get; }
        public List<int> ConnectToTileSetIndices = new List<int>();
    }

    public class TheaterIceTileSets
    {
        public TheaterIceTileSets(Theater theater, IniFile theaterIni)
        {
            int ice1SetId = theaterIni.GetIntValue("General", "Ice1Set", -1);
            int ice2SetId = theaterIni.GetIntValue("General", "Ice2Set", -1);
            int ice3SetId = theaterIni.GetIntValue("General", "Ice3Set", -1);
            int iceShoreSetId = theaterIni.GetIntValue("General", "IceShoreSet", -1);

            Ice1Set = theater.TryGetTileSetById(ice1SetId);
            Ice2Set = theater.TryGetTileSetById(ice2SetId);
            Ice3Set = theater.TryGetTileSetById(ice3SetId);
            IceShoreSet = theater.TryGetTileSetById(iceShoreSetId);
        }

        public TileSet Ice1Set { get; private set; }
        public TileSet Ice2Set { get; private set; }
        public TileSet Ice3Set { get; private set; }
        public TileSet IceShoreSet { get; private set; }
    }

    public class Theater : INIDefineable
    {
        public Theater(string name)
        {
            UIName = name;
        }

        public string UIName { get; }
        public string ConfigINIPath { get; set; }
        public List<string> ContentMIXName { get; set; }
        public List<string> OptionalContentMIXName { get; set; }
        public string TerrainPaletteName { get; set; }
        public string UnitPaletteName { get; set; }
        public string TiberiumPaletteName { get; set; }
        public string FileExtension { get; set; }
        public string FallbackTileFileExtension { get; set; }
        public char NewTheaterBuildingLetter { get; set; }

        public List<string> RoughConnectToTileSets { get; set; }
        public List<string> SandConnectToTileSets { get; set; }
        public List<string> PaveConnectToTileSets { get; set; }
        public List<string> GreenConnectToTileSets { get; set; }

        public TheaterIceTileSets IceTileSetInfo { get; private set; }

        public List<TileSet> TileSets = new List<TileSet>();
        public List<LATGround> LATGrounds = new List<LATGround>();
        public TileSet RampTileSet { get; set; }

        private const string REQUIRED_SECTION = "General";

        public TileSet FindTileSet(string tileSetName) => TileSets.Find(ts => ts.SetName == tileSetName);

        public void ReadConfigINI(string baseDirectoryPath, CCFileManager ccFileManager)
        {
            TileSets.Clear();

            IniFileEx theaterIni = IniFileEx.FromPathOrMix(ConfigINIPath, baseDirectoryPath, ccFileManager);

            if (!theaterIni.SectionExists(REQUIRED_SECTION))
            {
                throw new FileNotFoundException("Theater config INI not found or invalid: " + ConfigINIPath);
            }

            int i;

            for (i = 0; i < 10000; i++)
            {
                IniSection tileSetSection = theaterIni.GetSection($"TileSet{i:D4}");

                if (tileSetSection == null)
                    break;

                TileSet tileSet = new TileSet(i);
                tileSet.Read(tileSetSection);
                TileSets.Add(tileSet);
            }

            IceTileSetInfo = new TheaterIceTileSets(this, theaterIni);

            i = 1;
            while (true)
            {
                if (!InitLATGround(theaterIni, $"Ground{i}Tile", $"Ground{i}Lat", $"Ground{i}Base", $"Ground{i}Name", $"Ground{i}ConnectTo", null))
                    break;

                i++;
            }

            // DTA
            InitLATGround(theaterIni, "PvmntTile", "ClearToPvmntLat", null, null, null, "Pavement");

            // TS terrain
            InitLATGround(theaterIni, "RoughTile", "ClearToRoughLat", null, null, "RoughConnectTo", "Rough", RoughConnectToTileSets);
            InitLATGround(theaterIni, "SandTile", "ClearToSandLat", null, null, "SandConnectTo", "Sand", SandConnectToTileSets);
            InitLATGround(theaterIni, "PaveTile", "ClearToPaveLat", null, null, "PaveConnectTo", "Pavement", PaveConnectToTileSets);
            InitLATGround(theaterIni, "GreenTile", "ClearToGreenLat", null, null, "GreenConnectTo", "Green", GreenConnectToTileSets);

            int rampTileSetIndex = theaterIni.GetIntValue("General", "RampBase", -1);
            if (rampTileSetIndex < 0 || rampTileSetIndex >= TileSets.Count)
            {
                throw new INIConfigException("Invalid value specified for RampBase= in the theater configuration file!");
            }

            RampTileSet = TileSets[rampTileSetIndex];
        }

        public TileSet TryGetTileSetById(int id)
        {
            if (id < 0 || id >= TileSets.Count)
                return null;

            return TileSets[id];
        }

        private bool InitLATGround(IniFile theaterIni, string tileSetKey, string transitionTileSetKey, string baseTileSetKey, string nameKey, string connectToKey, string defaultName, IEnumerable<string> connectedTileSetIndices = null)
        {
            int groundTileSetIndex = theaterIni.GetIntValue("General", tileSetKey, -1);
            int transitionTileSetIndex = theaterIni.GetIntValue("General", transitionTileSetKey, -1);

            int baseTileSetIndex = -1;
            if (!string.IsNullOrEmpty(baseTileSetKey))
                baseTileSetIndex = theaterIni.GetIntValue("General", baseTileSetKey, -1);

            if (groundTileSetIndex < 0 || transitionTileSetIndex < 0)
                return false;

            if (groundTileSetIndex >= TileSets.Count || transitionTileSetIndex >= TileSets.Count)
                return false;

            string displayName = defaultName;
            if (!string.IsNullOrEmpty(nameKey))
                displayName = theaterIni.GetStringValue("General", nameKey, displayName);

            if (displayName == null)
            {
                string groundTileSetName = TileSets[groundTileSetIndex].SetName;
                displayName = groundTileSetName.Substring(0, Math.Min(groundTileSetName.Length, 4));
            }

            List<int> indices = new List<int>();

            if ((connectedTileSetIndices == null || !connectedTileSetIndices.Any()) && !string.IsNullOrEmpty(connectToKey))
                connectedTileSetIndices = theaterIni.GetStringValue("General", connectToKey, string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (connectedTileSetIndices != null)
            {
                foreach (string indexStr in  connectedTileSetIndices)
                {
                    int index = Conversions.IntFromString(indexStr, -1);

                    if (index != -1)
                        indices.Add(index);
                }
            }

            LATGrounds.Add(new LATGround(
                displayName,
                TileSets[groundTileSetIndex],
                TileSets[transitionTileSetIndex],
                baseTileSetIndex > -1 ? TileSets[baseTileSetIndex] : TileSets[0], indices));

            return true;
        }
    }
}
