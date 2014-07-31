using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DragDropLib;
using Color = System.Windows.Media.Color;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using DrawingColor = System.Drawing.Color;
using DrawingColorPalette = System.Drawing.Imaging.ColorPalette;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using PixelFormat = System.Windows.Media.PixelFormat;
using DrawingRectangle = System.Drawing.Rectangle;
using System.Drawing.Imaging;
using System.Windows.Controls;
using Bitmap = System.Drawing.Bitmap;

namespace System.Windows
{
    public static class WpfDataObjectExtensions
    {
        #region DLL imports

        [DllImport("gdiplus.dll")]
        private static extern bool DeleteObject(IntPtr hgdi);

        #endregion // DLL imports

        /// <summary>
        /// Sets the drag image by rendering the specified UIElement.
        /// </summary>
        /// <param name="dataObject">The DataObject to set the drag image for.</param>
        /// <param name="element">The element to render as the drag image.</param>
        /// <param name="cursorOffset">The offset of the cursor relative to the UIElement.</param>
        public static void SetDragImage(this IDataObject dataObject, UIElement element, Point cursorOffset)
        {
            Size size = element.RenderSize;

            // Get the device's DPI so we render at full size
            int dpix, dpiy;
            GetDeviceDpi(element, out dpix, out dpiy);

            // Create our renderer at full size
            RenderTargetBitmap renderSource = new RenderTargetBitmap(
                (int)size.Width, (int)size.Height, dpix, dpiy, PixelFormats.Pbgra32);

            // Render the element
            renderSource.Render(element);

            // Set the drag image by the bitmap source
            SetDragImage(dataObject, renderSource, cursorOffset);
        }

        /// <summary>
        /// Sets the drag image from a BitmapSource.
        /// </summary>
        /// <param name="dataObject">The DataObject on which to set the drag image.</param>
        /// <param name="image">The image source.</param>
        /// <param name="cursorOffset">The offset relative to the bitmap image.</param>
        public static void SetDragImage(this IDataObject dataObject, BitmapSource image, Point cursorOffset)
        {
            // Our internal routine requires an HBITMAP, so we'll convert the
            // BitmapSource to a System.Drawing.Bitmap.
            Bitmap bmp = GetBitmapFromBitmapSource(image, Colors.Magenta);
            
            // Sets the drag image from a Bitmap
            SetDragImage(dataObject, bmp, cursorOffset);
        }

        /// <summary>
        /// Sets the drag image.
        /// </summary>
        /// <param name="dataObject">The DataObject to set the drag image on.</param>
        /// <param name="image">The drag image.</param>
        /// <param name="cursorOffset">The location of the cursor relative to the image.</param>
        private static void SetDragImage(this IDataObject dataObject, Bitmap bitmap, Point cursorOffset)
        {
            ShDragImage shdi = new ShDragImage();

            Win32Size size;
            size.cx = bitmap.Width;
            size.cy = bitmap.Height;
            shdi.sizeDragImage = size;

            Win32Point wpt;
            wpt.x = (int)cursorOffset.X;
            wpt.y = (int)cursorOffset.Y;
            shdi.ptOffset = wpt;

            shdi.crColorKey = DrawingColor.Magenta.ToArgb();

            // This HBITMAP will be managed by the DragDropHelper
            // as soon as we pass it to InitializeFromBitmap. If we fail
            // to make the hand off, we'll delete it to prevent a mem leak.
            IntPtr hbmp = bitmap.GetHbitmap();
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
                // We failed to initialize the drag image, so the DragDropHelper
                // won't be managing our memory. Release the HBITMAP we allocated.
                DeleteObject(hbmp);
            }
        }

        #region Helper methods

        /// <summary>
        /// Gets the device capabilities.
        /// </summary>
        /// <param name="reference">A reference UIElement for getting the relevant device caps.</param>
        /// <param name="dpix">The horizontal DPI.</param>
        /// <param name="dpiy">The vertical DPI.</param>
        private static void GetDeviceDpi(Visual reference, out int dpix, out int dpiy)
        {
            Matrix m = PresentationSource.FromVisual(reference).CompositionTarget.TransformToDevice;
            dpix = (int)(96 * m.M11);
            dpiy = (int)(96 * m.M22);
        }

        /// <summary>
        /// Gets a System.Drawing.Bitmap from a BitmapSource.
        /// </summary>
        /// <param name="source">The source image from which to create our Bitmap.</param>
        /// <param name="transparencyKey">The transparency key. This is used by the DragDropHelper
        /// in rendering transparent pixels.</param>
        /// <returns>An instance of Bitmap which is a copy of the BitmapSource's image.</returns>
        private static Bitmap GetBitmapFromBitmapSource(BitmapSource source, Color transparencyKey)
        {
            // Copy at full size
            Int32Rect sourceRect = new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight);
            
            // Convert to our destination pixel format
            DrawingPixelFormat pxFormat = ConvertPixelFormat(source.Format);

            // Create the Bitmap, full size, full rez
            Bitmap bmp = new Bitmap(sourceRect.Width, sourceRect.Height, pxFormat);
            // If the format is an indexed format, copy the color palette
            if ((pxFormat & DrawingPixelFormat.Indexed) == DrawingPixelFormat.Indexed)
                ConvertColorPalette(bmp.Palette, source.Palette);

            // Get the transparency key as a System.Drawing.Color
            DrawingColor transKey = transparencyKey.ToDrawingColor();

            // Lock our Bitmap bits, we need to write to it
            BitmapData bmpData = bmp.LockBits(
                sourceRect.ToDrawingRectangle(),
                ImageLockMode.ReadWrite,
                pxFormat);
            {
                // Copy the source bitmap data to our new Bitmap
                source.CopyPixels(sourceRect, bmpData.Scan0, bmpData.Stride * sourceRect.Height, bmpData.Stride);

                // The drag image seems to work in full 32-bit color, except when
                // alpha equals zero. Then it renders those pixels at black. So
                // we make a pass and set all those pixels to the transparency key
                // color. This is only implemented for 32-bit pixel colors for now.
                if ((pxFormat & DrawingPixelFormat.Alpha) == DrawingPixelFormat.Alpha)
                    ReplaceTransparentPixelsWithTransparentKey(bmpData, transKey);
            }
            // Done, unlock the bits
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        /// <summary>
        /// Replaces any pixel with a zero alpha value with the specified transparency key.
        /// </summary>
        /// <param name="bmpData">The bitmap data in which to perform the operation.</param>
        /// <param name="transKey">The transparency color. This color is rendered transparent
        /// by the DragDropHelper.</param>
        /// <remarks>
        /// This function only supports 32-bit pixel formats for now.
        /// </remarks>
        private static void ReplaceTransparentPixelsWithTransparentKey(BitmapData bmpData, DrawingColor transKey)
        {
            DrawingPixelFormat pxFormat = bmpData.PixelFormat;

            if (DrawingPixelFormat.Format32bppArgb == pxFormat
                || DrawingPixelFormat.Format32bppPArgb == pxFormat)
            {
                int transKeyArgb = transKey.ToArgb();

                // We will just iterate over the data... we don't care about pixel location,
                // just that every pixel is checked.
                unsafe
                {
                    byte* pscan = (byte*)bmpData.Scan0.ToPointer();
                    {
                        for (int y = 0; y < bmpData.Height; ++y, pscan += bmpData.Stride)
                        {
                            int* prgb = (int*)pscan;
                            for (int x = 0; x < bmpData.Width; ++x, ++prgb)
                            {
                                // If the alpha value is zero, replace this pixel's color
                                // with the transparency key.
                                if ((*prgb & 0xFF000000L) == 0L)
                                    *prgb = transKeyArgb;
                            }
                        }
                    }
                }
            }
            else
            {
                // If it is anything else, we aren't supporting it, but we
                // won't throw, cause it isn't an error
                System.Diagnostics.Trace.TraceWarning("Not converting transparent colors to transparency key.");
                return;
            }
        }

        /// <summary>
        /// Converts a System.Windows.Media.Color to System.Drawing.Color.
        /// </summary>
        /// <param name="color">System.Windows.Media.Color value to convert.</param>
        /// <returns>System.Drawing.Color value.</returns>
        private static DrawingColor ToDrawingColor(this Color color)
        {
            return DrawingColor.FromArgb(
                color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts a System.Windows.Int32Rect to a System.Drawing.Rectangle value.
        /// </summary>
        /// <param name="rect">The System.Windows.Int32Rect to convert.</param>
        /// <returns>The System.Drawing.Rectangle converted value.</returns>
        private static DrawingRectangle ToDrawingRectangle(this Int32Rect rect)
        {
            return new DrawingRectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Converts the entries in a BitmapPalette to ColorPalette entries.
        /// </summary>
        /// <param name="destPalette">ColorPalette destination palette.</param>
        /// <param name="bitmapPalette">BitmapPalette source palette.</param>
        private static void ConvertColorPalette(DrawingColorPalette destPalette, BitmapPalette bitmapPalette)
        {
            DrawingColor[] destEntries = destPalette.Entries;
            IList<Color> sourceEntries = bitmapPalette.Colors;

            if (destEntries.Length < sourceEntries.Count)
                throw new ArgumentException("Destination palette has less entries than the source palette");

            for (int i = 0, count = sourceEntries.Count; i < count; ++i)
                destEntries[i] = sourceEntries[i].ToDrawingColor();
        }

        /// <summary>
        /// Converts a System.Windows.Media.PixelFormat instance to a
        /// System.Drawing.Imaging.PixelFormat value.
        /// </summary>
        /// <param name="pixelFormat">The input PixelFormat.</param>
        /// <returns>The converted value.</returns>
        private static DrawingPixelFormat ConvertPixelFormat(PixelFormat pixelFormat)
        {
            if (PixelFormats.Bgr24 == pixelFormat)
                return DrawingPixelFormat.Format24bppRgb;
            if (PixelFormats.Bgr32 == pixelFormat)
                return DrawingPixelFormat.Format32bppRgb;
            if (PixelFormats.Bgr555 == pixelFormat)
                return DrawingPixelFormat.Format16bppRgb555;
            if (PixelFormats.Bgr565 == pixelFormat)
                return DrawingPixelFormat.Format16bppRgb565;
            if (PixelFormats.Bgra32 == pixelFormat)
                return DrawingPixelFormat.Format32bppArgb;
            if (PixelFormats.BlackWhite == pixelFormat)
                return DrawingPixelFormat.Format1bppIndexed;
            if (PixelFormats.Gray16 == pixelFormat)
                return DrawingPixelFormat.Format16bppGrayScale;
            if (PixelFormats.Indexed1 == pixelFormat)
                return DrawingPixelFormat.Format1bppIndexed;
            if (PixelFormats.Indexed4 == pixelFormat)
                return DrawingPixelFormat.Format4bppIndexed;
            if (PixelFormats.Indexed8 == pixelFormat)
                return DrawingPixelFormat.Format8bppIndexed;
            if (PixelFormats.Pbgra32 == pixelFormat)
                return DrawingPixelFormat.Format32bppPArgb;
            if (PixelFormats.Prgba64 == pixelFormat)
                return DrawingPixelFormat.Format64bppPArgb;
            if (PixelFormats.Rgb24 == pixelFormat)
                return DrawingPixelFormat.Format24bppRgb;
            if (PixelFormats.Rgb48 == pixelFormat)
                return DrawingPixelFormat.Format48bppRgb;
            if (PixelFormats.Rgba64 == pixelFormat)
                return DrawingPixelFormat.Format64bppArgb;

            throw new NotSupportedException("The pixel format of the source bitmap is not supported.");
        }

        #endregion // Helper methods
    }
}
