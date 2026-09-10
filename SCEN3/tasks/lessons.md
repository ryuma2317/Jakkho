# Lessons Learned

## VR Physics & Screen Shaking
1. **CharacterController Collider Overlaps**:
   - **Pattern**: Creating decorative props (chair seats, desk clutter, keyboard, notebook, desk legs) with default solid Unity BoxColliders or CylinderColliders right next to the player causes the CharacterController capsule to continuously penetrate them.
   - **Consequence**: Unity's physics engine depenetrates the player every frame, resulting in high-frequency camera stutter and violent screen shaking.
   - **Rule**: All decorative furniture (especially chairs the player sits in, keyboard, mouse, notebook, pens, monitor frames) MUST have their colliders removed (keepCollider = false). Only the main walkable floor and the tabletop surface should have colliders.

2. **CharacterController Gravity While Grounded**:
   - **Pattern**: Calling _character.Move(negative_y) unconditionally every FixedUpdate when _character.isGrounded is true forces the capsule into the floor, triggering a depenetration pop every tick.
   - **Rule**: In VR FixedUpdate, only apply gravity when !_character.isGrounded.

3. **Room-Scale Head Pushback**:
   - **Pattern**: Using Physics.OverlapSphere at head position and kicking the XR Origin by fixed distance causes extreme oscillation when leaning over a desk or near a monitor.
   - **Rule**: Never teleport or push the XR Origin in head-sphere checks near interactive desks or workstations.

## Sci-Fi VR Aesthetics
4. **Holographic Floating Displays vs Primitive Geometry**:
   - In sci-fi facility environments, floating holographic UI screens look significantly cleaner and more futuristic than blocky primitive cube/cylinder furniture. Keep floor surfaces clean without odd marker discs.

## TextMeshPro & Typography in VR
5. **LiberationSans SDF Missing Emoji Glyphs**:
   - **Pattern**: Using Unicode emojis (e.g., 📞, 🚩, 🛡️, 💬, ⚠️, ❌) with standard TMP font assets like `LiberationSans SDF` causes Unity to fail glyph lookups and render missing character squares (`\u25A1`).
   - **Rule**: In Unity TextMeshPro UIs without emoji sprite fallbacks, use clean typographic badges (e.g., `[ ! Not secure ]`, `[ CALL SOPHEA ]`, `[ Flag: ... ]`, `[ 2FA ]`) rather than color emojis.

## Narrative & Content Cohesion
6. **Full-Stack Scenario Synchronization**:
   - **Rule**: When updating a phishing scenario's narrative (e.g., switching from art contest vote to NextGen project survey), synchronize all interconnected layers:
     1. Telegram chat list item & preview snippet
     2. Active chat dialogue & rich link preview card
     3. Phishing browser omnibox URL, tab title & alert banner
     4. QR code verification call-to-action
     5. Manager red-flag descriptions & inspection triggers
     6. Phone call verification dialogue
     7. Consequence and debrief educational lesson cards
     8. Main menu scenario cards and carousel data
     9. Always trigger asset database refresh and recompile (`refresh_unity`) before re-running editor scene builders.

## VR Floating UI Ergonomics & Scale
7. **Floating Holographic Display Sizing & Height**:
   - **Pattern**: Applying pitch rotation to a local offset vector when positioning world-space canvases causes vector math ($y \cdot \cos\theta - z \cdot \sin\theta$) to drag down the world Y coordinate (e.g. dropping from 1.30m down to 1.17m, waist/knee level). Additionally, using standard desktop resolutions ($1440 \times 900$) at small world scale ($0.0011$) leaves a floating VR screen feeling like a distant, tiny monitor rather than a grand holographic console.
   - **Consequence**: Users in VR have to crane their necks downward, and the UI appears small and far away.
   - **Rule**:
      1. Keep floating VR UI plumb vertical ($0^\circ$ pitch) or apply pitch around the screen's own center pivot, never rotating the offset position vector.
      2. For computer workstation displays (simulating desktop monitors/Telegram/browsers where user sits or stands at a terminal): Set screen center at $Y = 1.10\,\text{m} - 1.15\,\text{m}$ (matching MainMenu_Canvas at $1.15\,\text{m}$). This places the top edge comfortably at $\approx 1.55\,\text{m} - 1.57\,\text{m}$ and bottom edge at $0.63\,\text{m} - 0.68\,\text{m}$, perfectly aligned with natural seated eye-level without craning upward. Setting center to $1.35\,\text{m}+$ pushes the top to $1.82\,\text{m}+$, which feels unnaturally high up.
      3. For grand sci-fi holographic presentation displays, elevate to $1.45\,\text{m} - 1.50\,\text{m}$ center.

## Panoramic Curved VR Workstation Geometry & Hardware Cradling
8. **Cockpit Wing Seams & Parametric Visor Backing**:
   - **Pattern**:
     1. In 3-wing cockpit layouts (Left Wing, Center, Right Wing), arbitrary wing offsets cause non-zero horizontal gaps and jagged Z-steps between panels (e.g. 16px gap, 27px Z-step).
     2. Generating a simple circular cylinder mesh ($x = R\sin\theta, z = R\cos\theta$) centered at origin as a visor backing causes the visor edges to curve forward much faster than the UI wings ($z$ drops by 38cm vs 31cm), leading to severe visual clipping where the glass and cyber rails pass 15cm straight through the side wings.
   - **Rule**:
     1. Calculate wing placement with exact trigonometric seam matching:
        For Center half-width $W_c/2$ and Wing half-width $W_w/2$ rotated by angle $\theta$, place wing center at $X = \pm (W_c/2 + W_w/2 \cdot \cos\theta)$ and $Z = -W_w/2 \cdot \sin\theta$. This guarantees a $0.0\,\text{mm}$ seamless connection to the center panel.
     2. Generate the visor mesh parametrically along the exact cockpit polyline with a cubic Hermite fillet around the seams, keeping the visor uniformly $2.5\,\text{cm}$ behind the UI across the entire arc with zero clipping.
     3. Frame the visor with matching cyber rails along top/bottom and vertical cyber-pillars at the outer tips.

## Realistic Workspace UI Architecture
9. **Single Unified Realistic Monitor vs. Multi-Wing Cockpits**:
   - **Pattern**: Creating complex 3-wing holographic cockpit layouts (angled left and right wings at $\pm 25^\circ$) for simulating everyday computer interactions (email, chat messaging, web browsing).
   - **Consequence**: Users dislike the fragmented sci-fi spaceship feel; it breaks immersion for realistic cybersecurity training; requires unnatural neck turning to read side panels; and unnaturally tears cohesive desktop applications (like Telegram or Chrome) across separate angled panels.
   - **Rule**: Standardize on a clean, single unified monitor/display (standard 16:9 or 21:9 ultrawide) sitting on or directly above the workstation desk. Applications must live as coherent, realistic desktop windows where sidebars, navigation, and content share a single continuous screen surface.

## Audio & Sound Design in VR
10. **Ambient Drone Audio vs. Hardware Scare**:
    - **Pattern**: Attaching continuous looping low-frequency ambient drone or industrial room tone audio clips (e.g. `AmbientBackgroundNoise.mp3`) with `playOnAwake = true`.
    - **Consequence**: In headphones or laptop speakers, continuous low-frequency hums and electrical room tones sound identical to laptop fan bearing failure, GPU coil whine, or electrical ground loop issues, causing genuine alarm that hardware is dying.
    - **Rule**: Never auto-play continuous mechanical drone or electrical buzz clips. Restrict audio purely to crisp, high-quality, intentional event feedback (button ticks, notification chimes, victory fanfares, ringtones). Never leave looping ambient noise clips running in the background.

11. **Short Clip Looping & Waveform Synthesis**:
    - **Pattern**: Setting `loop = true` on short UI sound clips (e.g. 10ms - 60ms button clicks or hovers) to serve as continuous audio (like a telephone ringtone or alarm).
    - **Consequence**: Looping a 12ms clip 83 times per second turns the audio into an 83Hz harsh waveform oscillator—producing an intense, screeching, roaring electrical buzz that sounds like an error alarm or broken engine drill. Additionally, firing haptic impulses per-frame in `Update()` rattles controller motors uncontrollably.
    - **Rule**:
      1. Never loop short UI blips (< 1 second) as sustained sounds. Use dedicated musical ringtones (e.g. 3.0s marimba motifs with breathing pauses between rings).
      2. When using haptic feedback during incoming calls, pulse rhythmically (e.g. 2 gentle pulses every 1.5s) using a delta-time timer, never in unrestricted per-frame `Update()`.
      3. Always ensure audio sources stop and reset `loop = false` when exiting active call states.

12. **Curved VR Floating Screens Without Wings**:
    - **Pattern**:
      1. Flattening the entire WorldSpace canvas when told to avoid "wings", leaving the screen completely flat while rails above and below are curved.
      2. Splitting a curved screen into separate floating cards with margins or divider pillars, making it look like 3 detached monitors ("wings").
      3. Inverting rotation angles in Unity (+Y rotation in left-handed system), causing outer edges to bend away from the player (convex) or step off the Z-plane.
    - **Rule**:
      1. Construct the curved display as tangent sections touching with exact 0.0mm seam precision:
         - Left Section center: X = -(W_c/2 + W_s/2 * cos(theta)), Z = -W_s/2 * sin(theta), rotY = -theta.
         - Right Section center: X = +(W_c/2 + W_s/2 * cos(theta)), Z = -W_s/2 * sin(theta), rotY = +theta.
      2. Top window bars (Telegram blue header #2277BB, Chrome dark omnibox bar #202124, CADT OS status bar) and window backgrounds MUST run edge-to-edge across all sections without divider pillars or margins, presenting a single continuous curved ultrawide screen.
      3. Cyber rails and transparent URP Lit glass visor must span the full width of the screen (~68.6 deg) so the glowing rails frame the screen seamlessly from corner to corner.
      4. Never introduce physical desks or floating knee-shelves when a pure sci-fi floating holographic screen is requested.
13. **Asset Migration Path Sync & Directory Verification**:
    - **Pattern**: When moving asset folders (e.g. `Assets/Materials/` -> `Assets/_Project/Materials/` or referencing third-party packages like `Barking_Dog`), leaving old hardcoded string paths in procedural builder scripts (`AssetDatabase.FindAssets`, `AssetDatabase.CreateAsset`, `AssetDatabase.LoadAssetAtPath`).
    - **Consequence**: Unity throws `Folder not found` and `Parent directory must exist before creating asset` errors during automated scene builds. Additionally, calling `DestroyImmediate` on a canvas with `TrackedDeviceGraphicRaycaster` in edit mode throws a `KeyNotFoundException` if raycasters are active in XRI's internal lookup dictionary.
    - **Rule**:
      1. Always ensure parent directories exist via `Directory.CreateDirectory` before calling `AssetDatabase.CreateAsset`.
      2. Always check `AssetDatabase.IsValidFolder` before passing folder arrays to `AssetDatabase.FindAssets`.
      3. Before calling `DestroyImmediate` on a canvas with `TrackedDeviceGraphicRaycaster`, disable all `BaseRaycaster` components first inside a try-catch block.
      4. Always grep across the codebase for old string paths when reorganizing repository directories.

14. **Modular Kit Floor Pits, UI Sprite Tinting & Eye-Level Height Alignment**:
    - **Pattern**:
      1. BarkingDog modular kit's `Room_Big_Part_03` contains a 3-meter deep sunken pit with vertical floor wall panels. Searching for legacy or mismatched names (like `floor_plain_4x4_01 (4)`) leaves the pit open, exposing `SafetySubFloor` (pitch-black) and creating a giant black void in the lower half of the screen.
      2. Setting `fill = new Color(0.08, 0.12, 0.20, 0.92)` inside `CreateGlassCardSprite` AND passing `cyberDarkBg = new Color(0.05, 0.08, 0.14, 0.96)` to `CreateGlassCard` double-multiplies the color into pure pitch-black `(0.004, 0.009, 0.028)` and turns the glowing cyan border black.
      3. Setting `WorkstationHeight = 1.10m` while the VR camera sits at standard eye level `1.36m` leaves the screen floating awkwardly below eye level with distorted perspective.
    - **Rule**:
      1. Always inspect actual modular kit child transforms dynamically (`c.localPosition.y < -0.5f` and rotation $\approx 90^\circ$) to level floors and deactivate vertical pit walls.
      2. In Unity UI 9-sliced sprites, keep the sprite's glass fill translucent ($\alpha \approx 0.55$) and pass `Color.white` to `CreateGlassCard` so tint multiplication never crushes borders or fill colors into opaque black.
      3. Keep `WorkstationHeight` set to $1.30\,\text{m}$ so the center of the curved holographic screen aligns with the VR player's eye level.

