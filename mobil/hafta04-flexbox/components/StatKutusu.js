import { StyleSheet, Text, View } from 'react-native';

// One stat box of the profile screen, e.g. <StatKutusu deger={120} ad="Can" />.
// flex: 1 makes every box in the row share the width equally.
export default function StatKutusu({ deger, ad }) {
  return (
    <View style={styles.kutu}>
      <Text style={styles.deger}>{deger}</Text>
      <Text>{ad}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  kutu: {
    flex: 1,
    alignItems: 'center',
    backgroundColor: '#eef3ff',
    padding: 16,
    marginHorizontal: 4,
    borderRadius: 10,
  },
  deger: { fontSize: 22, fontWeight: 'bold' },
});
