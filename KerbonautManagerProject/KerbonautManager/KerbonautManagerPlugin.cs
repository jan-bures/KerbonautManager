using System.Collections;
using BepInEx;
using HarmonyLib;
using KSP.Game;
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
        private enum Panel
        {
            A,
            B
        }

        #region Metadata

        public const string ModGuid = "com.munix.KerbonautManager";
        public const string ModName = "Kerbonaut Manager";
        public const string ModVer = "0.2.1";

        #endregion

        #region Internal constants

        private const string LocationAPath =
            "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Popup Canvas/KerbalManager(Clone)/KSP2UIWindow/Root/UIPanel/GRP-Body/KerbalManager_LocationA/Scroll View/Viewport/LocationAPanels/";

        private const string LocationBPath =
            "GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Popup Canvas/KerbalManager(Clone)/KSP2UIWindow/Root/UIPanel/GRP-Body/KerbalManager_LocationB/Scroll View/Viewport/LocationBPanels/";

        internal const string ToolbarButtonID = "BTN-KerbonautManagerOAB";

        #endregion

        #region State

        private static KerbonautManagerPlugin _instance;

        private bool _loaded;
        private Dictionary<Panel, GameObject> _kerbalPanels = new() { { Panel.A, null }, { Panel.B, null } };
        private readonly Dictionary<Panel, List<IGGuid>> _kerbals = new();

        #endregion

        #region External objects

        private Camera _mainCamera;

        #endregion


        #region Lifecycle

        public override void OnInitialized()
        {
            base.OnInitialized();

            if (_loaded)
            {
                Destroy(this);
            }

            _loaded = true;
            _instance = this;
            _mainCamera = Camera.main;

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
            if (!Input.GetMouseButtonDown(1) || !IsInVab() || (!_kerbalPanels[Panel.A] && !_kerbalPanels[Panel.B]))
            {
                return;
            }

            ProcessPanel(Panel.A);
            ProcessPanel(Panel.B);
        }

        private void ProcessPanel(Panel panel)
        {
            if (!_kerbalPanels[panel])
            {
                return;
            }

            var kerbalButtons = _kerbalPanels[panel].transform.GetChild(4);
            for (var kerbalIndex = 0; kerbalIndex < kerbalButtons.childCount; kerbalIndex++)
            {
                var rectTransform = kerbalButtons.GetChild(kerbalIndex) as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    Input.mousePosition,
                    _mainCamera,
                    out var localPoint
                );
                if (!rectTransform!.rect.Contains(localPoint))
                {
                    continue;
                }

                KerbonautManagerWindow.IsOpen = true;
                KerbonautManagerWindow.SetSelectedKerbal(_kerbals[panel][kerbalIndex]);
                return;
            }
        }

        private void OnGUI()
        {
            KerbonautManagerWindow.UpdateGUI();
        }

        private static bool IsInVab()
        {
            return Game && Game.GlobalGameState?.GetState() == GameState.VehicleAssemblyBuilder;
        }

        #endregion

        #region Harmony patches

        [HarmonyPatch(typeof(KerbalManager), "OnShowWindow")]
        [HarmonyPostfix]
        public static void KerbalManager_OnShowWindow()
        {
            if (!IsInVab())
            {
                return;
            }

            RefreshPanel(Panel.A);
            RefreshPanel(Panel.B);
            KerbonautManagerWindow.IsOpen = true;
        }

        [HarmonyPatch(typeof(KerbalManager), "OnHideWindow")]
        [HarmonyPrefix]
        public static void KerbalManager_OnHideWindow()
        {
            if (!IsInVab())
            {
                return;
            }

            RefreshPanel(Panel.A);
            RefreshPanel(Panel.B);
            KerbonautManagerWindow.IsOpen = false;
        }

        [HarmonyPatch(typeof(KerbalManager), "ReloadLocations")]
        [HarmonyPostfix]
        public static void KerbalManager_ReloadLocations()
        {
            if (!IsInVab())
            {
                return;
            }

            RefreshPanel(Panel.A);
            RefreshPanel(Panel.B);
        }

        [HarmonyPatch(typeof(KerbalManager), "OnDropdownASelection")]
        [HarmonyPostfix]
        public static void KerbalManager_OnDropdownASelection()
        {
            if (!IsInVab())
            {
                return;
            }

            RefreshPanel(Panel.A);
        }

        [HarmonyPatch(typeof(KerbalManager), "OnDropdownBSelection")]
        [HarmonyPostfix]
        public static void KerbalManager_OnDropdownBSelection()
        {
            if (!IsInVab())
            {
                return;
            }

            RefreshPanel(Panel.B);
        }

        #endregion

        #region State update methods

        private static void RefreshPanel(Panel panel)
        {
            _instance._kerbalPanels[panel] = null;
            _instance._kerbals[panel] = null;

            AddPanelKerbals(panel, GameObject.Find(panel == Panel.A ? LocationAPath : LocationBPath));
        }

        private static void AddPanelKerbals(Panel panel, GameObject location)
        {
            if (location.transform.childCount <= 1)
            {
                return;
            }

            var panelObject = location.transform.GetChild(1).gameObject;
            _instance._kerbalPanels[panel] = panelObject;

            var kerbalList = panelObject.GetComponent<ContextBindRoot>().BoundParentContext
                .Lists["kerbalSlotList"].Data;
            var guids = (
                from kerbal in kerbalList
                where !(bool)kerbal.Properties["isEmpty"].GetObject()
                select (IGGuid)kerbal.Properties["kerbalId"].GetObject()
            ).ToList();

            _instance._kerbals[panel] = guids;
        }

        #endregion
    }
}