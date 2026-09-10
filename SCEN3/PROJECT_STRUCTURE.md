# JakkhoVR — Project Structure & Architecture Guide

> **Cybersecurity Awareness VR Training** — Unity 6 (6000.3.8f1) · URP 17 · XR Interaction Toolkit 3.5.1
>
> Single source of truth for repository organization, folder standards, and scenario workflows.

---

## Quick Start for New Teammates

1. Open the project in **Unity 6** (`6000.3.8f1`).
2. Ensure **XR Plugin Management** is active (OpenXR + Meta Quest feature group).
3. Build or open scenes:
   - **Main Menu**: `Assets/_Project/Core/Scenes/MainMenu.unity`
   - **Scenario Selection**: `Assets/_Project/Core/Scenes/ScenarioSelection.unity`
   - **Scenario 3 (Home)**: `Assets/Scenarios/Scenario3_Home/Scenes/Scenario3_Home.unity`
4. Automated scene builders can be run from the Unity top menu:
   - `Jakkho → Build Main Menu Scene & Prefab`
   - `Jakkho → Build Scenario Selection Scene & Prefab`
   - `Jakkho → Build Scenario 3 Scene & Environment`
5. Press **Play** in `Scenario3_Home` to test the active scenario.

---

## Complete Project Folder Structure

```
Assets/
├── _Project/                             <- Global Shared Framework & Infrastructure
│   ├── Core/
│   │   ├── Scripts/                      <- GameManager.cs, ScenarioManager.cs, ScoreTracker.cs
│   │   │   └── Editor/                   <- JakkhoAllScenesBuilder.cs, AudioAssetGenerator.cs
│   │   ├── ScriptableObjects/            <- ScenarioDataSO.cs, RedFlagSO.cs
│   │   ├── Prefabs/                      <- MainMenu_Canvas.prefab, ScenarioSelection_Canvas.prefab
│   │   └── Scenes/                       <- MainMenu.unity, ScenarioSelection.unity
│   │
│   ├── VR/                               <- Shared Physical VR Props & Interactions
│   │   ├── Scripts/                      <- VRInteractionHandler.cs, VRUIHoverEffect.cs, VRRoomScaleCollision.cs
│   │   ├── Props/                        <- VRInteractiveSmartphone.cs, VRHardwareSecurityKey.cs, VREmergencySeverButton.cs
│   │   └── Prefabs/                      <- Shared VR physical prop prefabs (Smartphone, YubiKey, E-Stop)
│   │
│   ├── UI/                               <- Shared UI Design System & Shaders
│   │   ├── Scripts/                      <- MainMenuUI.cs, CarouselController.cs, AudioWaveformVisualizer.cs, DialogueSystem.cs
│   │   ├── Sprites/                      <- UI_CircleAvatar.png, UI_GlassButton.png, UI_GlassCard.png, UI_GlassPanel.png
│   │   ├── Workstation/                  <- Curved holographic chassis math & configs
│   │   ├── Debrief/                      <- UniversalDebrief card layouts & XP counter templates
│   │   ├── Immersion/                    <- Universal briefing prompt templates
│   │   └── Shaders/                      <- Holographic cyber visor glass shaders
│   │
│   ├── Audio/                            <- Universal Synthesized SFX & UI Audio
│   │   ├── Phone_Notification.wav        <- 2-tone message chime
│   │   ├── Phone_Ringtone.wav            <- 3.0s marimba chord sequence
│   │   ├── Security_Warning.wav          <- Digital red flag warning alert
│   │   └── Success_Fanfare.wav           <- Victory ascending arpeggio
│   │
│   ├── Materials/                        <- 38 PBR/URP Lit Materials (Furniture, Tech, Glass, Neon)
│   │
│   └── Templates/                        <- Reusable Blueprints for Quick Copying
│       ├── _ScenarioTemplate_Manager.cs  <- Base manager template for new scenarios
│       └── Editor/
│           └── _ScenarioTemplateBuilder.cs <- Automated scene builder template
│
├── Scenarios/                            <- Feature-Isolated Training Scenarios
│   ├── Scenario1_Office/
│   │   ├── Scenes/                       <- Scenario1_Office.unity
│   │   ├── Scripts/                      <- Scenario1_SpearPhishingManager.cs
│   │   │   └── Editor/                   <- Scenario1Builder.cs
│   │   ├── Prefabs/                      <- OfficeDesk.prefab, OutlookClient.prefab
│   │   └── Data/                         <- Scenario1_Config.asset + RedFlag assets
│   │
│   ├── Scenario2_Cafe/
│   │   ├── Scenes/                       <- Scenario2_Cafe.unity
│   │   ├── Scripts/                      <- Scenario2_EvilTwinManager.cs
│   │   │   └── Editor/                   <- Scenario2Builder.cs
│   │   ├── Prefabs/                      <- CafeTable.prefab, RogueCaptivePortal.prefab
│   │   └── Data/                         <- Scenario2_Config.asset + RedFlag assets
│   │
│   └── Scenario3_Home/                   <- Fully implemented reference scenario
│       ├── Scenes/                       <- Scenario3_Home.unity
│       ├── Scripts/                      <- Scenario3_FakeLinkManager.cs
│       │   └── Editor/                   <- Scenario3Builder.cs
│       ├── Prefabs/                      <- Scenario3_Canvas.prefab, BarkingDog_Level.prefab
│       └── Data/                         <- Scenario3_Config.asset + RedFlag assets
│
└── Barking_Dog/                          <- Third-Party Environment Art Assets (Do not alter)
```

---

## Scenario Overview & Status

| # | Title | Setting | Threat Type | Status | Config Asset |
|---|-------|---------|-------------|--------|--------------|
| 1 | **Office Spear Phishing** | Corporate Office | Email Phishing & Disguised Attachments | 🟡 Stubs + Config ready | `Assets/Scenarios/Scenario1_Office/Data/Scenario1_Config.asset` |
| 2 | **Evil Twin at the Cafe** | Public Coffee Shop | Wi-Fi Man-in-the-Middle & SSL Stripping | 🟡 Stubs + Config ready | `Assets/Scenarios/Scenario2_Cafe/Data/Scenario2_Config.asset` |
| 3 | **The Fake Link Hijacking** | Home Study Room | Telegram Session Takeover & QR Phishing | ✅ Fully Built & Verified | `Assets/Scenarios/Scenario3_Home/Data/Scenario3_Config.asset` |

---

## Architecture & Conventions

### 1. The 5-Phase Scenario Standard

Every scenario controller inherits from `ScenarioManager` (`Assets/_Project/Core/Scripts/ScenarioManager.cs`) and executes the identical 5-phase training loop:

```
[Phase 1: Immersion] ──(4s intro)──► [Phase 2: Hook]
                                           │
                                     (threat trigger)
                                           ▼
                                [Phase 3: Investigation]
                                  │ (Inspect Red Flags)
                   ┌──────────────┼──────────────┐
                   ▼                             ▼
            [Bad Action]                  [Good / Ideal Action]
                   │                             │
                   ▼                             ▼
        [Phase 4: Consequence]        [Phase 4: Consequence]
        (Failure: 0 XP)               (Success: 100 XP / Ideal: 200 XP)
                   │                             │
                   └──────────────┬──────────────┘
                                  ▼
                         [Phase 5: Debrief]
                         (3 Educational Cards + XP)
                                  │
                    ┌─────────────┴─────────────┐
                    ▼                           ▼
            [Replay Scenario]           [Return to Menu]
```

### 2. Scoring & Outcomes

Standardized across all training modules:
- **`Failure` (0 XP)**: Player triggers the payload (e.g., scans rogue QR code, executes disguised attachment, submits credentials over plain HTTP).
- **`Partial` (50 XP)**: Player disengages but does not investigate or discover sufficient red flags.
- **`Success` (100 XP)**: Player rejects the attack and reports or disconnects safely.
- **`Ideal` (200 XP)**: Player discovers $\ge 3$ red flags AND takes proactive security defense (e.g., inserts hardware 2FA key, calls out-of-band to verify, enables VPN).

### 3. Data-Driven Red Flags (`RedFlagSO` & `ScenarioDataSO`)

Scenarios configure their metadata and red flags via ScriptableObject assets located in `Scenarios/ScenarioX/Data/`:

- **`ScenarioDataSO`**: Stores scenario title, description, setting, threat type, max XP, target scene name, and list of red flags.
- **`RedFlagSO`**: Defines `flagId`, `flagName`, `description`, `icon`, and `educationalText`.
- Managers register discoveries at runtime via:
  ```csharp
  RegisterRedFlag("flag_id", "Description of observed threat indicator");
  ```

---

## VR Hardware Standards

All scenarios follow these unified physical measurements to maintain spatial consistency across headsets:

| Element | Specification | Rationale |
|---------|--------------|-----------|
| **Workstation Height** | `Y = 1.10m` | Matches natural eye level for seated VR players |
| **Player-to-Screen Distance** | `Z = 1.45m` | Optimal vergence-accommodation distance without head strain |
| **Curved Visor Design** | 3 Tangent Sections | Prevents raycast warping with Unity's `TrackedDeviceGraphicRaycaster` |
| **Section Dimensions** | Center: 704×900px (0°)<br>Left: 448×900px (-18°)<br>Right: 448×900px (+18°) | Seamless 1600×900 panoramic display with 0.0mm physical gap |
| **Holographic Aesthetics** | Transparent URP Lit Visor + Cyan Rails | No heavy physical desks or opaque divider pillars |

---

## Developer Rules (Mandatory)

1. **Unity Input Standard**: Always use the **Input System Package (New)** (`com.unity.inputsystem`). Never use legacy `UnityEngine.Input`.
2. **Audio Discipline**: All `AudioSource` components must have `playOnAwake = false` and `loop = false`. Never leave continuous ambient drones playing on scene launch.
3. **Automated Scene Construction**: Keep manual scene edits to a minimum. Use or update automated builder scripts (`ScenarioNBuilder.cs`) so scenes can be rebuilt deterministically from code.
4. **Isolated Feature Folders**: When developing a scenario, put all scenario-specific assets in `Assets/Scenarios/ScenarioN/`. Keep `_Project/` reserved strictly for code, prefabs, and assets shared by two or more scenarios.
5. **URP Compliance**: All custom shaders and materials must target Universal Render Pipeline (URP).

---

## How to Implement a New Scenario (Step-by-Step)

1. **Create the Scenario Folder Structure**:
   ```
   Assets/Scenarios/ScenarioN_Name/
   ├── Data/
   ├── Prefabs/
   ├── Scenes/
   └── Scripts/
       └── Editor/
   ```
2. **Create the Manager Script**:
   - Copy `Assets/_Project/Templates/_ScenarioTemplate_Manager.cs` into `Assets/Scenarios/ScenarioN_Name/Scripts/ScenarioN_Manager.cs`.
   - Update class name and configure threat-specific red flags and phase callbacks.
3. **Create the Builder Script**:
   - Copy `Assets/_Project/Templates/Editor/_ScenarioTemplateBuilder.cs` into `Assets/Scenarios/ScenarioN_Name/Scripts/Editor/ScenarioNBuilder.cs`.
   - Reference `Scenario3Builder.cs` for automated UI and VR origin instantiation.
4. **Create ScriptableObject Data Assets**:
   - In Unity: Right-click `Data/` → **Create → JakkhoVR → Scenario Data**.
   - Create 4 corresponding **Create → JakkhoVR → Red Flag** assets and assign them.
5. **Register in Carousel & Build Settings**:
   - Add the scenario entry to `CarouselController.cs` in `Assets/_Project/UI/Scripts/CarouselController.cs`.
   - Add the scene to `File → Build Settings` and `GameManager.cs`.
