:: Set the PACKAGE_NAME and APK_NAME variables to match your setup.
setlocal
set PACKAGE_NAME=com.gopositron.voyagerdemo
set APK_NAME=VoyagerDemo.apk

:: Uninstall prev version
adb uninstall %PACKAGE_NAME%

:: Install the APK and grant permissions
adb install %APK_NAME%
adb shell pm grant %PACKAGE_NAME% android.permission.READ_EXTERNAL_STORAGE
adb shell pm grant %PACKAGE_NAME% android.permission.WRITE_EXTERNAL_STORAGE