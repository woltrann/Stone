# Çekiç başı ile hedefleme ve imleç — 29 Ekim 2026

StoneChippingTable.cs düzeltildi. Önceki kod modelin kökünü hedefe taşıyor; takip görseline ekran düzleminde offset ve yukarı kaldırma ekliyordu. Dolayısıyla görünen demir başının konumu tıklamayı doğrulayan gerçek mouse ray'iyle örtüşmüyordu.

Mevcut WarehouseHammer mesh'inin M_Metal_Steel submesh'i incelendi. Metal başın dış yüzüne HammerImpactPoint child'ı eklendi; yerel konumu (0.07574, 0.02286, 0.14421), forward ekseni yerel +X. Table Inspector'daki Hammer Impact Point alanına bağlıdır. Yeni model gerekmedi.

- Çekiç seçildiğinde cursor gizlenir; lockState None kalır, mouse serbestçe hareket eder.
- Metal temas noktası gerçek mouse ray'inde tutulur; modelin pivotu veya sapı hedef olarak kullanılmaz. Dönüş yumuşatılır; temas noktasının ekrandaki konumu geciktirilmez.
- Preview ve tıklama aynı hedef sorgusunu kullanır. Çekicin kendi collider'ları vuruş ray'ini engellemez; mevcut katı duvar/masa, başka taş ve UI reddi korunur.
- Darbe anında temas noktası gerçek RaycastHit.point'e, metal yüzün yönü de yüzey normaline hizalanır. Kare zamanı ideal darbe anını atlasa bile temas bir kare tam olarak uygulanır; vuruş bir kez sayılır.
- Çekiç panoya veya ESC ile iade edilince cursor yeniden görünür; çalışma modundan çıkınca önceki oyun cursor/kontrol durumu geri gelir.

ChippingSetup.cs test/kurulum yardımcısı yeni temas noktası referansını kurabilecek şekilde güncellendi. Mevcut materyal/yontma ilerleme mantığı, pickup ve kamera yerleştirme akışı değiştirilmedi. Başka çekiç modeli kullanılırsa Hammer Impact Point'i metal vuruş yüzüne yerleştirip forward eksenini yüzün dışına yöneltmek yeterlidir. Mevcut sahnede kullanıcı ayarı gerekmez.

ChippingPlayChecks genişletildi ve ayrı kayıt üzerinde **55 kontrol geçti**. Gerçek Input System sol mouse basılı tutma bir vuruş üretir; metal başın projeksiyonu gerçek pointer'a 0,5 pikselden yakın; altı gerçek darbenin her birinde temas hatası 0,0001 metrenin altında. Cursor gizleme/iade, E/iki aşamalı ESC, yanlış taş/masa/boşluk/UI engelleri, ilerleme, kesit materyali ve snapshot restore kontrolleri de geçti.

Derleme başarılı; son Editor Console ground truth 0 error, 0 warning. Gerçek ekonomi/level/progress dosyaları değişmedi (Hammer-head-final-audit.json). Test dosyaları/ayarları temizlendi, kendi Play Mode oturumu kapatıldı ve sahne kaydedildi. Standalone build çalıştırılmadı.
