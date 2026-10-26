using OpenCvSharp;
using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public sealed class CameraCapture
{
    public Task<CameraSession> OpenAsync(int index,CancellationToken cancellation=default) => Task.Run(() =>
    {
        if(!OperatingSystem.IsWindows())throw new BusinessException("Camera native is supported on Windows only.");
        try
        {
            var capture=new VideoCapture(index,VideoCaptureAPIs.DSHOW);
            if(!capture.IsOpened()){capture.Dispose();throw new BusinessException("Không mở được camera. Kiểm tra quyền camera hoặc dùng QR/mã.");}
            return new CameraSession(capture);
        }
        catch(Exception e) when(e is DllNotFoundException or TypeInitializationException){throw new BusinessException("Camera native chưa khả dụng. Dùng QR/mã hoặc ảnh kiểm tra.");}
    },cancellation);
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

public sealed class CameraSession : IDisposable
{
    private readonly VideoCapture capture;
    private readonly object gate=new();
    private bool disposed;
    internal CameraSession(VideoCapture capture)=>this.capture=capture;
    public Task<byte[]> ReadFrameAsync(CancellationToken cancellation=default)=>Task.Run(() =>
    {
        cancellation.ThrowIfCancellationRequested();
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            using var frame=new Mat();capture.Read(frame);
            Rules.Require(!frame.Empty(),"Camera không trả ảnh. Kiểm tra camera rồi thử lại.");
            Cv2.ImEncode(".png",frame,out var bytes);return bytes;
        }
    },cancellation);
    public async Task<byte[]> CaptureStableAsync(CancellationToken cancellation=default)
    {
        byte[]? latest=null;
        for(var i=0;i<5;i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var frame=await ReadFrameAsync(cancellation);
            if(latest!=null)Array.Clear(latest);
            latest=frame;
            if(i<4)await Task.Delay(100,cancellation);
        }
        return latest!;
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;capture.Dispose();}}
}
