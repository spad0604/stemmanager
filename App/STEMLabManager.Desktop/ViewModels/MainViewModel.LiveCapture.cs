using STEMLabManager.AI;
namespace STEMLabManager.Desktop.ViewModels;

public partial class MainViewModel
{
    private async Task<byte[]?> CaptureLiveAsync()
    {
        using var cameraSession=await camera.OpenAsync(0);
        var preview=await cameraSession.ReadFrameAsync();
        try
        {
            if(!await dialogs.PreviewLiveAsync("Chụp ảnh khuôn mặt","Nhìn thẳng vào camera, căn khuôn mặt rõ trong khung hình rồi nhấn Chụp ảnh. Hệ thống dùng một ảnh để đăng ký; ảnh chỉ giữ tạm trong RAM.",preview,cameraSession.ReadFrameAsync))return null;
            return await cameraSession.CaptureStableAsync();
        }
        finally{Array.Clear(preview);}
    }
}
