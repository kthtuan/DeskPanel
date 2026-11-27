# Nhật ký thay đổi

## 0.13.4 (27/11/2026)
- Bật Wi-Fi / cập nhật: nếu Task Scheduler trên máy không chạy được DeskPanel, DeskPanel tự chạy lại theo cách khác thay vì tắt luôn.
- Có lối tắt **DeskPanel** trong Start Menu: mở DeskPanel (nếu chưa chạy) và trang cài đặt.

## 0.13.3 (27/11/2026)
- Cài đặt: nếu tác vụ tự chạy không khởi động được DeskPanel, file cài thử chạy thẳng DeskPanel. Chạy được thì mở trang cài đặt luôn; không được thì hiện lỗi thật và trạng thái tác vụ để chẩn đoán.
- DeskPanel ghi lỗi khởi động (ví dụ cổng bị chiếm) vào `deskpanel.log` thay vì tắt im lặng.

## 0.13.2 (27/11/2026)
- Cài đặt: chạy DeskPanel và mở trình duyệt ngay, rồi mới cài PawnIO (trước đây máy mới phải chờ winget tới 4 phút trong im lặng, trang báo "refused to connect").
- Hộp thoại cài đặt luôn nổi trên cùng, không bị che sau cửa sổ khác.

## 0.13.1 (26/11/2026)
- **Giao diện riêng từng trang**: Cài đặt → Giao diện → **Áp dụng cho** chọn một trang để chỉnh màu, nền, kiểu thẻ, bo góc, khoảng cách, cỡ chữ, số cột nút cho riêng trang đó. Nút **Dùng lại giao diện chung** để bỏ.
- Trang cài đặt báo **Điện thoại đã nối qua Wi-Fi** khi điện thoại vào bằng mã QR (trước chỉ đếm cáp USB).
- Thẻ ổ cứng không còn chữ chồng lên nhau khi chữ to; số giờ chạy ghi theo ngôn ngữ đang chọn.

## 0.13.0 (26/11/2026)
- **Tiếng Anh**: chọn 🌐 Tiếng Việt / English ở góc trên trang Cài đặt. Điện thoại đổi theo. Máy cài mới dùng ngôn ngữ của Windows; máy đang dùng giữ tiếng Việt. Hộp thoại cài/gỡ, app Android (theo ngôn ngữ điện thoại) và tiện ích trình duyệt cũng có tiếng Anh.
- **Trang Bảo mật & quyền riêng tư** (⚙ → Chung, và `SECURITY.md` trên GitHub): DeskPanel đọc gì, lưu gì, kết nối tới đâu, cách báo lỗi bảo mật.

## 0.12.2 (26/11/2026)
- App Android: tự xin quyền Camera trước khi mở máy quét QR; nếu app bị lỗi, lần mở sau hiện chi tiết lỗi để gửi lại. Màn hình đầu ghi số phiên bản app.

## 0.12.1 (26/11/2026)
- App Android: sửa lỗi app tự đóng khi bấm **Quét mã QR** (thiếu thư viện xin quyền camera).

## 0.12.0 (26/11/2026)
- **Tiện ích Chrome / Edge "DeskPanel AI"**: dùng luôn phiên claude.ai và chatgpt.com đã đăng nhập trong trình duyệt, 3 phút một lần gửi % giới hạn về DeskPanel trên máy (localhost). Không phải đăng nhập ở cửa sổ khác. Tải và hướng dẫn cài ở Cài đặt → Chung → Theo dõi AI.
- Cửa sổ đăng nhập trong DeskPanel vẫn giữ làm cách dự phòng.

## 0.11.0 (26/11/2026)
- **Một file cài duy nhất** `DeskPanel-Setup-0.11.0.exe`: nhấp đúp, bấm Yes là cài xong vào `C:\DeskPanel`, tự chạy khi đăng nhập Windows, mở trang cài đặt. Chạy file cài bản mới là cập nhật tại chỗ. Gỡ trong Windows Settings → Apps.
- Giao diện và APK Android nằm sẵn trong exe. Tự tải adb của Google (chế độ cáp USB) và tự cài PawnIO (nhiệt độ CPU), không phải tải tay.
- **Đăng nhập Claude / ChatGPT ngay trong DeskPanel** (Cài đặt → Chung → Theo dõi AI): cửa sổ claude.ai / chatgpt.com mở trên PC, đăng nhập một lần, widget AI tự lấy % giới hạn. Không cần cài Claude Code hay chạy lệnh.
- Bỏ install.cmd / update.cmd / uninstall.cmd.

## 0.10.2 (26/11/2026)
- Cài đặt: đợi DeskPanel chạy hẳn (tối đa 60 giây) rồi mới mở trình duyệt. Nếu không chạy được, cửa sổ cài đặt hiện lý do và các dòng cuối của nhật ký.

## 0.10.1 (26/11/2026)
- Widget AI: khi PC chưa đăng nhập Claude Code bằng tài khoản Claude, widget ghi rõ cách đăng nhập thay vì để trống.

## 0.10.0 (26/11/2026)
- Widget **Theo dõi AI** hiện % giới hạn chính thức của gói, giống trang "Mức sử dụng" trên web:
  - Claude (Pro/Max): phiên 5 giờ, tuần, tuần theo từng model, kèm tên gói (vd. Max 5x). Giới hạn này dùng chung cho Claude trên web, app và Claude Code.
  - ChatGPT/Codex: giới hạn 5 giờ và tuần, tên gói, credits.
  - Mỗi giới hạn là một vòng tròn kèm giờ làm mới, vạch "đáng lẽ đã dùng tới đây" và dự báo "theo nhịp này: hết sau …".
- Số liệu lấy bằng đăng nhập sẵn có của Claude Code và Codex trên PC, 3 phút một lần. DeskPanel chỉ đọc, không tự làm mới đăng nhập, không gửi mã đăng nhập ra điện thoại. Tắt được ở Cài đặt → Chung.

## 0.9.0 (26/11/2026)
- Widget mới **Theo dõi AI** (danh mục → nhóm Chung) và mẫu trang **AI**: xem mức dùng Claude Code và Codex trên PC.
  - Claude Code: token hôm nay, 7 ngày, số lượt, khung 5 giờ hiện tại và giờ làm mới.
  - Codex: token hôm nay, 7 ngày, thanh giới hạn 5 giờ và tuần (theo % Codex ghi lại) kèm giờ làm mới.
  - Chấm xanh khi phiên đang chạy, kèm tên thư mục dự án.
- Số liệu đọc từ nhật ký trên máy (`.claude\projects`, `.codex\sessions`), không gửi đi đâu, không cần đăng nhập.

## 0.8.2 (26/11/2026)
- Sửa mã QR trong Cài đặt → Chung bị cắt mất phần bên phải và bên dưới (ảnh QR không co theo khung).

## 0.8.1 (26/11/2026)
- iPhone: quét mã ghép nối bằng Camera rồi "Thêm vào MH chính" là chạy toàn màn hình và tự kết nối (biểu tượng mang theo mã ghép nối).
- Nhãn mã QR ghi rõ mã nào cho Android, mã nào dùng được cho cả iPhone.

## 0.8.0 (26/11/2026)
- Nút bấm → **+ Thêm phần mềm**: chọn nhiều phần mềm đã cài (đọc từ Start Menu) trong một lần, tự tạo nhóm và trang "Phần mềm".
- Nút mở chương trình hiện **icon thật** của phần mềm trên điện thoại và trong trang cài đặt (tùy chọn "Dùng icon thật của chương trình").

## 0.7.1 (26/11/2026)
- Nút AutoCAD và nút phím có chọn chương trình: kiểm tra lại cửa sổ đã thật sự lên trước rồi mới gõ. Nếu Windows chặn đổi cửa sổ, điện thoại báo lỗi thay vì gõ nhầm vào cửa sổ khác.
- Kiểm tra kết quả gửi phím: nếu Windows chặn (máy khóa, hộp thoại UAC), điện thoại báo lỗi thay vì báo thành công.

## 0.7.0 (26/11/2026)
- Kết nối qua Wi-Fi, không cần cáp: bật trong Cài đặt → Chung → **Kết nối qua Wi-Fi**. Điện thoại ghép nối một lần bằng mã QR. Từ mạng LAN chỉ xem bảng và bấm nút được, không mở được trang cài đặt hay cập nhật.
- App Android **DeskPanel** (file APK, Android 5 trở lên): quét mã QR, toàn màn hình, giữ màn hình sáng, mất kết nối tự thử lại. Dùng được cả với cáp USB.
- GitHub tự build APK; APK có sẵn trong bản Windows nên điện thoại tải được thẳng từ PC bằng mã QR.
- Sửa lỗi kiểm tra Origin của WebSocket không chạy (thứ tự middleware).

## 0.6.4 (26/11/2026)
- Sửa lỗi "Đường dẫn thư mục tạm quá dài" khi bấm Cập nhật / Cài bản (lệnh của tác vụ Task Scheduler vượt 261 ký tự).

## 0.6.3 (26/11/2026)
- Ô repo GitHub nhận cả link đầy đủ (`https://github.com/chu/repo` hoặc `.git`), tự cắt về dạng `chu/repo`.

## 0.6.2 (26/11/2026)
- Nhóm nhiều nút (ví dụ 17 nút AutoCAD) tự tăng số cột cho vừa trang, không còn hàng cuối bị cắt.
- README mới có ảnh chụp, logo và nhật ký thay đổi.

## 0.6.1 (26/11/2026)
- Cập nhật được từ repo private: dán token GitHub chỉ đọc vào Cài đặt → Chung, lưu mã hóa bằng DPAPI.

## 0.6.0 (26/11/2026)
- Nút cập nhật trong Cài đặt → Chung: tải file zip lên, hoặc tự kiểm tra và cài bản mới từ GitHub Releases.
- GitHub Actions tự build bản Windows và phát hành khi tăng số phiên bản.

## 0.5.2 (26/11/2026)
- Thời tiết có nguồn dự phòng MET Norway và wttr.in khi Open-Meteo báo lỗi 429.

## 0.5.1 (26/11/2026)
- Thời tiết: tọa độ sẵn cho 20 thành phố Việt Nam, nhận "vĩ độ,kinh độ", ưu tiên IPv4, thử lại sau 1/2/5/10 phút, hiện lý do lỗi.

## 0.5.0 (25/11/2026)
- Trang cài đặt mới kiểu PhoneBox: nhiều trang, kéo thả và đổi cỡ widget trên lưới 12 × 6, 16 loại widget, mẫu trang, xem trước trực tiếp, Ctrl+Z.
- Thẻ Giao diện: màu nhấn, nền, kiểu thẻ, bo góc, khoảng cách, cỡ chữ.

## 0.4.0 (25/11/2026)
- Trang Tổng quan: đồng hồ lớn, thời tiết, thẻ nhạc đang phát, 4 ô có đồ thị.

## 0.3.x (25/11/2026)
- Tách trang Tổng quan, Hệ thống, Ổ cứng và mỗi nhóm nút một trang; vuốt và chấm trang.
- update.cmd: cập nhật không cần gỡ cài đặt. Điện thoại tự tải lại khi có bản mới.
- Sửa đọc SMART ổ cứng, chọn đúng GPU rời.

## 0.1–0.2 (25/11/2026)
- Bản đầu: thông số PC, sức khỏe ổ cứng, nút AutoCAD, phím tắt, nối qua USB.
