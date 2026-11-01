# ArmorSim — Reverse-Engineered iOS Port

<div align="center">

**ArmorSim** rebuilt from a Windows IL2CPP binary and adapted for iPhone.

[![Build iOS IPA](https://github.com/SanekYo-tech/armorsim-ios/actions/workflows/ios.yml/badge.svg)](https://github.com/SanekYo-tech/armorsim-ios/actions/workflows/ios.yml)

</div>

---

## What is this?

A full reverse-engineering of [ArmorSim](https://store.steampowered.com/app/5159810/ArmorSim/) — a terminal-ballistics sandbox by Timerlan Galimov — from its compiled Windows IL2CPP binary (`GameAssembly.dll`, Unity 6000.3.11f1) back into source code, then adapted for iOS.

The original game has no source available. Every C# class, method, constant, and string was recovered by analyzing:
- **IL2CPP dump** (ISIL intermediate representation + x64 disassembly)
- **Ghidra decompilation** (9,642 functions, all named and typed)
- **global-metadata.dat** (string literals, constants, field offsets)

---

## Stats

| Metric | Value |
|--------|-------|
| Total C# files | 810 |
| Fully restored | 752+ |
| Methods recovered | ~9,600 |
| Original binary size | 132 MB (RAR5) |
| Restored project size | 118 MB |
| Unity version | 6000.3.11f1 |

## Game systems fully ported

- **PenetrationCore** — Tate model, ballistic limit, bisection solver
- **Flat2D Solver** — 2D continuum dynamics on GPU (ComputeShader), full draw screen with brush/decal/ruler
- **Continuum Backend** — ULTRA mode solver (8 Burst jobs, 256-thread parallel, Alexishevsky-Tate finish)
- **SUPER GPU Kernel** — 32 compute kernels, sparse grid, VRAM management, tape recorder with FramePipeline
- **RADAR** — Physical Optics solver, SBR tracing, facet integration (Dunavant quadrature), atlas baking, PPI scope
- **Crew Anatomy** — 16 organs, tissue damage model, shell layers
- **Aero** — streamline solver, cloud display, BVH raycasting
- **ERA/NERA** — reactive armor cassettes, NERA bulge, flyover fracture

## iOS adaptations

- Steamworks removed (`#if !UNITY_IOS && UNITY_STANDALONE_WIN`)
- P/Invoke guards (`winmm.dll`, `kernel32.dll` → soft fallback on iOS)
- **TouchInputHelper** — pinch-to-zoom, one-finger orbit, two-finger pan on all cameras
- **CanvasScaler** — 1920×1080 reference resolution on all 12 canvases
- **SafeAreaHelper** — notch/Dynamic Island support
- **IMGUI letterbox** — proper 1920×1080 scaling for `OnGUI` classes
- Local achievement/workshop backends (no Steam dependency)

## Building the .ipa

The project builds via **GitHub Actions** (`.github/workflows/ios.yml`):

1. **Unity export** — game-ci/unity-builder exports an Xcode project (requires Unity license secrets)
2. **Code signing** — imports .p12 certificate + provisioning profile (requires Apple Developer secrets)
3. **Archive & export** — `xcodebuild archive` → `xcodebuild -exportArchive` → `.ipa`

### Required secrets

| Secret | Description |
|--------|-------------|
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_SERIAL` | Unity Pro/Plus serial number |
| `IOS_CERTIFICATE_BASE64` | Base64-encoded .p12 signing certificate |
| `IOS_CERTIFICATE_PASSWORD` | Password for the .p12 file |
| `IOS_PROVISION_PROFILE_BASE64` | Base64-encoded .mobileprovision |
| `IOS_TEAM_ID` | Apple Developer Team ID |
| `IOS_BUNDLE_ID` | Bundle identifier (e.g. `com.galimovdynamics.armorsim`) |
| `IOS_EXPORT_METHOD` | `development`, `ad-hoc`, or `app-store-connect` |

Without these secrets, the workflow will fail at the Unity license step.

## Tools used for reverse engineering

Located in `tools_decomp/`:

- **`isil.py`** — reads IL2CPP ISIL dumps with annotated constants, field names, and method names
- **`strlit.py`** — extracts string literals from `global-metadata.dat` (v39)
- **`datlook.py`** — reads float/int constants directly from `GameAssembly.dll` at virtual addresses
- **`typeusage.py`** — resolves IL2CPP metadata usage tokens
- **`parse_cctor.py`** — parses static constructors from binary blobs
- **Ghidra scripts** — `ImportMethods.java` (names 9,642 functions), `DecompileMethods.java` (C pseudocode for all methods)

## Project structure

```
├── .github/workflows/ios.yml    # GitHub Actions pipeline
├── docs/
│   ├── PORTING.md               # Rules for restoring methods from IL2CPP
│   ├── IOS.md                   # iOS pipeline setup guide
│   └── ReadGeneralBodies_notes.md
├── port/                        # Unity project (restore target)
│   ├── Assets/
│   │   ├── Scripts/Assembly-CSharp/  # ~810 restored C# files
│   │   ├── Resources/                  # Models, textures, audio, shaders
│   │   ├── Editor/ArmorSimBuild.cs     # iOS build automation
│   │   ├── Scenes/                     # 7 game scenes
│   │   └── Plugins/                    # Steamworks.NET (Windows only)
│   ├── Packages/manifest.json
│   └── ProjectSettings/
├── tools_decomp/                # Reverse engineering tools
└── .gitignore
```

## Credits

- **Original game**: [ArmorSim](https://store.steampowered.com/app/5159810/ArmorSim/) by Timerlan Galimov / Galimov Dynamics
- **Reverse engineering**: IL2CPP dump analysis, Ghidra decompilation, custom tooling
- **Build pipeline**: [game-ci](https://game.ci/) for Unity builds in CI

---

*This project is for educational purposes. The original game is available on Steam.*
