// advanced-flee.js
// Strategy: Escape the combat area while dodging enemy fire.
// Calculates the nearest grid boundary and navigates toward it at full speed.
// Jinks perpendicular to enemy firing lines every few ticks to dodge kinetic
// projectiles. Navigates around terrain obstacles when they block the escape path.
//
// Minimum compute: 1
// Recommended loadouts: Fast ships (corvettes/frigates with ion/fusion drives).
//   Shields help survive the escape window. Weapons optional -- this script
//   does not fire back.

var escapeAngle = null;
var jinkDirection = 1;
var lastJinkTick = 0;
var JINK_INTERVAL = 8; // ticks between direction changes
var JINK_AMPLITUDE = 0.6; // radians of perpendicular offset

function onStart(state) {
    escapeAngle = findNearestBoundaryAngle(state);
}

function onTick(state) {
    var ship = state.myShip;
    var enemies = state.enemies;

    // Recalculate escape angle if we drifted or it was never set
    if (escapeAngle === null) {
        escapeAngle = findNearestBoundaryAngle(state);
    }

    // Check for obstacles along escape path and adjust if needed
    if (state.terrain && state.terrain.length > 0) {
        escapeAngle = avoidObstacles(ship, escapeAngle, state.terrain);
    }

    // Jink perpendicular to enemy fire lines to dodge kinetic projectiles
    var thrustAngle = escapeAngle;
    if (enemies && enemies.length > 0) {
        // Switch jink direction periodically
        if (state.tick - lastJinkTick >= JINK_INTERVAL) {
            jinkDirection = -jinkDirection;
            lastJinkTick = state.tick;
        }
        thrustAngle = escapeAngle + (JINK_AMPLITUDE * jinkDirection);
    }

    // Full power toward the (jinking) escape heading
    commands.thrust(thrustAngle, 1.0);

    // No shooting -- pure evasion
    commands.holdFire();
}

function findNearestBoundaryAngle(state) {
    var ship = state.myShip;
    var px = ship.position.x;
    var py = ship.position.y;

    // Grid boundaries (default 20000x20000, -10000 to 10000)
    var minX = -10000;
    var maxX = 10000;
    var minY = -10000;
    var maxY = 10000;

    if (state.grid) {
        minX = state.grid.minX;
        maxX = state.grid.maxX;
        minY = state.grid.minY;
        maxY = state.grid.maxY;
    }

    // Distance to each boundary
    var dLeft = Math.abs(px - minX);
    var dRight = Math.abs(maxX - px);
    var dBottom = Math.abs(py - minY);
    var dTop = Math.abs(maxY - py);

    var minDist = dLeft;
    var angle = Math.PI; // left

    if (dRight < minDist) {
        minDist = dRight;
        angle = 0; // right
    }
    if (dBottom < minDist) {
        minDist = dBottom;
        angle = -Math.PI / 2; // down
    }
    if (dTop < minDist) {
        minDist = dTop;
        angle = Math.PI / 2; // up
    }

    return angle;
}

function avoidObstacles(ship, desiredAngle, terrain) {
    // Look ahead in the desired direction. If an obstacle is within a
    // short distance along that line, nudge the angle to go around it.
    var lookAhead = 1500;
    var targetX = ship.position.x + Math.cos(desiredAngle) * lookAhead;
    var targetY = ship.position.y + Math.sin(desiredAngle) * lookAhead;
    var targetPos = { x: targetX, y: targetY };

    for (var i = 0; i < terrain.length; i++) {
        var obs = terrain[i];
        var obsPos = { x: obs.x, y: obs.y };
        var dist = utils.distance(ship.position, obsPos);
        var margin = (obs.radius || 200) + 300;

        if (dist < margin + lookAhead) {
            // Check if our path passes near this obstacle
            var obsAngle = utils.angleTo(ship.position, obsPos);
            var angleDiff = normalizeAngle(obsAngle - desiredAngle);

            if (Math.abs(angleDiff) < 0.5) {
                // Obstacle is roughly ahead -- steer perpendicular
                if (angleDiff >= 0) {
                    return desiredAngle - 0.6;
                } else {
                    return desiredAngle + 0.6;
                }
            }
        }
    }
    return desiredAngle;
}

function normalizeAngle(a) {
    while (a > Math.PI) a -= 2 * Math.PI;
    while (a < -Math.PI) a += 2 * Math.PI;
    return a;
}
