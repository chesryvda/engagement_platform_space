using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// BikeDebugPanel — in-game instellingen voor de usertest (werkt in de editor EN in de build.exe).
// Je hoeft het NIET in de scene te zetten: het maakt zichzelf aan (samen met FpsCounter).
// Getekend met OnGUI: in VR verschijnt het enkel op het desktopvenster, niet in de headset.
//   F1        paneel open/dicht
//   F5        rit herstarten (scene herladen), instellingen blijven bewaard
//   Esc 2x    game afsluiten
// Hier stel je in: fiets of SPATIE, COM-poort, baudrate, dataformaat, pulsen per trap,
// en de kalibratie traag trappen = minimumsnelheid / hard trappen = maximumsnelheid.
public class BikeDebugPanel : MonoBehaviour
{
    public BikeInput bikeInput;
    public Cadence cadence;
    public ShipSpeed shipSpeed;

    [Tooltip("Paneel open bij de start.")]
    public bool showOnStart = false;
    [Tooltip("Boven deze RPM klopt de omrekening bijna zeker niet (niemand trapt zo snel).")]
    public float suspiciousRpm = 180f;

    private bool visible;
    private string[] ports = new string[0];
    private Vector2 scroll;

    // Metingen per halve seconde
    private float sampleTimer;
    private int lastLines, lastPulses;
    private long lastBytes;
    private float linesPerSecond, pulsesPerSecond, bytesPerSecond;

    // "Meet 1 omwenteling"
    private bool measuring;
    private int measureStartPulses;

    // Afsluiten
    private float escPressedAt = -10f;

    private static readonly string[] ModeLabels = { "Regel = 1 puls", "Getal = teller", "Getal = RPM" };
    private const string T = "tune.";

    // Maakt zichzelf aan bij het starten van de game: niets in de scene nodig.
    // Het object overleeft een herstart (F5), en zoekt daarna de nieuwe BikeInput/Cadence/ShipSpeed op.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindFirstObjectByType<BikeDebugPanel>() != null) return;
        var go = new GameObject("DebugTools (F1 / F2)");
        go.AddComponent<BikeDebugPanel>();
        if (FindFirstObjectByType<FpsCounter>() == null) go.AddComponent<FpsCounter>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        visible = showOnStart;
        RefreshPorts();
    }

    // Na het laden of herstarten van de scene: de nieuwe componenten zoeken en de bewaarde kalibratie toepassen.
    private void FindReferences()
    {
        if (bikeInput != null) return;
        bikeInput = FindFirstObjectByType<BikeInput>();
        cadence = FindFirstObjectByType<Cadence>();
        shipSpeed = FindFirstObjectByType<ShipSpeed>();
        if (bikeInput != null) LoadTuning();
    }

    void Update()
    {
        FindReferences();

        if (Pressed(KeyId.F1))
        {
            visible = !visible;
            if (visible) RefreshPorts();
        }

        if (Pressed(KeyId.F5))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (Pressed(KeyId.Escape))
        {
            if (Time.unscaledTime - escPressedAt < 2f) Quit();
            else escPressedAt = Time.unscaledTime;
        }

        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= 0.5f && bikeInput != null)
        {
            int lines = bikeInput.TotalLines, pulses = bikeInput.TotalPulses;
            long bytes = bikeInput.TotalBytes;
            linesPerSecond = (lines - lastLines) / sampleTimer;
            pulsesPerSecond = (pulses - lastPulses) / sampleTimer;
            bytesPerSecond = (bytes - lastBytes) / sampleTimer;
            lastLines = lines; lastPulses = pulses; lastBytes = bytes;
            sampleTimer = 0f;
        }
    }

    void OnGUI()
    {
        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        if (Time.unscaledTime - escPressedAt < 2f)
            GUI.Box(new Rect(Screen.width / scale / 2f - 160f, 20f, 320f, 30f), "Nogmaals Esc om af te sluiten");

        if (!visible || bikeInput == null) return;

        float height = Screen.height / scale - 40f;
        GUILayout.BeginArea(new Rect(20f, 20f, 480f, height), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("<b>Fiets-instellingen</b>   (F1 sluit · F5 herstart rit · Esc 2x stop)", Rich());

        DrawConnection();
        DrawConversion();
        DrawCalibration();
        DrawLive();
        DrawRaw();
        DrawFooter();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------- Secties ----------

    private void DrawConnection()
    {
        Section("Verbinding");

        bool useBike = GUILayout.Toggle(bikeInput.useBike, " Fiets gebruiken (uit = enkel SPATIE)");
        if (useBike != bikeInput.useBike) bikeInput.SetUseBike(useBike);

        Color old = GUI.color;
        GUI.color = bikeInput.BikeConnected ? Color.green : new Color(1f, 0.5f, 0.4f);
        GUILayout.Label("Status: " + bikeInput.Status);
        GUI.color = old;

        GUILayout.BeginHorizontal();
        GUILayout.Label("COM-poort:", GUILayout.Width(90));
        if (ports.Length == 0) GUILayout.Label("(geen poorten gevonden)");
        foreach (string p in ports)
        {
            bool selected = p == bikeInput.portName;
            if (GUILayout.Toggle(selected, p, GUI.skin.button) && !selected)
                bikeInput.portName = p;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Vernieuwen", GUILayout.Width(90))) RefreshPorts();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Baudrate:", GUILayout.Width(90));
        int baudIndex = System.Array.IndexOf(BikeInput.BaudRates, bikeInput.baudRate);
        string[] baudLabels = System.Array.ConvertAll(BikeInput.BaudRates, b => b.ToString());
        int newBaud = GUILayout.SelectionGrid(baudIndex, baudLabels, baudLabels.Length);
        if (newBaud != baudIndex && newBaud >= 0) bikeInput.baudRate = BikeInput.BaudRates[newBaud];
        GUILayout.EndHorizontal();

        if (GUILayout.Button(bikeInput.BikeConnected ? "Opnieuw verbinden" : "Verbinden"))
        {
            bikeInput.useBike = true;
            bikeInput.Connect(bikeInput.portName, bikeInput.baudRate);
            bikeInput.SavePrefs();
        }
    }

    private void DrawConversion()
    {
        Section("Omrekening");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Dataformaat:", GUILayout.Width(90));
        int newMode = GUILayout.SelectionGrid((int)bikeInput.mode, ModeLabels, ModeLabels.Length);
        if (newMode != (int)bikeInput.mode) bikeInput.SetMode((BikeInput.InputMode)newMode);
        GUILayout.EndHorizontal();

        if (bikeInput.mode == BikeInput.InputMode.NumberIsRpm)
        {
            GUILayout.Label("RPM komt rechtstreeks uit de software: pulsen per trap doet niets.");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Pulsen per trap: <b>" + bikeInput.pulsesPerRevolution + "</b>", Rich(), GUILayout.Width(160));
        if (GUILayout.Button("−", GUILayout.Width(30))) bikeInput.SetPulsesPerRevolution(bikeInput.pulsesPerRevolution - 1);
        if (GUILayout.Button("+", GUILayout.Width(30))) bikeInput.SetPulsesPerRevolution(bikeInput.pulsesPerRevolution + 1);
        if (GUILayout.Button("12 (Arduino)")) bikeInput.SetPulsesPerRevolution(12);
        if (GUILayout.Button("24 (Technopolis)")) bikeInput.SetPulsesPerRevolution(24);
        GUILayout.EndHorizontal();
        GUILayout.Label("Hoger getal = trager schip, lager getal = sneller schip.");

        // Hulp: tel de pulsen van precies 1 omwenteling.
        GUILayout.BeginHorizontal();
        if (!measuring)
        {
            if (GUILayout.Button("Meet 1 omwenteling"))
            {
                measuring = true;
                measureStartPulses = bikeInput.TotalPulses;
            }
        }
        else
        {
            int counted = bikeInput.TotalPulses - measureStartPulses;
            GUILayout.Label("Draai de trapper precies 1 keer rond… <b>" + counted + "</b> pulsen", Rich());
            if (GUILayout.Button("Gebruik " + counted, GUILayout.Width(110)) && counted > 0)
            {
                bikeInput.SetPulsesPerRevolution(counted);
                measuring = false;
            }
            if (GUILayout.Button("Stop", GUILayout.Width(50))) measuring = false;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawCalibration()
    {
        if (shipSpeed == null || cadence == null) return;
        Section("Snelheid kalibreren");

        GUILayout.Label("Laat iemand rustig trappen → klik 'RPM = min'. Hard trappen → 'RPM = max'.");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Min-snelheid bij: <b>" + shipSpeed.rpmForMin.ToString("0") + "</b> RPM", Rich(), GUILayout.Width(190));
        if (GUILayout.Button("−5", GUILayout.Width(36))) SetRpmRange(shipSpeed.rpmForMin - 5f, shipSpeed.rpmForMax);
        if (GUILayout.Button("+5", GUILayout.Width(36))) SetRpmRange(shipSpeed.rpmForMin + 5f, shipSpeed.rpmForMax);
        if (GUILayout.Button("RPM nu = min")) SetRpmRange(cadence.RPM, shipSpeed.rpmForMax);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Max-snelheid bij: <b>" + shipSpeed.rpmForMax.ToString("0") + "</b> RPM", Rich(), GUILayout.Width(190));
        if (GUILayout.Button("−5", GUILayout.Width(36))) SetRpmRange(shipSpeed.rpmForMin, shipSpeed.rpmForMax - 5f);
        if (GUILayout.Button("+5", GUILayout.Width(36))) SetRpmRange(shipSpeed.rpmForMin, shipSpeed.rpmForMax + 5f);
        if (GUILayout.Button("RPM nu = max")) SetRpmRange(shipSpeed.rpmForMin, cadence.RPM);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Optrekken: <b>" + shipSpeed.acceleration.ToString("0.#") + "</b> /s", Rich(), GUILayout.Width(190));
        if (GUILayout.Button("−", GUILayout.Width(36))) SetAcceleration(shipSpeed.acceleration - 1f);
        if (GUILayout.Button("+", GUILayout.Width(36))) SetAcceleration(shipSpeed.acceleration + 1f);
        GUILayout.Label("(kleiner = zachter)");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("SPATIE per trap: <b>" + cadence.keyboardPressesPerRevolution.ToString("0.#") + "</b>", Rich(), GUILayout.Width(190));
        if (GUILayout.Button("−", GUILayout.Width(36))) SetKeyboardPresses(cadence.keyboardPressesPerRevolution - 0.5f);
        if (GUILayout.Button("+", GUILayout.Width(36))) SetKeyboardPresses(cadence.keyboardPressesPerRevolution + 0.5f);
        GUILayout.EndHorizontal();
    }

    private void DrawLive()
    {
        Section("Live");

        GUILayout.Label(string.Format("Bytes/s: {0:0}   Regels/s: {1:0.0}   Pulsen/s: {2:0.0}",
            bytesPerSecond, linesPerSecond, pulsesPerSecond));
        GUILayout.Label(string.Format("Pulsen totaal: {0}   Omwentelingen: {1}",
            bikeInput.TotalPulses, bikeInput.TotalRevolutions));

        if (cadence != null)
        {
            float rpm = cadence.RPM;
            GUILayout.Label("RPM: <b>" + rpm.ToString("0") + "</b>", Rich());
            if (rpm > suspiciousRpm)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 0.5f, 0.4f);
                GUILayout.Label("RPM onrealistisch hoog: pulsen per trap staat waarschijnlijk te LAAG.");
                GUI.color = old;
            }
        }

        if (shipSpeed != null)
        {
            GUILayout.Label(string.Format("Snelheid: {0:0.0} (doel {1:0.0})   p = {2:0.00}",
                shipSpeed.CurrentSpeed, shipSpeed.TargetSpeed, shipSpeed.P));
            Rect bar = GUILayoutUtility.GetRect(400f, 14f, GUILayout.ExpandWidth(true));
            GUI.Box(bar, GUIContent.none);
            GUI.Box(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(shipSpeed.P), bar.height), GUIContent.none);
            GUILayout.Label("min  ←  p  →  max");
        }

        if (bikeInput.BikeConnected && bytesPerSecond > 0f && linesPerSecond <= 0f)
            GUILayout.Label("Wel bytes maar geen regels: probeer een andere baudrate.");
    }

    private void DrawRaw()
    {
        Section("Ruwe data (laatste regels)");
        string[] lines = bikeInput.GetRawLines();
        GUILayout.TextArea(lines.Length == 0 ? "(nog niets ontvangen)" : string.Join("\n", lines), GUILayout.MinHeight(120));
    }

    private void DrawFooter()
    {
        Section("Overig");
        if (GUILayout.Button("Instellingen terugzetten naar Unity-standaard (daarna F5)"))
        {
            BikeInput.ClearPrefs();
            foreach (string key in new[] { "rpmMin", "rpmMax", "accel", "keyPresses" })
                PlayerPrefs.DeleteKey(T + key);
            PlayerPrefs.Save();
        }
        GUILayout.Label("Logbestand: " + Application.persistentDataPath + "/Player.log");
    }

    // ---------- Tuning (ShipSpeed / Cadence) ----------

    private void SetRpmRange(float min, float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(min + 5f, max); // max moet altijd boven min liggen
        shipSpeed.rpmForMin = Mathf.Round(min);
        shipSpeed.rpmForMax = Mathf.Round(max);
        SaveTuning();
    }

    private void SetAcceleration(float value)
    {
        shipSpeed.acceleration = Mathf.Max(0.5f, value);
        SaveTuning();
    }

    private void SetKeyboardPresses(float value)
    {
        cadence.keyboardPressesPerRevolution = Mathf.Max(1f, value);
        SaveTuning();
    }

    private void SaveTuning()
    {
        if (shipSpeed != null)
        {
            PlayerPrefs.SetFloat(T + "rpmMin", shipSpeed.rpmForMin);
            PlayerPrefs.SetFloat(T + "rpmMax", shipSpeed.rpmForMax);
            PlayerPrefs.SetFloat(T + "accel", shipSpeed.acceleration);
        }
        if (cadence != null)
            PlayerPrefs.SetFloat(T + "keyPresses", cadence.keyboardPressesPerRevolution);
        PlayerPrefs.Save();
    }

    private void LoadTuning()
    {
        if (shipSpeed != null && PlayerPrefs.HasKey(T + "rpmMin"))
        {
            shipSpeed.rpmForMin = PlayerPrefs.GetFloat(T + "rpmMin", shipSpeed.rpmForMin);
            shipSpeed.rpmForMax = PlayerPrefs.GetFloat(T + "rpmMax", shipSpeed.rpmForMax);
            shipSpeed.acceleration = PlayerPrefs.GetFloat(T + "accel", shipSpeed.acceleration);
        }
        if (cadence != null && PlayerPrefs.HasKey(T + "keyPresses"))
            cadence.keyboardPressesPerRevolution = PlayerPrefs.GetFloat(T + "keyPresses", cadence.keyboardPressesPerRevolution);
    }

    // ---------- Hulpjes ----------

    private void RefreshPorts()
    {
        ports = BikeInput.GetPorts();
        System.Array.Sort(ports);
    }

    private static void Section(string title)
    {
        GUILayout.Space(8f);
        GUILayout.Label("<b>" + title + "</b>", Rich());
    }

    private static GUIStyle richStyle;
    private static GUIStyle Rich()
    {
        if (richStyle == null) richStyle = new GUIStyle(GUI.skin.label) { richText = true };
        return richStyle;
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private enum KeyId { F1, F5, Escape }

    private static bool Pressed(KeyId key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        switch (key)
        {
            case KeyId.F1: return k.f1Key.wasPressedThisFrame;
            case KeyId.F5: return k.f5Key.wasPressedThisFrame;
            default: return k.escapeKey.wasPressedThisFrame;
        }
#else
        switch (key)
        {
            case KeyId.F1: return Input.GetKeyDown(KeyCode.F1);
            case KeyId.F5: return Input.GetKeyDown(KeyCode.F5);
            default: return Input.GetKeyDown(KeyCode.Escape);
        }
#endif
    }
}
