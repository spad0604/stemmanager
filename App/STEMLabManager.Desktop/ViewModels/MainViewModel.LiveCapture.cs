using System.Security.Cryptography;
using STEMLabManager.AI;
using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;

public partial class MainViewModel
{
    private async Task<byte[]?> CaptureLiveAsync(string detector,string embeddingModel)
    {
        if(!await dialogs.ConfirmAsync("Quan sát người thật","Đối chiếu thẻ/QR, đảm bảo người trước camera là người thật và nhìn thẳng. Bước sau sẽ yêu cầu quay mặt theo hướng ngẫu nhiên. Đây là kiểm tra chuyển động cơ bản, cần giám sát của nhân viên."))return null;
        var first=await camera.CaptureAsync(0);
        try
        {
            var initial=await Task.Run(()=>face.Detect(first,detector));Rules.Require(initial.Count==1,"Cần đúng một khuôn mặt.");
            int direction=RandomNumberGenerator.GetInt32(2)==0?-1:1;
            if(!await dialogs.PreviewAsync("Thử thách camera",$"Ảnh thứ nhất (chỉ giữ trong RAM). Quay mặt nhẹ về phía {(direction<0?"trái":"phải")} trong ảnh camera. Nhân viên quan sát chuyển động thật rồi nhấn Xác nhận để chụp lần hai.",first)){Array.Clear(first);return null;}
            var second=await camera.CaptureAsync(0);
            try
            {
                var next=await Task.Run(()=>face.Detect(second,detector));Rules.Require(next.Count==1,"Lần hai cần đúng một khuôn mặt.");LiveChallenge.Validate(initial[0],next[0],direction);
                var firstEmbedding=await Task.Run(()=>face.Embed(first,initial[0],embeddingModel));var secondEmbedding=await Task.Run(()=>face.Embed(second,next[0],embeddingModel));
                try{Rules.Require(FaceEngine.Match(firstEmbedding,[(1,secondEmbedding)],0.5).Count==1,"Hai ảnh không đủ giống nhau. Hãy thử lại hoặc dùng QR/mã.");}
                finally{Array.Clear(firstEmbedding);Array.Clear(secondEmbedding);}
            }
            finally{Array.Clear(second);}
            return first;
        }
        catch{Array.Clear(first);throw;}
    }
}
