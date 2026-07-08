# SOMA Public

SOMA Public is a Unity project for visualizing motion data from IMU-based wearable sensors in real time. The project is designed to map sensor readings from a Soma device onto a 3D mannequin or avatar, making it possible to inspect motion in a visual and interactive way.

## What this project does

The repository combines three main ideas:

- Receiving IMU sensor data from a Bluetooth-enabled device
- Translating that data into bone or body-part rotations in Unity
- Providing a UI for telemetry, simulation, and debugging

In practice, this means you can connect a Soma-compatible sensor setup, calibrate the motion, and see the body model react to movement.

## Project summary

This repo is a Unity 6 project targeting the editor version:

- Unity: 6000.4.7f1

It includes:

- A sample Unity scene
- A custom UI generator for SOMA controls
- Bluetooth/IMU processing scripts
- Simulation tools for testing without hardware
- Telemetry visualization for real-time debugging

## Repository structure

- [Assets](Assets) – main Unity assets and project content
  - [Assets/Editor](Assets/Editor) – editor tooling
  - [Assets/Scripts](Assets/Scripts) – runtime gameplay and sensor logic
  - [Assets/Scripts/BLE_Manager](Assets/Scripts/BLE_Manager) – BLE-related test scripts
  - [Assets/Scenes](Assets/Scenes) – Unity scenes
  - [Assets/Model](Assets/Model) – 3D model assets
  - [Assets/Plugins](Assets/Plugins) – platform-specific plugins
- [Packages](Packages) – Unity package configuration
- [ProjectSettings](ProjectSettings) – Unity project settings
- [SOMA-PUBLIC.slnx](SOMA-PUBLIC.slnx) and [SOMA.slnx](SOMA.slnx) – solution files

## Main components

### 1. Multi sensor motion controller

File: [Assets/Scripts/MultiSensorController.cs](Assets/Scripts/MultiSensorController.cs)

This is the core runtime script of the project.

It is responsible for:

- Receiving IMU data from the Bluetooth pipeline
- Mapping sensor IDs to body segments such as elbow, wrist, shoulder, hip, knee, and ankle
- Applying rotation logic to Unity bones
- Supporting several motion modes:
  - Accelerometer mode
  - Gyroscope mode
  - Improved hybrid mode
- Handling calibration and axis inversion/swap settings

The script contains a configurable list of sensor bones, where each entry can be tuned with:

- offset pose
- tracking mode
- axis inversion
- axis swapping
- sensitivity
- smoothing

This is the main place to adjust how sensor motion is translated into avatar motion.

### 2. Simple sensor demo controller

File: [Assets/Scripts/SensorController.cs](Assets/Scripts/SensorController.cs)

This is a simpler script used as a lightweight example of applying simulated rotations to a target bone. It is not the main production pipeline but can be useful for experimentation or prototyping.

### 3. Telemetry viewer

File: [Assets/Scripts/SOMATelemetryViewer.cs](Assets/Scripts/SOMATelemetryViewer.cs)

This script powers the telemetry UI panel. It:

- finds the telemetry panel in the scene
- binds the open/close buttons
- collects active MultiSensorController instances
- displays bone rotation values in a readable text panel

It is mainly used for debugging and monitoring live motion data.

### 4. Fake data simulator

File: [Assets/Scripts/SOMAFakeDataTester.cs](Assets/Scripts/SOMAFakeDataTester.cs)

This script makes it possible to preview motion without hardware.

It provides:

- a popup simulation menu
- buttons for full-body or limb-specific preview motion
- automatic preview behavior
- fake motion generation based on sensor IDs and body-part heuristics

This is useful when you want to test the visual system before connecting real sensors.

### 5. UI generator

File: [Assets/Editor/SOMA_UIGenerator.cs](Assets/Editor/SOMA_UIGenerator.cs)

This is an editor tool that generates a custom Unity UI layout for the SOMA experience.

When run from the Unity editor menu Tools > Generate SOMA UI, it creates:

- a top bar
- left/right side panels
- a simulation popup
- telemetry UI buttons and panels
- a basic visual layout for the application

This is helpful for quickly scaffolding the interface used by the other scripts.

### 6. BLE test script

File: [Assets/Scripts/BLE_Manager/BLE_Test.cs](Assets/Scripts/BLE_Manager/BLE_Test.cs)

This is a smaller Bluetooth test implementation. It demonstrates how to:

- scan for a BLE peripheral
- connect to it
- receive notification data
- parse JSON IMU frames
- update UI text based on the latest readings

It is more experimental than the main controller script and is useful as a reference for BLE parsing.

## How the motion pipeline works

The project follows a simple data flow:

1. Sensor data arrives from BLE as JSON
2. The controller parses the payload into IMU values
3. The values are transformed using calibration, inversion, swapping, and sensitivity settings
4. A rotation is computed for the relevant bone
5. The bone is rotated in Unity to visually reflect the motion

This means the behavior of the avatar is driven by the combination of:

- the sensor data itself
- the body segment mapping
- the calibration state
- the chosen tracking mode

## Unit tests

The project now includes a small Unity Test Runner suite for the main C# scripts under [Assets/Scripts](Assets/Scripts).

### Test location

The tests are stored in:

- [Assets/Tests/Runtime/SOMAScriptsTests.cs](Assets/Tests/Runtime/SOMAScriptsTests.cs)

### What the tests cover

The current suite validates:

- creation and initialization of the main MonoBehaviour scripts
- sensor-bone configuration for MultiSensorController
- fake data mode selection and limb filtering
- basic telemetry viewer initialization
- hybrid tracking rotation updates
- IMU JSON parsing behavior for the Bluetooth pipeline
- BLE test component creation on Apple platforms

### How to run the tests in Unity

1. Open the project in Unity.
2. Go to Window > General > Test Runner.
3. Select PlayMode or EditMode depending on the kind of test you want to execute.
4. Run the SOMA-related tests from the list.

### Notes on testing

- These tests are designed to be lightweight and compatible with Unity’s built-in test framework.
- They focus on verifying the main public behaviors and core logic of the scripts without requiring a connected device.
- For more complete validation, you can extend the suite with additional calibration and UI-specific tests later.

## Setup instructions

### Prerequisites

- Unity 6000.4.7f1 or a compatible Unity 6 version
- macOS/iOS build support if you want to use CoreBluetooth directly

### Open the project

1. Open the folder in Unity
2. Open the sample scene from [Assets/Scenes](Assets/Scenes)
3. Make sure the scene contains a mannequin or avatar with bones that can be assigned to the controller

### Generate the UI

1. In the Unity editor, open the menu item:
   - Tools > Generate SOMA UI
2. This creates the UI panels and simulation controls used by the scripts

### Configure the controller

In the scene, assign the relevant bones to the MultiSensorController component:

- each sensor bone entry should point to the correct Transform
- the sensor ID should match the naming convention expected by the script

### Calibrate

Use the following controls while running the scene:

- Press C to calibrate the sensors
- Press R to reset rotations

### Test without hardware

If you do not have the hardware device connected, you can use the simulation popup generated by the UI, or enable the preview mode in the fake data tester.

## Bluetooth and hardware notes

The project includes Bluetooth logic through the UnityCoreBluetooth integration on macOS/iOS.

Important details:

- The main controller looks for a peripheral named "Soma.firmware"
- The BLE pipeline expects JSON payloads containing IMU data
- The implementation is platform-specific and may require the relevant plugin and build target to be configured correctly

## Important limitations

- The sensor-to-bone mapping is currently based on predefined IDs and heuristics
- The project appears to be a prototype or research-oriented visualization tool rather than a fully polished production app
- Some parts are hard-coded for the current device and scene structure
- The Bluetooth support is focused on the macOS/iOS path and may need adaptation for other platforms

## Recommended next steps

If you want to extend this project, the most useful areas are:

- adding more robust sensor-to-bone mapping
- improving calibration stability
- adding support for more device types or protocols
- improving the UI and telemetry formatting
- creating a cleaner data model for IMU frames

## Quick interpretation guide

If you are new to this repository, think of it as:

- a motion visualization tool
- a Unity front-end for wearable IMU data
- a prototype pipeline for turning sensor motion into animated body movement

The most important script to understand first is [Assets/Scripts/MultiSensorController.cs](Assets/Scripts/MultiSensorController.cs).
