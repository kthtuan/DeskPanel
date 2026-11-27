# Bảo mật và quyền riêng tư của DeskPanel

DeskPanel chạy trên PC của bạn và chỉ nói chuyện với điện thoại của bạn. Không có máy chủ DeskPanel nào cả: không tài khoản, không thu thập số liệu sử dụng, không gửi gì cho người viết phần mềm.

Trang này liệt kê đúng những gì DeskPanel đọc, lưu và gửi đi, để bạn tự kiểm tra.

## Tóm tắt

- DeskPanel **không gửi dữ liệu của bạn cho người viết phần mềm** và không có quảng cáo hay theo dõi.
- Mã đăng nhập Claude, ChatGPT, GitHub **chỉ nằm trên PC**, không bao giờ gửi ra điện thoại.
- Điện thoại chỉ nhận số liệu để hiển thị và chỉ bấm được các nút bạn đã tạo; không mở được trang cài đặt.
- Mặc định DeskPanel chỉ nghe trên `localhost` (cáp USB). Chế độ Wi-Fi phải tự bật và cần mã ghép nối.

## DeskPanel đọc gì trên PC

| Đọc | Để làm gì |
|---|---|
| Cảm biến phần cứng (CPU, GPU, RAM, nhiệt độ, SMART ổ cứng) | Hiện trên điện thoại. Cần quyền admin, vì vậy DeskPanel chạy bằng quyền admin. |
| Danh sách lối tắt trong Start Menu và icon | Khi bạn bấm **+ Thêm phần mềm** trong Cài đặt. |
| Bài nhạc đang phát (Windows Media) | Thẻ nhạc. |
| Nhật ký Claude Code (`%USERPROFILE%\.claude\projects`) và Codex (`%USERPROFILE%\.codex\sessions`) | Widget AI: chỉ lấy số token, thời điểm và tên thư mục dự án. Nội dung trò chuyện **không được lưu và không gửi đi đâu**. |
| Mã đăng nhập Claude Code (`.claude\.credentials.json`) và Codex (`.codex\auth.json`) | Widget AI: hỏi % giới hạn của gói. Chỉ đọc, không sửa, không tự làm mới. Tắt được ở Cài đặt → Chung → Theo dõi AI. |

## DeskPanel kết nối tới đâu

| Địa chỉ | Khi nào | Gửi gì |
|---|---|---|
| `api.open-meteo.com`, `api.met.no`, `wttr.in` | Có đặt thành phố thời tiết | Tên thành phố hoặc tọa độ bạn nhập. |
| `api.github.com` | Có điền repo cập nhật | Hỏi bản mới nhất; token GitHub nếu bạn dán (repo riêng tư). |
| `api.anthropic.com` | Widget AI, PC đã đăng nhập Claude Code | Mã đăng nhập Claude Code, để hỏi % giới hạn. |
| `chatgpt.com` | Widget AI, PC đã đăng nhập Codex hoặc bạn đăng nhập ChatGPT trong DeskPanel | Mã đăng nhập, để hỏi % giới hạn. |
| `claude.ai` | Bạn đăng nhập Claude trong cửa sổ của DeskPanel | Phiên đăng nhập của cửa sổ đó, để hỏi % giới hạn. |
| `dl.google.com` | Lần đầu, nếu chưa có `adb` | Tải Android Platform Tools (chế độ cáp USB). |
| `winget` (Microsoft) | Lúc cài, nếu chưa có PawnIO | Cài driver PawnIO để đọc nhiệt độ CPU. |

Ngoài các địa chỉ trên, DeskPanel không gọi ra Internet.

## DeskPanel lưu gì (trong thư mục cài, mặc định `C:\DeskPanel`)

| File | Nội dung |
|---|---|
| `config.json` | Nút, trang, giao diện, thành phố thời tiết, ngôn ngữ. Không chứa mật khẩu hay mã đăng nhập. |
| `lan-key.txt` | Mã ghép nối Wi-Fi (trong mã QR). Đổi được bằng nút **Đổi mã ghép nối**. |
| `lan-devices.json` | Danh sách điện thoại đã ghép qua Wi-Fi: tên, địa chỉ IP, lần cuối kết nối và mã băm SHA-256 của mã riêng từng máy (không lưu mã gốc). |
| `update-token.bin` | Token GitHub (nếu có), mã hóa theo tài khoản Windows (DPAPI). |
| `webview\` | Phiên đăng nhập claude.ai / chatgpt.com nếu bạn đăng nhập trong cửa sổ của DeskPanel. Xóa bằng nút **Đăng xuất**. |
| `deskpanel.log` | Nhật ký lỗi. Không ghi mã đăng nhập. |

Gỡ DeskPanel (Windows Settings → Apps → DeskPanel → Uninstall) xóa tất cả các file trên, tác vụ tự chạy và quy tắc tường lửa.

## Điện thoại và mạng nội bộ

- **Cáp USB (mặc định):** DeskPanel chỉ nghe trên `localhost`; điện thoại vào qua `adb reverse`. Không mở cổng nào ra mạng.
- **Wi-Fi (tự bật):** mở cổng cho mạng Private/Domain trên Windows Firewall. Thiết bị phải ghép nối bằng mã trong QR; sau đó mỗi thiết bị nhận một mã riêng và có thể bị thu hồi riêng trong trang cài đặt. Kết nối Wi-Fi là `http` thường, không mã hóa. Từ mạng LAN chỉ xem bảng và bấm nút; không mở được trang cài đặt, không cập nhật, không đổi cấu hình.
- Trang web lạ trên PC không gọi được vào DeskPanel: DeskPanel chặn tên miền lạ (DNS rebinding) và yêu cầu header riêng cho mọi lệnh thay đổi.
- Nút bấm chỉ chạy đúng những lệnh bạn đã tạo trong Cài đặt. Chương trình DeskPanel mở ra chạy bằng quyền thường, không mang quyền admin.

## Tiện ích Chrome / Edge "DeskPanel AI"

- Quyền xin: `claude.ai`, `chatgpt.com`, `localhost`, cùng `alarms` và `storage`.
- 3 phút một lần dùng phiên đăng nhập sẵn có của trình duyệt để hỏi % giới hạn, rồi gửi **chỉ về DeskPanel trên chính máy đó** (`http://localhost`).
- Không đọc trò chuyện, không đọc trang khác, không gửi đi nơi nào khác. Mã nguồn nằm trong thư mục `extension/`, ngắn, đọc được.

## Giới hạn cần biết

- Widget AI dùng các địa chỉ không có tài liệu chính thức của Anthropic và OpenAI (giống các công cụ theo dõi mã nguồn mở khác). Nếu họ thay đổi, widget có thể ngừng hiện %.
- DeskPanel chạy bằng quyền admin để đọc cảm biến. Chỉ cài bản tải từ trang Releases chính thức.
- File cài chưa có chữ ký số, nên Windows SmartScreen có thể cảnh báo.

## Báo lỗi bảo mật

Nếu bạn tìm thấy lỗ hổng, **đừng đăng công khai**. Hãy báo riêng cho chủ repo trên GitHub (`kthtuan`), qua mục **Security → Report a vulnerability** nếu repo đang công khai, hoặc nhắn trực tiếp. Mô tả cách tái hiện và phiên bản DeskPanel. Lỗi sẽ được sửa ở bản kế tiếp và ghi trong `CHANGELOG.md`.

<!-- en -->

# DeskPanel security and privacy

DeskPanel runs on your PC and only talks to your phone. There is no DeskPanel server: no account, no usage analytics, nothing sent to the author.

This page lists exactly what DeskPanel reads, stores and sends, so you can check for yourself.

## Summary

- DeskPanel **does not send your data to its author** and has no ads or tracking.
- Claude, ChatGPT and GitHub sign-in tokens **stay on the PC** and are never sent to the phone.
- The phone only receives numbers to display and can only press the buttons you created; it cannot open the settings page.
- By default DeskPanel only listens on `localhost` (USB cable). Wi-Fi mode must be turned on by you and needs a pairing key.

## What DeskPanel reads on the PC

| Reads | Why |
|---|---|
| Hardware sensors (CPU, GPU, RAM, temperatures, disk SMART) | Shown on the phone. This needs admin rights, so DeskPanel runs as admin. |
| Start Menu shortcuts and icons | When you click **+ Add apps** in Settings. |
| The song playing (Windows Media) | Music card. |
| Claude Code logs (`%USERPROFILE%\.claude\projects`) and Codex logs (`%USERPROFILE%\.codex\sessions`) | AI widget: only token counts, times and project folder names. Conversation content is **not stored and not sent anywhere**. |
| Claude Code (`.claude\.credentials.json`) and Codex (`.codex\auth.json`) sign-in tokens | AI widget: to ask for the plan's limit %. Read only, never changed or refreshed. Can be turned off in Settings → General → AI usage. |

## Where DeskPanel connects

| Address | When | What is sent |
|---|---|---|
| `api.open-meteo.com`, `api.met.no`, `wttr.in` | A weather city is set | The city name or coordinates you entered. |
| `api.github.com` | An update repo is set | A request for the latest release; your GitHub token if you pasted one (private repo). |
| `api.anthropic.com` | AI widget, Claude Code signed in on the PC | The Claude Code token, to ask for the limit %. |
| `chatgpt.com` | AI widget, Codex signed in on the PC, or you signed in to ChatGPT in DeskPanel | The sign-in token, to ask for the limit %. |
| `claude.ai` | You signed in to Claude in a DeskPanel window | That window's session, to ask for the limit %. |
| `dl.google.com` | First run, if `adb` is missing | Downloads Android Platform Tools (USB mode). |
| `winget` (Microsoft) | During setup, if PawnIO is missing | Installs the PawnIO driver to read CPU temperature. |

Apart from these, DeskPanel does not connect to the Internet.

## What DeskPanel stores (in the install folder, `C:\DeskPanel` by default)

| File | Contents |
|---|---|
| `config.json` | Buttons, pages, appearance, weather city, language. No passwords or tokens. |
| `lan-key.txt` | The Wi-Fi pairing key (in the QR code). Change it with **Change pairing key**. |
| `lan-devices.json` | Phones paired over Wi-Fi: name, IP address, last seen and a SHA-256 hash of each device key (the key itself is not stored). |
| `update-token.bin` | GitHub token (if any), encrypted for your Windows account (DPAPI). |
| `webview\` | claude.ai / chatgpt.com sessions if you signed in through a DeskPanel window. Removed with **Sign out**. |
| `deskpanel.log` | Error log. Never contains tokens. |

Uninstalling DeskPanel (Windows Settings → Apps → DeskPanel → Uninstall) removes all of these files, the startup task and the firewall rule.

## Phone and local network

- **USB cable (default):** DeskPanel only listens on `localhost`; the phone connects through `adb reverse`. No port is opened to the network.
- **Wi-Fi (opt-in):** opens a port for Private/Domain networks in Windows Firewall. Devices pair with the key in the QR code; each device then gets its own key, which can be revoked on its own in settings. The Wi-Fi connection is plain `http`, not encrypted. From the LAN a device can only view the panel and press buttons; it cannot open settings, update or change the configuration.
- Unknown web pages on the PC cannot call DeskPanel: it rejects unknown host names (DNS rebinding) and requires a custom header for every request that changes something.
- Buttons only run the commands you created in Settings. Programs DeskPanel starts run with normal rights, not admin.

## "DeskPanel AI" Chrome / Edge extension

- Permissions: `claude.ai`, `chatgpt.com`, `localhost`, plus `alarms` and `storage`.
- Every 3 minutes it uses the browser's existing session to ask for the limit %, then sends it **only to DeskPanel on the same PC** (`http://localhost`).
- It does not read conversations or other pages, and sends nothing anywhere else. The source is in the `extension/` folder, short and readable.

## Limitations

- The AI widget uses endpoints that Anthropic and OpenAI do not officially document (like other open-source trackers). If they change, the widget may stop showing %.
- DeskPanel runs as admin to read sensors. Only install builds from the official Releases page.
- The installer is not code-signed yet, so Windows SmartScreen may warn you.

## Reporting a vulnerability

If you find a vulnerability, **do not post it publicly**. Report it privately to the repo owner on GitHub (`kthtuan`), through **Security → Report a vulnerability** if the repo is public, or by direct message. Describe how to reproduce it and the DeskPanel version. It will be fixed in the next release and noted in `CHANGELOG.md`.
