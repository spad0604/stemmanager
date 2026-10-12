using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using STEMLabManager.Application;
namespace STEMLabManager.Desktop.Controls;

public abstract class MetricChart : Control
{
    public static readonly StyledProperty<IEnumerable<MetricPoint>?> ItemsSourceProperty=AvaloniaProperty.Register<MetricChart,IEnumerable<MetricPoint>?>(nameof(ItemsSource));
    public static readonly StyledProperty<IBrush?> ForegroundProperty=TextBlock.ForegroundProperty.AddOwner<MetricChart>();
    public static readonly StyledProperty<IBrush?> GridBrushProperty=AvaloniaProperty.Register<MetricChart,IBrush?>(nameof(GridBrush),Brush.Parse("#E4EAF5"));
    public IEnumerable<MetricPoint>? ItemsSource {get=>GetValue(ItemsSourceProperty);set=>SetValue(ItemsSourceProperty,value);}
    public IBrush? Foreground {get=>GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
    public IBrush? GridBrush {get=>GetValue(GridBrushProperty);set=>SetValue(GridBrushProperty,value);}
    protected IReadOnlyList<MetricPoint> Points=>ItemsSource?.ToList()??[];
    static MetricChart(){AffectsRender<MetricChart>(ItemsSourceProperty,ForegroundProperty,GridBrushProperty);}
    protected FormattedText Text(string value,double size=11,IBrush? brush=null,FontWeight weight=FontWeight.Normal)=>new(value,CultureInfo.GetCultureInfo("vi-VN"),FlowDirection.LeftToRight,new Typeface(FontFamily.Default,FontStyle.Normal,weight),size,brush??Foreground??Brushes.Black);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if(change.Property==ItemsSourceProperty){if(change.OldValue is INotifyCollectionChanged old)old.CollectionChanged-=Changed;if(change.NewValue is INotifyCollectionChanged current)current.CollectionChanged+=Changed;}
    }
    private void Changed(object? sender,NotifyCollectionChangedEventArgs e)=>InvalidateVisual();
}
public sealed class TrendChart : MetricChart
{
    private int? hovered;
    private static readonly IBrush Blue=Brush.Parse("#315BDB");
    public TrendChart(){MinHeight=220;PointerMoved+=Moved;PointerExited+=(_,_)=>{hovered=null;InvalidateVisual();};}
    private void Moved(object? sender,PointerEventArgs e)
    {
        var points=Points;var x=e.GetPosition(this).X;hovered=points.Count==0?null:Math.Clamp((int)Math.Round((x-38)/Math.Max(1,Bounds.Width-54)*(points.Count-1)),0,points.Count-1);InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        var data=Points;double left=38,top=20,width=Math.Max(1,Bounds.Width-left-16),height=Math.Max(1,Bounds.Height-top-30);
        if(data.Count==0){context.DrawText(Text("Chưa có dữ liệu trong kỳ",13),new(20,80));return;}
        double maximum=Math.Max(4,Math.Ceiling(data.Max(p=>p.Value)/4)*4);
        for(int i=0;i<=4;i++)
        {
            double y=top+height-height*i/4;context.DrawLine(new Pen(GridBrush,1),new(left,y),new(left+width,y));var label=Text((maximum*i/4).ToString("0.#"));context.DrawText(label,new(left-label.Width-10,y-label.Height/2));
        }
        Point At(int i)=>new(left+width*i/Math.Max(1,data.Count-1),top+height-height*data[i].Value/maximum);
        var fill=new StreamGeometry();using(var path=fill.Open()){path.BeginFigure(new(left,top+height),true);for(int i=0;i<data.Count;i++)path.LineTo(At(i));path.LineTo(new(left+width,top+height));path.EndFigure(true);}
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(18,49,91,219)),null,fill);
        var line=new StreamGeometry();using(var path=line.Open()){path.BeginFigure(At(0),false);for(int i=1;i<data.Count;i++)path.LineTo(At(i));path.EndFigure(false);}
        context.DrawGeometry(null,new Pen(Blue,2.75,lineCap:PenLineCap.Round,lineJoin:PenLineJoin.Round),line);
        foreach(var index in new[]{0,data.Count/3,data.Count*2/3,data.Count-1}.Distinct())
        {var label=Text(data[index].Label);var x=Math.Clamp(At(index).X-label.Width/2,0,Math.Max(0,Bounds.Width-label.Width));context.DrawText(label,new(x,top+height+12));}
        if(hovered is { } selected && selected<data.Count)
        {
            var point=At(selected);context.DrawLine(new Pen(Blue,1,new DashStyle([3,4],0)),new(point.X,top),new(point.X,top+height));context.DrawEllipse(Brushes.White,new Pen(Blue,3),point,4.5,4.5);
            var label=Text($"{data[selected].Label}  ·  {data[selected].Value:0} phiếu",12,Brushes.White,FontWeight.SemiBold);double x=Math.Clamp(point.X-label.Width/2-12,0,Math.Max(0,Bounds.Width-label.Width-24));double y=Math.Max(0,point.Y-42);
            context.DrawRectangle(Blue,null,new Rect(x,y,label.Width+24,30),8,8);context.DrawText(label,new(x+12,y+7));
        }
    }
}
public sealed class RankingChart : MetricChart
{
    private static readonly IBrush[] Palette=[Brush.Parse("#315BDB"),Brush.Parse("#008579"),Brush.Parse("#8754CF"),Brush.Parse("#C58B29"),Brush.Parse("#C24968")];
    public RankingChart(){MinHeight=255;}
    public override void Render(DrawingContext context)
    {
        var data=Points.Take(5).ToList();if(data.Count==0){context.DrawText(Text("Chưa có giao dịch trong kỳ",13),new(4,70));return;}
        double maximum=Math.Max(1,data.Max(p=>p.Value));double width=Math.Max(1,Bounds.Width-8);
        for(int i=0;i<data.Count;i++)
        {
            double y=i*49;var value=Text(data[i].Value.ToString("0"),12,Foreground,FontWeight.SemiBold);var name=Text(data[i].Label,12);name.MaxTextWidth=Math.Max(1,width-value.Width-20);name.Trimming=TextTrimming.CharacterEllipsis;
            context.DrawText(name,new(0,y));context.DrawText(value,new(width-value.Width,y));
            context.DrawRectangle(GridBrush,null,new Rect(0,y+25,width,7),3.5,3.5);var length=width*data[i].Value/maximum;if(length>0)context.DrawRectangle(Palette[i],null,new Rect(0,y+25,length,7),3.5,3.5);
        }
    }
}
public sealed class DonutChart : Control
{
    public static readonly StyledProperty<double> ValueProperty=AvaloniaProperty.Register<DonutChart,double>(nameof(Value));
    public static readonly StyledProperty<IBrush?> ForegroundProperty=TextBlock.ForegroundProperty.AddOwner<DonutChart>();
    public static readonly StyledProperty<IBrush?> TrackBrushProperty=AvaloniaProperty.Register<DonutChart,IBrush?>(nameof(TrackBrush),Brush.Parse("#EAF0FC"));
    public double Value {get=>GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public IBrush? Foreground {get=>GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
    public IBrush? TrackBrush {get=>GetValue(TrackBrushProperty);set=>SetValue(TrackBrushProperty,value);}
    static DonutChart(){AffectsRender<DonutChart>(ValueProperty,ForegroundProperty,TrackBrushProperty);}
    public override void Render(DrawingContext context)
    {
        var center=new Point(Bounds.Width/2,Bounds.Height/2);double radius=Math.Max(1,Math.Min(Bounds.Width,Bounds.Height)/2-10),fraction=double.IsFinite(Value)?Math.Clamp(Value,0,1):0;
        var accent=Brush.Parse("#C24957");context.DrawEllipse(null,new Pen(TrackBrush,11),center,radius,radius);
        if(fraction>=0.9999)context.DrawEllipse(null,new Pen(accent,11),center,radius,radius);
        else if(fraction>0)
        {double angle=fraction*Math.PI*2-Math.PI/2;var arc=new StreamGeometry();using(var path=arc.Open()){path.BeginFigure(new(center.X,center.Y-radius),false);path.ArcTo(new(center.X+radius*Math.Cos(angle),center.Y+radius*Math.Sin(angle)),new(radius,radius),0,fraction>0.5,SweepDirection.Clockwise);path.EndFigure(false);}context.DrawGeometry(null,new Pen(accent,11,lineCap:PenLineCap.Round),arc);}
        var value=new FormattedText(fraction.ToString("P1",CultureInfo.GetCultureInfo("vi-VN")),CultureInfo.GetCultureInfo("vi-VN"),FlowDirection.LeftToRight,new Typeface(FontFamily.Default,FontStyle.Normal,FontWeight.SemiBold),25,Foreground??Brushes.Black);
        context.DrawText(value,new(center.X-value.Width/2,center.Y-value.Height/2));
    }
}
