# Kritter v2.0.0

## English

### Highlights

- Added CS2 settings backup and restore through Steam account selection and `.kritter` packaging
- Refreshed the desktop UI with a more polished visual system and clearer navigation
- Preserved the existing package, installer, optimization, resume, and cleanup flows
- Added automatic Turkish/English UI selection based on the system language

### Release Assets

- `Kritter-v2.0.0.exe`
- `Kritter-v2.0.0-win-x64.zip`

### Requirements

- Windows 10 or Windows 11
- `winget`
- Administrator privileges for some operations

### Distribution Notes

- `Kritter-v2.0.0.exe` is a self-contained single-file Windows x64 build, so a separate .NET runtime is not required.
- `winget` may still need to finish registering on a brand-new Windows profile before app reinstall flows work.
- The current public EXE is unsigned, so Windows SmartScreen or Smart App Control may warn on download or launch.
- The repository now includes `scripts/Publish-Release.ps1` for repeatable publish output and optional Authenticode signing when a valid public code-signing certificate is available.

## Türkçe

### Öne Çıkanlar

- Steam hesap seçimi ve `.kritter` paketleme akışı ile CS2 ayar yedekleme ve geri yükleme desteği eklendi
- Masaüstü arayüzü daha profesyonel bir görsel sistem ve daha net bir gezinme yapısı ile yenilendi
- Mevcut paket, installer, optimizasyon, yeniden başlatma sonrası devam ve temizlik akışları korundu
- Sistem diline göre otomatik Türkçe/İngilizce arayüz seçimi eklendi

### Sürüm Dosyaları

- `Kritter-v2.0.0.exe`
- `Kritter-v2.0.0-win-x64.zip`

### Gereksinimler

- Windows 10 veya Windows 11
- `winget`
- Bazı işlemler için yönetici yetkisi

### Dağıtım Notları

- `Kritter-v2.0.0.exe`, self-contained single-file Windows x64 build olarak yayınlandığı için ayrıca .NET runtime gerektirmez.
- Yepyeni bir Windows kullanıcı profilinde uygulama geri yükleme akışlarının çalışması için `winget` kaydının tamamlanması gerekebilir.
- Mevcut public EXE imzasız olduğu için Windows SmartScreen veya Smart App Control indirme ya da açılışta uyarı gösterebilir.
- Repo içinde artık tekrarlanabilir publish çıktısı ve geçerli public code-signing sertifikası ile opsiyonel Authenticode imzalama için `scripts/Publish-Release.ps1` bulunur.
