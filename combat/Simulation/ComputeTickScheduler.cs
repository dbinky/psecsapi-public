using psecsapi.Domain.Combat;
using psecsapi.Combat;
using psecsapi.Combat;
using psecsapi.Combat.Snapshots;

namespace psecsapi.Combat.Simulation;

/// <summary>
/// Schedules compute ticks per ship based on ComputeCapacity.
/// Higher compute = more frequent script execution.
/// Formula: interval = BaseComputeInterval / (computeCapacity / ComputeCapacityBase)
/// Minimum interval = 1 (executes every sim tick).
/// </summary>
public class ComputeTickScheduler
{
    private readonly Dictionary<Guid, int> _shipIntervals = new();
    private readonly Dictionary<Guid, int> _shipLastComputeTick = new();

    /// <summary>
    /// Initialize the scheduler with all ships participating in combat.
    /// Calculates each ship's compute tick interval based on their ComputeCapacity.
    /// </summary>
    public void Initialize(List<CombatShipSnapshot> allShips)
    {
        _shipIntervals.Clear();
        _shipLastComputeTick.Clear();

        foreach (var ship in allShips)
        {
            int interval = CalculateInterval(ship.ComputeCapacity);
            _shipIntervals[ship.ShipId] = interval;
            // First compute tick fires at tick 0 for all ships (onStart)
            _shipLastComputeTick[ship.ShipId] = -interval; // So tick 0 triggers
        }
    }

    /// <summary>
    /// Get the compute tick interval for a specific ship.
    /// </summary>
    public int GetInterval(Guid shipId)
    {
        return _shipIntervals.TryGetValue(shipId, out int interval) ? interval : CombatConstants.BaseComputeInterval;
    }

    /// <summary>
    /// Update the compute tick interval for a ship after module damage.
    /// Call this whenever a ship's compute modules take condition damage.
    /// Does nothing if the ship is not registered.
    /// </summary>
    public void UpdateShipInterval(Guid shipId, double currentComputeCapacity)
    {
        if (!_shipIntervals.ContainsKey(shipId)) return;
        _shipIntervals[shipId] = CalculateInterval(currentComputeCapacity);
    }

    /// <summary>
    /// Get the list of ships that should execute their compute tick on the given sim tick.
    /// A ship fires its compute tick when (currentSimTick - lastComputeTick) >= interval.
    /// </summary>
    public List<Guid> GetShipsForComputeTick(int currentSimTick)
    {
        var result = new List<Guid>();

        foreach (var (shipId, interval) in _shipIntervals)
        {
            int lastTick = _shipLastComputeTick[shipId];
            if (currentSimTick - lastTick >= interval)
            {
                result.Add(shipId);
                _shipLastComputeTick[shipId] = currentSimTick;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate the sim-tick interval between compute ticks for a given compute capacity.
    /// interval = BaseComputeInterval / (computeCapacity / ComputeCapacityBase)
    /// Minimum interval is 1 (ship executes every tick if compute is very high).
    /// </summary>
    public static int CalculateInterval(double computeCapacity)
    {
        if (computeCapacity <= 0) return CombatConstants.BaseComputeInterval * 10; // Effectively disabled

        double ratio = computeCapacity / CombatConstants.ComputeCapacityBase;
        int interval = (int)Math.Ceiling(CombatConstants.BaseComputeInterval / ratio);
        return Math.Max(1, interval);
    }
}
