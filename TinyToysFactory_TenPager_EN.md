# CASE STUDY: TINY TOYS FACTORY - TEN-PAGER v1

## Page 1 — Title, Tagline, High Concept, Audience

**Title:** Tiny Toys Factory
**Tagline:** Build fast. Manage smartly. Deliver on time.
**High Concept (2–3 sentences)**
You are a manager at a bustling toy factory, responsible for managing resources, workers, and a multi-stage production line under strict time pressure. Every shift presents random events that force you to make split-second decisions: repair broken machinery, reallocate power, or deal with material shortages to keep the assembly line running. The game delivers an intense experience of optimizing management and constantly resolving crises.

**Target Player**
* Players who enjoy Management Simulation and resource optimization.
* Players who like the feeling of time pressure and multitasking.
* Players who revel in "clutch moments" when successfully navigating a chain of bad events to deliver an order just in time.

**Platform & Controls**
* PC (Keyboard & Mouse / Point-and-Click).
* Accessibility baseline: Clear UI, an active Pause button to review the factory state, readable event telegraphs/alerts.

---

## Page 2 — Emotional Intent & Experience Pillars (with Design Rules)

**Emotional Intent (North Star)**
* **Tense:** Time pressure (deadlines) + depleting resources mean the player must always remain highly focused.
* **Satisfaction/Flow:** Seeing Line A (Assembly) transition smoothly to Line B (Paint & Pack) creates a huge sense of accomplishment ("Everything is running perfectly").

**We know we succeeded when…**
* Players sigh in relief or cheer when delivering a successful order at the last second.
* Roughly 70% of successful levels finish with less than 15% time remaining (maintaining the "tension band").

**3 Experience Pillars**
1. Multi-Resource Tension
2. Assembly-to-Packaging Pipeline Optimization
3. Crisis Decision Making

**Pillars → Design Rules (must-haves)**
* **P1 (Resources):** Materials, Power, and Worker Stamina (Fatigue) are never enough to run all machines at max capacity simultaneously.
* **P2 (Pipeline):** There is always a delay/wait time between stages (Assembly -> Paint). Players must anticipate and queue up production commands early to avoid bottlenecks.
* **P3 (Crisis):** Resolving random events always comes with a cost (Lose Time / Lose Resources / Lose Reputation). There is no "perfect" choice.

---

## Page 3 — Core Loop, Win/Lose, Mission Structure

**Core Loop (repeatable)**
Accept Order → Allocate Workers/Power → Run Machine A (Assembly) → Transfer to Machine B (Paint/Pack) → Resolve Random Event (Decision) → Deliver → Earn Credits/Reputation → Upgrade → Repeat.

**Win / Lose Conditions (MVP)**
* **Win:** Complete and deliver the required amount of Products (Car, Robot, Doll) to the partner (Toy Kingdom, Prestige Play...) before Time (Deadline) = 0.
* **Lose:** Run out of time before the order is complete, OR deplete Budget/Resources causing the production line to halt completely.

**Mission Structure (5–8 minute shifts)**
* Divided into 3-4 production Batches.
* **Pacing:** Starts smoothly (1-2 mins) → Alternating runs (2 mins) → Crisis/Event occurs (10-15s) → Final Sprint.

---

## Page 4 — Signature Mechanics (USP) & Game Identity

**Game Identity (one line)**
A factory time- and resource-management game focused on maintaining production tempo through resolving "decision spikes."

**Signature Mechanics (4 bullets)**
* **2-Stage Production Line (A & B):** Products must go through Assembly (costs 30 Power), then transfer to Paint & Pack (costs 20 Power). Requires precise timing.
* **Decision Triad Events:** Whenever a Random Event (e.g., Machine Breakdown) occurs, players choose 1 of 3 options: Pay for a quick fix / Stop machine for manual worker repair / Ignore and suffer slower production.
* **Worker Fatigue System:** Workers are not machines. They need rest. Forcing a worker to work continuously reduces production speed or causes errors.
* **Pressure Director:** An AI silently monitors player progress. The better you play, the faster the progress -> random events (Breakdowns, Shortages) become more frequent toward the end of the shift to test you.

**No-Go Rules**
* No free-form base-building. Machine placements are fixed to focus purely on management.
* No fully automated production cycles; players must always click to initiate product batches (Start Batch).

---

## Page 5 — Player Kit (Management & Tools)

**Management Kit (MVP)**
* **Assign:** Drag-and-drop or click to assign a Worker to Machine A, Machine B, or the Break Room.
* **Start Batch:** Click the run button when the material bin is full and a worker is present.
* **Power Routing:** Toggle power switches for different zones to route 100% capacity to the area that needs it most urgently.

**Crisis & Gadgets/Actions**
* **Coffee Break (Recovery):** Urgently send a worker to drink coffee to instantly reduce Fatigue.
* **Overdrive:** Boost machine speed by 150% but consume double Power and increase the breakdown rate for 10 seconds.
* **Emergency Supplies:** Instantly call in flying material drones when the warehouse is empty (costs significantly more than normal purchasing).

---

## Page 6 — Core Systems (States) & Pressure Director

**Core States (The Essential Five)**
1. **Time (Deadline):** The countdown determining win or loss.
2. **Materials:** Fuel for crafting toys.
3. **Power:** Total capacity limit (e.g., Max 50).
4. **Fatigue:** The stamina state of each Worker.
5. **Reputation:** Score used to unlock new levels/orders.

**Pressure Director System**
* **Pressure Increases:** When multiple machines run simultaneously, or when nearing the deadline of a Hot order (e.g., Flash Deals).
* **Pressure Tiers:**
  * T1: Minor material shortage, slight power flickers.
  * T3: Staff fatigues faster, orders demand sudden changes.
  * T5: Localized blackout, a machine totally breaks requiring an emergency fix.
* **What "Resource Tension" means here:** 
  "You have limited power. If you turn on both the Robot Assembly (high cost) and Doll Assembly (high cost), the system overloads. You are forced to temporarily pause the Packaging machine."

---

## Page 7 — Encounter Grammar: Decision Triad Events

**Encounter = "Decision Spike"**
Goal: Make a lightning-fast choice (< 5 seconds) when the alarm pop-up appears.

| Choice | Benefit | Cost | When it shines |
| :--- | :--- | :--- | :--- |
| **Pay/Fix Fast** | Machine restarts instantly, saving time | Costs a lot of Credits/Score | Near Deadline (Deadline priority) |
| **Manual Repair** | Costs no money, preserves profit margin | Machine halts for 10s, Worker loses stamina | Early in the shift with plenty of time padding |
| **Ignore/Trade-off** | Production line isn't delayed immediately | Output slows by 30% or Materials are lost | When hands are tied focusing heavily on another product |

**Telegraph Rules (Read in 1 second)**
* **Machine Breakdown:** Red alarm siren + electrical sparks flying from the Machine GameObject.
* **Exhausted Worker:** Giant sweat drop icon above the Worker + movement speed drops significantly.
* **Order Crisis:** Yellow UI flash on the Order Manager corner + emergency "Ping" audio.

---

## Page 8 — Progression & Upgrade Philosophy

**Progression Currencies**
* **Credits:** Buy new workers, buy upgrades in-level or between levels.
* **Reputation:** Unlock higher-tier Partners (Prestige Play).

**Upgrade Philosophy**
Each upgrade node must directly change player strategy:
* **Speed vs. Consumption:** Upgrade Assembly Machine A to run 20% faster, but it will require +10 Power.
* **HR Management:** Upgrade break room chairs to help Workers recover Fatigue twice as fast.

**3 Branches (Playstyle Identities)**
1. **WORKER-FOCUS:** Less Fatigue, faster movement, auto-repairs minor breakdowns.
2. **MACHINE-FOCUS:** Shortens process from 12s -> 8s (for Robots), can hold more finished goods in the queue.
3. **LOGISTICS-FOCUS:** Increases total Power cap, reduces material purchasing costs.

---

## Page 9 — Level Design, Client Identity, Mission Types

**Level Design Rules (MVP)**
* The pipeline spans vertically or left-to-right across the screen: Material Storage -> Machine A -> Intermediate Conveyor -> Machine B -> Shipping Area.
* UIManager (HUD) must always display 3 stats: Power, Materials, Time in the most visible locations.
* Onboarding: The first level only requires Toy Cars (simplest product) and has zero Random Events.

**Clients/Orders**
1. **Toy Kingdom:** Basic orders, demands a high volume of Toy Cars, relaxed time limits.
2. **Prestige Play:** Demands high-quality Dolls and Robots, massive monetary penalties if late.
3. **Flash Deals:** Lightning orders, extremely short deadlines but pays triple.

**Mission Types**
* **Standard Shift:** Produce a balanced mix of required goods.
* **Power Outage Shift:** Max Power cap is reduced by 30%. Forces the player to constantly micromanage (toggle on/off) alternating machines.
* **Rush Hour:** Zero wait time; all machines must run continuously to win.

---

## Page 10 — MVP Scope, Vertical Slice, Risks, Metrics

**MVP Scope Box (v1)**
* **In-scope:**
  * 1 Standard Level/Scene (`GameScene.unity`).
  * 3 Product types (Toy Car, Robot, Luxury Doll) connected via ScriptableObjects.
  * 2 Machine stages (A & B) and 1 Worker type.
  * Pressure Director System handling a minimum of 3 Random Events (Breakdown, Exhaustion, Shortage).
  * The accept - process - deliver loop.
* **Out-of-scope (for now):**
  * Free-form machine base-building, deep narrative, multi-factory management.

**Vertical Slice (5-minute Demo)**
* Must include:
  * One order consisting of 5 Toy Cars and 2 Robots.
  * Clearly demonstrating the need to route Power if both Machine A and Machine B are turned on at the same time.
  * At least 1 random Machine Breakdown event (triggering EventPopupUI) to validate the Decision Triad.

**Top Risks & Risk-First Prototype Plan**
* **Risk:** The UI is too cluttered regarding stats (Time, Power, Fatigue, Materials).
  * *Solution:* Minimalist HUD design, use color-coded icons (Red = Shortage, Green = Sufficient) instead of just numbers.
* **Risk:** Pacing becomes boring while waiting for goods to transfer from A to B.
  * *Solution:* Force the player to manage a Random Event or calculate the next batch requirements during machine runtime.

**Success Metrics (Playtest)**
* Decision Latency on Event pop-ups < 5 seconds.
* At least 60% of players experience the "just in time" feeling (finishing in the last 0-10 seconds).
* Failure reasons are clustered around "Out of Time" (deadlines) rather than "Frustration" (quitting mid-game due to misunderstanding rules).
