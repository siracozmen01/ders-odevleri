import { StatusBar } from 'expo-status-bar';
import { Image, ScrollView, StyleSheet, Text, View } from 'react-native';

import StatKutusu from './components/StatKutusu';

export default function App() {
  return (
    // In a ScrollView, alignment goes into contentContainerStyle.
    <ScrollView contentContainerStyle={styles.ekran}>
      {/* Top: avatar and name. Local image from assets via require. */}
      <Image source={require('./assets/karakter.png')} style={styles.avatar} />
      <Text style={styles.isim}>Kahraman Kaan</Text>
      <Text style={styles.sinif}>Seviye 7 · Savaşçı</Text>

      {/* Middle: four stat boxes side by side (flexDirection: 'row') */}
      <View style={styles.satir}>
        <StatKutusu deger={120} ad="Can" />
        <StatKutusu deger={45} ad="Güç" />
        <StatKutusu deger={30} ad="Hız" />
        <StatKutusu deger={18} ad="Zeka" />
      </View>

      {/* Bottom: description */}
      <Text style={styles.aciklama}>
        Kaan, kuzey dağlarından gelen cesur bir savaşçıdır.
      </Text>

      <StatusBar style="auto" />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  ekran: { alignItems: 'center', padding: 24, paddingTop: 80 },
  // borderRadius = half of the width -> round image
  avatar: { width: 120, height: 120, borderRadius: 60, marginBottom: 12 },
  isim: { fontSize: 26, fontWeight: 'bold' },
  sinif: { fontSize: 16, color: 'gray', marginBottom: 24 },
  satir: { flexDirection: 'row', justifyContent: 'space-between', width: '100%' },
  aciklama: { marginTop: 24, fontSize: 16, textAlign: 'center' },
});
