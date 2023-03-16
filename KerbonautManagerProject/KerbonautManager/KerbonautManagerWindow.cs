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

    private static Rect _windowRect = new(
        (Screen.width - WindowWidth) / 2,
        (Screen.height - WindowHeight) / 3 * 2,
        WindowWidth,
        WindowHeight
    );

    private const string KerbalAlreadyExistsKey = "KerbonautManager/NotificationEvent/KerbalAlreadyExists";
    private const string FirstNameRequiredKey = "KerbonautManager/NotificationEvent/FirstNameRequired";
    private const string LastNameRequiredKey = "KerbonautManager/NotificationEvent/LastNameRequired";

    private const string DefaultSurname = "Kerman";

    private const float CloseButtonSize = 16;
    private const float CloseButtonOffset = 3;

    private static Rect CloseButtonRect => new(
        _windowRect.width - CloseButtonSize - CloseButtonOffset,
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

    private static KerbalInfo _activeKerbalInfo;
    private static string _kerbalName;
    private static string _kerbalSurname = DefaultSurname;

    public static void UpdateGUI()
    {
        GUI.skin = Skins.ConsoleSkin;

        if (!IsOpen)
        {
            return;
        }

        _windowRect = GUILayout.Window(
            GUIUtility.GetControlID(FocusType.Passive),
            _windowRect,
            FillWindow,
            "Kerbonaut Manager",
            GUILayout.Height(WindowHeight),
            GUILayout.Width(WindowWidth)
        );
    }

    private static void FillWindow(int windowID)
    {
        if (GUI.Button(CloseButtonRect, "x"))
        {
            IsOpen = false;
        }

        var kerbalExists = _activeKerbalInfo != null;

        if (kerbalExists)
        {
            CenterLayout(() => GUILayout.Box(
                _activeKerbalInfo.Portrait.texture,
                GUILayout.Height(64),
                GUILayout.Width(64)
            ));
        }

        CenterLayout(() =>
            GUILayout.Label(kerbalExists ? _activeKerbalInfo.Attributes.GetFullName() : "New Kerbonaut")
        );

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("First name");
        GUILayout.FlexibleSpace();
        GUILayout.Label("Last name");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        _kerbalName = GUILayout.TextField(_kerbalName, GUILayout.MaxWidth(WindowWidth / 2 - 16));
        _kerbalSurname = GUILayout.TextField(_kerbalSurname, GUILayout.MaxWidth(WindowWidth / 2 - 16));
        GUILayout.EndHorizontal();

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
        if (!IsKerbalValid(_kerbalName, _kerbalSurname))
        {
            return;
        }

        UpdateKerbal(_activeKerbalInfo, _kerbalName, _kerbalSurname);

        SetSelectedKerbal(null);
    }

    private static void OnCreateKerbal()
    {
        var rosterManager = GameManager.Instance.Game.SessionManager.KerbalRosterManager;
        if (string.IsNullOrEmpty(_kerbalName))
        {
            rosterManager.CreateKerbal();
            SetSelectedKerbal(null);
            return;
        }

        if (!IsKerbalValid(_kerbalName, _kerbalSurname))
        {
            return;
        }

        var createdKerbal = rosterManager.CreateKerbalByName(_kerbalName);
        UpdateKerbal(createdKerbal, _kerbalName, _kerbalSurname);

        SetSelectedKerbal(null);
    }

    private static void OnDeleteKerbal()
    {
        GameManager.Instance.Game.SessionManager.KerbalRosterManager.DestroyKerbal(_activeKerbalInfo.Id);
        SetSelectedKerbal(null);
    }

    private static void UpdateKerbal(KerbalInfo kerbalInfo, string name, string surname)
    {
        var newAttrs = new KerbalAttributes(
            string.Join("_", $"{name} {surname}".Split(' ')).ToUpper(),
            name,
            surname
        );
        kerbalInfo.Attributes = newAttrs;
        if (GameManager.Instance.Game.Messages.TryCreateMessage(out KerbalAddedToRoster msg))
        {
            msg.Kerbal = kerbalInfo;
            GameManager.Instance.Game.Messages.Publish(msg);
        }
    }

    private static bool IsKerbalValid(string name, string surname)
    {
        if (string.IsNullOrEmpty(name))
        {
            GameManager.Instance.Game.Notifications.ProcessNotification(new NotificationData
            {
                Tier = NotificationTier.Passive,
                Primary = new NotificationLineItemData { LocKey = FirstNameRequiredKey }
            });
            return false;
        }

        if (string.IsNullOrEmpty(surname))
        {
            GameManager.Instance.Game.Notifications.ProcessNotification(new NotificationData
            {
                Tier = NotificationTier.Passive,
                Primary = new NotificationLineItemData { LocKey = LastNameRequiredKey }
            });
            return false;
        }

        var kerbalExists = GameManager.Instance.Game.SessionManager.KerbalRosterManager
            .KerbalExists($"{name} {surname}");

        if (kerbalExists)
        {
            GameManager.Instance.Game.Notifications.ProcessNotification(new NotificationData
            {
                Tier = NotificationTier.Passive,
                Primary = new NotificationLineItemData { LocKey = KerbalAlreadyExistsKey }
            });
        }

        return !kerbalExists;
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
            SetKerbalEmpty();
            return;
        }

        GameManager.Instance.Game.SessionManager.KerbalRosterManager.TryGetKerbalByID(
            selectedKerbal!.Value,
            out var selectedKerbalInfo
        );

        _activeKerbalInfo = selectedKerbalInfo;
        _kerbalName = selectedKerbalInfo != null ? _activeKerbalInfo.Attributes.FirstName : "";
        _kerbalSurname = selectedKerbalInfo != null ? _activeKerbalInfo.Attributes.Surname : DefaultSurname;
    }

    private static void SetKerbalEmpty()
    {
        _activeKerbalInfo = null;
        _kerbalName = null;
        _kerbalSurname = DefaultSurname;
    }
}