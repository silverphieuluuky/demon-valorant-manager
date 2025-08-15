using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RiotAutoLogin.Utilities
{
    public static class VisualTreeHelperExtensions
    {
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();
            FindVisualChildren(parent, children);
            return children;
        }

        private static void FindVisualChildren<T>(DependencyObject parent, List<T> children) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    children.Add(result);

                FindVisualChildren(child, children);
            }
        }

        public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null)
                return null;

            if (parent is T result)
                return result;

            return FindVisualParent<T>(parent);
        }
    }
}
