# Vuruş noktasına göre parça parça yontma

Her başarılı çekiç vuruşu dünya temas noktasını ve normalini CutStoneHalf bileşenine iletir. Tamamlanmadan önce yalnızca temas çevresindeki küçük yüzey üçgenleri taşın mevcut kesit materyaline geçirilir. Önceki izler korunur. Altıncı (profilin RequiredHits değeri) vuruşta tüm taş aynı kesit materyaline döner. Süre, kontroller, hedefleme, çekiç animasyonu ve fizik davranışı değiştirilmedi.

## Değişiklikler
- TABLE/Chipping/Scripts/CutStoneHalf.ChipPatches.cs: taş başına görsel mesh kopyası, sınırlı alt bölme, düzensiz kenarlı yerel materyal bölgeleri ve bellek temizliği. Yeni shader, paket, model veya materyal oluşturulmadı. Her karede hesaplama yapılmaz; yalnızca vuruşta/kayıt yüklemede çalışır. Ek bölme iki tur ve 12000 üçgen bütçesiyle sınırlıdır; zaten büyük kaynak meshler daha fazla bölünmez.
- CutStoneHalf.Chipping.cs: konum/normal alan TryChip overload'u ve v3 kayıt. İzler taşın yerel koordinatlarında saklanır; taşıma/döndürme sırasında taşla birlikte hareket eder. v1/v2 eski kayıtlar korunur; geçmişte kaydedilmeyen vuruş konumları uydurulmaz. Konumsuz mevcut TryChip API'si çalışmaya devam eder.
- StoneChippingTable.cs: gerçek metal baş temasında konum ve normal yeni overload'a gönderilir.
- STONE/Scripts/Cutting/CutStoneHalf.cs: taş yok olurken geçici görsel mesh temizlenir.
- STONE/Scripts/Cutting/StoneMeshGeometry.cs: görsel ek materyal yuvası kaynak geometri kaydına karışmaz; kaynak materyal düzeni kaydedilir. Bu, kısmen yontulmuş taşların yeniden oluşturulmasında yanlış materyal/yuva sayısını önler.

## Çalıştırılan testler
- Unity derlemesi: 0 hata, 0 uyarı.
- ProgressiveChipChecks: 51 kontrol geçti. Ayrı konumlarda beş yerel alanın birikmesi, dokunulmayan alanların korunması, tam materyal kimliği, değişmeyen kaynak geometri/fizik collider meshleri, reset, v3 kayıt dönüşü, yeni örnekte kaynak + yarım taş kaydından yeniden oluşturma, taşıma/döndürme/ölçek, altıncı vuruş, fazladan vuruş reddi, eski v2 kayıt ve NaN hedef reddi.
- ProgressiveHammerRegression: 41 kontrol geçti. Kullanıcının Stein (2)_right taşının aynı 4434 vertex geometrisi ve önceki oyuncu konumu kullanıldı. 319/319 görünür nokta hedeflenebildi. 18 fiziksel fare tıklaması; altı ardışık vuruşta bölgesel ilerleme ve son vuruşta tam dönüşüm. Metal baş temas hatası yaklaşık 0.0012 mm, sap tutuş merkezli salınım korundu.
- ChippingPlayChecks: mevcut uçtan uca akış geçti; mağaza/sipariş API, teslimat, gerçek testere, iki yarım taş, mevcut yerleştirme girdisi, masa kontrol kilitleri, UI engelleme, ESC, alma/bırakma, boş taş, tamamlanmış taş ve kayıt yeniden oluşturma.
- Test görüntüleri: Progressive-hit-1.png, Progressive-hit-3.png, Progressive-hit-6.png. Görsel olarak incelendi.
- Editör testinde ilk (soğuk) hazırlama yaklaşık 68.5 ms, sonraki vuruşların en yükseği yaklaşık 2.15 ms ölçüldü. Bu ölçüm JIT/editör maliyetini içerir; build performans ölçümü değildir. İlk hazırlama önceki 165 ms değerinden düşürüldü. Aynı sahnedeki tüm taşlar için arka planda çalışan bir döngü eklenmedi.

## Güvenlik ve kullanım
Ek Inspector bağlantısı gerekmez. İlk 1–5 vuruşta vurulan farklı noktalar açılır; aynı yere tekrar vurmak aynı bölge üzerinde kalabilir. Son vuruşta tamamı dönüşür. Fiziksel parça koparma/oyuk oluşturma eklenmedi; istenen yerel materyal görünümü uygulanır.
Testler ayrı ekonomi dosyası ve bellek içi framework kayıt deposuyla çalıştı. Kendi Play Mode oturumları kapatıldı; başlangıç kayıt ayarları geri getirildi. Gerçek ekonomi, level ve progress kayıt metinleri önce/sonra aynı doğrulandı. Kullanıcının önceden değişmiş ve kaydedilmemiş sahnesi zorla kaydedilmedi.
