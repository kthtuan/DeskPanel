/* DeskPanel: song ngu Viet / Anh.
   Van ban goc viet bang tieng Viet. Khi chon English, T() va bo dich DOM thay bang ban dich ben duoi.
   - T('Chuoi {0}', x): dung trong JS cho chuoi ghep so lieu.
   - Chu tinh trong HTML, chuoi may chu (PC) tra ve, confirm/prompt/alert: dich tu dong khi hien ra.
   - Khoa co dau # la mau: moi # la mot so (vd 12, 3.5, 14:30). */
(function(){
  'use strict';
  var KEY = 'dp.lang';
  function stored(){ try { return localStorage.getItem(KEY); } catch(e) { return null; } }
  var L = stored() === 'en' ? 'en' : 'vi';

  var D = {
    // ---------- Chung ----------
    'Cài đặt': 'Settings',
    'DeskPanel – Cài đặt': 'DeskPanel – Settings',
    'Lỗi {0}': 'Error {0}',
    'Đang tải…': 'Loading…',
    'Lưu': 'Save',
    'Hủy': 'Cancel',
    'Xóa': 'Delete',
    'Sửa': 'Edit',
    'Thử': 'Test',
    'Thêm': 'Add',
    'Tìm': 'Search',
    'Xong': 'Done',
    'Mặc định': 'Default',
    'Chi tiết': 'Details',
    'vừa xong': 'just now',
    '{0} phút trước': '{0} min ago',
    '{0} phút': '{0} min',
    'Chung': 'General',

    // ---------- Bang dieu khien (dien thoai) ----------
    '⚠ Mất kết nối với máy tính': '⚠ Lost connection to the PC',
    'Kiểm tra cáp USB và DeskPanel trên PC. Đang thử lại…': 'Check the USB cable and DeskPanel on the PC. Retrying…',
    'Bật {0} ngày {1} giờ': 'Up {0}d {1}h',
    'Bật {0} giờ {1} phút': 'Up {0}h {1}m',
    'Tốt': 'Good', 'Chú ý': 'Caution', 'Hỏng': 'Failing',
    'Chủ nhật': 'Sunday', 'Thứ Hai': 'Monday', 'Thứ Ba': 'Tuesday', 'Thứ Tư': 'Wednesday', 'Thứ Năm': 'Thursday', 'Thứ Sáu': 'Friday', 'Thứ Bảy': 'Saturday',
    'CN': 'Sun', 'T2': 'Mon', 'T3': 'Tue', 'T4': 'Wed', 'T5': 'Thu', 'T6': 'Fri', 'T7': 'Sat',
    '{0}, {1} tháng {2}': '{0}, {3} {1}',
    ' · Ổ cứng: {0}': ' · Disks: {0}',
    'Cảm giác {0}°': 'Feels {0}°',
    'làm mới {0}': 'resets {0}',
    'làm mới {0} (còn {1})': 'resets {0} (in {1})',
    '{0}g{1}': '{0}h{1}',
    '{0}n {1}g': '{0}d {1}h',
    '{0}g {1}': '{0}h {1}',
    'còn {0}': 'in {0}',
    'Đã hết': 'Used up',
    'Còn nhiều': 'Plenty left',
    'Theo nhịp này: hết sau {0}': 'At this pace: out in {0}',
    'Đúng nhịp': 'On pace',
    'Đang chạy': 'Running',
    'Nghỉ': 'Idle',
    'Hôm nay <b></b> token · 7 ngày <b></b>': 'Today <b></b> tokens · 7 days <b></b>',
    '{0} lượt': '{0} turns',
    'Khung 5 giờ: {0} token': '5-hour block: {0} tokens',
    'Chưa có dữ liệu AI. Trên PC mở ⚙ Cài đặt → Chung → Đăng nhập Claude hoặc ChatGPT.': 'No AI data yet. On the PC open ⚙ Settings → General → Sign in to Claude or ChatGPT.',
    'Đang làm việc': 'Working',
    'Thời tiết': 'Weather',
    'Chưa bật thời tiết (⚙ → Chung)': 'Weather is off (⚙ → General)',
    'Âm nhạc': 'Music',
    'Không có gì đang phát': 'Nothing playing',
    'Mở Spotify, YouTube… trên PC': 'Play Spotify, YouTube… on the PC',
    'Mạng': 'Network',
    'Nhiệt CPU': 'CPU temp',
    'Nhiệt GPU': 'GPU temp',
    'Bộ xử lý': 'Processor',
    'Đồ họa': 'Graphics',
    'Bộ nhớ': 'Memory',
    'Sức khỏe: {0}': 'Health: {0}',
    'Nhiệt độ': 'Temperature',
    'Tuổi thọ': 'Life left',
    'Giờ chạy': 'Power-on hours',
    'Đã ghi': 'Written',
    'Đã dùng': 'Used',
    'Không đọc được ổ cứng': 'Cannot read the disks',
    'Ổ cứng': 'Disks',
    'Tất cả ổ đều tốt': 'All disks are healthy',
    'Có ổ sắp hỏng!': 'A disk is failing!',
    'Có ổ cần chú ý': 'A disk needs attention',
    'Nhóm "{0}" chưa có nút': 'Group "{0}" has no buttons',
    'Chưa chọn nút': 'No button selected',
    'Trang trống. Thêm widget trong ⚙ Cài đặt.': 'Empty page. Add widgets in ⚙ Settings.',

    // ---------- Cai dat: dau trang, tab ----------
    'Về màn hình chính': 'Back to the dashboard',
    'Trang & widget': 'Pages & widgets',
    'Nút bấm': 'Buttons',
    'Giao diện': 'Appearance',
    'Bỏ thay đổi': 'Discard changes',
    'Có thay đổi chưa lưu': 'Unsaved changes',
    'Đã lưu hết': 'All saved',
    'Phiên bản': 'Version',
    'Phiên bản:': 'Version:',
    ' (thử)': ' (test)',
    ' (chế độ thử, không phải Windows)': ' (test mode, not Windows)',
    'Điện thoại': 'Phone',
    'Điện thoại:': 'Phone:',
    'Cảm biến': 'Sensors',
    'Cảm biến:': 'Sensors:',
    'Thời tiết:': 'Weather:',
    'Đọc bình thường': 'Reading normally',
    'Điện thoại đã nối': 'Phone connected',
    'Chưa nối điện thoại': 'No phone connected',
    'Điện thoại đã nối qua Wi-Fi ({0})': 'Phone connected over Wi-Fi ({0})',
    'Wi-Fi: {0} thiết bị đang mở bảng': 'Wi-Fi: {0} device(s) showing the panel',

    // ---------- Cai dat: trang & widget ----------
    '+ Thêm trang': '+ Add page',
    'Kéo để đổi thứ tự. Thứ tự này là thứ tự khi vuốt trên điện thoại.': 'Drag to reorder. This is the order you swipe through on the phone.',
    'Dồn lên': 'Compact',
    'Xếp lại widget lấp chỗ trống từ trên xuống': 'Move widgets up to fill empty space',
    'Xóa hết widget': 'Remove all widgets',
    'Kéo widget từ danh sách bên phải thả vào đây,': 'Drag a widget from the list on the right and drop it here,',
    'hoặc bấm vào widget để thêm vào chỗ trống đầu tiên.': 'or click a widget to add it to the first free spot.',
    'Kéo để di chuyển, kéo góc dưới phải để đổi kích thước. Lưới 12 × 6 ô. Phím mũi tên dịch widget đang chọn, Delete để xóa.': 'Drag to move, drag the bottom-right corner to resize. Grid of 12 × 6 cells. Arrow keys move the selected widget, Delete removes it.',
    'Xem trước trên điện thoại': 'Phone preview',
    'Số liệu thật từ PC, cập nhật khi bạn sửa': 'Live data from the PC, updates as you edit',
    'Thêm widget': 'Add widget',
    'Tìm widget…': 'Find a widget…',
    'Đồng hồ': 'Clock',
    'Giờ, ngày, thời tiết': 'Time, date, weather',
    'Nhiệt độ, cao/thấp': 'Temperature, high/low',
    'Theo dõi AI': 'AI usage',
    'Claude Code, Codex: token, giới hạn': 'Claude Code, Codex: tokens, limits',
    'Nhạc đang phát': 'Now playing',
    'Tải + đồ thị': 'Load + chart',
    'Ô số liệu': 'Stat tile',
    'Đã dùng + đồ thị': 'Used + chart',
    'Tải xuống/lên': 'Download/upload',
    'Vòng đo': 'Gauge',
    'Một chỉ số tự chọn': 'One metric of your choice',
    'Bộ xử lý (chi tiết)': 'Processor (details)',
    'Tải, nhiệt, công suất': 'Load, temperature, power',
    'Đồ họa (chi tiết)': 'Graphics (details)',
    'Tải, nhiệt, VRAM': 'Load, temperature, VRAM',
    'RAM, VRAM, các ổ': 'RAM, VRAM, drives',
    'Mạng (chi tiết)': 'Network (details)',
    'Đồ thị lên/xuống': 'Up/down chart',
    'Thẻ SMART từng ổ': 'SMART card per disk',
    'Tóm tắt ổ cứng': 'Disk summary',
    'Sức khỏe chung': 'Overall health',
    'Nhóm nút': 'Button group',
    'Cả một nhóm nút': 'A whole button group',
    'Một nút': 'Single button',
    'Nút lẻ, to tùy ý': 'One button, any size',
    'Tải CPU': 'CPU load',
    'Nhiệt độ CPU': 'CPU temperature',
    'Tải GPU': 'GPU load',
    'Nhiệt độ GPU': 'GPU temperature',
    'RAM đã dùng': 'RAM used',
    'VRAM đã dùng': 'VRAM used',
    'Trang đã kín chỗ. Thu nhỏ hoặc xóa bớt widget.': 'The page is full. Shrink or remove some widgets.',
    'ẩn': 'hidden',
    'Nhóm: {0}': 'Group: {0}',
    'Chưa chọn nhóm': 'No group selected',
    'Không kèm thời tiết': 'Without weather',
    '{0} widget · dùng {1}/{2} ô': '{0} widgets · {1}/{2} cells used',
    ' · đang ẩn': ' · hidden',
    'Chỗ đó đã có widget khác': 'Another widget is already there',
    'Bấm để thêm, hoặc kéo thả vào lưới ({0}×{1})': 'Click to add, or drag onto the grid ({0}×{1})',
    'Trang này': 'This page',
    'Biểu tượng và tên': 'Icon and name',
    'Ẩn trang này trên điện thoại': 'Hide this page on the phone',
    'Nhân bản trang': 'Duplicate page',
    ' (bản sao)': ' (copy)',
    'Xóa trang': 'Delete page',
    'Cần giữ lại ít nhất một trang': 'Keep at least one page',
    'Xóa trang "{0}"?': 'Delete page "{0}"?',
    'Bấm vào một widget trên lưới để chỉnh nó.': 'Click a widget on the grid to edit it.',
    'Bỏ chọn (Esc)': 'Deselect (Esc)',
    'Chỉ số': 'Metric',
    '{0} ({1} nút)': '{0} ({1} buttons)',
    'Sửa danh sách nút ở thẻ "Nút bấm".': 'Edit the button list in the "Buttons" tab.',
    'Nút': 'Button',
    'Hiện dưới đồng hồ': 'Show under the clock',
    'Không hiện': 'Hidden',
    'Tiêu đề (để trống = mặc định)': 'Title (empty = default)',
    'Màu': 'Color',
    '(đang dùng màu mặc định)': '(using the default color)',
    'Cột': 'Column', 'Hàng': 'Row', 'Rộng': 'Width', 'Cao': 'Height',
    'Không đặt được: ra ngoài lưới hoặc chồng lên widget khác': 'Cannot place it: outside the grid or overlapping another widget',
    'Vị trí và kích thước (ô)': 'Position and size (cells)',
    'Nhân bản': 'Duplicate',
    'Lên trước': 'Bring forward',
    'Ra sau': 'Send back',
    'Trang đã kín chỗ': 'The page is full',
    'Chuyển sang trang…': 'Move to page…',
    'Trang "{0}" đã kín chỗ': 'Page "{0}" is full',
    'Xóa widget': 'Remove widget',
    'Xóa hết widget trên trang "{0}"?': 'Remove all widgets on page "{0}"?',
    'Trang': 'Page',
    'Trang trống': 'Blank page',
    'Tự kéo widget vào': 'Drag widgets in yourself',
    'Tổng quan': 'Overview',
    'Đồng hồ, nhạc, 4 ô số liệu': 'Clock, music, 4 stat tiles',
    'Hệ thống': 'System',
    'CPU, GPU, bộ nhớ, mạng chi tiết': 'CPU, GPU, memory, network details',
    '6 vòng đo nhiệt độ và tải': '6 gauges for temperature and load',
    'Thẻ sức khỏe từng ổ': 'Health card per disk',
    'Theo dõi Claude Code, Codex + số liệu máy': 'Claude Code, Codex usage + PC stats',
    'Làm việc + nút': 'Work + buttons',
    'Số liệu nhỏ ở trên, nhóm nút ở dưới': 'Small stats on top, buttons below',
    '🎛️ Nút: {0}': '🎛️ Buttons: {0}',
    'Cả nhóm nút "{0}" chiếm toàn trang': 'Button group "{0}" fills the page',
    'Thêm trang': 'Add page',

    // ---------- Cai dat: nut bam ----------
    'Thư viện nút. Mỗi nhóm hiện trên điện thoại qua widget': 'Button library. Each group shows on the phone through the',
    '; từng nút lẻ có thể đặt riêng bằng widget': 'widget; a single button can be placed on its own with the',
    '. Trong AutoCAD, dấu cách tương đương Enter, nên lệnh': 'widget. In AutoCAD a space works like Enter, so the command',
    '= ZOOM rồi Extents.': '= ZOOM then Extents.',
    '+ Thêm phần mềm': '+ Add apps',
    '+ Thêm nút': '+ Add button',
    '{0} nút': '{0} buttons',
    'Chưa hiện trên trang nào': 'Not shown on any page',
    'Tạo trang cho nhóm': 'Create a page for this group',
    'Đổi tên nhóm': 'Rename group',
    'Tên mới cho nhóm "{0}"': 'New name for group "{0}"',
    '+ Nút': '+ Button',
    'Xóa nút "{0}"?': 'Delete button "{0}"?',
    '\nWidget "Một nút" đang dùng nút này ({0}) cũng bị xóa.': '\nThe "Single button" widget using it ({0}) will also be removed.',
    'Hãy Lưu trước khi thử nút này': 'Save before testing this button',
    'Đã chạy "{0}"': 'Ran "{0}"',
    'Thêm nút': 'Add button',
    'Sửa nút': 'Edit button',
    'Tên hiển thị': 'Label',
    'Nhóm': 'Group',
    'Biểu tượng (emoji)': 'Icon (emoji)',
    'Loại': 'Type',
    'Lệnh AutoCAD': 'AutoCAD command',
    'Mở chương trình': 'Open a program',
    'Mở trang web / thư mục': 'Open a website / folder',
    'Tổ hợp phím': 'Keyboard shortcut',
    'Gõ chữ': 'Type text',
    'Nội dung': 'Value',
    'Tham số (không bắt buộc)': 'Arguments (optional)',
    'Để trống thì chương trình mở với quyền thường. Có tham số thì chương trình sẽ chạy với quyền admin.': 'Leave empty to open the program with normal rights. With arguments the program runs as admin.',
    'Dùng icon thật của chương trình (thay cho emoji)': 'Use the program\'s real icon (instead of the emoji)',
    'Đưa chương trình này lên trước khi gửi (tên tiến trình, không bắt buộc)': 'Bring this program to the front before sending (process name, optional)',
    'Ví dụ': 'For example',
    'cho AutoCAD,': 'for AutoCAD,',
    '. Để trống thì gửi vào cửa sổ đang mở.': '. Leave empty to send to the active window.',
    'Nhấn Enter sau khi gõ': 'Press Enter after typing',
    'Chương trình': 'Program',
    'Web/thư mục': 'Web/folder',
    'Phím': 'Keys',
    'Gõ như trên dòng lệnh, ví dụ L, TR, O, DLI hoặc Z E. DeskPanel sẽ đưa AutoCAD lên trước, bấm Esc 2 lần, gõ lệnh rồi Enter.': 'Type it as on the command line, e.g. L, TR, O, DLI or Z E. DeskPanel brings AutoCAD to the front, presses Esc twice, types the command and presses Enter.',
    'Đường dẫn chương trình': 'Program path',
    'Ví dụ %SystemRoot%\\System32\\calc.exe hoặc C:\\Program Files\\Autodesk\\AutoCAD 2024\\acad.exe': 'E.g. %SystemRoot%\\System32\\calc.exe or C:\\Program Files\\Autodesk\\AutoCAD 2024\\acad.exe',
    'Địa chỉ': 'Address',
    'Ví dụ https://google.com hoặc D:\\BanVe': 'E.g. https://google.com or D:\\Drawings',
    'Ví dụ Ctrl+S, Win+Shift+S, Alt+Tab, F5, VolumeUp, MediaPlayPause. Nhiều tổ hợp nối tiếp: Ctrl+K, Ctrl+C': 'E.g. Ctrl+S, Win+Shift+S, Alt+Tab, F5, VolumeUp, MediaPlayPause. Several in a row: Ctrl+K, Ctrl+C',
    'Chữ cần gõ': 'Text to type',
    'Gõ vào cửa sổ đang mở (hoặc chương trình ở ô bên dưới).': 'Typed into the active window (or the program in the box below).',
    'acad,acadlt (mặc định)': 'acad,acadlt (default)',
    'Thêm phần mềm': 'Add apps',
    'Danh sách lấy từ Start Menu trên PC. Tích các phần mềm muốn mở từ điện thoại; nút sẽ hiện icon thật của phần mềm.': 'The list comes from the Start Menu on the PC. Tick the apps you want to open from the phone; each button shows the app\'s real icon.',
    'Cho vào nhóm': 'Add to group',
    'Không tìm thấy': 'Nothing found',
    'Không đọc được Start Menu': 'Cannot read the Start Menu',
    'đã có': 'already added',
    'Đã chọn {0}': '{0} selected',
    'Thêm {0} phần mềm': 'Add {0} apps',
    'Phần mềm': 'Apps',
    'Đã thêm {0} phần mềm vào nhóm "{1}". Bấm Lưu để điện thoại cập nhật.': 'Added {0} apps to group "{1}". Click Save to update the phone.',
    // Nut mac dinh
    'Máy tính': 'Calculator', 'Thư mục': 'Folders', 'Chụp MH': 'Screenshot', 'Màn hình nền': 'Desktop',
    'Phát/Dừng': 'Play/Pause', 'Giảm âm': 'Vol −', 'Tăng âm': 'Vol +', 'Tắt tiếng': 'Mute', 'Hoàn tác': 'Undo',

    // ---------- Cai dat: giao dien ----------
    'Màu nhấn': 'Accent color',
    'Nền': 'Background',
    'Kiểu thẻ': 'Card style',
    'Bo góc': 'Corner radius',
    'Khoảng cách giữa các thẻ': 'Gap between cards',
    'Cỡ chữ': 'Font size',
    'Số cột tối đa của widget Nhóm nút (khi rộng hết màn hình)': 'Max columns of the Button group widget (at full width)',
    '{0} cột': '{0} columns',
    'Về mặc định': 'Reset to default',
    'Áp dụng cho': 'Apply to',
    'Tất cả trang (giao diện chung)': 'All pages (shared look)',
    ' · riêng': ' · custom',
    'Dùng lại giao diện chung': 'Use the shared look again',
    'Các trang chưa có giao diện riêng dùng giao diện này.': 'Pages without their own look use this one.',
    'Trang này đang dùng giao diện riêng.': 'This page has its own look.',
    'Trang này đang dùng giao diện chung. Chỉnh bất kỳ mục nào bên dưới để tạo giao diện riêng cho trang.': 'This page uses the shared look. Change anything below to give this page its own look.',
    'Xem trước': 'Preview',
    'Cực quang': 'Aurora', 'Tối': 'Dark', 'Đen (AMOLED)': 'Black (AMOLED)',
    'Thẻ viền': 'Outlined', 'Phẳng': 'Flat', 'Không nền': 'No background',

    // ---------- Cai dat: chung ----------
    'Thành phố xem thời tiết (để trống = tắt)': 'Weather city (empty = off)',
    'Có thể nhập tọa độ dạng': 'You can also enter coordinates like',
    '. Tình trạng xem ở khung phía trên.': '. The status is shown in the box above.',
    'Cổng (đổi xong cần khởi động lại DeskPanel)': 'Port (restart DeskPanel after changing)',
    'Đường dẫn adb.exe (để trống = tự tìm)': 'Path to adb.exe (empty = find automatically)',
    'Tự nối điện thoại qua USB (adb reverse)': 'Connect the phone over USB automatically (adb reverse)',
    'Theo dõi AI (widget AI)': 'AI usage (AI widget)',
    'Lấy % giới hạn chính thức của gói Claude và ChatGPT': 'Get the official plan limit % for Claude and ChatGPT',
    'Chọn một trong các cách dưới. Nếu PC đã đăng nhập Claude Code hoặc Codex thì không cần làm gì. Số liệu chỉ nằm trên PC, không gửi ra ngoài.': 'Pick one of the options below. If Claude Code or Codex is already signed in on this PC, nothing else is needed. The data stays on the PC and is not sent anywhere.',
    'Cách 1 (khuyên dùng): tiện ích cho Chrome / Edge': 'Option 1 (recommended): Chrome / Edge extension',
    'Dùng luôn phiên claude.ai và chatgpt.com bạn đã đăng nhập trong trình duyệt, không phải đăng nhập ở đâu khác.': 'Uses the claude.ai and chatgpt.com sessions already signed in in your browser, so you don\'t sign in anywhere else.',
    'Tải tiện ích (deskpanel-extension.zip)': 'Download the extension (deskpanel-extension.zip)',
    'rồi giải nén ra một thư mục, ví dụ': 'and unzip it into a folder, e.g.',
    'Mở': 'Open',
    '), bật': '), turn on',
    'Bấm': 'Click',
    'và chọn thư mục vừa giải nén.': 'and choose the unzipped folder.',
    'Đăng nhập claude.ai / chatgpt.com trong trình duyệt như bình thường (nếu chưa). Khoảng 1 phút sau widget AI có số liệu.': 'Sign in to claude.ai / chatgpt.com in the browser as usual (if you haven\'t). The AI widget shows data about a minute later.',
    'Cách 2: đăng nhập trong cửa sổ của DeskPanel': 'Option 2: sign in from a DeskPanel window',
    'Đăng nhập Claude': 'Sign in to Claude',
    'Đăng nhập ChatGPT': 'Sign in to ChatGPT',
    'Đăng nhập {0}': 'Sign in to {0}',
    'Đăng xuất': 'Sign out',
    'Đã đăng nhập trong DeskPanel': 'Signed in through DeskPanel',
    'Chưa đăng nhập': 'Not signed in',
    'Máy này chưa có Microsoft Edge WebView2 nên chưa mở được cửa sổ đăng nhập.': 'This PC does not have Microsoft Edge WebView2, so the sign-in window cannot open.',
    'Chưa kết nối': 'Not connected',
    'Đã kết nối (gửi lần cuối {0})': 'Connected (last sent {0})',
    'Mất kết nối {0} phút': 'Disconnected for {0} min',
    'Đang đăng xuất…': 'Signing out…',
    'Đã mở cửa sổ đăng nhập trên PC (có thể nằm sau trình duyệt). Đăng nhập xong cửa sổ tự đóng.': 'The sign-in window is open on the PC (it may be behind the browser). It closes by itself once you sign in.',
    'Đã đăng xuất.': 'Signed out.',
    'Đăng nhập xong. Widget AI cập nhật trong khoảng 1 phút.': 'Signed in. The AI widget updates within about a minute.',
    'Chưa đăng nhập (cửa sổ đã đóng).': 'Not signed in (the window was closed).',
    'Kết nối qua Wi-Fi': 'Connect over Wi-Fi',
    'Cho điện thoại kết nối qua Wi-Fi (cùng mạng với PC, không cần cáp)': 'Let phones connect over Wi-Fi (same network as the PC, no cable)',
    'Bật xong DeskPanel tự chạy lại trong vài giây. Cáp USB vẫn dùng được như cũ.': 'DeskPanel restarts itself within a few seconds. USB still works as before.',
    '1. Android: cài app DeskPanel (chỉ lần đầu)': '1. Android: install the DeskPanel app (first time only)',
    'Quét bằng camera để tải file APK rồi mở ra cài. iPhone bỏ qua mã này.': 'Scan with the camera to download the APK, then open it to install. iPhone users can skip this code.',
    'Tải APK về PC': 'Download the APK to the PC',
    '2. Ghép nối (Android và iPhone)': '2. Pair (Android and iPhone)',
    'Android: mở app DeskPanel → Quét mã QR. iPhone: quét bằng Camera, mở trong Safari, rồi Chia sẻ → Thêm vào MH chính. Hoặc nhập địa chỉ:': 'Android: open the DeskPanel app → Scan QR code. iPhone: scan with the Camera, open in Safari, then Share → Add to Home Screen. Or enter the address:',
    'Đổi mã ghép nối': 'Change pairing key',
    'Đổi mã thì mọi điện thoại đã ghép phải quét lại. Dùng khi lỡ lộ mã.': 'All paired phones must scan again after a change. Use it if the key leaked.',
    'Nếu điện thoại không vào được: trên PC vào Settings → Network → Wi-Fi/Ethernet → chọn': 'If the phone cannot connect: on the PC go to Settings → Network → Wi-Fi/Ethernet → choose',
    '. DeskPanel chỉ mở cổng cho mạng Private, không mở cho mạng công cộng.': '. DeskPanel only opens the port on Private networks, never on public ones.',
    'Đã bật, cần chạy lại DeskPanel để có hiệu lực.': 'Enabled. Restart DeskPanel for it to take effect.',
    'PC chưa có mạng LAN/Wi-Fi nào.': 'The PC is not on any LAN/Wi-Fi network.',
    'Đang bật. Điện thoại cần ghép nối một lần bằng mã QR dưới đây.': 'On. Each phone needs to pair once with the QR code below.',
    'PC có nhiều địa chỉ: {0}. Mã QR dùng {1}; nếu không vào được, thử thay bằng địa chỉ khác cùng dải với điện thoại.': 'The PC has several addresses: {0}. The QR code uses {1}; if it does not work, try another address in the same range as the phone.',
    'Có thay đổi chưa lưu sẽ mất khi DeskPanel chạy lại. Vẫn tiếp tục?': 'Unsaved changes will be lost when DeskPanel restarts. Continue anyway?',
    'Đang lưu…': 'Saving…',
    'Đã lưu nhưng chưa tự chạy lại được ({0}). Hãy tắt rồi mở lại DeskPanel.': 'Saved, but DeskPanel could not restart itself ({0}). Close and reopen DeskPanel.',
    'Đang chạy lại DeskPanel…': 'Restarting DeskPanel…',
    'Xong. Đang tải lại trang…': 'Done. Reloading the page…',
    'Quá 1 phút chưa thấy DeskPanel chạy lại. Mở DeskPanel từ Start Menu (gõ DeskPanel), hoặc chạy lại file cài. Chi tiết trong update.log ở thư mục cài.': 'DeskPanel has not come back after 1 minute. Open DeskPanel from the Start Menu (type DeskPanel), or run the installer again. Details are in update.log in the install folder.',
    'Đổi mã ghép nối? Các điện thoại đang nối qua Wi-Fi sẽ phải quét mã QR mới.': 'Change the pairing key? Phones connected over Wi-Fi will need to scan the new QR code.',
    'Đã đổi mã. Quét lại mã QR trên điện thoại.': 'Key changed. Scan the QR code again on the phone.',
    'Cập nhật DeskPanel': 'Update DeskPanel',
    'Bản đang chạy:': 'Running version:',
    'Cập nhật từ file zip đã tải về (DeskPanel-…-win-x64.zip)': 'Update from a downloaded zip (DeskPanel-…-win-x64.zip)',
    'Cập nhật': 'Update',
    'Không cần giải nén. DeskPanel tự thay bản cũ, giữ nguyên các nút và trang, rồi chạy lại. Điện thoại tự tải lại.': 'No need to unzip. DeskPanel replaces the old version, keeps your buttons and pages, and restarts. The phone reloads by itself.',
    'Tự kiểm tra bản mới trên GitHub (dạng chu/ten-repo, dán link cũng được)': 'Check GitHub for new versions (owner/repo, or paste the link)',
    'Kiểm tra': 'Check',
    'Điền repo rồi bấm Lưu. DeskPanel kiểm tra 6 giờ một lần.': 'Enter the repo and click Save. DeskPanel checks every 6 hours.',
    'Token GitHub (chỉ cần khi repo để private)': 'GitHub token (only for a private repo)',
    'Lưu token': 'Save token',
    'Tạo tại': 'Create one at',
    ': Repository access chọn riêng repo DeskPanel, Permissions → Contents: Read-only. Token được mã hóa theo tài khoản Windows, không lưu trong config.json và không gửi ra điện thoại.': ': Repository access → only the DeskPanel repo, Permissions → Contents: Read-only. The token is encrypted for your Windows account, is not stored in config.json and is never sent to the phone.',
    ' (lúc {0})': ' (at {0})',
    'Có bản mới {0}.': 'New version {0} available.',
    'Đang dùng bản mới nhất ({0}), kiểm tra lúc {1}.': 'You have the latest version ({0}), checked at {1}.',
    'Chưa kiểm tra.': 'Not checked yet.',
    'Cài bản {0}': 'Install {0}',
    '•••••• (đã lưu, dán token mới để thay)': '•••••• (saved, paste a new token to replace it)',
    'Đã cập nhật xong bản {0}. Đang tải lại trang…': 'Updated to {0}. Reloading the page…',
    'Quá 2 phút chưa thấy DeskPanel chạy lại. Xem file update.log trong thư mục cài.': 'DeskPanel has not come back after 2 minutes. Check update.log in the install folder.',
    'Chọn file zip trước': 'Choose a zip file first',
    'Có thay đổi chưa lưu sẽ mất khi DeskPanel chạy lại. Vẫn cập nhật?': 'Unsaved changes will be lost when DeskPanel restarts. Update anyway?',
    'Đang gửi {0} MB…': 'Uploading {0} MB…',
    'Đang tải bản mới từ GitHub…': 'Downloading the new version from GitHub…',
    'Bấm Lưu trước để lưu tên repo': 'Click Save first to store the repo name',
    'Đang kiểm tra…': 'Checking…',
    'Đang lưu token và kiểm tra…': 'Saving the token and checking…',
    'Đã lưu token.': 'Token saved.',
    'Đã xóa token.': 'Token deleted.',
    'Dán token vào ô trước': 'Paste the token into the box first',
    'Xóa token GitHub đã lưu?': 'Delete the saved GitHub token?',
    'Đã lưu. Đổi cổng cần khởi động lại DeskPanel.': 'Saved. Restart DeskPanel to apply the new port.',
    'Đã lưu. Điện thoại tự cập nhật.': 'Saved. The phone updates by itself.',
    'Xem toàn bộ cảm biến đọc được:': 'All sensors DeskPanel can read:',
    '. Nhật ký lỗi:': '. Error log:',
    'trong thư mục cài DeskPanel.': 'in the DeskPanel install folder.',
    'Bảo mật & quyền riêng tư': 'Security & privacy',
    ': DeskPanel đọc gì, lưu gì, gửi đi đâu.': ': what DeskPanel reads, stores and sends.',
    'Ngôn ngữ / Language': 'Language / Ngôn ngữ',

    // ---------- Chuoi tu PC (may chu) ----------
    'Đang khởi động': 'Starting',
    'Đang tải adb từ Google…': 'Downloading adb from Google…',
    'Đã tải adb': 'adb downloaded',
    'Đã tắt tự động adb reverse': 'Automatic adb reverse is off',
    'Không tìm thấy adb.exe (DeskPanel tự tải lại sau ít phút, hoặc đặt thư mục platform-tools cạnh DeskPanel.exe)': 'adb.exe not found (DeskPanel downloads it again in a few minutes, or put the platform-tools folder next to DeskPanel.exe)',
    'Điện thoại chưa cho phép gỡ lỗi USB: mở khóa điện thoại và bấm Cho phép': 'The phone has not allowed USB debugging: unlock it and tap Allow',
    'Chưa thấy điện thoại (cắm cáp USB, bật Gỡ lỗi USB)': 'No phone found (plug in the USB cable, turn on USB debugging)',
    'Đã kết nối # điện thoại qua USB': '# phone(s) connected over USB',
    'adb không phản hồi': 'adb is not responding',
    'Giới hạn': 'Limit',
    'Phiên # giờ': '#-hour session',
    '# giờ': '# hours',
    'Tuần': 'Week',
    'Tháng': 'Month',
    'Trình duyệt chưa đăng nhập claude.ai': 'The browser is not signed in to claude.ai',
    'Trình duyệt chưa đăng nhập chatgpt.com': 'The browser is not signed in to chatgpt.com',
    'Phiên đăng nhập Claude trong DeskPanel đã hết: vào ⚙ Cài đặt → Chung đăng nhập lại': 'The Claude sign-in in DeskPanel has expired: sign in again in ⚙ Settings → General',
    'Phiên đăng nhập ChatGPT trong DeskPanel đã hết: vào ⚙ Cài đặt → Chung đăng nhập lại': 'The ChatGPT sign-in in DeskPanel has expired: sign in again in ⚙ Settings → General',
    'Không lấy được giới hạn (mạng?)': 'Could not get limits (network?)',
    'Chưa đăng nhập Claude Code trên PC này: vào ⚙ Cài đặt → Chung → Đăng nhập Claude': 'Claude Code is not signed in on this PC: go to ⚙ Settings → General → Sign in to Claude',
    'Chưa có tài khoản Claude: vào ⚙ Cài đặt → Chung → Đăng nhập Claude': 'No Claude account yet: go to ⚙ Settings → General → Sign in to Claude',
    'Đăng nhập Claude Code đã hết hạn, mở Claude Code một lần để làm mới': 'The Claude Code sign-in has expired; open Claude Code once to refresh it',
    'Claude từ chối, mở Claude Code một lần để làm mới đăng nhập': 'Claude refused; open Claude Code once to refresh the sign-in',
    'ChatGPT từ chối, mở Codex một lần để làm mới đăng nhập': 'ChatGPT refused; open Codex once to refresh the sign-in',
    'Dùng thêm: $#': 'Extra usage: $#',
    'Dùng thêm: $# / $#': 'Extra usage: $# / $#',
    'Credits: không giới hạn': 'Credits: unlimited',
    'Chưa lấy': 'Not fetched yet',
    'Dữ liệu giả': 'Fake data',
    'Đang tắt': 'Off',
    'Các nguồn thời tiết đều không trả lời': 'No weather service responded',
    "Không tìm thấy thành phố (thử tên tiếng Anh hoặc nhập 'vĩ độ,kinh độ')": "City not found (try the English name or enter 'latitude,longitude')",
    'Hết thời gian chờ máy chủ thời tiết': 'The weather server timed out',
    'Máy chủ thời tiết trả lỗi #': 'The weather server returned error #',
    'Không phân giải được tên miền (DNS)': 'Could not resolve the domain name (DNS)',
    'Lỗi chứng chỉ HTTPS (phần mềm diệt virus/proxy chặn?)': 'HTTPS certificate error (blocked by antivirus/proxy?)',
    'Không kết nối được máy chủ thời tiết': 'Cannot reach the weather server',
    'Dữ liệu thời tiết không đọc được': 'Unreadable weather data',
    'Không lấy được thời tiết': 'Could not get the weather',
    'Trời quang': 'Clear', 'Ít mây': 'Partly cloudy', 'Có mây': 'Cloudy', 'Nhiều mây': 'Overcast', 'Sương mù': 'Fog',
    'Mưa phùn': 'Drizzle', 'Mưa': 'Rain', 'Mưa to': 'Heavy rain', 'Tuyết': 'Snow', 'Mưa rào': 'Showers', 'Mưa rào lớn': 'Heavy showers', 'Dông': 'Thunderstorm',
    'Chưa đọc được nhiệt độ CPU: cần cài PawnIO và chạy DeskPanel bằng quyền admin.': 'Cannot read CPU temperature yet: install PawnIO and run DeskPanel as admin.',
    'Ổ đĩa': 'Drive',
    'Demo CPU (dữ liệu giả)': 'Demo CPU (fake data)',
    'Đang chạy dữ liệu giả (không phải Windows).': 'Running on fake data (not Windows).',
    'Đang bận thực hiện nút khác': 'Busy running another button',
    'Loại nút không hợp lệ': 'Invalid button type',
    'Windows chặn gửi phím (máy đang khóa, đang hiện hộp thoại UAC, hoặc cửa sổ đích chạy quyền cao hơn)': 'Windows blocked the keystrokes (the PC is locked, a UAC prompt is open, or the target window runs with higher rights)',
    'Không có nút này': 'No such button',
    'Lệnh không hợp lệ': 'Invalid command',
    'Token không đúng dạng (ví dụ github_pat_…)': 'The token format is wrong (e.g. github_pat_…)',
    'Máy chưa có Microsoft Edge WebView2. Tải tại https://go.microsoft.com/fwlink/p/?LinkId=2124703 rồi thử lại.': 'Microsoft Edge WebView2 is missing. Get it at https://go.microsoft.com/fwlink/p/?LinkId=2124703 and try again.',
    'Chưa điền repo GitHub': 'No GitHub repo set',
    'Token GitHub sai hoặc đã hết hạn. Tạo token mới rồi dán lại': 'The GitHub token is wrong or expired. Create a new one and paste it again',
    'Không thấy repo, repo chưa có bản phát hành, hoặc token chưa được cấp quyền đọc repo này': 'Repo not found, it has no releases, or the token cannot read it',
    'Không thấy repo hoặc chưa có bản phát hành. Nếu repo để private, hãy dán token GitHub': 'Repo not found or it has no releases. If the repo is private, paste a GitHub token',
    'GitHub tạm giới hạn số lần hỏi hoặc token thiếu quyền, thử lại sau': 'GitHub is rate-limiting or the token lacks permission; try again later',
    'Không có bản mới hơn bản đang chạy': 'No version newer than the running one',
    'Đường dẫn tải không phải của GitHub': 'The download link is not from GitHub',
    'Đang cập nhật, hãy chờ': 'Updating, please wait',
    'File quá lớn (tối đa 200 MB)': 'File too large (max 200 MB)',
    'File không phải zip hợp lệ': 'Not a valid zip file',
    'Trong zip không có DeskPanel.exe và thư mục wwwroot. Hãy chọn file DeskPanel-…-win-x64.zip': 'The zip has no DeskPanel.exe and wwwroot folder. Choose the DeskPanel-…-win-x64.zip file',
    'Zip hợp lệ (chế độ thử, không phải Windows nên không cài)': 'Valid zip (test mode: not Windows, so nothing is installed)',
    'Không phải Windows: hãy tự chạy lại DeskPanel': 'Not Windows: restart DeskPanel yourself',
    'Màu nhấn phải dạng #RRGGBB': 'The accent color must be #RRGGBB',
    'Cổng (port) phải từ # đến #': 'The port must be between # and #',
    'Số cột phải từ # đến #': 'Columns must be between # and #',
    'Tên thành phố quá dài': 'The city name is too long',
    'Repo GitHub phải có dạng chu-repo/ten-repo': 'The GitHub repo must look like owner/repo',
    'Tối đa # nút': 'At most # buttons',
    'Thiết bị đã ghép nối': 'Paired devices',
    'Chưa có thiết bị nào.': 'No devices yet.',
    '{0} giờ trước': '{0} h ago',
    '{0} ngày trước': '{0} days ago',
    'lần cuối: {0}': 'last seen: {0}',
    ' · đã thu hồi': ' · revoked',
    'Đổi tên': 'Rename',
    'Tên thiết bị': 'Device name',
    'Thu hồi': 'Revoke',
    'Thu hồi "{0}"? Thiết bị này bị ngắt ngay và không tự kết nối lại được.': 'Revoke "{0}"? It is disconnected right away and cannot reconnect by itself.',
    'Xóa khỏi danh sách': 'Remove from list',
    '+ Phần mềm đang mở': '+ Running app',
    'Chọn phần mềm…': 'Choose an app…',
    'Gợi ý: AutoCAD': 'Suggest: AutoCAD',
    'Tự mở trang này khi dùng phần mềm': 'Open this page when using an app',
    'Tên tiến trình trên PC, cách nhau dấu phẩy. Khi bạn chuyển sang phần mềm đó trên PC, điện thoại tự chuyển sang trang này.': 'Process names on the PC, separated by commas. When you switch to that app on the PC, the phone jumps to this page.',
    'Bật tự chuyển trang (áp dụng mọi trang)': 'Auto-switch pages (all pages)',
    'Tối đa # trang': 'At most # pages'
  };

  // Mau co phan chu thay doi (ten nguon, ten file...): [regex tieng Viet, ban dich]
  var RX = [
    [/^Trang '(.+)': danh sách phần mềm tự chuyển trang không hợp lệ \(tên tiến trình, cách nhau dấu phẩy, tối đa 10\)$/, "Page '$1': invalid app list for auto-switch (process names, comma separated, at most 10)"],
    [/^OK \((.+)\), cập nhật lúc (.+)$/, 'OK ($1), updated at $2'],
    [/^(.+): tạm nghỉ tới (.+)$/, '$1: paused until $2'],
    [/^Không tải được adb \((.*)\)\. Thử lại sau 1 giờ\.$/, 'Could not download adb ($1). Retrying in 1 hour.'],
    [/^Lỗi adb: (.*)$/, 'adb error: $1'],
    [/^adb reverse lỗi: (.*)$/, 'adb reverse failed: $1'],
    [/^Không kiểm tra được: (.*)$/, 'Could not check: $1'],
    [/^Cập nhật lỗi: (.*)$/, 'Update failed: $1'],
    [/^Dữ liệu không hợp lệ: (.*)$/, 'Invalid data: $1'],
    [/^Không mở được cổng trên Windows Firewall: (.*)$/, 'Could not open the port in Windows Firewall: $1'],
    [/^Bản (.+) chưa có file …-win-x64\.zip$/, 'Release $1 has no …-win-x64.zip file'],
    [/^Đang cài bản (.+) \(bản hiện tại (.+)\)\. DeskPanel sẽ tắt và chạy lại trong khoảng 10–20 giây\.$/, 'Installing $1 (current $2). DeskPanel will close and restart in about 10–20 seconds.'],
    [/^(.+) chưa mở$/, '$1 is not open'],
    [/^Không đưa được (.+) lên trước \(Windows chặn đổi cửa sổ\)\. Bấm vào (.+) trên PC một lần rồi thử lại\.$/, 'Could not bring $1 to the front (Windows blocked the switch). Click $2 on the PC once and try again.'],
    [/^Trang '(.+)': (.+)$/, "Page '$1': $2"],
    [/^Nút '(.+)': (.+)$/, "Button '$1': $2"]
  ];

  var NUM = /\d+(?:[.,:]\d+)*/g;
  var VI = /[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ]/;

  function fmt(s, a){ return s.replace(/\{(\d)\}/g, function(_, i){ var v = a[+i]; return v == null ? '' : String(v); }); }
  function T(s){
    var a = Array.prototype.slice.call(arguments, 1);
    return fmt(L === 'en' && D[s] != null ? D[s] : s, a);
  }

  // Dich mot doan chu bat ky (da cat khoang trang dau/cuoi). Tra ve null neu khong co ban dich.
  function tr(k, deep){
    if (!k) return null;
    if (D[k] != null && k.indexOf('{') < 0) return D[k];
    if (!VI.test(k)) return null;
    var nums = [], tk = k.replace(NUM, function(m){ nums.push(m); return '#'; });
    if (nums.length && D[tk] != null) { var i = 0; return D[tk].replace(/#/g, function(){ return nums[i++]; }); }
    for (var j = 0; j < RX.length; j++) if (RX[j][0].test(k)) {
      var parts = k.match(RX[j][0]);
      return RX[j][1].replace(/\$(\d)/g, function(_, n){ var p = parts[+n] || ''; return (deep || 0) < 2 ? (tr(p, (deep || 0) + 1) || p) : p; });
    }
    if (/:$/.test(k)) { var c = tr(k.slice(0, -1), deep); if (c != null) return c + ':'; }
    // "🏠 Tổng quan": bieu tuong o dau
    var ic = k.match(/^([^\s\wÀ-ỹ]+\s+)(.+)$/);
    if (ic) { var r2 = tr(ic[2], deep); if (r2 != null) return ic[1] + r2; }
    // "A [B]"
    var br = k.match(/^(.+) \[(.+)\]$/);
    if (br) { var b1 = tr(br[1], deep), b2 = tr(br[2], deep); if (b1 != null || b2 != null) return (b1 || br[1]) + ' [' + (b2 || br[2]) + ']'; }
    var seps = [' · ', '; ', '\n'];
    for (var s = 0; s < seps.length; s++) if (k.indexOf(seps[s]) > 0) {
      var ch = false, out = k.split(seps[s]).map(function(p){ var t = p.trim() ? tr(p.trim(), deep) : null; if (t != null) { ch = true; return p.replace(p.trim(), t); } return p; });
      if (ch) return out.join(seps[s]);
    }
    // "Phím: Win+E", "Open-Meteo: <loi>"
    var ci = k.indexOf(': ');
    if (ci > 0 && ci < 40) {
      var hd = k.slice(0, ci), tl = k.slice(ci + 2);
      var th = D[hd] != null ? D[hd] : (VI.test(hd) ? null : hd), tt = tr(tl, deep);
      if (tt == null && !VI.test(tl)) tt = tl;
      if (th != null && tt != null) return th + ': ' + tt;
    }
    return null;
  }
  function trText(s){
    var k = s.trim(); if (!k) return null;
    var t = tr(k); return t == null ? null : s.replace(k, t);
  }

  var ATTRS = ['placeholder', 'title', 'aria-label'];
  function skip(n){ for (var e = n.nodeType === 1 ? n : n.parentNode; e && e.nodeType === 1; e = e.parentNode) { var tn = e.nodeName; if (tn === 'SCRIPT' || tn === 'STYLE' || tn === 'TEXTAREA' || e.getAttribute('translate') === 'no') return true; } return false; }
  function doText(n){ if (n.__dp === n.data || skip(n)) return; var t = trText(n.data); if (t != null && t !== n.data) n.data = t; n.__dp = n.data; }
  function doEl(e){
    if (skip(e)) return;
    ATTRS.forEach(function(a){ var v = e.getAttribute(a); if (v) { var t = trText(v); if (t != null && t !== v) e.setAttribute(a, t); } });
  }
  function walk(root){
    if (root.nodeType === 3) { doText(root); return; }
    if (root.nodeType !== 1 && root.nodeType !== 9) return;
    if (root.nodeType === 1) doEl(root);
    var w = document.createTreeWalker(root, 5 /* element | text */, null, false), n;
    while ((n = w.nextNode())) { if (n.nodeType === 3) doText(n); else doEl(n); }
  }

  function start(){
    if (L !== 'en') return;
    document.documentElement.lang = 'en';
    var t = trText(document.title); if (t) document.title = t;
    walk(document.body);
    new MutationObserver(function(ms){
      ms.forEach(function(m){
        if (m.type === 'characterData') doText(m.target);
        else if (m.type === 'attributes') doEl(m.target);
        else for (var i = 0; i < m.addedNodes.length; i++) walk(m.addedNodes[i]);
      });
    }).observe(document.body, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ATTRS });
  }
  if (L === 'en') {
    ['confirm', 'alert', 'prompt'].forEach(function(f){
      var orig = window[f];
      window[f] = function(msg){ var a = Array.prototype.slice.call(arguments); if (typeof msg === 'string') { var t = trText(msg); if (t != null) a[0] = t; } return orig.apply(window, a); };
    });
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start); else start();
  }

  window.T = T;
  window.I18N = {
    lang: L,
    tr: function(s){ return L === 'en' ? (trText(s) || s) : s; },
    // Dat ngon ngu va tai lai trang neu doi
    set: function(v){ v = v === 'en' ? 'en' : 'vi'; try { localStorage.setItem(KEY, v); } catch(e) {} if (v !== L) location.reload(); },
    // Theo cau hinh tren PC; tra ve true neu sap tai lai trang
    sync: function(v){ if (v !== 'en' && v !== 'vi') return false; try { localStorage.setItem(KEY, v); } catch(e) {} if (v !== L) { location.reload(); return true; } return false; }
  };
})();
