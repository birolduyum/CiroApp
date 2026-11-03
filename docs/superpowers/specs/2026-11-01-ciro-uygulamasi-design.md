# Ciro Uygulaması — Tasarım

Tarih: 2026-11-01 · Durum: Onay bekliyor

## Amaç ve neden

Günlük ciro (nakit, kredi kartı, havale/EFT) şu an Excel'de elle tutuluyor. Amaç: aynı tabloyu üreten, hem Windows'ta (exe) hem telefonda çalışan, günlük girişi hızlı yapan, aylık veya iki tarih arası Excel rapor üreten ve bu raporu dosyaya kaydedip e-posta / WhatsApp ile gönderebilen bir uygulama.

Tek kullanıcılı bir araçtır. Kullanıcı: işletme sahibi (Birol).

## Hedefler

- Günlük nakit ve kredi kartı toplamını girmek.
- Havaleleri o gün içinde tek tek (tutar + gönderen kişi adı) girmek.
- Günleri tek listede görmek, düzenlemek, silmek.
- İki tarih arası iki sayfalı Excel rapor üretmek.
- Raporu dosyaya kaydetmek, uygulamanın kendisinden e-posta ile göndermek, telefonda WhatsApp'a paylaşmak.
- Veriyi dışa/içe aktararak telefon ile bilgisayar arasında taşımak ve yedeklemek.
- İnternet veya sunucu olmadan çalışmak (e-posta gönderimi hariç).

## Hedef dışı

- Cihazlar arası otomatik eşitleme, bulut, çok kullanıcı, giriş/yetki.
- Müşteri/cari takibi, fatura, KDV, gider takibi (yalnız ciro).
- Outlook'un OAuth girişi (ilk sürümde yok; bkz. E-posta).
- iOS ve macOS. Kullanıcı kararı: yalnızca Windows ve Android hedeflenir; iPhone için ayrıca hazırlık yapılmaz.

## Kaçınılacak durumlar (anti-hedef)

- Havale toplamının iki yerde elle tutulup birbiriyle çelişmesi.
- Silme veya içe aktarmada kullanıcının uyarılmadan veri kaybetmesi.
- Excel çıktısının kullanıcının verdiği örnek düzenden sapması.

## Kullanıcının kararları

### Ana liste (ana form)
- Her gün bir satır. Sütunlar: Tarih, Nakit, Kredi kartı, Havale, Günlük Toplam.
- Her satırda **Düzenle** ve **Sil** düğmeleri. Üstte ay seçimi ve **Yeni Gün Ekle**.
- Sil, o günün havalelerini de siler.

### Günlük giriş formu
- Tarih seçilir; nakit ve kredi kartı toplamı girilir.
- Havale toplamı formda gösterilir ama elle yazılmaz (havale kayıtlarının toplamıdır).
- Havale toplamının yanında **Düzenle** düğmesi havale formunu o tarih için açar.

### Havale formu
- Seçili tarihin havalelerini tek tek ekleme, değiştirme, silme. Alanlar: tutar, kişi adı.
- Formda o günün havale toplamı görünür; kapanınca günlük forma yansır.

### Excel rapor
- Kullanıcı iki tarih seçer; dosya iki sayfadan oluşur:
  1. **Ciro** — sütunlar: Tarih, Nakit, Kredi kartı, Havale, Günlük Toplam. Seçilen aralıktaki her gün bir satır (verisi olmayan günler boş satır), en altta Genel Toplam. Üstte "2026 Ekim" gibi dönem başlığı.
  2. **Havale-EFT** (sayfa adında "/" Excel tarafından yasak) — gün başına: tarih, her havale için tutar + müşteri adı, ardından "Günlük Toplam". Boş günler tarih satırı olarak yer alır. En altta Genel Toplam.
- Dosya adı: `<yıl> <ay adı> Cirosu.xlsx` (örn. `2026 Ekim Cirosu.xlsx`).
- Tarih biçimi `1.10.2026`, Türkçe ay adları.
- Örnek düzen kullanıcının gönderdiği iki tablodur; sapma olmayacak.
- **Ciro sayfası düzeni (kullanıcının örneği):** A1 hücresinde dönem başlığı ("2026 Ekim"); 2. satırda başlıklar (Tarih, Nakit, Kredi kartı, Havale, Günlük Toplam); sonra her gün için bir satır; en altta "Genel Toplam" satırı, Nakit/Kredi kartı/Havale/Günlük Toplam sütunlarının toplamıyla.
- **Havale/EFT sayfası düzeni (kullanıcının örneği):** A1'de dönem başlığı. Sütunlar: A Tarih, B Havale (tutar), C Müşteri (kişi adı), D günlük toplam. Her günün ilk havalesi tarihle aynı satırda başlar, sonraki havaleler tarih sütunu boş satırlarda devam eder. Havaleler bittikten sonra C sütununda "Günlük Toplam", D sütununda o günün toplamı yazılan ayrı bir satır gelir. Havalesi olmayan günler yalnızca tarih satırıdır (günlük toplam satırı yok). En altta "Genel Toplam" satırı, B sütununda tüm havalelerin toplamı.
- Ciro sayfasındaki Havale sütunu, Havale/EFT sayfasındaki o günün günlük toplamıyla aynıdır.

### Rapor dağıtımı
- **Dosyaya kaydet:** Windows'ta klasör seçimi, telefonda cihaza kayıt.
- **E-posta:** Uygulamanın kendisi gönderir. Ayarlar ekranında sağlayıcı (Gmail / Outlook / Diğer) seçilir, sunucu ve port otomatik dolar ("Diğer"de elle), adres ve parola girilir, "Test e-postası gönder" düğmesi vardır. Gönderim her zaman bu kayıtlı hesapla yapılır. Parola cihazın güvenli depolamasında tutulur.
- **Alıcılar:** Gönderim penceresinde daha önce kullanılmış alıcılar işaretlenebilir; yeni adres de yazılabilir. Başarılı gönderimde yeni adres listeye kendiliğinden eklenir.
- **WhatsApp:** Yalnız telefonda, işletim sisteminin paylaş menüsü üzerinden.

### Dışa / içe aktarma
- Dışa aktar: tüm veriyi tek yedek dosyası olarak kaydeder.
- İçe aktar: dosyadan veriyi yükler. Aynı tarihte iki farklı kayıt çıkarsa **her çakışma için** kullanıcıya hangisinin kalacağı sorulur (bu karar kullanıcıya önerildi, itiraz gelmedi).

## Benim kararlarım (kullanıcı kontrol etmeli)

- **Mimari (kullanıcı kararı, 2026-11-01):** Katmanlı temiz mimari: Domain, Application, Infrastructure, UI. Bağımlılık içe doğrudur; portlar Application'da, gerçeklemeler Infrastructure'dadır.
- **Teknoloji:** .NET MAUI Blazor Hybrid + MudBlazor; Windows ve Android hedef.
- **Veri:** Her cihazda yerel SQLite; sunucu yok.
- **Excel üretimi:** ClosedXML.
- **E-posta:** SMTP (MailKit). Gmail için ayarlar ekranında "uygulama parolası" açıklaması gösterilir (2 adımlı doğrulama gerekir). Outlook parolalı SMTP'ye izin vermiyorsa test e-postası açık hata verir; OAuth ilk sürümde yok.
- **Tahminler:** aynı güne ikinci giriş yeni satır açmaz, mevcut günü düzenler; silmeden önce onay sorulur; liste varsayılan olarak içinde bulunulan ayı gösterir; telefonda liste tablo yerine kart görünümündedir; aynı gönderimde birden fazla alıcı seçilebilir; ayarlarda kayıtlı alıcı silinebilir.
- **Para:** Türk lirası. Tutarlar iki ondalık (kuruş) hassasiyetiyle `decimal` olarak saklanır, negatif tutar kabul edilmez. Excel'de binlik ayracıyla, para birimi simgesi olmadan gösterilir. Yazı tipi, sütun genişliği ve başlık biçimi builder'ın seçimidir.
- **Rapor aralığı:** Aralık birden fazla ayı veya yılı kapsayabilir. Tam bir takvim ayıysa başlık ve dosya adı "2026 Ekim" / `2026 Ekim Cirosu.xlsx` olur. Başka bir aralıkta başlık `15.10.2026 - 10.11.2026` ve dosya adı `15.10.2026 - 10.11.2026 Cirosu.xlsx` olur. Aynı adlı dosya varsa kullanıcıya üzerine yazma sorulur.
- **Gün kaydı:** Bir gün, günlük form **Kaydet** ile kaydedilince oluşur; üç tutarı da sıfır olabilir. Yeni günün havale formu, günlük form kaydedilene kadar bellekte taslak tutar; **Kaydet** günü ve havaleleri birlikte yazar, **İptal** hepsini atar. Mevcut günü düzenlerken de aynı kural geçerlidir.
- **Yedek dosyası:** Yalnızca günler ve havaleleri içerir. Ayarlar, SMTP hesabı, parola ve alıcı listesi yedeğe girmez (parola düz metin dosyada dolaşmasın diye). İçe aktarmada dosyada olup cihazda olmayan günler eklenir, cihazda olup dosyada olmayanlar korunur (birleştirme). Aynı tarihte nakit, kredi kartı veya havale listesi farklıysa çakışma sayılır. Kullanıcı dosyayı seçerse o günün tamamı (havaleleri dahil) dosyadakiyle değişir.
- **E-posta sağlayıcıları:** İlk sürümde tek genel SMTP formu vardır. Gmail, Outlook, Diğer seçimi yalnızca sunucu ve port alanlarını önceden doldurur.
- **Dağıtım:** Windows için paketlenmemiş (MSIX olmayan), çift tıklayıp çalıştırılan bir exe. Telefon için sideload edilen APK.
- **Proje konumu:** `C:\Users\birol\source\repos\CiroApp` (LawAstCrm'den bağımsız).

## Builder'a bırakılanlar

Proje yapısı, katmanlama, veri modeli ayrıntıları, ekran düzeni ve görsel tasarım, doğrulama kuralları (negatif tutar, boş ad), yedek dosyası biçimi, test stratejisi, uygulama adı ve simgesi.
