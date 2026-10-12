using Microsoft.EntityFrameworkCore;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    public Task SeedDemoAsync() => WriteAsync(Permission.Settings,async db=>
    {
        Rules.Require(demoMode,"Dữ liệu minh họa chỉ được nạp khi khởi động bằng --demo, trong cơ sở dữ liệu riêng.");
        Rules.Require(!await db.Assets.AnyAsync(),"Demo đã có dữ liệu. Dùng thư mục demo mới để tạo lại, không ghi đè dữ liệu hiện có.");
        var categoryNames=new[]{"Robot","Arduino","Cảm biến","Máy in 3D","Máy tính nhúng","Dụng cụ đo","Cơ khí","Điện tử","Sách","Vật tư","Quang học","Năng lượng"};
        var categories=new List<Category>();foreach(var name in categoryNames){var c=new Category {Name=name};db.Categories.Add(c);categories.Add(c);}
        var locations=await db.Locations.ToListAsync();for(int i=2;i<=4;i++){var l=new Location {Name=$"Phòng STEM / Kệ {i}"};db.Locations.Add(l);locations.Add(l);}await db.SaveChangesAsync();
        var borrowers=new List<Borrower>();for(int i=1;i<=30;i++){var b=new Borrower {Code=$"DEMO-{i:000}",DisplayName=$"Người mượn minh họa {i:00}",ClassName=$"{6+i%4}A{1+i%3}"};db.Borrowers.Add(b);borrowers.Add(b);}await db.SaveChangesAsync();
        var assets=new List<Asset>();var units=new List<AssetUnit>();
        var names=new[]{"Robot giáo dục","Arduino Uno","Cảm biến siêu âm","Bộ in 3D","Raspberry Pi","Đồng hồ đo","Bộ cơ khí","Bảng mạch","Sách STEM","Dây nối","Thấu kính","Pin mặt trời"};
        for(int i=0;i<60;i++)
        {
            var a=new Asset {Code=$"EQ-{i+1:000}",Name=$"{names[i%12]} {i/12+1}",Kind=AssetKind.Unit,CategoryId=categories[i%12].Id,LocationId=locations[i%4].Id,MinimumStock=1};db.Assets.Add(a);assets.Add(a);
        }
        for(int i=0;i<20;i++){var a=new Asset {Code=$"MAT-{i+1:000}",Name=$"Vật tư minh họa {i+1:00}",Kind=i%2==0?AssetKind.Consumable:AssetKind.Bulk,CategoryId=categories[9].Id,LocationId=locations[i%4].Id,UnitOfMeasure="bộ",MinimumStock=10};db.Assets.Add(a);assets.Add(a);}
        await db.SaveChangesAsync();
        for(int i=0;i<150;i++){var a=assets[i%60];var u=new AssetUnit {AssetId=a.Id,LocationId=a.LocationId,Serial=$"DEMO-SN-{i+1:000}",QrValue=$"STEM:UNIT:DEMO-{i+1:000}",Status=UnitStatus.Available};db.Units.Add(u);units.Add(u);}
        foreach(var a in assets.Where(x=>x.Kind!=AssetKind.Unit))db.Ledger.Add(new InventoryEntry {AssetId=a.Id,QuantityDelta=100,Reason="Tồn minh họa",ReferenceType="Demo",ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow.AddMonths(-6)});
        await db.SaveChangesAsync();var now=DateTime.UtcNow;
        for(int i=0;i<200;i++)
        {
            var u=units[i%150];var issued=now.AddDays(-180+i%170);var due=issued.AddDays(3);
            var loan=new Loan {Code=$"DEMO-LOAN-{i+1:000}",BorrowerId=borrowers[i%30].Id,IssuedById=CurrentActor!.Id,IssuedUtc=issued,DueUtc=due,Status=LoanStatus.Closed,Purpose="Hoạt động STEM minh họa",Lines=[new LoanLine {AssetId=u.AssetId,UnitId=u.Id,QuantityIssued=1,QuantityReturned=1}]};db.Loans.Add(loan);await db.SaveChangesAsync();
            db.Returns.Add(new ReturnTransaction {LoanId=loan.Id,ReceivedById=CurrentActor.Id,CreatedUtc=due.AddDays(i%5==0?2:-1),Note="Giao dịch minh họa",Lines=[new ReturnLine {LoanLineId=loan.Lines[0].Id,Quantity=1,Condition=ReturnCondition.Good}]});
        }
        for(int i=0;i<8;i++)
        {
            var u=units[i];u.Status=UnitStatus.CheckedOut;db.Loans.Add(new Loan {Code=$"DEMO-OPEN-{i+1}",BorrowerId=borrowers[i].Id,IssuedById=CurrentActor!.Id,IssuedUtc=now.AddDays(-5),DueUtc=now.AddDays(i-3),Purpose="Phiếu đang mượn minh họa",Lines=[new LoanLine {AssetId=u.AssetId,UnitId=u.Id,QuantityIssued=1}]});
        }
        var bulk=assets.First(x=>x.Kind==AssetKind.Bulk);
        var partial=new Loan {Code="DEMO-PARTIAL",BorrowerId=borrowers[10].Id,IssuedById=CurrentActor!.Id,IssuedUtc=now.AddDays(-2),DueUtc=now.AddDays(2),Purpose="Trả một phần minh họa",Lines=[new LoanLine {AssetId=bulk.Id,QuantityIssued=10,QuantityReturned=4}]};db.Loans.Add(partial);await db.SaveChangesAsync();
        db.Returns.Add(new ReturnTransaction {LoanId=partial.Id,ReceivedById=CurrentActor!.Id,CreatedUtc=now.AddDays(-1),Lines=[new ReturnLine {LoanLineId=partial.Lines[0].Id,Quantity=4,Condition=ReturnCondition.Good}]});
        for(int i=8;i<12;i++){var u=units[i];u.Status=UnitStatus.Maintenance;db.Maintenance.Add(new MaintenanceCase {UnitId=u.Id,OpenedById=CurrentActor!.Id,OpenedUtc=now.AddDays(-2),Reason="Hỏng minh họa"});}
        for(int i=0;i<20;i++)db.Reservations.Add(new Reservation {Code=$"DEMO-RES-{i+1:00}",BorrowerId=borrowers[i].Id,RequestedById=CurrentActor!.Id,ApprovedById=i%4==0?CurrentActor.Id:null,StartUtc=now.AddDays(i+1),EndUtc=now.AddDays(i+1).AddHours(2),Status=(i%4) switch {0=>ReservationStatus.Approved,1=>ReservationStatus.Pending,2=>ReservationStatus.Rejected,_=>ReservationStatus.Cancelled},Note="Lịch minh họa",Lines=[new ReservationLine {AssetId=units[i+20].AssetId,UnitId=units[i+20].Id,Quantity=1}]});
        Audit(db,"SeedDemo","System",null,"60 assets, 150 units, 20 materials, 30 synthetic borrowers, 209 loans, 20 reservations");
    });
}
