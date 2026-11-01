# Сборка ArmorSim.ipa в GitHub Actions

Workflow: `.github/workflows/ios.yml`, запуск вручную (`workflow_dispatch`). Локально `.ipa` не собираем.

Цепочка: macOS-раннер → Unity 6000.3.11f1 + iOS Build Support → `ArmorSimBuild.BuildiOS` → Xcode-проект → `xcodebuild archive` → `xcodebuild -exportArchive` → artifact `ArmorSim-iOS`.

## Secrets

Unity:
- `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_SERIAL` — лицензия Unity для GameCI.

Подпись iOS:
- `IOS_CERTIFICATE_BASE64` — base64 `.p12` с Apple Development/Distribution certificate и private key.
- `IOS_CERTIFICATE_PASSWORD` — пароль `.p12`.
- `IOS_PROVISION_PROFILE_BASE64` — base64 `.mobileprovision` под bundle id и нужные устройства (для ad-hoc/development).
- `IOS_TEAM_ID` — 10-символьный Apple Team ID.
- `IOS_BUNDLE_ID` — например `com.galimovdynamics.armorsim` (должен совпадать с provisioning profile).
- `IOS_EXPORT_METHOD` — `development`, `ad-hoc` или `app-store-connect`; по умолчанию `ad-hoc`.

Для установки на конкретный iPhone без App Store нужен development/ad-hoc профиль, содержащий UDID этого iPhone.

## Статус
Это заготовка pipeline. Она станет рабочей после восстановления игровых скриптов и проверки Unity-проекта. До первого реального прогона возможны правки под конкретную версию Unity/Xcode и Bundle ID.
