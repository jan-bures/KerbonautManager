using BepInEx;
using HarmonyLib;
using KSP.Sim.impl;
using KSP.UI;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;

namespace KerbonautManager
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    public class KerbonautManagerPlugin : BaseSpaceWarpPlugin
    {
        #region Metadata

        public const string ModGuid = "com.munix.KerbonautManager";
        public const string ModName = "Kerbonaut Manager";
        public const string ModVer = "0.1.0";

        #endregion

        #region Internal constants

        private const string LocationAPath = "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Popup Canvas/KerbalManager(Clone)/KSP2UIWindow/Root/UIPanel/GRP-Body/KerbalManager_LocationA/Scroll View/Viewport/LocationAPanels/";
        private const string LocationBPath = "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Popup Canvas/KerbalManager(Clone)/KSP2UIWindow/Root/UIPanel/GRP-Body/KerbalManager_LocationB/Scroll View/Viewport/LocationBPanels/";

        internal const string ToolbarButtonID = "BTN-KerbonautManagerOAB";
        
        #endregion

        #region State

        private static KerbonautManagerPlugin _instance;
        
        private bool loaded;
        private List<GameObject> kerbalPanels = new();
        private readonly List<List<IGGuid>> kerbals = new();
        
        #endregion

        #region External objects

        private Camera mainCamera;

        #endregion
        
        
        #region Lifecycle

        public override void OnInitialized()
        {
            base.OnInitialized();

            if (loaded)
            {
                Destroy(this);
            }

            loaded = true;
            _instance = this;
            mainCamera = Camera.main;

            var harmony = new Harmony(ModGuid);
            harmony.PatchAll(typeof(KerbonautManagerPlugin));
            harmony.PatchAll(typeof(KerbalsVanishingPatch));
            
            var buttonTexture = AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png");
            Appbar.RegisterOABAppButton("Kerbonaut Manager", ToolbarButtonID, buttonTexture, isOpen =>
            {
                if (!isOpen)
                {
                    KerbonautManagerWindow.SetSelectedKerbal(null);
                }
                GameObject.Find(ToolbarButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
                KerbonautManagerWindow.IsOpen = isOpen;
            });
            
            Logger.LogInfo("KerbonautManager is initialized.");
        }
        
        private void LateUpdate()
        {
            if (!kerbalPanels.Any() || !Input.GetMouseButtonUp(1))
            {
                return;
            }

            for (var panelIndex = 0; panelIndex < kerbalPanels.Count(); panelIndex++)
            {
                var kerbalButtons = kerbalPanels[panelIndex].transform.GetChild(4);
                for (var kerbalIndex = 0; kerbalIndex < kerbalButtons.childCount; kerbalIndex++)
                {
                    var rectTransform = kerbalButtons.GetChild(kerbalIndex) as RectTransform;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform,
                        Input.mousePosition,
                        mainCamera,
                        out var localPoint
                    );
                    if (!rectTransform!.rect.Contains(localPoint))
                    {
                        continue;
                    }
                    KerbonautManagerWindow.IsOpen = true;
                    KerbonautManagerWindow.SetSelectedKerbal(kerbals[panelIndex][kerbalIndex]);
                    return;
                }
            }
        }
        
        private void OnGUI()
        {
            KerbonautManagerWindow.UpdateGUI();
        }

        #endregion

        #region Harmony patches

        [HarmonyPatch(typeof(KerbalManager), "OnShowWindow")]
        [HarmonyPostfix]
        public static void KerbalManager_OnShowWindow()
        {
            refreshPanels();
            KerbonautManagerWindow.IsOpen = true;
        }

        [HarmonyPatch(typeof(KerbalManager), "OnHideWindow")]
        [HarmonyPrefix]
        public static void KerbalManager_OnHideWindow()
        {
            refreshPanels();
            KerbonautManagerWindow.IsOpen = false;
        }

        [HarmonyPatch(typeof(KerbalManager), "ReloadLocations")]
        [HarmonyPostfix]
        public static void DropDownUpdate()
        {
            refreshPanels();
        }

        #endregion

        #region State update methods
        
        private static void refreshPanels()
        {
            _instance.kerbalPanels.Clear();
            _instance.kerbals.Clear();

            var locationA = GameObject.Find(LocationAPath);
            var locationB = GameObject.Find(LocationBPath);

            addPanelKerbals(locationA);
            addPanelKerbals(locationB);
        }
        
        private static void addPanelKerbals(GameObject location)
        {
            if (location.transform.childCount <= 1)
            {
                return;
            }
                
            var panel = location.transform.GetChild(1).gameObject;
            _instance.kerbalPanels.Add(panel);
            
            var kerbalList = panel.GetComponent<ContextBindRoot>().BoundParentContext
                .Lists["kerbalSlotList"].Data;
            var guids = (
                from kerbal in kerbalList
                where !(bool)kerbal.Properties["isEmpty"].GetObject()
                select (IGGuid)kerbal.Properties["kerbalId"].GetObject()
            ).ToList();

            _instance.kerbals.Add(guids);
        }

        #endregion
    }
}