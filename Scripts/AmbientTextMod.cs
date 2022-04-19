// Project:         AmbientText mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Regnier
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;

namespace AmbientText
{
    public class AmbientTextMod : MonoBehaviour
    {

        public bool AmbientTextEnabled { get; private set; }

        static Mod mod;
        float lastTickTime;
        float tickTimeInterval;
        int textChance = 95;
        float stdInterval = 2f;
        float postTextInterval = 4f;
        int textDisplayTime = 3;
        int lastIndex = -1;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<AmbientTextMod>();
        }

        void Awake()
        {
            Debug.Log("Begin mod init: AmbientText");

            /*
            ModSettings settings = mod.GetSettings();
            AmbientTextEnabled = settings.GetBool("Modules", "ambientText");
            if (AmbientTextEnabled && false)
            {
                textChance          = mod.GetSettings().GetInt("AmbientText", "textChance");
                stdInterval         = mod.GetSettings().GetInt("AmbientText", "interval");
                postTextInterval    = mod.GetSettings().GetInt("AmbientText", "postTextInterval");
                textDisplayTime     = mod.GetSettings().GetInt("AmbientText", "textDisplayTime");
            }
            */

            mod.IsReady = true;
            Debug.Log("Finished mod init: AmbientText");
        }

        void Start()
        {
            lastTickTime = Time.unscaledTime;
            tickTimeInterval = stdInterval;
        }

        void Update()
        {
            if (true) //TODO: AmbientTextEnabled)
            {
                PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
                if (!DaggerfallUnity.Instance.IsReady || !playerEnterExit || GameManager.IsGamePaused)
                    return;

                // Ambient text module.
                if (!playerEnterExit.IsPlayerInsideBuilding && Time.unscaledTime > lastTickTime + tickTimeInterval)
                {
                    lastTickTime = Time.unscaledTime;
                    tickTimeInterval = stdInterval;
                    //Debug.Log("tick");

                    if (Dice100.SuccessRoll(textChance))
                    {
                        string textMsg = SelectAmbientText();
                        if (!string.IsNullOrWhiteSpace(textMsg))
                        {
                            Debug.Log(textMsg);
                            DaggerfallUI.AddHUDText(textMsg, textDisplayTime);
                            tickTimeInterval = postTextInterval;
                        }
                    }
                }
            }
        }

        public string SelectAmbientText()
        {
            Random.InitState(Time.frameCount);
            string textKey;

            // Generate index (0-9)
            int index = lastIndex;
            while (index == lastIndex)
                index = Random.Range(0, 10);
            lastIndex = index;

            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            if (playerEnterExit.IsPlayerInsideDungeon)
            {
                // Handle dungeon interiors
                DFRegion.DungeonTypes dungeonType = playerEnterExit.Dungeon.Summary.DungeonType;

                textKey = string.Format("{0}{1}", dungeonType.ToString(), index);
            }
            else
            {
                // Handle exteriors - wilderness and locations based on climate, locationtype, weather, day/night.

                // LocationType
                PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
                DFRegion.LocationTypes locationType = playerGPS.IsPlayerInLocationRect ? playerGPS.CurrentLocationType : DFRegion.LocationTypes.None;

                // Day / Night
                string dayNight = DaggerfallUnity.Instance.WorldTime.Now.IsDay ? "Day" : "Night";

                int outsideVariant = Random.Range(0, 3);
                if (outsideVariant == 0)
                {
                    // LocationType & Climate: <locType><climate><0-9>
                    textKey = string.Format("{0}{1}{2}", locationType.ToString(), ClimateKey(), index);
                }
                else if (outsideVariant == 1)
                {
                    // LocationType & DayNight: <locType><dayNight><0-9>
                    textKey = string.Format("{0}{1}{2}", locationType.ToString(), dayNight, index);
                }
                else// if (outsideVariant == 2)
                {
                    // LocationType & DayNight & Weather: <locType><dayNight><weather><0-9>
                    textKey = string.Format("{0}{1}{2}{3}", locationType.ToString(), dayNight, WeatherKey(), index);
                }
            }

            if (AmbientText.AmbientTexts.Contains(textKey))
                return (string)AmbientText.AmbientTexts[textKey];
            else
                // TODO: return null;
                return textKey;
        }

        private static string WeatherKey()
        {
            // Weather
            string weather = "Clear";
            WeatherManager weatherManager = GameManager.Instance.WeatherManager;
            if (weatherManager.IsRaining || weatherManager.IsStorming)
                weather = "Rainy";
            else if (weatherManager.IsOvercast)
                weather = "Cloudy";
            else if (weatherManager.IsSnowing)
                weather = "Snowy";
            return weather;
        }

        static string ClimateKey()
        {
            switch (GameManager.Instance.PlayerGPS.CurrentClimateIndex)
            {
                case (int)MapsFile.Climates.Desert2:
                case (int)MapsFile.Climates.Desert:
                case (int)MapsFile.Climates.Subtropical:
                    return "Desert";
                case (int)MapsFile.Climates.Rainforest:
                case (int)MapsFile.Climates.Swamp:
                    return "Swamp";
                case (int)MapsFile.Climates.Woodlands:
                case (int)MapsFile.Climates.HauntedWoodlands:
                case (int)MapsFile.Climates.MountainWoods:
                    return "Woods";
                case (int)MapsFile.Climates.Mountain:
                    return "Mountains";
                default:
                    return "Ocean";
            }
        }

    }
}