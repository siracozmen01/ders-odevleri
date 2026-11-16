import { StyleSheet, Text, View } from 'react-native';

// Extra task 3: the level progress bar as a component,
// e.g. <IlerlemeCubugu seviye={7} yuzde={65} />.
// It is two nested boxes; the inner box width is the percentage.
export default function IlerlemeCubugu({ seviye, yuzde }) {
  return (
    <View style={styles.bolum}>
      <View style={styles.seviyeSatir}>
        <Text style={styles.bolumBaslik}>Seviye {seviye}</Text>
        <Text style={styles.yuzde}>%{yuzde}</Text>
      </View>
      <View style={styles.cubukDis}>
        <View style={[styles.cubukIc, { width: `${yuzde}%` }]} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  bolum: { backgroundColor: 'white', padding: 16, borderRadius: 12, marginBottom: 12 },
  seviyeSatir: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  bolumBaslik: { fontSize: 16, fontWeight: 'bold' },
  yuzde: { fontSize: 16, color: '#2f6fed', fontWeight: 'bold' },
  cubukDis: { height: 12, backgroundColor: '#e3e7f0', borderRadius: 6 },
  cubukIc: { height: 12, backgroundColor: '#2f6fed', borderRadius: 6 },
});
