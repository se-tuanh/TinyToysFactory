# 🧸 Tiny Toys Factory — Unity Project

PRU213 Game Programming | Manufacture Simulation

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs         ← Singleton: state, timer, credits, reputation
│   │   ├── ResourceManager.cs     ← Materials, Power, Workers
│   │   ├── ProductionManager.cs   ← Queue-based Process A & B pipeline
│   │   ├── OrderManager.cs        ← Orders, shipping, win detection
│   │   └── PressureDirector.cs    ← Difficulty tier (1-5), random event scheduler
│   ├── Data/  (ScriptableObjects)
│   │   ├── ProductData.cs         ← Define Toy Car / Robot / Doll
│   │   ├── OrderData.cs           ← Define Toy Kingdom / Prestige / Flash orders
│   │   └── RandomEventData.cs     ← Define events (Breakdown, Shortage, Surge…)
│   ├── Gameplay/
│   │   ├── Machine.cs             ← Click to produce; shows visual state
│   │   └── Worker.cs              ← Fatigue + rest mechanic
│   └── UI/
│       ├── UIManager.cs           ← HUD: timer, bars, tier indicators
│       └── EventPopupUI.cs        ← Decision Triad popup (Prioritize/Repair/Trade)
├── ScriptableObjects/
│   ├── Products/                  ← Put .asset files for each product here
│   ├── Orders/                    ← Put .asset files for each order here
│   └── Events/                    ← Put .asset files for each event here
├── Prefabs/
│   ├── Machines/
│   └── UI/
├── Scenes/
│   └── GameScene.unity            ← Main gameplay scene
├── Art/Sprites/
└── Audio/
```

---

## 🚀 Setup Steps (Unity 2022 LTS or newer)

### Step 1 — Create Unity Project
1. Open **Unity Hub** → New Project
2. Select **2D (URP)** template
3. Name: `TinyToysFactory`
4. **Copy all files** from this folder into the new project's `Assets/` folder

### Step 2 — Install TextMeshPro
- **Window → Package Manager → TextMeshPro** → Install
- When prompted, import TMP Essential Resources

### Step 3 — Create ScriptableObject Assets

#### Products (Assets/ScriptableObjects/Products/)
Right-click → Create → TinyToysFactory → ProductData

| Asset Name       | productName   | woodPlasticCost | assemblyTime | paintFabricCost | paintPackTime | baseCredits |
|------------------|---------------|-----------------|--------------|-----------------|---------------|-------------|
| Product_ToyCar   | Toy Car       | 10              | 8s           | 8               | 6s            | 30          |
| Product_Robot    | Robot         | 15              | 12s          | 12              | 10s           | 55          |
| Product_Doll     | Luxury Doll   | 20              | 18s          | 18              | 14s           | 90          |

For all: `workersRequired = 1`, `powerPerAssembly = 30`, `powerPerPaint = 20`

#### Orders (Assets/ScriptableObjects/Orders/)
Right-click → Create → TinyToysFactory → OrderData

| Asset Name           | faction      | Required Products       | deadline | creditsReward |
|----------------------|--------------|-------------------------|----------|---------------|
| Order_ToyKingdom1    | ToyKingdom   | 6× ToyCar, 3× Robot     | 180s     | 200           |
| Order_Prestige1      | PrestigePlay | 2× Doll                 | 240s     | 180           |
| Order_FlashDeal1     | FlashDeals   | 4× Robot                | 120s     | 220           |

#### Events (Assets/ScriptableObjects/Events/)
Right-click → Create → TinyToysFactory → RandomEventData

| Asset Name             | eventType           | minTier | Prioritize → effect        |
|------------------------|---------------------|---------|----------------------------|
| Event_MachineBreakdown | MachineBreakdown    | 1       | Repair: halt 20s           |
| Event_MaterialShortage | MaterialShortage    | 1       | Trade: -50 credits         |
| Event_BulkOrderSpike   | BulkOrderSpike      | 2       | Accept/Decline choice      |
| Event_PowerSurge       | PowerSurge          | 2       | Redistribute or halt       |
| Event_WorkerExhaustion | WorkerExhaustion    | 3       | Rest or risk broken output |

### Step 4 — Scene Setup

#### GameObjects to create in GameScene:
```
Scene Hierarchy:
├── [Manager]              (Empty GameObject)
│   ├── GameManager        (Add GameManager.cs)
│   ├── ResourceManager    (Add ResourceManager.cs)
│   ├── ProductionManager  (Add ProductionManager.cs)
│   ├── OrderManager       (Add OrderManager.cs → assign availableOrders)
│   └── PressureDirector   (Add PressureDirector.cs → assign eventPool)
│
├── [Machines]
│   ├── AssemblyMachineA   (Sprite + Machine.cs, type=AssemblyA, assign product)
│   └── PaintMachineB      (Sprite + Machine.cs, type=PaintPackB, assign product)
│
├── [Workers]
│   ├── Worker1            (Sprite + Worker.cs)
│   ├── Worker2            (Sprite + Worker.cs)
│   └── Worker3            (Sprite + Worker.cs)
│
└── Canvas (UI)
    ├── HUD                (Add UIManager.cs, wire all text/image references)
    └── EventPopup         (Add EventPopupUI.cs, wire buttons + texts, SetActive=false)
```

### Step 5 — Wire References in Inspector
- **OrderManager**: drag Order .asset files into `availableOrders` list
- **PressureDirector**: drag Event .asset files into `eventPool` list
- **UIManager**: drag all Text, Image, Button references from Canvas
- **EventPopupUI**: drag panel, buttons, texts

---

## 🎮 Core Game Flow

```
Start → GameManager.StartGame()
     → OrderManager.AutoAcceptNextOrder()
     → Player clicks Machine → StartBatchA()
     → ProductionManager processes A then B
     → OnBatchCompletedB → OrderManager tracks progress
     → PressureDirector fires random events
     → EventPopupUI shows Triad → player chooses
     → All products shipped → GameManager.TriggerWin()
```

---

## 📋 Script Dependencies

```
GameManager     ← (no deps — Singleton root)
ResourceManager ← GameManager
ProductionManager ← ResourceManager
OrderManager    ← ProductionManager, PressureDirector, GameManager
PressureDirector← ProductionManager, GameManager
UIManager       ← All above
EventPopupUI    ← PressureDirector
Machine         ← ProductionManager
Worker          ← ResourceManager, GameManager
```

> **Important:** Add all Manager scripts to the scene **before** hitting Play.  
> All managers use `Instance` Singleton pattern — only one of each should exist.

---

## 👥 Team Task Division

| Member | Files to own |
|--------|-------------|
| A (Designer) | ProductData, OrderData, RandomEventData (tuning numbers) |
| B (Core Dev) | ProductionManager, OrderManager, PressureDirector |
| C (UI Dev) | UIManager, EventPopupUI |
| D (Art/Level) | Machine sprites, Worker sprites, Scene layout |

---

*PRU213 — Tiny Toys Factory | March 2026*
