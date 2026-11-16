import { StyleSheet, Text, View } from 'react-native';

// Homework component: an inventory item card showing the item name (`ad`)
// and its value (`deger`).
export default function EsyaKarti({ ad, deger }) {
  return (
    <View style={styles.kart}>
      <Text style={styles.ad}>{ad}</Text>
      <Text style={styles.deger}>{deger} altın</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  kart: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: '#fff6e0',
    borderLeftWidth: 4,
    borderLeftColor: '#e2a400',
    padding: 14,
    borderRadius: 10,
    marginBottom: 10,
  },
  ad: { fontSize: 17, fontWeight: 'bold' },
  deger: { fontSize: 16, color: '#8a6400' },
});
