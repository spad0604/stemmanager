using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public static class LiveChallenge
{
    public static void Validate(DetectedFace first,DetectedFace second,int direction)
    {
        Rules.Require(direction is -1 or 1,"Hướng thử thách không hợp lệ.");
        static double NosePosition(DetectedFace f)
        {
            var p=f.Landmarks;Rules.Require(p.Length==10,"Thiếu landmark.");
            var eyeDistance=Math.Sqrt(Math.Pow(p[2]-p[0],2)+Math.Pow(p[3]-p[1],2));Rules.Require(eyeDistance>=15,"Ảnh chưa đủ chất lượng.");return(p[4]-(p[0]+p[2])/2)/eyeDistance;
        }
        var movement=NosePosition(second)-NosePosition(first);
        Rules.Require(movement*direction>=0.08,"Chưa quan sát được chuyển động khuôn mặt theo thử thách. Chụp lại hoặc dùng QR/mã.");
        // This rejects static frames, but is not a certified presentation-attack detector.
    }
}
