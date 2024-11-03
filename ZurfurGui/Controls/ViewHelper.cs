using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Controls;

public static class ViewHelper
{

    /// <summary>
    /// Create controllers from properties, add to views
    /// </summary>
    public static void AddControllers(IList<View> views, Properties[]? controllerProperties)
    {
        Debug.Assert(controllerProperties != null);
        if (controllerProperties != null)
            foreach (var control in controllerProperties)
                views.Add(GetViewFromProperties(control));
    }

    public static View GetViewFromProperties(Properties controllerProperties)
    {
        var controllerName = controllerProperties.Getc(ZGui.Controller) ?? "";
        if (controllerName == "")
            throw new ArgumentException($"The control controller property is not specified.");

        var control = ControlRegistry.Create(controllerName)
            ?? throw new ArgumentException($"Control '{controllerName}' not found in the control registry");
        control.Properties = controllerProperties;
        return control.BuildView(controllerProperties);
    }


}
