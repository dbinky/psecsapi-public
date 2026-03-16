// defensive-multi.js
// Strategy: Multi-ship coordinated defense.
// When flying solo, falls back to balanced engagement behavior. In a group,
// all friendlies focus-fire the closest enemy for fast kills. Maintains
// formation spread to avoid splash/overlap. Protects the weakest friendly by
// positioning between it and enemies. Retreats toward terrain cover as a group
// when taking heavy losses. Flees at <25% structure HP.
//
// Minimum compute: 2
// Recommended loadouts: Mixed fleet with shields. Destroyers + frigates with
//   energy weapons work well. At least one ship with a tactical computer for
//   higher compute.

var FLEE_THRESHOLD = 0.25;
var FORMATION_SPREAD = 800;
var RETREAT_HP_THRESHOLD = 0.5;

function onTick(state) {
    var ship = state.myShip;
    var enemies = state.enemies;

    if (!enemies || enemies.length === 0) {
        commands.stop();
        return;
    }

    // Flee check
    var hpRatio = ship.structure / ship.maxStructure;
    if (hpRatio < FLEE_THRESHOLD) {
        commands.flee();
        return;
    }

    // Solo fallback: balanced 1v1 behavior
    var friendlies = state.friendlies;
    if (!friendlies || friendlies.length <= 1) {
        soloFallback(ship, enemies);
        return;
    }

    // Focus-fire target: pick the closest enemy to the fleet center
    var target = pickFocusTarget(ship, friendlies, enemies);

    // Determine if fleet should retreat (average HP below threshold)
    var avgHp = getAverageFleetHp(friendlies);
    var retreating = avgHp < RETREAT_HP_THRESHOLD;

    // Find weakest friendly to protect
    var weakest = findWeakestFriendly(ship, friendlies);

    // Movement: maintain formation while engaging
    var angleToTarget = utils.angleTo(ship.position, target.position);
    var dist = utils.distance(ship.position, target.position);

    if (retreating && state.terrain && state.terrain.length > 0) {
        // Retreat toward nearest terrain for cover
        var cover = findNearestCover(ship, state.terrain);
        if (cover) {
            var coverAngle = utils.angleTo(ship.position, cover);
            commands.thrust(coverAngle, 0.7);
        } else {
            // No cover -- back away from enemies
            var awayAngle = utils.angleTo(target.position, ship.position);
            commands.thrust(awayAngle, 0.6);
        }
    } else if (weakest && weakest.id !== ship.id) {
        // Position between the weakest friendly and the target
        var midX = (weakest.position.x + target.position.x) / 2;
        var midY = (weakest.position.y + target.position.y) / 2;

        // Offset by formation spread based on our index in the fleet
        var myIndex = getFleetIndex(ship, friendlies);
        var offsetAngle = angleToTarget + (Math.PI / 2) * ((myIndex % 2 === 0) ? 1 : -1);
        var spreadX = midX + Math.cos(offsetAngle) * FORMATION_SPREAD * ((myIndex + 1) * 0.5);
        var spreadY = midY + Math.sin(offsetAngle) * FORMATION_SPREAD * ((myIndex + 1) * 0.5);

        commands.moveTo(spreadX, spreadY, ship.maxSpeed * 0.7);
    } else {
        // We are the weakest or solo -- maintain medium range
        if (dist < 1500) {
            var awayAngle2 = utils.angleTo(target.position, ship.position);
            commands.thrust(awayAngle2, 0.6);
        } else if (dist > 3500) {
            commands.thrust(angleToTarget, 0.7);
        } else {
            // Orbit
            var orbitAngle = angleToTarget + Math.PI / 2;
            commands.thrust(orbitAngle, 0.3);
        }
    }

    // All weapons on the focus target
    fireAllWeapons(ship, target);
}

function soloFallback(ship, enemies) {
    // Simple balanced behavior for solo ships
    var target = enemies[0];
    var bestDist = utils.distance(ship.position, enemies[0].position);
    for (var i = 1; i < enemies.length; i++) {
        var d = utils.distance(ship.position, enemies[i].position);
        if (d < bestDist) {
            bestDist = d;
            target = enemies[i];
        }
    }

    var dist = bestDist;
    var angleToTarget = utils.angleTo(ship.position, target.position);

    if (dist > 3000) {
        commands.thrust(angleToTarget, 0.8);
    } else if (dist < 1500) {
        var away = utils.angleTo(target.position, ship.position);
        commands.thrust(away, 0.6);
    } else {
        commands.thrust(angleToTarget + Math.PI / 2, 0.3);
    }

    fireAllWeapons(ship, target);
}

function pickFocusTarget(ship, friendlies, enemies) {
    // Find fleet center
    var cx = 0, cy = 0;
    for (var i = 0; i < friendlies.length; i++) {
        cx += friendlies[i].position.x;
        cy += friendlies[i].position.y;
    }
    cx /= friendlies.length;
    cy /= friendlies.length;
    var center = { x: cx, y: cy };

    // Pick closest enemy to fleet center (easier for everyone to engage)
    var best = enemies[0];
    var bestDist = utils.distance(center, enemies[0].position);
    for (var i = 1; i < enemies.length; i++) {
        var d = utils.distance(center, enemies[i].position);
        if (d < bestDist) {
            bestDist = d;
            best = enemies[i];
        }
    }
    return best;
}

function getAverageFleetHp(friendlies) {
    var total = 0;
    for (var i = 0; i < friendlies.length; i++) {
        total += friendlies[i].structure / friendlies[i].maxStructure;
    }
    return total / friendlies.length;
}

function findWeakestFriendly(ship, friendlies) {
    var weakest = null;
    var lowestHp = 2.0;
    for (var i = 0; i < friendlies.length; i++) {
        var ratio = friendlies[i].structure / friendlies[i].maxStructure;
        if (ratio < lowestHp) {
            lowestHp = ratio;
            weakest = friendlies[i];
        }
    }
    return weakest;
}

function getFleetIndex(ship, friendlies) {
    for (var i = 0; i < friendlies.length; i++) {
        if (friendlies[i].id === ship.id) return i;
    }
    return 0;
}

function findNearestCover(ship, terrain) {
    var best = null;
    var bestDist = 999999;
    for (var i = 0; i < terrain.length; i++) {
        var obs = terrain[i];
        // Only use asteroids for cover (not stars/black holes)
        var t = (obs.type || "").toLowerCase();
        if (t === "star" || t === "blackhole") continue;
        var pos = { x: obs.x, y: obs.y };
        var d = utils.distance(ship.position, pos);
        if (d < bestDist) {
            bestDist = d;
            best = pos;
        }
    }
    return best;
}

function fireAllWeapons(ship, target) {
    var weapons = ship.weapons;
    if (!weapons) return;

    for (var i = 0; i < weapons.length; i++) {
        var w = weapons[i];
        var dtype = (w.damageType || "").toLowerCase();

        if (dtype === "kinetic") {
            var projSpeed = (w.range || 3000) * 1.5;
            var lead = utils.leadTarget(ship, target, projSpeed);
            commands.fireAt(w.id, lead.x, lead.y);
        } else {
            commands.fire(w.id, target.id);
        }
    }
}
