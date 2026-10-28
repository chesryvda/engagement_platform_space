# Audio – Reis naar de maan (VR)

Alle geluiden in deze map zijn zelf gegenereerd met audiosynthese (geen opnames van derden). Ze zijn dus **volledig rechtenvrij** en mogen gebruikt worden in de publieke installatie bij Technopolis.

Formaat: WAV, 48 kHz, 16-bit PCM. Unity importeert ze automatisch.

## Unity-instellingen (aanbevolen)

- **Mono-bestanden** zijn bedoeld voor objecten in de wereld (satelliet, afval, asteroïden, Tesla, rompgeluiden): AudioSource → *Spatial Blend = 1 (3D)*. Voor flybys: *Doppler Level* ±1.
- **Stereo-bestanden** (muziek, ambience, UI, Lisa, overgangen): *Spatial Blend = 0 (2D)*.
- Bestanden met `_loop` in de naam: zet *Loop* aan op de AudioSource. Ze zijn naadloos gemaakt.
- Import Settings: muziek en lange loops (>10s) → *Load Type: Streaming*, *Vorbis* kwaliteit 70. Korte effecten → *Decompress On Load*.
- Motor: speel `engine_loop_low`, `engine_loop_high` en `engine_rumble_sub_loop` tegelijk. Koppel trapsnelheid (0–1) aan pitch (0.8 → 1.3) en meng low → high. Bij max snelheid: `engine_limiter_loop` zachtjes erbij.
- Herhaalde geluiden (tikjes, afval, asteroïden): kies willekeurig een variant en randomiseer pitch (0.9–1.1) en volume (0.8–1.0).
- Maak in een Audio Mixer groepen *Lisa*, *Muziek*, *SFX*, *Ambience*, *UI*. Laat Muziek en Ambience 6–10 dB zakken wanneer Lisa praat (ducking).

## Overzicht per map

### 00_Algemeen/Cockpit

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `cockpit_lifesupport_loop.wav` | 20.0s | stereo | ja | Luchtcirculatie/ventilatie, basislaag cockpit. Loop. |
| `cockpit_dashboard_hum_loop.wav` | 10.0s | stereo | ja | Elektrisch zoemen van dashboard en schermen. Loop. |
| `cockpit_detail_tick_01.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `cockpit_detail_tick_02.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `cockpit_detail_tick_03.wav` | 0.8s | mono |  | Klein tikje in een leiding. Willekeurig afspelen met random pitch. |
| `cockpit_relay_click_01.wav` | 0.6s | mono |  | Relais dat klikt. |
| `cockpit_relay_click_02.wav` | 0.6s | mono |  | Relais dat klikt. |
| `cockpit_hull_creak_01.wav` | 2.3s | mono |  | Metalen kraakje van de romp. |
| `cockpit_hull_creak_02.wav` | 2.7s | mono |  | Metalen kraakje van de romp. |

### 00_Algemeen/Motor

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `engine_loop_low.wav` | 6.0s | mono | ja | Motor laag toerental. Pitch 0.8-1.3 en volume koppelen aan trapsnelheid. Crossfaden met engine_loop_high. |
| `engine_loop_high.wav` | 6.0s | mono | ja | Motor hoog toerental (met turbine-gierende toon). Inmengen bij hogere snelheid. |
| `engine_rumble_sub_loop.wav` | 6.0s | mono | ja | Sub-bas gerommel. Volume omhoog bij hoge snelheid (voelbaar op de fiets). |
| `engine_limiter_loop.wav` | 6.0s | mono | ja | Stabiele zoemtoon bij maximumsnelheid (begrenzer). |
| `engine_startup.wav` | 3.9s | mono |  | Motor start op wanneer de bezoeker begint te trappen. |
| `engine_spooldown.wav` | 3.2s | mono |  | Motor valt stil wanneer de bezoeker stopt met trappen. |

### 00_Algemeen/Ruimte

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `space_drone_loop.wav` | 30.0s | stereo | ja | Diepe ruimte-drone, de 'stilte' van de ruimte. Loopt de hele ervaring. |

### 00_Algemeen/UI

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `ui_dashboard_boot.wav` | 5.0s | stereo |  | Opstartsequentie van het dashboard (scene 1). |
| `ui_lisa_waves_appear.wav` | 2.4s | stereo |  | Digitale 'bloei' wanneer Lisa's audiogolven verschijnen. |
| `ui_zone_enter.wav` | 2.5s | stereo |  | Interactiezone start: snelheid wordt overgenomen. |
| `ui_zone_exit.wav` | 2.5s | stereo |  | Controle terug naar de bezoeker: jij mag weer trappen. |
| `ui_speed_tick.wav` | 0.0s | mono |  | Tik bij oplopende snelheidsmeter. |
| `ui_radar_ping.wav` | 3.8s | stereo |  | Radarping: object gedetecteerd (satelliet, Tesla, asteroiden). |
| `lisa_radio_on.wav` | 0.2s | mono |  | Klikje + ruis voor Lisa begint te praten. |
| `lisa_radio_off.wav` | 0.3s | mono |  | Afsluitklik na Lisa's zin. |

### 00_Algemeen/Overgangen

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `transition_whoosh_01.wav` | 2.0s | stereo |  | Whoosh bij scenewissel. |
| `transition_whoosh_02.wav` | 2.5s | stereo |  | Whoosh bij scenewissel. |
| `transition_swell.wav` | 3.5s | stereo |  | Opzwellende overgang (omgekeerde galm) naar een nieuwe scene. |

### Scene1_BovenDeAarde

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s1_wakeup_chime.wav` | 4.5s | stereo |  | Zacht wekgeluid: Lisa maakt de bezoeker wakker. |
| `s1_system_wake.wav` | 3.5s | stereo |  | Het schip 'ontwaakt': systemen schakelen aan, lucht begint te stromen. |
| `s1_music_calm_loop.wav` | 32.0s | stereo | ja | Rustige, warme ambient muziek boven de aarde. Loop (32s). |
| `s1_mission_control_radio_loop.wav` | 12.0s | stereo | ja | Zwak, onverstaanbaar radioverkeer/ruis 'van de aarde'. Heel stil onder de mix leggen. Loop. |
| `s1_engine_idle_loop.wav` | 4.0s | mono | ja | Stationair pruttelende motor wanneer de bezoeker stilstaat (aanmoediging om te trappen). Loop. |

### Scene2_Satelliet

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s2_satellite_beep_loop.wav` | 4.0s | mono | ja | Het klassieke 'bliep... bliep' (knipoog naar Spoetnik). Mono: op de satelliet zetten met 3D spatial blend. Loop. |
| `s2_satellite_hum_loop.wav` | 5.0s | mono | ja | Elektrisch gezoem van de satelliet als je dichtbij bent. Mono, 3D. Loop. |
| `s2_data_crackle_loop.wav` | 6.0s | mono | ja | Digitaal datageknetter van de satelliet. Mono, 3D. Loop. |
| `s2_music_wonder_loop.wav` | 32.0s | stereo | ja | Rustige, nieuwsgierige muziek bij de satelliet. Loop (32s). |

### Scene3_Ruimteafval

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s3_warning_alarm.wav` | 3.6s | stereo |  | Waarschuwing bij het binnenvliegen van de afvalwolk. |
| `s3_debris_tick_01.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_tick_02.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_tick_03.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_tick_04.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_tick_05.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_tick_06.wav` | 1.2s | mono |  | Schroefje/klein stukje afval tikt tegen de romp. Randomiseer variant, pitch (0.9-1.1) en volume. |
| `s3_debris_clank_heavy_01.wav` | 4.3s | mono |  | Zwaarder paneel botst tegen het schip: schrikmoment (1-2 keer gebruiken). |
| `s3_debris_clank_heavy_02.wav` | 4.3s | mono |  | Zwaarder paneel botst tegen het schip: schrikmoment (1-2 keer gebruiken). |
| `s3_debris_whoosh_01.wav` | 1.0s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `s3_debris_whoosh_02.wav` | 0.9s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `s3_debris_whoosh_03.wav` | 1.2s | mono |  | Brokstuk zweeft vlak langs je hoofd. Mono, 3D op het brokstuk. |
| `s3_panel_creak_loop.wav` | 6.0s | mono | ja | Metalen kraakgeluid van een rondtollend paneel. Mono, 3D. Loop. |
| `s3_music_tense_loop.wav` | 16.0s | stereo | ja | Gespannen muziek met pulserende bas en tikkend ritme (120 BPM). Loop (16s). |

### Scene4_Tesla

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s4_radio_tune_in.wav` | 3.0s | mono |  | Radio wordt 'afgestemd' bij het naderen van de Tesla; gaat over in s4_car_radio_music_loop. |
| `s4_car_radio_music_loop.wav` | 16.0s | mono | ja | Eigen retro-synthwave nummer door een autoradio-filter, alsof het uit de Tesla komt. Mono: op de Tesla zetten (3D). Rechtenvrij. Loop (16s). |
| `s4_retro_music_clean_loop.wav` | 16.0s | stereo | ja | Hetzelfde nummer zonder radiofilter (als je het als gewone soundtrack wil gebruiken). Loop. |
| `s4_rotation_whoosh_loop.wav` | 8.0s | mono | ja | Trage zwiep die bij het ronddraaien van de auto hoort. Mono, 3D. Loop. |
| `s4_horn_radio.wav` | 1.3s | mono |  | Komische, gedempte claxon ('toet-toeet') als knipoog. |

### Scene5_Asteroiden

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s5_asteroid_flyby_large_01.wav` | 4.5s | mono |  | Grote asteroide raast voorbij: laag, traag, rommelend. Mono, 3D (Doppler aan op de AudioSource). |
| `s5_asteroid_flyby_large_02.wav` | 4.5s | mono |  | Grote asteroide raast voorbij: laag, traag, rommelend. Mono, 3D (Doppler aan op de AudioSource). |
| `s5_asteroid_flyby_small_01.wav` | 1.1s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `s5_asteroid_flyby_small_02.wav` | 1.3s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `s5_asteroid_flyby_small_03.wav` | 1.0s | mono |  | Kleine asteroide vliegt snel voorbij: hoger en sneller. Mono, 3D. |
| `s5_hull_groan_01.wav` | 4.2s | mono |  | Romp kreunt onder de spanning. |
| `s5_hull_groan_02.wav` | 4.2s | mono |  | Romp kreunt onder de spanning. |
| `s5_radar_ping_fast_loop.wav` | 2.0s | stereo | ja | Snelle radarpings = gevaar. Loop (pitch iets omhoog voor nog meer spanning). |
| `s5_rock_scrape.wav` | 2.6s | mono |  | Klein steentje schraapt langs het schip ('crunch'). |
| `s5_music_climax_loop.wav` | 16.0s | stereo | ja | Opbouwende, spannende muziek met tromgeroffel (toms) en strijkersachtige synths. Loop (16s). |

### Scene6_AankomstMaan

| Bestand | Duur | Kanalen | Loop | Gebruik |
|---|---|---|---|---|
| `s6_riser.wav` | 8.0s | stereo |  | Stijgende 'riser' terwijl het schip versnelt naar de maan (8s, eindigt abrupt op de piek: start s6_moon_boom op het einde). |
| `s6_speedometer_accel.wav` | 6.7s | mono |  | Snelheidsmeter-tikjes die steeds sneller gaan (6s). |
| `s6_moon_boom.wav` | 10.2s | stereo |  | Diepe, warme 'boem' + akkoord wanneer de maan het beeld vult. |
| `s6_whiteout_shimmer.wav` | 7.0s | stereo |  | Heldere, luchtige shimmer die met de fade naar wit oplost. |
| `s6_sonic_logo.wav` | 5.0s | stereo |  | Rustig 'sonic logo' bij het verschijnen van het Technopolis-logo. |
| `s6_music_finale.wav` | 21.5s | stereo |  | Episch einde: muziek bouwt op en lost op in een groot C-akkoord (~21s, geen loop). Starten rond 1:43. |

## Wat ontbreekt / later te vervangen

- **Lisa's stem**: neem die zelf op (of laat inspreken). Zet er een licht radiofilter op (bandpass 300–3500 Hz) en gebruik `lisa_radio_on` / `lisa_radio_off` ervoor en erna.
- Wil je echte opnames in plaats van gesynthetiseerde geluiden? Goede, (meestal) vrij bruikbare bronnen (controleer per bestand de licentie):
  - NASA – ruimtegeluiden en sonificaties: https://www.nasa.gov/audio-and-ringtones/ en https://soundcloud.com/nasa
  - Sonniss GDC Game Audio Bundle (gratis, royalty-free): https://sonniss.com/gameaudiogdc
  - Freesound (filter op CC0): https://freesound.org
  - Kenney (CC0 sci-fi/UI geluiden): https://kenney.nl/assets/category:Audio
