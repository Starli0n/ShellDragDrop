using System;
using System.Drawing;
using System.Windows.Forms;
using DragDropLib;

namespace System.Windows.Forms
{
    public static class DropTargetHelper
    {
        /// <summary>
        /// Internal instance of the DragDropHelper.
        /// </summary>
        private static IDropTargetHelper s_instance = (IDropTargetHelper)new DragDropHelper();

        static DropTargetHelper()
        {
        }

        /// <summary>
        /// Notifies the DragDropHelper that the specified Control received
        /// a DragEnter event.
        /// </summary>
        /// <param name="control">The Control the received the DragEnter event.</param>
        /// <param name="data">The DataObject containing a drag image.</param>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void DragEnter(Control control, IDataObject data, System.Drawing.Point cursorOffset, DragDropEffects effect)
        {
            SwfDropTargetHelperExtensions.DragEnter(s_instance, control, data, cursorOffset, effect);
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Control received
        /// a DragOver event.
        /// </summary>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void DragOver(System.Drawing.Point cursorOffset, DragDropEffects effect)
        {
            SwfDropTargetHelperExtensions.DragOver(s_instance, cursorOffset, effect);
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Control received
        /// a DragLeave event.
        /// </summary>
        public static void DragLeave()
        {
            s_instance.DragLeave();
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Control received
        /// a DragOver event.
        /// </summary>
        /// <param name="data">The DataObject containing a drag image.</param>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void Drop(IDataObject data, System.Drawing.Point cursorOffset, DragDropEffects effect)
        {
            SwfDropTargetHelperExtensions.Drop(s_instance, data, cursorOffset, effect);
        }

        /// <summary>
        /// Tells the DragDropHelper to show or hide the drag image.
        /// </summary>
        /// <param name="show">True to show the image. False to hide it.</param>
        public static void Show(bool show)
        {
            s_instance.Show(show);
        }
    }
}
