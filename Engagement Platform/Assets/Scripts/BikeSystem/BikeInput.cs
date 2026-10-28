using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// SCRIPT 1 — BikeInput
// Taak: tellen hoeveel OMWENTELINGEN er getrapt zijn.
// - Fiets: de Arduino stuurt pulsen. X pulsen = 1 omwenteling.
// - Nep-input: elke keer je op SPATIE duwt = 1 omwenteling.
//   Sneller op spatie duwen = sneller trappen.
// Leest de Arduino in de ACHTERGROND, zodat de game nooit bevriest.
// Komt het echte systeem van Technopolis? Dan vervang je ENKEL dit script.
public class BikeInput : MonoBehaviour
{
    [Header("Fiets (Arduino)")]
    [Tooltip("Uit = enkel nep-input met SPATIE")]
    public bool useBike = false;
    [Tooltip("Op Windows bv. COM5. Kijk in de Console welke poorten gevonden worden.")]
    public string portName = "COM5";
    public int baudRate = 9600;
    [Tooltip("MEET DIT met de echte fiets: draai 1 keer rond en tel de getallen in de Console.")]
    public int pulsesPerRevolution = 2;

    [Header("Live (alleen kijken)")]
    [SerializeField] private int totalRevolutions;
    [SerializeField] private bool bikeConnected;

    // Dit getal leest Cadence.
    public int TotalRevolutions => totalRevolutions;

    private SerialPort port;
    private Thread readThread;
    private volatile bool running;
    private int bikePulses;          // wordt verhoogd door de achtergrond-thread
    private int keyboardRevolutions;

    void Start()
    {
        if (!useBike)
        {
            Debug.Log("Nep-input: duw op SPATIE voor elke omwenteling.");
            return;
        }

        Debug.Log("Gevonden poorten: " + string.Join(", ", SerialPort.GetPortNames()));

        try
        {
            port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 500; // nooit langer dan een halve seconde wachten
            port.Open();

            running = true;
            readThread = new Thread(ReadLoop) { IsBackground = true };
            readThread.Start();
            bikeConnected = true;
            Debug.Log("Fiets verbonden op " + portName);
        }
        catch (Exception e)
        {
            // Geen fiets? Geen probleem: SPATIE werkt nog.
            Debug.LogWarning("Fiets niet gevonden op " + portName + " (" + e.Message + "). Gebruik SPATIE.");
            bikeConnected = false;
        }
    }

    // Draait in de achtergrond. Elke regel van de Arduino = 1 puls.
    private void ReadLoop()
    {
        while (running)
        {
            try
            {
                string line = port.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    Interlocked.Increment(ref bikePulses);
            }
            catch (TimeoutException)
            {
                // Niemand trapt. Gewoon opnieuw proberen.
            }
            catch (Exception)
            {
                // Kabel losgetrokken of andere fout: stoppen met lezen.
                running = false;
                bikeConnected = false;
            }
        }
    }

    void Update()
    {
        // Nep-input: 1 duw op spatie = 1 omwenteling
        if (SpacePressedThisFrame())
            keyboardRevolutions++;

        // Fiets: pulsen omrekenen naar hele omwentelingen
        int bikeRevolutions = Volatile.Read(ref bikePulses) / Mathf.Max(1, pulsesPerRevolution);

        totalRevolutions = bikeRevolutions + keyboardRevolutions;
    }

    private bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    void OnDestroy()
    {
        running = false;
        if (readThread != null && readThread.IsAlive) readThread.Join(1000);
        if (port != null && port.IsOpen) port.Close();
    }
}
