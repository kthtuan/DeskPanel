# DeskPanel

Biến điện thoại Android thành màn hình phụ cho PC Windows qua cáp USB: xem CPU, GPU, RAM, nhiệt độ, sức khỏe ổ cứng, thời tiết, nhạc đang phát và bấm nút lệnh AutoCAD, phím tắt. Tự xếp trang và widget bằng kéo thả.

- **Tải về:** mục [Releases](../../releases), file `DeskPanel-x.y.z-win-x64.zip`.
- **Hướng dẫn cài và dùng:** [pkg/HUONG-DAN.md](pkg/HUONG-DAN.md).
- **Cập nhật:** từ bản 0.6, mở trang cài đặt → thẻ Chung → điền repo `kthtuan/DeskPanel` để DeskPanel tự báo khi có bản mới.

## Cấu trúc

- `src/`: chương trình chạy trên PC (C# .NET 8, ASP.NET Core). Đọc cảm biến bằng LibreHardwareMonitorLib, SMART ổ cứng bằng DiskInfoToolkit, nối điện thoại bằng `adb reverse`.
- `src/wwwroot/index.html`: giao diện trên điện thoại. `settings.html`: trang cài đặt.
- `pkg/`: script cài, cập nhật, gỡ và hướng dẫn.
- `.github/workflows/release.yml`: mỗi lần tăng `<Version>` trong `src/DeskPanel.csproj` và đẩy lên `main`, GitHub tự build bản Windows và tạo Release `vX.Y.Z`.

## Build

```
dotnet publish src/DeskPanel.csproj -c Release -r win-x64 -o out/DeskPanel
```

## Ghi công

Giao diện lấy cảm hứng từ [PhoneBox](https://github.com/ersinkoc/phonebox) (MIT, © 2026 Ersin KOÇ). DeskPanel không chép mã của PhoneBox.
