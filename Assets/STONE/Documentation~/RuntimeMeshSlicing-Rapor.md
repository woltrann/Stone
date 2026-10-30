# Runtime Mesh Slicing — 27 Ekim 2026

Gerçek mesh kesimi mevcut testereye entegre edildi. Yeni kesimler sol/sag prefablarını kullanmaz; oyuncunun koyduğu taşın o anki MeshFilter geometrisinden iki kapalı mesh üretilir. Mevcut pickup, taşıma, yerleştirme, T tuşu ve iki aşamalı cevher olasılığı korunur. Sahne kaydedildi, Play Mode kapatıldı, geçici test ayarları geri alındı.

## Değiştirilen scriptler

| Dosya | Görevi / değişiklik |
| --- | --- |
| Scripts/Cutting/StoneSawMachine.cs | CuttingPlane, kesit materyali ve geometri/fizik ayarları; süre sonunda gerçek mesh servisi ve parça fabrikasını çağırır. İki sonuç başarılı olmadan kaynağı tüketmez. Hata halinde kısmi sonuçları temizler. |
| Scripts/Cutting/RawStone.cs | Gerçek mesh verilerinin snapshot'a yazılması ve geri oluşturulması; ayrı meshVersion; eski prefab kayıtlarına uyumluluk; kayıt yeniden oluşturulamıyorsa kaynak taşın korunması. Cevher yeniden seçilmez. |
| Scripts/Cutting/CutStoneHalf.cs | Runtime mesh/physics kaynaklarının sahipliği ve temizliği; gerçek kesit merkezi, normal, yarıçap, cap submesh ve hacim bilgileri; geometriden türeyen cevher görseli. |
| Scripts/Cutting/StoneCutProfile.cs | Sağ/sol prefab alanları yeni kesim için devre dışı ve Inspector'da gizli. Alanlar ve assetler eski kayıtları okuyabilmek için korunur. Olasılık algoritması değişmedi. |
| Scripts/ProceduralRocks/RockMeshSlicer.cs | Mevcut kesici genişletildi: dış yüzey normalleri/UV0/renkler, kapak normalleri, tangent üretimi, ayarlanabilir tolerans, kontur bütçesi; üçgen başına geçici dizi/listeler azaltıldı. Yakın düzlem vertex'leri için T-junction düzeltildi. |
| TidyUpFramework/Runtime/Objects/TidyObject.cs | Tek bir AssignPhysicsProfileOverride metodu: parçanın kütle oranını mevcut fizik durum geçişlerinde korur. |
| TidyUpFramework/Runtime/Interaction/TidyPlacementPreview.cs | Runtime mesh yok edildiğinde eski önizleme referansının güvenle atlanması. |
| FrameworkIntegration/ITidyObjectSnapshotParticipant.cs | İsteğe bağlı kayıt kurtarma arayüzü eklendi. |
| TidyUpFramework/Runtime/Save/TidyLevelSnapshotter.cs | Kesilmiş sonuç yeniden oluşturulamadıysa kaynak nesneyi tüketilmiş/gizli bırakmak yerine alınabilir durumda korur. |

OreDefinition ve cevher ağırlıkları yeniden yazılmadı. HamTas üzerindeki mevcut %100 ayarın korundu; testlerin %0/%100 ayarları yalnız geçici kopyalara uygulandı.

## Yeni scriptler

- StoneRuntimeMeshSlicer.cs: aktif statik MeshFilter/MeshRenderer çiftlerini toplar, dönüşümleri uygular, iki gerçek yarıyı ve kesit bilgisini üretir.
- StoneMeshGeometry.cs: tam geometri snapshot'ı, kapalı manifold kenar/hacim denetimi ve güvenli kesit noktası hesabı.
- StoneCutPieceFactory.cs: TidyObject, mesh, materyaller, Rigidbody, Collider, cevher ve fizik profilini yeni parçalara bağlar; çıkışların birbirine girmesini önler.
- Tests~/RuntimeSliceGeometryChecks.cs ve RuntimeSliceSafetyChecks.cs: gerçek Editor/Play Mode doğrulamaları. Mevcut PlayChecks ve OreVisualChecks yeni geometriye uyarlandı.

## Algoritma ve kesit

1. Kaynak taşın aktif, görünür MeshFilter geometrileri okunur. Child transform'lar ve parent zincirindeki dönüşüm/ölçek/shear, birim ölçekli ortak bir referans uzayında vertex'lere uygulanır. Ters ölçek için winding düzeltilir. Normal dönüşümünde inverse-transpose kullanılır.
2. Her kaynak submesh kendi materyal indeksiyle taşınır; sonuçta bunlara bir cap submesh eklenir. UV0 ve vertex renkleri korunur. Kaynak normal yoksa normal üretilir; sonuç tangent'leri yeniden hesaplanır.
3. CuttingPlane'in local YZ düzlemi kullanılır. Normali local X eksenidir; world normal inverse-transpose ile hesaplandığından düzlemin kendi parent'ındaki non-uniform scale/shear de hesaba katılır.
4. Mevcut RockMeshSlicer iki ters yarı-uzay için çalışır. Kesişen üçgenler kırpılır; kesişim vertex'lerinde UV ve normal interpolasyonu yapılır. Kesilmeyen geometri korunur. Tolerans içinde kalan vertex'ler aynı düzleme projekte edilir; yakın düzlemde kapak ile yüzey arasında farklı uç noktaları oluşmaz.
5. Sınır segmentleri toleranslı uzamsal eşleştirmeyle kapalı konturlara bağlanır. Basit, girintili konturlar ear clipping ile üçgenlenir; triangle fan kullanılmaz. Birbirinden ayrı birden fazla kontur desteklenir. İç içe delikli konturlar açık uyarıyla reddedilir.
6. Cap normalleri her yarı için dışarı bakar; UV'leri düzleme projeksiyonla oluşturulur. Çıktıların kenar eşleşmesi ve hacimleri kontrol edilir. Toplam hacim kaynak hacmiyle tolerans içinde uyuşmalıdır. Anlamsız ince parçalar reddedilir.

Parçalar çıkış noktalarının rotasyonuna yönlenir. Kaynak ölçeği geometriye işlendiği için ölçek ikinci kez çarpılmaz. Ayrılma mesafesi yalnız sonuçları konumlandırır; taştan testere kalınlığı kadar hacim eksiltilmez.

## Cevherin görünmesi

Cevher varsa cap üçgenlerinin içinde kalan aday noktalar değerlendirilir. Kontur sınırına uzaklığı uygun olan bir nokta ve güvenli yarıçap seçilir. Yeni GeneratedCutSurface transform'u bu noktada oluşturulur; önceden hazırlanmış OreSocket gerekmez.

Cevher modeli kesit normaline yönlenir, prefab pivotu ve yazılmış ölçeği hesaba katılarak güvenli kesit yarıçapına sığdırılır. Varsayılan merkez kesit düzlemi üzerindedir; modelin bir kısmı taşın içinde kalır. Boolean oyuk açılmaz. Her iki yüz aynı taşın önceden belirlenmiş cevher bilgisini taşır. Boş taşlarda cevher görseli oluşmaz.

Gelecek mekanikler GeneratedMesh, CutSurfaceCenter, CutSurfaceNormal, CutSurfaceRadius, CutSurfaceSubmesh, Volume, SourceStoneId, SourceProfile ve ContainedOre üzerinden geometri ve veriye erişebilir.

## Fizik ve kayıt

Basit parçalar, hem üçgen hem benzersiz vertex bütçesi uygunsa Convex MeshCollider kullanır. Karmaşık/parçalı geometride PhysX'in convex yüz sınırını aşmamak için sınırlı çözünürlükte muhafazakâr compound BoxCollider üretilir. Ardışık hücreler birleştirilerek collider sayısı azaltılır. Bu fallback yaklaşık bir çarpışma hacmidir; çok küçük girintilerin birebir fizik modeli değildir.

Kaynağın etkileşim modu, fizik profili, sönümleme ve uygun Rigidbody ayarları korunur. Kütle hacim oranına göre paylaşılır. Profil kopyaları her parçaya özeldir; kaynak ScriptableObject değiştirilmez. Yeni parçalar mevcut TidyObject alma/bırakma akışını kullanır.

Snapshot; vertex, normal, UV0, renk, submesh indeksleri, kesit bilgisi, hacim, poz ve cevher sonucunu saklar. Geri yükleme yeni mesh üretir; başka bir rastgele taş üretmez. Materyaller kaynağın materyal listesi ve testerenin kesit materyali üzerinden eşleştirilir. Asset adlarını/kaynak materyal listesini kayıtlar çıktıktan sonra değiştirirken veri göçü gerekebilir.

Eski prefab kayıtları meshVersion olmadan okunur; yalnız bu eski kayıtlar korunmuş sol/sag assetlerine ihtiyaç duyar. Eksik/bozuk yeni mesh kaydında kaynak taş alınabilir durumda kalır.

## Sahne ve Inspector

Cube üzerindeki StoneSawMachine için CuttingPlane oluşturuldu ve bağlandı. Yeni kesit materyali Art/Materials/Rocks/Stone_CutSurface.mat olarak oluşturuldu. İki çıkış gerçek kesitleri oyuncuya gösterecek şekilde yönlendirildi. Kopyalanmış taşların kimlikleri ayrıldı; ilk dört taşın tarihsel kimlikleri korundu. Sahne genelinde duplicate TidyObject kimliği ve missing script kalmadı.

Gerekli bağlantılar tamamlandı. Cube → StoneSawMachine üzerinden:

- Cutting Plane: local YZ bıçak yüzeyi, local X normal. Gerçek model eklendiğinde bu düzlemi bıçakla hizala.
- Cut Surface Material: yeni iç yüzey materyali.
- Cut Duration: mevcut 2 saniye.
- Left/Right Output Point: konum ve parça yönleri.
- Mesh Settings: Relative Tolerance varsayılan 0.00001; Minimum Volume Fraction 0.001; Piece Separation 0.04 metre.
- Maximum Source Triangles varsayılan 50.000; Maximum Contour Vertices 2.048.
- Compound Collider Resolution 4; Convex Triangle Limit 180; ayrıca güvenli vertex sınırı uygulanır.
- Ore Radius Fraction, Ore Surface Offset, Ore Euler Offset, Ore Normal Offset: cevher görünümü.

Yeni modellerde Read/Write Enabled gerekir; runtime CPU'nun üçgen/vertex verisini okuyabilmesi içindir. Mevcut taş modeli okunabilir durumdaydı; importer ayarı değiştirilmedi.

## Gerçekten çalıştırılan testler

Unity 6000.6.5f1 ve canlı Pipeline bağlantısı kullanıldı. Yeni paket veya asset indirilmedi; Git işlemi yapılmadı.

- 81 geometri kontrolü geçti: gerçek FBX; dış yüzey normalleri; eksik normal verisi; girintili L kesiti; çoklu child MeshFilter ve ayrı konturlar; düzlemin vertex/kenardan geçmesi; ters scale, rotasyon, offset pivot ve sheared parent; CuttingPlane normalinin dikliği; hacim korunumu; submesh/materyal eşleşmesi. 17 başarılı kesim örneği ölçüldü. Aşırı deforme bir örnekte iç içe kontur oluştuğu için beklenen güvenli ret doğrulandı.
- 34 Play Mode kontrolü geçti: mevcut pickup, elde takip, drop, Interact ile yerleştirme, Input System üzerinden T, kesim kilitleri, iki boş/cevherli gerçek mesh, parça pickup'ı, prefab referansları boşken çalışma, snapshot JSON'dan geometriyi yeniden oluşturma ve kesim öncesi kayda dönüş.
- 17 güvenlik/fizik kontrolü geçti: düzlem eksikliği, kesmeyen düzlem, ilk parça üretildikten sonra ikinci çıkışın kaybı ve rollback, eski kayıt formatı, eksik/bozuk yeni kayıt kurtarması, gerçek dinamik düşüş/zemin çarpışması, convex ve compound fallback, pickup/drop sonrasında kütle oranı.
- Kömür, yakut, zümrüt ve elmas ayrı kesimlerden geçirildi. Doğru cevher mesh'i, hesaplanan yüzey konumu, fizik bileşeni olmayan cevher görseli ve iki çıktının çakışmaması doğrulandı.
- Kamera görüntüsü alındı ve görsel olarak incelendi. Önizlemede mevcut diğer masa taşları yalnız çekim süresince gizlendi, sonra geri açıldı. runtime-mesh-preview.png gerçek yeni iki parçayı gösterir.
- Son derleme 0 hata/0 uyarı; son Play Mode Console kontrolü 0 runtime hata. Hata enjeksiyonu testlerinin beklenen uyarıları ve düzeltilmiş hataların geçmiş kayıtları silinmedi.

Sahne taramasında 14 mevcut ham taşın 13'ü kesilebildi. Stein (5) mevcut prosedürel geometrisi açık/non-manifold kenar verdiği için reddedildi; taş korunuyor. Tüm 100 farklı modelin denenmiş olduğu iddia edilmiyor.

Son ölçümde geometri servisi Editor'deki 17 örnekte ortalama yaklaşık 45 ms, en çok yaklaşık 81 ms sürdü. Bunlar cihaz garantisi veya tam oyun profiler sonucu değildir. Kesim ana thread üzerindedir; büyük meshlerde kısa frame spike olabilir. İş yalnız kesim anında yapılır; her kare mesh kesimi/üçgen başına geçici dizi oluşturma yoktur. Büyük konturların ear clipping maliyeti ve memory/GC için bütçe sınırları bulunur; Jobs/Burst eklenmedi.

## Kayıt testi sırasında yapılan kurtarma

İlk eski-kayıt denemesinde otomatik kayıt henüz açık olduğundan, eski formatın yeni format sanılması üç taşın kaydında tutarsız tüketilme durumu oluşturdu. Bu hata düzeltildi ve kayıt dosyası yedeklendi.

Yalnız stone_warehouse_1, stone_warehouse_2 ve stone_warehouse_3 alınabilir ham duruma döndürüldü. CustomState cevher verileri birebir korundu: taş 2 kömür; taş 1 ve 3 boş. Diğer ilerleme alanları değiştirilmedi. Bu üç taş yeniden kesilebilir; önceki parça pozları geri getirildiği iddia edilmiyor.

SaveRecoveryBackup-20261027.json kurtarma öncesi yedektir; SaveRecovery-result.json değişen kayıtların listesidir. Sonraki testler gerçek kayda yazmayan izole oturumlarda yapıldı. Sonunda otomatik kayıt ayarı önceki true değerine, Run In Background önceki false değerine geri getirildi.

## Mevcut sınırlar / senden gerekenler

- Stein (5) için kapalı, tutarlı winding'e sahip geçerli mesh gerekir. Mevcut hatalı geometriyi rastgele bir modelle değiştirmedim. Prosedürel önizleme Play Mode çıkışında mevcut parametreleriyle yeniden oluşturuldu. Çalışma anında kullanılacak prosedürel taş için geçerli mesh'i asset olarak kaydetmek veya Generate On Play kullanmak gerekebilir.
- İç içe delikli konturlar, açık/non-manifold ya da kendi üzerine katlanan yüzeyler destek sözleşmesi dışındadır. Temel kenar denetimi her tür self-intersection'ı bulmayı garanti etmez; bu tür kaynaklar mesh aracında onarılmalıdır.
- SkinnedMeshRenderer desteklenmez. Aktif statik child MeshFilter'lar ve çoklu submesh desteklenir; tek renderer'a birleştirilir. UV0/normaller/renkler korunur; UV1/UV2/lightmap bake verisi, skin blendshape ve renderer'a özel MaterialPropertyBlock'lar taşınmaz.
- Complex collider yaklaşık geometri kullanır. Hassas fizik için çözünürlüğü kontrollü artır; yüksek değer daha çok collider üretir.
- Tam uygulama yeniden başlatma, build/platform ve mobil profiler testi yapılmadı. Kayıt testleri gerçek snapshot CaptureWorld/RestoreWorld ve JSON roundtrip ile yapıldı; üç kaydın disk onarımı yazılıp yeniden okunarak ayrıca doğrulandı.
- Gerçek bıçak modeli hâlâ yok; mevcut süre/geri bildirim korunur. Zımparalama, satış ve cevher çıkarma eklenmedi.
