using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// SCRIPT 1 — BikeInput
// Taak: tellen hoeveel OMWENTELINGEN er getrapt zijn.
// - Fiets: Arduino of Technopolis sturen data over een COM-poort. X pulsen = 1 omwenteling.
// - Nep-input: elke keer je op SPATIE duwt = 1 omwenteling. Werkt altijd, ook met fiets (noodoplossing).
// Leest de poort in de ACHTERGROND, zodat de game nooit bevriest.
// Alles (poort, baudrate, pulsen per trap, modus) is ook in-game te wijzigen via BikeDebugPanel (F1)
// en wordt bewaard in PlayerPrefs, zodat het in de build.exe werkt zonder Inspector.
public class BikeInput : MonoBehaviour
{
    public enum InputMode
    {
        LinePerPulse,     // elke regel = 1 puls (Arduino-stijl)
        NumberIsCounter,  // elke regel bevat een oplopend totaal aantal pulsen
        NumberIsRpm       // elke regel bevat meteen de RPM
    }

    public static readonly int[] BaudRates = { 9600, 19200, 38400, 57600, 115200 };

    [Header("Fiets")]
    [Tooltip("Uit = enkel nep-input met SPATIE. Ook te wisselen in-game met F1.")]
    public bool useBike = false;
    [Tooltip("Op Windows bv. COM5. In-game kies je de poort met F1.")]
    public string portName = "COM5";
    public int baudRate = 9600;
    [Tooltip("Arduino: 12. Technopolis: vermoedelijk 24. Draai 1 keer rond en kijk hoeveel pulsen er binnenkomen.")]
    [Min(1)] public int pulsesPerRevolution = 24;
    public InputMode mode = InputMode.LinePerPulse;

    [Header("Instellingen")]
    [Tooltip("Aan = wat je in-game (F1) instelt wordt onthouden en overschrijft de waarden hierboven.")]
    public bool usePlayerPrefs = true;

    [Header("Live (alleen kijken)")]
    [SerializeField] private int totalRevolutions;
    [SerializeField] private bool bikeConnected;
    [SerializeField] private string status = "Niet verbonden";

    // Dit lezen Cadence, StartOnFirstPedal en het debugpaneel.
    public int TotalRevolutions => totalRevolutions;
    public bool BikeConnected => bikeConnected;
    public string Status => status;
    public int TotalPulses => Volatile.Read(ref bikePulses);
    public long TotalBytes => Interlocked.Read(ref bytesReceived);
    public int TotalLines => Volatile.Read(ref linesReceived);
    public float ExternalRpm { get { lock (rawLock) return externalRpm; } }
    public bool UsesExternalRpm => mode == InputMode.NumberIsRpm && bikeConnected;

    private SerialPort port;
    private Thread readThread;
    private volatile bool running;

    // Geschreven door de achtergrond-thread
    private int bikePulses;
    private long bytesReceived;
    private int linesReceived;
    private float externalRpm;
    private long lastCounterValue = -1;
    private readonly object rawLock = new object();
    private readonly Queue<string> rawLines = new Queue<string>();
    private const int RawLineCount = 10;

    // Omzetting pulsen -> omwentelingen (op de hoofdthread)
    private int processedPulses;
    private int pulseRemainder;
    private float rpmRevolutionAccumulator; // RPM-modus: omwentelingen opbouwen uit de RPM
    private int keyboardRevolutions;

    private static readonly Regex NumberPattern = new Regex(@"-?\d+(?:[.,]\d+)?", RegexOptions.Compiled);

    void Start()
    {
        if (usePlayerPrefs) LoadPrefs();
        processedPulses = TotalPulses;

        if (useBike) Connect(portName, baudRate);
        else Debug.Log("Nep-input: duw op SPATIE voor elke omwenteling.");
    }

    // ---------- Poort ----------

    public static string[] GetPorts()
    {
        try { return SerialPort.GetPortNames(); }
        catch (Exception) { return new string[0]; }
    }

    public bool Connect(string newPort, int newBaud)
    {
        Disconnect();
        portName = newPort;
        baudRate = newBaud;
        lastCounterValue = -1;

        if (string.IsNullOrEmpty(portName))
        {
            SetStatus("Geen poort gekozen");
            return false;
        }

        try
        {
            port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 500;  // nooit langer dan een halve seconde wachten
            port.DtrEnable = true;   // sommige borden (bv. Leonardo) sturen pas data als DTR aan staat
            port.Open();

            running = true;
            readThread = new Thread(ReadLoop) { IsBackground = true, Name = "BikeSerial" };
            readThread.Start();
            bikeConnected = true;
            SetStatus("Verbonden op " + portName + " @ " + baudRate);
            return true;
        }
        catch (Exception e)
        {
            // Geen fiets? Geen probleem: SPATIE werkt nog.
            bikeConnected = false;
            SetStatus("Fout op " + portName + ": " + e.Message);
            Debug.LogWarning("Fiets niet gevonden op " + portName + " (" + e.Message + "). Gebruik SPATIE.");
            return false;
        }
    }

    public void Disconnect()
    {
        running = false;
        if (readThread != null && readThread.IsAlive) readThread.Join(1000);
        readThread = null;
        try { if (port != null && port.IsOpen) port.Close(); } catch (Exception) { }
        port = null;
        if (bikeConnected) SetStatus("Niet verbonden");
        bikeConnected = false;
    }

    public void SetUseBike(bool on)
    {
        useBike = on;
        if (on) Connect(portName, baudRate);
        else { Disconnect(); SetStatus("Fiets uit (SPATIE)"); }
        SavePrefs();
    }

    // Draait in de achtergrond. Leest bytes en knipt ze zelf in regels op \n en \r,
    // zodat het werkt ongeacht welk regeleinde de software gebruikt.
    private void ReadLoop()
    {
        var buffer = new byte[256];
        var line = new StringBuilder();

        while (running)
        {
            try
            {
                int count = port.Read(buffer, 0, buffer.Length);
                Interlocked.Add(ref bytesReceived, count);

                for (int i = 0; i < count; i++)
                {
                    char c = (char)buffer[i];
                    if (c == '\n' || c == '\r')
                    {
                        if (line.Length > 0) HandleLine(line.ToString());
                        line.Length = 0;
                    }
                    else if (line.Length < 200)
                    {
                        line.Append(c);
                    }
                }
            }
            catch (TimeoutException)
            {
                // Niemand trapt. Gewoon opnieuw proberen.
            }
            catch (Exception e)
            {
                // Kabel losgetrokken of andere fout: stoppen met lezen.
                running = false;
                bikeConnected = false;
                status = "Verbinding verloren: " + e.Message;
            }
        }
    }

    private void HandleLine(string line)
    {
        Interlocked.Increment(ref linesReceived);
        lock (rawLock)
        {
            rawLines.Enqueue(line);
            while (rawLines.Count > RawLineCount) rawLines.Dequeue();
        }

        switch (mode)
        {
            case InputMode.LinePerPulse:
                if (!string.IsNullOrWhiteSpace(line)) Interlocked.Increment(ref bikePulses);
                break;

            case InputMode.NumberIsCounter:
                if (TryParseNumber(line, out double counter))
                {
                    long value = (long)counter;
                    // Eerste waarde of teller teruggezet: enkel onthouden, niets optellen.
                    if (lastCounterValue >= 0 && value > lastCounterValue)
                        Interlocked.Add(ref bikePulses, (int)Math.Min(value - lastCounterValue, 1000));
                    lastCounterValue = value;
                }
                break;

            case InputMode.NumberIsRpm:
                if (TryParseNumber(line, out double rpmValue))
                    lock (rawLock) externalRpm = Mathf.Max(0f, (float)rpmValue);
                break;
        }
    }

    private static bool TryParseNumber(string line, out double value)
    {
        value = 0;
        Match m = NumberPattern.Match(line);
        return m.Success && double.TryParse(m.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public string[] GetRawLines()
    {
        lock (rawLock) return rawLines.ToArray();
    }

    private void SetStatus(string s)
    {
        status = s;
    }

    // ---------- Omwentelingen ----------

    void Update()
    {
        // Nep-input: 1 duw op spatie = 1 omwenteling
        if (SpacePressedThisFrame())
            keyboardRevolutions++;

        int bikeRevolutionsThisFrame = 0;

        if (mode == InputMode.NumberIsRpm && bikeConnected)
        {
            // Geen pulsen: omwentelingen opbouwen uit de RPM, zodat StartOnFirstPedal blijft werken.
            rpmRevolutionAccumulator += ExternalRpm / 60f * Time.deltaTime;
            bikeRevolutionsThisFrame = Mathf.FloorToInt(rpmRevolutionAccumulator);
            rpmRevolutionAccumulator -= bikeRevolutionsThisFrame;
            processedPulses = TotalPulses;
        }
        else
        {
            // Enkel de NIEUWE pulsen omrekenen. Zo springt het totaal niet als je
            // pulsesPerRevolution tijdens het spelen verandert.
            int pulses = TotalPulses;
            pulseRemainder += pulses - processedPulses;
            processedPulses = pulses;

            int ppr = Mathf.Max(1, pulsesPerRevolution);
            bikeRevolutionsThisFrame = pulseRemainder / ppr;
            pulseRemainder -= bikeRevolutionsThisFrame * ppr;
        }

        totalRevolutions += bikeRevolutionsThisFrame;
        totalRevolutions += keyboardRevolutions;
        keyboardRevolutions = 0;
    }

    public void SetPulsesPerRevolution(int value)
    {
        pulsesPerRevolution = Mathf.Clamp(value, 1, 500);
        pulseRemainder = Mathf.Min(pulseRemainder, pulsesPerRevolution - 1);
        SavePrefs();
    }

    public void SetMode(InputMode newMode)
    {
        mode = newMode;
        lastCounterValue = -1;
        lock (rawLock) externalRpm = 0f;
        SavePrefs();
    }

    private bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // ---------- Bewaren ----------

    private const string P = "bike.";

    public void SavePrefs()
    {
        if (!usePlayerPrefs) return;
        PlayerPrefs.SetInt(P + "useBike", useBike ? 1 : 0);
        PlayerPrefs.SetString(P + "port", portName ?? "");
        PlayerPrefs.SetInt(P + "baud", baudRate);
        PlayerPrefs.SetInt(P + "ppr", pulsesPerRevolution);
        PlayerPrefs.SetInt(P + "mode", (int)mode);
        PlayerPrefs.Save();
    }

    private void LoadPrefs()
    {
        if (!PlayerPrefs.HasKey(P + "ppr")) return; // nog nooit iets ingesteld: Inspector-waarden gebruiken
        useBike = PlayerPrefs.GetInt(P + "useBike", useBike ? 1 : 0) == 1;
        portName = PlayerPrefs.GetString(P + "port", portName);
        baudRate = PlayerPrefs.GetInt(P + "baud", baudRate);
        pulsesPerRevolution = Mathf.Max(1, PlayerPrefs.GetInt(P + "ppr", pulsesPerRevolution));
        mode = (InputMode)PlayerPrefs.GetInt(P + "mode", (int)mode);
    }

    public static void ClearPrefs()
    {
        foreach (string key in new[] { "useBike", "port", "baud", "ppr", "mode" })
            PlayerPrefs.DeleteKey(P + key);
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }
}
