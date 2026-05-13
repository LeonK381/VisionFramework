# Vision Framework

Unity-basiertes Framework zur Echtzeit-Simulation von Sehbeeinträchtigungen in Mixed-Reality-Umgebungen mit **adaptiven Kompensationsstrategien**. Entwickelt für Barrierefreiheitsforschung, Empathie-Training und Lehre der Sehwissenschaften.

## 🎯 Überblick

Dieses Framework ermöglicht es Forschern, Designern und Pädagogen, Sehbeeinträchtigungen durch MR-Headsets zu erleben. Es bietet wissenschaftlich validierte Simulationen häufiger Sehstörungen mit **Echtzeit-adaptiver Kompensation**, die sich basierend auf interaktiven Sehtests anpasst.

### Unterstützte Plattformen
- **Meta Quest** (Android/Oculus)
- **Apple iPhone** / iOS (ARKit + LiDAR)

### Simulierte Sehbeeinträchtigungen
- **Myopie** (Kurzsichtigkeit)
- **Hyperopie** (Weitsichtigkeit)
- **Astigmatismus** (Stabsichtigkeit)
- **Katarakt** (Grauer Star)
- **Glaukom** (Grüner Star)
- **Protanopie** (Rot-Grün-Blindheit)
- **Makuladegeneration** (AMD)
- **Diabetische Retinopathie**
- **Retinitis Pigmentosa**
- **Glaskörpertrübungen** (Mouches volantes)

### Hauptfunktionen
- ✅ **Interaktive Analyzer** – Wissenschaftliche Sehtests (Landolt-C, Ishihara, M-Charts, Humphrey Field)
- ✅ **Adaptives Kompensationssystem** – Mehrere Strategien, die sich an gemessene Beeinträchtigungsschwere anpassen
- ✅ **Sensorintegration** – Eye-Tracking, Tiefenerkennung (LiDAR), Kopfbewegungstracking
- ✅ **Modulare Architektur** – Plug-and-Play Effekt-System mit ScriptableObject-Profilen
- ✅ **Erweiterbarkeit** – Einfaches Hinzufügen eigener Effekte, Analyzer und Kompensationsstrategien
- ✅ **Profilsystem** – Speichern/Laden von Sehbeeinträchtigungs-Konfigurationen
- ✅ **Echtzeit-Performance**

---

## 📋 Anforderungen

### Unity Version
- **Unity 6000.0.61f1** (Unity 6)

### Hardware
- **Meta Quest 2/3/Pro** – für Android/Oculus Builds
- **iOS-Gerät mit LiDAR** – für ARKit Builds
- **Entwicklungs-PC/Mac** – Windows 10+ oder macOS 12+

### Automatische Package-Installation
Unity installiert beim Öffnen des Projekts automatisch alle benötigten Packages. Die `Packages/manifest.json` enthält:
- TextMesh Pro
- XR Plugin Management
- AR Foundation (iOS)
- ARKit XR Plugin (iOS)
- Oculus XR Plugin (Meta Quest)

**Sie müssen keine Packages manuell installieren!** Unity erledigt dies automatisch.

---

## 🚀 Schnellstart

### 1. Repository klonen

```bash
git clone https://github.com/LeonK381/VisionFramework.git
cd VisionFramework
```

### 2. In Unity öffnen

1. **Unity Hub** öffnen
2. **Öffnen** klicken → `VisionFramework`-Ordner auswählen
3. Unity 6000.0.61f1 wird automatisch:
   - Alle benötigten Packages installieren
   - XR-Einstellungen konfigurieren

### 3. Demo-Szene laden

Das Projekt enthält eine vorkonfigurierte Demo-Szene:
- **Assets/VisionFramework/Scenes/VisionSampleScene.unity**

### 4. Plattform auswählen & Build erstellen

#### **Option A: Meta Quest**

1. **Datei → Build-Einstellungen**
2. Plattform zu **Android** wechseln (falls noch nicht geschehen)
3. Quest via USB verbinden, **Entwicklermodus** aktivieren
4. **Build and Run** klicken

**Das war's!** Keine manuelle XR-Konfiguration erforderlich.

#### **Option B: iOS**

1. **Datei → Build-Einstellungen**
2. Plattform zu **iOS** wechseln
3. **Build** klicken
4. Generiertes Xcode-Projekt öffnen
5. Mit Apple Developer Account signieren
6. Auf Gerät deployen
