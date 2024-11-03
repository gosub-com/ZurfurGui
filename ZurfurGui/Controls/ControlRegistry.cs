using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ZurfurGui.Controls;

public static class ControlRegistry
{
    static object s_lock = new();
    static Dictionary<string, Func<Controllable>> s_controls = new();

    /// <summary>
    /// Add a new control creator function
    /// </summary>
    public static void Add(string name, Func<Controllable> createControl)
    {
        lock (s_lock)
        {
            if (s_controls.ContainsKey(name))
                throw new ArgumentException($"Control registray already contains '{name}'");
            s_controls[name] = createControl;
        }   
    }

    /// <summary>
    /// Crete a new control, or return null if the control name doesn't exist
    /// </summary>
    public static Controllable? Create(string name)
    {
        lock (s_lock)
        {
            if (s_controls.TryGetValue(name, out var createFun))
                return createFun();
            return null;
        }
    }

    
}
