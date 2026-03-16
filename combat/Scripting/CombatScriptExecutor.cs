using psecsapi.Domain.Combat;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;

namespace psecsapi.Combat.Scripting;

/// <summary>
/// Executes a player's combat script in a sandboxed Jint JavaScript engine.
/// One instance per ship per combat. The engine is reused across ticks to
/// preserve any script-level variables the player sets in onStart.
/// </summary>
public class CombatScriptExecutor : IDisposable
{
    private readonly string _scriptSource;
    private readonly int _stepLimit;
    private Engine? _engine;
    private ScriptCommandCollector? _commandCollector;
    private bool _initialized;
    private bool _hasSyntaxError;

    /// <summary>
    /// Creates a new script executor for a ship.
    /// </summary>
    /// <param name="scriptSource">The JavaScript source code.</param>
    /// <param name="stepLimit">Maximum Jint execution steps per tick (CombatConstants.MaxJintStepsPerTick).</param>
    public CombatScriptExecutor(string scriptSource, int stepLimit)
    {
        _scriptSource = scriptSource ?? throw new ArgumentNullException(nameof(scriptSource));
        _stepLimit = stepLimit;
    }

    /// <summary>
    /// Initializes the Jint engine, parses the script, and configures sandbox constraints.
    /// Must be called once before ExecuteOnStart/ExecuteOnTick.
    /// Returns false if the script has a syntax error (ship falls back to auto-fire).
    /// </summary>
    public bool Initialize()
    {
        if (_initialized)
            return !_hasSyntaxError;

        _initialized = true;
        _commandCollector = new ScriptCommandCollector();

        _engine = new Engine(options =>
        {
            // Step limit prevents infinite loops.
            options.MaxStatements(_stepLimit);

            // No strict mode -- more lenient for player scripts.
            options.Strict(false);

            // Memory limit to prevent excessive allocations.
            options.LimitMemory(4_000_000); // 4MB

            // CLR access is disabled by default in Jint 4.x -- no AllowClr call needed.
            // Scripts cannot access .NET types, System.IO, HttpClient, etc.
        });

        RegisterCommands(_engine, _commandCollector);
        RegisterUtils(_engine);

        try
        {
            _engine.Execute(_scriptSource);
        }
        catch (JavaScriptException)
        {
            // Jint 4.x throws JavaScriptException for both parse and runtime errors
            // during initial script evaluation.
            _hasSyntaxError = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calls the script's onStart(state) function if it exists.
    /// Called once at the beginning of combat for this ship.
    /// Returns the commands issued during onStart, or empty list on error.
    /// </summary>
    public List<ScriptCommand> ExecuteOnStart(Dictionary<string, object> state)
    {
        if (_engine == null || _hasSyntaxError || _commandCollector == null)
            return new List<ScriptCommand>();

        return ExecuteFunction("onStart", state);
    }

    /// <summary>
    /// Calls the script's onTick(state) function.
    /// Called on each of this ship's compute ticks.
    /// Returns the commands issued during onTick, or empty list on error.
    /// </summary>
    public List<ScriptCommand> ExecuteOnTick(Dictionary<string, object> state)
    {
        if (_engine == null || _hasSyntaxError || _commandCollector == null)
            return new List<ScriptCommand>();

        return ExecuteFunction("onTick", state);
    }

    /// <summary>
    /// Whether the script had a syntax error and is non-functional.
    /// </summary>
    public bool HasSyntaxError => _hasSyntaxError;

    private List<ScriptCommand> ExecuteFunction(string functionName, Dictionary<string, object> state)
    {
        if (_engine == null || _commandCollector == null)
            return new List<ScriptCommand>();

        try
        {
            // Check if the function exists in the script
            var fnValue = _engine.GetValue(functionName);
            if (fnValue.IsUndefined() || fnValue.IsNull())
                return new List<ScriptCommand>();

            // Convert the state dictionary to a Jint JS object
            var jsState = JsValue.FromObject(_engine, state);

            // Reset the statement counter for this execution
            _engine.Constraints.Reset();

            // Invoke the function with the state argument
            _engine.Invoke(functionName, jsState);

            // Drain and return the collected commands
            return _commandCollector.DrainCommands();
        }
        catch (StatementsCountOverflowException)
        {
            // Script exceeded step limit (infinite loop or too complex).
            // Return whatever commands were collected before the limit was hit.
            return _commandCollector.DrainCommands();
        }
        catch (JavaScriptException)
        {
            // Script threw a runtime error. Ship falls back to auto-fire + maintain course.
            _commandCollector.DrainCommands(); // discard partial commands
            return new List<ScriptCommand>();
        }
        catch (MemoryLimitExceededException)
        {
            // Script tried to allocate too much memory.
            _commandCollector.DrainCommands();
            return new List<ScriptCommand>();
        }
        catch (Exception)
        {
            // Unexpected error -- treat as script failure.
            _commandCollector.DrainCommands();
            return new List<ScriptCommand>();
        }
    }

    /// <summary>
    /// Registers the 'commands' object on the Jint engine, binding each method
    /// to the ScriptCommandCollector. Uses anonymous object which Jint wraps
    /// with CLR-to-JS interop automatically.
    /// </summary>
    private static void RegisterCommands(Engine engine, ScriptCommandCollector collector)
    {
        engine.SetValue("commands", new
        {
            thrust = new Func<double, double, JsValue>((angle, power) =>
            {
                collector.Thrust(angle, power);
                return JsValue.Undefined;
            }),
            moveTo = new Func<double, double, double, JsValue>((x, y, speed) =>
            {
                collector.MoveTo(x, y, speed);
                return JsValue.Undefined;
            }),
            stop = new Func<JsValue>(() =>
            {
                collector.Stop();
                return JsValue.Undefined;
            }),
            fire = new Func<string, string, JsValue>((weaponId, targetId) =>
            {
                collector.Fire(weaponId, targetId);
                return JsValue.Undefined;
            }),
            fireAt = new Func<string, double, double, JsValue>((weaponId, x, y) =>
            {
                collector.FireAt(weaponId, x, y);
                return JsValue.Undefined;
            }),
            holdFire = new Func<JsValue>(() =>
            {
                collector.HoldFire();
                return JsValue.Undefined;
            }),
            flee = new Func<JsValue>(() =>
            {
                collector.Flee();
                return JsValue.Undefined;
            })
        });
    }

    /// <summary>
    /// Registers the 'utils' object on the Jint engine with distance, angleTo,
    /// and leadTarget functions. Uses anonymous object for Jint 4.x compatibility.
    ///
    /// These delegate adapters convert Jint's JsValue arguments to the
    /// IDictionary types that ScriptUtilities expects.
    /// </summary>
    private static void RegisterUtils(Engine engine)
    {
        engine.SetValue("utils", new
        {
            distance = new Func<JsValue, JsValue, double>((a, b) =>
            {
                var dictA = JsObjectToDictionary(a);
                var dictB = JsObjectToDictionary(b);
                return ScriptUtilities.Distance(dictA, dictB);
            }),
            angleTo = new Func<JsValue, JsValue, double>((a, b) =>
            {
                var dictA = JsObjectToDictionary(a);
                var dictB = JsObjectToDictionary(b);
                return ScriptUtilities.AngleTo(dictA, dictB);
            }),
            leadTarget = new Func<JsValue, JsValue, double, JsValue>((me, enemy, speed) =>
            {
                var dictMe = JsObjectToDictionary(me);
                var dictEnemy = JsObjectToDictionary(enemy);
                var result = ScriptUtilities.LeadTarget(dictMe, dictEnemy, speed);
                // Convert result dictionary back to JS object
                return JsValue.FromObject(engine, result);
            })
        });
    }

    private const int MaxJsObjectDepth = 20;

    /// <summary>
    /// Converts a Jint JsValue (ObjectInstance) to a Dictionary for use with
    /// ScriptUtilities methods. Handles nested objects (position, velocity).
    /// </summary>
    private static Dictionary<string, object> JsObjectToDictionary(JsValue jsValue)
        => JsObjectToDictionary(jsValue, 0);

    private static Dictionary<string, object> JsObjectToDictionary(JsValue jsValue, int depth)
    {
        if (depth > MaxJsObjectDepth)
            return new Dictionary<string, object>();

        var dict = new Dictionary<string, object>();

        if (jsValue is ObjectInstance obj)
        {
            foreach (var prop in obj.GetOwnProperties())
            {
                var key = prop.Key.ToString();
                var value = prop.Value.Value;

                if (value is ObjectInstance nestedObj && !value.IsNull())
                {
                    // Recursively convert nested objects (position, velocity)
                    dict[key] = JsObjectToDictionary(nestedObj, depth + 1);
                }
                else
                {
                    dict[key] = value.ToObject() ?? 0.0;
                }
            }
        }

        return dict;
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }
}
