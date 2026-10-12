using STEMLabManager.Domain;
namespace STEMLabManager.Application;

public sealed record DemandWindow(DateTime StartUtc,DateTime EndUtc,int Quantity);
public static class CapacityPlanner
{
    public static int Peak(DateTime start,DateTime end,IEnumerable<DemandWindow> demand)
    {
        Rules.Interval(start,end);var events=new List<(DateTime Time,int Delta)>();
        foreach(var d in demand.Where(d=>Rules.Overlaps(start,end,d.StartUtc,d.EndUtc)))
        {
            Rules.Require(d.Quantity>=0,"Nhu cầu không được âm.");
            events.Add((d.StartUtc<start?start:d.StartUtc,d.Quantity));events.Add((d.EndUtc>end?end:d.EndUtc,-d.Quantity));
        }
        int current=0,peak=0;foreach(var e in events.OrderBy(e=>e.Time).ThenBy(e=>e.Delta)){current=checked(current+e.Delta);peak=Math.Max(peak,current);}return peak;
    }
}
public static class TransactionPolicies
{
    public static void ValidateReturn(LoanLine line,int quantity,ReturnCondition condition)
    {
        Rules.Require(!line.IsConsumable,"Vật tư tiêu hao không được trả như thiết bị.");
        Rules.Require(quantity>0 && quantity<=line.QuantityIssued-line.QuantityReturned,"Số lượng trả vượt số đang giữ.");
        Rules.Require(Enum.IsDefined(condition),"Tình trạng không hợp lệ.");
    }
    public static bool CanTransition(ReservationStatus from,ReservationStatus to,DateTime endUtc,DateTime nowUtc)=>to switch
    {
        ReservationStatus.Approved or ReservationStatus.Rejected=>from==ReservationStatus.Pending,
        ReservationStatus.Cancelled=>from is ReservationStatus.Pending or ReservationStatus.Approved,
        ReservationStatus.Expired=>(from is ReservationStatus.Pending or ReservationStatus.Approved) && endUtc<=nowUtc,
        _=>false
    };
}
