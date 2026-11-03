# Ciro Uygulaması Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Günlük nakit/kredi kartı/havale cirosunu giren, iki sayfalı Excel rapor üreten, e-posta/WhatsApp ile gönderen ve yedek alıp aktaran Windows + Android uygulaması.

**Architecture:** Tüm iş mantığı (model, SQLite, Excel, yedek, e-posta, taslak düzenleme durumu) `CiroApp.Core` sınıf kütüphanesinde ve xunit ile test edilir. `CiroApp` (.NET MAUI Blazor Hybrid + MudBlazor) yalnızca ekranlar ve platform servisleridir (dosya, paylaşım, güvenli depolama).

**Tech Stack:** .NET (kurulu SDK'nın LTS sürümü; `dotnet --version` ile doğrula) + MAUI iş yükü, MudBlazor, sqlite-net-pcl, ClosedXML, MailKit, xunit.

**Spec:** `docs/superpowers/specs/2026-11-01-ciro-uygulamasi-design.md`

## Global Constraints

- Hedef platformlar yalnızca Windows ve Android. iOS/macOS hedefi eklenmez.
- Proje konumu: `C:\Users\birol\source\repos\CiroApp`. LawAstCrm deposuna dokunulmaz.
- Para: `decimal`, iki ondalık, negatif kabul edilmez, TL simgesi yok, binlik ayraçlı.
- Tarih biçimi `d.M.yyyy` (örn. `1.10.2026`), Türkçe ay adları (Ocak … Aralık), kültür `tr-TR`.
- Excel iki sayfa: `Ciro` ve `Havale-EFT`. Dosya adı: tam ay → `2026 Ekim Cirosu.xlsx`; başka aralık → `15.10.2026 - 10.11.2026 Cirosu.xlsx`.
- Havale toplamı hiçbir yerde elle girilmez; her zaman havale kayıtlarının toplamıdır.
- Yedek dosyası yalnızca günler ve havaleleri içerir; ayarlar, parola, alıcılar girmez.
- Kullanıcı açıkça "commit at" demedikçe commit atılmaz; bu planda commit adımı yoktur.
- Kullanıcıya görünen tüm metinler Türkçe. "Buton" yerine "düğme" denir.
- Mimari: katmanlı temiz mimari (bkz. File Structure). Domain pakete bağlı değildir; UI, Infrastructure sınıflarını doğrudan `new`lemez, yalnızca DI'dan alır. Mimari kuralı bir testle korunur (Task 1): Domain'in ve Application'ın proje referansları kontrol edilir.
- Dağıtım: Windows'ta paketlenmemiş (MSIX değil) exe, Android'de sideload APK.

## Review Focus

1. Ondalık girişi: kullanıcı `12,50` yazar (virgül); nokta ve virgül karışımı veya `1.250,75` doğru okunmalı, yanlış okunup 100 kat yanlış tutar yazılmamalı. (Task 9)
2. Excel formül enjeksiyonu: kişi adı `=1+1` veya `+cmd` ile başlıyorsa Excel'de formül değil düz metin olmalı. (Task 4)
3. Aralık kenarları: başlangıç > bitiş reddedilir; yıl geçişi (`15.12.2026 - 10.01.2027`) ve 29 Şubat doğru gün sayısı verir; hiç verisi olmayan aralık boş ama geçerli dosya üretir. (Task 3, 10)
4. Bozuk veya başka sürüm yedek dosyası: uygulama çökmez, "dosya okunamadı" der, mevcut veriye dokunmaz. (Task 5)
5. Aynı gün için Yeni Gün Ekle: mevcut günü düzenlemeye açar, ikinci kayıt oluşturmaz. (Task 8)

---

## File Structure — Katmanlı temiz mimari (kullanıcı kararı)

Bağımlılık hep içe doğrudur: `UI → Infrastructure → Application → Domain`. Domain hiçbir pakete bağlı değildir; Application yalnızca Domain'e; arayüzler (port) Application'da, gerçeklemeler Infrastructure'da; UI yalnızca Application servislerini ve arayüzlerini bilir. Bağlantı `MauiProgram.cs` içindeki DI kaydında yapılır.

Aşağıdaki görevlerde "Core" geçen her yol, tablodaki katman projesine okunur:

| Proje | İçerik (görev yolundaki karşılığı) |
|---|---|
| `src/CiroApp.Domain` | `Models/CiroDay`, `HavaleEntry`, `Reports/ReportRange`, `ReportNaming`, `Formatting/TrFormat` (Task 1, 3) |
| `src/CiroApp.Application` | Portlar: `ICiroRepository`, `IRecipientRepository`, `IReportBuilder` (`byte[] Build(ReportRange, IReadOnlyList<CiroDay>)`), `IEmailSender`, `ISettingsStore`, `IFileService`. Kullanım senaryoları: `DayService`, `ReportService`, `BackupService`, `ImportPlanner`, `EmailReportService`, `DayEditorState` (Task 5, 6, 7'nin `SmtpSettings`/`SmtpPresets` kısmı) |
| `src/CiroApp.Infrastructure` | `CiroRepository`, `RecipientRepository` (Task 2), `ExcelReportBuilder : IReportBuilder` (Task 4), `MailKitEmailSender` (Task 7) |
| `src/CiroApp` (MAUI) | `Platform/FileService`, `SecureSettingsStore`, sayfalar (Task 8–11) |
| `tests/CiroApp.Tests` | Tüm katmanlara referans verir; testler katman klasörleriyle adlandırılır |

Application servis imzaları (UI yalnızca bunları çağırır, depoları doğrudan çağırmaz):
- `DayService(ICiroRepository)`: `Task<IReadOnlyList<CiroDay>> GetMonthAsync(int year, int month)`, `Task<CiroDay?> GetAsync(DateOnly)`, `Task SaveAsync(CiroDay)`, `Task DeleteAsync(DateOnly)`
- `ReportService(ICiroRepository, IReportBuilder)`: `Task<ReportFile> BuildAsync(ReportRange)`; `record ReportFile(string FileName, byte[] Content)` (adı `ReportNaming.FileName` verir)
- `EmailReportService(ReportService, IEmailSender, ISettingsStore, IRecipientRepository)`: `Task SendAsync(ReportRange, IReadOnlyList<string> to)` — ayar yoksa `EmailSettingsMissingException`; gönderim başarılıysa alıcılar `AddIfMissingAsync` ile eklenir
- `BackupService(ICiroRepository)`: `Task<byte[]> ExportAsync()`, `Task<ImportPlan> PlanImportAsync(byte[] data)`, `Task ApplyImportAsync(ImportPlan, Func<ImportConflict,bool> useImported)`

Task 5'teki statik `BackupService`/`ApplyAsync` imzaları bu servisin iç yardımcısı olur; dış imza yukarıdakidir.

```
CiroApp/
  CiroApp.sln
  src/CiroApp.Domain/  src/CiroApp.Application/  src/CiroApp.Infrastructure/
  src/CiroApp/         # MAUI Blazor Hybrid (Windows + Android)
    Platform/FileService.cs, SecureSettingsStore.cs
    Pages/Home.razor, DayEditor.razor, HavaleDialog.razor, Report.razor, Settings.razor, Backup.razor
  tests/CiroApp.Tests/
```

---

### Task 1: Çözüm iskeleti, modeller, Türkçe biçimleme

**Files:**
- Create: `CiroApp.sln`, `src/CiroApp.Core/CiroApp.Core.csproj`, `tests/CiroApp.Tests/CiroApp.Tests.csproj`
- Create: `src/CiroApp.Core/Models/CiroDay.cs`, `Models/HavaleEntry.cs`, `Formatting/TrFormat.cs`
- Test: `tests/CiroApp.Tests/TrFormatTests.cs`, `CiroDayTests.cs`

**Interfaces:**
- Produces:
  - `sealed class HavaleEntry { int Id; decimal Tutar; string KisiAdi }`
  - `sealed class CiroDay { int Id; DateOnly Tarih; decimal Nakit; decimal KrediKarti; List<HavaleEntry> Havaleler; decimal HavaleToplam; decimal GunlukToplam }` (iki toplam hesaplanır: `HavaleToplam` = havale tutarları toplamı, `GunlukToplam` = Nakit + KrediKarti + HavaleToplam)
  - `static class TrFormat { string Tarih(DateOnly); string AyAdi(int ay); string YilAy(DateOnly); bool TryParseTutar(string, out decimal) }`

- [ ] **Step 1:** `dotnet new sln`, Core sınıf kütüphanesi ve xunit test projesi oluştur, Core'u teste referansla; MAUI projesi bu task'ta oluşturulmaz.
- [ ] **Step 2: Failing tests** — `TrFormat.Tarih(new DateOnly(2026,10,1)) == "1.10.2026"`; `TrFormat.YilAy(new DateOnly(2026,10,15)) == "2026 Ekim"`; `TryParseTutar("12,50")` → 12.50; `"1.250,75"` → 1250.75; `"12.50"` → false (belirsiz biçim reddedilir); `"-5"` → false; `""` → false. `CiroDay` ile 10/10 ve havaleler 10 → `GunlukToplam == 30`.
- [ ] **Step 3:** Çalıştır: `dotnet test` — FAIL (tipler yok).
- [ ] **Step 4:** Modelleri ve `TrFormat`'ı uygula (`CultureInfo("tr-TR")`, ay adları sabit dizi).
- [ ] **Step 5:** `dotnet test` — PASS.

### Task 2: SQLite depoları

**Files:**
- Create: `Data/ICiroRepository.cs`, `Data/CiroRepository.cs`, `Data/IRecipientRepository.cs`, `Data/RecipientRepository.cs`
- Test: `tests/CiroApp.Tests/CiroRepositoryTests.cs`

**Interfaces:**
- Consumes: `CiroDay`, `HavaleEntry` (Task 1)
- Produces:
  - `ICiroRepository { Task<CiroDay?> GetAsync(DateOnly); Task<IReadOnlyList<CiroDay>> GetRangeAsync(DateOnly from, DateOnly to); Task<IReadOnlyList<CiroDay>> GetAllAsync(); Task SaveAsync(CiroDay day); Task DeleteAsync(DateOnly) }` — `SaveAsync` aynı tarihte kayıt varsa günü ve havalelerini tek işlemde değiştirir (ikinci satır açmaz). Silme havaleleri de siler. Sonuçlar tarihe göre artan.
  - `CiroRepository(string dbPath)`; tarih `yyyy-MM-dd` metni olarak saklanır.
  - `IRecipientRepository { Task<IReadOnlyList<string>> GetAllAsync(); Task AddIfMissingAsync(string email); Task RemoveAsync(string email) }` — büyük/küçük harf duyarsız tekilleştirir.

- [ ] **Step 1: Failing tests** (geçici dosya DB): aynı tarihe iki kez `SaveAsync` → `GetAllAsync().Count == 1` ve ikinci kaydın havaleleri geçerli; `DeleteAsync` sonrası `GetAsync` null ve havaleler kalmaz; `GetRangeAsync(1.10, 31.10)` aralık dışını getirmez; `AddIfMissingAsync("A@x.com")` sonra `"a@x.com"` → tek kayıt.
- [ ] **Step 2:** Çalıştır, FAIL doğrula.
- [ ] **Step 3:** `sqlite-net-pcl` ile uygula; havale tablosunda `CiroDayId` yabancı anahtar; `SaveAsync` `RunInTransactionAsync` içinde.
- [ ] **Step 4:** `dotnet test --filter CiroRepositoryTests` — PASS.

### Task 3: Rapor adlandırma ve aralık

**Files:**
- Create: `Reports/ReportRange.cs`, `Reports/ReportNaming.cs`
- Test: `tests/CiroApp.Tests/ReportNamingTests.cs`

**Interfaces:**
- Produces: `record ReportRange(DateOnly From, DateOnly To)` (From > To ise yapıcı `ArgumentException` fırlatır; `IEnumerable<DateOnly> Days()`; `bool IsFullMonth`); `static class ReportNaming { string Title(ReportRange); string FileName(ReportRange) }`.

- [ ] **Step 1: Failing tests:** 1.10.2026–31.10.2026 → `Title == "2026 Ekim"`, `FileName == "2026 Ekim Cirosu.xlsx"`; 15.10.2026–10.11.2026 → `"15.10.2026 - 10.11.2026"` ve `"15.10.2026 - 10.11.2026 Cirosu.xlsx"`; şubat 2028 tam ay → `Days().Count() == 29`; 15.12.2026–10.01.2027 → 27 gün; From > To → `ArgumentException`.
- [ ] **Step 2:** Çalıştır, FAIL.
- [ ] **Step 3:** Uygula.
- [ ] **Step 4:** PASS.

### Task 4: Excel rapor üretici (iki sayfa)

**Files:**
- Create: `Reports/ExcelReportBuilder.cs`
- Test: `tests/CiroApp.Tests/ExcelReportBuilderTests.cs`

**Interfaces:**
- Consumes: `CiroDay`, `ReportRange`, `ReportNaming.Title`
- Produces: `static class ExcelReportBuilder { byte[] Build(ReportRange range, IReadOnlyList<CiroDay> days) }`

**Düzen (spec'teki örnekler):**
- Sayfa `Ciro`: A1 `Title`; satır 2 başlıkları `Tarih, Nakit, Kredi kartı, Havale, Günlük Toplam`; satır 3'ten itibaren aralıktaki her gün bir satır (veri yoksa yalnız tarih); son satır A=`Genel Toplam`, B–E sütun toplamları. Tarih hücresi gerçek tarih, biçim `d.M.yyyy`.
- Sayfa `Havale-EFT`: A1 `Title`; satır 2: B=`Havale`, C=`Müşteri`; her gün için ilk havale tarihle aynı satırda (A=tarih, B=tutar, C=ad), sonrakiler A boş satırlarda; havalesi olan günde ardından C=`Günlük Toplam`, D=gün toplamı satırı; havalesiz gün yalnız tarih satırı; son satır A=`Genel Toplam`, B=tüm havaleler toplamı.
- Kişi adı her zaman düz metin olarak yazılır (formül değil).

- [ ] **Step 1: Failing tests** (`ClosedXML` ile geri okuyarak): Ciro için 1.10 (10/10/havale 10) ve 2.10 (5/12/havale 11), aralık 1–31 Ekim → A1 `"2026 Ekim"`, B3=10, E3=30, E4=28, Genel Toplam satırı (satır 33) B=15, C=22, D=21, E=58. Havale sayfası için 1.10 {10 Ahmet As, 12 Veli Ak}, 2.10 {10 Veli Veri, 12 Saim Bir, 12 Veli Ak}, 3.10 {15 Ahmet As, 26 Veli Ak, 36 Saim Bir} → A3=1.10.2026, B3=10, C3=`Ahmet As`, B4=12, A4 boş, C5=`Günlük Toplam`, D5=22; ikinci gün D=34; üçüncü gün D=77; Genel Toplam B=133. Ayrıca: kişi adı `=1+1` olan havalede ilgili hücre `HasFormula == false` ve değeri `"=1+1"`; verisi olmayan aralıkta dosya açılır, iki sayfa var.
- [ ] **Step 2:** Çalıştır, FAIL.
- [ ] **Step 3:** ClosedXML ile uygula; hücre değerleri `SetValue`/tipli atanır, metin için `XLDataType.Text`.
- [ ] **Step 4:** PASS.

### Task 5: Yedek dışa/içe aktarma ve birleştirme planı

**Files:**
- Create: `Backup/BackupFile.cs`, `Backup/BackupService.cs`, `Backup/ImportPlanner.cs`
- Test: `tests/CiroApp.Tests/BackupTests.cs`

**Interfaces:**
- Consumes: `CiroDay`, `ICiroRepository`
- Produces:
  - `static class BackupService { byte[] Export(IReadOnlyList<CiroDay> days); BackupFile Parse(byte[] data) }` — JSON, `{ "version": 1, "days": [...] }`; geçersiz/başka sürüm → `BackupFormatException`.
  - `record ImportConflict(CiroDay Local, CiroDay Imported)`; `record ImportPlan(IReadOnlyList<CiroDay> ToAdd, IReadOnlyList<ImportConflict> Conflicts)`
  - `static class ImportPlanner { ImportPlan Plan(IReadOnlyList<CiroDay> local, BackupFile file) }` — aynı tarihte nakit, kart veya havale listesi (tutar+ad, sıradan bağımsız) farklıysa çakışma; aynıysa hiçbiri; dosyada olup yerelde olmayan eklenir; yerel-only günlere dokunulmaz.
  - `static Task ApplyAsync(ICiroRepository repo, ImportPlan plan, Func<ImportConflict,bool> useImported)` — `ToAdd` kaydedilir, `useImported` true ise çakışmada dosyadaki gün (havaleleri dahil) yerel günün yerini alır.

- [ ] **Step 1: Failing tests:** Export→Parse gidiş-dönüş eşitliği; rastgele bayt dizisi ve `{"version":99}` → `BackupFormatException`; aynı gün farklı nakit → 1 çakışma; aynı gün aynı verilerle havaleler farklı sırada → çakışma yok; çakışmada `useImported=false` yerel kalır, `true` havaleler dosyadakine eşit olur; yerel-only gün `ApplyAsync` sonrası durur.
- [ ] **Step 2:** FAIL. **Step 3:** `System.Text.Json` ile uygula. **Step 4:** PASS.

### Task 6: Düzenleme taslağı (günlük form + havale formu mantığı)

**Files:**
- Create: `Editing/DayEditorState.cs`
- Test: `tests/CiroApp.Tests/DayEditorStateTests.cs`

**Interfaces:**
- Consumes: `CiroDay`, `HavaleEntry`
- Produces: `sealed class DayEditorState { DayEditorState(DateOnly tarih, CiroDay? existing); DateOnly Tarih; decimal Nakit {get;set;}; decimal KrediKarti {get;set;}; IReadOnlyList<HavaleEntry> Havaleler; decimal HavaleToplam; decimal GunlukToplam; void AddHavale(decimal tutar, string kisiAdi); void UpdateHavale(int index, decimal tutar, string kisiAdi); void RemoveHavale(int index); IReadOnlyList<string> Validate(); CiroDay ToDay() }`
- Taslak, `existing` nesnesini değiştirmez (kopya üzerinde çalışır); `Validate()` negatif tutar ve boş kişi adı için Türkçe hata mesajı döner.

- [ ] **Step 1: Failing tests:** `existing` verilince değerler kopyalanır; `AddHavale(10,"Ahmet As")`, `AddHavale(12,"Veli Ak")` → `HavaleToplam == 22`; taslakta değişiklik `existing`'i etkilemez (İptal = taslağı atmak); `Validate()` boş ad ve negatif nakit için hata döner; `ToDay()` Id'yi korur.
- [ ] **Step 2:** FAIL. **Step 3:** Uygula. **Step 4:** PASS.

### Task 7: E-posta ayarları, gönderici ve ayar deposu

**Files:**
- Create: `Email/SmtpSettings.cs`, `Email/ISettingsStore.cs`, `Email/IEmailSender.cs`, `Email/MailKitEmailSender.cs`
- Test: `tests/CiroApp.Tests/SmtpPresetTests.cs`, `EmailSenderTests.cs`

**Interfaces:**
- Produces:
  - `record SmtpSettings(string Provider, string Host, int Port, bool UseSsl, string Email, string Password)`; `static class SmtpPresets { SmtpSettings For(string provider, string email, string password) }` — Gmail: `smtp.gmail.com`, 465, SSL; Outlook: `smtp-mail.outlook.com`, 587, StartTls; Diğer: boş host, elle girilir.
  - `ISettingsStore { Task<SmtpSettings?> LoadAsync(); Task SaveAsync(SmtpSettings s) }` (Core'da arayüz; MAUI uygulaması parolayı `SecureStorage`'a yazar).
  - `IEmailSender { Task SendReportAsync(SmtpSettings s, IReadOnlyList<string> to, string subject, string fileName, byte[] xlsx); Task SendTestAsync(SmtpSettings s) }` — hata durumunda `EmailSendException` (Türkçe, kullanıcıya gösterilir; kimlik doğrulama hatasında Gmail için "uygulama parolası" ipucu içerir).
  - `MailKitEmailSender : IEmailSender`.

- [ ] **Step 1: Failing tests:** Gmail ön ayarı host/port/SSL değerleri; Outlook ön ayarı; `MailKitEmailSender` boş alıcı listesiyle `ArgumentException` fırlatır; yanlış host → `EmailSendException` (gerçek ağ olmadan, geçersiz ana makine adıyla).
- [ ] **Step 2:** FAIL. **Step 3:** Uygula (MailKit `SmtpClient`, ek olarak xlsx MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`). **Step 4:** PASS.
- [ ] **Step 5:** Elle doğrulama (otomatik değil): Task 11'den sonra gerçek Gmail uygulama parolasıyla test e-postası gider.

### Task 8: MAUI Blazor uygulaması, MudBlazor, platform servisleri, ana liste

**Files:**
- Create: `src/CiroApp/` (MAUI Blazor Hybrid, hedefler: `net…-windows10.0.19041.0` ve `net…-android` yalnızca), `Platform/IFileService.cs`, `Platform/FileService.cs`, `Platform/SecureSettingsStore.cs`, `Pages/Home.razor`, `MauiProgram.cs` içinde DI
- Test: Core testleri değişmez; UI burada çalıştırarak doğrulanır.

**Interfaces:**
- Produces: `IFileService { Task<string?> SaveAsync(string suggestedName, byte[] data); Task<byte[]?> PickFileAsync(); Task ShareAsync(string fileName, byte[] data); bool CanShare }` — Windows: `CanShare=false`, `SaveAsync` klasör/dosya seçtirir; Android: Paylaş menüsü ve kaydetme. `SecureSettingsStore : ISettingsStore` parolayı `SecureStorage`'da tutar, geri kalanı `Preferences`'ta.
- DI: `ICiroRepository`, `IRecipientRepository` (db yolu `FileSystem.AppDataDirectory/ciro.db3`), `IEmailSender`, `ISettingsStore`, `IFileService`, MudBlazor servisleri.

- [ ] **Step 1:** MAUI Blazor Hybrid projesini oluştur, iOS/MacCatalyst hedeflerini projeden çıkar, MudBlazor ekle, tema/düzenleme (üstte menü: Ana Liste, Rapor, Yedek, Ayarlar), `CiroApp.Core`'a referans ver.
- [ ] **Step 2:** `Home.razor`: ay seçimi (varsayılan içinde bulunulan ay), **Yeni Gün Ekle**, tablo sütunları `Tarih, Nakit, Kredi kartı, Havale, Günlük Toplam` ve satır başına **Düzenle** / **Sil**. Dar ekranda (telefon) kart görünümü (`MudHidden` / `Breakpoint`). **Sil** onay penceresi gösterir ("Bu günün havale kayıtları da silinecek").
- [ ] **Step 3:** **Yeni Gün Ekle** seçilen tarihte kayıt varsa o günü düzenlemeye açar (yeni kayıt oluşturmaz).
- [ ] **Step 4:** Doğrula: `dotnet build src/CiroApp -f net…-windows10.0.19041.0` başarılı; uygulama açılır, boş liste görünür. Android için `dotnet build -f net…-android` başarılı.

### Task 9: Günlük giriş formu ve havale formu

**Files:**
- Create: `Pages/DayEditor.razor`, `Pages/HavaleDialog.razor`

**Interfaces:**
- Consumes: `DayEditorState` (Task 6), `ICiroRepository`, `TrFormat.TryParseTutar`

- [ ] **Step 1:** `DayEditor.razor`: tarih seçici (düzenlemede kilitli), Nakit ve Kredi kartı tutar alanları (`tr-TR` kültürü; metin alanı + `TrFormat.TryParseTutar`, geçersizse alan altında Türkçe hata), salt-okunur **Havale toplamı** ve yanında **Düzenle**, salt-okunur **Günlük Toplam**, **Kaydet** ve **İptal**.
- [ ] **Step 2:** `HavaleDialog.razor`: aynı `DayEditorState` üzerinde havale satırlarını ekle/değiştir/sil (tutar + kişi adı), üstte seçili tarih, altta havale toplamı; kapatınca günlük formdaki toplam güncellenir; `Validate()` hatalarını gösterir.
- [ ] **Step 3:** **Kaydet** `Validate()` boşsa `ToDay()` → `SaveAsync`; **İptal** taslağı atar (veritabanına dokunmaz).
- [ ] **Step 4:** Elle doğrulama: 1.10.2026 günü ekle (10 nakit, 10 kart, havale 10) → listede `30`; düzenle, havale ekle, İptal ile çık → değişmedi; `12,50` yaz → 12,50 kaydolur; `12.50` yaz → hata mesajı.

### Task 10: Rapor ekranı (kaydet, e-posta, WhatsApp)

**Files:**
- Create: `Pages/Report.razor`

**Interfaces:**
- Consumes: `ExcelReportBuilder.Build`, `ReportNaming.FileName`, `IFileService`, `IEmailSender`, `IRecipientRepository`, `ISettingsStore`

- [ ] **Step 1:** İki tarih seçici (varsayılan: içinde bulunulan ayın ilk–son günü); başlangıç > bitiş ise düğmeler pasif ve Türkçe uyarı.
- [ ] **Step 2:** **Dosyaya Kaydet:** raporu üretip `IFileService.SaveAsync(FileName, bytes)`; dosya zaten varsa üzerine yazma sorulur.
- [ ] **Step 3:** **E-posta Gönder:** pencere; kayıtlı alıcılar işaretlenebilir (çoklu), yeni adres yazılabilir; ayar yoksa "Önce Ayarlar'dan e-posta hesabını girin" uyarısı. Başarılı gönderimde yeni adresler `AddIfMissingAsync` ile eklenir; hata `EmailSendException` mesajıyla gösterilir.
- [ ] **Step 4:** **WhatsApp ile Gönder:** yalnız `IFileService.CanShare` true ise görünür; `ShareAsync` çağırır.
- [ ] **Step 5:** Elle doğrulama: Windows'ta kaydedilen dosya Excel'de açılınca iki sayfa ve Task 4'teki düzen görünür; 15.12.2026–10.01.2027 aralığı hata vermeden dosya üretir; Android'de WhatsApp paylaşımı açılır.

### Task 11: Ayarlar ve Yedek ekranları

**Files:**
- Create: `Pages/Settings.razor`, `Pages/Backup.razor`

**Interfaces:**
- Consumes: `SmtpPresets`, `ISettingsStore`, `IEmailSender`, `IRecipientRepository`, `BackupService`, `ImportPlanner`, `IFileService`

- [ ] **Step 1:** `Settings.razor`: sağlayıcı seçimi (Gmail/Outlook/Diğer; seçince host/port/SSL dolar, "Diğer"de elle), e-posta, parola (maskeli), kısa açıklama kutusu ("Gmail için normal parola yerine 2 adımlı doğrulama açıp 'uygulama parolası' oluşturun"), **Kaydet**, **Test e-postası gönder** (sonuç/hata görünür); kayıtlı alıcı listesi ve her satırda **Sil**.
- [ ] **Step 2:** `Backup.razor`: **Dışa Aktar** (`Export` → `SaveAsync("Ciro Yedek yyyy-MM-dd.json")`); **İçe Aktar** (`PickFileAsync` → `Parse`; `BackupFormatException` olursa "Dosya okunamadı", veri değişmez). Çakışma yoksa özet ("N gün eklendi"). Çakışma varsa her biri için yan yana yerel/dosya değerleri ve "Dosyadakini kullan" / "Cihazdakini koru" seçimi, sonunda **Uygula**.
- [ ] **Step 3:** Elle doğrulama: Windows'ta dışa aktar → Android'de içe aktar; aynı güne farklı değer verip çakışma sorusunun geldiğini gör; bozuk bir `.json` seç → çökmeden uyarı.

### Task 12: Dağıtım ve uçtan uca doğrulama

**Files:**
- Create: `docs/YAYIN.md` (Windows exe ve APK üretim komutları)

- [ ] **Step 1:** Windows: `dotnet publish src/CiroApp -f net…-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true --self-contained`; çıkan klasördeki exe çift tıklayınca çalışır.
- [ ] **Step 2:** Android: `dotnet publish src/CiroApp -f net…-android -c Release -p:AndroidPackageFormat=apk`; APK telefona yüklenir (bilinmeyen kaynak izni gerekir).
- [ ] **Step 3:** Uçtan uca kontrol listesi, her iki platformda: gün ekle → havale ekle → listede toplam doğru → rapor (2 sayfa, spec örneğiyle aynı düzen) → dosyaya kaydet → e-posta ile gönder → dışa/içe aktar.
- [ ] **Step 4:** `dotnet test` tüm test projesi PASS; sonucu kullanıcıya bildir.
