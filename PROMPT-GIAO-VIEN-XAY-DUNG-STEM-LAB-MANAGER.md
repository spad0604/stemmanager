# Prompt giao việc xây dựng STEM Lab Manager

Tôi là giáo viên phụ trách phòng STEM của Trường THCS Trọng Điểm. Tôi muốn bạn xây dựng giúp tôi một phần mềm quản lý phòng STEM có tên **STEM Lab Manager**, để theo dõi thiết bị, sắp xếp lịch sử dụng, thực hiện mượn–trả và xem báo cáo. Phần mềm cần dễ sử dụng đối với giáo viên, có giao diện tiếng Việt và chạy được trên máy tính Windows ngay cả khi không có Internet.

Tôi muốn bạn viết mã, xây dựng đầy đủ các chức năng, chạy kiểm tra và tạo bộ cài để tôi có thể sử dụng. Yêu cầu cụ thể như sau.

## 1. Xây dựng phần mềm từ đầu

Hãy tạo mới phần mềm từ các yêu cầu tôi mô tả trong prompt này. Bạn tự tổ chức mã nguồn, thiết kế cơ sở dữ liệu và xây dựng các màn hình cùng chức năng cần thiết. Tôi muốn có một ứng dụng hoàn chỉnh để sử dụng trong công việc hằng ngày.

Tôi muốn ứng dụng desktop dùng C#/.NET, Avalonia và SQLite. Dữ liệu phải được lưu lại sau khi đóng phần mềm. Các màn hình phải làm việc với dữ liệu thật trong cơ sở dữ liệu, không chỉ hiển thị số liệu hoặc nút bấm dựng sẵn.

Hãy chọn phiên bản .NET phù hợp, cấu hình các thư viện cần thiết và thiết lập ứng dụng desktop làm dự án khởi động. Cấu hình phiên bản SDK cần cho phép người phát triển cài đặt và chạy được thuận tiện. Cuối cùng ghi rõ lệnh chạy bằng dotnet cho tôi.

## 2. Giao diện và màn hình đăng nhập

Giao diện dùng nền trắng, điểm nhấn màu xanh, chữ dễ đọc và bố cục thống nhất giữa các màn hình. Các nút, bảng, ô nhập và cửa sổ có khoảng cách hợp lý, hiển thị đầy đủ nội dung. Phần mềm cần dùng thuận tiện trên màn hình laptop thông thường và ở các mức phóng đại của Windows.

Tôi sẽ cung cấp logo trường trong file logo.png và ảnh cổng trường trong file school.jpg. Hãy dùng logo trường làm biểu tượng ứng dụng, biểu tượng bộ cài và nhận diện trong giao diện. Màn hình đăng nhập mở ở trạng thái phóng to, có khu vực giới thiệu trường và khối đăng nhập gọn, cân đối. Logo cùng lời giới thiệu đặt ở phần trên của ảnh; ô tài khoản, mật khẩu và nút đăng nhập có kích thước vừa phải, dễ thao tác.

Lần chạy đầu cho phép tạo tên phòng và tài khoản quản trị, không dùng chung một mật khẩu mặc định. Có đăng nhập, đăng xuất, đổi mật khẩu và khóa phiên khi không sử dụng một thời gian. Báo lỗi bằng lời dễ hiểu, chỉ rõ người dùng cần sửa ở đâu.

Các trường ngày giờ trong biểu mẫu và bộ lọc hiển thị trong một ô gọn. Khi bấm vào, người dùng chọn ngày bằng lịch và chọn giờ bằng đồng hồ. Ngày hiển thị theo định dạng ngày/tháng/năm, giờ theo hệ 24 tiếng.

## 3. Người sử dụng và phân quyền

Về nghiệp vụ, tôi muốn phân biệt người phụ trách quản lý và người mượn. Người mượn có thể là giáo viên hoặc học sinh; không bắt họ phải có tài khoản phần mềm mới được lập hồ sơ mượn. Hồ sơ người mượn và tài khoản đăng nhập là hai phần riêng.

Các tài khoản trong ứng dụng cần có quyền rõ ràng:

- **Quản trị:** quản lý tài khoản, phân quyền, cấu hình, sao lưu và phục hồi; sử dụng đầy đủ các chức năng quản lý.
- **Quản lý phòng STEM:** quản lý thiết bị, kho, người mượn, lịch đặt, giao nhận, kiểm kê, bảo trì, báo cáo và nhận diện theo quyền được giao.
- **Giáo viên sử dụng:** tra cứu thiết bị, tạo yêu cầu đặt trước, xem lịch và giao dịch được phép; chỉ được giao nhận khi được cấp quyền phù hợp.
- **Học sinh:** tra cứu thiết bị, xem tổng quan và báo cáo tổng hợp không có thông tin cá nhân. Không cho xem danh sách người mượn, Phân tích AI, đăng ký khuôn mặt, quản lý tài khoản, nhập kho, kiểm kê, bảo trì, sao lưu hoặc sửa dữ liệu.

Mỗi tài khoản chỉ thấy các trang và thao tác phù hợp với vai trò của mình. Quyền truy cập cũng phải được kiểm tra khi xử lý và lưu dữ liệu. Báo cáo có tên, lớp hoặc liên hệ của người mượn phải được giới hạn theo quyền.

## 4. Tổng quan và tìm kiếm

Màn hình Tổng quan cần có các ô Thiết bị sẵn sàng, Đang mượn, Quá hạn và Lịch hôm nay. Bấm vào ô nào phải mở danh sách chi tiết đúng với nội dung đó. Ví dụ bấm Thiết bị sẵn sàng thì ra danh sách có thể bàn giao; bấm Đang mượn thì thấy người nhận, ngày mượn, hạn trả và thiết bị đang giữ.

Cho tìm theo tên hoặc mã, lọc theo trạng thái, danh mục, vị trí và khoảng thời gian phù hợp. Có sắp xếp, phân trang và nút tải lại. Khi lọc xong, số lượng hiển thị và dữ liệu trong bảng phải khớp nhau. Có tìm nhanh trong ứng dụng và trung tâm thông báo cho lịch chờ duyệt, sắp đến hạn, quá hạn, tồn thấp và thiết bị cần bảo trì.

## 5. Phòng, vị trí và hồ sơ thiết bị

Cho thêm, sửa danh mục thiết bị, phòng, kho, kệ và vị trí lưu trữ để giáo viên biết đồ nằm ở đâu. Trong phạm vi ứng dụng này, quản lý một phòng STEM có nhiều vị trí; không cần xây dựng hệ thống quản lý tập trung nhiều trường.

Hồ sơ thiết bị cần có mã riêng, tên, danh mục, loại quản lý, đơn vị tính, vị trí, mô tả, ảnh, số lượng, tình trạng và ghi chú. Cho nhập các thông tin bổ sung khi cần như nhà sản xuất, model, số serial, ngày mua và giá tham khảo. Phân biệt thiết bị theo từng chiếc có mã riêng, thiết bị quản lý theo số lượng và vật tư tiêu hao.

Biểu mẫu tạo và sửa thiết bị có phần tải ảnh từ máy, xem trước và thay ảnh. Ảnh được lưu cùng dữ liệu của ứng dụng để tiếp tục sử dụng sau khi file gốc được di chuyển. Danh sách thiết bị có cột ảnh thu nhỏ; màn hình chọn thiết bị để mượn cũng hiển thị ảnh để dễ nhận biết. Thiết bị chưa có ảnh dùng hình đại diện mặc định.

Đối với thiết bị quản lý theo từng chiếc, mỗi chiếc có mã riêng, mã QR, số serial, vị trí và tình trạng. Cho ghi chú trầy xước, hư hỏng, thiếu phụ kiện và đính kèm ảnh để đối chiếu khi bàn giao, nhận trả. Đơn vị đếm hiển thị là “Cái”.

Thiết bị đã phát sinh giao dịch không được xóa mất lịch sử. Nếu không dùng nữa thì chuyển sang ngừng sử dụng hoặc lưu trữ. Có thể chuyển vị trí và xem lịch sử thay đổi.

## 6. Nhập kho, điều chỉnh, kiểm kê và bảo trì

Cho nhập số lượng ban đầu, nhập bổ sung và điều chỉnh tăng giảm với lý do cụ thể. Phải ghi ai thực hiện và thời điểm thực hiện. Với vật tư tiêu hao, số đã dùng được ghi nhận vào lịch sử xuất sử dụng, không yêu cầu trả như thiết bị thông thường.

Cho tạo phiên kiểm kê theo phòng hoặc vị trí, dùng QR hoặc nhập số lượng kiểm đếm. Phần mềm đối chiếu số đang có trong sổ với số đếm được, hiển thị chênh lệch để người quản lý kiểm tra và xác nhận trước khi điều chỉnh tồn.

Có chức năng báo hỏng, ghi mô tả, ảnh, người báo, ngày báo và quá trình sửa chữa. Thiết bị đang bảo trì, đã mất hoặc ngừng sử dụng không được hiện là sẵn sàng cho mượn. Sau khi sửa xong, người có quyền xác nhận thì mới đưa vào sử dụng lại.

## 7. Hồ sơ người mượn

Cho thêm, sửa, tìm kiếm và ngừng sử dụng hồ sơ người mượn. Thông tin gồm mã, họ tên, lớp hoặc bộ môn, liên hệ khi cần, người phụ trách và ghi chú. Không bắt buộc tách riêng quy trình mượn của giáo viên và học sinh nếu nghiệp vụ giống nhau.

Cho nhập danh sách người mượn và thiết bị từ CSV/Excel, có mẫu, xem trước, chọn cột và báo dòng bị lỗi hoặc trùng mã trước khi ghi. Không tự nhập một phần rồi bỏ dở mà không thông báo. Không lấy tên giống nhau để tự kết luận hai người là một người.

## 8. Đặt trước, mượn, trả và gia hạn

Cho đặt thiết bị theo khoảng ngày giờ, số lượng và mục đích sử dụng. Có lịch dễ nhìn, yêu cầu chờ duyệt, duyệt, từ chối kèm lý do, hủy và chuyển sang phiếu mượn khi bàn giao. Lịch đã duyệt chưa có nghĩa thiết bị đã được giao. Khi đặt trùng thời gian hoặc vượt số lượng, cần chỉ rõ xung đột để chọn lại.

Phiếu mượn nên theo ba bước: chọn người mượn, chọn thiết bị, kiểm tra và xác nhận. Tìm người bằng tên, mã, QR hoặc khuôn mặt. Chọn nhiều thiết bị trong một phiếu, thấy ảnh và số có thể mượn, chọn từng cái khi thiết bị có mã riêng. Có thể chọn đồ từ danh sách thiết bị vào giỏ rồi mở phiếu mượn với các món đã chọn.

Trước khi xác nhận, hiển thị người giao, người nhận, các thiết bị, số lượng, hạn trả, mục đích và ghi chú. Có biên nhận hoặc phiếu xuất được, kèm mã phiếu/QR để tra cứu. Nếu người mượn đang có phiếu quá hạn thì cảnh báo rõ.

Khi trả, cho tìm bằng mã phiếu, người mượn, QR hoặc khuôn mặt. Nếu một người có nhiều phiếu đang mở thì hiện các phiếu để chọn, không tự đóng tất cả. Cho trả toàn bộ hoặc một phần, ghi số lượng, tình trạng tốt, trầy xước, hỏng, mất, phụ kiện thiếu và ảnh nếu cần. Thiết bị hỏng chuyển sang bảo trì; thiết bị mất không được cộng lại vào số sẵn sàng. Có gia hạn kèm lý do, kiểm tra lịch đặt trước trước khi chấp nhận.

Không được cho mượn một cái cho hai người cùng lúc, cho mượn quá tồn, trả quá lượng còn nợ hoặc điều chỉnh làm âm kho. Trước khi lưu phải kiểm tra lại dữ liệu, kể cả khi hai cửa sổ cùng thao tác. Phiếu nhiều dòng có lỗi thì không được lưu dở dang một nửa. Mọi sửa đổi quan trọng cần giữ được lịch sử, không xóa dấu vết giao dịch.

## 9. Nhận diện khuôn mặt để thao tác nhanh

Tôi muốn tính năng nhận diện khuôn mặt được bật mặc định ở cài đặt mới, có thể tắt trong Cài đặt. Bật tính năng không có nghĩa tự mở camera hoặc tự đăng ký mọi người. Chỉ lưu thông tin nhận diện sau khi người được đăng ký đồng ý và người có quyền xác nhận.

Người dùng đăng ký bằng **một ảnh trực diện** chụp từ camera. Cửa sổ camera hiển thị hình ảnh trực tiếp và có nút chụp, xem lại, xác nhận. Ảnh cần rõ và có đúng một khuôn mặt; nếu ảnh chưa đạt thì hướng dẫn chụp lại bằng lời dễ hiểu.

Nhận diện sử dụng mô hình chạy trên máy để đối chiếu ảnh chụp với hồ sơ đã đăng ký. Khi mượn và trả, ứng dụng tìm hồ sơ hoặc phiếu liên quan rồi hiển thị cho người vận hành xác nhận trước khi lưu giao dịch. Nếu kết quả chưa đủ chắc chắn hoặc người đó chưa đăng ký, thông báo chưa xác định được và cho tìm bằng tên, mã hoặc QR.

Không dùng nhận diện một ảnh để thay mật khẩu đăng nhập quản trị, và không giới thiệu nó như tính năng chống giả mạo. Luôn có QR, mã và tìm tên để thay thế. Có đăng ký lại, thu hồi và xóa thông tin nhận diện. Không lưu ảnh camera thừa hoặc ghi dữ liệu khuôn mặt vào nhật ký. Camera phải được giải phóng khi đóng cửa sổ; nhận diện hoặc xử lý ảnh không được làm đơ giao diện.

## 10. Phân tích AI và biểu đồ

Phần Phân tích AI hỗ trợ người phụ trách theo dõi việc sử dụng và chuẩn bị thiết bị. Sử dụng mô hình phân tích hoặc dự báo chạy cục bộ từ lịch sử mượn–trả, trình bày kết quả chủ yếu bằng biểu đồ và nhận xét ngắn. Các chức năng phân tích cơ bản hoạt động offline.

Khi mở phân tích, cho chọn khoảng thời gian và hiển thị cửa sổ biểu đồ rõ ràng: xu hướng mượn theo ngày/tuần, nhóm thiết bị được mượn nhiều, tình trạng trả đúng hạn/quá hạn và dự báo nhu cầu 7 ngày tới. Có tiêu đề, đơn vị, chú giải và số liệu đọc được. Khi phù hợp, cho bấm vào nhóm trên biểu đồ để xem danh sách liên quan và xuất biểu đồ để dùng trong báo cáo.

Các nhận xét dùng ngôn ngữ gần gũi với giáo viên, chẳng hạn nhóm thiết bị cần kiểm tra trước giờ học hoặc những phiếu cần nhắc trả. Thông tin kỹ thuật đặt trong phần giải thích riêng. Nếu dữ liệu chưa đủ để dự báo thì thông báo cần thêm lịch sử. Dự báo là thông tin tham khảo để đối chiếu với lịch học, lịch đặt và tồn kho.

Nếu có thêm LLM để hỏi đáp, cho giáo viên hỏi bằng tiếng Việt về số liệu tổng hợp và yêu cầu mở biểu đồ liên quan. Ví dụ: “Tháng này thiết bị nào được dùng nhiều nhất?”, “Cho tôi xem tình hình trả thiết bị”, “Tuần tới cần chuẩn bị những gì?”. LLM phải lấy số liệu qua chức năng truy vấn có kiểm soát của ứng dụng, không tự bịa số và không được tùy ý sửa dữ liệu hoặc chạy lệnh trong máy.

LLM qua mạng là phần bổ sung, chỉ bật khi quản lý cấu hình. Chỉ gửi dữ liệu tổng hợp cần thiết, không gửi tên, lớp, liên hệ, ảnh hoặc thông tin nhận diện. Không có mạng hay lỗi dịch vụ thì phần quản lý và biểu đồ local vẫn hoạt động. Trong tài liệu kỹ thuật hãy giải thích rõ mô hình nào chạy ở máy, LLM nào cần cấu hình và giới hạn của từng phần.

## 11. Báo cáo và xuất dữ liệu

Cho chọn kỳ báo cáo và xuất CSV, Excel, PDF phù hợp để giáo viên dùng được. Tôi cần các báo cáo sau:

- Danh sách thiết bị, số lượng sẵn sàng, đang mượn, bảo trì, mất và ngừng sử dụng.
- Chi tiết mượn–trả theo thiết bị hoặc theo người mượn: số lần, số lượng, ngày mượn, hạn trả, ngày trả và trạng thái.
- Quá hạn theo thiết bị hoặc người mượn để tiện nhắc trả.
- Lịch đặt trước, kết quả kiểm kê, bảo trì và tình hình sử dụng theo kỳ.
- **Báo cáo Nhập/Điều chỉnh**, có ít nhất các cột **STT, Tên thiết bị, Số lượng, Lý do**. Phân biệt tăng và giảm, có thể bổ sung ngày và người thực hiện.

Báo cáo phải lấy từ dữ liệu đang lưu, đúng bộ lọc và đúng quyền. File xuất không bị mất dấu tiếng Việt, tràn cột hoặc thiếu trang; các tổng số cần khớp với chi tiết. Báo cáo của Học sinh chỉ có thông tin tổng hợp, không lộ hồ sơ cá nhân.

## 12. Lưu dữ liệu, sao lưu và nhật ký

Lưu dữ liệu ở thư mục phù hợp của tài khoản Windows, không ghi cơ sở dữ liệu vận hành vào thư mục cài ứng dụng. Mật khẩu không được lưu dạng đọc trực tiếp. Khóa API và thông tin nhận diện cần được bảo vệ khi lưu; nhật ký không được chứa mật khẩu, khóa bí mật hoặc dữ liệu khuôn mặt.

Cho người có quyền sao lưu, xem lịch sử sao lưu, đặt lịch và phục hồi. Bản sao lưu cần đủ dữ liệu, ảnh và các thành phần cần thiết để phục hồi được. Thử file trước khi thay dữ liệu đang dùng, giữ đường phục hồi nếu thao tác lỗi. Nâng cấp ứng dụng phải giữ dữ liệu cũ và có cách chuyển cấu trúc dữ liệu an toàn.

Ghi nhật ký các việc quan trọng như nhập/điều chỉnh tồn, tạo và trả phiếu, duyệt lịch, thay đổi tài khoản, xuất báo cáo cá nhân, đăng ký/xóa nhận diện và phục hồi dữ liệu. Lịch sử giao dịch được lưu để tra cứu và đối chiếu.

## 13. Chạy và kiểm tra đầy đủ

Sau khi xây dựng, hãy tự build, chạy bằng dotnet và kiểm tra các luồng chính. Sử dụng cơ sở dữ liệu kiểm thử riêng với dữ liệu minh họa đủ để kiểm tra các trạng thái, biểu đồ và báo cáo.

Cần kiểm tra thêm/sửa phòng, vị trí, danh mục, thiết bị và ảnh; nhập danh sách; nhập/điều chỉnh kho; kiểm kê; bảo trì; đặt trước và xung đột; mượn nhiều dòng; mượn vượt tồn; trả một phần; trả hỏng/mất; gia hạn; tìm người bằng khuôn mặt; báo cáo, biểu đồ, xuất file; các tài khoản và quyền; sao lưu, phục hồi; đóng rồi mở lại ứng dụng. Kiểm tra các trường hợp nhập sai, danh sách trống và lỗi mạng ở phần LLM.

Kiểm tra giao diện các cửa sổ bật lên, ô nhập, bảng và ô ngày giờ ở nhiều kích thước màn hình. Bấm thử các ô Tổng quan để chắc chắn chúng mở đúng danh sách. Đối chiếu số liệu báo cáo và biểu đồ với giao dịch trong cơ sở dữ liệu.

Kiểm tra chức năng camera và nhận diện với người tham gia đã đồng ý: xem hình trực tiếp, chụp ảnh đăng ký, nhận diện khi mượn và trả, xử lý người chưa đăng ký, đóng và mở lại camera. Ghi rõ kết quả và điều kiện kiểm tra; phần cần thiết bị hoặc người tham gia mà chưa thực hiện được thì liệt kê khi bàn giao.

## 14. Tạo bộ cài và bàn giao

Cuối cùng tạo bộ cài Windows dạng Setup.exe, có logo trường và kèm mô hình AI, thư viện cần thiết cùng môi trường chạy. Máy nhận không cần cài SDK hay tự đi tải mô hình mới mở được các chức năng local. Có thể làm thêm bản portable. Kiểm tra cài, mở, gỡ và nâng cấp mà không làm mất dữ liệu người dùng.

Không đưa cơ sở dữ liệu vận hành, tài khoản cá nhân, mật khẩu, khóa API hoặc thông tin nhận diện của tôi vào bộ cài. Nếu có chế độ demo, để dữ liệu demo tách khỏi dữ liệu sử dụng hằng ngày. Kèm giấy phép của những thư viện và mô hình được phân phối.

Khi bàn giao, gửi bộ cài, mã nguồn, hướng dẫn sử dụng ngắn, lệnh chạy bằng dotnet và kết quả kiểm tra. Nói rõ phần đã hoàn thành, phần cần cấu hình và phần cần kiểm tra thêm trên máy thực tế.

Bạn hãy bắt đầu tạo ứng dụng từ những yêu cầu trên, thực hiện lần lượt các chức năng, tự chạy kiểm tra và sửa lỗi đến khi có phần mềm cùng bộ cài để tôi sử dụng.
