# 🚀 Asteroid Dodger

A 2D arcade space game built in Unity where you pilot a spaceship through an endless field of falling asteroids. Survive as long as possible, collect power-ups, and compete for the highest score.

---

## 📱 Gameplay

- **Move** your ship left and right to dodge incoming asteroids
- **Survive** as long as possible — one hit ends the run
- **Collect power-ups** that drop randomly during gameplay
- **Score** increases every 0.1 seconds you stay alive
- **Three difficulty levels** with distinct speed and spawn pressure

---

## ✨ Features

| Feature | Description |
|---|---|
| 3 Difficulty Levels | Easy, Medium, and Hard — tuned via ScriptableObject configs |
| Trail Customization | 12 trail color presets, selected before each game, persisted via PlayerPrefs |
| Power-Up System | Shield, Slow-Mo, and Score Boost drop randomly during gameplay |
| Cloud Firestore Backend | High score, trail color, games played, and asteroids dodged saved per device |
| Screen Shake | Camera shake on death for cinematic impact |
| Particle Explosions | Full VFX explosion on player death |
| Custom Sprites | All UI and game sprites authored as scalable SVGs |

---

## 🎮 Power-Ups

| Power-Up | Duration | Effect |
|---|---|---|
| 🛡️ Shield | 5s | Absorbs the next asteroid hit — ship flashes on impact |
| 🌀 Slow-Mo | 4s | Drops time scale to 0.3x — asteroids crawl |
| ⚡ Score Boost | 6s | Doubles score tick rate for the duration |

---

## 🏗️ Architecture

The project is built around a clean event-driven architecture with three scenes managed by a persistent `GameManager` singleton.

```
MainMenu → CustomizationScene → SampleScene
```

### Design Patterns Used

- **Singleton** — `GameManager` and `FirebaseManager` persist across scene loads
- **Object Pool** — Asteroids pre-warmed (x15) and recycled, no GC spikes
- **Observer** — C# events for cross-script communication, no direct dependencies
- **ScriptableObject** — `DifficultyConfig` and `PowerUpConfig` as data containers



## 🔧 Tech Stack

- **Engine** — Unity 2022/2023 (2D URP)
- **Language** — C#
- **Input** — Unity Input System (new)
- **Backend** — Cloud Firestore (Firebase Unity SDK)
- **Version Control** — Git + GitHub with LFS for binary assets

---

## 🚀 Getting Started

### Prerequisites

- Unity 2022.3 LTS or later
- Unity Input System package installed
- Firebase Unity SDK (FirebaseAnalytics + FirebaseFirestore)
- A `google-services.json` file from your Firebase project

### Setup

1. Clone the repo

```bash
git clone https://github.com/FirasAhmed2/astroid.git
cd astroid
```

2. Open the project in Unity Hub

3. Place your `google-services.json` inside `Assets/`

4. Open `MainMenu` scene and hit Play

> The game loads Login first. Make sure all four scenes are added to **File → Build Settings** in this order:
> - `Login` (index 0)
> - `MainMenu` (index 1)
> - `CustomizationScene` (index 2)
> - `SampleScene` (index 3)

### Firebase Setup

1. Create a project at [console.firebase.google.com](https://console.firebase.google.com)
2. Add an Android/iOS app and download `google-services.json`
3. Enable **Cloud Firestore** in your Firebase project
4. Set security rules to allow read/write per device ID

```json
{
  "rules": {
    "players": {
      "$deviceId": {
        ".read": true,
        ".write": true
      }
    }
  }
}
```

---

## 🗄️ Database Schema

Each player is identified by `SystemInfo.deviceUniqueIdentifier`. Data is stored in a `players` Firestore collection.

```json
{
  "players": {
    "{deviceId}": {
      "highScore": 847,
      "trailColorIndex": 3,
      "totalGamesPlayed": 42,
      "totalAsteroids": 1205,
      "lastDifficulty": "Hard",
      "lastPlayed": "2026-04-10T18:32:00Z"
    }
  }
}
```

---

## 🎨 Difficulty Configuration

All difficulty values are ScriptableObject assets

| Setting | Easy | Medium | Hard |
|---|---|---|---|
| Player Speed | 8 u/s | 6 u/s | 5 u/s |
| Asteroid Speed | 3 u/s | 5 u/s | 8 u/s |
| Spawn Interval | 2.0s | 1.0s | 0.4s |

---

## 🌈 Trail Colors

12 preset trail colors selectable in the customization scene, saved to PlayerPrefs and synced to Firebase.

`Inferno` · `Solar` · `Venom` · `Arctic` · `Void` · `Rose` · `Glacier` · `Crimson` · `Ghost` · `Copper` · `Nebula` · `Default`

---

## 📁 Git LFS

This repo uses Git LFS for large binary files. Make sure LFS is installed before cloning:

```bash
git lfs install
git clone https://github.com/FirasAhmed2/astroid.git
```

Tracked extensions: `.bundle` `.so` 

> Note: Firebase native plugin files (`FirebaseCppApp-*.so`, `FirebaseCppApp-*.bundle`) are excluded from the repo via `.gitignore` as they exceed GitHub's 100MB limit. They are restored automatically when you import the Firebase SDK into Unity.

---

## 📄 License

This project was built as a Senior Year Project. All rights reserved.

---

## 👤 Author

**Firas Ahmed**
