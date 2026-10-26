using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace STEMLabManager.Desktop.Controls;

/// <summary>Small, consistent outline icons drawn as vectors; no font or bitmap dependency.</summary>
public sealed class AppIcon : Control
{
    public static readonly StyledProperty<string> KindProperty=AvaloniaProperty.Register<AppIcon,string>(nameof(Kind),"box");
    public static readonly StyledProperty<IBrush?> ForegroundProperty=TextBlock.ForegroundProperty.AddOwner<AppIcon>();
    public string Kind {get=>GetValue(KindProperty);set=>SetValue(KindProperty,value);}
    public IBrush? Foreground {get=>GetValue(ForegroundProperty);set=>SetValue(ForegroundProperty,value);}
    static AppIcon(){WidthProperty.OverrideDefaultValue<AppIcon>(22);HeightProperty.OverrideDefaultValue<AppIcon>(22);AffectsRender<AppIcon>(KindProperty,ForegroundProperty);}
    public override void Render(DrawingContext context)
    {
        var geometry=Paths.GetValueOrDefault(Kind)??Paths["box"];
        using(context.PushTransform(Matrix.CreateScale(Bounds.Width/24,Bounds.Height/24)))
            context.DrawGeometry(null,new Pen(Foreground??Brushes.Black,1.75,lineCap:PenLineCap.Round,lineJoin:PenLineJoin.Round),geometry);
    }
    private static readonly Dictionary<string,Geometry> Paths=new Dictionary<string,string>
    {
        ["dashboard"]="M3 3H10V10H3Z M14 3H21V7H14Z M3 14H10V21H3Z M14 11H21V21H14Z",
        ["box"]="M12 2L21 7V17L12 22L3 17V7Z M3 7L12 12L21 7 M12 12V22 M7.5 4.5L16.5 9.5",
        ["layers"]="M12 3L22 8L12 13L2 8Z M2 12L12 17L22 12 M2 16L12 21L22 16",
        ["clipboard"]="M8 4H5A2 2 0 0 0 3 6V20A2 2 0 0 0 5 22H19A2 2 0 0 0 21 20V6A2 2 0 0 0 19 4H16 M8 2H16V6H8Z M7 11H17 M7 15H13 M7 19H11",
        ["tool"]="M14 5A6 6 0 0 0 6 13L2 17A3 3 0 0 0 7 22L11 18A6 6 0 0 0 19 10L15 14L10 9Z",
        ["arrows"]="M3 7H21 M16 2L21 7L16 12 M21 17H3 M8 12L3 17L8 22",
        ["calendar"]="M5 4H19A2 2 0 0 1 21 6V20A2 2 0 0 1 19 22H5A2 2 0 0 1 3 20V6A2 2 0 0 1 5 4Z M7 2V6 M17 2V6 M3 10H21 M7 14H9 M13 14H15 M7 18H9",
        ["clock"]="M22 12A10 10 0 1 1 2 12A10 10 0 1 1 22 12 M12 6V12L16 14",
        ["users"]="M16 7A4 4 0 1 1 8 7A4 4 0 1 1 16 7 M4 21V19A6 6 0 0 1 10 13H14A6 6 0 0 1 20 19V21 M20 3A4 4 0 0 1 20 11 M22 16V21",
        ["user"]="M16 7A4 4 0 1 1 8 7A4 4 0 1 1 16 7 M4 21V19A6 6 0 0 1 10 13H14A6 6 0 0 1 20 19V21",
        ["chart"]="M3 3V21H22 M7 16L11 10L15 13L21 5",
        ["sparkles"]="M12 2L14.5 8.5L21 11L14.5 13.5L12 20L9.5 13.5L3 11L9.5 8.5Z M21 17V23 M18 20H24 M3 1V5 M1 3H5",
        ["scan"]="M3 8V3H8 M16 3H21V8 M21 16V21H16 M8 21H3V16 M9 9H9.1 M15 9H15.1 M8 15Q12 19 16 15",
        ["report"]="M5 2H14L20 8V22H5Z M14 2V8H20 M9 12H16 M9 16H16 M9 20H13",
        ["settings"]="M10 2H14L15 5L18 6L21 5L23 9L20 12L21 15L19 19L16 19L14 22H10L9 19L6 18L3 19L1 15L4 12L3 9L5 5L8 5Z M16 12A4 4 0 1 1 8 12A4 4 0 1 1 16 12",
        ["database"]="M21 5A9 3 0 1 1 3 5A9 3 0 1 1 21 5 M3 5V19A9 3 0 0 0 21 19V5 M3 12A9 3 0 0 0 21 12",
        ["history"]="M3 9A9 9 0 1 1 4 18 M3 3V9H9 M12 7V12L16 14",
        ["help"]="M22 12A10 10 0 1 1 2 12A10 10 0 1 1 22 12 M9 8A3 3 0 0 1 15 8Q15 10 12 12V14 M12 18H12.1",
        ["search"]="M17 10A7 7 0 1 1 3 10A7 7 0 1 1 17 10 M15 15L22 22",
        ["bell"]="M18 8A6 6 0 0 0 6 8V14L3 18H21L18 14Z M9 21Q12 24 15 21",
        ["plus"]="M12 5V19 M5 12H19",
        ["upload"]="M12 16V2 M6 8L12 2L18 8 M3 16V21H21V16",
        ["download"]="M12 2V16 M6 10L12 16L18 10 M3 16V21H21V16",
        ["image"]="M3 3H21V21H3Z M3 16L9 10L15 16L18 13L21 16 M18 7A2 2 0 1 1 14 7A2 2 0 1 1 18 7",
        ["camera"]="M8 4H16L18 7H22V21H2V7H6Z M16 14A4 4 0 1 1 8 14A4 4 0 1 1 16 14",
        ["edit"]="M4 16L16 4L20 8L8 20H4Z M14 6L18 10 M14 21H22",
        ["check"]="M4 12L9 17L20 6",
        ["close"]="M6 6L18 18 M18 6L6 18",
        ["refresh"]="M20 8A9 9 0 0 0 4 5L2 8 M2 2V8H8 M4 16A9 9 0 0 0 20 19L22 16 M16 16H22V22",
        ["lock"]="M5 10H19V22H5Z M8 10V6A4 4 0 0 1 16 6V10 M12 15V18",
        ["qr"]="M3 3H9V9H3Z M15 3H21V9H15Z M3 15H9V21H3Z M15 15H18V18H21V21H15Z M21 13H21.1 M12 3V6 M12 10V13H7 M3 12H3.1 M12 18V21",
        ["arrow-right"]="M4 12H20 M14 6L20 12L14 18",
        ["arrow-left"]="M20 12H4 M10 6L4 12L10 18",
        ["location"]="M20 10Q20 16 12 22Q4 16 4 10A8 8 0 0 1 20 10 M15 10A3 3 0 1 1 9 10A3 3 0 1 1 15 10",
        ["chevron-right"]="M9 5L16 12L9 19",
        ["shield"]="M12 2L21 6V12Q21 19 12 22Q3 19 3 12V6Z M8 12L11 15L17 9",
        ["wifi-off"]="M2 2L22 22 M2 9Q6 6 10 6 M14 6Q18 7 22 9 M5 13Q8 10 12 10 M17 12L19 13 M8 17Q12 13 16 17 M12 21H12.1",
        ["moon"]="M21 13A9 9 0 1 1 11 3A7 7 0 0 0 21 13",
        ["filter"]="M3 5H21 M6 12H18 M10 19H14",
        ["leaf"]="M21 3Q7 1 4 10Q1 20 10 21Q22 20 21 3Z M3 22L17 8",
    }.ToDictionary(p=>p.Key,p=>Geometry.Parse(p.Value));
}
