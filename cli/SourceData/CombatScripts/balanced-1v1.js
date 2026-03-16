// balanced-1v1.js
// Strategy: Single-ship balanced engagement.
// Maintains optimal range based on weapon types: energy weapons prefer distance
// (hitscan with distance attenuation), kinetic weapons prefer closing in
// (projectile, no attenuation). Uses terrain for cover when available. Fires
// all weapons on cooldown, prioritizing accuracy over volume. Flees when
// structure drops below 25%.
//
// Minimum compute: 1
// Recommended loadouts: Mixed weapons (energy + kinetic), moderate armor/shields.
//   Destroyers or frigates with tactical computers for faster script ticks.

var optimalRange = null;
var hasEnergy = false;
var hasKinetic = false;
var FLEE_THRESHOLD = 0.25;

function onStart(state) {
    analyzeLoadout(state.myShip);
}

function onTick(state) {
    var ship = state.myShip;
    var enemies = state.enemies;

    if (!enemies || enemies.length === 0) {
        commands.stop();
        return;
    }

    // Analyze loadout on first tick if onStart was missed
    if (optimalRange === null) {
        analyzeLoadout(ship);
    }

    // Flee check: disengage at low structure
    var hpRatio = ship.structure / ship.maxStructure;
    if (hpRatio < FLEE_THRESHOLD) {
        commands.flee();
        return;
    }

    // Pick primary target: lowest HP enemy (focus fire for kills)
    var target = pickTarget(ship, enemies);

    var dist = utils.distance(ship.position, target.position);
    var angleToTarget = utils.angleTo(ship.position, target.position);

    // Range management
    if (dist > optimalRange + 500) {
        // Too far -- close in
        commands.thrust(angleToTarget, 0.8);
    } else if (dist < optimalRange - 500) {
        // Too close -- back off (especially for energy builds)
        var awayAngle = utils.angleTo(target.position, ship.position);
        commands.thrust(awayAngle, 0.6);
    } else {
        // In the sweet spot -- orbit to make ourselves harder to hit
        var orbitAngle = angleToTarget + Math.PI / 2;
        commands.thrust(orbitAngle, 0.4);
    }

    // Fire all weapons
    fireWeapons(ship, target);
}

function analyzeLoadout(ship) {
    var weapons = ship.weapons;
    var maxRange = 0;
    var totalEnergyRange = 0;
    var totalKineticRange = 0;
    var energyCount = 0;
    var kineticCount = 0;

    if (weapons) {
        for (var i = 0; i < weapons.length; i++) {
            var w = weapons[i];
            var range = w.range || 3000;
            if (range > maxRange) maxRange = range;

            var dtype = (w.damageType || "").toLowerCase();
            if (dtype === "energy") {
                hasEnergy = true;
                totalEnergyRange += range;
                energyCount++;
            } else {
                hasKinetic = true;
                totalKineticRange += range;
                kineticCount++;
            }
        }
    }

    // Energy weapons: distance attenuation means closer is worse for them
    // but we still want to be in range. Kinetic: no attenuation, close is fine.
    if (energyCount > 0 && kineticCount === 0) {
        // Pure energy: stay at 60-70% of max range for good damage with room to kite
        optimalRange = maxRange * 0.65;
    } else if (kineticCount > 0 && energyCount === 0) {
        // Pure kinetic: get close for accuracy (projectile travel time)
        optimalRange = maxRange * 0.35;
    } else {
        // Mixed: balance at about 50%
        optimalRange = maxRange * 0.50;
    }

    if (optimalRange < 500) optimalRange = 500;
}

function pickTarget(ship, enemies) {
    var best = enemies[0];
    var bestScore = targetScore(ship, enemies[0]);

    for (var i = 1; i < enemies.length; i++) {
        var s = targetScore(ship, enemies[i]);
        if (s > bestScore) {
            bestScore = s;
            best = enemies[i];
        }
    }
    return best;
}

function targetScore(ship, enemy) {
    // Prefer low-HP enemies (finish them off) and closer ones
    var hpPercent = enemy.structure / enemy.maxStructure;
    var dist = utils.distance(ship.position, enemy.position);
    // Lower HP = higher score; closer = higher score
    return (1.0 - hpPercent) * 100 + (10000 - dist) * 0.01;
}

function fireWeapons(ship, target) {
    var weapons = ship.weapons;
    if (!weapons) return;

    for (var i = 0; i < weapons.length; i++) {
        var w = weapons[i];
        var dtype = (w.damageType || "").toLowerCase();

        if (dtype === "kinetic") {
            // Lead the target for kinetic projectiles
            // Approximate projectile speed based on weapon range / travel time
            var projSpeed = (w.range || 3000) * 1.5;
            var lead = utils.leadTarget(ship, target, projSpeed);
            commands.fireAt(w.id, lead.x, lead.y);
        } else {
            // Energy: hitscan, fire directly
            commands.fire(w.id, target.id);
        }
    }
}
