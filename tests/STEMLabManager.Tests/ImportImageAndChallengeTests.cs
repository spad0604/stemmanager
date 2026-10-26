using SkiaSharp;
using STEMLabManager.Infrastructure;
using STEMLabManager.Domain;
using Xunit;
namespace STEMLabManager.Tests;

public sealed class ImportImageAndChallengeTests
{
    [Fact] public async Task AssetImportIsValidatedAndAtomic()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-assets-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","ImportPassword123!","Admin","Room");var category=(await service.CategoriesAsync())[0];var location=(await service.LocationsAsync())[0];
            var file=Path.Combine(directory,"assets.csv");await File.WriteAllTextAsync(file,$"Code,Name,Kind,Category,Location,Unit,Quantity\nA1,Robot,Unit,{category.Name},{location.Name},cái,2\nA2,Invalid,Wrong,{category.Name},{location.Name},cái,1");
            var preview=await service.PreviewAssetsAsync(file);Assert.NotEmpty(preview.Errors);await Assert.ThrowsAsync<BusinessException>(()=>service.ImportAssetsAsync(preview));Assert.Empty(await service.AssetsAsync());
            await File.WriteAllTextAsync(file,$"Code,Name,Kind,Category,Location,Unit,Quantity\nA1,Robot,Unit,{category.Name},{location.Name},cái,2");preview=await service.PreviewAssetsAsync(file);await service.ImportAssetsAsync(preview);Assert.Equal(2,(await service.UnitsAsync((await service.AssetsAsync()).Single().Id)).Count);
            await Assert.ThrowsAsync<BusinessException>(()=>service.ImportAssetsAsync(preview));Assert.Single(await service.AssetsAsync());
        }
        finally{service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [Fact] public async Task ImagesAndSecretsSurviveRestoreToDifferentDirectory()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-image-"+Guid.NewGuid().ToString("N"));var target=Path.Combine(Path.GetTempPath(),"stem-target-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);var other=new LabService(target);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","ImagePassword123!","Admin","Room");var c=(await service.CategoriesAsync())[0].Id;var l=(await service.LocationsAsync())[0].Id;var asset=await service.SaveAssetAsync(null,new("A1","Image asset",AssetKind.Unit,c,l,"cái",1));
            using var bitmap=new SKBitmap(50,50);bitmap.Erase(SKColors.Blue);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,90);var source=Path.Combine(directory,"source.png");await File.WriteAllBytesAsync(source,data.ToArray());await service.ImportAssetImageAsync(asset,source);await service.SaveSettingAsync("llm.secret","secret-to-restore");var backup=await service.BackupAsync();
            await other.InitializeAsync();await other.SetupAsync("temporary","TempPassword123!","Temp","Room");await other.RestoreAsync(backup);await other.LoginAsync("admin","ImagePassword123!");Assert.Equal("secret-to-restore",await other.ReadLlmKeyAsync());var restored=await other.AssetDetailsAsync(asset);Assert.NotNull(restored.ImagePath);Assert.True(File.Exists(Path.Combine(target,restored.ImagePath!)));Assert.StartsWith("Images/",restored.ImagePath);
        }
        finally{service.Logout();other.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);Directory.Delete(target,true);}
    }
}
