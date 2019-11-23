using Harmony;
using AK;
using UnityEngine;


namespace Fishing
{


    [HarmonyPatch(typeof(GearItem), "Deserialize")]
    internal class GearItem_Deserialize
    {
        internal static void Postfix(GearItem __instance)
        {
            if (__instance.name == "GEAR_RawLakeWhiteFish" ||
                __instance.name == "GEAR_RawSmallMouthBass" ||
                __instance.name == "GEAR_RawRainbowTrout" ||
                __instance.name == "GEAR_RawCohoSalmon")
            { 


                float length = Mathf.Pow(__instance.m_WeightKG, 0.3333f);

                float lengthfactor = 1f;

                if (__instance.name == "GEAR_RawLakeWhiteFish") lengthfactor = 0.8f;
                if (__instance.name == "GEAR_RawSmallMouthBass") lengthfactor = 0.9f;
                if (__instance.name == "GEAR_RawRainbowTrout") lengthfactor = 0.8f;
                if (__instance.name == "GEAR_RawCohoSalmon") lengthfactor = 0.74f;

                __instance.gameObject.transform.localScale *= length * lengthfactor;
            }
        }
    }



    [HarmonyPatch(typeof(SaveGameSystem), "RestoreGlobalData")]
    internal class SaveGameSystemPatch_RestoreGlobalData
    {
        internal static void Postfix(string name)
        {
            FishingHoles.LoadData(name);
        }
    }

    [HarmonyPatch(typeof(SaveGameSystem), "SaveGlobalData")]
    internal class SaveGameSystemPatch_SaveGlobalData
    {
        public static void Postfix(SaveSlotType gameMode, string name)
        {
            FishingHoles.SaveData(gameMode, name);
        }
    }

    [HarmonyPatch(typeof(IceFishingHole), "MaybeCatchFish")]
    internal class IceFishingHoleMaybeCatchFish
    {
        private static bool Prefix(IceFishingHole __instance, bool ___m_FishingInProgress, ref float ___m_ElapsedFishingTimeMinutes)
        {
            float fishingInterval = 5f;

            if (!___m_FishingInProgress)
            {
                return false; ;
            }
            if (GameManager.GetPlayerManagerComponent().IsInspectModeActive())
            {
                return false; ;
            }
            if (InterfaceManager.m_Panel_GenericProgressBar.IsPaused())
            {
                InterfaceManager.m_Panel_GenericProgressBar.Resume();
                GameAudioManager.PlaySound(EVENTS.RESUME_SEARCHCONTAINER, GameManager.GetPlayerObject());
            }
            if (___m_ElapsedFishingTimeMinutes > fishingInterval)
            {
                float myrand = UnityEngine.Random.Range(0.0f, 1.0f);

                float popfactor = FishingHoles.getFishingFactor(RegionManager.GetCurrentRegion(), Utils.GetGuidFromGameObject((__instance.gameObject)));
                
                float fishing_skill = GameManager.GetSkillIceFishing().ReduceFishingTimeScale();
                
                float myprob = ((1f / fishing_skill) * popfactor)*(fishingInterval/60f);
                Debug.Log("Rolling for fish, FISH catch chance (per hour avg):" + popfactor + " Skill modifier:" + (1f/fishing_skill) + " final prob:" + myprob+ " rand:"+myrand);
                if (myrand < myprob)
                {
                    if (Utils.RollChance(GameManager.GetSkillIceFishing().GetLineBreakOnChancePercent()))
                    {
                        AccessTools.Method(typeof(IceFishingHole), "LineBreak").Invoke(__instance, null);  //CHECKTHIS
                    }
                    else
                    {
                        AccessTools.Method(typeof(IceFishingHole), "CatchFish").Invoke(__instance, null);
                        FishingHoles.CatchInProgress(RegionManager.GetCurrentRegion(), Utils.GetGuidFromGameObject((__instance.gameObject)));
                    }
                }
                ___m_ElapsedFishingTimeMinutes = 0;
            }
            return false;
        }

    }
    
    [HarmonyPatch(typeof(IceFishingHole), "RevealFishInInspectMode")]
    internal class IceFishingHoleRevealFishInInspectMode
    {
        private static bool Prefix(IceFishingHole __instance, GameObject go, ref float ___m_ElapsedFishingTimeMinutes)
        {

            GearItem component = go.GetComponent<GearItem>();
            if (!component)
            {
                return false;
            }
            component.gameObject.SetActive(true);
            component.m_CurrentHP = UnityEngine.Random.Range(0.9f * component.m_MaxHP, component.m_MaxHP);

            float myrol1 = FishingHoles.FishingHolesPoisson();
            float myrol2 = FishingHoles.FishingHolesPoisson(); //do it twice to widen the tail of the distribution
            float FISHmod = (myrol1 * myrol2 + 0.25f) / 1.25f; //ensure mod is Non-zero

            float skillmod = GameManager.GetSkillIceFishing().GetFishWeightScale();

            Debug.Log("Fish mod:" + FISHmod +"skill mod:"+skillmod + " changing " + component.name + " weight from:" + component.m_WeightKG + " to:" + component.m_WeightKG * FISHmod * skillmod);
       
            component.m_WeightKG = component.m_WeightKG * FISHmod* skillmod;
            float length = Mathf.Pow(component.m_WeightKG, 0.3333f);
 
            float lengthfactor = 1f;

            if (component.name == "GEAR_RawLakeWhiteFish") lengthfactor = 0.8f;
            if (component.name == "GEAR_RawSmallMouthBass") lengthfactor = 0.9f;
            if (component.name == "GEAR_RawRainbowTrout") lengthfactor = 0.8f;
            if (component.name == "GEAR_RawCohoSalmon") lengthfactor = 0.74f;

            component.gameObject.transform.localScale *= length * lengthfactor;

            component.m_FoodItem.m_CaloriesTotal *= FISHmod * skillmod;
            component.m_FoodItem.m_CaloriesRemaining *= FISHmod * skillmod;


            GameManager.GetPlayerManagerComponent().EnterInspectGearModeFromFishingHole(component, __instance);

            InterfaceManager.m_Panel_GenericProgressBar.Pause();
            GameAudioManager.PlaySound(EVENTS.PAUSE_SEARCHCONTAINER, GameManager.GetPlayerObject());
            StatsManager.IncrementValue(StatID.FishCaught, component.GetItemWeightKG());
            GameManager.GetAchievementManagerComponent().CaughtFish(component);

            component.MarkAsHarvested();
            return false;
        }
    }

        [HarmonyPatch(typeof(PlayerManager), "PlayPutBackAudio")]
    internal class PlayerManagerPlayPutBackAudio_Inc_Pop
    {
        private static void Postfix()
        {
            FishingHoles.FishCaught(false);
        }

    }
    [HarmonyPatch(typeof(PlayerManager), "CanPickup")]
    internal class PlayerManager_CanPickup_Dec_Pop_trigger
    {
        private static void Postfix()
        {
            FishingHoles.FishCaught(true);
        }

    }
}




