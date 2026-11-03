# SDD ledger — plan: docs/superpowers/plans/2026-11-01-ciro-uygulamasi.md
Pre-flight: Task 4/5/7 ports Application'da (katmanlı mimari kararı); git yok, commit yok (CLAUDE.md: onaysız commit atma).
Ruling: CiroApp git deposu değil ve commit atılmıyor — kullanıcının kalıcı "onaysız commit atma" kuralı — scriptler (task-done vb.) kullanılmıyor, ledger elle tutuluyor.
Ruling: .NET 10 (kurulu SDK 10.0.402), çözüm dosyası .slnx — dotnet new varsayılanı — maliyeti: yok.
Task 1: complete (tests: dotnet test tests/CiroApp.Tests → 15/15 pass)
Task 2: complete (tests: dotnet test tests/CiroApp.Tests → 19/19 pass; repolar IAsyncDisposable eklendi — test dosya kilidi için)
Task 3: complete (tests: dotnet test tests/CiroApp.Tests → 24/24 pass)
Task 4: Ruling: Ciro sayfası Genel Toplam satırı 34 (başlık 1 + üst başlık 2 + 31 gün = 33; plan 33 yazmıştı) — satır sayımı hatası — maliyet: yok
Task 4: Ruling: sayfa adı "Havale/EFT" yerine "Havale-EFT" — Excel sayfa adında "/" yasak (ClosedXML ArgumentException) — maliyet: kullanıcı farklı ad isterse tek sabit değişir
Task 4: complete (tests: dotnet test tests/CiroApp.Tests → 31/31 pass)
Task 5: complete (tests: dotnet test tests/CiroApp.Tests → 40/40 pass; BackupSerializer/ImportPlanner/BackupService Application'da — mimari kararı)
Task 6: complete (tests: dotnet test tests/CiroApp.Tests → 45/45 pass; RED ortam kurulumu yüzünden gözlenemedi, testler sonradan yeşil doğrulandı)
Task 7: complete (tests: dotnet test tests/CiroApp.Tests → 53/53 pass; ISettingsStore/IFileService/IEmailSender Application'da; DayService/ReportService/EmailReportService eklendi)
Task 8: Windows build OK (0 hata); Ruling: App sınıfı Microsoft.Maui.Controls.Application tam adıyla — CiroApp.Application ad alanı çakışması; Ruling: CommunityToolkit.Maui FileSaver ile dosya kaydı — Windows klasör seçimi için; Android derlemesi doğrulanıyor
Task 9-11: complete (Windows build 0 hata; ekranlar yazıldı: DayEditor, HavaleDialog, Report, EmailDialog, Settings, Backup; elle UI doğrulaması YAPILMADI). Ruling: Dosya varsa üzerine yazma sorusunu işletim sisteminin kaydet penceresi yapar — FileSaver — maliyet: uygulama kendi sorusunu sormaz. Ruling: içe aktarmada varsayılan seçim 'Cihazdakini koru' — veri kaybını önlemek — maliyet: yok. Android SDK kurulu değil (XA5300) — Android derleme/APK doğrulanamadı.
Final: fixed BackupSerializer null içerikte çökme — Parse_IcindeNullOlanDosyayiCokmedenReddeder RED→GREEN, suite 58/58
Final: fixed MailKit boş sunucu / bozuk adres EmailSendException; StartTls zorunlu (düz metin parola riski) — 2 test RED→GREEN
Final: fixed (UI, otomatik test yok — Ruling: UI testi altyapısı kurulmadı, elle doğrulama bekliyor) ErrorBoundary + try/catch (Report, Backup, Settings, DayEditor), canlı Günlük Toplam, FileService gerçek hatayı fırlatır, Android WRITE_EXTERNAL_STORAGE(<=28), SecureStorage okuma try/catch, EmailDialog yazılı adres, HavaleDialog satır no, InvariantCulture URL
Final: minor (deferred): kaydedilmemiş değişiklik uyarısı (NavigationLock) / geri tuşu
Final: minor (deferred): düzenleme sonrası liste cari aya döner (ay query string)
Final: minor (deferred): içe aktarmada veri doğrulama (negatif/çift tarih) ve tek işlemde uygulama
Final: minor (deferred): UI doğrudan IRecipientRepository/ISettingsStore/IEmailSender enjekte ediyor (servis sarmalayıcı yok)
Final: minor (deferred): çakışma ekranı yalnız toplamları gösteriyor, havale satırlarını göstermiyor
Final: minor (deferred): allowBackup=true — SecureStorage dosyası yedek kurallarından dışlanmadı
Final: açık soru: Havale-EFT sayfası A2'de "Tarih" başlığı yok (kullanıcının örneğinde de yok)

Task 12: complete (Android SDK C:\Android\sdk + JDK C:\Android\jdk kuruldu; Android Release APK uretildi publish/android/com.birol.ciroapp-Signed.apk 46 MB; docs/YAYIN.md yazildi). Ruling: sistem JDK 21 surum dizesi (21.0.13.0.2) Android araci tarafindan okunamadi - araca ayri JDK kurduruldu, maliyet: ~300 MB disk.

Final: fixed Havale-EFT A2 'Tarih' basligi (RED->GREEN), ice aktarimda negatif tutar/ayni tarih reddi (test eklendi, RED gozlenmedi), allowBackup=false, duzenleme sonrasi liste ilgili aya doner (?ay=), kaydedilmemis degisiklik uyarisi (NavigationLock), cakisma ekraninda havale satirlari; suite 61/61, Windows build 0 hata. UI degisiklikleri elle dogrulanmadi. Kalan minor: ice aktarma tek islemde degil, UI dogrudan port enjekte ediyor.

Final: fixed UI dogrudan port enjekte ediyor - EmailSettingsService + RecipientService eklendi, Settings/EmailDialog servislere baglandi, dogrulama kurallari Application'da testli (10 yeni test, RED derleme hatasi gozlendi); suite 71/71, Windows build 0 hata. UI elle dogrulanmadi.

Final: fixed ice aktarma tek islemde degil - ICiroRepository.SaveManyAsync (tek transaction), BackupService tek cagri; 3 test (depo atomiklik, ApplyImport atomiklik, toplu kayit) RED(derleme hatasi)->GREEN, suite 74/74, Windows build 0 hata. Kalan minor yok (UI elle dogrulama haric).

Istek: Rapor sayfasi (tarih araligi + Sorgula/Dosyaya Kaydet/E-Posta yan yana, gun gun liste, duzenle/sil), ortak DayList bileseni (Home+Rapor), DayEditor tarih secici + ?don= donus adresi, DayService.GetRangeAsync (RED->GREEN), gri 0,00 ipucu. suite 75/75, Windows build 0 hata. UI elle dogrulanmadi. publish/ klasorleri eski.
