using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Recovered.FlowWater;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Reconnects the extracted scene controls to the local recovered game. Original
/// MonoBehaviour fields remain intact. Billing and advertising are deliberately
/// represented as unavailable local services, never as successful transactions.
/// </summary>
public sealed class FlowWaterRecoveryUI : MonoBehaviour
{
    public static FlowWaterRecoveryUI Instance { get; private set; }
    public bool IsInGame { get; private set; }
    public int SelectedPart { get; private set; } = 9;
    public int SelectedTab { get; private set; } = 1;
    public string LastAction { get; private set; } = "Ready";

    const string MusicKey = "Recovery.MusicVolume";
    const string SoundKey = "Recovery.SoundVolume";
    const string CompletePrefix = "Recovery.Complete.";
    readonly List<int> levelIds = new List<int>();
    readonly HashSet<int> available = new HashSet<int>();
    Canvas overlay;
    Font font;
    GameObject menuBar;
    GameObject gameBar;
    GameObject gameFooter;
    GameObject dialog;
    GameObject catalog;
    Text gameTitle;
    Text gameStatus;
    Text menuStatus;
    Text catalogTitle;
    Text soundLabel;
    Text musicLabel;
    Text[] catalogLabels;
    Button[] catalogButtons;
    Action dialogConfirm;
    RectTransform pagingPanels;
    Vector2 pagingOrigin;
    int page;
    int pageCount = 5;
    int catalogPage;
    bool escapeWasDown;
    float musicVolume = 0.65f;
    float soundVolume = 1f;
    string settingsReturnScene = "Portada";
    string storeReturnScene = "SelLevel0";
    RecoveredFlowGame subscribedGame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var root = new GameObject("RecoveryUI");
        DontDestroyOnLoad(root);
        root.AddComponent<FlowWaterRecoveryUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.65f);
        soundVolume = PlayerPrefs.GetFloat(SoundKey, 1f);
        // gmNNN is the stable source identifier. Save.level is not unique.
        AddRange(1, 200); AddRange(251, 300); AddRange(351, 800);
        AddRange(1001, 1250); AddRange(1651, 2050);
        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (subscribedGame != null) subscribedGame.StateChanged -= RefreshGame;
        if (Instance == this) Instance = null;
    }

    void AddRange(int first, int last)
    {
        for (int id = first; id <= last; id++) { levelIds.Add(id); available.Add(id); }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsInGame = false;
        gameBar.SetActive(false);
        gameFooter.SetActive(false);
        menuBar.SetActive(true);
        catalog.SetActive(false);
        dialog.SetActive(false);
        pagingPanels = null;
        page = 0;
        ConfigureOriginalScene(scene);
        BindSceneControls(scene);
        ApplyVolumes();
        RefreshMenu();
        // The original loader methods were absent from the export. Direct Play
        // from any original gameplay scene must enter the recovered loader too.
        foreach (var loader in SceneComponents<SaveLoadManager>(scene))
        {
            int id = PlayerPrefs.GetInt("Recovery.LastLevelId", 1);
            StartGame(available.Contains(id) ? id : 1);
            break;
        }
        // Canvas layout and animation state are evaluated on the first frame.
        StartCoroutine(FinalizeScene(scene));
    }

    IEnumerator FinalizeScene(Scene scene)
    {
        yield return null;
        if (scene == SceneManager.GetActiveScene() && !IsInGame)
        {
            StopBlockingFades(scene);
            Canvas.ForceUpdateCanvases();
        }
    }

    IEnumerable<T> SceneComponents<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
                yield return component;
    }

    void ConfigureOriginalScene(Scene scene)
    {
        foreach (var originalSystem in SceneComponents<EventSystem>(scene))
            originalSystem.gameObject.SetActive(false);
        foreach (var canvas in SceneComponents<Canvas>(scene))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                canvas.transform.localScale = Vector3.one;
            foreach (var animator in canvas.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
        }
        StopBlockingFades(scene);
        foreach (var port in SceneComponents<UIManagerPortScript>(scene))
            SetActive(port.panelExit, false);
        foreach (var options in SceneComponents<UIManagerOptScript>(scene))
        {
            SetActive(options.cajaReset, false); SetActive(options.cajaProgress, false);
            if (options.volumen1 != null)
            {
                options.volumen1.onValueChanged = new Slider.SliderEvent();
                options.volumen1.SetValueWithoutNotify(musicVolume);
                options.volumen1.onValueChanged.AddListener(SetMusicVolume);
            }
            if (options.volumen2 != null)
            {
                options.volumen2.onValueChanged = new Slider.SliderEvent();
                options.volumen2.SetValueWithoutNotify(soundVolume);
                options.volumen2.onValueChanged.AddListener(SetSoundVolume);
            }
        }
        foreach (var store in SceneComponents<UIManagerStoreScript>(scene))
            SetActive(store.cajaError, false);
        foreach (var selector in SceneComponents<UIManagerSelScript0>(scene))
        {
            SetActive(selector.panelDesign0, false); SetActive(selector.panelDesign, false);
            SetActive(selector.panelUnlock, false); SetActive(selector.popupSpinBall, false);
            foreach (var field in typeof(UIManagerSelScript0).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType == typeof(GameObject) &&
                    (field.Name.StartsWith("candado", StringComparison.Ordinal) ||
                     field.Name.StartsWith("fin", StringComparison.Ordinal)))
                    SetActive((GameObject)field.GetValue(selector), false);
            }
            ShowCategory(SelectedTab);
        }
        foreach (var selector in SceneComponents<SelectLevel1>(scene))
        {
            for (int i = 0; i < 50; i++)
            {
                int id = ResolveLevel(i + 1);
                bool completed = PlayerPrefs.GetInt(CompletePrefix + id, 0) != 0;
                SetArrayActive(selector.cuadroNoActivo, i, true);
                SetArrayActive(selector.cuadroActivo, i, completed);
                SetArrayActive(selector.candados, i, false);
                SetArrayActive(selector.checkOk, i, completed);
                SetArrayText(selector.objNum, i, (i + 1).ToString());
                SetArrayText(selector.objNum2, i, (i + 1).ToString());
            }
            if (selector.animatorPaneles != null)
                pagingPanels = selector.animatorPaneles.transform as RectTransform;
        }
        if (scene.name == "Store")
        {
            foreach (var rect in SceneComponents<RectTransform>(scene))
                if (rect.name == "Paneles") { pagingPanels = rect; break; }
            pageCount = 2;
        }
        else pageCount = 5;
        if (pagingPanels != null)
        {
            pagingOrigin = pagingPanels.anchoredPosition;
            pagingOrigin.x = 0;
            UpdatePage();
        }
        if (scene.name == "SelLevel1")
            foreach (var text in SceneComponents<Text>(scene))
                if (String.Equals(text.text.Trim(), "EASY", StringComparison.OrdinalIgnoreCase))
                    text.text = SelectedPart == 9 ? "BASIC" : PackName(SelectedPart);
    }

    void StopBlockingFades(Scene scene)
    {
        foreach (var rect in SceneComponents<RectTransform>(scene))
            if (rect.name.StartsWith("PanelFade", StringComparison.Ordinal))
                rect.gameObject.SetActive(false);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    static void SetArrayActive(GameObject[] items, int index, bool active)
    {
        if (items != null && index < items.Length) SetActive(items[index], active);
    }

    static void SetArrayText(Text[] items, int index, string text)
    {
        if (items != null && index < items.Length && items[index] != null)
            items[index].text = text;
    }

    void BindSceneControls(Scene scene)
    {
        var levelButtons = new Dictionary<GameObject, int>();
        foreach (var trigger in SceneComponents<EventTrigger>(scene))
        {
            if (trigger.triggers == null) continue;
            foreach (var entry in trigger.triggers)
            {
                if (entry.callback == null || entry.callback.GetPersistentEventCount() == 0) continue;
                string method = entry.callback.GetPersistentMethodName(0);
                if (String.IsNullOrEmpty(method)) continue;
                int argument = PersistentInt(entry.callback, 0);
                var source = trigger.gameObject;
                if (method == "Level") levelButtons[source] = argument;
                // Replace each original event once; pointer down and pointer up
                // animation-only entries are not converted into extra clicks.
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener(data => Dispatch(method, argument, source));
            }
        }
        foreach (var button in SceneComponents<Button>(scene))
        {
            if (button.onClick.GetPersistentEventCount() == 0) continue;
            string method = button.onClick.GetPersistentMethodName(0);
            int argument = PersistentInt(button.onClick, 0);
            var source = button.gameObject;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Dispatch(method, argument, source));
        }
        // The exported level cells contain both a Button with an empty onClick
        // and an EventTrigger holding Level(N). Reconnect both public controls so
        // keyboard/submit and direct Button activation work as well as pointer down.
        foreach (var item in levelButtons)
        {
            var source = item.Key;
            int argument = item.Value;
            var button = source.GetComponent<Button>();
            if (button == null) continue;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Dispatch("Level", argument, source));
        }
    }

    static int PersistentInt(UnityEventBase unityEvent, int index)
    {
        // Unity exposes persistent target/method publicly but not the serialized
        // integer. Reading it preserves the original scene's pack/level links.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var group = typeof(UnityEventBase).GetField("m_PersistentCalls", flags).GetValue(unityEvent);
        var calls = (IList)group.GetType().GetField("m_Calls", flags).GetValue(group);
        var call = calls[index];
        var arguments = call.GetType().GetField("m_Arguments", flags).GetValue(call);
        return (int)arguments.GetType().GetField("m_IntArgument", flags).GetValue(arguments);
    }

    void Dispatch(string method, int argument, GameObject source)
    {
        LastAction = method + "(" + argument + ")";
        switch (method)
        {
            case "botonPlayDown":
                if (SceneManager.GetActiveScene().name == "Settings")
                {
                    if (source.name == "Music") ToggleMusic(); else ToggleSound();
                }
                else SceneManager.LoadScene("SelLevel0");
                break;
            case "botonOptionsDown": OpenSettings(); break;
            case "botonTabDown": ShowCategory(argument); break;
            case "botonPartDown": OpenPack(argument); break;
            case "Level": OpenLevel(argument); break;
            case "botonMenuDown":
                if (SceneManager.GetActiveScene().name == "SelLevel1") SceneManager.LoadScene("SelLevel0");
                else GoHome();
                break;
            case "botonBackDown":
                SceneManager.LoadScene(SceneManager.GetActiveScene().name == "Store" ? storeReturnScene : settingsReturnScene);
                break;
            case "botonStoreDown": case "botonBuyOff": case "buyOffDown":
            case "botonOkUnlockDown": OpenStore(); break;
            case "botonFlechaIzquierda": ShiftPage(-1); break;
            case "botonFlechaDerecha": ShiftPage(1); break;
            case "botonExitDown": Confirm("Exit Flow Water?", Application.Quit); break;
            case "botonCheckOkDown": Application.Quit(); break;
            case "botonResetDown": Confirm("Reset completed levels in this local recovery?", ResetProgress); break;
            case "botonResetOk": ResetProgress(); break;
            case "botonCancelDown": case "botonCancelUnlockDown": case "botonResetCancel":
            case "botonProgressClose": case "botonCloseCajaDown":
            case "cancelWatch": case "BotonCerrarPopupSpinBall": case "BotonLaterSpinBall": CloseDialog(); break;
            case "botonLangDown": Notice("The recovered local interface uses English. Original localization code was not present in the exported project."); break;
            case "botonRestartDown": if (IsInGame) Restart(); else ShowCatalog(); break;
            case "botonRateDown": case "BotonInstalarSpinBall":
                Notice("This local recovery has no connected app-store service."); break;
            default:
                if (method.IndexOf("Buy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.IndexOf("Ads", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.IndexOf("Watch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.StartsWith("IntNo", StringComparison.Ordinal) || method == "noUnity" || method == "noAdmob")
                    Notice("Ads and purchases are unavailable in this local recovery. No payment is made. All recovered level assets are available through Levels.");
                else if (method.IndexOf("Design", StringComparison.OrdinalIgnoreCase) >= 0 || method == "solu0")
                    Notice("The original level designer was not recovered. Open a recovered level and use Hint to inspect its saved solution.");
                break;
        }
        RefreshMenu();
    }

    public void OpenPack(int part)
    {
        SelectedPart = Math.Max(1, part);
        UIManagerSelScript0.parte = SelectedPart;
        UIManagerSelScript0.subparte = 1;
        LastAction = "Open pack " + SelectedPart;
        SceneManager.LoadScene("SelLevel1");
    }

    public void OpenLevel(int level)
    {
        int id = ResolveLevel(level);
        if (!available.Contains(id))
        {
            Notice("The exported game does not contain gm" + id + ". Choose an existing asset in Levels.");
            return;
        }
        StartGame(id);
    }

    int ResolveLevel(int level)
    {
        // Original pack handlers are empty. Consecutive 50-level source groups
        // are a documented reconstruction, not recovered original difficulty logic.
        return (Math.Max(1, SelectedPart) - 1) * 50 + Math.Max(1, Math.Min(level, 50));
    }

    public void StartGame(int id)
    {
        var game = RecoveredFlowGame.EnsureCreated();
        if (subscribedGame != game)
        {
            if (subscribedGame != null) subscribedGame.StateChanged -= RefreshGame;
            subscribedGame = game;
            game.StateChanged += RefreshGame;
        }
        IsInGame = true;
        catalog.SetActive(false); dialog.SetActive(false);
        menuBar.SetActive(false); gameBar.SetActive(true); gameFooter.SetActive(true);
        LastAction = "Load gm" + id;
        PlayerPrefs.SetInt("Recovery.LastLevelId", id);
        PlayerPrefs.Save();
        game.LoadLevel(id);
        RefreshGame();
    }

    public void ShowLevelSelection()
    {
        ReturnFromGame();
        SceneManager.LoadScene("SelLevel1");
    }

    public void GoHome()
    {
        ReturnFromGame();
        SceneManager.LoadScene("Portada");
    }

    void ReturnFromGame()
    {
        if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.ReturnToOriginalScene();
        IsInGame = false;
    }

    void OpenSettings()
    {
        settingsReturnScene = SceneManager.GetActiveScene().name;
        ReturnFromGame();
        SceneManager.LoadScene("Settings");
    }

    void OpenStore()
    {
        storeReturnScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("Store");
    }

    public void ShowCategory(int tab)
    {
        SelectedTab = Mathf.Clamp(tab, 1, 6);
        foreach (var selector in SceneComponents<UIManagerSelScript0>(SceneManager.GetActiveScene()))
        {
            var tabs = new[] { selector.grupoTab1, selector.grupoTab2, selector.grupoTab3,
                selector.grupoTab4, selector.grupoTab5, selector.grupoTab6 };
            var previews = new[] { selector.panel1, selector.panel2, selector.panel3,
                selector.panel4, selector.panel5, selector.panel6 };
            for (int i = 0; i < tabs.Length; i++)
            {
                SetActive(tabs[i], i == SelectedTab - 1);
                SetActive(previews[i], i == SelectedTab - 1);
            }
        }
        RefreshMenu();
    }

    void ShiftPage(int direction)
    {
        page = Mathf.Clamp(page + direction, 0, pageCount - 1);
        UpdatePage();
        RefreshMenu();
    }

    void UpdatePage()
    {
        if (pagingPanels == null) return;
        pagingPanels.anchoredPosition = pagingOrigin + Vector2.left * (page * 900);
        for (int i = 0; i < pageCount; i++)
        {
            Transform child = pagingPanels.Find("Panel" + (i + 1));
            if (child != null) child.gameObject.SetActive(i == page);
        }
    }

    static string PackName(int part)
    {
        switch (part)
        {
            case 1: return "EASY";
            case 2: return "HARD";
            case 3: return "MASTER";
            case 4: return "GENIUS";
            case 6: return "MIX";
            case 8: return "MANIAC";
            case 9: return "BASIC";
            default: return "PACK " + part;
        }
    }

    void Restart() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.RestartLevel(); }
    void Hint() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.ApplyHint(); }
    void Undo() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.Undo(); }
    void Next() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.NextLevel(); }
    void Raise() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.RaiseSelectedPiece(); }
    void Lower() { if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.LowerSelectedPiece(); }

    void RefreshGame()
    {
        var game = RecoveredFlowGame.Instance;
        if (game == null) return;
        gameTitle.text = "FLOW WATER  /  gm" + game.CurrentLevelId + "     Moves " + game.MoveCount;
        gameStatus.text = game.Status;
        if (game.IsComplete)
        {
            PlayerPrefs.SetInt(CompletePrefix + game.CurrentLevelId, 1);
            PlayerPrefs.Save();
        }
    }

    void RefreshMenu()
    {
        if (menuStatus == null) return;
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "Store") menuStatus.text = "Local build: purchases / ads offline";
        else if (scene == "SelLevel1")
            menuStatus.text = "gm" + ResolveLevel(1) + " - gm" + ResolveLevel(50) + "   " + (page + 1) + "/5";
        else menuStatus.text = "1,350 recovered levels";
        if (musicLabel != null) musicLabel.text = musicVolume > 0 ? "Music On" : "Music Off";
        if (soundLabel != null) soundLabel.text = soundVolume > 0 ? "Sound On" : "Sound Off";
    }

    void ToggleMusic() { SetMusicVolume(musicVolume > 0 ? 0 : 0.65f); }
    void ToggleSound() { SetSoundVolume(soundVolume > 0 ? 0 : 1f); }
    void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, musicVolume);
        PlayerPrefs.Save(); ApplyVolumes(); RefreshMenu();
    }
    void SetSoundVolume(float value)
    {
        soundVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SoundKey, soundVolume);
        PlayerPrefs.Save(); ApplyVolumes(); RefreshMenu();
    }
    void ApplyVolumes()
    {
        AudioListener.volume = 1;
        foreach (var source in FindObjectsOfType<AudioSource>(true))
            source.volume = source.loop ? musicVolume : soundVolume;
    }

    void ResetProgress()
    {
        foreach (int id in levelIds) PlayerPrefs.DeleteKey(CompletePrefix + id);
        PlayerPrefs.Save();
        CloseDialog();
        ConfigureOriginalScene(SceneManager.GetActiveScene());
        Notice("Local completed-level progress reset.");
    }

    void Update()
    {
        bool escape = Input.GetKey(KeyCode.Escape);
        if (escape && !escapeWasDown)
        {
            if (dialog.activeSelf) CloseDialog();
            else if (catalog.activeSelf) catalog.SetActive(false);
            else if (IsInGame) ShowLevelSelection();
            else if (SceneManager.GetActiveScene().name == "SelLevel1") SceneManager.LoadScene("SelLevel0");
            else GoHome();
        }
        escapeWasDown = escape;
    }

    void BuildOverlay()
    {
        var canvasObject = new GameObject("RecoveryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 31000;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var eventRoot = new GameObject("RecoveryEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventRoot.transform.SetParent(transform, false);

        menuBar = Panel("RecoveryMenuBar", overlay.transform, new Color(0.025f, 0.07f, 0.10f, 0.92f));
        Anchor(menuBar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 19), new Vector2(0, 38));
        MakeButton("RecoveryLevels", menuBar.transform, "Levels", new Vector2(48, 0), new Vector2(88, 30), ShowCatalog, true);
        MakeButton("RecoveryHome", menuBar.transform, "Home", new Vector2(137, 0), new Vector2(78, 30), GoHome, true);
        menuStatus = MakeText("RecoveryMenuStatus", menuBar.transform, "", 12, TextAnchor.MiddleCenter);
        Anchor(menuStatus.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(310, 30));
        var musicButton = MakeButton("RecoveryMusic", menuBar.transform, "Music On", new Vector2(-139, 0), new Vector2(88, 30), ToggleMusic, false);
        musicLabel = musicButton.GetComponentInChildren<Text>();
        var soundButton = MakeButton("RecoverySound", menuBar.transform, "Sound On", new Vector2(-47, 0), new Vector2(88, 30), ToggleSound, false);
        soundLabel = soundButton.GetComponentInChildren<Text>();

        gameBar = Panel("RecoveryGameBar", overlay.transform, new Color(0.025f, 0.07f, 0.10f, 0.94f));
        Anchor(gameBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), new Vector2(0, 52));
        MakeButton("RecoveryBack", gameBar.transform, "Back", new Vector2(43, 0), new Vector2(76, 34), ShowLevelSelection, true);
        gameTitle = MakeText("RecoveryLevelTitle", gameBar.transform, "FLOW WATER", 17, TextAnchor.MiddleCenter);
        Anchor(gameTitle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 42));
        MakeButton("RecoveryNext", gameBar.transform, "Next", new Vector2(-43, 0), new Vector2(76, 34), Next, false);
        gameFooter = Panel("RecoveryGameFooter", overlay.transform, new Color(0.025f, 0.07f, 0.10f, 0.94f));
        Anchor(gameFooter, Vector2.zero, new Vector2(1, 0), new Vector2(0, 41), new Vector2(0, 82));
        gameStatus = MakeText("RecoveryGameStatus", gameFooter.transform, "", 13, TextAnchor.MiddleCenter);
        Anchor(gameStatus.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -16), new Vector2(-12, 28));
        MakeButton("RecoveryRestart", gameFooter.transform, "Restart [R]", new Vector2(-330, -17), new Vector2(118, 32), Restart);
        MakeButton("RecoveryHint", gameFooter.transform, "Hint [H]", new Vector2(-198, -17), new Vector2(118, 32), Hint);
        MakeButton("RecoveryUndo", gameFooter.transform, "Undo [Ctrl+Z]", new Vector2(-66, -17), new Vector2(118, 32), Undo);
        MakeButton("RecoveryRaise", gameFooter.transform, "Up", new Vector2(66, -17), new Vector2(118, 32), Raise);
        MakeButton("RecoveryLower", gameFooter.transform, "Down", new Vector2(198, -17), new Vector2(118, 32), Lower);
        MakeButton("RecoveryCatalog", gameFooter.transform, "Levels", new Vector2(330, -17), new Vector2(118, 32), ShowCatalog);
        BuildDialog();
        BuildCatalog();
        gameBar.SetActive(false); gameFooter.SetActive(false);
    }

    void BuildDialog()
    {
        dialog = Panel("RecoveryDialog", overlay.transform, new Color(0, 0, 0, 0.68f));
        Stretch(dialog);
        var body = Panel("RecoveryDialogBody", dialog.transform, new Color(0.07f, 0.15f, 0.20f));
        Anchor(body, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(580, 240));
        var text = MakeText("RecoveryDialogText", body.transform, "", 19, TextAnchor.MiddleCenter);
        Anchor(text.gameObject, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0, 25), new Vector2(520, 145));
        MakeButton("RecoveryDialogOK", body.transform, "OK", new Vector2(-75, -80), new Vector2(130, 38), () =>
        {
            var confirm = dialogConfirm; CloseDialog(); if (confirm != null) confirm();
        });
        MakeButton("RecoveryDialogCancel", body.transform, "Close", new Vector2(75, -80), new Vector2(130, 38), CloseDialog);
        dialog.SetActive(false);
    }

    public void Notice(string text) { Confirm(text, null); }
    void Confirm(string text, Action confirmed)
    {
        dialogConfirm = confirmed;
        dialog.GetComponentInChildren<Text>(true).text = text;
        dialog.SetActive(true);
        dialog.transform.SetAsLastSibling();
    }
    void CloseDialog() { dialog.SetActive(false); dialogConfirm = null; }

    void BuildCatalog()
    {
        catalog = Panel("RecoveryLevelCatalog", overlay.transform, new Color(0.035f, 0.10f, 0.14f, 0.99f));
        Stretch(catalog);
        catalogTitle = MakeText("RecoveryCatalogTitle", catalog.transform, "Recovered levels", 23, TextAnchor.MiddleCenter);
        Anchor(catalogTitle.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -43), new Vector2(700, 60));
        var subtext = MakeText("RecoveryCatalogHelp", catalog.transform, "Original gm asset IDs. Select any recovered level.", 15, TextAnchor.MiddleCenter);
        Anchor(subtext.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(700, 30));
        catalogButtons = new Button[50]; catalogLabels = new Text[50];
        for (int i = 0; i < 50; i++)
        {
            int slot = i;
            var button = MakeButton("RecoveryCatalogLevel" + i, catalog.transform, "", new Vector2(-324 + (i % 10) * 72, 143 - (i / 10) * 64), new Vector2(66, 51), () =>
            {
                int index = catalogPage * 50 + slot;
                if (index < levelIds.Count) StartGame(levelIds[index]);
            });
            catalogButtons[i] = button;
            catalogLabels[i] = button.GetComponentInChildren<Text>();
            catalogLabels[i].fontSize = 15;
        }
        MakeButton("RecoveryCatalogPrevious", catalog.transform, "Previous", new Vector2(-170, -235), new Vector2(140, 42), () => { catalogPage = Math.Max(0, catalogPage - 1); RefreshCatalog(); });
        MakeButton("RecoveryCatalogClose", catalog.transform, "Close", new Vector2(0, -235), new Vector2(140, 42), () => catalog.SetActive(false));
        MakeButton("RecoveryCatalogNext", catalog.transform, "Next", new Vector2(170, -235), new Vector2(140, 42), () => { catalogPage = Math.Min((levelIds.Count - 1) / 50, catalogPage + 1); RefreshCatalog(); });
        catalog.SetActive(false);
    }

    public void ShowCatalog()
    {
        RefreshCatalog(); catalog.SetActive(true); catalog.transform.SetAsLastSibling();
    }
    void RefreshCatalog()
    {
        catalogTitle.text = "RECOVERED LEVELS     " + (catalogPage + 1) + " / " + ((levelIds.Count + 49) / 50);
        for (int i = 0; i < catalogButtons.Length; i++)
        {
            int index = catalogPage * 50 + i;
            catalogButtons[i].gameObject.SetActive(index < levelIds.Count);
            if (index >= levelIds.Count) continue;
            int id = levelIds[index];
            catalogLabels[i].text = "gm" + id + (PlayerPrefs.GetInt(CompletePrefix + id, 0) != 0 ? "\nDone" : "");
        }
    }

    GameObject Panel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }
    Text MakeText(string name, Transform parent, string text, int size, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font; label.text = text; label.fontSize = size;
        label.alignment = alignment; label.color = new Color(0.91f, 0.97f, 0.98f);
        label.raycastTarget = false;
        return label;
    }
    Button MakeButton(string name, Transform parent, string title, Vector2 position, Vector2 size, Action action)
    {
        var go = Panel(name, parent, new Color(0.12f, 0.30f, 0.36f));
        Anchor(go, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.50f, 0.95f, 1); colors.pressedColor = new Color(0.25f, 0.68f, 0.75f);
        button.colors = colors;
        button.onClick.AddListener(() => action());
        var label = MakeText(name + "Label", go.transform, title, 14, TextAnchor.MiddleCenter);
        Stretch(label.gameObject);
        return button;
    }
    Button MakeButton(string name, Transform parent, string title, Vector2 position, Vector2 size, Action action, bool left)
    {
        var button = MakeButton(name, parent, title, position, size, action);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(left ? 0 : 1, 0.5f);
        return button;
    }
    static void Anchor(GameObject target, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)target.transform;
        rect.anchorMin = min; rect.anchorMax = max;
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static void Stretch(GameObject target)
    {
        var rect = (RectTransform)target.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}
