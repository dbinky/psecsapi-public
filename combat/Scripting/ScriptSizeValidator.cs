using psecsapi.Domain.Combat;
using System.Text;

namespace psecsapi.Combat.Scripting;

/// <summary>
/// Validates that a combat script source is small enough for a ship to execute,
/// based on the ship's ComputeCapacity.
///
/// Max size = computeCapacity * MaxScriptSizePerCompute (16KB per point).
/// A ship with ComputeCapacity 50 can load up to 800KB.
/// Ships whose compute cannot accommodate the script fall back to flee behavior.
/// </summary>
public static class ScriptSizeValidator
{
    /// <summary>
    /// Checks if a script source is within the allowed size for a given compute capacity.
    /// </summary>
    /// <param name="scriptSource">The JavaScript source code.</param>
    /// <param name="computeCapacity">The ship's ComputeCapacity value.</param>
    /// <returns>True if the script is allowed; false if it exceeds the size limit.</returns>
    public static bool IsScriptAllowed(string scriptSource, double computeCapacity)
    {
        if (string.IsNullOrEmpty(scriptSource))
            return true;

        if (computeCapacity <= 0)
            return false;

        long scriptSizeBytes = Encoding.UTF8.GetByteCount(scriptSource);
        long maxSizeBytes = (long)(computeCapacity * CombatConstants.MaxScriptSizePerCompute);

        return scriptSizeBytes <= maxSizeBytes;
    }

    /// <summary>
    /// Returns the maximum allowed script size in bytes for a given compute capacity.
    /// </summary>
    public static long GetMaxScriptSize(double computeCapacity)
    {
        if (computeCapacity <= 0)
            return 0;

        return (long)(computeCapacity * CombatConstants.MaxScriptSizePerCompute);
    }
}
