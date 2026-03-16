// aggressive-multi.js
// Strategy: Multi-ship coordinated aggression.
// When solo, pursues the weakest enemy directly. In a group, distributes
// targets across friendlies to avoid overkill, flanks from different angles,
// closes aggressively, and uses leadTarget for kinetic weapons. No flee
// threshold -- fights to the last ship.
//
// Minimum compute: 2
// Recommended loadouts: Kinetic weapons (railguns, mass drivers, gauss lances)
//   for maximum projectile damage at close range. High speed ships to close
//   distance quickly. Armor over shields (kinetic builds favor brawling range).

function onTick(state) {
    var ship = state.myShip;
    var enemies = state.enemies;

    if (!enemies || enemies.length === 0) {
        // No enemies: charge toward center to find stragglers
        commands.moveTo(0, 0, ship.maxSpeed);
        return;
    }

    var friendlies = state.friendlies;
    var isSolo = !friendlies || friendlies.length <= 1;

    // Pick target
    var target;
    if (isSolo) {
        target = pickWeakestEnemy(ship, enemies);
    } else {
        target = pickDistributedTarget(ship, friendlies, enemies);
    }

    var dist = utils.distance(ship.position, target.position);
    var angleToTarget = utils.angleTo(ship.position, target.position);

    // Movement: aggressive close-in with flanking
    if (isSolo) {
        // Solo: beeline toward target at full power
        commands.thrust(angleToTarget, 1.0);
    } else {
        // Multi: flank from an offset angle based on fleet index
        var myIndex = getFleetIndex(ship, friendlies);
        var fleetSize = friendlies.length;

        // Spread flanking angles: each ship approaches from a different direction
        // Ship 0: head-on, Ship 1: +45 deg, Ship 2: -45 deg, etc.
        var flankOffset = 0;
        if (fleetSize > 1) {
            var halfSpread = Math.PI / 3; // 60 degrees total spread
            flankOffset = halfSpread * ((myIndex / (fleetSize - 1)) * 2 - 1);
        }

        if (dist > 2000) {
            // Closing phase: approach from flanking angle
            var approachAngle = angleToTarget + flankOffset * 0.5;
            commands.thrust(approachAngle, 1.0);
        } else {
            // Brawling phase: orbit tight around target from our flank angle
            var orbitAngle = angleToTarget + Math.PI / 2 + flankOffset;
            commands.thrust(orbitAngle, 0.7);
        }
    }

    // Fire everything -- aggressive scripts waste nothing
    fireWeaponsAggressive(ship, target, dist);
}

function pickWeakestEnemy(ship, enemies) {
    var weakest = enemies[0];
    var lowestHp = enemies[0].structure;

    for (var i = 1; i < enemies.length; i++) {
        if (enemies[i].structure < lowestHp) {
            lowestHp = enemies[i].structure;
            weakest = enemies[i];
        }
    }
    return weakest;
}

function pickDistributedTarget(ship, friendlies, enemies) {
    // Distribute targets: each ship picks a different enemy when possible.
    // Use fleet index modulo enemy count for basic distribution.
    // If more friendlies than enemies, multiple ships share targets,
    // but they still spread across all enemies before doubling up.
    var myIndex = getFleetIndex(ship, friendlies);

    if (enemies.length === 1) return enemies[0];

    // Sort enemies by HP (weakest first) so the first ships finish kills
    var sorted = enemies.slice(0);
    sorted.sort(function(a, b) { return a.structure - b.structure; });

    var targetIndex = myIndex % sorted.length;
    return sorted[targetIndex];
}

function getFleetIndex(ship, friendlies) {
    for (var i = 0; i < friendlies.length; i++) {
        if (friendlies[i].id === ship.id) return i;
    }
    return 0;
}

function fireWeaponsAggressive(ship, target, dist) {
    var weapons = ship.weapons;
    if (!weapons) return;

    for (var i = 0; i < weapons.length; i++) {
        var w = weapons[i];
        var dtype = (w.damageType || "").toLowerCase();
        var range = w.range || 3000;

        if (dtype === "kinetic") {
            // Always lead kinetic shots for maximum accuracy
            var projSpeed = range * 1.5;
            var lead = utils.leadTarget(ship, target, projSpeed);
            commands.fireAt(w.id, lead.x, lead.y);
        } else {
            // Energy: fire direct -- hitscan hits instantly
            commands.fire(w.id, target.id);
        }
    }
}
