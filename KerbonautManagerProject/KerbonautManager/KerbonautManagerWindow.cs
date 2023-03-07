using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.UI.Binding;
using SpaceWarp.API.UI;
using UnityEngine;

namespace KerbonautManager;

public static class KerbonautManagerWindow
{
    // Window options
    private const float WindowWidth = 350;
    private const float WindowHeight = 150;

    private static Rect windowRect = new(
        (Screen.width - WindowWidth) / 2,
        (Screen.height - WindowHeight) / 3 * 2,
        WindowWidth,
        WindowHeight
    );

    private const float CloseButtonSize = 16;
    private const float CloseButtonOffset = 3;

    private static Rect closeButtonRect => new(
        windowRect.width - CloseButtonSize - CloseButtonOffset,
        CloseButtonOffset,
        CloseButtonSize,
        CloseButtonSize
    );

    // State
    private static bool _isOpen;

    public static bool IsOpen
    {
        get => _isOpen;
        set
        {
            _isOpen = value;
            GameObject.Find(KerbonautManagerPlugin.ToolbarButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()
                ?.SetValue(_isOpen);
        }
    }

    private static KerbalInfo activeKerbalInfo;
    private static string kerbalName;

    public static void UpdateGUI()
    {
        GUI.skin = Skins.ConsoleSkin;

        if (!IsOpen)
        {
            return;
        }

        windowRect = GUILayout.Window(
            GUIUtility.GetControlID(FocusType.Passive),
            windowRect,
            FillWindow,
            "Kerbonaut Manager",
            GUILayout.Height(WindowHeight),
            GUILayout.Width(WindowWidth)
        );
    }

    private static void FillWindow(int windowID)
    {
        if (GUI.Button(closeButtonRect, "x"))
        {
            IsOpen = false;
        }

        var kerbalExists = activeKerbalInfo != null;

        if (kerbalExists)
        {
            CenterLayout(() => GUILayout.Box(
                activeKerbalInfo.Portrait.texture,
                GUILayout.Height(64),
                GUILayout.Width(64)
            ));
        }

        CenterLayout(() =>
            GUILayout.Label(kerbalExists ? activeKerbalInfo.Attributes.GetFullName() : "New Kerbonaut")
        );

        kerbalName = GUILayout.TextField(kerbalName);

        if (!kerbalExists)
        {
            GUILayout.Label("Leaving name empty will generate a random kerbonaut.");
        }

        GUILayout.BeginHorizontal();

        if (kerbalExists)
        {
            if (GUILayout.Button("Save"))
            {
                OnSaveKerbal();
            }

            if (GUILayout.Button("Cancel"))
            {
                SetSelectedKerbal(null);
            }
        }
        else
        {
            if (GUILayout.Button("Create"))
            {
                OnCreateKerbal();
            }

            if (GUILayout.Button("Close"))
            {
                IsOpen = false;
            }
        }

        GUILayout.EndHorizontal();

        if (kerbalExists && GUILayout.Button("Delete"))
        {
            OnDeleteKerbal();
        }

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private static void OnSaveKerbal()
    {
        var newAttrs = new KerbalAttributes(
            string.Join("_", kerbalName.Split(' ')).ToUpper(),
            kerbalName,
            KerbalAttributes.DEFAULT_KERBAL_SURNAME
        );
        activeKerbalInfo.Attributes = newAttrs;
        if (GameManager.Instance.Game.Messages.TryCreateMessage(out KerbalAddedToRoster msg))
        {
            msg.Kerbal = activeKerbalInfo;
            GameManager.Instance.Game.Messages.Publish(msg);
        }

        SetSelectedKerbal(null);
    }

    private static void OnCreateKerbal()
    {
        if (!string.IsNullOrEmpty(kerbalName))
        {
            GameManager.Instance.Game.SessionManager.KerbalRosterManager.CreateKerbalByName(kerbalName);
        }
        else
        {
            GameManager.Instance.Game.SessionManager.KerbalRosterManager.CreateKerbal();
        }

        SetSelectedKerbal(null);
    }

    private static void OnDeleteKerbal()
    {
        GameManager.Instance.Game.SessionManager.KerbalRosterManager.DestroyKerbal(activeKerbalInfo.Id);
        SetSelectedKerbal(null);
    }

    private static void CenterLayout(Action content)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        content();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    public static void SetSelectedKerbal(IGGuid? selectedKerbal)
    {
        if (selectedKerbal == null)
        {
            activeKerbalInfo = null;
            kerbalName = null;
            return;
        }

        GameManager.Instance.Game.SessionManager.KerbalRosterManager.TryGetKerbalByID(
            selectedKerbal!.Value,
            out var selectedKerbalInfo
        );

        if (activeKerbalInfo == selectedKerbalInfo)
        {
            activeKerbalInfo = null;
            kerbalName = null;
        }

        activeKerbalInfo = selectedKerbalInfo;
        kerbalName = selectedKerbalInfo != null ? activeKerbalInfo.Attributes.FirstName : "";
    }
}