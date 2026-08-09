allprojects {
    repositories {
        google()
        mavenCentral()

        // Fallback mirror of Google's Maven repository — see the matching note in settings.gradle.kts.
        // Declared last so the canonical repository is always preferred where it is reachable.
        maven { url = uri("https://maven.aliyun.com/repository/google") }
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
