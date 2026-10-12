using OpenCvSharp;
using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public sealed class CameraCapture
{
    public Task<byte[]> CaptureAsync(int index,CancellationToken cancellation=default) => Task.Run(()=>
    {
        if(!OperatingSystem.IsWindows())throw new BusinessException("Camera native được đóng gói cho Windows. Trên Ubuntu, dùng ảnh kiểm tra hoặc QR/mã người mượn.");
        try
        {
            using var capture=new VideoCapture(index,VideoCaptureAPIs.DSHOW);Rules.Require(capture.IsOpened(),"Không mở được camera. Kiểm tra quyền camera hoặc dùng QR/mã.");
            using var frame=new Mat();for(int i=0;i<5;i++){cancellation.ThrowIfCancellationRequested();capture.Read(frame);}
            Rules.Require(!frame.Empty(),"Camera không trả ảnh.");Cv2.ImEncode(".png",frame,out var bytes);return bytes;
        }
        catch(Exception e) when(e is DllNotFoundException or TypeInitializationException){throw new BusinessException("Camera native chưa khả dụng. Dùng QR/mã hoặc ảnh kiểm tra.");}
    },cancellation);
}
