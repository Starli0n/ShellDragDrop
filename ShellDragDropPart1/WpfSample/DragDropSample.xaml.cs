using System;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DragDropLib;
using Bitmap = System.Drawing.Bitmap;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using DataObject = System.Windows.DataObject;

namespace WpfSample
{
    public partial class DragDropSample : Window
    {
        public DragDropSample()
        {
            InitializeComponent();

            this.AllowDrop = true;

            Grid grid = new Grid();

            Border border = new Border();
            border.Width = 100.0;
            border.Height = 100.0;

            Rectangle rect = new Rectangle();
            rect.RadiusX = 4.0;
            rect.RadiusY = 4.0;
            rect.Stroke = new SolidColorBrush(Colors.Orange);
            rect.Fill = new LinearGradientBrush(Colors.Blue, Colors.White, 90.0);
            rect.MouseDown += new MouseButtonEventHandler(this.rect_MouseDown);

            border.Child = rect;

            grid.Children.Add(border);

            this.Content = grid;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragEnter(wndHelper.Handle, (ComIDataObject)e.Data, ref wp, (int)e.Effects);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragLeave();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.DragOver(ref wp, (int)e.Effects);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            Win32Point wp;
            e.Effects = DragDropEffects.Copy;
            Point p = e.GetPosition(this);
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
            dropHelper.Drop((ComIDataObject)e.Data, ref wp, (int)e.Effects);
        }

        private void rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;

            RenderTargetBitmap rbmp = new RenderTargetBitmap(100, 100, 96.0, 96.0, PixelFormats.Default);
            rbmp.Render(rect);

            Bitmap bmp = new Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            
            BitmapData bdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            rbmp.CopyPixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bdata.Scan0, bdata.Stride * bmp.Height, bdata.Stride);

            int magenta = System.Drawing.Color.Magenta.ToArgb();
            unsafe
            {
                byte* pscan = (byte*)bdata.Scan0.ToPointer();
                for (int y = 0; y < bmp.Height; ++y, pscan += bdata.Stride)
                {
                    int* prgb = (int*)pscan;
                    for (int x = 0; x < bmp.Width; ++x, ++prgb)
                    {
                        if ((*prgb & 0xff000000L) == 0L)
                            *prgb = magenta;
                    }
                }
            }
            
            bmp.UnlockBits(bdata);

            DataObject data = new DataObject(new DragDropLib.DataObject());

            ShDragImage shdi = new ShDragImage();
            Win32Size size;
            size.cx = bmp.Width;
            size.cy = bmp.Height;
            shdi.sizeDragImage = size;
            Point p = e.GetPosition(rect);
            Win32Point wpt;
            wpt.x = (int)p.X;
            wpt.y = (int)p.Y;
            shdi.ptOffset = wpt;
            shdi.hbmpDragImage = bmp.GetHbitmap();
            shdi.crColorKey = System.Drawing.Color.Magenta.ToArgb();

            IDragSourceHelper sourceHelper = (IDragSourceHelper)new DragDropHelper();
            sourceHelper.InitializeFromBitmap(ref shdi, data);

            DragDrop.DoDragDrop(rect, data, DragDropEffects.Copy);
        }
    }
}