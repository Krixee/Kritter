# Kritter

## English

Kritter is a WPF desktop tool for rebuilding a Windows setup after format, restoring packaged apps, backing up selected game settings, and applying optimization scripts.

### What It Does

- Scans the current PC for reinstallable apps
- Builds portable `.kritter` packages
- Reinstalls apps with `winget` or bundled direct installers
- Backs up and restores CS2 settings from Steam `userdata/<accountId>/730`
- Applies `Kritter Recommended` and optional `fr33ty Recommended` optimization scripts
- Resumes the setup flow after reboot and runs cleanup at the end

### Project Structure

- `Kritter/`: WPF application source
- `Kritter Recommended/`: built-in optimization scripts
- `fr33ty Recommended/`: optional optimization script set
- `artifacts/`: publish and release output
- `scripts/`: release automation scripts

### Can The GitHub Release Run On A Fresh PC?

Yes, with a few conditions.

- The GitHub release EXE is published as a self-contained single-file Windows x64 app, so a separate .NET runtime is not required.
- The target machine still needs Windows 10 1809 or later, or Windows 11.
- Kritter launches with administrator elevation because the app manifest uses `requireAdministrator`.
- `winget` is required for the package flows that reinstall apps through Windows Package Manager.
- Internet access is required for `winget` installs and direct-download installers.
- CS2 restore needs Steam to be installed and the target Steam account to have signed in at least once so `Steam\userdata\<accountId>\730` exists.
- Offline use is still possible for bundled local setup files already stored inside a `.kritter` package.

### SmartScreen And Smart App Control

- The current public `Kritter-v2.3.0.exe` is unsigned.
- Because of that, Windows SmartScreen or Smart App Control can warn users with messages like "Windows protected your PC".
- There is no project-only flag that safely removes that warning for other people.
- The real fix is signing the release with a publicly trusted code-signing identity and building reputation over time.
- A self-signed certificate or localhost certificate does not solve the warning for end users.

### Release Signing

- The repository includes `scripts/Publish-Release.ps1`.
- It publishes the release EXE, ZIP, and SHA-256 hash.
- If you provide a real code-signing certificate thumbprint, it signs the published EXE and verifies the Authenticode signature.
- If you do not sign the release, the script prints a warning that SmartScreen may still flag it.
- If you want Microsoft's managed path instead of buying a traditional OV/EV certificate, evaluate Microsoft Artifact Signing.

### Build

```powershell
dotnet build Kritter.sln
```

### Publish

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Release.ps1 -Version 2.3.0
```

### Sign A Release

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Release.ps1 -Version 2.3.0 -SigningThumbprint "<CERT_THUMBPRINT>" -TimestampServer "<TIMESTAMP_URL>"
```

### Notes

- The app UI switches automatically: Turkish on `tr` systems, English on non-`tr` systems.
- The scan excludes common Windows runtimes, frameworks, SDKs, and system helper packages such as Windows App Runtime, .NET SDK, UI Xaml, VCLibs, PhysX, and chipset helper entries.
- CS2 account selection uses local Steam data and, when available, public Steam profile XML data for avatar and profile confirmation.
- Valorant and League of Legends are not packaged in this release because Riot documents a mixed local and server-side settings flow.
- Optimization scripts modify Windows settings. Creating a restore point first is recommended.

## Türkçe

Kritter, format sonrası Windows kurulumunu yeniden toparlamak, paketlenmiş uygulamaları geri yüklemek, seçili oyun ayarlarını yedeklemek ve optimizasyon scriptleri çalıştırmak için geliştirilmiş bir WPF masaüstü uygulamasıdır.

### Neler Yapar

- Mevcut bilgisayarı tarayıp yeniden kurulabilecek uygulamaları bulur
- Taşınabilir `.kritter` paketleri oluşturur
- Uygulamaları `winget` veya pakete eklenmiş direct installer ile tekrar kurar
- Steam `userdata/<accountId>/730` içinden CS2 ayarlarını yedekler ve geri yükler
- `Kritter Recommended` ve isteğe bağlı `fr33ty Recommended` scriptlerini uygular
- Yeniden başlatma sonrası kaldığı yerden devam eder ve sonda temizlik yapar

### Proje Yapısı

- `Kritter/`: WPF uygulama kaynak kodu
- `Kritter Recommended/`: yerleşik optimizasyon scriptleri
- `fr33ty Recommended/`: isteğe bağlı optimizasyon script seti
- `artifacts/`: publish ve release çıktıları
- `scripts/`: release otomasyon scriptleri

### GitHub Release Sıfır Bilgisayarda Çalışır mı?

Evet, ama birkaç şart var.

- GitHub release EXE dosyası self-contained single-file Windows x64 olarak yayınlandığı için ayrıca .NET runtime kurulması gerekmez.
- Hedef makinede yine de Windows 10 1809 veya sonrası ya da Windows 11 bulunmalıdır.
- Kritter yönetici yetkisi isteyerek açılır çünkü manifest içinde `requireAdministrator` kullanılır.
- Windows Package Manager üzerinden uygulama kuran akışlar için `winget` gerekir.
- `winget` kurulumları ve direct installer indirmeleri için internet gerekir.
- CS2 geri yükleme için Steam kurulu olmalı ve hedef Steam hesabı en az bir kez giriş yapmış olmalıdır; böylece `Steam\userdata\<accountId>\730` klasörü oluşur.
- `.kritter` paketi içine önceden eklenmiş yerel setup dosyaları varsa, o kısım internet olmadan da kullanılabilir.

### SmartScreen ve Smart App Control

- Şu anki public `Kritter-v2.3.0.exe` imzasızdır.
- Bu yüzden Windows SmartScreen veya Smart App Control indiren kullanıcıya "Windows bilgisayarınızı korudu" benzeri uyarılar gösterebilir.
- Bu uyarıyı başka kullanıcılar için güvenli biçimde kapatan bir proje ayarı yoktur.
- Gerçek çözüm, release dosyasını public trust code-signing kimliğiyle imzalamak ve zamanla itibar kazanmaktır.
- Self-signed veya localhost sertifikası son kullanıcı tarafındaki uyarıyı çözmez.

### Release İmzalama

- Repo içinde `scripts/Publish-Release.ps1` bulunur.
- Bu script release EXE, ZIP ve SHA-256 hash dosyasını üretir.
- Geçerli bir code-signing sertifika thumbprint'i verirsen yayınlanan EXE'yi imzalar ve Authenticode imzasını doğrular.
- Release imzalanmazsa script SmartScreen riski hakkında uyarı basar.
- Klasik OV/EV sertifika yerine Microsoft'un yönetilen çözümünü kullanmak istersen Microsoft Artifact Signing değerlendirilebilir.

### Derleme

```powershell
dotnet build Kritter.sln
```

### Yayın Alma

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Release.ps1 -Version 2.3.0
```

### İmzalı Release Alma

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Release.ps1 -Version 2.3.0 -SigningThumbprint "<SERTIFIKA_THUMBPRINT>" -TimestampServer "<TIMESTAMP_URL>"
```

### Notlar

- Uygulama arayüzü otomatik dil seçer: sistem dili `tr` ise Türkçe, diğer dillerde İngilizce açılır.
- Tarama akışı artık Windows App Runtime, .NET SDK, UI Xaml, VCLibs, PhysX ve benzeri sistem/runtime bileşenlerini dışarıda bırakır.
- CS2 hesap seçimi için yerel Steam verisi ve erişilebildiğinde public Steam profil XML verisi kullanılır.
- Valorant ve League of Legends bu sürümde pakete eklenmedi; Riot tarafında hem yerel hem sunucu tabanlı ayar akışı dokümante edildiği için kapsam şimdilik CS2 ile sınırlı.
- Optimizasyon scriptleri Windows ayarlarını değiştirir. Öncesinde geri yükleme noktası oluşturmak önerilir.
