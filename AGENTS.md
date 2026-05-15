# VP_DeviceManager

## Overview

Unity 6000.3.10f1 project for managing VR/XR devices in a virtual production context. Provides a unified framework for discovering, monitoring, and bridging real-world devices (XBee wireless I/O modules, HTC Vive Trackers, Unity Input System actions) into OSC (Open Sound Control) for use by external applications.

## Project Structure

```
Assets/
├── Materials/
│   └── Textures/         Grid textures
├── Prefabs/
│   └── TrackedObjectInfo.prefab
├── Scripts/
│   ├── Common.Logging.*/         Vendored logging framework adapted for Unity
│   ├── XBeeLibrary.Core/        Vendored XBee communication library (ZigBee, 802.15.4, DigiMesh)
│   ├── XBeeLibrary.Windows/     Windows serial port implementation for XBee
│   ├── IDevice.cs               Core device interface
│   ├── IDeviceManager.cs        Core device manager interface
│   ├── XBeeManager.cs           XBee coordinator & remote device management
│   ├── HTCViveTrackerProfile.cs         OpenXR interaction profile (full)
│   ├── HTCViveTrackerProfile_HTC.cs     Alternate tracker profile (disabled, #if false)
│   ├── DeviceInformationUI.cs   TMP-based device info display
│   ├── OSCDeviceInformationProvider.cs  Exposes OSC variables as devices
│   ├── Tracked_OSC_Device.cs    Sends transform pose via OSC
│   └── InputAction_OSC_Device.cs        Bridges InputSystem actions to OSC
├── TextMesh Pro/                TMP resources
└── XR/                          OpenXR settings & loaders
```

## Core Architecture

### Interfaces

- **`IDevice`** (`Assets/Scripts/IDevice.cs`) — Base interface with `GetDeviceName()` and `GetDeviceInformation(StringBuilder, string)`. Also provides `IDeviceComparer` for alphabetical sorting.
- **`IDeviceManager`** (`Assets/Scripts/IDeviceManager.cs`) — Interface with `GetDevices(List<IDevice>)` for enumerating managed devices.

### Device Managers & Device Implementations

- **`XBeeManager`** — Manages XBee radio modules over serial (COM port). Discovers remote XBee nodes, processes digital IO samples, exposes IO states as OSC bool variables. Supports XBee v1 (Raw802), v2/v3 (ZigBee). Includes configurable stale device timeout (default 0 = never) that removes devices and their OSC variables after a period of inactivity.
- **`OSCDeviceInformationProvider`** — Discovers `IOSCVariableContainer` components on the same GameObject and reports their OSC variables as device information.
- **`HTCViveTrackerProfile`** — Custom OpenXR interaction profile implementing `XR_HTCX_vive_tracker_interaction`. Registers `HTCViveTrackerOpenXR` Input System device layout with 12 body role usages (left/right foot, shoulder, elbow, knee, waist, chest, camera, keyboard). Maps full set of inputs (pose, system/menu/grip/trigger/trackpad buttons, analog trigger/trackpad, haptic).
- **`Tracked_OSC_Device`** — Monitors a Transform and sends position/rotation as a 6-DoF pose OSC variable.
- **`InputAction_Device`** — Maps 4 `InputActionProperty` bools to OSC variables and provides haptic output via `XRControllerWithRumble.SendImpulse`.

### UI

- **`DebugInformation`** (`DeviceInformationUI.cs`) — Periodically queries all `IDeviceManager` instances in the scene and renders device info via TextMeshPro.

## Key Patterns

- **OSC Bridging**: Devices create `OSC_BoolVariable` / `OSC_6DofPoseVariable` instances and call `SendUpdate()` at configurable intervals.
- **Coroutine-based initialization**: `XBeeManager` uses coroutines (`OpenCoordinator`) for async serial port setup and network discovery.
- **Auto-discovery**: `DebugInformation.GatherDeviceManagers()` uses `FindObjectsByType<MonoBehaviour>().OfType<IDeviceManager>()` at runtime.
- **TypeConstraint attribute**: Used on `DebugInformation.Device` to constrain the inspector to `IDeviceManager` GameObjects.
- **Stale device pruning**: `XBeeManager` tracks last IO sample time per device and removes devices (and their OSC variables) that exceed `DeviceTimeout`.

## Dependencies

- **Unity**: 6000.3.10f1
- **Packages**:
  - `com.unity.inputsystem` 1.18.0
  - `com.unity.xr.openxr` 1.16.1
  - `com.unity.ugui` 2.0.0
  - `nz.ac.aut.sentiencelab` (SentienceLab Unity Framework, git dependency v1.8) — provides OSC variables, `IOSCVariableContainer`, and other base utilities
- **Vendored**: XBeeLibrary (Core + Windows), Common.Logging (Portable + Core)

## Build & Run

- Platform: Standalone (Windows), WSA
- OpenXR loader configured, HTC Vive Tracker Profile as interaction feature
- Default scripting backend: standard .NET

## Conventions

- Namespace: `UnityEngine.XR.OpenXR.Features.Interactions` for tracker profile
- AddComponentMenu entries: `"VP/XBee Manager"`, `"VP/Tracked OSC Device"`, `"VP/InputAction OSC Device"`
- OSC path structure: `{prefix}/{node_id}/{dio|input}{N}` for XBee, `{prefix}/pose` for trackers
- Serial communication: configurable COM port, baud rate, stop bits, parity, handshake

## Remaining Issues

1. **HTCViveTrackerProfile_HTC.cs (entirely `#if false`)** — ~410 lines of disabled dead code. Should be deleted.
2. **Commented-out debug code in XBeeManager.cs** — Several blocks of old debug logging remain commented out, adding noise.
3. **InputAction_Device public bools** — `Input1On`–`Input4On` are serialized public fields but overwritten every frame in `Update()`, making inspector values misleading.
4. **Unused using directive** — `System.Net` is imported in `XBeeManager.cs` but never used.
5. **No tests** — Zero test infrastructure for a device bridging framework.
