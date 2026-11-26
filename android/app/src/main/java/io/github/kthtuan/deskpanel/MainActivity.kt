package io.github.kthtuan.deskpanel

import android.annotation.SuppressLint
import android.app.Activity
import android.app.AlertDialog
import android.content.Context
import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.graphics.Color
import android.graphics.Typeface
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.text.InputType
import android.util.TypedValue
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.view.WindowInsets
import android.view.WindowInsetsController
import android.view.WindowManager
import android.webkit.CookieManager
import android.webkit.WebChromeClient
import android.webkit.WebResourceError
import android.webkit.WebResourceRequest
import android.webkit.WebResourceResponse
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.Button
import android.widget.EditText
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import com.google.zxing.integration.android.IntentIntegrator

/**
 * DeskPanel cho dien thoai: mot WebView toan man hinh mo bang dieu khien tren PC.
 * - Ket noi qua Wi-Fi (quet ma QR trong trang cai dat DeskPanel tren PC) hoac cap USB (localhost).
 * - Giu man hinh sang, an thanh trang thai/thanh dieu huong.
 * - Mat ket noi thi tu thu lai moi 3 giay.
 */
class MainActivity : Activity() {
    companion object {
        const val USB_URL = "http://localhost:8765/"
        const val PREFS = "deskpanel"
        const val KEY_URL = "url"
        const val RETRY_MS = 3000L
    }

    private lateinit var root: FrameLayout
    private lateinit var web: WebView
    private lateinit var overlay: LinearLayout
    private lateinit var overlayText: TextView
    private var setup: View? = null
    private val ui = Handler(Looper.getMainLooper())
    private var mainError = false
    private val retry = Runnable { load() }

    private val prefs by lazy { getSharedPreferences(PREFS, Context.MODE_PRIVATE) }
    private var url: String?
        get() = prefs.getString(KEY_URL, null)
        set(v) { prefs.edit().putString(KEY_URL, v).apply() }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Ghi lai loi lam app dong de lan mo sau hien ra (de bao loi)
        val prev = Thread.getDefaultUncaughtExceptionHandler()
        Thread.setDefaultUncaughtExceptionHandler { t, e ->
            try {
                val sw = java.io.StringWriter(); e.printStackTrace(java.io.PrintWriter(sw))
                prefs.edit().putString("crash", "DeskPanel ${appVersion()} · Android ${Build.VERSION.RELEASE} · ${Build.MANUFACTURER} ${Build.MODEL}\n" + sw.toString().take(3000)).commit()
            } catch (_: Throwable) {}
            prev?.uncaughtException(t, e)
        }
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        root = FrameLayout(this).apply { setBackgroundColor(Color.BLACK) }
        web = makeWebView()
        root.addView(web, FrameLayout.LayoutParams(-1, -1))
        overlay = makeOverlay()
        root.addView(overlay, FrameLayout.LayoutParams(-1, -1))
        setContentView(root)
        hideBars()
        val crash = prefs.getString("crash", null)
        if (crash != null) {
            prefs.edit().remove("crash").apply()
            showSetup(null)
            AlertDialog.Builder(this).setTitle("Lần trước DeskPanel bị lỗi")
                .setMessage("Chụp màn hình này gửi lại để được sửa:\n\n$crash")
                .setPositiveButton("OK", null).show()
            return
        }
        if (url == null) showSetup(null) else load()
    }

    private fun appVersion(): String = try {
        packageManager.getPackageInfo(packageName, 0).versionName ?: "?"
    } catch (_: Throwable) { "?" }

    // ---------------- WebView ----------------

    @SuppressLint("SetJavaScriptEnabled")
    private fun makeWebView(): WebView {
        val w = WebView(this)
        w.setBackgroundColor(Color.BLACK)
        w.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
            mediaPlaybackRequiresUserGesture = false
            cacheMode = WebSettings.LOAD_DEFAULT
            useWideViewPort = true
            loadWithOverviewMode = true
            setSupportZoom(false)
            builtInZoomControls = false
        }
        CookieManager.getInstance().setAcceptCookie(true)
        w.webChromeClient = WebChromeClient()
        w.webViewClient = object : WebViewClient() {
            override fun onPageStarted(view: WebView, u: String?, favicon: Bitmap?) { mainError = false }

            override fun onPageFinished(view: WebView, u: String?) {
                if (!mainError) overlay.visibility = View.GONE
                CookieManager.getInstance().flush()
            }

            override fun onReceivedError(view: WebView, req: WebResourceRequest, err: WebResourceError) {
                if (Build.VERSION.SDK_INT >= 23 && req.isForMainFrame) failed(err.description?.toString())
            }

            @Deprecated("Deprecated in Java")
            override fun onReceivedError(view: WebView, code: Int, desc: String?, failingUrl: String?) {
                if (Build.VERSION.SDK_INT < 23) failed(desc)
            }

            override fun onReceivedHttpError(view: WebView, req: WebResourceRequest, resp: WebResourceResponse) {
                if (!req.isForMainFrame) return
                if (resp.statusCode == 401) {
                    mainError = true
                    ui.post { showSetup("PC chưa nhận điện thoại này hoặc mã ghép nối đã đổi. Hãy quét lại mã QR trong trang cài đặt DeskPanel trên PC.") }
                } else if (resp.statusCode >= 500) failed("Lỗi máy chủ " + resp.statusCode)
            }

            override fun shouldOverrideUrlLoading(view: WebView, req: WebResourceRequest): Boolean {
                val target = req.url
                val home = url?.let { Uri.parse(it) }
                if (home != null && target.host == home.host && target.port == home.port) return false
                // Link ra ngoai (vd trang web) mo bang trinh duyet
                try { startActivity(Intent(Intent.ACTION_VIEW, target)) } catch (_: Exception) {}
                return true
            }
        }
        return w
    }

    private fun load() {
        ui.removeCallbacks(retry)
        val u = url ?: return showSetup(null)
        mainError = false
        web.loadUrl(u)
    }

    private fun failed(reason: String?) {
        mainError = true
        val where = url?.let { Uri.parse(it).let { p -> p.host + (if (p.port > 0) ":" + p.port else "") } } ?: ""
        val usb = url?.startsWith(USB_URL) == true
        overlayText.text = buildString {
            append("Chưa kết nối được DeskPanel ($where)\n")
            if (!reason.isNullOrBlank()) append(reason).append('\n')
            append(if (usb) "\nKiểm tra: cáp USB đã cắm, DeskPanel trên PC đang chạy và đã bật USB debugging."
                   else "\nKiểm tra: PC đang bật DeskPanel, điện thoại và PC cùng mạng Wi-Fi.")
            append("\nĐang thử lại…")
        }
        overlay.visibility = View.VISIBLE
        ui.removeCallbacks(retry)
        ui.postDelayed(retry, RETRY_MS)
    }

    // ---------------- Man hinh thong bao / cai dat ----------------

    private fun dp(v: Int) = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v.toFloat(), resources.displayMetrics).toInt()

    private fun button(text: String, primary: Boolean = false, onClick: () -> Unit) = Button(this).apply {
        this.text = text
        isAllCaps = false
        textSize = 16f
        setTextColor(if (primary) Color.BLACK else Color.WHITE)
        setBackgroundColor(if (primary) Color.parseColor("#58A6FF") else Color.parseColor("#21262D"))
        setPadding(dp(16), dp(10), dp(16), dp(10))
        setOnClickListener { onClick() }
        layoutParams = LinearLayout.LayoutParams(-1, -2).apply { topMargin = dp(10) }
    }

    private fun makeOverlay(): LinearLayout {
        val box = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER
            setBackgroundColor(Color.parseColor("#0D1117"))
            setPadding(dp(32), dp(24), dp(32), dp(24))
            visibility = View.VISIBLE
            isClickable = true
        }
        overlayText = TextView(this).apply {
            setTextColor(Color.parseColor("#C9D1D9")); textSize = 16f; gravity = Gravity.CENTER
            text = "Đang kết nối…"
        }
        box.addView(overlayText)
        val row = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; layoutParams = LinearLayout.LayoutParams(dp(280), -2).apply { topMargin = dp(16) } }
        row.addView(button("Thử lại ngay", true) { load() })
        row.addView(button("Đổi kết nối") { showSetup(null) })
        box.addView(row)
        return box
    }

    private fun showSetup(message: String?) {
        ui.removeCallbacks(retry)
        setup?.let { root.removeView(it) }
        val col = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER_HORIZONTAL
            setPadding(dp(28), dp(28), dp(28), dp(28))
        }
        col.addView(TextView(this).apply {
            text = "DeskPanel"; textSize = 28f; setTypeface(typeface, Typeface.BOLD); setTextColor(Color.WHITE); gravity = Gravity.CENTER
        })
        col.addView(TextView(this).apply {
            text = "Biến điện thoại thành màn hình phụ cho PC. Bản " + appVersion()
            textSize = 15f; setTextColor(Color.parseColor("#8B949E")); gravity = Gravity.CENTER
            setPadding(0, dp(4), 0, dp(8))
        })
        if (message != null) col.addView(TextView(this).apply {
            text = message; textSize = 15f; setTextColor(Color.parseColor("#F0883E")); gravity = Gravity.CENTER
            setPadding(0, dp(8), 0, dp(8))
        })
        val btns = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; layoutParams = LinearLayout.LayoutParams(dp(300), -2) }
        btns.addView(button("Quét mã QR (Wi-Fi)", true) { scan() })
        btns.addView(button("Nhập địa chỉ") { askUrl() })
        btns.addView(button("Dùng cáp USB") { use(USB_URL) })
        if (url != null && message == null) btns.addView(button("Quay lại") { closeSetup(); load() })
        col.addView(btns)
        col.addView(TextView(this).apply {
            text = "Wi-Fi: trên PC mở trang cài đặt DeskPanel → tab Chung → bật \"Kết nối qua Wi-Fi\" rồi quét mã QR hiện ra.\n\n" +
                "Cáp USB: cắm cáp, bật USB debugging trên điện thoại. DeskPanel trên PC tự nối."
            textSize = 13f; setTextColor(Color.parseColor("#8B949E"))
            setPadding(0, dp(20), 0, 0)
            layoutParams = LinearLayout.LayoutParams(dp(300), -2)
        })
        val sv = ScrollView(this).apply {
            setBackgroundColor(Color.parseColor("#0D1117"))
            isFillViewport = true
            addView(col, FrameLayout.LayoutParams(-1, -2).apply { gravity = Gravity.CENTER })
        }
        col.layoutParams = FrameLayout.LayoutParams(-1, -2, Gravity.CENTER)
        setup = sv
        root.addView(sv, FrameLayout.LayoutParams(-1, -1))
    }

    private fun closeSetup() { setup?.let { root.removeView(it) }; setup = null }

    private fun use(u: String) {
        url = u
        closeSetup()
        overlayText.text = "Đang kết nối…"
        overlay.visibility = View.VISIBLE
        load()
    }

    private fun askUrl() {
        val input = EditText(this).apply {
            inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_URI
            hint = "http://192.168.1.10:8765/?key=…"
            setText(url ?: "")
        }
        val wrap = FrameLayout(this).apply { setPadding(dp(20), dp(8), dp(20), 0); addView(input) }
        AlertDialog.Builder(this)
            .setTitle("Địa chỉ DeskPanel")
            .setMessage("Nhập đúng địa chỉ hiện dưới mã QR trong trang cài đặt trên PC (có cả phần ?key=…).")
            .setView(wrap)
            .setPositiveButton("Kết nối") { _, _ -> normalize(input.text.toString())?.let { use(it) } }
            .setNegativeButton("Hủy", null)
            .show()
    }

    private fun normalize(raw: String): String? {
        var s = raw.trim()
        if (s.isEmpty()) return null
        if (!s.startsWith("http://", true) && !s.startsWith("https://", true)) s = "http://$s"
        val u = Uri.parse(s)
        if (u.host.isNullOrEmpty()) return null
        return if (u.port <= 0 && u.scheme.equals("http", true)) u.buildUpon().encodedAuthority(u.host + ":8765").build().toString() else s
    }

    // ---------------- Quet QR ----------------

    private fun scan() {
        // Tu xin quyen camera truoc khi mo man hinh quet
        if (Build.VERSION.SDK_INT >= 23 && checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(arrayOf(Manifest.permission.CAMERA), 7)
            return
        }
        try {
            IntentIntegrator(this).apply {
                setDesiredBarcodeFormats(IntentIntegrator.QR_CODE)
                setPrompt("Quét mã QR trong trang cài đặt DeskPanel trên PC")
                setBeepEnabled(false)
                setOrientationLocked(false)
            }.initiateScan()
        } catch (e: Throwable) {
            AlertDialog.Builder(this).setTitle("Không mở được máy quét")
                .setMessage("${e.javaClass.simpleName}: ${e.message}\n\nDùng \"Nhập địa chỉ\" thay thế, và chụp màn hình này gửi lại.")
                .setPositiveButton("OK", null).show()
        }
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode != 7) return
        if (grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) scan()
        else AlertDialog.Builder(this).setMessage("Cần quyền Camera để quét mã QR. Bạn có thể dùng \"Nhập địa chỉ\" thay thế.").setPositiveButton("OK", null).show()
    }

    @Deprecated("Deprecated in Java")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        val r = IntentIntegrator.parseActivityResult(requestCode, resultCode, data)
        if (r == null) { super.onActivityResult(requestCode, resultCode, data); return }
        val text = r.contents ?: return
        val u = Uri.parse(text.trim())
        if (u.scheme?.lowercase() !in setOf("http", "https") || u.host.isNullOrEmpty()) {
            AlertDialog.Builder(this).setMessage("Mã QR này không phải của DeskPanel:\n$text").setPositiveButton("OK", null).show()
            return
        }
        // Dang nhap moi: xoa cookie cu de dung ma vua quet
        CookieManager.getInstance().removeAllCookies(null)
        use(text.trim())
    }

    // ---------------- Toan man hinh, nut Back ----------------

    @Suppress("DEPRECATION")
    private fun hideBars() {
        if (Build.VERSION.SDK_INT >= 30) {
            window.setDecorFitsSystemWindows(false)
            window.insetsController?.let {
                it.hide(WindowInsets.Type.statusBars() or WindowInsets.Type.navigationBars())
                it.systemBarsBehavior = WindowInsetsController.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
            }
        } else {
            window.decorView.systemUiVisibility = (View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                or View.SYSTEM_UI_FLAG_FULLSCREEN
                or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                or View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN)
        }
        if (Build.VERSION.SDK_INT >= 28) {
            window.attributes = window.attributes.apply {
                layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
            }
        }
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) hideBars()
    }

    @Deprecated("Deprecated in Java")
    override fun onBackPressed() {
        if (setup != null && url != null) { closeSetup(); load(); return }
        AlertDialog.Builder(this)
            .setTitle("DeskPanel")
            .setItems(arrayOf("Tải lại", "Đổi kết nối", "Thoát")) { _, i ->
                when (i) {
                    0 -> load()
                    1 -> showSetup(null)
                    2 -> finish()
                }
            }
            .show()
    }

    override fun onResume() {
        super.onResume()
        web.onResume()
        hideBars()
        if (mainError) load()
    }

    override fun onPause() {
        web.onPause()
        CookieManager.getInstance().flush()
        super.onPause()
    }

    override fun onDestroy() {
        ui.removeCallbacksAndMessages(null)
        (web.parent as? ViewGroup)?.removeView(web)
        web.destroy()
        super.onDestroy()
    }
}
