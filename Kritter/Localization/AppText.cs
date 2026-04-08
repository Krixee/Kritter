using System.Globalization;

namespace Kritter.Localization;

public static class AppText
{
    public static bool IsTurkish => string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "tr", System.StringComparison.OrdinalIgnoreCase);

    private static string Pick(string tr, string en) => IsTurkish ? tr : en;

    public static string AppName => "Kritter";
    public static string WindowSubtitle => Pick("Format sonrası kurulum, geri yükleme ve optimizasyon konsolu", "Post-format setup, restore, and optimization console");

    public static string NavFormatAt => Pick("Format At", "Build Package");
    public static string NavInstall => Pick("Yükle", "Install");
    public static string NavOptimize => Pick("Optimizasyon", "Optimization");
    public static string NavInfo => Pick("Bilgi", "Info");

    public static string FormatPageTitle => Pick("Format At", "Build Package");
    public static string FormatPageSubtitle => Pick(
        "Sistemi tara, uygulamaları seç, setup dosyalarını ekle ve format sonrası tekrar kullanılacak profesyonel bir `.kritter` paketi üret.",
        "Scan the system, select apps, include local setup files, and produce a reusable `.kritter` package for after a fresh Windows install.");
    public static string ScanSystem => Pick("Sistemi Tara", "Scan System");
    public static string InstalledAppsMetric => Pick("Yüklü Uygulamalar", "Installed Apps");
    public static string SetupFilesMetric => Pick("Setup Dosyaları", "Setup Files");
    public static string GameBackupsMetric => Pick("Oyun Yedekleri", "Game Backups");
    public static string ScanningSystem => Pick("Sistem taranıyor...", "Scanning system...");
    public static string DetectedAppsTitle => Pick("Tespit Edilen Uygulamalar", "Detected Applications");
    public static string DetectedAppsSubtitle => Pick("Mevcut sistemde bulunan ve yeniden kurulabilecek uygulamalar.", "Applications detected on the current machine and available for reinstall.");
    public static string SelectAll => Pick("Tümünü Seç", "Select All");
    public static string Clear => Pick("Temizle", "Clear");
    public static string NoDetectedApps => Pick("Tespit edilen uygulama yok.", "No applications were detected.");
    public static string CommonAppsTitle => Pick("Yaygın Uygulamalar", "Common Applications");
    public static string CommonAppsSubtitle => Pick("Şu anda yüklü olmayan ama pakete eklemek isteyebileceğiniz uygulamalar.", "Apps that are not installed right now but can still be included in the package.");
    public static string SetupFilesTitle => Pick("Setup Dosyaları", "Setup Files");
    public static string SetupFilesSubtitle => Pick("Driver kurulumlarını ayrı tutmak daha güvenli. Klasör seçildiğinde uygun setup dosyaları otomatik tespit edilir.", "Keeping driver installers separate is safer. Once a folder is selected, compatible setup files are detected automatically.");
    public static string SelectSetupFolder => Pick("Setup klasörü seç", "Choose Setup Folder");
    public static string GameSettingsTitle => Pick("Oyun Ayarları", "Game Settings");
    public static string GameSettingsSubtitle1 => Pick("Bu sürümde yalnızca CS2 destekleniyor. Steam `userdata` içindeki `730` klasörü seçilen hesap için pakete dahil edilir.", "This release currently supports only CS2. The `730` folder under Steam `userdata` is packaged for the selected account.");
    public static string GameSettingsSubtitle2 => Pick("Valorant ve League of Legends tarafı şu aşamada kapsam dışında tutuldu; önce net geri yüklenebilen CS2 akışı ürünleştirildi.", "Valorant and League of Legends are out of scope for now; the first productized settings flow is the clearly restorable CS2 path.");
    public static string SelectCs2Account => Pick("CS2 hesabı seç / değiştir", "Select / Change CS2 Account");
    public static string OptimizationModeTitle => Pick("Optimizasyon Modu", "Optimization Mode");
    public static string KritterRecommended => Pick("Kritter Önerilen", "Kritter Recommended");
    public static string Fr33tyAll => Pick("Fr33ty Tüm Optimizasyon", "Fr33ty Full Optimization");
    public static string KeepCurrent => Pick("Şu anki ayarları koru", "Keep Current Settings");
    public static string SelectScripts => Pick("Scriptleri Seç...", "Select Scripts...");
    public static string CreatePackage => Pick("Paketi Oluştur", "Create Package");

    public static string InstallPageTitle => Pick("Yükle", "Install");
    public static string InstallPageSubtitle => Pick("Bir `.kritter` paketi seçin, kurulum akışını başlatın ve sisteminizi paketlenen içerikle tekrar ayağa kaldırın.", "Choose a `.kritter` package, start the install flow, and rebuild the machine using the packaged content.");
    public static string PackageStatus => Pick("Paket Durumu", "Package Status");
    public static string ExecutionState => Pick("Çalışma Durumu", "Execution State");
    public static string ReadyLabel => Pick("Hazır", "Ready");
    public static string NoPackageLabel => Pick("Paket yok", "No package");
    public static string RunningLabel => Pick("Çalışıyor", "Running");
    public static string IdleLabel => Pick("Boşta", "Idle");
    public static string SelectPackage => Pick("Paket Seç", "Select Package");
    public static string PackageInfoTitle => Pick("Paket Bilgisi", "Package Information");
    public static string StartInstall => Pick("Yüklemeyi Başlat", "Start Install");
    public static string RestartNow => Pick("Yeniden Başlat", "Restart");
    public static string RestartWaiting => Pick("Optimizasyon tamamlandı. Devam etmek için yeniden başlatma bekleniyor.", "Optimization completed. Waiting for a restart to continue.");
    public static string OperationLog => Pick("İşlem Günlüğü", "Operation Log");
    public static string OperationLogSubtitle => Pick("Kurulum akışındaki tüm adımlar burada akıyor.", "All steps in the install flow are shown here.");
    public static string LiveExecution => Pick("Canlı Çalışma", "Live Execution");
    public static string ProcessRunning => Pick("İşlem devam ediyor...", "Operation in progress...");

    public static string OptimizationPageTitle => Pick("Optimizasyon", "Optimization");
    public static string OptimizationPageSubtitle => Pick("Kritter veya Fr33ty akışlarını seçin, scriptleri yönetin ve sistemi doğrudan uygulama içinden optimize edin.", "Choose Kritter or Fr33ty flows, manage scripts, and optimize the system directly from the app.");
    public static string KritterScriptsMetric => Pick("Kritter Scriptleri", "Kritter Scripts");
    public static string Fr33tySelectedMetric => Pick("Seçili Fr33ty", "Fr33ty Selected");
    public static string ApplyOptimization => Pick("Optimizasyonu Uygula", "Apply Optimization");
    public static string ScriptListTitle => Pick("Scriptler", "Scripts");
    public static string ScriptListSubtitle => Pick("Kritter önerilen script listesi.", "Recommended Kritter script list.");
    public static string OptimizationLogSubtitle => Pick("Uygulanan veya atlanan tüm optimizasyon adımları burada görünür.", "All optimization steps, applied or skipped, are shown here.");
    public static string OptimizationRunning => Pick("Optimizasyon Çalışıyor", "Optimization Running");
    public static string OptimizationContinuing => Pick("Optimizasyon devam ediyor...", "Optimization in progress...");
    public static string Fr33tyScriptCountLabel(int count) => Pick($"{count} Fr33ty script seçildi", $"{count} Fr33ty scripts selected");

    public static string InfoPageTitle => Pick("Bilgi", "Info");
    public static string InfoPageSubtitle => Pick("Kritter'in ne yaptığını, nasıl çalıştığını ve hangi akışları birleştirdiğini tek yerde özetler.", "A single place that explains what Kritter does, how it works, and which workflows it brings together.");
    public static string InfoCard1Title => Pick("Nasıl Çalışır", "How It Works");
    public static string InfoCard1Body => Pick("Önce mevcut sistemi tarar, ardından seçtiğiniz uygulamaları, yerel setup dosyalarını ve desteklenen oyun ayarlarını `.kritter` paketine yazar. Format sonrası bu paket tekrar okunup kurulum akışı çalıştırılır.", "It first scans the current machine, then writes your selected applications, local setup files, and supported game settings into a `.kritter` package. After a fresh install, that package is read again and the install flow is executed.");
    public static string InfoCard2Title => Pick("Neler Yapar", "What It Does");
    public static string InfoCard2Body => Pick("Uygulamaları `winget` veya direct installer ile kurar, setup dosyalarını paketler, CS2 ayarlarını geri yükler, optimizasyon scriptlerini uygular ve gerekirse yeniden başlatma sonrası kaldığı yerden devam eder.", "It reinstalls apps through `winget` or direct installers, packages local setup files, restores CS2 settings, applies optimization scripts, and resumes after a reboot when needed.");
    public static string InfoCard3Title => Pick("Yapımcı", "Author");
    public static string InfoCard3Body => Pick("Krixe tarafından geliştirildi. Kritter, format sonrası kurulum ve optimizasyon sürecini tek bir masaüstü akışında toplamak için tasarlandı.", "Built by Krixe. Kritter is designed to bring post-format setup and optimization into a single desktop workflow.");

    public static string Fr33tyModalTitle => Pick("Fr33ty Script Seçimi", "Fr33ty Script Selection");
    public static string Fr33tyModalHeading => Pick("Fr33ty Optimizasyon Scriptleri", "Fr33ty Optimization Scripts");
    public static string Fr33tyModalSubtitle => Pick("Uygulamak istediğiniz scriptleri seçin.", "Choose the scripts you want to apply.");
    public static string DeselectAll => Pick("Tümünü Kaldır", "Clear All");
    public static string Cancel => Pick("İptal", "Cancel");
    public static string Confirm => Pick("Tamam", "Confirm");

    public static string SteamAccountWindowTitle => Pick("CS2 Hesap Seçimi", "CS2 Account Selection");
    public static string SteamAccountHeading => Pick("Counter-Strike 2 Steam Hesabı", "Counter-Strike 2 Steam Account");
    public static string SteamAccountSubtitle => Pick("`userdata` içinde `730` klasörü bulunan hesabı seçin. Seçilen hesabın CS2 ayarları `.kritter` paketine eklenecek.", "Select the account that contains the `730` folder under `userdata`. The selected account's CS2 settings will be added into the `.kritter` package.");
    public static string MostRecent => Pick("Son kullanılan", "Most recent");
    public static string UseAccount => Pick("Hesabı Kullan", "Use Account");

    public static string SplashStatus => Pick("Uygulamalar taranıyor...", "Scanning applications...");
    public static string ReleaseV2 => "Release v2";

    public static string BuildPackageFilter => Pick("Kritter Paket (*.kritter)|*.kritter", "Kritter Package (*.kritter)|*.kritter");
    public static string SelectPackageTitle => Pick("Kritter Paketi Seç", "Select Kritter Package");
    public static string SetupFolderWarningTitle => Pick("Setup Klasörü Uyarısı", "Setup Folder Warning");
    public static string SetupFolderWarningMessage => Pick("Driver'ları manuel kurmanız önerilir. Lütfen seçtiğiniz klasörde driver kurulum dosyaları varsa çıkarın.", "Manual driver installation is recommended. If the selected folder contains driver installers, remove them before continuing.");
    public static string SetupFolderDialogDescription => Pick("Setup dosyalarının olduğu klasörü seçin.", "Select the folder that contains setup files.");

    public static string ScanStatus => Pick("Sistem taranıyor...", "Scanning system...");
    public static string NoReinstallableAppsFound => Pick("Yeniden kurulabilir uygulama bulunamadı.", "No reinstallable applications were found.");
    public static string ReinstallableAppsDetected(int count) => Pick($"{count} yeniden kurulabilir uygulama tespit edildi", $"{count} reinstallable applications detected");
    public static string ScanError(string message) => Pick($"Tarama hatası: {message}", $"Scan error: {message}");
    public static string NoSetupFilesFound => Pick("Seçilen klasörde uygun setup dosyası bulunamadı.", "No compatible setup files were found in the selected folder.");
    public static string SetupFilesDetected(int count) => Pick($"{count} setup dosyası bulundu ve pakete eklenebilir.", $"{count} setup files were found and can be included in the package.");
    public static string SelectAtLeastOnePackageItem => Pick("Lütfen en az bir uygulama, setup dosyası veya oyun ayarı seçin.", "Select at least one application, setup file, or game settings backup.");
    public static string PackageCreatedStatus(string fileName) => Pick($"Paket oluşturuldu: {fileName}", $"Package created: {fileName}");
    public static string PackageCreatedMessage(string path) => Pick($"Paket başarıyla oluşturuldu!\n\n{path}", $"Package created successfully.\n\n{path}");
    public static string PackageCreateError(string message) => Pick($"Paket oluşturma hatası: {message}", $"Package creation error: {message}");
    public static string SearchingSteamAccounts => Pick("Steam/CS2 hesapları aranıyor...", "Searching Steam/CS2 accounts...");
    public static string NoCs2AccountsMessage => Pick("Steam `userdata` içinde `730` klasörü bulunan bir hesap bulunamadı.", "No account with a `730` folder under Steam `userdata` was found.");
    public static string NoCs2AccountsStatus => Pick("CS2 hesabı bulunamadı.", "No CS2 account found.");
    public static string Cs2SelectionCancelled => Pick("CS2 hesap seçimi iptal edildi.", "CS2 account selection was cancelled.");
    public static string Cs2Selected(string name) => Pick($"CS2 hesabı seçildi: {name}", $"CS2 account selected: {name}");
    public static string Cs2ReadError(string message) => Pick($"CS2 hesapları okunamadı: {message}", $"Could not read CS2 accounts: {message}");
    public static string Cs2ScanFailed => Pick("CS2 hesap taraması başarısız oldu.", "CS2 account scan failed.");
    public static string SteamAccountRequired => Pick("Lütfen bir Steam hesabı seçin.", "Please select a Steam account.");

    public static string KritterScriptsFound(int count) => Pick($"{count} Kritter optimizasyon scripti bulundu.", $"{count} Kritter optimization scripts found.");
    public static string KritterScriptsNotFound => Pick("Kritter scripti bulunamadı.", "No Kritter scripts were found.");
    public static string ScriptLoadError(string message) => Pick($"Script yükleme hatası: {message}", $"Script loading error: {message}");
    public static string Fr33tySelectionPrompt => Pick("Fr33ty scriptlerini seçmek için \"Scriptleri Seç\" butonunu kullanın.", "Use the \"Select Scripts\" button to choose Fr33ty scripts.");
    public static string SelectAtLeastOneScript => Pick("Lütfen en az bir script seçin.", "Please select at least one script.");
    public static string SelectFr33tyScripts => Pick("Lütfen Fr33ty scriptlerini seçin.", "Please select Fr33ty scripts.");
    public static string OptimizationStarted => Pick("--- Optimizasyon Başlatıldı ---", "--- Optimization Started ---");
    public static string AllOptimizationsCompleted => Pick("Tüm optimizasyonlar tamamlandı.", "All optimizations completed.");
    public static string OptimizationsAppliedSuccessfully => Pick("Optimizasyonlar başarıyla uygulandı.", "Optimizations were applied successfully.");
    public static string OptimizationFailed => Pick("Optimizasyon sırasında hata oluştu.", "An error occurred during optimization.");
    public static string ApplyingProgress(string name, int pct) => Pick($"{name} uygulanıyor... %{pct}", $"{name} is being applied... %{pct}");
    public static string Applied(string name) => Pick($"{name} uygulandı.", $"{name} applied.");
    public static string ApplyFailedContinue(string name) => Pick($"{name} - uygulanamadı (devam ediliyor).", $"{name} could not be applied (continuing).");
    public static string FileMissingSkipped(string name) => Pick($"{name} - dosya bulunamadı, atlanıyor.", $"{name} - file not found, skipping.");

    public static string PackageReadFailed => Pick("Paket okunamadı.", "The package could not be read.");
    public static string PackageLoadError(string message) => Pick($"Paket yükleme hatası: {message}", $"Package loading error: {message}");
    public static string ResumeAfterRestart => Pick("Yeniden başlatma sonrası devam ediliyor...", "Resuming after restart...");
    public static string AllTasksCompleted => Pick("Tüm işlemler tamamlandı.", "All tasks completed.");
    public static string ErrorPrefix(string message) => Pick($"HATA: {message}", $"ERROR: {message}");
    public static string OptimizationSkipped => Pick("Optimizasyon atlandı (ayarlar korunuyor).", "Optimization skipped (current settings preserved).");
    public static string OptimizationsCompletedRestart => Pick("Optimizasyonlar tamamlandı. Bilgisayarınızı yeniden başlatın.", "Optimizations completed. Restart the computer to continue.");
    public static string WaitingForRestart => Pick("Yeniden başlatma bekleniyor...", "Waiting for restart...");
    public static string RestartShutdownComment => Pick("Kritter optimizasyonları uygulandı. Yeniden başlatılıyor...", "Kritter optimizations were applied. Restarting...");
    public static string OptimizationPhase => Pick("--- Optimizasyon Aşaması ---", "--- Optimization Phase ---");
    public static string NoFr33tyScriptsSelected => Pick("Fr33ty script seçilmemiş.", "No Fr33ty scripts were selected.");
    public static string RemindersHeader => Pick("--- Hatırlatmalar ---", "--- Reminders ---");
    public static string ManualSettingRequired(string name) => Pick($"  Manuel ayar gerekli: {name}", $"  Manual setting required: {name}");
    public static string NoAppsToInstall => Pick("Yüklenecek uygulama yok.", "No applications need to be installed.");
    public static string AppInstallPhase => Pick("--- Uygulama Yükleme Aşaması ---", "--- Application Install Phase ---");
    public static string InstallingProgress(string name, int pct) => Pick($"{name} yükleniyor... %{pct}", $"{name} is being installed... %{pct}");
    public static string AlreadyInstalled(string name, string method) => Pick($"{name} ({method}) zaten yüklü.", $"{name} ({method}) is already installed.");
    public static string Installed(string name, string method) => Pick($"{name} ({method}) yüklendi.", $"{name} ({method}) installed.");
    public static string InstallFailed(string name, string method) => Pick($"{name} ({method}) - yüklenemedi.", $"{name} ({method}) could not be installed.");
    public static string NoSetupInstallNeeded => Pick("Setup kurulumu gerekmiyor.", "No setup-file installation is required.");
    public static string SetupInstallPhase => Pick("--- Setup Dosyaları Kurulum Aşaması ---", "--- Setup Files Install Phase ---");
    public static string SetupInstallingProgress(string name, int pct) => Pick($"{name} (Setup) kuruluyor... %{pct}", $"{name} (Setup) is being installed... %{pct}");
    public static string SetupInstalled(string name) => Pick($"{name} (Setup) kuruldu.", $"{name} (Setup) installed.");
    public static string SetupInstallFailed(string name) => Pick($"{name} (Setup) - kurulamadı.", $"{name} (Setup) could not be installed.");
    public static string NoGameSettingsRestoreNeeded => Pick("Oyun ayarı geri yükleme gerekmiyor.", "No game settings restore is required.");
    public static string GameSettingsRestorePhase => Pick("--- Oyun Ayarları Geri Yükleme Aşaması ---", "--- Game Settings Restore Phase ---");
    public static string RestoringProgress(string name, int pct) => Pick($"{name} geri yükleniyor... %{pct}", $"{name} is being restored... %{pct}");
    public static string Restored(string name) => Pick($"{name} geri yüklendi.", $"{name} restored.");
    public static string RestoreFailed(string name) => Pick($"{name} - geri yüklenemedi.", $"{name} could not be restored.");
    public static string CleanupPhase => Pick("--- Temizlik Aşaması ---", "--- Cleanup Phase ---");
    public static string CleaningTemporaryFiles => Pick("Geçici dosyalar temizleniyor...", "Cleaning temporary files...");
    public static string TemporaryFilesCleaned => Pick("Geçici dosyalar temizlendi.", "Temporary files cleaned.");

    public static string ModeLabel => Pick("Mod", "Mode");
    public static string AppsLabel => Pick("Uygulama", "Apps");
    public static string WingetLabel => "Winget";
    public static string DirectLabel => "Direct";
    public static string SetupLabel => Pick("Setup", "Setup");
    public static string GameSettingsLabel => Pick("Oyun ayarı", "Game settings");
    public static string CreatedAtLabel => Pick("Oluşturma", "Created");
    public static string Fr33tyScriptLabel => Pick("Fr33ty Script", "Fr33ty Scripts");
    public static string ItemsSuffix => Pick("adet", "items");
    public static string UnknownAccount => Pick("Bilinmeyen hesap", "Unknown account");
    public static string UnknownUserName => Pick("bilinmiyor", "unknown");
    public static string SteamAccountLabel(string accountName, string accountId) => Pick($"Steam hesabı: {accountName} | userdata/{accountId}", $"Steam account: {accountName} | userdata/{accountId}");
    public static string SteamUserLabel(string loginName, string accountId) => Pick($"Steam kullanıcı adı: {loginName} | userdata/{accountId}", $"Steam username: {loginName} | userdata/{accountId}");
    public static string UnsupportedGameSettingsType => Pick("Desteklenmeyen oyun ayarı türü.", "Unsupported game settings type.");
    public static string Cs2FolderMissingFromPackage => Pick("CS2 ayar klasörü paketten çıkarılamadı.", "The CS2 settings folder could not be extracted from the package.");
    public static string SteamNotFound => Pick("Steam bulunamadı. Lütfen önce Steam'i yükleyip en az bir kez giriş yapın.", "Steam was not found. Install Steam and sign in at least once first.");
    public static string Cs2TargetPathInvalid => Pick("CS2 hedef yolu doğrulanamadı.", "The CS2 target path could not be validated.");
    public static string Cs2TargetFolderInvalid => Pick("CS2 hedef klasörü hazırlanamadı.", "The CS2 target folder could not be prepared.");
    public static string SetupFileNotFound(string name) => Pick($"Setup dosyası bulunamadı: {name}", $"Setup file not found: {name}");
    public static string SetupFolderNotFound(string name) => Pick($"Oyun ayarı klasörü bulunamadı: {name}", $"Game settings folder not found: {name}");
    public static string UnsupportedSetupType(string extension) => Pick($"Desteklenmeyen setup türü: {extension}", $"Unsupported setup type: {extension}");
}
