using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Services
{
    /// <summary>
    /// Helper class to safely manage view parent relationships and prevent IllegalStateException
    /// </summary>
    public static class ViewParentHelper
    {
        /// <summary>
        /// Safely adds a view to a parent container, removing it from any existing parent first
        /// </summary>
        /// <param name="view">The view to add</param>
        /// <param name="parent">The parent container to add the view to</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool SafeAddToParent(View view, Layout parent)
        {
            if (view == null || parent == null)
                return false;

            try
            {
                // Remove from current parent if it exists
                if (view.Parent != null)
                {
                    if (view.Parent is Layout currentParent)
                    {
                        currentParent.Children.Remove(view);
                    }
                }

                // Add to new parent if not already present
                if (!parent.Children.Contains(view))
                {
                    parent.Children.Add(view);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeAddToParent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely removes a view from its parent container
        /// </summary>
        /// <param name="view">The view to remove</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool SafeRemoveFromParent(View view)
        {
            if (view?.Parent == null)
                return true; // Already removed or no parent

            try
            {
                if (view.Parent is Layout parent)
                {
                    parent.Children.Remove(view);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeRemoveFromParent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely clears all children from a layout container
        /// </summary>
        /// <param name="layout">The layout to clear</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool SafeClearChildren(Layout layout)
        {
            if (layout == null)
                return false;

            try
            {
                layout.Children.Clear();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeClearChildren: {ex.Message}");
                return false;
            }
        }
    }
}
