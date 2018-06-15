:: Push a VoyagerDevice config file to your target Andriod device over ADB.
:: Place the config file (ex: InterfaceConfig.json) in the same folder as this .bat.
:: Set the PACKAGE_NAME, CONFIG_DIRNAME, and CONFIG_FILENAME variables to match your setup.
setlocal
set PACKAGE_NAME=com.gopositron.voyagerdemo
set CONFIG_DIRNAME=Config
set CONFIG_FILENAME=InterfaceConfig.json

adb shell mkdir -p storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/
adb push %CONFIG_FILENAME% /storage/emulated/0/Android/data/%PACKAGE_NAME%/files/%CONFIG_DIRNAME%/%CONFIG_FILENAME%