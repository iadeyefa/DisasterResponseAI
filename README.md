# 🚦 Intelligent Traffic Management Simulation

## Project Overview

This repository contains the source code and assets for an Intelligent Traffic Management Simulation built in Unity. The project leverages real-world Geographic Information System (GIS) data, specifically from OpenStreetMap processed through ArcGIS CityEngine, to create highly realistic and geometrically accurate urban environments.

The primary objective of this simulation is to compare the performance of a Fixed-Time ("Dumb") traffic control system against an Adaptive ("Smart") traffic control system based on a heuristic "Pain Score." Our research shows that the adaptive system can significantly increase traffic throughput and drastically reduce average wait times.

### Core Features:
- **Real-World GIS Integration:** Automated pipeline to convert OpenStreetMap data into smooth, drivable Bezier curve-based road networks.
- **Adaptive Traffic Control:** Implementation of a heuristic-based system that dynamically prioritizes traffic signals to prevent gridlock and vehicle starvation.
- **Intelligent Vehicle Agents:** Agents use hierarchical navigation combining global A* pathfinding on the lane graph with local obstacle avoidance.
- **3D Visualization:** High-fidelity real-time visualization built in Unity for accessible analysis and urban planning presentation.

## Project Setup and Access

This project can be accessed in two ways: by downloading the standalone executable for immediate use, or by cloning the repository to open and modify the Unity source project.

### 1. Executable Build (Recommended for Users)

The standalone executable allows immediate testing of the simulation without installing Unity.

**Location:** The compiled build files are located in the `GameBuild/` directory.

**Setup:**
1. Download the `Game.zip` file from the `GameBuild/` directory.
2. Extract the ZIP file to your preferred location.
3. Open the extracted `Game` folder.
4. Run the executable: `DisasterResponseAI.exe` (Windows).

**Controls and UI:**
- **Mouse Look:** Press and hold the scroll wheel and move the mouse.
- **Movement:** Use W, A, S, D to move the camera.
- **UI Dashboard:** The interface displays:
  - **Throughput Score:** Shows cars/min metric
  - **Average Wait Time:** Displays average wait time in seconds
  - **Traffic Overlay Toggle:** Enable/disable traffic visualization overlay
  - **Smart Traffic Toggle:** Enable/disable the adaptive traffic control system for comparison
  - **Time Scale Dropdown:** Adjust simulation speed between 1x, 10x, and 20x

### 2. Unity Project (For Developers)

To open, modify, and extend the simulation, you must use the Unity Editor.

#### Dependencies
- **Unity Version:** Unity 6.2 (6000.2.8f1)

#### Setup Steps

1. **Clone the Repository:**
```bash
git clone https://github.com/rs-dkd/IntelligentTrafficManagementSim.git
cd IntelligentTrafficManagementSim
```

2. **Open in Unity:**
   - Launch the Unity Hub.
   - Click **Add** and select the root directory of the cloned repository (`IntelligentTrafficManagementSim`).
   - Ensure the project opens with the correct Unity version (6.2).

3. **Run the Scene:**
   - In the Project window, navigate to the main scene: `Assets/Scenes/`.
   - Double-click the main scene file (`Main2`) to load it.
   - Press the **Play** button to start the simulation.

## Project Structure and Key Folders

The project follows a standard, organized Unity folder hierarchy.

| Folder Path | Description | Key Components |
|------------|-------------|----------------|
| `Assets/Scripts` | Contains all C# source code for agents, navigation, and systems. | `TrafficVehicle.cs`, `TrafficLightGroup.cs`, `CityEngineLaneBuilder.cs`, A* implementation. |
| `Assets/Scenes` | Contains the core Unity scene files (the levels). | The primary scene for the simulation environment. |
| `Assets/Prefabs` | Reusable game objects for vehicles, traffic lights, and other props. | Vehicle models, Traffic Light Prefabs. |
| `Assets/Imports` | Raw or imported data, including the CityEngine JSON export. | GIS road network data. |
| `Assets/Resources` | Assets loaded via the Resources API (if used). | Textures, materials, or small data files. |
| `Assets/Settings` | Project-specific configuration files (e.g., Input Settings, Physics settings). | Simulation parameters and global variables. |
| `Packages/` | External Unity packages (managed by the Unity Package Manager). | Includes the CityEngine Environment and utility packages. |

## Development and Contribution

The core of this system is the integration of GIS data into a functional road network. The primary tool used is the `CityEngineLaneBuilder`.

### GIS Data Import Pipeline

The pipeline automates the creation of the Bezier-curve based road network:

1. **OpenStreetMap Extraction:** Road network for the target region is extracted.
2. **CityEngine Processing:** CityEngine generates 3D geometry and exports the street graph data as JSON.
3. **Unity Import:** The custom `CityEngineLaneBuilder` script parses the JSON, converts polylines to Bezier Curves, and establishes the `nextLanes`/`neighborLane` connectivity graph for A*.

## Key Metrics and Results

The Smart AI system, which uses the **Pain Score = (C_g × w_c) + (T_wait × w_t)** heuristic, significantly outperforms the Fixed-Time system.

| Metric | Basic System (Fixed-Time) | Smart AI (Adaptive) |
|--------|---------------------------|---------------------|
| **Throughput (cars/min)** | 2000 - 3000 | 7000 - 9000 |
| **Avg. Wait Time (s)** | 30 - 50 | 7 - 12 |

**Conclusion:** The Smart AI achieves up to 300% higher throughput and reduces average wait times by over 75% by dynamically skipping empty phases and prioritizing lanes with high cumulative wait times.
