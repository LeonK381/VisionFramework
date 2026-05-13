# Vision Framework

Unity-based framework for real-time simulation of visual impairments in mixed reality environments with **adaptive compensation strategies**. Designed for accessibility research, empathy training, and vision science education.

## 🎯 Overview

This framework enables researchers, designers, and educators to experience visual impairments through MR-headsets. It provides scientifically-validated simulations of common vision disorders with **real-time adaptive compensation** that adjusts based on interactive vision tests.

### Supported Platforms
- **Meta Quest** (Android/Oculus)
- **Apple iPhone** / iOS (ARKit + LiDAR)

### Simulated Visual Impairments
- **Myopia**
- **Hyperopia**
- **Astigmatism**
- **Cataract**
- **Glaucoma**
- **Protanopia**
- **Macular degeneration**
- **Diabetic Retinopathy**
- **Retinitis Pigmentosa**
- **Vitreous opacities**

### Key Features
- ✅ **Interactive Analyzers** – Scientific vision tests (Landolt-C, Ishihara, M-Charts, Humphrey Field)
- ✅ **Adaptive Compensation System** – Multiple strategies that adapt to measured impairment severity
- ✅ **Sensor Integration** – Eye-tracking, depth sensing (LiDAR), head movement tracking
- ✅ **Modular Architecture** – Plug-and-play effect system with ScriptableObject profiles
- ✅ **Extensible Design** – Easy to add custom effects, analyzers, and compensation strategies
- ✅ **Profile System** – Save/load visual impairment configurations
- ✅ **Real-time Performance**

---

## 📋 Requirements

### Unity Version
- **Unity 6000.0.61f1** (Unity 6)

### Hardware
- **Meta Quest 2/3/Pro** – for Android/Oculus builds
- **iOS device with LiDAR** – for ARKit builds
- **Development PC/Mac** – Windows 10+ or macOS 12+

### Automatic Package Installation
Unity automatically installs all required packages when you open the project. The `Packages/manifest.json` includes:
- TextMesh Pro
- XR Plugin Management
- AR Foundation (iOS)
- ARKit XR Plugin (iOS)
- Oculus XR Plugin (Meta Quest)

**You don't need to manually install packages!** Unity handles this automatically.

---

## 🚀 Quick Start 

### 1. Clone the Repository

```bash
git clone https://github.com/LeonK381/VisionFramework.git
cd VisionFramework
```

### 2. Open in Unity

1. Open **Unity Hub**
2. Click **Open** → Select the `VisionFramework` folder
3. Unity 6000.0.61f1 will automatically:
   - Install all required packages
   - Configure XR settings

### 3. Load the Demo Scene

The project includes a pre-configured demo scene:
- **Assets/VisionFramework/Scenes/VisionSampleScene.unity**

### 4. Select Your Platform & Build

#### **Option A: Meta Quest**

1. **File → Build Settings**
2. Switch platform to **Android** (if not already)
3. Connect Quest via USB, enable **Developer Mode**
4. Click **Build and Run**

**That's it!** No manual XR configuration needed.

#### **Option B: iOS/Vision Pro**

1. **File → Build Settings**
2. Switch platform to **iOS**
3. Click **Build**
4. Open generated Xcode project
5. Sign with your Apple Developer account
6. Deploy to device


