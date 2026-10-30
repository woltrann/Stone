Etkileşimli bilgisayar sistemi — 29 Ekim 2026

Sonraki istek üzerine güncelleme: ekrana hedefleme sırasında outline artık PC/Monitor 2 üzerinde gösteriliyor. InteractiveComputer içindeki Outline Renderers alanına Monitor 2'nin LOD0 renderer'ı atandı; Screen kamera/UI referansı olarak kaldı. UI/PCMonitorOutlineMesh.asset, kaynak monitör modelini değiştirmeyen ve framework UV3 kenar normallerini taşıyan özel kopyadır. Monitör outline materyali LessEqual derinlik testi kullanır. Play Mode’da hover açık/kapalı durumları ve gerçek Game View görüntüsü doğrulandı; PC-Monitor2-outline.png güncel görseldir. Aşağıdaki Screen outline açıklamaları ilk teslimin tarihçesidir. Bu güncellemede 51 kontrolün tamamı yeniden çalıştırılmadı; derleme, hedef bağlantısı ve görsel hover davranışı kontrol edildi.

1. Tamamlanan davranış

TidyUp_Warehouse sahnesindeki mevcut PC çalışır biçimde bağlandı. Ekranı önden ve 2,5 metre içinde hedefleyince yalnızca Screen vurgulanıyor; mevcut HUD üzerinde “İnternete erişmek için [E] tuşuna bas” yazıyor. E ile kamera 0,75 saniyede monitöre yaklaşıyor. Ekrandaki World Space arayüzünde STONE MARKET, Taş Satın Al, Siparişlerim ve Çıkış bulunuyor. İlk iki buton Console mesajı ve gelecekte bağlanabilecek C# olayları üretiyor. ESC veya Çıkış, kamerayı 0,65 saniyede geri döndürüyor. Taş tutarken PC kullanımına giriş engelleniyor; taş kendiliğinden bırakılmıyor.

2. İncelenen ve kullanılan mevcut altyapı

TidyFirstPersonController hareket, kafa ve yaw/pitch yönetiyor. TidyInteractor mevcut fizik sorgusu ve taşıma sistemini sağlıyor. TidyControlLock sahip bazında hareket/bakış/etkileşim/cursor kilitlerini, TidyCursorController cursor politikasını yönetiyor. TidyInteractionPromptView mevcut HUD yazısını, TidyOutlineHighlighter mevcut outline shader’ını kullanıyor. TidyPauseMenu ve TidyScreenStack ESC işliyor; PC boyunca geçici olarak askıya alınıyorlar. Sahnedeki tek EventSystem ve InputSystemUIInputModule korundu. Butonlarda mevcut TidyButtonFeedback kullanıldı. URP incelendi; projede DOTween bulunmadığı için paket eklemeden coroutine kullanıldı.

3. Yeni çalışma zamanı scriptleri — Assets/PC/Scripts

- InteractiveComputer.cs: hedefleme, Idle/Entering/Using/Exiting durumları, kamera geçişleri, kontrol kilitleri, geri yükleme ve buton olayları.
- InteractiveComputerSettings.cs: mevcut PC’ye atanmış ScriptableObject üzerinden mesafe, tuş, yazı, outline, kamera ve emission ayarları.
- ComputerDesktopView.cs: World Space Canvas ve üç butonun bağlantıları; kullanım dışında tıklamayı kapatır.
- ComputerScreenEmission.cs: ekranın emission değerini MaterialPropertyBlock ile uygular, ortak materyal örneklerini değiştirmez.

4. Değiştirilen ortak scriptler

- Assets/TidyUpFramework/Runtime/Interaction/TidyInteractor.cs: mevcut TryFindAimed metoduna isteğe bağlı mesafe parametresi eklendi. Eski çağrılar aynı varsayılan mesafeyi kullanır.
- Assets/TidyUpFramework/Runtime/Interaction/TidyOutlineHighlighter.cs: aynı çizim sistemi içinde sahibi belirtilen özel görünüm materyali desteği eklendi. PC yalnızca Screen renderer’ını gönderir; taşların mevcut stilleri korunur.
- Assets/TidyUpFramework/Runtime/UI/TidyInteractionPromptView.cs: sahibi belirtilen geçici dış prompt ve font desteği eklendi. PC bitince normal prompt/font geri gelir.
- Assets/TidyUpFramework/Runtime/Feedback/TidyCameraPunch.cs: geçici kamera darbesini bitiren Cancel metodu eklendi. PC girişinde mevcut görünür pozdan yumuşak geçilir; dönüş hedefi darbenin kalıcı olmayan ofsetini içermez.

5. Assetler ve sahne düzeni

Yeni assetler PC altında: Settings/PCSettings.asset, Materials/PCScreen.mat, Prefabs/ComputerDesktop.prefab, UI/PCInterfaceFont.asset ve UI/PCScreenMesh.asset. Türkçe font atlası PC’ye özeldir. Ekran mesh’inin özel kopyasına framework’ün TidySmoothNormalBaker aracıyla UV3 kenar normalleri hazırlandı; kaynak mesh değiştirilmedi. Mevcut bilgisayar modelleri yerinde kullanıldı.

PC köküne InteractiveComputer ve PCViewPoint eklendi. Screen altına ComputerDesktop prefab örneği, Screen’e ince BoxCollider ve ComputerScreenEmission eklendi. Tüm gerekli referanslar bağlandı. Desktop altında başlık, açıklama, üç buton ve ESC ipucu var. World Space Canvas ekranın fiziksel boyutuna ve ölçeğine uyar; GraphicRaycaster ve mevcut EventSystem üzerinden çalışır.

Masanın geniş BoxCollider’ı monitor ışınını engelliyordu. Bu bileşen silinmeden kapatıldı; mevcut masa mesh’i ve fizik materyaliyle statik MeshCollider eklendi. Screen’in Batching Static bayrağı kapatıldı: aksi halde Play Mode’da birleşik sahne mesh’i ekranın merkez hesabını ve outline çizimini bozuyordu. Diğer statik bayraklar korundu.

PC outline materyali World Space UI’dan önce çizilir. Çerçevenin ekran çizgisini örtmemesi için özel görünüm materyalinde depth testi Always kullanılır; görünürlüğün mesafe, ön taraf ve katı engel denetimini mevcut TidyInteractor sorgusu yapar. Monitör kasası ve masa outline listesine alınmaz.

6. Inspector ayarları

Eksik zorunlu bağlantı bırakılmadı. PC kökündeki Settings alanından PCSettings.asset açılabilir. Interaction Distance, Interaction Key, Exit Key, Prompt, Outline Color/Width, Enter/Exit Duration, Screen Padding, Near Clip Clearance ve Emission Color/Intensity değiştirilebilir. {key}, seçilen tuşla değiştirilir; ESC her zaman çıkış sağlar.

PCViewPoint konumu düzenlenebilir. Automatically Frame Screen açıkken kamera ekran merkezine döner ve dört köşeyi sığdıracak mesafeyi hesaplar. Tamamen elle konum/rotasyon kullanmak için bu ayar kapatılabilir. UI metinleri ve butonlar ComputerDesktop prefabından düzenlenebilir. Screen mesh’i ileride değiştirilirse outline UV3 verisi ve Batching Static ayarı yeniden kontrol edilmelidir.

7. Kamera, cursor ve kontrol güvenliği

Girişte kamera ebeveyni, yerel/dünya pozu, FOV, yaw/pitch ve bileşenlerin önceki etkinlik durumları saklanır. Geçiş ve kullanım boyunca mevcut oyuncu kontrolü, interactor, kamera darbesi ve pause işleyicileri askıya alınır; global kilitler dünya etkileşimlerini de durdurur. Kullanımda cursor görünür ve serbesttir. Çıkışta UI hemen kapanır, normal cursor politikası döner; hareket ve bakış ancak kamera dönüşü ve çıkış tuşunun bırakılması tamamlanınca açılır. Aynı ESC Pause menüsüne geçmez. PC devre dışı bırakılsa bile kendi kilitleri temizlenir, diğer sahiplerin kilitleri korunur.

8. Gerçekleştirilen doğrulamalar

PCIntegrationChecks.cs son turu: 51 başarılı kontrol, 0 başarısızlık; 1668 oyun karesi ilerledi. Unity Input System’e geçici klavye/fare aygıtları üzerinden olaylar gönderildi; üretim kodu ve mevcut UI input modülü bu olayları işledi. Buton onClick metodunu testten doğrudan çağırarak başarı varsayılmadı.

Kontroller: uzaklık, önden hedefleme, katı engel, başka yöne bakma, tam Türkçe prompt, yalnızca Screen outline kaydı, emission, E ile gerçek giriş, tekrarlı E, yumuşak kamera hareketi, kontrol kilitleri, görünür/serbest cursor, fare bakışı/WASD/zıplama/bırakma engeli, 16:9–4:3–9:16 oranlarında ekranın dört köşesinin görünmesi, üç butonda gerçek pointer hover/pressed/click, UI çıkışı, ESC çıkışı ve basılı ESC’nin Pause’a geçmemesi, girişin iptali, tekrar giriş, kamera pozu/FOV/yaw/pitch dönüşü, dönüş sonrası gerçek WASD ve mouse look, PC kapanırken temizleme, başka kilit sahibinin korunması, mevcut ham taşı alma/tutma/bırakma ve taş tutarken PC’nin reddi.

PCCameraEffectChecks.cs: aktif kamera darbesiyle giriş/çıkış ayrıca ölçüldü; düzeltme sonrasında konum hatası 0 ve FOV hatası 0. Görsel kontrol için PC-in-use.png (1440×900 kamera görüntüsü) ve PC-gameview-hover.png (HUD ve outline dahil gerçek Game View) incelendi. Editörün mevcut Game View’u 392×207 olduğundan ikinci görüntü büyütülmüş ve pikselli görünür; yüksek çözünürlüklü ilk görüntüde arayüz ayrıca doğrulandı.

Play Mode kapanışında ayrıca bir temizleme hatası yakalandı: hover prompt’u kapanırken UI yenilemesi, daha önce yok edilmiş framework tween host’unu tekrar oluşturabiliyordu. InteractiveComputer artık OnApplicationQuit sonrasında UI yenilemeden yalnızca kendi outline/kilit sahipliğini temizler. Düzeltmeden sonra hem hover açıkken hem State=Using iken Play Mode kapatıldı; iki senaryoda da yeni Console hatası oluşmadı. Normal oyun içi devre dışı bırakmada tam geri yükleme davranışı korunur.

Son script derlemesi başarılı, derleme hatası yok. Güncel canlı Console kontrolünde yeni runtime hata/uyarı yoktu. Pipeline geçmişinde framework’ün önceden mevcut CS0618 obsolete API uyarıları ve bu çalışmadan önceki bir paket içe aktarma hata kaydı bulunuyor; bunlar bilgisayar mekaniğiyle ilgili olmadığından kapsam dışı dosyalar değiştirilmedi. Derleme sonrası geçici “Hold on...” durumunda bir Play Mode komutu bağlanamadı; editör hazır olunca son testler tamamlandı.

9. Kayıt ve proje güvenliği

İlk Play Mode’dan önce mevcut kayıt içerikleri yerel test yedeğine alındı ve TidyLevelManager saveProgress kapatıldı. Test oturumları kapatıldıktan sonra eski kayıt ayarı geri yüklendi ve sahne kaydedildi. Seviye ve ilerleme kayıtlarının test öncesi/sonrası SHA-256 değerleri birebir aynı; PC-final-audit.json içinde bulunur. PC hiyerarşisinde eksik script sayısı 0. Arka planda çalışma test ayarı eski kapalı durumuna döndürüldü. Git işlemi, paket indirme, mevcut sahne objesi silme veya ortak kaynak materyal değiştirme yapılmadı.

10. Sınırlar ve yardımcı dosyalar

Bağımsız oyuncu build’i, fiziksel kullanıcı eliyle fare/klavye oturumu ve bu görevin dışında kalan taş kesme akışının tamamı yeniden test edilmedi. Taş satın alma, sipariş oluşturma ve ekonomi işlevleri istenen kapsam gereği eklenmedi; buton olayları sonraki aşama için hazır.

Tests~/ altındaki dosyalar tek seferlik sahne kurulum/inceleme, input tanılama, Play Mode kontrolleri ve son kayıt denetimi için kullanılan CLI yardımcılarıdır; Unity bunları oyun scripti olarak derlemez. SaveBaseline.json yerel test öncesi kayıt yedeğidir. PC-test-results.json, PC-camera-effect-results.json ve PC-final-audit.json gerçek test çıktılarıdır. Documentation~/ içeriği de oyun build’ine asset olarak alınmaz.
