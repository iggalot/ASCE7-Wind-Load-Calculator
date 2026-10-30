using System;
using System.Windows;
using System.Windows.Controls;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// WPF groups radio buttons by GroupName across the whole window, so two copies of a control (or two
    /// controls that both use e.g. "roof") would uncheck each other's buttons. Giving each control
    /// instance its own suffix keeps every group inside its control.
    /// </summary>
    public static class RadioGroups
    {
        public static void MakeUnique(DependencyObject root)
        {
            string suffix = "_" + Guid.NewGuid().ToString("N");
            Walk(root, suffix);
        }

        private static void Walk(DependencyObject node, string suffix)
        {
            if (node is RadioButton rb && !string.IsNullOrEmpty(rb.GroupName))
                rb.GroupName += suffix;
            foreach (object child in LogicalTreeHelper.GetChildren(node))
                if (child is DependencyObject d) Walk(d, suffix);
        }
    }
}
