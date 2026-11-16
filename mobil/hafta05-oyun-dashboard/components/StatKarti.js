import { StyleSheet, Text, View } from 'react-native';

// Stat card of the dashboard. The array style merges the fixed card style
// with the per-card left border colour that comes from the `renk` prop.
export default function StatKarti({ deger, baslik, renk }) {
  return (
    <View style={[styles.kart, { borderLeftColor: renk }]}>
      <Text style={styles.kartDeger}>{deger}</Text>
      <Text style={styles.kartBaslik}>{baslik}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  // width 48% -> two cards fit on one row, the parent wraps them into a 2x2 grid
  kart: {
    width: '48%',
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 12,
    borderLeftWidth: 5,
    marginBottom: 12,
  },
  kartDeger: { fontSize: 24, fontWeight: 'bold' },
  kartBaslik: { fontSize: 14, color: 'gray', marginTop: 4 },
});
