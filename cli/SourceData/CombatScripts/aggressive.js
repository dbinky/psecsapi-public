// aggressive.js
// Strategy: Simple aggression. Chase the closest enemy and fire all weapons.
// No coordination, no flee threshold — just rush in and shoot.
//
// Minimum compute: 1
// Recommended loadouts: Any combat-focused ship. Works well as a fallback
//   when no better script is available.

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

    // Move toward closest enemy
    var angle = utils.angleTo(state.myShip, closest);
    commands.thrust(angle, 1.0);

    // Fire all weapons at closest enemy
    var weapons = state.myShip.weapons;
    if (weapons) {
        for (var w = 0; w < weapons.length; w++) {
            commands.fire(weapons[w].id, closest.id);
        }
    }
}
