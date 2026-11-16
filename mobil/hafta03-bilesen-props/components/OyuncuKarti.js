import { StyleSheet, Text, View } from 'react-native';

// Player card from the in-class exercise, extended with the homework's
// third prop: `seviye` (level).
export default function OyuncuKarti({ isim, puan, seviye }) {
  return (
    <View style={styles.kart}>
      <Text style={styles.isim}>{isim}</Text>
      <Text>Puan: {puan}</Text>
      <Text>Seviye: {seviye}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  kart: { backgroundColor: '#eef3ff', padding: 16, borderRadius: 10, marginBottom: 10 },
  isim: { fontSize: 18, fontWeight: 'bold' },
});
