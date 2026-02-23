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
| `state.myShip` | object | Your ship's current state (see below) |
| `state.enemies` | array | All enemy ships (see below) |
| `state.friendlies` | array | Your other fleet ships (see below) |

### `state.myShip`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Ship ID |
| `position` | `{x, y}` | Current position on the combat grid |
| `velocity` | `{x, y}` | Current velocity vector |
| `facing` | number | Current facing angle in radians |
| `speed` | number | Current speed |
| `maxSpeed` | number | Maximum speed |
| `structurePoints` | number | Current HP |
| `maxStructurePoints` | number | Maximum HP |
| `shieldEffectiveness` | number | Shield damage reduction (0.0-1.0) |
| `armorEffectiveness` | number | Armor damage reduction (0.0-1.0) |
| `isAlive` | boolean | Whether this ship is still alive |

### `state.enemies[]` and `state.friendlies[]`
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Ship ID |
| `position` | `{x, y}` | Current position |
| `velocity` | `{x, y}` | Current velocity (enemies only) |
| `isAlive` | boolean | Whether this ship is alive |

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
- **Step limit**: Scripts are limited in execution steps per tick (prevents infinite loops)
- **Memory limit**: 4MB allocation limit
- **No external access**: No network, filesystem, or .NET CLR access
- **Error handling**: Syntax errors or runtime exceptions cause the ship to fall back to auto-fire + flee
- Scripts persist across ticks — variables set in `onStart` are available in `onTick`

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
