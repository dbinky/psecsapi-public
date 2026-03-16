# Combat Simulation Guide

## Overview

The PSECS CLI includes a local combat simulator that runs entirely offline — no API connection or authentication required. Use it to develop, test, and iterate on combat scripts before deploying them to your fleet in the live game.

This guide covers the simulation workflow, CLI commands, the combat visualizer, the damage model, and an autonomous testing loop for AI agents building combat scripts.

## CLI Commands

### `papi combat simulate`

Runs a local combat simulation between two fleets.

**Flags:**
| Flag | Alias | Description |
|------|-------|-------------|
| `--fleet1 <config>` | `-a` | Fleet 1 config: `preset:<name>` or path to JSON file (required) |
| `--fleet2 <config>` | `-d` | Fleet 2 config: `preset:<name>` or path to JSON file (required) |
| `--terrain <type>` | `-t` | Terrain/sector type (default: `void`) |
| `--seed <int>` | `-s` | Random seed for deterministic simulation |
| `--output <path>` | `-o` | Output file path for replay binary (default: `combat-replay.bin`) |
| `--visualize` | `-v` | Launch visualizer after simulation |
| `--list-presets` | | List available fleet presets and exit |
| `--list-terrains` | | List available terrain types and exit |

**Fleet config format:** Use `preset:<name>` to reference a built-in preset (e.g., `preset:balanced-trio`), or pass a path to a fleet JSON file. Use `--list-presets` to see all available presets.

**Example:**
```bash
papi combat simulate --fleet1 my-fleet.json --fleet2 preset:balanced-trio --terrain rubble --seed 42
```

**Output:** JSON on stdout with simulation results:
```json
{
  "outcome": "AttackerWon",
  "ticks": 1847,
  "durationSeconds": 92.35,
  "fleet1Surviving": 2,
  "fleet2Surviving": 0,
  "seed": 42,
  "replayFile": "/path/to/combat-replay.bin",
  "replayBytes": 45632
}
```

The `outcome` field is one of: `AttackerWon`, `DefenderWon`, `Draw`, `TimedOut`.

### `papi combat export-fleet <fleet-id>`

Exports a real fleet from the API as a simulation-compatible JSON config file. Requires authentication.

**Flags:**
| Flag | Alias | Description |
|------|-------|-------------|
| `--output <path>` | `-o` | Output file path (default: `fleet-{id}.json`) |

**Example:**
```bash
papi combat export-fleet a1b2c3d4-e5f6-7890-abcd-ef1234567890 -o my-fleet.json
```

The exported JSON contains ship loadouts (chassis, weapons, modules, cargo) and can include script references. Edit the `scriptFile` field to point to your `.js` combat script.

**Fleet config JSON structure:**
```json
{
  "ships": [
    {
      "name": "Heavy Cruiser",
      "chassis": "cruiser-mk2",
      "weapons": ["Kinetic Cannon", "Energy Beam"],
      "modules": [
        {"type": "Shield Generator", "slot": "Interior"},
        {"type": "Armor Plating", "slot": "Exterior"}
      ],
      "cargo": 0
    }
  ],
  "script": null,
  "scriptFile": "my-script.js"
}
```

Set `scriptFile` to the path of your `.js` combat script, or set `script` to a built-in name (`aggressive`, `defensive`, `flee`).

### `papi combat visualize <replay-file>`

Opens a browser-based combat replay viewer. The visualizer renders the full battle on a 2D canvas with real-time ship positions, weapon fire, damage numbers, and per-module system status for every ship.

**Flags:**
| Flag | Alias | Description |
|------|-------|-------------|
| `--port <int>` | `-p` | Port for local HTTP server (0 = auto-assign) |
| `--no-browser` | | Do not open browser automatically |

**Example:**
```bash
papi combat visualize combat-replay.bin
```

**Shortcut:** Use `--visualize` / `-v` on the simulate command to generate a replay and launch the visualizer in one step:
```bash
papi combat simulate --fleet1 my-fleet.json --fleet2 preset:balanced-trio -v
```

#### Visualizer Controls

| Key | Action |
|-----|--------|
| Space | Play / Pause |
| Arrow Right | Step forward one tick |
| Arrow Left | Step back one tick |
| Home | Jump to start |
| End | Jump to end |
| 1-9 | Set playback speed (1x through 32x) |

The visualizer layout has three columns: Fleet 1 stats on the left, the combat canvas in the center, and Fleet 2 stats on the right. Click any ship tile in the stats panels to highlight that ship on the canvas.

## Damage Pipeline

Understanding how damage flows through ships is critical for writing effective combat scripts and choosing the right module loadouts.

### The Damage Stages

Every weapon hit passes through four stages in order:

1. **Shields** — Energy resistance modules provide shield effectiveness as a ratio (0.0 to 1.0), not an HP pool. Shields absorb a percentage of incoming damage proportional to their current effectiveness. As shield modules take condition damage, effectiveness degrades.

2. **Armor** — Kinetic resistance modules provide armor effectiveness the same way. Armor absorbs a portion of whatever damage passes through shields. Like shields, armor degrades as its modules lose condition.

3. **Structure** — Any damage that passes through both shields and armor hits the ship's structure HP directly. When structure reaches zero, the ship is destroyed.

4. **Random Module Hit** — Each hit that deals structure damage also has a chance to damage a random module on the ship. The hit reduces that module's condition percentage. This is where cascading failures begin.

### Module Condition System

Every module on a ship has a **condition** percentage (0-100%). Condition represents how functional the module is. When a module takes a hit, its condition drops. At 0% condition, the module is destroyed and provides no benefit.

**This creates cascading failures:** A hit to a reactor module reduces power output, which degrades all powered systems. A hit to a shield module reduces shield effectiveness, letting more damage through on subsequent hits. A hit to an engine module reduces speed, making the ship easier to hit.

### System Status Indicators

The visualizer tracks module condition with single-letter status indicators for each system category:

| Indicator | System | What It Does |
|-----------|--------|-------------|
| **S** | Shields | Energy/quantum resistance — absorbs incoming damage |
| **A** | Armor | Kinetic resistance — absorbs damage after shields |
| **E** | Engines | Speed — affects maneuverability and escape |
| **W** | Weapons | Energy/kinetic/quantum damage output |
| **R** | Reactor | Power generation — feeds all other powered systems |
| **N** | Sensors | Detection range and targeting accuracy |
| **C** | Compute | Script execution capacity |

Each indicator shows the **worst condition** among modules in that category. Color coding:
- **Healthy** (50-100%) — normal color
- **Degraded** (1-49%) — dimmed
- **Destroyed** (0%) — marked with "X" suffix

### Power Delivery Cascade

The reactor is the most critical system. When reactor modules take condition damage, reduced power generation cascades to all powered systems. A ship with a crippled reactor loses shield effectiveness, weapon output, engine power, and sensor range simultaneously — even if those modules are individually undamaged.

**Script implication:** If your script can identify an enemy ship with reactor damage (via reduced speed or weapon output), focus fire on it. A ship in a power cascade is far easier to destroy than its remaining HP might suggest.

## Terrain Types

Different terrain types create different tactical environments. Test your scripts across all of them.

| Type | Description |
|------|-------------|
| `void` | Empty space. No obstacles. Pure ship-vs-ship. |
| `rubble` | Asteroid debris fields. Ships must navigate around obstacles. |
| `nebula` | Nebula clouds affecting visibility and movement. |
| `star-system` | Star(s) with gravitational and heat effects near the center. Use `starsystem` (no hyphen) in the CLI. |
| `black-hole` | Massive gravity well at center. Ships near it take environmental damage. |

**Note:** In the CLI `--terrain` flag, use `starsystem` (no hyphen) and `blackhole` (no hyphen).

## Script Authoring Reference

Combat scripts are JavaScript files with two lifecycle functions. See the `psecs://guide/combat-scripting` resource for the full API reference. Quick summary:

### Lifecycle
- `onStart(state)` — Called once when combat begins for each ship. Use for initial setup.
- `onTick(state)` — Called on each of the ship's compute ticks. Issue movement and firing commands.

### Available APIs

**`commands.*`** — Ship control:
- `commands.thrust(angle, power)` — Newtonian thrust at angle (radians), power 0.0-1.0
- `commands.moveTo(x, y, speed)` — Auto-navigate to coordinates at speed
- `commands.stop()` — Kill all thrust, coast on current velocity
- `commands.fire(weaponId, targetId)` — Fire weapon at enemy ship by ID
- `commands.fireAt(weaponId, x, y)` — Fire weapon at coordinates (for leading shots)
- `commands.holdFire()` — Disable all weapon fire this tick
- `commands.flee()` — Head for nearest grid boundary at max speed

**`utils.*`** — Math helpers:
- `utils.distance(posA, posB)` — Euclidean distance between `{x, y}` objects
- `utils.angleTo(posA, posB)` — Angle from A to B in radians
- `utils.leadTarget(myShip, enemy, projectileSpeed)` — Predicts intercept point, returns `{x, y}`

**`state.*`** — Read-only game state:
- `state.myShip` — Your ship (position, velocity, weapons, shields, structure, etc.)
- `state.myFleet` — All friendly ships (position, velocity, structure, mass)
- `state.enemyFleet` — All visible enemy ships (position, velocity, structure, mass)
- `state.terrain` — Grid obstacles (position, radius, type)
- `state.grid` — Grid boundaries (width, height, minX, minY, maxX, maxY)
- `state.tick` — Current simulation tick number

### Constraints
- **10,000 steps** per tick execution (prevents infinite loops)
- **4MB memory** limit per script engine
- **16KB per compute capacity point** script size limit
- **No external access** — no network, filesystem, or system calls
- **Syntax error** in script: ship defaults to flee for entire combat
- **Runtime error** in tick: that tick's commands discarded, ship continues with previous commands

## Tips for Script Authors

### Checking Module Conditions

Your script receives module status via `state.myShip`. Key things to monitor:

- **Your own shield/armor effectiveness** — if these are dropping, you are taking module hits. Consider retreating to preserve remaining systems.
- **Enemy behavior changes** — a ship that suddenly slows down likely has engine or reactor damage. Focus fire on it.
- **Reactor cascade awareness** — if your reactor is damaged, all your systems degrade. A script that detects its own reactor damage and switches to evasive tactics will survive longer than one that fights to the last hit point.

### Simulation Workflow for Script Development

1. **Start with `--seed` for determinism** — same seed produces the same combat, so you can isolate the effect of script changes.
2. **Use `--visualize` to watch the replay** — seeing the battle helps identify script weaknesses (ships flying into obstacles, ignoring flanking enemies, etc.).
3. **Check module damage patterns** — if your ships consistently lose shields first, you may need better positioning to reduce incoming fire.
4. **Test across terrain types** — a script that dominates in void may crash into asteroids in rubble terrain.
5. **Test across seeds** — a script that wins with seed 42 might lose with seed 43 due to different initial positions.

## Autonomous Testing Loop

AI agents can use this workflow to iteratively develop and optimize combat scripts without human intervention:

### Step-by-Step Process

```
1. EXPORT FLEET
   papi combat export-fleet <fleet-id> -o my-fleet.json
   # Gets your real fleet's ship loadouts as simulation input

2. WRITE SCRIPT
   # Save a .js file with onStart() and onTick() functions
   # Start simple — target closest enemy, fire all weapons, close distance

3. SIMULATE
   papi combat simulate \
     --fleet1 my-fleet.json \
     --fleet2 preset:balanced-trio \
     --terrain void \
     --seed 42

4. PARSE RESULT
   # Read the JSON from stdout
   # Check "outcome" field: AttackerWon, DefenderWon, Draw, TimedOut
   # Check fleet1Surviving and fleet2Surviving counts

5. ITERATE
   # Modify script based on results
   # Re-run with SAME seed for deterministic comparison
   # A change that wins with seed 42 might lose with seed 43

6. VISUALIZE (optional)
   papi combat visualize combat-replay.bin
   # Watch the replay to identify positioning issues, wasted fire, etc.
   # Check the system status indicators (S/A/E/W/R/N/C) to see
   # which modules are getting hit and when cascading failures start

7. TEST ACROSS TERRAINS
   # Run against each terrain type:
   for terrain in void rubble nebula starsystem blackhole; do
     papi combat simulate \
       --fleet1 my-fleet.json \
       --fleet2 preset:balanced-trio \
       --terrain $terrain \
       --seed 42
   done

8. TEST ACROSS OPPONENTS
   # List available presets:
   papi combat simulate --list-presets
   # Test against each relevant preset

9. TEST ACROSS SEEDS
   # Use multiple seeds to check consistency:
   for seed in 42 123 456 789 1000; do
     papi combat simulate \
       --fleet1 my-fleet.json \
       --fleet2 preset:balanced-trio \
       --terrain void \
       --seed $seed
   done

10. DEPLOY
    # Once satisfied with win rate, deploy to live game:
    papi combat script create --name "Optimized Script v1" --file my-script.js
    # Note the returned script ID

11. ASSIGN
    papi combat script assign <fleet-id> <script-id>
    # Fleet will now use this script in real combat engagements
```

### Iteration Strategy

**Start simple, add complexity:**
1. First script: target closest enemy, fire all weapons, close to firing range
2. Add kiting: maintain optimal range based on weapon range
3. Add target selection: focus fire on weakest or most dangerous enemy
4. Add lead targeting: use `utils.leadTarget()` for moving enemies
5. Add terrain awareness: check `state.terrain` to avoid obstacles
6. Add fleet coordination: use `state.myFleet` to avoid friendly fire or coordinate focus
7. Add damage awareness: monitor your own module conditions and adapt tactics

**Evaluating results:**
- Win rate across 10+ seeds matters more than any single seed result
- A script that wins 8/10 against one preset but 2/10 against another needs terrain/opponent adaptation
- Check `fleet1Surviving` — winning with all ships intact is better than a pyrrhic victory
- Compare `ticks` — faster wins mean more decisive scripts
- Use the visualizer to check if ships are taking unnecessary module damage

**Common pitfalls:**
- Scripts that only work in `void` terrain may crash into obstacles in `rubble`
- Hard-coded weapon IDs break when ship loadouts change — iterate over `state.myShip.weapons` instead
- Ignoring `state.grid` boundaries leads to ships flying off-grid and being eliminated
- Not handling the "no enemies left" case causes errors on the final ticks
- Ignoring module condition — a ship with 10% structure but healthy reactor is more dangerous than one with 50% structure and a destroyed reactor
