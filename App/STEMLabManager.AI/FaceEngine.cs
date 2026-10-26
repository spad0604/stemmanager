using System.Security.Cryptography;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public sealed record DetectedFace(float X,float Y,float Width,float Height,float Score,float[] Landmarks);
public sealed record FaceCandidate(long BorrowerId,double Similarity);
public sealed class FaceEngine
{
    public ModelInfo Inspect(string path)
    {
        if(!File.Exists(path))return new(path,"",false,"Không tìm thấy model. Chọn file ONNX trong Cài đặt.",[],[]);
        try
        {
            using var session=new InferenceSession(path);var sha=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            return new(path,sha,true,"Model ONNX nạp thành công.",session.InputMetadata.Select(x=>$"{x.Key}: [{string.Join(',',x.Value.Dimensions)}]").ToList(),session.OutputMetadata.Select(x=>$"{x.Key}: [{string.Join(',',x.Value.Dimensions)}]").ToList());
        }
        catch(Exception e) when(e is OnnxRuntimeException or IOException or DllNotFoundException or TypeInitializationException){return new(path,"",false,$"Model/native runtime không nạp được: {e.GetType().Name}. Kiểm tra file model và thư viện native.",[],[]);}
    }
    public IReadOnlyList<DetectedFace> Detect(byte[] image,string modelPath,float threshold=0.8f)
    {
        using var original=SKBitmap.Decode(image);Rules.Require(original!=null,"Không đọc được ảnh.");
        const int size=320;using var bitmap=original!.Resize(new SKImageInfo(size,size),new SKSamplingOptions(SKFilterMode.Linear));Rules.Require(bitmap!=null,"Không thể resize ảnh.");
        using var session=new InferenceSession(modelPath);var tensor=ImageTensor(bitmap!,false);
        using var outputs=session.Run([NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(),tensor)]);
        var values=outputs.ToDictionary(x=>x.Name,x=>x.AsTensor<float>().ToArray());var detections=new List<DetectedFace>();
        foreach(int stride in new[]{8,16,32})
        {
            Rules.Require(values.ContainsKey($"cls_{stride}") && values.ContainsKey($"obj_{stride}") && values.ContainsKey($"bbox_{stride}") && values.ContainsKey($"kps_{stride}"),"Model detector không phải YuNet có đầu ra cls/obj/bbox/kps.");
            var cls=values[$"cls_{stride}"];var obj=values[$"obj_{stride}"];var boxes=values[$"bbox_{stride}"];var kps=values[$"kps_{stride}"];
            for(int i=0;i<cls.Length;i++)
            {
                float score=MathF.Sqrt(Math.Clamp(cls[i],0,1)*Math.Clamp(obj[i],0,1));if(score<threshold)continue;
                int col=i%(size/stride),row=i/(size/stride);float cx=(col+boxes[i*4])*stride,cy=(row+boxes[i*4+1])*stride,w=MathF.Exp(boxes[i*4+2])*stride,h=MathF.Exp(boxes[i*4+3])*stride;
                var points=new float[10];for(int j=0;j<5;j++){points[j*2]=(col+kps[i*10+j*2])*stride*original.Width/size;points[j*2+1]=(row+kps[i*10+j*2+1])*stride*original.Height/size;}
                detections.Add(new((cx-w/2)*original.Width/size,(cy-h/2)*original.Height/size,w*original.Width/size,h*original.Height/size,score,points));
            }
        }
        var accepted=new List<DetectedFace>();foreach(var f in detections.OrderByDescending(x=>x.Score))if(accepted.All(g=>IoU(f,g)<0.3f))accepted.Add(f);return accepted;
    }
    private static float IoU(DetectedFace a,DetectedFace b)
    {float w=Math.Max(0,Math.Min(a.X+a.Width,b.X+b.Width)-Math.Max(a.X,b.X)),h=Math.Max(0,Math.Min(a.Y+a.Height,b.Y+b.Height)-Math.Max(a.Y,b.Y));return w*h/(a.Width*a.Height+b.Width*b.Height-w*h);}
    public float[] Embed(byte[] image,DetectedFace face,string modelPath)
    {
        using var bitmap=SKBitmap.Decode(image);Rules.Require(bitmap!=null,"Ảnh không hợp lệ.");Rules.Require(face.Width>=60 && face.Height>=60,"Khuôn mặt quá nhỏ, hãy chụp gần hơn.");
        // Least squares similarity alignment to SFace canonical five landmarks.
        float[] canonical=[38.2946f,51.6963f,73.5318f,51.5014f,56.0252f,71.7366f,41.5493f,92.3655f,70.7299f,92.2041f];
        double sx=0,sy=0,tx=0,ty=0;for(int i=0;i<5;i++){sx+=face.Landmarks[2*i];sy+=face.Landmarks[2*i+1];tx+=canonical[2*i];ty+=canonical[2*i+1];}sx/=5;sy/=5;tx/=5;ty/=5;
        double den=0,a=0,b=0;for(int i=0;i<5;i++){double x=face.Landmarks[2*i]-sx,y=face.Landmarks[2*i+1]-sy,u=canonical[2*i]-tx,v=canonical[2*i+1]-ty;den+=x*x+y*y;a+=x*u+y*v;b+=x*v-y*u;}
        Rules.Require(den>1,"Landmark không hợp lệ.");a/=den;b/=den;
        using var aligned=new SKBitmap(112,112);using(var canvas=new SKCanvas(aligned)){canvas.Clear(SKColors.Black);canvas.SetMatrix(new SKMatrix((float)a,(float)-b,(float)(tx-a*sx+b*sy),(float)b,(float)a,(float)(ty-b*sx-a*sy),0,0,1));canvas.DrawBitmap(bitmap,0,0);}
        using var session=new InferenceSession(modelPath);var tensor=ImageTensor(aligned,true);
        using var output=session.Run([NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(),tensor)]);var vector=output.First().AsTensor<float>().ToArray();
        double norm=Math.Sqrt(vector.Sum(x=>(double)x*x));Rules.Require(norm>0,"Embedding trống.");for(int i=0;i<vector.Length;i++)vector[i]/=(float)norm;return vector;
    }
    private static DenseTensor<float> ImageTensor(SKBitmap bitmap,bool rgb)
    {
        var t=new DenseTensor<float>([1,3,bitmap.Height,bitmap.Width]);for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){var p=bitmap.GetPixel(x,y);t[0,0,y,x]=rgb?p.Red:p.Blue;t[0,1,y,x]=p.Green;t[0,2,y,x]=rgb?p.Blue:p.Red;}return t;
    }
    public static IReadOnlyList<FaceCandidate> Match(float[] query,IReadOnlyList<(long BorrowerId,float[] Embedding)> profiles,double threshold)
    {
        Rules.Require(threshold>=0.3 && threshold<=1,"Ngưỡng không hợp lệ.");var result=new List<FaceCandidate>();
        foreach(var p in profiles)
        {
            if(p.Embedding.Length!=query.Length)continue;double dot=0,a=0,b=0;for(int i=0;i<query.Length;i++){dot+=(double)query[i]*p.Embedding[i];a+=(double)query[i]*query[i];b+=(double)p.Embedding[i]*p.Embedding[i];}
            var score=a*b==0?0:dot/Math.Sqrt(a*b);if(score>=threshold)result.Add(new(p.BorrowerId,score));
        }
        return result.OrderByDescending(x=>x.Similarity).Take(5).ToList();
    }
}
