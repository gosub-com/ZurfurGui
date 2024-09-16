using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Controls;

internal static class ViewHelper
{
    /// <summary>
    /// Call this function once on the highest level control.
    /// </summary>
    public static View BuildViewTree(Controllable control)
    {
        BuildViewTree(control.View);
        return control.View;
    }


    /// <summary>
    /// Build the view tree
    /// </summary>
    static void BuildViewTree(View view)
    {
        Debug.Assert(view.ParentView == null); // Shouldn't call on a view that is already in a tree
        view.SetParentView(null);
        if (view.Control != null)
            view.Control.PopulateView();

        foreach (var child in view.Views)
        {
            BuildViewTree(child);
            child.SetParentView(view);
        }
    }


}
