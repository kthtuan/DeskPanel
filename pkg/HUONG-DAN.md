# DeskPanel: hướng dẫn cài

DeskPanel biến điện thoại Android thành màn hình phụ cho PC Windows. Màn hình hiển thị CPU, GPU, RAM, nhiệt độ và sức khỏe ổ cứng, kèm các nút bấm (lệnh AutoCAD, Calculator, phím tắt). Điện thoại nối với PC qua cáp USB, hoặc qua Wi-Fi nếu bạn bật (mục B2).

## A. Trên PC (làm một lần)

1. Tải **`DeskPanel-Setup-….exe`** ở trang Releases, nhấp đúp, bấm **Yes** khi Windows hỏi quyền admin, rồi bấm **Yes** để cài.
   - Nếu Windows SmartScreen chặn (file chưa có chữ ký số), bấm **More info → Run anyway**.
   - DeskPanel tự chép vào `C:\DeskPanel`, tự chạy ngầm mỗi khi bạn đăng nhập Windows, rồi mở trang cài đặt trong trình duyệt.
   - Lần đầu DeskPanel tự cài **PawnIO** để đọc nhiệt độ CPU (cần khởi động lại máy một lần) và tự tải **adb** của Google cho chế độ cáp USB.
2. Gỡ cài đặt: Windows **Settings → Apps → DeskPanel → Uninstall**.

## B. Trên điện thoại (làm một lần)

1. Vào **Cài đặt → Thông tin điện thoại**, chạm 7 lần vào **Số hiệu bản tạo** (Build number) để bật Tùy chọn nhà phát triển.
2. Vào **Tùy chọn nhà phát triển**:
   - Bật **Gỡ lỗi USB** (USB debugging).
   - Bật **Không khóa màn hình / Luôn bật màn hình khi sạc** (Stay awake).
3. Cắm cáp USB vào PC. Khi điện thoại hỏi **"Cho phép gỡ lỗi USB?"**, tích **Luôn cho phép từ máy tính này** rồi bấm **Cho phép**.
4. Mở **Chrome**, vào `http://localhost:8765`.
5. Bấm menu **⋮ → Thêm vào màn hình chính**. Từ lần sau, mở DeskPanel bằng biểu tượng này để chạy toàn màn hình.

## B2. Dùng Wi-Fi thay cho cáp (tùy chọn, từ bản 0.7)

Không cần bật gỡ lỗi USB. Điện thoại và PC phải cùng một mạng (cùng modem Wi-Fi, PC cắm dây LAN cũng được).

1. Trên PC mở `http://localhost:8765/settings.html` → thẻ **Chung** → tích **Cho điện thoại kết nối qua Wi-Fi**. DeskPanel tự chạy lại trong vài giây và hiện 2 mã QR.
2. **Cài app** (lần đầu): mở camera điện thoại, quét mã **"1. Cài app DeskPanel"**, tải file APK rồi mở ra cài. Android sẽ hỏi cho phép cài ứng dụng ngoài Play Store: bấm **Cài đặt → Cho phép từ nguồn này**. File APK cũng có trong mục Releases trên GitHub.
3. Mở app **DeskPanel** → **Quét mã QR (Wi-Fi)** → quét mã **"2. Mở app → Quét mã QR"**. Xong. App nhớ địa chỉ, lần sau mở là vào thẳng.
4. Nếu điện thoại không vào được: trên PC vào **Settings → Network & internet → Wi-Fi (hoặc Ethernet) → chọn mạng đang dùng → Private network**. DeskPanel chỉ mở cổng cho mạng Private, không mở trên mạng công cộng (quán cà phê, sân bay).

**iPhone / iPad** (không có app, dùng Safari):
1. iPhone bắt cùng Wi-Fi với PC, mở **Camera** quét mã **"2. Ghép nối"**, bấm vào đường link hiện ra.
2. Trong Safari bấm **Chia sẻ → Thêm vào MH chính** để có biểu tượng chạy toàn màn hình.
3. Vào **Cài đặt → Màn hình & Độ sáng → Tự động khóa → Không bao giờ** (và cắm sạc) để màn hình không tắt.
iPhone không dùng được cáp USB (cách nối cáp dựa vào adb, chỉ có trên Android).

Ghi chú:
- Ai có mã QR ghép nối đều bấm được các nút trên PC của bạn. Chỉ đưa cho người bạn tin; muốn thu hồi thì **Đổi mã ghép nối** (mọi máy phải quét lại).
- App giữ màn hình luôn sáng và chạy toàn màn hình. Nút **Back** mở menu: Tải lại, Đổi kết nối, Thoát.
- Mất kết nối (PC tắt, đổi mạng), app tự thử lại 3 giây một lần.
- App dùng được cả với cáp: chọn **Dùng cáp USB** (vẫn cần các bước ở mục B).
- Modem cấp lại địa chỉ khác cho PC thì quét lại mã QR. Muốn địa chỉ cố định, đặt "IP tĩnh/DHCP reservation" cho PC trong trang quản trị modem.
- Lỡ để người khác chụp được mã QR: bấm **Đổi mã ghép nối**, rồi quét lại trên điện thoại của bạn.

## C. Các trang trên điện thoại

Vuốt trái/phải để chuyển trang. Chấm ở đáy màn hình cho biết đang ở trang nào, chạm vào chấm để nhảy thẳng tới trang đó. Điện thoại nhớ trang bạn xem gần nhất.

Bộ trang mặc định gồm Tổng quan (đồng hồ, thời tiết, nhạc đang phát, 4 ô CPU/GPU/RAM/Mạng), Hệ thống, Ổ cứng và mỗi nhóm nút một trang. Bạn sửa lại toàn bộ được ở phần D.

- Thời tiết lấy từ Open-Meteo qua mạng của PC. Nếu Open-Meteo từ chối (ví dụ lỗi 429 do IP dùng chung), DeskPanel tự chuyển sang MET Norway, rồi wttr.in. Cả ba đều miễn phí, không cần tài khoản. Đổi thành phố ở thẻ **Chung** trong trang cài đặt, để trống nếu muốn tắt.
- Thẻ nhạc đọc bài đang phát trên Windows (Spotify, YouTube trên Chrome/Edge…).
- Widget **Theo dõi AI** (từ bản 0.9): đọc nhật ký Claude Code (`%USERPROFILE%\.claude\projects`) và Codex (`%USERPROFILE%\.codex\sessions`) trên PC, cập nhật mỗi 20 giây. Hiện token hôm nay, 7 ngày, khung 5 giờ của Claude Code, thanh giới hạn 5 giờ/tuần của Codex và phiên nào đang chạy. Số token của Claude Code là ước tính từ nhật ký, không phải con số chính thức của Anthropic. Widget hiện % giới hạn chính thức của gói (giống trang Mức sử dụng trên web), hỏi 3 phút một lần. Cách khuyên dùng (từ 0.12): cài tiện ích **DeskPanel AI** cho Chrome/Edge ở Cài đặt → Chung → **Theo dõi AI** (tải zip, giải nén, `chrome://extensions` → Developer mode → Load unpacked). Tiện ích dùng phiên claude.ai / chatgpt.com bạn đã đăng nhập sẵn trong trình duyệt, chỉ gửi số liệu về DeskPanel trên máy (localhost). Cách dự phòng: nút **Đăng nhập Claude** / **Đăng nhập ChatGPT** mở cửa sổ đăng nhập riêng của DeskPanel. Nếu PC đã đăng nhập Claude Code hoặc Codex thì không cần bước này. Giới hạn Claude dùng chung cho web, app và Claude Code. Chat thường của ChatGPT không tính vào hạn mức Codex. Nếu báo đăng nhập hết hạn, đăng nhập lại ở Cài đặt → Chung. Tắt ở Cài đặt → Chung.

## D. Trang cài đặt

Trên PC, mở `http://localhost:8765/settings.html` (hoặc bấm ⚙ trên điện thoại). Trang có 4 thẻ:

**1. Trang & widget**
- Cột trái: danh sách trang. Kéo để đổi thứ tự, bấm **+ Thêm trang** để tạo trang trống hoặc từ mẫu (Tổng quan, Hệ thống, Vòng đo, Ổ cứng, Làm việc + nút, hoặc một nhóm nút).
- Giữa: lưới 12 × 6 ô tượng trưng cho màn điện thoại. Kéo widget để di chuyển, kéo góc dưới phải để đổi kích thước. Widget không được chồng lên nhau (khung chuyển đỏ nếu chồng).
- Bên phải: danh mục widget (bấm để thêm vào chỗ trống, hoặc kéo thả vào lưới) và bảng thuộc tính của widget đang chọn: tiêu đề, màu, chỉ số (với Vòng đo), nhóm nút, vị trí, nhân bản, chuyển sang trang khác, xóa. Khi không chọn widget nào, bảng này cho đổi tên, biểu tượng, ẩn, nhân bản hoặc xóa trang.
- Bên dưới có khung **xem trước** chạy số liệu thật, cập nhật ngay khi bạn sửa.
- Phím tắt: mũi tên dịch widget, Shift + mũi tên đổi kích thước, Delete xóa, Ctrl+Z / Ctrl+Y hoàn tác / làm lại, Ctrl+S lưu.

**2. Nút bấm**: thư viện nút theo nhóm. Thêm, sửa, thử, xóa, đổi thứ tự, đổi tên nhóm. Nhóm nào chưa nằm trên trang nào sẽ có nút **Tạo trang cho nhóm**.
   - **+ Thêm phần mềm** (từ 0.8): liệt kê phần mềm đã cài trong Start Menu, tìm và tích chọn nhiều cái một lần. Nút tạo ra dùng icon thật của phần mềm, tự vào nhóm "Phần mềm" và tự có trang riêng. Bấm **Lưu** để điện thoại cập nhật. Với nút tự thêm bằng tay, tích **Dùng icon thật của chương trình** trong hộp sửa nút.

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

**Cách 2: tự kiểm tra trên GitHub.** Điền `kthtuan/DeskPanel` vào ô ở cùng mục rồi bấm **Lưu**. DeskPanel kiểm tra 6 giờ một lần. Khi có bản mới, nút **Cài bản …** hiện ra.

Nếu repo để **private**, DeskPanel cần một token chỉ đọc:
1. Mở https://github.com/settings/personal-access-tokens/new (Fine-grained token).
2. Đặt tên (ví dụ `DeskPanel PC`), chọn thời hạn (tối đa 1 năm).
3. **Repository access** → **Only select repositories** → chọn `DeskPanel`.
4. **Permissions** → **Repository permissions** → **Contents** → **Read-only**. Không cấp quyền nào khác.
5. Bấm **Generate token**, chép token (bắt đầu bằng `github_pat_`), dán vào ô **Token GitHub** rồi bấm **Lưu token**.

Token được mã hóa theo tài khoản Windows (DPAPI) trong file `update-token.bin`, không nằm trong `config.json` và không gửi ra điện thoại. Hết hạn thì tạo token mới và dán lại.

**Cách 3: chạy file cài mới.** Tải `DeskPanel-Setup-….exe` bản mới, nhấp đúp và bấm **Yes**. File cài nhận ra bản đang có và cập nhật tại chỗ.

Cả ba cách đều giữ nguyên `config.json` (nút, trang, giao diện), `platform-tools` và đăng nhập AI. Nhật ký cập nhật nằm ở `update.log` trong thư mục cài.

## F. Khi có trục trặc

- **Điện thoại báo "Mất kết nối"**: mở trang cài đặt trên PC, xem dòng **Điện thoại**. Dòng này cho biết lỗi nằm ở đâu: chưa thấy `adb.exe`, chưa cho phép gỡ lỗi USB, hay chưa cắm cáp.
- **Thời tiết báo lỗi**: dòng chữ nhỏ dưới thông báo cho biết lý do (mạng, DNS, tường lửa, chứng chỉ HTTPS…). Trang cài đặt, thẻ **Chung**, dòng **Thời tiết** cũng hiện lý do. Nếu tên thành phố không tìm thấy, nhập tọa độ dạng `10.78,106.70`. DeskPanel tự thử lại sau 1, 2, 5 rồi 10 phút.
- **Nhiệt độ CPU hiện `--`**: chưa cài PawnIO, hoặc chưa khởi động lại máy sau khi cài.
- **Muốn xem toàn bộ cảm biến đọc được**: mở `http://localhost:8765/api/sensors`.
- Nhật ký lỗi nằm ở `C:\DeskPanel\deskpanel.log`.
- **App báo "Chưa kết nối được"** (Wi-Fi): kiểm tra PC và điện thoại cùng mạng, mạng trên PC đặt là Private, và địa chỉ trong app trùng địa chỉ dưới mã QR trên PC. PC có nhiều card mạng (VPN, máy ảo) thì trang cài đặt liệt kê hết, thử địa chỉ cùng dải với điện thoại.
- **App báo "chưa nhận điện thoại này"**: mã ghép nối đã đổi, quét lại mã QR.
- **Trình duyệt báo `localhost refused to connect` / `ERR_CONNECTION_REFUSED`**: DeskPanel không chạy. Lần đầu có thể mất 10–30 giây, đợi rồi tải lại. Nếu vẫn lỗi: mở Task Manager xem có `DeskPanel.exe` không; vào Windows Security → Protection history xem có bị chặn không; chạy lại `DeskPanel-Setup-….exe` và bấm **Yes** khi Windows hỏi quyền admin; gửi file `deskpanel.log` trong thư mục DeskPanel nếu có.
- **Gỡ cài đặt**: Windows Settings → Apps → DeskPanel → Uninstall (gỡ luôn tác vụ tự chạy và quy tắc tường lửa).

## Về an toàn

- Mặc định DeskPanel chỉ nhận kết nối từ chính PC (localhost). Điện thoại vào được là nhờ `adb reverse` qua cáp USB.
- Khi bật Wi-Fi: thiết bị trong mạng phải có mã ghép nối (24 ký tự ngẫu nhiên, nằm trong mã QR, lưu ở `lan-key.txt` trên PC). Ghép xong, mỗi thiết bị có mã riêng; xem và thu hồi trong Cài đặt → Chung → Wi-Fi. Từ Wi-Fi chỉ xem bảng và bấm nút; trang cài đặt, đổi cấu hình và cập nhật chỉ làm được trên chính PC. Cổng chỉ mở trên Windows Firewall cho mạng Private. Kết nối là `http` thường (không mã hóa), nên chỉ bật Wi-Fi ở mạng nhà/văn phòng bạn tin cậy.
- Mọi lệnh được lưu trong `config.json` trên PC. Điện thoại chỉ gửi mã nút.
- Trang web lạ mở trên PC không gọi được DeskPanel: mọi yêu cầu thay đổi phải kèm header riêng và đúng tên máy `localhost`.
- DeskPanel chạy với quyền admin vì cần đọc nhiệt độ và SMART. Các chương trình nó mở (không kèm tham số) vẫn chạy với quyền thường.

## Ghi công

Giao diện từ bản 0.2 trở đi (bố cục trang Tổng quan và Hệ thống, đồ thị nhỏ, chấm trang, nút nền tối viền màu, trình xếp widget dạng lưới) lấy cảm hứng từ [PhoneBox](https://github.com/ersinkoc/phonebox) (giấy phép MIT, © 2026 Ersin KOÇ). DeskPanel không chép mã của PhoneBox; toàn bộ được viết lại.
