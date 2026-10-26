using Microsoft.EntityFrameworkCore;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    private static async Task EnsureCapacityAsync(LabDbContext db, IReadOnlyList<IssueLine> lines, DateTime start, DateTime end, long? excludeReservation = null, long? excludeLoan = null, int capacityDelta = 0, bool checkRequested = true)
    {
        Rules.Interval(start,end); Rules.Require(lines.Count>0,"Chọn ít nhất một thiết bị.");
        Rules.Require(lines.All(x=>x.Quantity>0 && x.Quantity<=10000),"Số lượng phải từ 1 đến 10.000.");
        Rules.Require(lines.Where(x=>x.UnitId.HasValue).GroupBy(x=>x.UnitId).All(g=>g.Count()==1),"Không chọn trùng một cá thể.");
        var reservations = await db.Reservations.Include(x=>x.Lines).Where(x=>x.Status==ReservationStatus.Approved && x.Id!=excludeReservation && x.EndUtc>DateTime.UtcNow).ToListAsync();
        var loans = await db.Loans.Include(x=>x.Lines).Where(x=>x.Status==LoanStatus.Open && x.Id!=excludeLoan && x.IssuedUtc<end).ToListAsync();
        foreach(var group in lines.GroupBy(x=>x.AssetId))
        {
            var asset=await db.Assets.SingleOrDefaultAsync(x=>x.Id==group.Key);
            Rules.Require(asset is not null && !asset.IsArchived,"Thiết bị không tồn tại hoặc đã ngừng sử dụng.");
            var units=await db.Units.Where(x=>x.AssetId==group.Key).ToListAsync();
            foreach(var l in group)
            {
                if(l.UnitId.HasValue) Rules.Require(asset!.Kind==AssetKind.Unit && l.Quantity==1 && units.Any(u=>u.Id==l.UnitId && u.Status is UnitStatus.Available or UnitStatus.CheckedOut),"Cá thể không thuộc thiết bị hoặc không khả dụng.");
                else Rules.Require(asset!.Kind!=AssetKind.Unit || l.Quantity>=1,"Thiếu cá thể.");
            }
            var capacity=asset!.Kind==AssetKind.Unit?units.Count(u=>u.Status is UnitStatus.Available or UnitStatus.CheckedOut):await BalanceAsync(db,group.Key)+capacityDelta;
            if(asset.Kind==AssetKind.Consumable)
            {
                var allocated=reservations.SelectMany(r=>r.Lines).Where(l=>l.AssetId==asset.Id).Sum(l=>l.Quantity);
                Rules.Require(allocated+(checkRequested?group.Sum(l=>l.Quantity):0)<=capacity,$"{asset.Name}: tồn tiêu hao đã được phân bổ cho lịch đặt, không đủ để xuất thêm.");
                continue;
            }
            var demand=new List<DemandWindow>();
            foreach(var r in reservations)
            {
                if(!Rules.Overlaps(r.StartUtc,r.EndUtc,start,end))continue;
                var qty=r.Lines.Where(x=>x.AssetId==group.Key).Sum(x=>x.Quantity);
                if(qty>0)demand.Add(new(r.StartUtc,r.EndUtc,qty));
                foreach(var l in group.Where(x=>x.UnitId.HasValue)) Rules.Require(!r.Lines.Any(x=>x.UnitId==l.UnitId),"Cá thể đã có lịch đặt trùng thời gian.");
            }
            foreach(var loan in loans)
            {
                var effectiveEnd=loan.DueUtc<DateTime.UtcNow?DateTime.MaxValue:loan.DueUtc;
                if(effectiveEnd<=start)continue;
                var qty=loan.Lines.Where(x=>x.AssetId==group.Key && !x.IsConsumable).Sum(x=>x.QuantityIssued-x.QuantityReturned);
                if(qty>0)demand.Add(new(loan.IssuedUtc,effectiveEnd,qty));
                foreach(var l in group.Where(x=>x.UnitId.HasValue)) Rules.Require(!loan.Lines.Any(x=>x.UnitId==l.UnitId && x.QuantityReturned<x.QuantityIssued),"Cá thể đang mượn trong khoảng thời gian này.");
            }
            int peak=CapacityPlanner.Peak(start,end,demand);
            Rules.Require(peak+(checkRequested?group.Sum(x=>x.Quantity):0)<=capacity,$"{asset.Name}: không đủ số lượng khả dụng trong khoảng thời gian chọn.");
        }
    }
    public async Task<IReadOnlyList<LoanRow>> LoansAsync(bool history=false)
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.Read);
        var loans=await db.Loans.AsNoTracking().Include(x=>x.Lines).Where(x=>history || x.Status==LoanStatus.Open).OrderByDescending(x=>x.IssuedUtc).ToListAsync();
        var borrowers=await db.Borrowers.ToDictionaryAsync(x=>x.Id,x=>x.DisplayName); var users=await db.Users.ToDictionaryAsync(x=>x.Id,x=>x.DisplayName); var assets=await db.Assets.ToDictionaryAsync(x=>x.Id,x=>x.Name);
        return loans.Select(x=>new LoanRow(x.Id,x.Code,CurrentActor!.Role==Role.Viewer?"Ẩn danh":borrowers[x.BorrowerId],CurrentActor.Role==Role.Viewer?"Ẩn danh":users[x.IssuedById],x.IssuedUtc,x.DueUtc,x.Status,CurrentActor.Role==Role.Viewer?"Ẩn":x.Purpose,x.Lines.Select(l=>new LoanLineRow(l.Id,l.AssetId,l.UnitId,assets[l.AssetId],l.QuantityIssued,l.QuantityReturned,l.IsConsumable)).ToList(),CurrentActor.Role==Role.Viewer?null:x.BorrowerId)).ToList();
    }
    private async Task TeacherPolicyAsync(LabDbContext db)
    {
        if(CurrentActor!.Role==Role.Teacher) Rules.Require((await db.Settings.SingleOrDefaultAsync(x=>x.Key=="teacher.issue"))?.Value=="true","Giáo viên chưa được cấu hình quyền bàn giao.");
    }
    public Task<long> IssueAsync(IssueRequest request) => WriteAsync(Permission.IssueLoan,async db=>
    {
        await TeacherPolicyAsync(db); var now=DateTime.UtcNow; Rules.Interval(now,request.DueUtc); Rules.Text(request.Purpose,"Mục đích",1000);
        Rules.Require(await db.Borrowers.AnyAsync(x=>x.Id==request.BorrowerId && x.IsActive),"Người mượn không hoạt động.");
        Reservation? reservation=null;
        if(request.ReservationId.HasValue)
        {
            reservation=await db.Reservations.Include(x=>x.Lines).SingleAsync(x=>x.Id==request.ReservationId);
            Rules.Require(reservation.Status==ReservationStatus.Approved && reservation.BorrowerId==request.BorrowerId && reservation.StartUtc<=now && now<reservation.EndUtc,"Lịch đặt không hợp lệ để bàn giao hiện tại.");
            Rules.Require(request.DueUtc<=reservation.EndUtc,"Hạn trả vượt thời gian đặt.");
            var requested=request.Lines.GroupBy(x=>x.AssetId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Quantity));
            Rules.Require(reservation.Lines.GroupBy(x=>x.AssetId).All(g=>requested.GetValueOrDefault(g.Key)==g.Sum(x=>x.Quantity)) && requested.Count==reservation.Lines.Select(x=>x.AssetId).Distinct().Count(),"Bàn giao phải khớp toàn bộ các dòng đặt trước.");
            Rules.Require(reservation.Lines.Where(l=>l.UnitId.HasValue).All(l=>request.Lines.Any(q=>q.UnitId==l.UnitId)),"Phải bàn giao đúng cá thể đã đặt.");
        }
        await EnsureCapacityAsync(db,request.Lines,now,request.DueUtc,request.ReservationId);
        var loan=new Loan { Code=Code("LOAN"),BorrowerId=request.BorrowerId,IssuedById=CurrentActor!.Id,IssuedUtc=now,DueUtc=request.DueUtc,Purpose=request.Purpose,ReservationId=request.ReservationId };
        var claimed=new HashSet<long>();
        foreach(var line in request.Lines)
        {
            var asset=await db.Assets.SingleAsync(x=>x.Id==line.AssetId);
            if(asset.Kind==AssetKind.Unit)
            {
                var candidates=await db.Units.Where(x=>x.AssetId==asset.Id && x.Status==UnitStatus.Available && (!line.UnitId.HasValue || x.Id==line.UnitId)).OrderBy(x=>x.Id).ToListAsync();
                var blocked=await db.Reservations.Where(x=>x.Id!=request.ReservationId && x.Status==ReservationStatus.Approved && x.StartUtc<request.DueUtc && x.EndUtc>now).SelectMany(x=>x.Lines).Where(x=>x.UnitId!=null).Select(x=>x.UnitId!.Value).ToListAsync();
                var selected=candidates.Where(x=>!claimed.Contains(x.Id) && !blocked.Contains(x.Id)).Take(line.Quantity).ToList();
                Rules.Require(selected.Count==line.Quantity,"Không đủ cá thể sẵn sàng để bàn giao.");
                foreach(var u in selected) { claimed.Add(u.Id); u.Status=UnitStatus.CheckedOut; loan.Lines.Add(new LoanLine { AssetId=asset.Id,UnitId=u.Id,QuantityIssued=1 }); }
            }
            else loan.Lines.Add(new LoanLine { AssetId=asset.Id,QuantityIssued=line.Quantity,QuantityReturned=asset.Kind==AssetKind.Consumable?line.Quantity:0,IsConsumable=asset.Kind==AssetKind.Consumable });
        }
        if(loan.Lines.All(x=>x.IsConsumable))loan.Status=LoanStatus.Closed;
        db.Loans.Add(loan); await db.SaveChangesAsync();
        foreach(var l in loan.Lines.Where(x=>x.IsConsumable)) db.Ledger.Add(new InventoryEntry { AssetId=l.AssetId,QuantityDelta=-l.QuantityIssued,Reason="Xuất tiêu hao",ReferenceType="Loan",ReferenceId=loan.Id,ActorId=CurrentActor!.Id,CreatedUtc=now });
        if(reservation!=null)reservation.Status=ReservationStatus.Fulfilled;
        Audit(db,"IssueLoan","Loan",loan.Id,$"Lines={loan.Lines.Count}"); return loan.Id;
    });
    public Task ReturnAsync(long loanId,IReadOnlyList<ReturnInput> lines,string note) => WriteAsync(Permission.ReturnLoan,async db=>
    {
        await TeacherPolicyAsync(db); Rules.Require(lines.Count>0 && lines.Select(x=>x.LoanLineId).Distinct().Count()==lines.Count,"Dòng trả trống hoặc bị trùng.");
        var loan=await db.Loans.Include(x=>x.Lines).SingleAsync(x=>x.Id==loanId); Rules.Require(loan.Status==LoanStatus.Open,"Phiếu đã đóng.");
        var transaction=new ReturnTransaction { LoanId=loanId,ReceivedById=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow,Note=note };
        foreach(var input in lines)
        {
            var l=loan.Lines.SingleOrDefault(x=>x.Id==input.LoanLineId); Rules.Require(l!=null && !l.IsConsumable,"Dòng không thuộc phiếu hoặc là vật tư tiêu hao.");
            TransactionPolicies.ValidateReturn(l!,input.Quantity,input.Condition);
            l!.QuantityReturned+=input.Quantity; transaction.Lines.Add(new ReturnLine { LoanLineId=l.Id,Quantity=input.Quantity,Condition=input.Condition });
            if(l.UnitId.HasValue)
            {
                var u=await db.Units.SingleAsync(x=>x.Id==l.UnitId); u.Status=input.Condition switch { ReturnCondition.Good=>UnitStatus.Available,ReturnCondition.Damaged=>UnitStatus.Maintenance,_=>UnitStatus.Lost }; u.Condition=input.Condition.ToString();
                if(input.Condition==ReturnCondition.Damaged) db.Maintenance.Add(new MaintenanceCase { UnitId=u.Id,OpenedById=CurrentActor!.Id,OpenedUtc=DateTime.UtcNow,Reason=$"Trả hỏng từ phiếu {loan.Code}: {note}" });
            }
            else if(input.Condition!=ReturnCondition.Good) db.Ledger.Add(new InventoryEntry { AssetId=l.AssetId,QuantityDelta=-input.Quantity,Reason=$"Trả {input.Condition}: {note}",ReferenceType="Return",ReferenceId=loanId,ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow });
        }
        if(loan.Lines.All(l=>l.QuantityReturned==l.QuantityIssued))loan.Status=LoanStatus.Closed;
        db.Returns.Add(transaction); Audit(db,"ReturnLoan","Loan",loanId,$"Lines={lines.Count}; Condition={string.Join(',',lines.Select(x=>x.Condition))}");
    });
    public Task ExtendAsync(long loanId,DateTime dueUtc,string reason) => WriteAsync(Permission.ApproveReservation,async db=>
    {
        Rules.Text(reason,"Lý do",1000); var loan=await db.Loans.Include(x=>x.Lines).SingleAsync(x=>x.Id==loanId); Rules.Require(loan.Status==LoanStatus.Open && dueUtc>loan.DueUtc,"Phiếu đã đóng hoặc hạn mới không dài hơn hạn cũ.");
        var start=loan.DueUtc>DateTime.UtcNow?loan.DueUtc:DateTime.UtcNow;
        await EnsureCapacityAsync(db,loan.Lines.Where(l=>!l.IsConsumable && l.QuantityReturned<l.QuantityIssued).Select(l=>new IssueLine(l.AssetId,l.UnitId,l.QuantityIssued-l.QuantityReturned)).ToList(),start,dueUtc,excludeLoan:loan.Id);
        var old=loan.DueUtc; loan.DueUtc=dueUtc; Audit(db,"ExtendLoan","Loan",loanId,$"From={old:O}; To={dueUtc:O}; Reason={reason}");
    });
    public async Task<IReadOnlyList<ReservationRow>> ReservationsAsync()
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.Read);
        var list=await db.Reservations.AsNoTracking().Include(x=>x.Lines).OrderByDescending(x=>x.StartUtc).ToListAsync(); var borrowers=await db.Borrowers.ToDictionaryAsync(x=>x.Id,x=>x.DisplayName);
        return list.Select(x=>new ReservationRow(x.Id,x.Code,CurrentActor!.Role==Role.Viewer?"Ẩn danh":borrowers[x.BorrowerId],x.StartUtc,x.EndUtc,x.Status,CurrentActor.Role==Role.Viewer?"Ẩn":x.Note,x.Lines,x.BorrowerId)).ToList();
    }
    public Task<long> ReserveAsync(ReservationInput request) => WriteAsync(Permission.RequestReservation,async db=>
    {
        Rules.Interval(request.StartUtc,request.EndUtc); Rules.Require(request.StartUtc>=DateTime.UtcNow,"Không đặt trong quá khứ."); Rules.Require(await db.Borrowers.AnyAsync(x=>x.Id==request.BorrowerId && x.IsActive),"Người mượn không hoạt động.");
        await EnsureCapacityAsync(db,request.Lines,request.StartUtc,request.EndUtc);
        var r=new Reservation { Code=Code("RES"),BorrowerId=request.BorrowerId,RequestedById=CurrentActor!.Id,StartUtc=request.StartUtc,EndUtc=request.EndUtc,Status=ReservationStatus.Pending,Note=request.Note,Lines=request.Lines.Select(l=>new ReservationLine { AssetId=l.AssetId,UnitId=l.UnitId,Quantity=l.Quantity }).ToList() };
        db.Reservations.Add(r); await db.SaveChangesAsync(); Audit(db,"RequestReservation","Reservation",r.Id); return r.Id;
    });
    public Task SetReservationStatusAsync(long id,ReservationStatus status,string reason="") => WriteAsync(status==ReservationStatus.Cancelled?Permission.RequestReservation:Permission.ApproveReservation,async db=>
    {
        var r=await db.Reservations.Include(x=>x.Lines).SingleAsync(x=>x.Id==id);
        if(CurrentActor!.Role==Role.Teacher)Rules.Require(r.RequestedById==CurrentActor.Id,"Chỉ được hủy yêu cầu của mình.");
        var valid=TransactionPolicies.CanTransition(r.Status,status,r.EndUtc,DateTime.UtcNow);
        Rules.Require(valid,"Chuyển trạng thái lịch đặt không hợp lệ.");
        if(status==ReservationStatus.Approved) { Rules.Require(r.EndUtc>DateTime.UtcNow,"Lịch đặt đã hết hạn."); await EnsureCapacityAsync(db,r.Lines.Select(x=>new IssueLine(x.AssetId,x.UnitId,x.Quantity)).ToList(),r.StartUtc,r.EndUtc,r.Id); r.ApprovedById=CurrentActor.Id; }
        if(status is ReservationStatus.Rejected or ReservationStatus.Cancelled)Rules.Text(reason,"Lý do",1000);
        r.Status=status; if(!string.IsNullOrWhiteSpace(reason))r.Note=reason; Audit(db,"ReservationStatus","Reservation",id,$"Status={status}; Reason={reason}");
    });
    public async Task<IReadOnlyList<MaintenanceCase>> MaintenanceAsync() { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Maintenance.AsNoTracking().OrderByDescending(x=>x.OpenedUtc).ToListAsync(); }
    public Task OpenMaintenanceAsync(long unitId,string reason) => WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(reason,"Lý do",1000); var u=await db.Units.SingleAsync(x=>x.Id==unitId); Rules.Require(u.Status==UnitStatus.Available,"Cá thể cần sẵn sàng trước khi chuyển bảo trì.");
        u.Status=UnitStatus.Maintenance; db.Maintenance.Add(new MaintenanceCase { UnitId=unitId,OpenedById=CurrentActor!.Id,OpenedUtc=DateTime.UtcNow,Reason=reason }); Audit(db,"OpenMaintenance","AssetUnit",unitId,reason);
    });
    public Task CloseMaintenanceAsync(long id,string resolution,decimal cost) => WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(resolution,"Kết quả",1000); Rules.Require(cost>=0,"Chi phí không hợp lệ."); var m=await db.Maintenance.SingleAsync(x=>x.Id==id); Rules.Require(m.ClosedUtc==null,"Biên bản đã đóng.");
        m.Resolution=resolution; m.Cost=cost; m.ClosedUtc=DateTime.UtcNow; var u=await db.Units.SingleAsync(x=>x.Id==m.UnitId); u.Status=UnitStatus.Available; u.Condition="Tốt"; Audit(db,"CloseMaintenance","MaintenanceCase",id,resolution);
    });
    public Task<long> CreateInventoryAsync(long locationId,IReadOnlyDictionary<long,int> counts,string note) => WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Require(counts.Count>0 && counts.Values.All(x=>x>=0),"Số kiểm kê không hợp lệ."); var session=new InventorySession { LocationId=locationId,ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow,Note=note };
        foreach(var pair in counts)
        {
            var a=await db.Assets.SingleAsync(x=>x.Id==pair.Key); Rules.Require(a.LocationId==locationId && !a.IsArchived,"Thiết bị không thuộc vị trí kiểm kê.");
            Rules.Require(a.Kind!=AssetKind.Unit,"Thiết bị cá thể phải kiểm kê theo QR để định danh từng cá thể thiếu/tìm lại.");
            var expected=a.Kind==AssetKind.Unit?await db.Units.CountAsync(x=>x.AssetId==a.Id && x.LocationId==locationId && x.Status==UnitStatus.Available):await BalanceAsync(db,a.Id)-await OpenQuantityAsync(db,a.Id);
            session.Counts.Add(new InventoryCount { AssetId=a.Id,Expected=expected,Actual=pair.Value });
        }
        db.Inventory.Add(session); await db.SaveChangesAsync(); Audit(db,"CreateInventory","InventorySession",session.Id); return session.Id;
    });
    public Task<long> CreateUnitInventoryAsync(long locationId,IReadOnlySet<string> scannedQrValues,string note)=>WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(note,"Lý do kiểm kê",1000);Rules.Require(await db.Locations.AnyAsync(x=>x.Id==locationId),"Vị trí không tồn tại.");
        var units=await db.Units.Where(x=>x.LocationId==locationId && (x.Status==UnitStatus.Available || x.Status==UnitStatus.Lost)).ToListAsync();Rules.Require(units.Count>0,"Không có cá thể cần kiểm kê.");
        Rules.Require(scannedQrValues.All(q=>units.Any(u=>u.QrValue==q)),"Có QR không thuộc vị trí hoặc cá thể đang mượn/bảo trì/ngừng sử dụng.");
        var session=new InventorySession {LocationId=locationId,ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow,Note=note,Units=units.Select(u=>new InventoryUnitCount {UnitId=u.Id,ExpectedStatus=u.Status,Present=scannedQrValues.Contains(u.QrValue)}).ToList()};
        db.Inventory.Add(session);await db.SaveChangesAsync();Audit(db,"CreateQrInventory","InventorySession",session.Id,$"Scanned={scannedQrValues.Count}; Expected={units.Count}");return session.Id;
    });
    public async Task<IReadOnlyList<InventorySession>> InventoryAsync() { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Inventory.AsNoTracking().Include(x=>x.Counts).Include(x=>x.Units).OrderByDescending(x=>x.CreatedUtc).ToListAsync(); }
    public Task ConfirmInventoryAsync(long id) => WriteAsync(Permission.ManageAssets,async db=>
    {
        var session=await db.Inventory.Include(x=>x.Counts).Include(x=>x.Units).SingleAsync(x=>x.Id==id); Rules.Require(session.ConfirmedUtc==null,"Phiên kiểm kê đã xác nhận."); Rules.Text(session.Note,"Lý do kiểm kê",1000);
        foreach(var count in session.Units)
        {
            var unit=await db.Units.SingleAsync(x=>x.Id==count.UnitId);Rules.Require(unit.LocationId==session.LocationId && unit.Status==count.ExpectedStatus,"Cá thể thay đổi trạng thái/vị trí sau kiểm kê; hãy kiểm kê lại.");
            var status=count.Present?UnitStatus.Available:UnitStatus.Lost;
            if(status!=unit.Status){unit.Status=status;Audit(db,count.Present?"InventoryFoundUnit":"InventoryLostUnit","AssetUnit",unit.Id,$"Inventory={id}");}
        }
        foreach(var c in session.Counts)
        {
            var a=await db.Assets.SingleAsync(x=>x.Id==c.AssetId);
            var current=a.Kind==AssetKind.Unit?await db.Units.CountAsync(x=>x.AssetId==a.Id && x.LocationId==session.LocationId && x.Status==UnitStatus.Available):await BalanceAsync(db,a.Id)-await OpenQuantityAsync(db,a.Id);
            Rules.Require(current==c.Expected,"Tồn kho đã thay đổi từ khi kiểm kê. Hãy tạo phiên mới.");
            int delta=c.Actual-c.Expected;
            if(delta==0)continue;
            if(a.Kind==AssetKind.Unit)
            {
                Rules.Require(false,"Chênh lệch cá thể phải xử lý theo QR: nhập bổ sung hoặc đánh dấu mất/ngừng sử dụng, rồi kiểm kê lại.");
            }
            else
            {
                await EnsureCapacityAsync(db,[new(a.Id,null,1)],DateTime.UtcNow,DateTime.UtcNow.AddYears(20),capacityDelta:delta,checkRequested:false);
                db.Ledger.Add(new InventoryEntry { AssetId=a.Id,QuantityDelta=delta,Reason=session.Note,ReferenceType="Inventory",ReferenceId=id,ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow });
            }
        }
        session.ConfirmedUtc=DateTime.UtcNow; Audit(db,"ConfirmInventory","InventorySession",id);
    });
}
