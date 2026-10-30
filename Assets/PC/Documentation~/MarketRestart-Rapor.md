Restart ile ekonomi ve satın alınan taşları sıfırlama — 29 Ekim 2026

ESC menüsündeki Restart, daha önce yalnızca TidyFramework.RestartLevel üzerinden level snapshot'ını temizleyip sahneyi yüklüyordu. DontDestroyOnLoad ile yaşayan ekonomi servisi ve kalıcı teslimat kaydı sıfırlanmıyordu.

Değişiklikler:

- TidyFramework.cs: sahne kaydı silinmeden önce kalıcı servislerin hazırlanması için LevelRestarting olayı eklendi. Bir katılımcı başarısız olursa yeniden yükleme başlamaz. Framework doğrudan ekonomi sınıfına bağlanmadı.
- StoneEconomyBootstrap.cs: mevcut EconomyService.StartNewGame(Settings.startingBalance) bu olaya bağlandı; mevcut lifecycle içinde abonelik kaldırılır. Mevcut yeni oyun akışı bekleyen arka plan yazımını tamamlar, ekonomi/sipariş/teslimat kaydını atomik sıfırlar ve teslimat yöneticisinin NewGame temizliğini çalıştırır. Yeni bir kayıt sistemi veya ikinci reset uygulaması yazılmadı.
- Sahne/prefab/ScriptableObject yapılandırması değiştirilmedi; ek Inspector ataması gerekmiyor. Başlangıç parası mevcut EconomySettings.startingBalance değeridir (test sırasında 10.000 TL).

Play Mode testi: MarketRestartChecks.cs, gerçek TidyPauseMenu.Pause ve restartButton.onClick akışını kullanarak sahneyi yeniden yükledi. 15 kontrol başarılı: satın alınmış taşın varlığı; yazım hatasında yeniden yüklemenin iptali ve önceki paranın korunması; devam eden teslimat sırasında restart; başlangıç bakiyesi; boş siparişler ve teslimat kayıtları; satın alınmış/kısmi teslimat taşlarının silinmesi; boş kuyruk ve başlangıçtaki kamyon; diskten yeniden okunduğunda temiz kayıt; bilgisayar bakiye etiketi ve Siparişlerim; eski coroutine/yazımın taşları geri getirmemesi.

Son derleme başarılı, Console'da mevcut hata yok. Bir warning, testte kasıtlı üretilen “restart write failed” hatasının kullanıcıya bildirildiği uyarıdır. Klavyeden fiziksel ESC tuşu otomatik basılmadı; ESC'nin açtığı aynı menü ve gerçek buton olayı programatik tetiklendi. Standalone build çalıştırılmadı.

Testler ayrı ekonomi dosyası ve bellekte framework depolaması kullanılarak yapıldı. Gerçek ekonomi/level/progress kayıtları önce/sonra aynı hash'e sahip. Test ayarları kaldırıldı, test taşları kalmadı. Mevcut oyuncu kaydı test amacıyla sıfırlanmadı; artık oyuncu Restart'a bastığında sıfırlanacak.

Çıktılar: Market-restart-results.json ve Market-restart-final-audit.json. Test yardımcı kodu Assets/PC/Tests~/MarketRestartChecks.cs altında, üretim derlemesinin dışında tutulur.
