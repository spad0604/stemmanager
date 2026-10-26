using Avalonia;
using Avalonia.Controls;
namespace STEMLabManager.Desktop.Controls;

public sealed class AdaptiveColumnsPanel : Panel
{
    public static readonly StyledProperty<double> RatioProperty=AvaloniaProperty.Register<AdaptiveColumnsPanel,double>(nameof(Ratio),2);
    public static readonly StyledProperty<double> BreakpointProperty=AvaloniaProperty.Register<AdaptiveColumnsPanel,double>(nameof(Breakpoint),760);
    public double Ratio {get=>GetValue(RatioProperty);set=>SetValue(RatioProperty,value);}
    public double Breakpoint {get=>GetValue(BreakpointProperty);set=>SetValue(BreakpointProperty,value);}
    private const double Gap=20;
    protected override Size MeasureOverride(Size available)
    {
        var children=Children.Where(c=>c.IsVisible).ToArray();if(children.Length==0)return default;
        var width=double.IsFinite(available.Width)?available.Width:1000;bool stacked=width<Breakpoint || children.Length!=2;
        double height=0;
        for(int i=0;i<children.Length;i++)
        {
            var childWidth=stacked?width:(width-Gap)*(i==0?Ratio:1)/(Ratio+1);
            children[i].Measure(new(childWidth,double.PositiveInfinity));height=stacked?height+children[i].DesiredSize.Height:Math.Max(height,children[i].DesiredSize.Height);
        }
        return new(width,height+(stacked?Gap*(children.Length-1):0));
    }
    protected override Size ArrangeOverride(Size final)
    {
        var children=Children.Where(c=>c.IsVisible).ToArray();bool stacked=final.Width<Breakpoint || children.Length!=2;double position=0;
        for(int i=0;i<children.Length;i++)
        {
            var width=stacked?final.Width:(final.Width-Gap)*(i==0?Ratio:1)/(Ratio+1);var height=stacked?children[i].DesiredSize.Height:final.Height;
            children[i].Arrange(new Rect(stacked?0:position,stacked?position:0,width,height));position+=(stacked?height:width)+Gap;
        }
        return final;
    }
}
public sealed class AdaptiveTilesPanel : Panel
{
    private const double Gap=16;
    private static int Columns(double width)=>width>=760?4:width>=380?2:1;
    protected override Size MeasureOverride(Size available)
    {
        var width=double.IsFinite(available.Width)?available.Width:1000;int columns=Columns(width);double height=0,rowHeight=0;
        for(int i=0;i<Children.Count;i++)
        {
            Children[i].Measure(new((width-(columns-1)*Gap)/columns,double.PositiveInfinity));rowHeight=Math.Max(rowHeight,Children[i].DesiredSize.Height);
            if(i%columns==columns-1 || i==Children.Count-1){height+=rowHeight+(i>=columns?Gap:0);rowHeight=0;}
        }
        return new(width,height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        int columns=Columns(final.Width);double width=(final.Width-(columns-1)*Gap)/columns,y=0;
        for(int start=0;start<Children.Count;start+=columns)
        {
            var height=Children.Skip(start).Take(columns).Max(c=>c.DesiredSize.Height);
            for(int col=0;col<columns && start+col<Children.Count;col++)Children[start+col].Arrange(new Rect(col*(width+Gap),y,width,height));y+=height+Gap;
        }
        return final;
    }
}
