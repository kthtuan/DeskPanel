# Nhật ký thay đổi

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
