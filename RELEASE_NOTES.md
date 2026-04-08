# Kritter v2.3

## English

### Highlights

- Tightened the app scan filter so Windows runtimes, SDKs, framework packages, chipset helpers, PhysX, and similar system components no longer appear as reinstall targets
- Added version visibility to the in-app Info page
- Preserved the existing package, installer, optimization, resume, and cleanup flows
- Continued automatic Turkish/English UI selection based on the system language

### Release Assets

- `Kritter-v2.3.0.exe`
- `Kritter-v2.3.0-win-x64.zip`
- `Kritter-v2.3.0.exe.sha256.txt`

### Requirements

- Windows 10 or Windows 11
- `winget`
- Administrator privileges for some operations

### Distribution Notes

- `Kritter-v2.3.0.exe` is a self-contained single-file Windows x64 build, so a separate .NET runtime is not required.
- The current public EXE is unsigned, so Windows SmartScreen or Smart App Control may warn on download or launch.
- The repository includes `scripts/Publish-Release.ps1` for repeatable publish output and optional Authenticode signing when a valid public code-signing certificate is available.

## Türkçe

### Öne Çıkanlar

- Uygulama tarama filtresi sıkılaştırıldı; Windows runtime, SDK, framework paketi, chipset yardımcıları, PhysX ve benzeri sistem bileşenleri artık yeniden kurulum listesinde görünmemeli
- Uygulama içindeki Bilgi sayfasına sürüm bilgisi eklendi
- Mevcut paket, installer, optimizasyon, yeniden başlatma sonrası devam ve temizlik akışları korundu
- Sistem diline göre otomatik Türkçe/İngilizce arayüz seçimi sürdürülüyor

### Sürüm Dosyaları

- `Kritter-v2.3.0.exe`
- `Kritter-v2.3.0-win-x64.zip`
- `Kritter-v2.3.0.exe.sha256.txt`

### Gereksinimler

- Windows 10 veya Windows 11
- `winget`
- Bazı işlemler için yönetici yetkisi

### Dağıtım Notları

- `Kritter-v2.3.0.exe`, self-contained single-file Windows x64 build olarak yayınlandığı için ayrıca .NET runtime gerektirmez.
- Mevcut public EXE imzasız olduğu için Windows SmartScreen veya Smart App Control indirme ya da açılışta uyarı gösterebilir.
- Repo içinde tekrarlanabilir publish çıktısı ve geçerli public code-signing sertifikası ile opsiyonel Authenticode imzalama için `scripts/Publish-Release.ps1` bulunur.
