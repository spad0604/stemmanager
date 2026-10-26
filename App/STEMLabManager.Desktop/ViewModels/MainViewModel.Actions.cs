using System.Globalization;
using Avalonia;
using Avalonia.Styling;
using STEMLabManager.Application;
using STEMLabManager.Domain;
using STEMLabManager.AI;
namespace STEMLabManager.Desktop.ViewModels;

public partial class MainViewModel
{
    private static IEnumerable<Choice> EnumChoices<T>(Func<T,string>? label=null) where T:struct,Enum=>Enum.GetValues<T>().Select(x=>new Choice(Convert.ToInt64(x),label?.Invoke(x)??x.ToString()));
    private static FormField Field(string key,string label,string value="",bool required=true,string hint="")=>new(key,label,value,required,hint);
    private static FormField Choose(string key,string label,IEnumerable<Choice> choices,long? selected=null)=>new(key,label,selected?.ToString()??"",choices:choices);
    private async Task AssetFormAsync(long? id)
    {
        var categories=await service.CategoriesAsync();var locations=await service.LocationsAsync();var a=id.HasValue?await service.AssetDetailsAsync(id.Value):null;
        var fields=new List<FormField>{Field("code","Mã duy nhất",a?.Code??""),Field("name","Tên thiết bị",a?.Name??""),Choose("kind","Loại quản lý",EnumChoices<AssetKind>(Labels.Kind),a is null?null:(long)a.Kind),Choose("category","Danh mục",categories.Select(x=>new Choice(x.Id,x.Name)),a?.CategoryId),Choose("location","Vị trí",locations.Select(x=>new Choice(x.Id,x.Name)),a?.LocationId),Field("unit","Đơn vị tính",a?.UnitOfMeasure??"Cái"),Field("minimum","Ngưỡng tồn thấp",a?.MinimumStock.ToString()??"0"),Field("description","Mô tả",a?.Description??"",false),Field("manufacturer","Nhà sản xuất",a?.Manufacturer??"",false),Field("model","Model",a?.Model??"",false),new FormField("image","Ảnh sản phẩm","",false,"Tùy chọn. Để trống để giữ ảnh hiện có.",browse:()=>dialogs.OpenAsync("Chọn ảnh sản phẩm","*.png","*.jpg","*.jpeg"))};
        if(id is null)fields.Add(Field("quantity","Tồn ban đầu","1"));
        fields.Add(new FormField("purchase","Ngày giờ mua (tùy chọn)",a?.PurchasedUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")??"",false,dateTime:true));fields.Add(Field("price","Giá tham khảo (₫)",a?.ReferencePrice?.ToString(CultureInfo.InvariantCulture)??"",false,"Số không âm; dùng dấu chấm cho phần thập phân, không phân cách hàng nghìn."));
        long? savedId=id;
        await dialogs.FormAsync(id.HasValue?"Sửa thiết bị":"Thêm thiết bị","Thiết bị cá thể có QR riêng. Ảnh sản phẩm tùy chọn; vật tư tiêu hao xuất khỏi tồn, không trả lại.",fields,async f=>{decimal? price=null;if(f["price"]!=""){Rules.Require(decimal.TryParse(f["price"],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var parsed),"Giá không hợp lệ.");price=parsed;}savedId=await service.SaveAssetAsync(savedId,new(f["code"],f["name"],(AssetKind)f.Id("kind"),f.Id("category"),f.Id("location"),f["unit"],savedId.HasValue?0:f.Integer("quantity"),f.Integer("minimum"),f["description"],f["manufacturer"],f["model"],a?.ImagePath,f["purchase"]==""?null:f.Utc("purchase"),price));if(f["image"]!="")await service.ImportAssetImageAsync(savedId.Value,f["image"]);});
    }
    private async Task AssetImageAsync()
    {
        var selected=Selected();var source=await dialogs.OpenAsync("Chọn ảnh thiết bị","*.png","*.jpg","*.jpeg");if(source==null)return;
        await service.ImportAssetImageAsync(selected.Id,source);Message="Đã lưu ảnh thiết bị.";
    }
    private async Task BorrowerFormAsync(BorrowerRow? row)
    {
        await dialogs.FormAsync(row==null?"Thêm người mượn":"Sửa người mượn","Không phân biệt giáo viên hay học sinh; mọi người nhận thiết bị đều là người mượn.",[Field("code","Mã",row?.Code??""),Field("name","Họ tên",row?.Name??""),Field("class","Lớp / bộ môn",row?.ClassName??"",false),Field("contact","Liên hệ (tùy chọn)",row?.Contact??"",false)],async f=>{await service.SaveBorrowerAsync(row?.Id,new(f["code"],f["name"],f["class"],"Người mượn",f["contact"]));});
    }
    private async Task LookupAsync(bool category,DisplayRow? row)
    {await dialogs.FormAsync(category?"Danh mục":"Vị trí","Tên duy nhất, tối đa 200 ký tự.",[Field("name","Tên",row?.Name??"")],f=>service.SaveLookupAsync(category,row?.Id,f["name"]));}
    private async Task StockAsync()
    {
        var assets=await service.AssetsAsync();var selected=SelectedRow?.Source as AssetRow;
        await dialogs.FormAsync("Nhập / điều chỉnh tồn","Số dương để nhập, âm để điều chỉnh giảm. Thiết bị cá thể chỉ nhập thêm; giảm qua trạng thái từng cá thể.",[Choose("asset","Thiết bị",assets.Select(x=>new Choice(x.Id,$"{x.Code} • {x.Name}")),selected?.Id),Field("delta","Lượng thay đổi","1"),Field("reason","Lý do")],f=>service.AdjustStockAsync(f.Id("asset"),f.Integer("delta"),f["reason"]));
    }
    private async Task MoveAsync(){var id=Selected().Id;var locations=await service.LocationsAsync();await dialogs.FormAsync("Chuyển vị trí","Ghi nhật ký chuyển cá thể.",[Choose("location","Vị trí mới",locations.Select(x=>new Choice(x.Id,x.Name)))],f=>service.MoveUnitAsync(id,f.Id("location")));}
    private async Task RetireAsync(){var r=Selected();await dialogs.FormAsync("Ngừng sử dụng cá thể",r.Code+" sẽ không được tính vào số khả dụng.",[Field("reason","Lý do")],f=>service.RetireUnitAsync(r.Id,f["reason"]));}
    private async Task WizardAsync(bool reservation,IReadOnlyList<AssetCartItem>? initialCart=null)
    {
        var defaultBorrower=reservation?null:await MatchCurrentBorrowerAsync();
        var vm=new LoanWizardViewModel(service,reservation,reservation?null:LookupBorrowerByFaceAsync,initialCart,defaultBorrower?.Id);await vm.LoadAsync();
        if(await dialogs.WizardAsync(vm)){if(initialCart!=null)assetCart.Clear();Message=reservation?"Đã tạo yêu cầu đặt trước.":"Đã tạo phiếu mượn. Biên nhận và QR có trong danh sách phiếu.";}
    }
    private async Task<BorrowerRow?> MatchCurrentBorrowerAsync()
    {
        var actor=service.CurrentActor;if(actor==null)return null;
        var byCode=(await service.BorrowersAsync(actor.Username)).Where(x=>x.Active&&x.Code.Equals(actor.Username,StringComparison.OrdinalIgnoreCase)).ToList();
        if(byCode.Count==1)return byCode[0];
        var byName=(await service.BorrowersAsync(actor.DisplayName)).Where(x=>x.Active&&x.Name.Equals(actor.DisplayName,StringComparison.OrdinalIgnoreCase)).ToList();
        return byName.Count==1?byName[0]:null;
    }
    private async Task AddSelectedAssetToCartAsync()
    {
        var asset=(AssetRow)Selected().Source;Rules.Require(!asset.Archived&&asset.Available>0,"Thiết bị này hiện không sẵn sàng để mượn.");
        var quantity=1;
        if(asset.Kind!=AssetKind.Unit)
        {
            var confirmed=await dialogs.FormAsync("Thêm vào giỏ mượn",$"Còn {asset.Available} {asset.Unit} có thể bàn giao.",[Field("quantity","Số lượng","1")],f=>{quantity=f.Integer("quantity");Rules.Require(quantity>0&&quantity<=asset.Available,$"Số lượng từ 1 đến {asset.Available}.");return Task.CompletedTask;});
            if(!confirmed)return;
        }
        var index=assetCart.FindIndex(x=>x.Asset.Id==asset.Id);
        var total=quantity+(index>=0?assetCart[index].Quantity:0);Rules.Require(total<=asset.Available,$"Trong giỏ đã có thiết bị này; tổng số không vượt quá {asset.Available}.");
        if(index>=0)assetCart[index]=new(asset,total);else assetCart.Add(new(asset,quantity));
        Message=$"Đã thêm {asset.Name} vào giỏ ({assetCart.Sum(x=>x.Quantity)} món).";
    }
    private Task StartCartLoanAsync(){Rules.Require(assetCart.Count>0,"Giỏ mượn đang trống. Chọn thiết bị rồi thêm vào giỏ.");return WizardAsync(false,assetCart.ToArray());}
    private async Task ReturnFormAsync()
    {
        var loan=(LoanRow)Selected().Source;await ReturnFormForLoanAsync(loan);
    }
    private async Task ReturnByFaceAsync()
    {
        var borrower=await LookupBorrowerByFaceAsync();if(borrower==null)return;
        var loans=(await service.LoansAsync()).Where(x=>x.BorrowerId==borrower.Id&&x.Status==LoanStatus.Open&&x.Lines.Any(l=>!l.Consumable&&l.QuantityReturned<l.QuantityIssued)).ToList();
        Rules.Require(loans.Count>0,$"{borrower.Name} không có thiết bị cần trả.");
        LoanRow? selected=null;
        var options=loans.Select(x=>new Choice(x.Id,$"{x.Code} • hạn {x.DueUtc.ToLocalTime():dd/MM/yyyy HH:mm} • {x.Summary}"));
        if(!await dialogs.FormAsync("Chọn phiếu cần trả",$"Người mượn: {borrower.Name}. Đối chiếu phiếu và thiết bị trước khi ghi nhận.",[Choose("loan","Phiếu đang mở",options)],f=>{selected=loans.Single(x=>x.Id==f.Id("loan"));return Task.CompletedTask;}))return;
        if(selected!=null)await ReturnFormForLoanAsync(selected);
    }
    private async Task ReturnFormForLoanAsync(LoanRow loan)
    {
        Rules.Require(loan.Status==LoanStatus.Open,"Phiếu đã đóng.");var fields=new List<FormField>();
        foreach(var line in loan.Lines.Where(x=>!x.Consumable && x.QuantityReturned<x.QuantityIssued))
        {fields.Add(Field($"qty-{line.Id}",$"{line.Asset} • còn {line.QuantityIssued-line.QuantityReturned}","0",hint:"0 để bỏ qua; có thể trả một phần."));fields.Add(Choose($"condition-{line.Id}","Tình trạng sau trả",EnumChoices<ReturnCondition>(c=>c switch{ReturnCondition.Good=>"Tốt",ReturnCondition.Damaged=>"Hỏng",_=>"Mất"})));}
        fields.Add(Field("note","Ghi chú","",false));
        await dialogs.FormAsync("Trả thiết bị",loan.Code+" • "+loan.Borrower,fields,async f=>
        {
            var inputs=new List<ReturnInput>();foreach(var line in loan.Lines.Where(x=>!x.Consumable && x.QuantityReturned<x.QuantityIssued)){var qty=f.Integer($"qty-{line.Id}");Rules.Require(qty>=0,"Số trả không được âm.");if(qty>0)inputs.Add(new(line.Id,qty,(ReturnCondition)f.Id($"condition-{line.Id}")));}
            await service.ReturnAsync(loan.Id,inputs,f["note"]);
        });
    }
    private async Task ExtendFormAsync(){var loan=(LoanRow)Selected().Source;await dialogs.FormAsync("Gia hạn",loan.Code+" • Không gia hạn khi xung đột lịch đặt đã duyệt.",[new FormField("due","Hạn mới",loan.DueUtc.ToLocalTime().AddDays(1).ToString("dd/MM/yyyy HH:mm"),dateTime:true),Field("reason","Lý do")],f=>service.ExtendAsync(loan.Id,f.Utc("due"),f["reason"]));}
    private async Task ChangeReservationAsync(ReservationStatus status)
    {
        var r=(ReservationRow)Selected().Source;
        if(status==ReservationStatus.Approved){if(await dialogs.ConfirmAsync("Duyệt lịch đặt",$"{r.Code} • {r.DetailsText()}. Hệ thống kiểm tra lại số lượng."))await service.SetReservationStatusAsync(r.Id,status);}
        else if(status==ReservationStatus.Expired){if(await dialogs.ConfirmAsync("Đánh dấu hết hạn/no-show",r.Code))await service.SetReservationStatusAsync(r.Id,status);}
        else await dialogs.FormAsync(Labels.Reservation(status),r.Code+". Không thay đổi phiếu đã bàn giao.",[Field("reason","Lý do")],f=>service.SetReservationStatusAsync(r.Id,status,f["reason"]));
    }
    private async Task FulfillAsync()
    {
        var r=(ReservationRow)Selected().Source;if(!await dialogs.ConfirmAsync("Bàn giao lịch đặt",$"{r.Code} • {r.Borrower}. Xác nhận đã giao đủ thiết bị cho người nhận."))return;
        await service.IssueAsync(new(r.BorrowerId,r.EndUtc,"Bàn giao lịch đặt "+r.Code,r.Lines.Select(x=>new IssueLine(x.AssetId,x.UnitId,x.Quantity)).ToList(),r.Id));
    }
    private async Task MaintenanceFormAsync(long? unitId)
    {
        var choices=new List<Choice>();if(!unitId.HasValue)foreach(var a in (await service.AssetsAsync()).Where(x=>x.Kind==AssetKind.Unit))foreach(var u in (await service.UnitsAsync(a.Id)).Where(x=>x.Status==UnitStatus.Available))choices.Add(new(u.Id,$"{a.Name} • {u.QrValue}"));
        var fields=new List<FormField>();if(!unitId.HasValue)fields.Add(Choose("unit","Cá thể",choices));fields.Add(Field("reason","Mô tả lỗi"));
        await dialogs.FormAsync("Báo hỏng","Chuyển sang bảo trì; lịch bị ảnh hưởng xuất hiện trong thông báo.",fields,f=>service.OpenMaintenanceAsync(unitId??f.Id("unit"),f["reason"]));
    }
    private async Task CloseMaintenanceAsync(){var r=Selected();await dialogs.FormAsync("Hoàn tất sửa chữa",r.Name+" sẽ trở lại sẵn sàng.",[Field("resolution","Kết quả sửa chữa"),Field("cost","Chi phí (₫)","0")],f=>{Rules.Require(decimal.TryParse(f["cost"],out var cost),"Chi phí không hợp lệ.");return service.CloseMaintenanceAsync(r.Id,f["resolution"],cost);});}
    private async Task InventoryFormAsync()
    {
        var locations=await service.LocationsAsync();long locationId=0;
        if(!await dialogs.FormAsync("Chọn vị trí kiểm kê","Chỉ kiểm đếm số lượng sẵn sàng tại kệ; không tính thiết bị đang cho mượn.",[Choose("location","Vị trí",locations.Select(x=>new Choice(x.Id,x.Name)))],f=>{locationId=f.Id("location");return Task.CompletedTask;}))return;
        var location=locations.Single(x=>x.Id==locationId);var assets=(await service.AssetsAsync()).Where(x=>x.Location==location.Name && x.Kind!=AssetKind.Unit).ToList();Rules.Require(assets.Count>0,"Vị trí chưa có vật tư theo số lượng. Với cá thể, dùng Kiểm kê QR.");
        var fields=assets.Select(x=>Field(x.Id.ToString(),$"{x.Code} • {x.Name} • dự kiến {x.Available}",x.Available.ToString())).ToList();fields.Add(Field("note","Lý do kiểm kê / điều chỉnh"));
        await dialogs.FormAsync("Kiểm kê",location.Name+". Tạo phiên nháp, sau đó chọn Xác nhận điều chỉnh.",fields,async f=>{await service.CreateInventoryAsync(locationId,assets.ToDictionary(x=>x.Id,x=>f.Integer(x.Id.ToString())),f["note"]);});
    }
    private async Task UserFormAsync(UserAccount? user)
    {await dialogs.FormAsync(user==null?"Thêm tài khoản":"Sửa tài khoản","Mật khẩu 12 ký tự, chữ hoa/thường và số. Tài khoản có lịch sử chỉ vô hiệu hóa.",[Field("username","Tài khoản",user?.Username??""),Field("name","Tên hiển thị",user?.DisplayName??""),Choose("role","Vai trò",EnumChoices<Role>(Labels.Role),user==null?null:(long)user.Role),new FormField("password","Mật khẩu mới","",user==null,"Để trống để giữ mật khẩu khi sửa.",true),Choose("active","Trạng thái",[new(1,"Hoạt động"),new(0,"Vô hiệu")],user==null || user.IsActive?1:0)],f=>service.SaveUserAsync(user?.Id,f["username"],f["name"],(Role)f.Id("role"),f["password"],f.Id("active")==1));}
    private async Task PasswordAsync(){await dialogs.FormAsync("Đổi mật khẩu","Mật khẩu mới 12 ký tự trở lên, có chữ hoa/thường và số.",[new FormField("old","Mật khẩu hiện tại",secret:true),new FormField("new","Mật khẩu mới",secret:true)],f=>service.ChangePasswordAsync(f["old"],f["new"]));}
    private Task ThemeAsync(){if(Avalonia.Application.Current is { } app)app.RequestedThemeVariant=app.RequestedThemeVariant==ThemeVariant.Dark?ThemeVariant.Light:ThemeVariant.Dark;return Task.CompletedTask;}
    private async Task RoomSettingsAsync()
    {
        var settings=await service.SettingsAsync();await dialogs.FormAsync("Cấu hình phòng","Quyền bàn giao của giáo viên được kiểm tra tại service.",[Field("room","Tên phòng",settings.GetValueOrDefault("room")??"Phòng STEM"),Choose("teacher","Giáo viên được bàn giao",[new(1,"Có"),new(0,"Không")],settings.GetValueOrDefault("teacher.issue")=="true"?1:0),Field("session","Khóa phiên sau (phút)",settings.GetValueOrDefault("session.minutes")??"10")],async f=>{await service.SaveSettingAsync("room",f["room"]);await service.SaveSettingAsync("teacher.issue",f.Id("teacher")==1?"true":"false");await service.SaveSettingAsync("session.minutes",f["session"]);Room=f["room"];SessionMinutes=f.Integer("session");});
    }
    private async Task LlmSettingsAsync()
    {
        var settings=await service.SettingsAsync();await dialogs.FormAsync("LLM tùy chọn","Endpoint chat completion HTTPS. Chỉ gửi JSON tổng hợp đã xem trước; không gửi tên hoặc ảnh.",[Choose("enabled","Bật LLM",[new(0,"Tắt"),new(1,"Bật")],settings.GetValueOrDefault("llm.enabled")=="true"?1:0),Field("endpoint","Endpoint",settings.GetValueOrDefault("llm.endpoint")??"https://api.openai.com/v1/chat/completions"),Field("model","Tên model",settings.GetValueOrDefault("llm.model")??"gpt-5"),new FormField("key","API key mới","",false,"Để trống để giữ khóa hiện tại.",true),Choose("clear","Xóa khóa đang lưu",[new(0,"Giữ"),new(1,"Xóa")])],async f=>{await service.SaveSettingAsync("llm.enabled",f.Id("enabled")==1?"true":"false");await service.SaveSettingAsync("llm.endpoint",f["endpoint"]);await service.SaveSettingAsync("llm.model",f["model"]);if(f.Id("clear")==1 || f["key"]!="")await service.SaveSettingAsync("llm.secret",f.Id("clear")==1?"":f["key"]);});
    }
    private async Task LlmAsync()
    {
        Rules.Require(analytics!=null,"Tải thống kê trước.");var settings=await service.SettingsAsync();Rules.Require(settings.GetValueOrDefault("llm.enabled")=="true","LLM đang tắt. Bật trong Cài đặt nếu muốn dùng.");
        if(!await dialogs.ConfirmAsync("Dữ liệu sẽ gửi tới LLM",Insights.Preview(analytics!)+"\nCác asset được thay bằng alias; phản hồi chỉ là tư vấn."))return;
        Message=await service.AnalyzeLlmAsync(analytics!.StartUtc,analytics.EndUtc);
    }
    private async Task FaceSettingsAsync()
    {
        var settings=await service.SettingsAsync();await dialogs.FormAsync("Nhận diện khuôn mặt","Face embedding chạy cục bộ. Đã bật sẵn; vẫn cần consent hợp lệ trước khi đăng ký embedding. Không dùng khuôn mặt để đăng nhập.",[Choose("enabled","Trạng thái",[new(0,"Tắt"),new(1,"Bật")],settings.GetValueOrDefault("face.enabled")=="false"?0:1),Field("detector","Đường dẫn YuNet",settings.GetValueOrDefault("face.model")??Path.Combine(AppContext.BaseDirectory,"Models","face_detection_yunet_2026may.onnx")),Field("embedding","Đường dẫn SFace",settings.GetValueOrDefault("face.embeddingModel")??Path.Combine(AppContext.BaseDirectory,"Models","face_recognition_sface_2021dec.onnx")),Field("threshold","Ngưỡng cosine",settings.GetValueOrDefault("face.threshold")??"0.5",hint:"0.3–1, cần hiệu chỉnh trên dữ liệu được phép.")],async f=>{if(f.Id("enabled")==1){Rules.Require(face.Inspect(f["detector"]).Loaded && face.Inspect(f["embedding"]).Loaded,"Model chưa nạp được.");}await service.SaveSettingAsync("face.model",f["detector"]);await service.SaveSettingAsync("face.embeddingModel",f["embedding"]);await service.SaveSettingAsync("face.threshold",f["threshold"]);await service.SaveSettingAsync("face.enabled",f.Id("enabled")==1?"true":"false");});
    }
    private async Task<(string Detector,string Embedding,string Version,double Threshold)> FaceConfigAsync(bool requireEnabled=true)
    {
        var settings=await service.SettingsAsync();if(requireEnabled)Rules.Require(settings.GetValueOrDefault("face.enabled")=="true","Face AI đang tắt; dùng QR/mã hoặc bật trong Cài đặt.");
        var detector=settings.GetValueOrDefault("face.model")??Path.Combine(AppContext.BaseDirectory,"Models","face_detection_yunet_2026may.onnx");var embedding=settings.GetValueOrDefault("face.embeddingModel")??Path.Combine(AppContext.BaseDirectory,"Models","face_recognition_sface_2021dec.onnx");var info=await Task.Run(()=>face.Inspect(embedding));
        return(detector,embedding,info.Sha256,double.TryParse(settings.GetValueOrDefault("face.threshold"),CultureInfo.InvariantCulture,out var t)?t:0.5);
    }
    private async Task InspectModelsAsync(){var c=await FaceConfigAsync(false);var detector=await Task.Run(()=>face.Inspect(c.Detector));var embedding=await Task.Run(()=>face.Inspect(c.Embedding));Message=$"YuNet: {detector.Status}\nSHA256: {detector.Sha256}\n{string.Join("\n",detector.Inputs)}\n{string.Join("\n",detector.Outputs)}\nSFace: {embedding.Status}\nSHA256: {embedding.Sha256}";}
    private async Task<byte[]?> FaceImageAsync(bool capture)
    {
        var config=await FaceConfigAsync();byte[]? bytes;
        if(capture){bytes=await CaptureLiveAsync();if(bytes==null)return null;}else{var path=await dialogs.OpenAsync("Ảnh kiểm tra detector (không lưu)","*.png","*.jpg","*.jpeg");if(path==null)return null;Rules.Require(new FileInfo(path).Length<10*1024*1024,"Ảnh tối đa 10 MB.");bytes=await File.ReadAllBytesAsync(path);}
        try{var detections=await Task.Run(()=>face.Detect(bytes,config.Detector));Message=$"Phát hiện {detections.Count} khuôn mặt. Đây là phát hiện vùng mặt; chưa xác nhận danh tính. Ảnh không lưu vào DB/log.";return bytes;}
        catch{Array.Clear(bytes);throw;}
    }
    private async Task EnrollAsync(bool capture)
    {
        var borrower=(BorrowerRow)Selected().Source;var config=await FaceConfigAsync();long consent=0;
        if(!await dialogs.FormAsync("Đăng ký tự nguyện",borrower.Name+". Dùng một ảnh chụp trực diện. Nhân viên phải đối chiếu người thật và thẻ/QR; ứng dụng không kiểm tra liveness hoặc chống ảnh giả. Ảnh không lưu, chỉ embedding được mã hóa.",[Field("version","Phiên bản thông báo consent","1.0"),Field("basis","Căn cứ / xác nhận người giám hộ (nếu cần)")],async f=>{consent=await service.GrantConsentAsync(borrower.Id,f["version"],f["basis"]);}))return;
        var bytes=await FaceImageAsync(capture);if(bytes==null)return;try{var faces=await Task.Run(()=>face.Detect(bytes,config.Detector));Rules.Require(faces.Count==1,"Đăng ký cần đúng một khuôn mặt.");var embedding=await Task.Run(()=>face.Embed(bytes,faces[0],config.Embedding));try{await service.EnrollFaceAsync(consent,embedding,config.Version);}finally{Array.Clear(embedding);}Message="Đã đăng ký embedding mã hóa; ảnh không lưu.";}finally{Array.Clear(bytes);}
    }
    private async Task FindFaceAsync()
    {
        var config=await FaceConfigAsync();var bytes=await FaceImageAsync(true);if(bytes==null)return;
        try
        {
            var faces=await Task.Run(()=>face.Detect(bytes,config.Detector));Rules.Require(faces.Count==1,"Tra cứu cần đúng một khuôn mặt. Dùng QR/mã khi không xác định.");
            var vector=await Task.Run(()=>face.Embed(bytes,faces[0],config.Embedding));var profiles=await service.FaceProfilesAsync(config.Version);
            try{var candidates=FaceEngine.Match(vector,profiles,config.Threshold);var borrowers=await service.BorrowersAsync();Message=candidates.Count==0?"Không xác định (unknown). Dùng QR/mã người mượn.":"Ứng viên — phải xác nhận thẻ/QR trước giao dịch:\n"+string.Join("\n",candidates.Select(c=>$"{borrowers.Single(b=>b.Id==c.BorrowerId).Name} • cosine {c.Similarity:F3}"));}
            finally{Array.Clear(vector);foreach(var p in profiles)Array.Clear(p.Embedding);}
        }
        finally{Array.Clear(bytes);}
    }
    private async Task<BorrowerRow?> LookupBorrowerByFaceAsync()
    {
        var config=await FaceConfigAsync();var bytes=await FaceImageAsync(true);if(bytes==null)return null;
        try
        {
            var faces=await Task.Run(()=>face.Detect(bytes,config.Detector));Rules.Require(faces.Count==1,"Cần thấy đúng một khuôn mặt. Có thể tìm người mượn bằng mã/QR.");
            var vector=await Task.Run(()=>face.Embed(bytes,faces[0],config.Embedding));var profiles=await service.FaceProfilesAsync(config.Version);
            try
            {
                var matches=FaceEngine.Match(vector,profiles,config.Threshold);var borrowers=await service.BorrowersAsync();
                var options=matches.Select(m=>(Match:m,Borrower:borrowers.FirstOrDefault(b=>b.Id==m.BorrowerId))).Where(x=>x.Borrower is {Active:true}).ToList();
                if(options.Count==0){Message="Không có kết quả khuôn mặt đủ ngưỡng. Dùng mã/QR người mượn.";return null;}
                BorrowerRow? selected=null;
                var choices=options.Select(x=>new Choice(x.Borrower!.Id,$"{x.Borrower.Name} • {x.Borrower.Code} • {x.Match.Similarity:P0}"));
                if(await dialogs.FormAsync("Xác nhận người mượn","Kết quả chỉ dùng để gợi ý. Nhân viên cần đối chiếu người thật/thẻ trước khi tiếp tục; ảnh camera không được lưu.",[Choose("borrower","Chọn đúng người",choices)],f=>{selected=options.Single(x=>x.Borrower!.Id==f.Id("borrower")).Borrower;return Task.CompletedTask;}))
                {Message="Đã chọn người mượn; tiếp tục xác nhận thiết bị và phiếu.";return selected;}
                return null;
            }
            finally{Array.Clear(vector);foreach(var p in profiles)Array.Clear(p.Embedding);}
        }
        finally{Array.Clear(bytes);}
    }
    private async Task FilterLoansByFaceAsync()
    {
        var borrower=await LookupBorrowerByFaceAsync();if(borrower==null)return;
        faceFilteredBorrowerId=borrower.Id;page=1;ApplyRows();
        if(!Rows.Any())Message=$"Không có phiếu phù hợp của {borrower.Name} trong danh sách hiện tại.";
        else Message=$"Đã lọc phiếu của {borrower.Name}. Chọn phiếu đang mở rồi nhấn Trả thiết bị.";
    }
    private Task ClearLoanFaceFilterAsync(){faceFilteredBorrowerId=null;return Task.CompletedTask;}
    private async Task QrAsync(string text){var path=await dialogs.SaveAsync("Lưu QR PNG","STEM-QR.png","*.png");if(path!=null)Message="Đã lưu QR: "+await service.ExportQrAsync(text,path);}
    private async Task ExportAsync(string extension){var report=(string)Selected().Source;var dates=Dates();var path=await dialogs.SaveAsync("Xuất báo cáo",$"STEM-{report}.{extension}","*."+extension);if(path!=null)Message="Đã xuất: "+await service.ExportAsync(report,path,dates.Start,dates.End);}
    private async Task ImportAsync()
    {
        var path=await dialogs.OpenAsync("Import người mượn","*.csv","*.xlsx");if(path==null)return;Dictionary<string,string>? mapping=null;
        if(!await dialogs.FormAsync("Ánh xạ cột","Xác nhận tên cột trong file. Code và Name bắt buộc; các cột khác tùy chọn.",[Field("Code","Cột mã","Code"),Field("Name","Cột họ tên","Name"),Field("Class","Cột lớp","Class",false),Field("Type","Cột loại","Type",false),Field("Contact","Cột liên hệ","Contact",false)],f=>{mapping=new[]{"Code","Name","Class","Type","Contact"}.ToDictionary(k=>k,k=>f[k]);return Task.CompletedTask;}))return;
        var preview=await service.PreviewBorrowersAsync(path,mapping);Rules.Require(preview.Errors.Count==0,string.Join("\n",preview.Errors.Take(30)));
        if(await dialogs.ConfirmAsync("Xem trước import",$"{preview.Rows.Count} dòng hợp lệ:\n"+string.Join("\n",preview.Rows.Take(15).Select(x=>$"{x.Code} • {x.Name} • {x.ClassName}"))+"\nNhập toàn bộ trong một transaction?"))await service.ImportBorrowersAsync(preview);
    }
    private async Task RestoreAsync(){var path=await dialogs.OpenAsync("Chọn backup","*.zip");if(path==null)return;if(!await dialogs.ConfirmAsync("Phục hồi dữ liệu","Thay DB đang dùng bằng backup. Giữ DB cũ để phục hồi; khóa phiên và yêu cầu đăng nhập lại.\n"+Path.GetFileName(path)))return;await service.RestoreAsync(path);Logout();Message="Đã phục hồi. Đăng nhập bằng tài khoản trong bản backup.";}
}
internal static class ReservationExtensions
{
    public static string DetailsText(this ReservationRow r)=>$"{r.StartUtc.ToLocalTime():dd/MM/yyyy HH:mm} → {r.EndUtc.ToLocalTime():dd/MM/yyyy HH:mm}";
}
