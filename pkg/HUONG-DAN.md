# DeskPanel 0.6: hướng dẫn cài

DeskPanel biến điện thoại Android thành màn hình phụ cho PC Windows. Màn hình hiển thị CPU, GPU, RAM, nhiệt độ và sức khỏe ổ cứng, kèm các nút bấm (lệnh AutoCAD, Calculator, phím tắt). Điện thoại nối với PC qua cáp USB và không mở cổng nào ra mạng LAN.

## A. Trên PC (làm một lần)

1. **Giải nén** thư mục `DeskPanel` vào `C:\DeskPanel`. Không đặt trong Downloads hoặc Desktop để tránh xóa nhầm.
2. **Tải Android Platform Tools cho Windows** tại https://developer.android.com/tools/releases/platform-tools. Giải nén rồi chép nguyên thư mục `platform-tools` vào `C:\DeskPanel\`, để có file `C:\DeskPanel\platform-tools\adb.exe`.
3. **Cài PawnIO** để đọc được nhiệt độ CPU (GPU, RAM và ổ cứng không cần bước này). Mở PowerShell rồi chạy:
   ```
   winget install namazso.PawnIO
   ```
   Cài xong thì khởi động lại máy.
4. Nhấp đúp **`install.cmd`**, rồi bấm **Yes** khi Windows hỏi quyền admin.
   - Nếu Windows SmartScreen chặn (file chưa có chữ ký số), bấm **More info → Run anyway**.
   - Từ đây DeskPanel tự chạy ngầm mỗi khi bạn đăng nhập Windows.

## B. Trên điện thoại (làm một lần)

1. Vào **Cài đặt → Thông tin điện thoại**, chạm 7 lần vào **Số hiệu bản tạo** (Build number) để bật Tùy chọn nhà phát triển.
2. Vào **Tùy chọn nhà phát triển**:
   - Bật **Gỡ lỗi USB** (USB debugging).
   - Bật **Không khóa màn hình / Luôn bật màn hình khi sạc** (Stay awake).
3. Cắm cáp USB vào PC. Khi điện thoại hỏi **"Cho phép gỡ lỗi USB?"**, tích **Luôn cho phép từ máy tính này** rồi bấm **Cho phép**.
4. Mở **Chrome**, vào `http://localhost:8765`.
5. Bấm menu **⋮ → Thêm vào màn hình chính**. Từ lần sau, mở DeskPanel bằng biểu tượng này để chạy toàn màn hình.

## C. Các trang trên điện thoại

Vuốt trái/phải để chuyển trang. Chấm ở đáy màn hình cho biết đang ở trang nào, chạm vào chấm để nhảy thẳng tới trang đó. Điện thoại nhớ trang bạn xem gần nhất.

Bộ trang mặc định gồm Tổng quan (đồng hồ, thời tiết, nhạc đang phát, 4 ô CPU/GPU/RAM/Mạng), Hệ thống, Ổ cứng và mỗi nhóm nút một trang. Bạn sửa lại toàn bộ được ở phần D.

- Thời tiết lấy từ Open-Meteo qua mạng của PC. Nếu Open-Meteo từ chối (ví dụ lỗi 429 do IP dùng chung), DeskPanel tự chuyển sang MET Norway, rồi wttr.in. Cả ba đều miễn phí, không cần tài khoản. Đổi thành phố ở thẻ **Chung** trong trang cài đặt, để trống nếu muốn tắt.
- Thẻ nhạc đọc bài đang phát trên Windows (Spotify, YouTube trên Chrome/Edge…).

## D. Trang cài đặt

Trên PC, mở `http://localhost:8765/settings.html` (hoặc bấm ⚙ trên điện thoại). Trang có 4 thẻ:

**1. Trang & widget**
- Cột trái: danh sách trang. Kéo để đổi thứ tự, bấm **+ Thêm trang** để tạo trang trống hoặc từ mẫu (Tổng quan, Hệ thống, Vòng đo, Ổ cứng, Làm việc + nút, hoặc một nhóm nút).
- Giữa: lưới 12 × 6 ô tượng trưng cho màn điện thoại. Kéo widget để di chuyển, kéo góc dưới phải để đổi kích thước. Widget không được chồng lên nhau (khung chuyển đỏ nếu chồng).
- Bên phải: danh mục widget (bấm để thêm vào chỗ trống, hoặc kéo thả vào lưới) và bảng thuộc tính của widget đang chọn: tiêu đề, màu, chỉ số (với Vòng đo), nhóm nút, vị trí, nhân bản, chuyển sang trang khác, xóa. Khi không chọn widget nào, bảng này cho đổi tên, biểu tượng, ẩn, nhân bản hoặc xóa trang.
- Bên dưới có khung **xem trước** chạy số liệu thật, cập nhật ngay khi bạn sửa.
- Phím tắt: mũi tên dịch widget, Shift + mũi tên đổi kích thước, Delete xóa, Ctrl+Z / Ctrl+Y hoàn tác / làm lại, Ctrl+S lưu.

**2. Nút bấm**: thư viện nút theo nhóm. Thêm, sửa, thử, xóa, đổi thứ tự, đổi tên nhóm. Nhóm nào chưa nằm trên trang nào sẽ có nút **Tạo trang cho nhóm**.

| Loại nút | Ví dụ nội dung | Tác dụng |
|---|---|---|
| Lệnh AutoCAD | `L`, `TR`, `O`, `Z E` | Đưa AutoCAD lên trước, bấm Esc 2 lần, gõ lệnh rồi Enter. Dấu cách tương đương Enter. |
| Mở chương trình | `%SystemRoot%\System32\calc.exe` | Mở chương trình |
| Mở web / thư mục | `D:\BanVe`, `https://...` | Mở bằng trình duyệt hoặc Explorer |
| Tổ hợp phím | `Ctrl+S`, `Win+Shift+S`, `VolumeUp` | Gửi phím vào cửa sổ đang mở, hoặc vào chương trình ghi ở ô "Đưa lên trước" |
| Gõ chữ | `Xin chào` | Gõ đoạn chữ, có thể kèm Enter |

Nút AutoCAD tìm cả `acad.exe` (AutoCAD) lẫn `acadlt.exe` (AutoCAD LT).

**3. Giao diện**: màu nhấn, nền (cực quang, tối, đen AMOLED), kiểu thẻ (viền, phẳng, không nền), bo góc, khoảng cách, cỡ chữ, số cột nút. Có xem trước từng trang.

**4. Chung**: thành phố thời tiết, cổng, đường dẫn adb, tự nối USB, tình trạng kết nối.

Mọi thay đổi chỉ có hiệu lực khi bấm **Lưu**. Lưu xong, điện thoại tự cập nhật. Bản 0.5 tự chuyển các nút và tab cũ của bạn sang dạng trang.

## E. Cập nhật lên bản mới

**Cách 1 (từ bản 0.6 trở đi, dễ nhất): nút Cập nhật.** Mở trang cài đặt, chọn thẻ **Chung**, mục **Cập nhật DeskPanel**. Chọn file `DeskPanel-…-win-x64.zip` vừa tải (không cần giải nén) rồi bấm **Cập nhật**. DeskPanel tự tắt, thay bản mới, chạy lại sau khoảng 10–20 giây và trang tự tải lại.

**Cách 2: tự kiểm tra trên GitHub.** Điền tên repo (dạng `chu/ten-repo`, repo phải công khai) vào ô ở cùng mục rồi bấm **Lưu**. DeskPanel kiểm tra 6 giờ một lần. Khi có bản mới, nút **Cài bản …** hiện ra.

**Cách 3: update.cmd** (dùng khi nút ở trên báo lỗi, hoặc khi đang ở bản cũ hơn 0.6):
1. Giải nén bản mới vào một thư mục bất kỳ, ví dụ `Downloads\DeskPanel-moi`. Không giải nén đè thẳng vào thư mục đã cài vì lúc đó DeskPanel đang chạy và khóa file `.exe`.
2. Trong thư mục vừa giải nén, nhấp đúp **`update.cmd`** và bấm **Yes**. Xong thì xóa thư mục vừa giải nén.

Cả ba cách đều giữ nguyên `config.json` (nút, trang, giao diện) và `platform-tools`. Nhật ký cập nhật nằm ở `update.log` trong thư mục cài.

## F. Khi có trục trặc

- **Điện thoại báo "Mất kết nối"**: mở trang cài đặt trên PC, xem dòng **Điện thoại**. Dòng này cho biết lỗi nằm ở đâu: chưa thấy `adb.exe`, chưa cho phép gỡ lỗi USB, hay chưa cắm cáp.
- **Thời tiết báo lỗi**: dòng chữ nhỏ dưới thông báo cho biết lý do (mạng, DNS, tường lửa, chứng chỉ HTTPS…). Trang cài đặt, thẻ **Chung**, dòng **Thời tiết** cũng hiện lý do. Nếu tên thành phố không tìm thấy, nhập tọa độ dạng `10.78,106.70`. DeskPanel tự thử lại sau 1, 2, 5 rồi 10 phút.
- **Nhiệt độ CPU hiện `--`**: chưa cài PawnIO, hoặc chưa khởi động lại máy sau khi cài.
- **Muốn xem toàn bộ cảm biến đọc được**: mở `http://localhost:8765/api/sensors`.
- Nhật ký lỗi nằm ở `C:\DeskPanel\deskpanel.log`.
- **Gỡ cài đặt**: chạy `uninstall.cmd`, sau đó xóa thư mục.

## Về an toàn

- DeskPanel chỉ nhận kết nối từ chính PC (localhost). Điện thoại vào được là nhờ `adb reverse` qua cáp USB, còn các máy khác trong mạng không vào được.
- Mọi lệnh được lưu trong `config.json` trên PC. Điện thoại chỉ gửi mã nút.
- Trang web lạ mở trên PC không gọi được DeskPanel: mọi yêu cầu thay đổi phải kèm header riêng và đúng tên máy `localhost`.
- DeskPanel chạy với quyền admin vì cần đọc nhiệt độ và SMART. Các chương trình nó mở (không kèm tham số) vẫn chạy với quyền thường.

## Ghi công

Giao diện từ bản 0.2 trở đi (bố cục trang Tổng quan và Hệ thống, đồ thị nhỏ, chấm trang, nút nền tối viền màu, trình xếp widget dạng lưới) lấy cảm hứng từ [PhoneBox](https://github.com/ersinkoc/phonebox) (giấy phép MIT, © 2026 Ersin KOÇ). DeskPanel không chép mã của PhoneBox; toàn bộ được viết lại.
