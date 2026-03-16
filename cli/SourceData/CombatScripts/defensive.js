// defensive.js
// Strategy: Simple defensive kiting. Maintain distance from enemies while
// firing all weapons. Backs away when enemies get too close.
//
// Minimum compute: 1
// Recommended loadouts: Ships with energy weapons (hitscan) and shields.
//   Works well for frigates and destroyers that can kite effectively.

function onTick(state) {
    var enemies = state.enemies;
    if (!enemies || enemies.length === 0) return;

    // Find closest enemy
    var closest = enemies[0];
    var closestDist = utils.distance(state.myShip, closest);
    for (var i = 1; i < enemies.length; i++) {
        var d = utils.distance(state.myShip, enemies[i]);
        if (d < closestDist) {
            closest = enemies[i];
            closestDist = d;
        }
    }

    // Maintain distance — if too close, back away
    if (closestDist < 2000) {
        var awayAngle = utils.angleTo(closest, state.myShip);
        commands.thrust(awayAngle, 0.8);
    }

    // Fire all weapons at closest enemy
    var weapons = state.myShip.weapons;
    if (weapons) {
        for (var w = 0; w < weapons.length; w++) {
            commands.fire(weapons[w].id, closest.id);
        }
    }
}
