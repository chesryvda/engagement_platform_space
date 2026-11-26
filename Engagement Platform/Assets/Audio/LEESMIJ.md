# Audio – Reis naar de maan (VR)

Alle geluiden in deze map zijn zelf gegenereerd met audiosynthese (geen opnames van derden). Ze zijn dus **volledig rechtenvrij** en mogen gebruikt worden in de publieke installatie bij Technopolis.

Formaat: WAV, 48 kHz, 16-bit PCM. Unity importeert ze automatisch.

## Unity-instellingen (aanbevolen)

- **Mono-bestanden** zijn bedoeld voor objecten in de wereld (satelliet, afval, asteroïden, Tesla, rompgeluiden): AudioSource → *Spatial Blend = 1 (3D)*. Voor flybys: *Doppler Level* ±1.
- **Stereo-bestanden** (muziek, ambience, UI, Lisa, overgangen): *Spatial Blend = 0 (2D)*.
- Bestanden met `_loop` in de naam: zet *Loop* aan op de AudioSource. Ze zijn naadloos gemaakt.
- Import Settings: muziek en lange loops (>10s) → *Load Type: Streaming*, *Vorbis* kwaliteit 70. Korte effecten → *Decompress On Load*.
- Motor: speel `A_EngineLoopLow`, `A_EngineLoopHigh` en `A_EngineRumbleSubLoop` tegelijk. Koppel trapsnelheid (0–1) aan pitch (0.8 → 1.3) en meng low → high. Bij max snelheid: `A_EngineLimiterLoop` zachtjes erbij.
- Herhaalde geluiden (tikjes, afval, asteroïden): kies willekeurig een variant en randomiseer pitch (0.9–1.1) en volume (0.8–1.0).
- Maak in een Audio Mixer groepen *Lisa*, *Muziek*, *SFX*, *Ambience*, *UI*. Laat Muziek en Ambience 6–10 dB zakken wanneer Lisa praat (ducking).

## Overzicht per map

### 00_General/Cockpit

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_CockpitLifesupportLoop.wav` | 20.0s | stereo | ja | Luchtcirculatie/ventilatie, basislaag cockpit. Loop. |
| `A_CockpitDashboardHumLoop.wav` | 10.0s | stereo | ja | Elektrisch zoemen van dashboard en schermen. Loop. |
| `A_CockpitDetailTick01.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `A_CockpitDetailTick02.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `A_CockpitDetailTick03.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `A_CockpitRelayClick01.wav` | 0.6s | mono |  | Relais dat klikt. |
| `A_CockpitRelayClick02.wav` | 0.6s | mono |  | Relais dat klikt. |
| `A_CockpitHullCreak01.wav` | 2.3s | mono |  | Metalen kraakje van de romp. |
| `A_CockpitHullCreak02.wav` | 2.7s | mono |  | Metalen kraakje van de romp. |

### 00_General/Engine

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_EngineLoopLow.wav` | 6.0s | mono | ja | Motor laag toerental. Pitch 0.8-1.3 en volume koppelen aan trapsnelheid. Crossfaden met A_EngineLoopHigh. |
| `A_EngineLoopHigh.wav` | 6.0s | mono | ja | Motor hoog toerental (met turbine-gierende toon). Inmengen bij hogere snelheid. |
| `A_EngineRumbleSubLoop.wav` | 6.0s | mono | ja | Sub-bas gerommel. Volume omhoog bij hoge snelheid (voelbaar op de fiets). |
| `A_EngineLimiterLoop.wav` | 6.0s | mono | ja | Stabiele zoemtoon bij maximumsnelheid (begrenzer). |
| `A_EngineStartup.wav` | 3.9s | mono |  | Motor start op wanneer de bezoeker begint te trappen. |
| `A_EngineSpooldown.wav` | 3.2s | mono |  | Motor valt stil wanneer de bezoeker stopt met trappen. |

### 00_General/Space

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_SpaceDroneLoop.wav` | 30.0s | stereo | ja | Diepe ruimte-drone, de 'stilte' van de ruimte. Loopt de hele ervaring. |

### 00_General/UI

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_UIDashboardBoot.wav` | 5.0s | stereo |  | Opstartsequentie van het dashboard (scene 1). |
| `A_UILisaWavesAppear.wav` | 2.4s | stereo |  | Digitale 'bloei' wanneer Lisa's audiogolven verschijnen. |
| `A_UIZoneEnter.wav` | 2.5s | stereo |  | Interactiezone start: snelheid wordt overgenomen. |
| `A_UIZoneExit.wav` | 2.5s | stereo |  | Controle terug naar de bezoeker: jij mag weer trappen. |
| `A_UISpeedTick.wav` | 0.0s | mono |  | Tik bij oplopende snelheidsmeter. |
| `A_UIRadarPing.wav` | 3.8s | stereo |  | Radarping: object gedetecteerd (satelliet, Tesla, asteroiden). |
| `A_LisaRadioOn.wav` | 0.2s | mono |  | Klikje + ruis voor Lisa begint te praten. |
| `A_LisaRadioOff.wav` | 0.3s | mono |  | Afsluitklik na Lisa's zin. |

### 00_General/Transitions

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_TransitionWhoosh01.wav` | 2.0s | stereo |  | Whoosh bij scenewissel. |
| `A_TransitionWhoosh02.wav` | 2.5s | stereo |  | Whoosh bij scenewissel. |
| `A_TransitionSwell.wav` | 3.5s | stereo |  | Opzwellende overgang (omgekeerde galm) naar een nieuwe scene. |

### Scene1_AboveEarth

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S1WakeupChime.wav` | 4.5s | stereo |  | Zacht wekgeluid: Lisa maakt de bezoeker wakker. |
| `A_S1SystemWake.wav` | 3.5s | stereo |  | Het schip 'ontwaakt': systemen schakelen aan, lucht begint te stromen. |
| `A_S1MusicCalmLoop.wav` | 32.0s | stereo | ja | Rustige, warme ambient muziek boven de aarde. Loop (32s). |
| `A_S1MissionControlRadioLoop.wav` | 12.0s | stereo | ja | Zwak, onverstaanbaar radioverkeer/ruis 'van de aarde'. Heel stil onder de mix leggen. Loop. |
| `A_S1EngineIdleLoop.wav` | 4.0s | mono | ja | Stationair pruttelende motor wanneer de bezoeker stilstaat (aanmoediging om te trappen). Loop. |

### Scene2_Satellite

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S2SatelliteBeepLoop.wav` | 4.0s | mono | ja | Het klassieke 'bliep... bliep' (knipoog naar Spoetnik). Mono: op de satelliet zetten met 3D spatial blend. Loop. |
| `A_S2SatelliteHumLoop.wav` | 5.0s | mono | ja | Elektrisch gezoem van de satelliet als je dichtbij bent. Mono, 3D. Loop. |
| `A_S2DataCrackleLoop.wav` | 6.0s | mono | ja | Digitaal datageknetter van de satelliet. Mono, 3D. Loop. |
| `A_S2MusicWonderLoop.wav` | 32.0s | stereo | ja | Rustige, nieuwsgierige muziek bij de satelliet. Loop (32s). |

### Scene3_SpaceDebris

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S3WarningAlarm.wav` | 3.6s | stereo |  | Waarschuwing bij het binnenvliegen van de afvalwolk. |
| `A_S3DebrisTick01.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisTick02.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisTick03.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisTick04.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisTick05.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisTick06.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `A_S3DebrisClankHeavy01.wav` | 4.3s | mono |  | Zwaarder paneel botst tegen het schip: schrikmoment (1-2 keer gebruiken). |
| `A_S3DebrisClankHeavy02.wav` | 4.3s | mono |  | Zwaarder paneel botst tegen het schip: schrikmoment (1-2 keer gebruiken). |
| `A_S3DebrisWhoosh01.wav` | 1.0s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `A_S3DebrisWhoosh02.wav` | 0.9s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `A_S3DebrisWhoosh03.wav` | 1.2s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `A_S3PanelCreakLoop.wav` | 6.0s | mono | ja | Metalen kraakgeluid van een rondtollend paneel. Mono, 3D. Loop. |
| `A_S3MusicTenseLoop.wav` | 16.0s | stereo | ja | Gespannen muziek met pulserende bas en tikkend ritme (120 BPM). Loop (16s). |

### Scene4_Tesla

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S4RadioTuneIn.wav` | 3.0s | mono |  | Radio wordt 'afgestemd' bij het naderen van de Tesla; gaat over in A_S4CarRadioMusicLoop. |
| `A_S4CarRadioMusicLoop.wav` | 16.0s | mono | ja | Eigen retro-synthwave nummer door een autoradio-filter, alsof het uit de Tesla komt. Mono: op de Tesla zetten (3D). Rechtenvrij. Loop (16s). |
| `A_S4RetroMusicCleanLoop.wav` | 16.0s | stereo | ja | Hetzelfde nummer zonder radiofilter (als je het als gewone soundtrack wil gebruiken). Loop. |
| `A_S4RotationWhooshLoop.wav` | 8.0s | mono | ja | Trage zwiep die bij het ronddraaien van de auto hoort. Mono, 3D. Loop. |
| `A_S4HornRadio.wav` | 1.3s | mono |  | Komische, gedempte claxon ('toet-toeet') als knipoog. |

### Scene5_Asteroids

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S5AsteroidFlybyLarge01.wav` | 4.5s | mono |  | Grote asteroide raast voorbij: laag, traag, rommelend. Mono, 3D (Doppler aan op de AudioSource). |
| `A_S5AsteroidFlybyLarge02.wav` | 4.5s | mono |  | Grote asteroide raast voorbij: laag, traag, rommelend. Mono, 3D (Doppler aan op de AudioSource). |
| `A_S5AsteroidFlybySmall01.wav` | 1.1s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `A_S5AsteroidFlybySmall02.wav` | 1.3s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `A_S5AsteroidFlybySmall03.wav` | 1.0s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `A_S5HullGroan01.wav` | 4.2s | mono |  | Romp kreunt onder de spanning. |
| `A_S5HullGroan02.wav` | 4.2s | mono |  | Romp kreunt onder de spanning. |
| `A_S5RadarPingFastLoop.wav` | 2.0s | stereo | ja | Snelle radarpings = gevaar. Loop (pitch iets omhoog voor nog meer spanning). |
| `A_S5RockScrape.wav` | 2.6s | mono |  | Klein steentje schraapt langs het schip ('crunch'). |
| `A_S5MusicClimaxLoop.wav` | 16.0s | stereo | ja | Opbouwende, spannende muziek met tromgeroffel (toms) en strijkersachtige synths. Loop (16s). |

### Scene6_MoonArrival

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `A_S6Riser.wav` | 8.0s | stereo |  | Stijgende 'riser' terwijl het schip versnelt naar de maan (8s, eindigt abrupt op de piek: start A_S6MoonBoom op het einde). |
| `A_S6SpeedometerAccel.wav` | 6.7s | mono |  | Snelheidsmeter-tikjes die steeds sneller gaan (6s). |
| `A_S6MoonBoom.wav` | 10.2s | stereo |  | Diepe, warme 'boem' + akkoord wanneer de maan het beeld vult. |
| `A_S6WhiteoutShimmer.wav` | 7.0s | stereo |  | Heldere, luchtige shimmer die met de fade naar wit oplost. |
| `A_S6SonicLogo.wav` | 5.0s | stereo |  | Rustig 'sonic logo' bij het verschijnen van het Technopolis-logo. |
| `A_S6MusicFinale.wav` | 21.5s | stereo |  | Episch einde: muziek bouwt op en lost op in een groot C-akkoord (~21s, geen loop). Starten rond 1:43. |

## Wat ontbreekt / later te vervangen

- **Lisa's stem**: neem die zelf op (of laat inspreken). Zet er een licht radiofilter op (bandpass 300–3500 Hz) en gebruik `A_LisaRadioOn` / `A_LisaRadioOff` ervoor en erna.
- Wil je echte opnames in plaats van gesynthetiseerde geluiden? Goede, (meestal) vrij bruikbare bronnen (controleer per bestand de licentie):
  - NASA – ruimtegeluiden en sonificaties: https://www.nasa.gov/audio-and-ringtones/ en https://soundcloud.com/nasa
  - Sonniss GDC Game Audio Bundle (gratis, royalty-free): https://sonniss.com/gameaudiogdc
  - Freesound (filter op CC0): https://freesound.org
  - Kenney (CC0 sci-fi/UI geluiden): https://kenney.nl/assets/category:Audio
