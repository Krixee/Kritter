# Kritter v3

## English

### Highlights

- Extended the app scan filter to also exclude Windows built-in Store apps and pre-installed components that should never be reinstall targets after a fresh format: App Installer, HEVC/HEIF and other codec extensions, Xbox / Game Bar, the Xbox "Games" app, GameInput, Microsoft Store, Microsoft Photos, Paint, Notepad, Calculator, the Store Engagement framework, .NET Native / UI.Xaml / VC++ runtimes, Microsoft Edge / WebView2, Minecraft Launcher, plus NVIDIA drivers/FrameView, AMD chipset software, Realtek audio and Inno Setup
- Real user apps (Discord, Steam, Spotify, browsers, VS Code, 7-Zip, VLC, OBS, Notepad++, Paint.NET, etc.) are still detected and selectable
- Duplicate detections of the same product are collapsed into a single entry
- Renamed the "keep current" optimization mode to **Default settings (nothing is changed)** so a no-op run is unambiguous
- Added an automatic startup update check against GitHub Releases; a newer version shows a badge next to the title and an optional prompt to open the download page
- Preserved the existing package, installer, optimization, resume, cleanup, localization, and CS2 backup flows

### Release Assets

- `Kritter-v3.0.0.exe`
- `Kritter-v3.0.0-win-x64.zip`
- `Kritter-v3.0.0.exe.sha256.txt`

### Requirements

- Windows 10 or Windows 11
- `winget`
- Administrator privileges for some operations

### Distribution Notes

- `Kritter-v3.0.0.exe` is a self-contained single-file Windows x64 build, so a separate .NET runtime is not required.
- The current public EXE is unsigned, so Windows SmartScreen or Smart App Control may warn on download or launch.
- The repository includes `scripts/Publish-Release.ps1` for repeatable publish output and optional Authenticode signing when a valid public code-signing certificate is available.

## Türkçe

### Öne Çıkanlar

- Uygulama tarama filtresi genişletildi; format sonrası asla yeniden kurulmaması gereken Windows yerleşik Store uygulamaları ve önceden yüklü bileşenler de dışlanıyor: App Installer, HEVC/HEIF ve diğer codec uzantıları, Xbox / Game Bar, Xbox "Oyunlar" uygulaması, GameInput, Microsoft Store, Microsoft Fotoğraflar, Paint, Not Defteri, Hesap Makinesi, Store Engagement framework, .NET Native / UI.Xaml / VC++ runtime'ları, Microsoft Edge / WebView2, Minecraft Launcher, ayrıca NVIDIA sürücüleri/FrameView, AMD chipset yazılımı, Realtek ses ve Inno Setup
- Gerçek kullanıcı uygulamaları (Discord, Steam, Spotify, tarayıcılar, VS Code, 7-Zip, VLC, OBS, Notepad++, Paint.NET vb.) hâlâ tespit edilip seçilebiliyor
- Aynı uygulamanın tekrarlı tespitleri tek girişe indirgeniyor
- "Şu anki ayarları koru" optimizasyon modu **Varsayılan ayarlar (hiçbir şey değiştirilmez)** olarak yeniden adlandırıldı; hiçbir şey yapmayan çalışma artık net
- Açılışta GitHub Releases üzerinden otomatik sürüm kontrolü eklendi; yeni sürüm varsa başlığın yanında rozet ve indirme sayfasını açmak için opsiyonel bir uyarı gösterilir
- Mevcut paket, installer, optimizasyon, yeniden başlatma sonrası devam, temizlik, yerelleştirme ve CS2 yedekleme akışları korundu

### Sürüm Dosyaları

- `Kritter-v3.0.0.exe`
- `Kritter-v3.0.0-win-x64.zip`
- `Kritter-v3.0.0.exe.sha256.txt`

### Gereksinimler

- Windows 10 veya Windows 11
- `winget`
- Bazı işlemler için yönetici yetkisi

### Dağıtım Notları

- `Kritter-v3.0.0.exe`, self-contained single-file Windows x64 build olarak yayınlandığı için ayrıca .NET runtime gerektirmez.
- Mevcut public EXE imzasız olduğu için Windows SmartScreen veya Smart App Control indirme ya da açılışta uyarı gösterebilir.
- Repo içinde tekrarlanabilir publish çıktısı ve geçerli public code-signing sertifikası ile opsiyonel Authenticode imzalama için `scripts/Publish-Release.ps1` bulunur.
