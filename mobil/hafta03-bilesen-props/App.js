import { StatusBar } from 'expo-status-bar';
import { ScrollView, StyleSheet, Text } from 'react-native';

import EsyaKarti from './components/EsyaKarti';
import OyuncuKarti from './components/OyuncuKarti';

export default function App() {
  return (
    // ScrollView instead of the note's View: with the homework cards added,
    // the content is taller than a small phone screen.
    <ScrollView contentContainerStyle={styles.ekran}>
      <Text style={styles.baslik}>Skor Tablosu</Text>
      {/* Text props use quotes, number props use braces */}
      <OyuncuKarti isim="Ayşe" puan={40} seviye={3} />
      <OyuncuKarti isim="Veli" puan={15} seviye={1} />
      <OyuncuKarti isim="Zeynep" puan={70} seviye={5} />

      <Text style={styles.baslik}>Envanter</Text>
      <EsyaKarti ad="Kılıç" deger={150} />
      <EsyaKarti ad="Kalkan" deger={90} />
      <EsyaKarti ad="İksir" deger={20} />
      <EsyaKarti ad="Asa" deger={300} />

      <StatusBar style="auto" />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  ekran: { padding: 24, paddingTop: 60 },
  baslik: { fontSize: 26, fontWeight: 'bold', marginBottom: 16, marginTop: 8 },
});
