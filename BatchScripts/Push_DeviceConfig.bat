:: Push a VoyagerDevice config file to your target Andriod device over ADB.
:: Place the config file (ex: InterfaceConfig.json) in the same folder as this .bat.
:: Set the PACKAGE_NAME, CONFIG_DIRNAME, and CONFIG_FILENAME variables to match your setup.
setlocal
set PACKAGE_NAME=com.gopositron.voyagerdemo
set CONFIG_DIRNAME=Config
set CONFIG_FILENAME=InterfaceConfig.json

adb shell mkdir -p storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/
adb push %CONFIG_FILENAME% /storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/%CONFIG_FILENAME%

:: Set Permissions, otherwise Unity won't have access to read them or even check if File.Exists properly
:: 555 for CONFIG_DIRNAME and 444 for CONFIG_FILENAME would be more secure, but this makes removing and manipulating them easier if you're in adb shell
:: and we're not a web server
adb shell chmod 777 /storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/
adb shell chmod 666 /storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/%CONFIG_FILENAME%