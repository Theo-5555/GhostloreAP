﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System.Reflection;

/* TODO:
4h * define an AP world for Ghostlore (almost done)
5h * interface current systems into the AP client package
2h * detect different goals reached
4h * complete the recipe system in this mod (loading currently unlocked recipes in the restaurant menu)
6h * add Chthonite and Astralite as checks
3h * successfully create a foolproof way of handing over the Chthonite and Astralite items to the player
2h * add chests as checks (up to 50)
2h * add coin rewards to the pool (from chests)
2h * add kill count feed to the right half of the screen HUD
8h * create text field form on character creation that saves an Archipelago profile (this will allow multiple saved characters to have their own individually assigned multiworld)
2h * save granted items to the archipelago profile (in case someone were to create multiple characters under the same server)
total: * 50h
 */

namespace GhostloreAP
{
    public class GLAPModLoader : IModLoader
    {
        private static TestTextDrawer debugMsg;

        private static Harmony harmony;

        private List<IGLAPSingleton> singletons;

        private static string DebugHierarchyPath = Path.Combine(LoadingManager.PersistantDataPath, "debug-hierarchy.txt");

        public static ModSummary modInfo;

        public void OnCreated()
        {
            modInfo = ModManager.Mods.Find((mod) => mod.Name == "Archipelago");
            singletons = new List<IGLAPSingleton>();
            GLAPNotification.EnsureExists();
            singletons.Add(GLAPNotification.instance);

            DebugShowMessage("Huh?");

            DebugShowMessage("Huh??");

            

            DebugShowMessage("Huh????");
        }

        private void InitSingletons()
        {
            CreatureCatalogLogger.instance.Init();
            ItemFactory.instance.Init();
            ExtendedBindingManager.EnsureExists();
            GLAPLocationManager.EnsureExists();
            GLAPItemGiver.EnsureExists();

            singletons.Add(ExtendedBindingManager.instance);
            singletons.Add(GLAPLocationManager.instance);
            singletons.Add(GLAPItemGiver.instance);
            singletons.Add(QuestFactory.instance);
            singletons.Add(CreatureCatalogLogger.instance);
            singletons.Add(ItemFactory.instance);
        }

        private void DisposeSingletons()
        {
            if(singletons == null) { return; }
            foreach(var singleton in singletons)
            {
                if(singleton != null)
                singleton.Cleanup();
            }
            singletons.Clear();
        }

        public void OnGameLoaded(LoadMode mode)
        {
            GameLoadedStart();

        }

        public async void GameLoadedStart()
        {
            GLAPClient.EnsureExists();
            singletons.Add(GLAPClient.instance);
            await GLAPClient.instance.Connect("ChromeWingGL");

            InitSingletons();

            ItemFactory.instance.CacheShopItemNames();
            GLAPLocationManager.instance.StartListeners();
            GLAPNotification.instance.Init();

            harmony = new Harmony("com.chromewing.ghostlore-archipelago");
            harmony.PatchAll();

            ReloadQuests();

        }

        private void ReloadQuests()
        {
            SaveGame.GetSaveGame().Managers.Find((m) => { return m.GetType() == typeof(QuestManager.Data); }).Deserialize();
            QuestFactory.instance.FixAfterAwake();
        }

        public void OnGameUnloaded()
        {
            if(GLAPLocationManager.instance != null)
            {
                GLAPLocationManager.instance.ShutdownListeners();
            }
        }

        public void OnReleased()
        {
            if(harmony != null)
            {
                harmony.UnpatchAll();
            }

            DisposeSingletons();
        }


        public static void DebugShowMessage(string msg_,bool logIt_=false)
        {
            if(debugMsg == null)
            {
                var go = new GameObject("Archipelago");
                debugMsg = go.AddComponent<TestTextDrawer>();
            }

            if (logIt_)
            {
                GLAPNotification.instance.DisplayLog(msg_);
            }
               

            //debugMsg.DisplayMessage(msg_); 
        }

        public static void ReportHierarchy()
        {
            File.WriteAllText(DebugHierarchyPath, DebugRoot());
        }

        static string DebugRoot()
        {
            string result = "----- Debug root -----\n";
            foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
            {
                if (go.transform.parent == null)
                {
                    result+= DebugDeeper(go.transform)+"\n";
                }
            }
            return result;
        }

        static string DebugDeeper(Transform transform)
        {
            int children = transform.childCount;
            string result = String.Format(
                "{0}-{1}>{2} has {3}\n",
                (transform.gameObject.activeSelf ? ("activeself") : ("notactiveself")),
                (transform.gameObject.activeSelf ? ("actInHier") : ("notActInHier")),
                transform.name,
                children
            );
            

            for (int child = 0; child < children; child++)
            {
                result+=DebugDeeper(transform.GetChild(child))+"\n";
            }
            return result;
        }


    }
}
