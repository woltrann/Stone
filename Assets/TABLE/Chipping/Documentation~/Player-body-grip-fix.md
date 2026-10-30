# Oyuncu collider'ı, çekiç salınımı ve yakın kamera — 29 Ekim 2026

Kalıcı üretim düzeltmesi StoneChippingTable.cs içine uygulandı; sahnede HammerGripPoint ve yakın TableCameraPoint ayarları kaydedildi. Önceki salt okunur teşhis/taslak aşamasından farklı olarak bu sürüm gerçek oyunun derlenen scriptidir.

## Bulunan neden

Kullanıcının açık oyunundaki **Stein (2)_right**, 4434 vertex, 2/6 ilerleme ve oyuncu konumu (-1,20;0,03;10,43) üzerinden incelendi. İlk 334 görünür mesh noktasının tamamını TidyPlayer collider'ı engelliyordu: hedeflenebilir 0/334. Önceki genel testin yaklaşma konumu bu vücut engelini üretmiyordu.

Çalışma kamerası karakterin normal göz konumundan ayrılırken karakterin fizik collider'ı yerinde kalır. Düzeltme yalnız masanın pointer sorgusunda oyuncunun kendi collider'ını ve çocuklarını dışlar; collider devre dışı bırakılmaz, yürüyüş fiziği değiştirilmez. Diğer taş/masa/duvar ve UI engelleri korunur.

Kesit submesh filtresi kaldırıldı. Aktif, tamamlanmamış kesilmiş taşın ekrandan erişilebilen **bütün submesh yüzeyleri** kabul edilir. Masanın/duvarın arkasından veya tamamlanmış taşa vurma açılmadı. Kesit normalde altta kalır; görünür hale getirildiğinde de yontma açısından reddedilmez.

## Animasyon

Eski vuruş metal temasını hedefe taşıyor ancak local yüz ekseni çevresinde dönüyordu. Yeni salınımın pivotu kauçuk sapın tutuş merkezi **HammerGripPoint**, yerel yaklaşık (0,01;0,02;-0,10). Metal yüzün uç noktası mevcut HammerImpactPoint olarak korunur.

Hazırlanma → tutuş merkezinden aşağı salınım → metal baş teması → geri toplama evreleri vardır. Aşağı vuruş evresinde tutuş merkezi sabittir; metal baş yay çizer. Salınım ekseni, başın vuruş normaline ve grip→head koluna dik hesaplanır. Son temasta demir yüzü mesh normaline hizalanır. Varsayılan Swing Degrees 55; mevcut Hit Duration korunur. Cursor çekici alınca gizli kalır.

## Kamera

TableCameraPoint dünya konumu **(-1,13;1,85;10,05)**; bakış yönü (0;-0,57;1,15). Önceki konum (-1,13;2,10;9,55) idi. Kamera öne ve aşağı alınarak taş büyütüldü. Pano üst kısmının bir bölümü dışarıda kalabilir; çekiç yuvası kadrajda ve tıklanabilir. Kamera FOV'u değiştirilmedi.

## Gerçek testler

Kullanıcının taşının mesh geometrisi, ID'leri, iki yarının ilerlemesi ve oyuncu konumu ayrı fixture'a salt okunur alındı. Gerçek kayıtlar üzerinde vuruş yapmadan, aynı **Stein (2)_right** geometrisi ve 2/6 durumu izole test oturumunda yeniden kuruldu.

- Yeni yakın kamera ile görünür yüzeyde **319/319 nokta** algılandı. Aynı noktalarda oyuncu vücudunun halen fiziksel engel olduğu ayrıca üretildi; sorgu onu doğru dışladı.
- Taşın farklı bölgelerinde 10 sol tık, bir uzun salınım doğrulaması ve görünür hale getirilen kesit yüzünde bir sol tık: **12/12 Input System tıklaması** başarılı.
- Metal başın temas hatası en çok yaklaşık **0,0000012 m**. Kauçuk sapın en yakın noktası darbe düzleminden en az **0,0318 m yukarıda** kaldı.
- Kare örneklemesinde aşağı vuruş esnasında tutuş merkezinin kayması 0,1 mm altında; demir başın yay hareketi 1 cm üzerinde. Sapın kendisinin vurması yerine başın hareketi doğrulandı.
- Yakın kamera ve çekiç yuvasının kadrajda kalması geçti. UserStoneRegressionChecks: 23 kontrol başarılı.
- Standart ChippingPlayChecks de yeniden geçti: E/ESC, cursor, preview/yerleştirme, gerçek sol mouse, UI/yanlış hedef engeli, bağımsız ilerleme, kayıt/yeniden kurma ve materyal.

Ham çıktı: User-stone-regression-results.json; görüntü: User-stone-close-camera.png. İlk teşhis: User-stone-coverage-before.json. Test kopyasının vuruş sayısı her denemede korunmuş başlangıç verisine döndürüldü; gerçek oyuncu ilerlemesi değiştirilmedi.

Derleme başarılı; son Console ground truth 0 error ve 0 warning. Gerçek ekonomi/level/progress dosyalarının önce/sonra hash'leri aynı (Player-body-final-audit.json). Test ayarları kaldırıldı, kendi Play Mode kapatıldı, sahne gerçek ayarlarıyla kaydedildi. Standalone build testi yapılmadı.

Sahne bağlantıları tamamlandı; kullanıcı ek ayar yapmadan oyunu yeniden başlatabilir. Yeni hedef mesajı **“Taş hedefte — sol tıkla vur”**. Çekicin demir ucunu masadaki taşın herhangi bir görünen kısmına getirip sol tıklamak yeterli; her vuruş animasyonunun bitmesi beklenir.
