# Kritter

## English

Kritter is a Windows desktop tool built with WPF and .NET 8 for post-format setup, software reinstallation, game-settings backup, and system optimization.

### What It Does

- Scans the current system for reinstallable applications
- Creates portable `.kritter` packages for later use
- Reinstalls apps with `winget` or silent direct installers
- Detects local setup files and includes them in the package
- Backs up and restores CS2 settings from the selected Steam `userdata/<accountId>/730` folder
- Applies optimization scripts from `Kritter Recommended`
- Supports optional script selection from `fr33ty Recommended`
- Resumes the installation flow after a reboot
- Runs a cleanup phase after setup is completed

### Project Structure

- `Kritter/`: WPF application source code
- `Kritter Recommended/`: built-in recommended optimization scripts
- `fr33ty Recommended/`: additional optimization script collection
- `artifacts/`: publish and release outputs

### Requirements

- Windows 10 or Windows 11
- .NET 8 SDK for building from source
- `winget` available on the target system
- Administrator privileges for some optimization and setup steps

### Build

```powershell
dotnet build Kritter.sln
```

### Publish

```powershell
dotnet publish Kritter\Kritter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

### Notes

- The UI is currently in Turkish.
- CS2 account selection uses the local Steam `loginusers.vdf` file and, when available, the public Steam Community profile XML for avatar and profile confirmation.
- Valorant and League of Legends are not packaged in this release because Riot documents a mixed server-side and local-settings flow.
- Optimization scripts modify Windows settings. Creating a restore point before use is recommended.

## Türkçe

Kritter, format sonrası kurulum, yazılım geri yükleme, oyun ayarı yedekleme ve sistem optimizasyonu için WPF ve .NET 8 ile geliştirilmiş bir Windows masaüstü uygulamasıdır.

### Neler Yapar

- Mevcut sistemi tarayıp yeniden kurulabilecek uygulamaları bulur
- Sonradan kullanılmak üzere taşınabilir `.kritter` paketleri oluşturur
- Uygulamaları `winget` veya sessiz direct installer ile tekrar kurar
- Yerel setup dosyalarını tespit edip pakete ekler
- Seçilen Steam `userdata/<accountId>/730` klasöründen CS2 ayarlarını yedekler ve geri yükler
- `Kritter Recommended` scriptlerini uygular
- `fr33ty Recommended` scriptleri için seçimli akışı destekler
- Yeniden başlatma sonrasında kurulum akışına devam eder
- Kurulum tamamlandıktan sonra temizlik adımı çalıştırır

### Proje Yapısı

- `Kritter/`: WPF uygulama kaynak kodu
- `Kritter Recommended/`: yerleşik önerilen optimizasyon scriptleri
- `fr33ty Recommended/`: ek optimizasyon script koleksiyonu
- `artifacts/`: publish ve release çıktıları

### Gereksinimler

- Windows 10 veya Windows 11
- Kaynaktan derlemek için .NET 8 SDK
- Hedef sistemde `winget`
- Bazı optimizasyon ve kurulum adımları için yönetici yetkisi

### Derleme

```powershell
dotnet build Kritter.sln
```

### Yayın Alma

```powershell
dotnet publish Kritter\Kritter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

### Notlar

- Arayüz şu anda Türkçedir.
- CS2 hesap seçimi için yerel `loginusers.vdf` dosyası ve erişilebildiğinde public Steam Community profil XML verisi kullanılır.
- Valorant ve League of Legends bu sürümde pakete eklenmedi; Riot tarafında hem sunucu hem yerel ayar akışı dokümante edildiği için kapsam şimdilik CS2 ile sınırlandı.
- Optimizasyon scriptleri Windows ayarlarını değiştirir. Kullanmadan önce geri yükleme noktası oluşturmak önerilir.
