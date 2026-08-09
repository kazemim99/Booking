pluginManagement {
    val flutterSdkPath =
        run {
            val properties = java.util.Properties()
            file("local.properties").inputStream().use { properties.load(it) }
            val flutterSdkPath = properties.getProperty("flutter.sdk")
            require(flutterSdkPath != null) { "flutter.sdk not set in local.properties" }
            flutterSdkPath
        }

    includeBuild("$flutterSdkPath/packages/flutter_tools/gradle")

    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()

        // Fallback mirror of Google's Maven repository.
        //
        // Google's repository (dl.google.com/dl/android/maven2) is unreachable from some networks — it answers
        // 404 for every artifact, including ones that certainly exist — which leaves AGP itself unresolvable and
        // no Android build possible. pub and the Flutter SDK are unaffected because they have their own mirrors.
        //
        // Declared LAST on purpose. Gradle tries repositories in order and treats a 404 as "not in this repo,
        // try the next one", so where google() is reachable it still wins and this line changes nothing; it is
        // only consulted when the canonical repository does not serve the artifact.
        //
        // Note this is a third-party mirror of a dependency source. It is here so local Android builds work, not
        // as an endorsement for release builds — prefer the canonical repository wherever it is reachable.
        maven { url = uri("https://maven.aliyun.com/repository/google") }
    }
}

plugins {
    id("dev.flutter.flutter-plugin-loader") version "1.0.0"
    id("com.android.application") version "8.11.1" apply false
    id("org.jetbrains.kotlin.android") version "2.2.20" apply false
}

include(":app")
