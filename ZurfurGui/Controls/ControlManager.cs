using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ZurfurGui.Controls;

public static class ControlManager
{
    static object s_lock = new();
    static Dictionary<string, Func<Controllable>> s_controls = new();

    /// <summary>
    /// Add a new control creator function.  The type of control must be inique.
    /// </summary>
    public static void Add(Func<Controllable> createControl)
    {
        lock (s_lock)
        {
            var typeName = createControl().TypeName;
            if (s_controls.ContainsKey(typeName))
                throw new ArgumentException($"Control registry already contains '{typeName}'");
            s_controls[typeName] = createControl;
        }
    }

    /// <summary>
    /// Crete a new control, or return null if the control name doesn't exist
    /// </summary>
    public static Controllable? Create(string typeName)
    {
        lock (s_lock)
        {
            if (s_controls.TryGetValue(typeName, out var createFun))
                return createFun();
            return null;
        }
    }


}
