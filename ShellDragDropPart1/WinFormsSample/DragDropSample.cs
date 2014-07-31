using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using DragDropLib;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using DataObject = System.Windows.Forms.DataObject;

class DragDropSample : Form
{
    public DragDropSample()
    {
        this.AllowDrop = true;
        
        Button bt = new Button();
        bt.Anchor = AnchorStyles.None;
        bt.Size = new Size(100, 100);
        bt.Location = new Point((ClientRectangle.Width - bt.Width) / 2, (ClientRectangle.Height - bt.Height) / 2);
        bt.Text = "Drag me";
        bt.MouseDown += new MouseEventHandler(bt_MouseDown);
        
        Controls.Add(bt);
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;
        IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
        dropHelper.DragEnter(IntPtr.Zero, (ComIDataObject)e.Data, ref wp, (int)e.Effect);
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;
        IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
        dropHelper.DragOver(ref wp, (int)e.Effect);
    }

    protected override void OnDragLeave(EventArgs e)
    {
        IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
        dropHelper.DragLeave();
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        e.Effect = DragDropEffects.Copy;
        Point p = Cursor.Position;
        Win32Point wp;
        wp.x = p.X;
        wp.y = p.Y;
        IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
        dropHelper.Drop((ComIDataObject)e.Data, ref wp, (int)e.Effect);
    }
    
    void bt_MouseDown(object sender, MouseEventArgs e)
    {
        Bitmap bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Magenta);
            using (Pen pen = new Pen(Color.Blue, 4f))
            {
                g.DrawEllipse(pen, 20, 20, 60, 60);
            }
        }
        
        DataObject data = new DataObject(new DragDropLib.DataObject());
        
        ShDragImage shdi = new ShDragImage();
        Win32Size size;
        size.cx = bmp.Width;
        size.cy = bmp.Height;
        shdi.sizeDragImage = size;
        Point p = e.Location;
        Win32Point wpt;
        wpt.x = p.X;
        wpt.y = p.Y;
        shdi.ptOffset = wpt;
        shdi.hbmpDragImage = bmp.GetHbitmap();
        shdi.crColorKey = Color.Magenta.ToArgb();
        
        IDragSourceHelper sourceHelper = (IDragSourceHelper)new DragDropHelper();
        sourceHelper.InitializeFromBitmap(ref shdi, data);
        
        DoDragDrop(data, DragDropEffects.Copy);
    }

    [STAThread]
    static void Main()
    {
        Application.Run(new DragDropSample());
    }
}
