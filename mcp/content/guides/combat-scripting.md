# Combat Scripting API Reference

## Overview
Combat scripts are JavaScript programs that control how your ships behave during fleet-vs-fleet combat.
Each fleet can have one combat script assigned. The script runs independently for each ship in the fleet.

**Without a script, ships default to flee behavior** (moving toward the grid boundary every tick).
Writing even a basic script gives you a massive advantage over unscripted opponents.

## Script Lifecycle
1. **Create** a script in your corp's library: `psecs_raw_create_corp_scripts`
   - Body: `{"name": "My Script", "source": "<javascript code>"}`
2. **Assign** the script to a fleet: `psecs_raw_update_fleet_combat_script`
   - Body: `{"scriptId": "<script-guid>"}`
3. **Engage** combat: `psecs_engage_combat`
4. During combat, the engine calls `onStart(state)` once, then `onTick(state)` each tick
5. **Review** results: `psecs_combat_summary`, `psecs_raw_combat_replay`

## Script Structure
```javascript
// Called once at the start of combat for each ship
function onStart(state) {
  // Initial setup — e.g., pick primary target
}

// Called every tick (many times per second)
function onTick(state) {
  // Read state, issue commands
  var enemies = state.enemies.filter(function(e) { return e.isAlive; });
  if (enemies.length > 0) {
    var target = enemies[0];
    var dist = utils.distance(state.myShip.position, target.position);
    commands.fire("weapon-0", target.id);
    if (dist > 200) {
      commands.moveTo(target.position.x, target.position.y, state.myShip.maxSpeed);
    }
  }
}
```

## State Object (`state`)
Passed to both `onStart` and `onTick`:

| Field | Type | Description |
|-------|------|-------------|
| `state.tick` | number | Current simulation tick number |
| `state.myShip` | object | Your ship's full state (see below) |
| `state.myFleet` | array | All friendly ships including yours (see fleet ship fields) |
| `state.enemyFleet` | array | All visible enemy ships (see fleet ship fields) |
| `state.terrain` | array | Terrain obstacles on the grid (see terrain fields) |
| `state.grid` | object | Grid dimensions (see grid fields) |

### `state.myShip`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Ship ID |
| `position` | `{x, y}` | Current position on the combat grid |
| `velocity` | `{x, y}` | Current velocity vector |
| `facing` | number | Current facing angle in radians |
| `speed` | number | Current speed |
| `maxSpeed` | number | Maximum speed |
| `maxAcceleration` | number | Maximum acceleration |
| `structurePoints` | number | Current structure HP |
| `maxStructurePoints` | number | Maximum structure HP |
| `shieldEffectiveness` | number | Shield damage reduction (0.0-1.0) |
| `armorEffectiveness` | number | Armor damage reduction (0.0-1.0) |
| `weapons` | array | Weapon modules (see weapon fields) |
| `modules` | array | All modules (see module fields) |
| `computeCapacity` | number | Ship's compute capacity |
| `mass` | number | Ship mass |
| `cargo` | array | Cargo entries `{assetId, type}` |

### `state.myShip.weapons[]`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Weapon module ID (use with `commands.fire`) |
| `type` | string | Damage type (e.g., "Kinetic", "Energy") |
| `damage` | number | Base damage per shot |
| `range` | number | Maximum firing range |
| `cooldownTicks` | number | Ticks between shots |
| `coneAngle` | number | Firing arc angle |
| `condition` | number | Module condition (0.0-1.0) |

### `state.myShip.modules[]`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Module ID |
| `name` | string | Module name |
| `condition` | number | Module condition (0.0-1.0) |
| `capabilities` | string[] | Capability types |

### `state.myFleet[]` and `state.enemyFleet[]`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Ship ID |
| `position` | `{x, y}` | Current position |
| `velocity` | `{x, y}` | Current velocity vector |
| `facing` | number | Current facing angle in radians |
| `structurePoints` | number | Current structure HP |
| `maxStructurePoints` | number | Maximum structure HP |
| `mass` | number | Ship mass |

### `state.terrain[]`
| Field | Type | Description |
|-------|------|-------------|
| `x` | number | Obstacle X position |
| `y` | number | Obstacle Y position |
| `radius` | number | Obstacle radius |
| `type` | string | Obstacle type |

### `state.grid`
| Field | Type | Description |
|-------|------|-------------|
| `width` | number | Grid width |
| `height` | number | Grid height |
| `minX` | number | Minimum X boundary |
| `minY` | number | Minimum Y boundary |
| `maxX` | number | Maximum X boundary |
| `maxY` | number | Maximum Y boundary |

## Commands API (`commands`)
Issue commands to control your ship. Multiple commands per tick are allowed.

| Command | Signature | Description |
|---------|-----------|-------------|
| `thrust` | `commands.thrust(angle, power)` | Apply thrust at angle (radians) with power (0.0-1.0) |
| `moveTo` | `commands.moveTo(x, y, speed)` | Navigate to coordinates at given speed |
| `stop` | `commands.stop()` | Kill all velocity |
| `fire` | `commands.fire(weaponId, targetId)` | Fire weapon at a target ship by ID |
| `fireAt` | `commands.fireAt(weaponId, x, y)` | Fire weapon at coordinates (for leading targets) |
| `holdFire` | `commands.holdFire()` | Stop all weapons from firing |
| `flee` | `commands.flee()` | Move toward the nearest grid boundary at max speed |

## Utility Functions (`utils`)

| Function | Signature | Description |
|----------|-----------|-------------|
| `distance` | `utils.distance(posA, posB)` | Euclidean distance between two `{x, y}` positions |
| `angleTo` | `utils.angleTo(posA, posB)` | Angle from posA to posB in radians |
| `leadTarget` | `utils.leadTarget(myShip, enemy, projectileSpeed)` | Predicts intercept point, returns `{x, y}` |

## Sandbox Constraints
- **Step limit**: 10,000 Jint execution steps per tick (prevents infinite loops). If exceeded, commands issued before the limit are still executed.
- **Memory limit**: 4MB allocation limit per script engine instance
- **Script size limit**: 16KB per compute capacity point on the ship (a ship with compute capacity 2 allows 32KB scripts)
- **No external access**: No network, filesystem, or .NET CLR access — the Jint sandbox blocks all system calls
- **Error handling**:
  - **Syntax errors** during script parsing: ship falls back to default flee behavior for the entire combat
  - **Runtime errors** during `onTick`/`onStart`: that tick's commands are discarded; ship continues with last successful commands
  - **Memory limit exceeded**: tick's commands are discarded
  - **Step limit exceeded**: commands issued before the limit are kept
- Scripts persist across ticks — variables set in `onStart` are available in `onTick`
- The Jint engine is reused across ticks for the same ship, preserving script-level state

## Combat Script Management Tools
- `psecs_raw_corp_scripts` — List all scripts in your corp library
- `psecs_raw_create_corp_scripts` — Create a new script (body: `{"name": "...", "source": "..."}`)
- `psecs_raw_corp_scripts_by_script` — Get a specific script with full source
- `psecs_raw_update_corp_scripts` — Update a script's name and source
- `psecs_raw_delete_corp_scripts` — Delete a script
- `psecs_raw_update_fleet_combat_script` — Assign a script to a fleet (body: `{"scriptId": "..."}`)
- `psecs_raw_delete_fleet_combat_script` — Remove script assignment from a fleet

## Example Scripts

### Aggressive Kiter (maintain distance, fire continuously)
```javascript
function onTick(state) {
  var enemies = state.enemies.filter(function(e) { return e.isAlive; });
  if (enemies.length === 0) return;

  // Target closest enemy
  var closest = enemies[0];
  var minDist = utils.distance(state.myShip.position, closest.position);
  for (var i = 1; i < enemies.length; i++) {
    var d = utils.distance(state.myShip.position, enemies[i].position);
    if (d < minDist) { minDist = d; closest = enemies[i]; }
  }

  // Fire at predicted position
  var lead = utils.leadTarget(state.myShip, closest, 500);
  commands.fireAt("weapon-0", lead.x, lead.y);

  // Maintain optimal range (150-250 units)
  if (minDist < 150) {
    // Too close — thrust away
    var awayAngle = utils.angleTo(closest.position, state.myShip.position);
    commands.thrust(awayAngle, 1.0);
  } else if (minDist > 250) {
    // Too far — close in
    commands.moveTo(closest.position.x, closest.position.y, state.myShip.maxSpeed * 0.7);
  }
}
```

### Focus Fire (all weapons on weakest target)
```javascript
function onTick(state) {
  var enemies = state.enemies.filter(function(e) { return e.isAlive; });
  if (enemies.length === 0) return;

  // Pick the enemy closest to death (lowest HP is not exposed, so pick closest)
  var target = enemies[0];
  var minDist = utils.distance(state.myShip.position, target.position);
  for (var i = 1; i < enemies.length; i++) {
    var d = utils.distance(state.myShip.position, enemies[i].position);
    if (d < minDist) { minDist = d; target = enemies[i]; }
  }

  // Close to firing range and fire
  commands.moveTo(target.position.x, target.position.y, state.myShip.maxSpeed);
  commands.fire("weapon-0", target.id);
}
```
