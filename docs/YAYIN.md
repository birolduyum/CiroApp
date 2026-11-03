# Ciro Uygulaması — Yayın ve Kurulum

## Windows (exe)

Derleme (self-contained, kurulumsuz):

```
dotnet publish src/CiroApp -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:UseMonoRuntime=false --self-contained -r win-x64 -o publish/windows
```

`publish/windows` klasörünü olduğu gibi kopyalayın, `CiroApp.exe` çalıştırılır. Veritabanı
`%LOCALAPPDATA%\User Name\com.birol.ciroapp\Data\ciro.db3` altında tutulur.

## Android (APK)

Gereksinimler (bir kez): Android SDK ve JDK, yönetici yetkisi gerektirmeyen yollara kurulur:

```
dotnet build src/CiroApp -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory=C:\Android\sdk -p:JavaSdkDirectory=C:\Android\jdk -p:AcceptAndroidSDKLicenses=True
```

Not: Sistemde kurulu Microsoft OpenJDK 21'in sürüm numarası (`21.0.13.0.2`) Android derleme
aracı tarafından okunamıyor; bu yüzden JDK `C:\Android\jdk` altına araç tarafından ayrıca kurulur.

APK üretimi:

```
dotnet publish src/CiroApp -f net10.0-android -c Release -p:AndroidSdkDirectory=C:\Android\sdk -p:JavaSdkDirectory=C:\Android\jdk -p:AndroidPackageFormat=apk -o publish/android
```

Çıktı: `publish/android/com.birol.ciroapp-Signed.apk`. Telefona kopyalayıp açın
("Bilinmeyen kaynaklardan yükleme" izni gerekir). Bu APK geliştirme anahtarıyla imzalıdır;
Play Store'a yüklenecekse kendi keystore'unuzla imzalanmalıdır.

## Henüz doğrulanmayanlar

- Arayüz ekranları elle gezilmedi (Windows ve Android).
- Gerçek bir Gmail/Outlook hesabıyla test e-postası gönderilmedi.
- WhatsApp paylaşımı telefonda denenmedi.
- Android APK derlendi ancak bir cihazda/emülatörde çalıştırılmadı.
