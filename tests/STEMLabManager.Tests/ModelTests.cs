using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using STEMLabManager.AI;
using Xunit;
namespace STEMLabManager.Tests;

public sealed class ModelTests
{
    private static string Root
    {
        get {var p=new DirectoryInfo(AppContext.BaseDirectory);while(p!=null && !File.Exists(Path.Combine(p.FullName,"STEMManager.sln")))p=p.Parent;return p?.FullName??throw new InvalidOperationException("Repository root missing.");}
    }
    [Fact] public void SuppliedYuNetLoadsAndRunsOnBlankImage()
    {
        var model=Path.Combine(Root,"face_detection_yunet_2026may.onnx");var engine=new FaceEngine();var info=engine.Inspect(model);Assert.True(info.Loaded,info.Status);Assert.Equal(64,info.Sha256.Length);
        using var bitmap=new SKBitmap(320,320);bitmap.Erase(SKColors.Black);using var image=SKImage.FromBitmap(bitmap);using var data=image.Encode(SKEncodedImageFormat.Png,100);
        var faces=engine.Detect(data.ToArray(),model);Assert.Empty(faces);
    }
    [Fact] public void SFaceRunsRealInference()
    {
        var model=Path.Combine(Root,"face_recognition_sface_2021dec.onnx");var info=new FaceEngine().Inspect(model);Assert.True(info.Loaded,info.Status);
        using var session=new InferenceSession(model);using var output=session.Run([NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(),new DenseTensor<float>([1,3,112,112]))]);Assert.Equal(128,output.First().AsTensor<float>().Length);
    }
    [Fact] public void MissingModelHasExplicitStatus(){var info=new FaceEngine().Inspect("missing-model.onnx");Assert.False(info.Loaded);Assert.Contains("Không tìm thấy",info.Status);}
}
