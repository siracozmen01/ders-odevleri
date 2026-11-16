import { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import KarakterAdiEkrani from './screens/KarakterAdiEkrani';
import KarakterOlusturEkrani from './screens/KarakterOlusturEkrani';
import SavasEkrani from './screens/SavasEkrani';
import SayiTahminEkrani from './screens/SayiTahminEkrani';
import TemellerEkrani from './screens/TemellerEkrani';

// One tab per note section. A simple state-based switcher is enough here;
// real navigation (Expo Router / React Navigation) comes in week 9.
const sekmeler = [
  { id: 'temeller', simge: '📝', baslik: 'Temeller', Ekran: TemellerEkrani },
  { id: 'ad', simge: '🛡️', baslik: 'Ad', Ekran: KarakterAdiEkrani },
  { id: 'savas', simge: '⚔️', baslik: 'Savaş', Ekran: SavasEkrani },
  { id: 'form', simge: '🧙', baslik: 'Form', Ekran: KarakterOlusturEkrani },
  { id: 'tahmin', simge: '🎯', baslik: 'Tahmin', Ekran: SayiTahminEkrani },
];

export default function App() {
  const [aktif, setAktif] = useState('temeller');
  const { Ekran } = sekmeler.find((s) => s.id === aktif);

  return (
    <View style={styles.uygulama}>
      <View style={styles.icerik}>
        <Ekran />
      </View>

      <View style={styles.sekmeCubugu}>
        {sekmeler.map((s) => (
          <Pressable
            key={s.id}
            onPress={() => setAktif(s.id)}
            style={[styles.sekme, aktif === s.id && styles.aktifSekme]}
          >
            <Text style={styles.sekmeSimge}>{s.simge}</Text>
            <Text style={[styles.sekmeYazi, aktif === s.id && styles.aktifSekmeYazi]}>
              {s.baslik}
            </Text>
          </Pressable>
        ))}
      </View>

      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  uygulama: { flex: 1, backgroundColor: '#f4f6fb' },
  icerik: { flex: 1 },
  sekmeCubugu: {
    flexDirection: 'row',
    backgroundColor: 'white',
    borderTopWidth: 1,
    borderTopColor: '#e3e7f0',
    paddingBottom: 16,
    paddingTop: 6,
  },
  sekme: { flex: 1, alignItems: 'center', paddingVertical: 6, borderRadius: 10, marginHorizontal: 2 },
  aktifSekme: { backgroundColor: '#eef3ff' },
  sekmeSimge: { fontSize: 20 },
  sekmeYazi: { fontSize: 12, color: 'gray', marginTop: 2 },
  aktifSekmeYazi: { color: '#2f6fed', fontWeight: 'bold' },
});
