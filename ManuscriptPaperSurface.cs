using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TagWriter;

public sealed class ManuscriptPaperSurface : FrameworkElement
{
    public int PageIndex { get; set; }
    public double Zoom { get; set; } = 1;
    public double CellSize { get; set; } = 34;
    public double PaperMargin { get; set; } = 42;
    public char[] Cells { get; set; } = Array.Empty<char>();
    public int CaretCell { get; set; } = -1;
    public int SelectionStartCell { get; set; } = -1;
    public int SelectionEndCell { get; set; } = -1;
    public int HoverCell { get; private set; } = -1;
    public Brush PaperBrush { get; set; } = Brushes.White;
    public Brush GridBrush { get; set; } = Brushes.Gray;
    public Brush TextBrush { get; set; } = Brushes.Black;
    public Brush CaretBrush { get; set; } = Brushes.LightBlue;
    public Brush HoverBrush { get; set; } = Brushes.LightGray;
    public Brush SelectionBrush { get; set; } = Brushes.LightBlue;
    public event Action<ManuscriptPaperSurface,int,MouseButtonEventArgs>? CellMouseDown;
    public event Action<ManuscriptPaperSurface,int,MouseEventArgs>? CellMouseMove;
    public event Action<ManuscriptPaperSurface,MouseButtonEventArgs>? CellMouseUp;

    public ManuscriptPaperSurface()
    {
        SnapsToDevicePixels = true;
        MouseMove += OnMouseMove;
        MouseLeave += (_,_) => { HoverCell=-1; InvalidateVisual(); };
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
    }

    int HitCell(Point p)
    {
        var col=(int)((p.X/Zoom-PaperMargin)/CellSize);
        var row=(int)((p.Y/Zoom-PaperMargin)/CellSize);
        if(col<0||col>=20||row<0||row>=10)return -1;
        return PageIndex*200+row*20+col;
    }
    void OnMouseMove(object? s,MouseEventArgs e)
    {
        var cell=HitCell(e.GetPosition(this));
        if(cell!=HoverCell){HoverCell=cell;InvalidateVisual();}
        if(e.LeftButton==MouseButtonState.Pressed&&cell>=0)CellMouseMove?.Invoke(this,cell,e);
    }
    void OnMouseDown(object? s,MouseButtonEventArgs e)
    {
        var cell=HitCell(e.GetPosition(this));
        if(cell<0)return;
        CaptureMouse();
        CellMouseDown?.Invoke(this,cell,e);
        e.Handled=true;
    }
    void OnMouseUp(object? s,MouseButtonEventArgs e)
    {
        if(IsMouseCaptured)ReleaseMouseCapture();
        CellMouseUp?.Invoke(this,e);
        e.Handled=true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(PaperBrush,null,new Rect(0,0,ActualWidth,ActualHeight));
        var pen=new Pen(GridBrush,1);
        var left=PaperMargin*Zoom; var top=PaperMargin*Zoom;
        var cell=CellSize*Zoom;
        for(int r=0;r<=10;r++){var y=top+r*cell;dc.DrawLine(pen,new Point(left,y),new Point(left+20*cell,y));}
        for(int c=0;c<=20;c++){var x=left+c*cell;dc.DrawLine(pen,new Point(x,top),new Point(x,top+10*cell));}

        void FillCell(int global,Brush brush,double opacity)
        {
            if(global<PageIndex*200||global>=PageIndex*200+200)return;
            var local=global-PageIndex*200;
            dc.PushOpacity(opacity);
            dc.DrawRectangle(brush,null,new Rect(left+(local%20)*cell,top+(local/20)*cell,cell,cell));
            dc.Pop();
        }
        if(HoverCell>=0)FillCell(HoverCell,HoverBrush,.45);
        if(SelectionStartCell>=0&&SelectionEndCell>SelectionStartCell)
            for(int i=Math.Max(SelectionStartCell,PageIndex*200);i<Math.Min(SelectionEndCell,PageIndex*200+200);i++)FillCell(i,SelectionBrush,.5);
        if(CaretCell>=0)FillCell(CaretCell,CaretBrush,.55);

        var typeface=new Typeface(new FontFamily("Batang"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
        var font=Math.Max(7,17*Zoom);
        for(int i=0;i<Math.Min(200,Cells.Length);i++)
        {
            var ch=Cells[i]; if(ch=='\0')continue;
            var ft=new FormattedText(ch.ToString(),CultureInfo.CurrentCulture,FlowDirection.LeftToRight,typeface,font,TextBrush,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var x=left+(i%20)*cell+(cell-ft.Width)/2; var y=top+(i/20)*cell+(cell-ft.Height)/2;
            dc.DrawText(ft,new Point(x,y));
        }
        var pageText=new FormattedText((PageIndex+1).ToString(),CultureInfo.CurrentCulture,FlowDirection.LeftToRight,typeface,Math.Max(8,10*Zoom),GridBrush,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(pageText,new Point((ActualWidth-pageText.Width)/2,ActualHeight-28*Zoom));
    }
}
