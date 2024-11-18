using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Render;

public static class ViewHelper
{

    /// <summary>
    /// Create controllers from properties, add to views
    /// </summary>
    public static void BuildViewsFromProperties(IList<View> views, Properties[]? controllerProperties)
    {
        if (controllerProperties != null)
            foreach (var control in controllerProperties)
                views.Add(BuildViewFromProperties(control));
    }

    public static View BuildViewFromProperties(Properties controllerProperties)
    {
        var controlName = controllerProperties.Get(ZGui.Controller) ?? "";
        if (controlName == "")
            throw new ArgumentException($"The control's controller property is not specified.");

        var control = ControlRegistry.Create(controlName);
        if (control == null)
            throw new ArgumentException($"'{controlName}' is not a registered control");


        control.View.Properties = controllerProperties;
        return control.BuildView(controllerProperties);
    }


}
