using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;






//Fish 
namespace Fishing
{
    internal class FishingHoles
    { //implements serialize

        //private static readonly FishGUISettings guiSettings = new FishGUISettings();

        private const string SAVE_FILE_NAME = "fish-settings";

        private const string configFileName = "FishConfig.json";

        private static List<FishingHole> myholes = new List<FishingHole>();

        private static int[] mylookups = { 0, 1, -1, 2, 3, -1, -1, -1, -1, -1, -1 };

        private static GameRegion catchregion = GameRegion.CoastalRegion;

        private static string catchhole = "none";

        public static float glBaseCatchChanceMod = 1; //future mods may adjust this

        private static float nextShuffle = -1;

        private static FishSettings settings;

        private class FishSettings
        {
            public float[] baseCatchesPerHour = { 1.0f, 1.0f, 1.0f, 1.0f };
            public float[] maxPops = { 50, 0.2f, 10, 20 };
            public float[] repopRates = { 5, 0.2f, 5, 5 };
            public float daysBetweenLakeHoleQualityReshuffle = 20f;
            public float[] lakeHoleQuals = { 0, 0.2f, 0.4f, 0.6f };
        }



        internal static void LoadData(string name)
        {
            string data = SaveGameSlots.LoadDataFromSlot(name, SAVE_FILE_NAME);
            if (data != null)
            {
                FishingHoles.deserialize(data);
            }
        }

        internal static void SaveData(SaveSlotType gameMode, string name)
        {
            SaveGameSlots.SaveDataToSlot(gameMode, SaveGameSystem.m_CurrentEpisode, SaveGameSystem.m_CurrentGameId, name, SAVE_FILE_NAME, FishingHoles.serialize());
        }

        internal static void ApplySettings(string json)
        {
            settings = new FishSettings();
            JsonSerializerSettings jsettings = new JsonSerializerSettings();
            jsettings.Error += (_, args) =>
            {
                string errorMessagge = args.ErrorContext.Error.Message;
                Debug.LogError("[Fish] Couldn't parse the configuration JSON string. Error:\n" + errorMessagge);
            };
            settings = JsonConvert.DeserializeObject<FishSettings>(json, jsettings);
        }


        public static void resetfromSettingsFile()
        {
            myholes = new List<FishingHole>();

            string modsDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configPath = Path.Combine(modsDir, configFileName);
            if (!File.Exists(configPath))
                Debug.LogError("[Fish] Could not find config file); ");

            string configJson = File.ReadAllText(configPath, Encoding.UTF8);

            ApplySettings(configJson);

            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Debug.Log("[Fish] Version " + version + " REloaded!");
        }

        public static void OnLoad()
        {
            string modsDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configPath = Path.Combine(modsDir, configFileName);
            if (!File.Exists(configPath))
                Debug.LogError("[Fish] Could not find config file); ");

            string configJson = File.ReadAllText(configPath, Encoding.UTF8);

            ApplySettings(configJson);

            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            uConsole.RegisterCommand("FISH-reset-from-config-file", resetfromSettingsFile);

            Debug.Log("[Fish] Version " + version + " loaded!");
        }



        public static float verhulstpopmodel(float r, float maxpop, float curpop, float time)
        {
            return maxpop / (1 + ((maxpop - curpop) / curpop) * Mathf.Exp(-1 * r * time));
        }


        public static void deserialize(string serialString)
        {
            StringArray stringArray = Utils.DeserializeObject<StringArray>(serialString);
            nextShuffle = Utils.DeserializeObject<float>(stringArray.strings[0]);
            Debug.Log("Loaded last shuffle:" + nextShuffle);
            myholes = Utils.DeserializeObject<List<FishingHole>>(stringArray.strings[1]);
            Debug.Log("Loaded " + myholes.Count + " holes");
            settings = Utils.DeserializeObject<FishSettings>(stringArray.strings[2]);
            //myholes = Utils.DeserializeObject<List<FishingHole>>(serialString);
        }

        public static string serialize()
        {
            StringArray stringArray = new StringArray();
            stringArray.strings = new string[3];
            stringArray.strings[0] = Utils.SerializeObject(nextShuffle);
            stringArray.strings[1] = Utils.SerializeObject(myholes);
            stringArray.strings[2] = Utils.SerializeObject(settings);
            return (Utils.SerializeObject(stringArray));
        }

        private static void ShuffleLakeHoles()
        {
            List<int> tempDeck = new List<int> { 0, 1, 2, 3 };

            for (int i = 0; i < myholes.Count; i++)
            {
                if (myholes[i].reg == GameRegion.LakeRegion)
                {
                    int lookup = Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 0.99999999f) * ((float)tempDeck.Count));
                    myholes[i].holeQual = tempDeck[lookup];
                    tempDeck.RemoveAt(lookup);
                    Debug.Log("shuffling holes, tempDeck:" + Utils.SerializeObject(tempDeck));

                }
            }
        }

        private static void SetupNewLakeHole(FishingHole myhole, float curTime)
        {
            //here we set up population and quality
            bool noOtherHoles = true;
            List<int> remainingholeQuals = new List<int> { 0, 1, 2, 3 };
            List<int> foundholeQuals = new List<int>();

            for (int i = 0; i < myholes.Count; i++)
            {
                if (myholes[i].reg == GameRegion.LakeRegion && myholes[i].GUID != myhole.GUID)
                {
                    //we have a match 
                    myhole.curPop = myholes[i].curPop;
                    myhole.lastupdated = myholes[i].lastupdated;

                    foundholeQuals.Add((int)myholes[i].holeQual);
                    noOtherHoles = false;
                }
            }
            Debug.Log("foundholeQuals" + Utils.SerializeObject(foundholeQuals));
            for (int i = 0; i < foundholeQuals.Count; i++)
            {
                remainingholeQuals.Remove(foundholeQuals[i]);
            }
            Debug.Log("remainingholes" + Utils.SerializeObject(remainingholeQuals));
            //
            if (noOtherHoles)
            {
                //first ever lake hole. 

                myhole.curPop = settings.maxPops[mylookups[(int)GameRegion.LakeRegion]];
                myhole.lastupdated = curTime;

            }

            int lookup = Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 0.99999999f) * ((float)remainingholeQuals.Count));
            Debug.Log(" returning index:" + lookup + "which is :" + remainingholeQuals[lookup]);
            myhole.holeQual = remainingholeQuals[lookup];

        }

        private static void UpdateOtherLakeHolesPop(FishingHole myhole)
        {
            for (int i = 0; i < myholes.Count; i++)
            {
                if (myholes[i].reg == GameRegion.LakeRegion && myholes[i].GUID != myhole.GUID)
                {
                    //we have a match 
                    myholes[i].curPop = myhole.curPop;
                    myholes[i].lastupdated = myhole.lastupdated;
                }
            }

        }

        private static void DecreaseFishPop(string GUID, GameRegion regionID, float curTime)
        {
            if (regionID == GameRegion.CoastalRegion) return;
            FishingHole hole = GetFishingHole(GUID, regionID, curTime);
            hole.curPop = Mathf.Max(0f, hole.curPop - 1);
            Debug.Log(Enum.GetName(typeof(GameRegion), regionID) + " pop update to" + hole.curPop);
            if (regionID == GameRegion.LakeRegion) UpdateOtherLakeHolesPop(hole);
        }



        private static FishingHole GetFishingHole(string GUID, GameRegion regionID, float curTime)
        {
            //if its in the list return it otherwise create a new one
            //Debug.Log("Locating hole: " + GUID);
            for (int i = 0; i < myholes.Count; i++)
            {
                // Debug.Log("Locating hole: " + GUID + " found hole:" + myholes[i].GUID);
                if (myholes[i].GUID == GUID)
                {
                    //we have a match 
                    return myholes[i];
                }
            }
            //Create a new hole
            FishingHole newhole = new FishingHole();
            newhole.baseChance = settings.baseCatchesPerHour[mylookups[(int)regionID]];
            newhole.maxPop = settings.maxPops[mylookups[(int)regionID]];
            newhole.repop = settings.repopRates[mylookups[(int)regionID]];
            newhole.curPop = newhole.maxPop;
            newhole.GUID = GUID;
            newhole.lastupdated = -1;
            newhole.reg = regionID;
            newhole.holeQual = 1;

            if (regionID == GameRegion.LakeRegion)
            {
                //need a new holeQual
                SetupNewLakeHole(newhole, curTime);
            }


            myholes.Add(newhole);

            return newhole;
        }

        public static float FishingHolesPoisson()
        { //generates a mean 1 discrete random variable drawn from an approximate Poisson distribution return values 0 and up in 0.25 increments
          //Up around 2.5 occurrence is pretty rare.
            float lambda = 4;
            float L = Mathf.Exp(-lambda);
            int k = 0;
            float p = 1;
            do
            {
                k++;
                float u = UnityEngine.Random.Range(0.0f, 1.0f);
                p = p * u;
                //Debug.Log("k:" + k + " p:" + p + " L:" + L);

            } while (p > L);
            return ((float)k - 1.0f) / 4.0f;   //mean is lambda or 4, divide by 4 sets mean to 1
        }


        public static void CatchInProgress(GameRegion regionID, String hole_ID)
        {
            //Debug.Log("Catchinprogress "+ hole_ID);
            catchregion = regionID;
            catchhole = hole_ID;
        }

        public static void FishCaught(bool wasCaught)
        {

            if (catchhole == "none") { return; }

            //Debug.Log(catchhole +": Fish was " + (wasCaught ? "caught" : "released"));

            float curTime = GameManager.GetUniStorm().GetElapsedHours() / 24;
            if (wasCaught && !(catchregion == GameRegion.CoastalRegion))
            {
                DecreaseFishPop(catchhole, catchregion, curTime);
            }
            catchhole = "none";
        }


        public static float getFishingFactor(GameRegion currentRegion, string GUID)
        {
            float curTime = GameManager.GetUniStorm().GetElapsedHours() / 24;
            float answer;

            //before fish factor look to reshuffle holes

            if (curTime > nextShuffle)
            {
                ShuffleLakeHoles();
                float t = settings.daysBetweenLakeHoleQualityReshuffle;
                nextShuffle = curTime + UnityEngine.Random.Range((t - 0.33f * t), (t + 0.33f * t)); ;
            }

            FishingHole hole = GetFishingHole(GUID, currentRegion, curTime);

            if (currentRegion == GameRegion.CoastalRegion)
            {
                // any shoal is a goal  treat hole.lastupdated as the next forecast, maxPop as shoalProb holeQual as ShoalQual
                if (curTime > hole.lastupdated)
                {
                    float shoal = UnityEngine.Random.Range(0f, 1.0f);
                    hole.curPop = (shoal < hole.maxPop) ? (1 / hole.maxPop) : 0; //this preserves a mean value of answer of 1.
                    hole.lastupdated = curTime + UnityEngine.Random.Range((hole.repop - 0.33f * hole.repop) / 24f, (hole.repop + 0.33f * hole.repop) / 24f);//periods between 2 hours and 4
                    Debug.Log("rolled for new shoal:" + shoal + " shoal_thresh:" + hole.maxPop + " next roll in :" + ((hole.lastupdated - curTime) * 24f) + " hours");

                }

                answer = hole.baseChance * glBaseCatchChanceMod * hole.curPop;

                Debug.Log(ggtime(curTime) + " coastal FISH factor:" + answer + " baseChance:" + hole.baseChance + ", shoal factor:" + hole.curPop + " globfactor:" + glBaseCatchChanceMod + " hours to next forecast:" + ggtime(hole.lastupdated) + ", GUID:" + hole.GUID);

            }
            else if (currentRegion == GameRegion.LakeRegion)
            {
                float r = hole.repop / 25f;
                //float oldpop = hole.curPop;
                hole.curPop = verhulstpopmodel(r, hole.maxPop, hole.curPop, curTime - hole.lastupdated);

                hole.lastupdated = curTime;

                float holeQual = settings.lakeHoleQuals[(int)hole.holeQual];

                UpdateOtherLakeHolesPop(hole);

                answer = hole.baseChance * holeQual * glBaseCatchChanceMod * Mathf.Pow((hole.curPop / hole.maxPop), 0.5f);
                if (hole.curPop < 1) answer = 0;
                Debug.Log(ggtime(curTime) + " Lake FISH factor:" + answer + " baseChance:" + hole.baseChance + ", HoleQual factor:" + holeQual + " Pop Factor:" + Mathf.Pow((hole.curPop / hole.maxPop), 0.5f) + " globfactor:" + glBaseCatchChanceMod + " lastupdate:" + ggtime(hole.lastupdated) + ", GUID:" + hole.GUID);

            }
            else
            {
                float r = hole.repop / 25f;
                hole.curPop = verhulstpopmodel(r, hole.maxPop, hole.curPop, curTime - hole.lastupdated);
                hole.lastupdated = curTime;

                answer = hole.baseChance * Mathf.Pow((hole.curPop / hole.maxPop), 0.5f);
                if (hole.curPop < 1) answer = 0;
                Debug.Log(ggtime(curTime) + " Lake FISH factor:" + answer + " baseChance:" + hole.baseChance + " Pop Factor:" + Mathf.Pow((hole.curPop / hole.maxPop), 0.5f) + " globfactor:" + glBaseCatchChanceMod + " lastupdate:" + ggtime(hole.lastupdated) + ", GUID:" + hole.GUID);

            }


            return answer;

        }

        internal static string ggtime(float intime) {

            return string.Format("{0:00}:{1:00}:{2:00}", Mathf.FloorToInt(intime), Mathf.FloorToInt(((intime) % 1) * 24), Mathf.FloorToInt(((intime) % (1f/24f)) *24* 60));
        }

        internal class FishingHole
        {
            public string GUID;
            public GameRegion reg;
            public float lastupdated;
            public float baseChance;
            public float curPop;
            public float maxPop;
            public float repop;
            public float holeQual;
        }


    }
}
