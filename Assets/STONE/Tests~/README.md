# Unity üzerinden testleri yeniden çalıştırma

Unity açık ve Pipeline bağlantısı hazır olmalıdır. Yeni paket gerekmez. Dosyalar Assets dışında tutulur.

Edit Mode'da olasılık testleri:

```powershell
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/ProbabilityChecks.cs
```

TidyUp_Warehouse sahnesinde Play Mode'a girdikten sonra:

```powershell
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/PlayChecks.cs --timeout 35
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/OreVisualChecks.cs --timeout 25
```

Play testlerini ayrı bir test oturumunda çalıştır. Testler oyuncuyu ve taşları geçici olarak hareket ettirir, snapshot ile geri döndürür. PlayChecks geçici Input System klavyesi ekleyip kaldırır. Arka planda çalıştırırken Editor auto-tick ve Run In Background etkin olmalı; testten sonra kendi önceki ayarlarına dön. Bir test başarısızsa dış komutun success alanına ek olarak data.result.success ve errorDetails alanlarını kontrol et.

Son testlerin kapsamı ve sınırlamaları Assets/STONE/Documentation~/Rapor.md içindedir.

## Runtime mesh slicing doğrulamaları

```powershell
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/RuntimeSliceGeometryChecks.cs --timeout 35
```

Play Mode içinde, gerçek kullanıcı kaydına yazmayan ayrı bir test oturumunda:

```powershell
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/PlayChecks.cs --timeout 35
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/RuntimeSliceSafetyChecks.cs --timeout 30
unity command script run --caller plugin --skill unity-cli --project-path . --format json -- --file Assets/STONE/Tests~/OreVisualChecks.cs --timeout 25
```

Bu Play testlerine başlamadan level manager üzerindeki Save Progress ayarını yalnız test oturumu için kapat; test bittikten sonra önceki değerini geri getir. Snapshot fonksiyonları test içinde doğrudan çağrılır. Böylece gerçek kullanıcı kayıtları yüklenmez/üzerine yazılmaz. Sonuçlar Documentation~/RuntimeSlice*-result.json dosyalarında, ayrıntılar RuntimeMeshSlicing-Rapor.md içindedir.
