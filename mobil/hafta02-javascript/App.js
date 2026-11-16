import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View } from 'react-native';

// Uygulama 4 — the same player data as in Uygulama 1, now rendered on screen.
const oyuncular = [
  { isim: 'Ayşe', puan: 40 },
  { isim: 'Zeynep', puan: 70 },
  { isim: 'Mehmet', puan: 55 },
];

export default function App() {
  return (
    <View style={styles.container}>
      {/* Uygulama 3 — the text change used to see fast refresh in action */}
      <Text style={{ fontSize: 24 }}>Merhaba DOT311</Text>

      {/* Uygulama 4 — one <Text> per player via map; key must be unique */}
      <Text style={styles.baslik}>Skor Tablosu</Text>
      {oyuncular.map((o) => (
        <Text key={o.isim} style={styles.satir}>
          {`${o.isim}: ${o.puan} puan`}
        </Text>
      ))}

      {/* Uygulama 4, optional improvement — filter before map (score > 50) */}
      <Text style={styles.altBaslik}>50 puan üstü</Text>
      {oyuncular
        .filter((o) => o.puan > 50)
        .map((o) => (
          <Text key={o.isim} style={styles.satir}>
            {`${o.isim}: ${o.puan}`}
          </Text>
        ))}

      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#fff', padding: 48, justifyContent: 'center' },
  baslik: { fontSize: 28, fontWeight: 'bold', marginTop: 24, marginBottom: 16 },
  altBaslik: { fontSize: 22, fontWeight: 'bold', marginTop: 16, marginBottom: 8, color: '#2f6fed' },
  satir: { fontSize: 20, marginBottom: 8 },
});
