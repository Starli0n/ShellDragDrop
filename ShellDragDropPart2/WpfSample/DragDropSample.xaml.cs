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
using System.Collections.Generic;

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
            e.Effects = DragDropEffects.Copy;
            DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
            e.Handled = true;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            DropTargetHelper.DragLeave();
            e.Handled = true;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            DropTargetHelper.DragOver(e.GetPosition(this), e.Effects);
            e.Handled = true;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), e.Effects);
            e.Handled = true;
        }

        private void rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;

            DataObject data = new DataObject(new DragDropLib.DataObject());
            data.SetDragImage(rect, e.GetPosition(rect));

            DragDrop.DoDragDrop(rect, data, DragDropEffects.Copy);
        }
    }
}