# Jakkho — Restore Notes (missing assets after the master-project merge)

## What happened
When the scenarios were merged into this single "master" project (commit `65f05fb`),
some asset folders were dropped. The main casualty for **Scenario 1** was the whole:

    Assets/Scenarios/Scenario1/Framework/

The UI sprites referenced by the monitor/Telegram UI were therefore missing, so every
`Image` rendered as a blank white box (the "white panel" look). Shaders/materials/audio
from that folder were also gone.

## Recovered assets (copied from D:\JAKKHO on 2026-09-11, GUIDs preserved)
Recovered into `Assets/Scenarios/Scenario1/Framework/...` and `Assets/TextMesh Pro/...`:

### UI sprites — `Framework/UI/Sprites/`
- Windows_Homescreen.png
- Telegram_Avatar_Circle.png
- Telegram_Plane_White.png
- Telegram_Canceled_Call_Bubble.png
- Telegram_Call_Screen_1080p.png
- Telegram_Desktop_Screen.png
- Telegram_Download_Cancel_Button.png
- Telegram_Download_Complete_Icon.png
- Telegram_Download_Spinner_Arc.png
- Command_Prompt_Header_1080p.png
- UI_Button_Rounded_9Slice.png
- UI_Card_Rounded_9Slice.png
- UI_Header_Top_Rounded_9Slice.png

### Audio — `Framework/Audio/`
- Security_Alarm_Siren.wav
- Telegram_Call_Ringout.wav

### Materials / shaders / prefabs
- `Framework/Materials/M_UI_CullBack.mat`  (uses shader…)
- `Framework/UI/Shaders/UI_Default_CullBack.shader`
- `Framework/Materials/M_VR_Cursor_Reticle.mat`
- `Framework/VR/Prefabs/VR_Cursor_Reticle.prefab`

### Fonts
- `Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset`

## Still missing (not in D:\JAKKHO either — need the original scenario repos/build)
- `556195ea283630547b0f1ccbefc14388` — referenced by Scenario2 `Smartphone.prefab`, `FloatingSmartphonePanel.prefab`, `Scenario2.unity`
- `9532b773a9b734544a2eb61f943958aa` — Scenario3 `BarkingDog_Level.prefab`
- `cf63880488ec29f43a759ecd9c6c5123` — Scenario3 `BarkingDog_Level.prefab`
- `da692e001514ec24dbc4cca1949ff7e8` — several Scenario2 materials (texture/shader ref)

## How to restore more missing assets (repeatable)
1. Compare GUIDs: scan scenario `.unity/.prefab/.asset/.mat/.shader` files for `guid: …`
   and check with `AssetDatabase.GUIDToAssetPath` (or .meta lookup) whether they exist.
2. For each missing GUID, look for a `.meta` containing that GUID in another copy of the
   project (e.g. `D:\JAKKHO`).
3. Copy the asset **and its `.meta`** into this repo (same relative path is nice, but the
   GUID in the .meta is what actually re-links references).
4. `AssetDatabase.Refresh()` in Unity.
5. If a recovered material points to a missing shader, recover the shader too (same process).

Helper scripts used (temp folder): `find_missing_assets.py`, `recover_more.py`, `list_missing_now.py`.

## Notes
- D:\JAKKHO is a separate test copy (other machine/chat) — read from it only, never write.
- Do NOT hand-edit assets: copy file + `.meta` together to preserve GUIDs.
