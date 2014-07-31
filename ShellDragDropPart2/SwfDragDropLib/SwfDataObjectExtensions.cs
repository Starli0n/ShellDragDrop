using System;
using System.Drawing;
using System.Runtime.InteropServices;
using DragDropLib;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using Point = System.Drawing.Point;

namespace System.Windows.Forms
{
    public static class SwfDataObjectExtensions
    {
        #region DLL imports

        [DllImport("gdiplus.dll")]
        private static extern bool DeleteObject(IntPtr hgdi);

        #endregion // DLL imports

        /// <summary>
        /// Sets the drag image as the rendering of a control.
        /// </summary>
        /// <param name="dataObject">The DataObject to set the drag image on.</param>
        /// <param name="control">The Control to render as the drag image.</param>
        /// <param name="cursorOffset">The location of the cursor relative to the control.</param>
        public static void SetDragImage(this IDataObject dataObject, Control control, System.Drawing.Point cursorOffset)
        {
            int width = control.Width;
            int height = control.Height;

            Bitmap bmp = new Bitmap(width, height);
            control.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));

            SetDragImage(dataObject, bmp, cursorOffset);
        }

        /// <summary>
        /// Sets the drag image.
        /// </summary>
        /// <param name="dataObject">The DataObject to set the drag image on.</param>
        /// <param name="image">The drag image.</param>
        /// <param name="cursorOffset">The location of the cursor relative to the image.</param>
        public static void SetDragImage(this IDataObject dataObject, Image image, System.Drawing.Point cursorOffset)
        {
            ShDragImage shdi = new ShDragImage();

            Win32Size size;
            size.cx = image.Width;
            size.cy = image.Height;
            shdi.sizeDragImage = size;

            Win32Point wpt;
            wpt.x = cursorOffset.X;
            wpt.y = cursorOffset.Y;
            shdi.ptOffset = wpt;

            shdi.crColorKey = Color.Magenta.ToArgb();

            // This HBITMAP will be managed by the DragDropHelper
            // as soon as we pass it to InitializeFromBitmap. If we fail
            // to make the hand off, we'll delete it to prevent a mem leak.
            IntPtr hbmp = GetHbitmapFromImage(image);
            shdi.hbmpDragImage = hbmp;

            try
            {
                IDragSourceHelper sourceHelper = (IDragSourceHelper)new DragDropHelper();

                try
                {
                    sourceHelper.InitializeFromBitmap(ref shdi, (ComIDataObject)dataObject);
                }
                catch (NotImplementedException ex)
                {
                    throw new Exception("A NotImplementedException was caught. This could be because you forgot to construct your DataObject using a DragDropLib.DataObject", ex);
                }
            }
            catch
            {
                DeleteObject(hbmp);
            }
        }

        /// <summary>
        /// Gets an HBITMAP from any image.
        /// </summary>
        /// <param name="image">The image to get an HBITMAP from.</param>
        /// <returns>An HBITMAP pointer.</returns>
        /// <remarks>
        /// The caller is responsible to call DeleteObject on the HBITMAP.
        /// </remarks>
        private static IntPtr GetHbitmapFromImage(Image image)
        {
            if (image is Bitmap)
            {
                return ((Bitmap)image).GetHbitmap();
            }
            else
            {
                Bitmap bmp = new Bitmap(image);
                return bmp.GetHbitmap();
            }
        }
    }
}
