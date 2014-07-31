﻿namespace DragDropLib
{
    using System;
    using System.Windows;

    public static class WpfDragDropLibExtensions
    {
        /// <summary>
        /// Converts a System.Windows.Point value to a DragDropLib.Win32Point value.
        /// </summary>
        /// <param name="pt">Input value.</param>
        /// <returns>Converted value.</returns>
        public static Win32Point ToWin32Point(this Point pt)
        {
            Win32Point wpt = new Win32Point();
            wpt.x = (int)pt.X;
            wpt.y = (int)pt.Y;
            return wpt;
        }
    }
}
