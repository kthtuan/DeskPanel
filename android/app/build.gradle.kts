plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

// So phien ban lay tu src/DeskPanel.csproj (workflow truyen vao bang -PappVersion=0.7.0)
val appVersion = (project.findProperty("appVersion") as String?) ?: "0.0.0"
val parts = appVersion.split(".").map { it.toIntOrNull() ?: 0 } + listOf(0, 0, 0)

android {
    namespace = "io.github.kthtuan.deskpanel"
    compileSdk = 35

    defaultConfig {
        applicationId = "io.github.kthtuan.deskpanel"
        minSdk = 21
        targetSdk = 35
        versionCode = parts[0] * 10000 + parts[1] * 100 + parts[2]
        versionName = appVersion
    }

    // Khoa ky co dinh de cai ban moi de len ban cu (Android tu choi neu khoa khac).
    // Repo de private; app chi cai tay (sideload), khong dua len Play Store.
    signingConfigs {
        create("release") {
            storeFile = file("../deskpanel.jks")
            storePassword = "deskpanel"
            keyAlias = "deskpanel"
            keyPassword = "deskpanel"
        }
    }
    buildTypes {
        release {
            isMinifyEnabled = false
            signingConfig = signingConfigs.getByName("release")
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    // App cai tay, khong len Play Store: khong de canh bao lint (vd targetSdk) chan ban build
    lint { checkReleaseBuilds = false; abortOnError = false }
}

dependencies {
    implementation("com.journeyapps:zxing-android-embedded:4.3.0") { isTransitive = false }
    // zxing core 3.3.0 chay duoc tren Android 5/6 (3.4+ can Android 7)
    implementation("com.google.zxing:core:3.3.0")
    // Man hinh quet (CaptureManager) goi androidx.core de xin quyen camera; thieu thu vien nay app bi dong khi bam Quet
    implementation("androidx.core:core:1.13.1")
}
