using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    public Task ImportAssetImageAsync(long id,string sourcePath)=>WriteAsync(Permission.ManageAssets,async db=>
    {
        var asset=await db.Assets.SingleAsync(x=>x.Id==id);Rules.Require(new FileInfo(sourcePath).Length<=10*1024*1024,"Ảnh tối đa 10 MB.");
        var bytes=await File.ReadAllBytesAsync(sourcePath);using var bitmap=SKBitmap.Decode(bytes);Rules.Require(bitmap!=null,"Không đọc được ảnh.");
        var directory=Path.Combine(DataDirectory,"Images");Directory.CreateDirectory(directory);var filename=$"{Guid.NewGuid():N}.png";
        using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,90);await File.WriteAllBytesAsync(Path.Combine(directory,filename),data.ToArray());
        asset.ImagePath="Images/"+filename;Audit(db,"SetAssetImage","Asset",id);
    });
}
