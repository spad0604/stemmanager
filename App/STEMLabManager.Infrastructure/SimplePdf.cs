using SkiaSharp;
namespace STEMLabManager.Infrastructure;

public static class SimplePdf
{
    public static void Write(string path,IReadOnlyList<string[]> rows)
    {
        using var output=File.Open(path,FileMode.Create,FileAccess.Write);
        using var doc=SKDocument.CreatePdf(output);
        using var typeface=SKTypeface.FromFamilyName(OperatingSystem.IsWindows()?"Segoe UI":"DejaVu Sans");
        using var font=new SKFont(typeface,10); using var paint=new SKPaint { Color=SKColors.Black,IsAntialias=true };
        SKCanvas? canvas=null;float y=0;int page=0;
        foreach(var row in rows)
        {
            var text=string.Join(" | ",row);var lines=Wrap(text,font,740);
            foreach(var line in lines)
            {
                if(canvas is null || y>550)
                {
                    if(canvas!=null)doc.EndPage();canvas=doc.BeginPage(842,595);page++;y=35;
                    canvas.DrawText($"STEM Lab Manager — Báo cáo • {DateTime.Now:dd/MM/yyyy HH:mm} • Trang {page}",30,y,font,paint);y+=25;
                }
                canvas.DrawText(line,30,y,font,paint);y+=15;
            }
            y+=5;
        }
        if(canvas!=null)doc.EndPage();doc.Close();
    }
    private static IEnumerable<string> Wrap(string text,SKFont font,float width)
    {
        var current="";
        foreach(var word in text.Replace('\r',' ').Replace('\n',' ').Split(' '))
        {
            var next=current.Length==0?word:current+" "+word;
            if(font.MeasureText(next)>width && current.Length>0){yield return current;current=word;}else current=next;
        }
        if(current.Length>0)yield return current;
    }
}
