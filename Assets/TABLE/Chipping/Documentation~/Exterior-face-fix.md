# Kesik yüz aşağı, dış yüzeye vuruş — 29 Ekim 2026

Güncelleme: son istekle kesit dahil tüm erişilebilir yüzeylere vuruş açıldı ve oyuncunun kendi collider engeli giderildi. Güncel davranış için Player-body-grip-fix.md dosyasını kullanın; aşağıdaki metin önceki dış-yüzey filtresi sürümünün tarihsel raporudur.

StoneWorkSlot.cs, StoneChippingTable.cs ve ilgili Play Mode testi değiştirildi.

Yerleştirme, gerçek CutSurfaceNormal yönünü masa normalinin tersine çevirir. Pivot genel local-Y bounds köşesinden değil CutSurfaceCenter noktasından hesaplanır: kesit merkezi doğrudan çalışma noktasına gelir. Böylece kesik yüz alta oturur, taşın kendi mesh'i masanın içine gömülmez. Framework snapshot'ı eski yerleştirilmiş rotasyonu yeniden uyguladığı için Restored olayında bu slota ait taşın pozu tekrar düzeltilir; ilerleme sıfırlanmaz.

Vuruş hedeflemesi artık aktif taşın görünen GeneratedMesh üçgenlerini kullanır. Taşın fizik convex/voxel proxy collider'ları tıklama siluetini belirlemez. Diğer katı masa/duvar/taş collider'ları engelleyici olmaya devam eder; UI reddi korunur. Önbelleğe alınan vertex/indeks verisi yalnız aktif taş değişince hazırlanır, geometri veya fizik collider'ları değiştirilmez. Kesit submesh'ine ait ilk görünür üçgene vurulamaz; yalnız dış yüzey kabul edilir. Çekiç metal temas noktası aynı kesin mesh hedefini izler.

Kullanım: taşı masaya yerleştir, E ile çalış, çekici al. Metal ucunu taşın görünen pürüzlü/bombeli üst veya dış yan yüzeyine getir. “Dış yüzey hedefte — sol tıkla vur” mesajını gördüğünde sol tıkla; sonraki vuruş için animasyonun bitmesini bekle. Kesik yüz artık alttadır. Masa, boşluk, başka taş ve UI vuruş sayılmaz. 6/6 tamamlanmış taşa tekrar vurulmaz. Çekiç eldeyken cursor gizlidir; ilk ESC çekici bırakır, ikinci ESC çalışma modundan çıkar.

ChippingPlayChecks son turda 121 kontrolü geçti (siluet örnek sayısı rastgele taş şekline bağlıdır). 29 farklı görünür taş pikselinde özel mesh hedeflemesi bağımsız, yalnız test süresince oluşturulan exact MeshCollider.Raycast ile karşılaştırıldı; hedef noktası farkı 1 mm altında kaldı. Gerçek Input System sol mouse tıklaması bir vuruş üretti; metal temas hatası 0,1 mm altında. Kesit alt yüzü reddi, masa üstünde geometri, eski face-up kayıt pozu düzeltmesi, 3/6 devam, altı darbe, materyal, yanlış hedef/UI ve E/ESC kontrolleri geçti.

Gerçek oyun örneği Exterior-click-guide.png dosyasındadır. Test için eklenen exact collider kaldırıldı; üretim nesnesine ek collider veya mesh oluşturulmadı. Son derleme başarılı; Console ground truth 0 error, 0 warning. Gerçek ekonomi/level/progress kayıtlarının hash'leri değişmedi (Exterior-face-final-audit.json). Test ayarları kaldırıldı, test Play Mode kapatıldı ve sahne kaydedildi. Standalone build çalıştırılmadı.
