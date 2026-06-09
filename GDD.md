# GDD: Remy - The Light Painter (Definitive Edition)

## 1. Konsept ve Vizyon (The Core Vision)

### 1.1 Genel Bakış
**Remy**, oyuncunun dünyayı sadece görmesini değil, onu bizzat inşa etmesini sağlayan "Light Painting" mekaniği üzerine kurulu, 3. şahıs bakış açısına sahip bir atmosferik macera oyunudur.

### 1.2 Tasarım Mottosu
*"Gördüğün şey gerçektir, çizdiğin şey ise kaderin."*

---

## 2. Derinlemesine Hikaye ve Evren (Lore & Narrative)

### 2.1 Aethelgard'ın Tarihi: Büyük Sönüş (The Sun-Fall)
Bin yıl önce, Aethelgard dünyası "Güneş-Kökü" (Sun-Root) adı verilen devasa bir ağacın ışığıyla besleniyordu. Ancak "Boşluk" (The Void) adı verilen kadim bir karanlık, ağacın özünü zehirledi. Güneş-Kökü'nün sönmesiyle dünya saniyeler içinde zifiri karanlığa gömüldü. Şehirler yıkıldı, insanlar gölgeye dönüştü. Bugün Aethelgard, sadece "Kıvılcım Taşıyıcılar"ın (Spark-Bearers) hatıralarıyla ayakta kalan bir harabedir.

### 2.2 Kahramanın Hikayesi: Remy ve Ebedi Meşale
Remy, son "Kıvılcım Taşıyıcı" ailesinin hayatta kalan tek üyesidir. Elindeki meşale, sıradan bir ateş değil; Güneş-Kökü'nün son canlı hücresini içeren "Lumina'nın Kalbi"dir. 
*   **Motivasyon:** Remy, babasının karanlıkta kaybolmadan önce ona fısıldadığı son sözü gerçekleştirmek için yoldadır: *"Işığı köklere geri götür."*
*   **İç Çatışma:** Remy karanlıktan korkar. Ancak ışığı kullandıkça meşalenin enerjisi azalır. Bu, oyunun mekaniğine "kaynak yönetimi" olarak yansırken, hikayeye ise "fedakarlık" teması olarak işlenir.

### 2.3 Yan Karakterler ve "Yankılar" (The Echoes)
Oyun boyunca oyuncu doğrudan konuşan karakterlerle karşılaşmaz. Bunun yerine:
*   **Işık Hatıraları (Echos):** Oyuncu belirli bölgeleri aydınlattığında, geçmişte orada yaşamış insanların ışık formundaki silüetlerini görür. Bu silüetler, bölgenin trajedisini dilsiz bir tiyatro gibi sergiler.
*   **Rehber Ruh (The Ember):** Meşaleden bazen ayrılan küçük bir kıvılcım (Firefly mekaniği ile senkronize), Remy'ye gizli yolları gösterir. Bu, aslında Remy'nin annesinin ruhunun bir parçasıdır.

### 2.4 Bölüm Bölüm Hikaye Akışı (Three-Act Structure)

*   **1. Perde: Uyanış (The Echoing Grove):** Remy, yıkılmış evinden ayrılır. Meşalenin gücünü ve dünyayı nasıl "çizerek" var edebileceğini öğrenir. İlk antik feneri yakarak ormanın bir kısmını huzura kavuşturur.
*   **2. Perde: Derinliklerdeki Yüzleşme (The Crystalline Caves & Marshes):** Remy, karanlığın sadece bir yokluk olmadığını, aktif bir düşman olduğunu anlar. "Gölge Yiyiciler" ile karşılaşır. Babasının karanlığa teslim olmadığını, aslında Güneş-Kökü'nü korumak için kendini bir mühre dönüştürdüğünü keşfeder.
*   **3. Perde: Kalbe Yolculuk (The Core):** Remy, dünyanın merkezine ulaşır. Burada "Boşluk'un Muhafızı" (The Void Sentinel) ile yüzleşir. Finalde oyuncu meşaledeki son ışığı köklere vermek (dünyayı aydınlatmak ama kendini karanlıkta bırakmak) veya ışığı kendine saklamak arasında ahlaki bir seçim yapar.

---

## 3. Oyun Döngüsü ve Mekanikler (Granular Mechanics)

### 3.1 Ontolojik Görsellik (Ontological Visibility)
Karanlıkta hiçbir nesne fiziksel olarak aktif değildir. Oyuncu bir bölgeyi aydınlattığında, o nesneler sadece görünür hale gelmez, aynı zamanda çarpışma (collision) özellikleri kazanır.

### 3.2 Işık Hattı ve Şekil Tanıma (Light Painting & Shapes)
`ShapeRecognizer` modülü üzerinden tanımlanan semboller, hikaye ile bağlantılıdır:
*   **Daire (Mühür):** Remy'nin ailesinin koruma büyüsüdür.
*   **Üçgen (Odak):** Antik gözlemcilerin kullandığı uzak görüş tekniğidir.

---

## 4. Düşmanlar ve Tehditler (The Bestiary)

### 4.1 Shadow Stalkers (Gölge Takipçileri)
Aslında karanlığa yenik düşmüş eski insanlardır. Işığa özlem duyarlar ama temas ettiklerinde acı çekerler. Oyuncuyu takip etmelerinin sebebi, ışığın sıcaklığına duydukları açlıktır.

### 4.2 The Void Sentinel (Boşluk Muhafızı)
Final boss'u. Işığı tamamen emebilen, şekilsiz bir dev. Onu yenmek için oyuncunun tüm "Light Painting" yeteneklerini kullanarak devasa bir ışık kafesi çizmesi gerekir.

---

## 5. Çevresel Anlatı (Environmental Storytelling)

*   **Duvar Resimleri:** Mağaralarda sadece ışık hattı üzerinden geçerken parlayan rünler, Aethelgard'ın yaratılış mitini anlatır.
*   **Kalıntılar:** Yollar üzerinde bulunan kırık oyuncaklar, kurumuş ekmekler veya terkedilmiş asalar; insanların karanlık çöktüğünde ne kadar hazırlıksız yakalandığını gösterir.

---

## 6. Teknik Mimari ve Optimizasyon

### 6.1 RoadMeshBuilder & NavMesh
Dinamik mesh oluşturma sistemi, hikayedeki "yol inşa etme" metaforunu teknik olarak destekler. Her çizilen hat, birer `Navigation Static` nesne gibi davranır.

### 6.2 Shader Graph & VFX
*   **Memory Shader:** Geçmişin yankılarını gösteren özel bir shader efekti. Nesneler yarı saydam ve parıltılı görünür.
*   **Atmospheric Fog:** Karanlığın ağırlığını hissettiren, `Depth-based` sis efektleri.

---

## 7. Ses ve Müzik (Procedural Audio)

*   **Fısıltılar:** Karanlık bölgelerde duyulan fısıltılar, Remy'nin zihnindeki şüpheleri temsil eder.
*   **Umut Teması:** Işık hattı uzadıkça müziğe keman ve flüt gibi enstrümanlar eklenir.

---

## 8. Kullanıcı Deneyimi (UX)

### 8.1 Diegetic HUD
Oyuncunun canı ve enerjisi tamamen görseldir. Meşale sönmeye başladığında ekran kararır, Remy'nin nefes alışverişi hızlanır ve adımları ağırlaşır.
