import { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Button, Image, ScrollView, StyleSheet, Text, View } from 'react-native';

import IlerlemeCubugu from './components/IlerlemeCubugu';
import StatKarti from './components/StatKarti';

// Step 4a + extra task 2: activities live in an array (two new ones added).
const etkinlikler = [
  { id: 1, metin: 'Boss savaşını kazandın', zaman: '2 saat önce' },
  { id: 2, metin: 'Yeni rozet: Hız Ustası', zaman: 'Dün' },
  { id: 3, metin: '3. seviyeyi bitirdin', zaman: '2 gün önce' },
  { id: 4, metin: 'Efsanevi kılıç buldun', zaman: '3 gün önce' },
  { id: 5, metin: 'Loncaya katıldın: Gece Kurtları', zaman: '1 hafta önce' },
];

const BASLANGIC = { puan: 1250, seviye: 7, yuzde: 65 };
const GOREV_PUANI = 50; // points per completed quest
const GOREV_ILERLEMESI = 15; // level progress (%) per completed quest

// 1250 -> "1.250" (Turkish thousands separator, as in the target screen)
const binlikAyir = (sayi) => String(sayi).replace(/\B(?=(\d{3})+(?!\d))/g, '.');

export default function App() {
  // "Haftaya bağlantı": points and level progress are state now, so the
  // screen re-renders when the button changes them.
  const [puan, setPuan] = useState(BASLANGIC.puan);
  const [seviye, setSeviye] = useState(BASLANGIC.seviye);
  const [yuzde, setYuzde] = useState(BASLANGIC.yuzde);

  function gorevTamamla() {
    setPuan(puan + GOREV_PUANI);
    const yeniYuzde = yuzde + GOREV_ILERLEMESI;
    if (yeniYuzde >= 100) {
      // Level up: carry the overflow into the next level.
      setSeviye(seviye + 1);
      setYuzde(yeniYuzde - 100);
    } else {
      setYuzde(yeniYuzde);
    }
  }

  function sifirla() {
    setPuan(BASLANGIC.puan);
    setSeviye(BASLANGIC.seviye);
    setYuzde(BASLANGIC.yuzde);
  }

  // Extra tasks 1 + 4: card data in an array (own values/colours), rendered with map.
  const kartlar = [
    { baslik: 'Puan', deger: binlikAyir(puan), renk: '#7b4fd6' },
    { baslik: 'Galibiyet', deger: '63', renk: '#22a06b' },
    { baslik: 'Altın', deger: '845', renk: '#e2a400' },
    { baslik: 'Kristal', deger: '27', renk: '#00a3bf' },
  ];

  return (
    <ScrollView style={styles.ekran} contentContainerStyle={styles.icerik}>
      {/* 1. Top bar: avatar and two lines of text side by side */}
      <View style={styles.ustBar}>
        <Image
          source={{ uri: 'https://reactnative.dev/img/tiny_logo.png' }}
          style={styles.avatar}
        />
        <View>
          <Text style={styles.selam}>Merhaba, Kaan!</Text>
          <Text style={styles.altYazi}>Bugün de oynayalım mı?</Text>
        </View>
      </View>

      {/* 2. Stat cards in a 2x2 grid (row + wrap) */}
      <View style={styles.kartlar}>
        {kartlar.map((k) => (
          <StatKarti key={k.baslik} deger={k.deger} baslik={k.baslik} renk={k.renk} />
        ))}
      </View>

      {/* 3. Level progress bar (component) */}
      <IlerlemeCubugu seviye={seviye} yuzde={yuzde} />

      {/* Next-week link: a useState button that changes points and progress */}
      <View style={styles.bolum}>
        <Text style={styles.bolumBaslik}>Günlük Görev</Text>
        <Text style={styles.gorevAciklama}>
          Her görev +{GOREV_PUANI} puan ve %{GOREV_ILERLEMESI} seviye ilerlemesi verir.
        </Text>
        <View style={styles.butonlar}>
          <View style={styles.buton}>
            <Button title="Görev Tamamla" color="#2f6fed" onPress={gorevTamamla} />
          </View>
          <View style={styles.buton}>
            <Button title="Sıfırla" color="#8a94a6" onPress={sifirla} />
          </View>
        </View>
      </View>

      {/* 4. Recent activities rendered with map */}
      <View style={styles.bolum}>
        <Text style={styles.bolumBaslik}>Son Etkinlikler</Text>
        {etkinlikler.map((e) => (
          <View key={e.id} style={styles.etkinlik}>
            <Text style={styles.etkinlikMetin}>• {e.metin}</Text>
            <Text style={styles.zaman}>{e.zaman}</Text>
          </View>
        ))}
      </View>

      <StatusBar style="auto" />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  ekran: { flex: 1, backgroundColor: '#f4f6fb' },
  // Padding sits on the content container so the bottom padding is kept when scrolling.
  icerik: { padding: 20, paddingTop: 60 },

  ustBar: { flexDirection: 'row', alignItems: 'center', marginBottom: 24 },
  avatar: { width: 56, height: 56, borderRadius: 28, marginRight: 12 },
  selam: { fontSize: 22, fontWeight: 'bold' },
  altYazi: { fontSize: 14, color: 'gray' },

  kartlar: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between' },

  bolum: { backgroundColor: 'white', padding: 16, borderRadius: 12, marginBottom: 12 },
  bolumBaslik: { fontSize: 16, fontWeight: 'bold' },
  gorevAciklama: { fontSize: 14, color: 'gray', marginTop: 4, marginBottom: 12 },
  butonlar: { flexDirection: 'row', gap: 8 },
  buton: { flex: 1 },

  etkinlik: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  etkinlikMetin: { flexShrink: 1, marginRight: 8 },
  zaman: { fontSize: 12, color: 'gray' },
});
