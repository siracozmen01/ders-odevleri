import { Pressable, StyleSheet, Text } from 'react-native';

// Reusable game button (Uygulama 2). What happens on press is decided by the
// parent through the onPress / onLongPress props; `pasif` disables the button.
export default function OyunButonu({ baslik, renk, onPress, onLongPress, pasif }) {
  return (
    <Pressable
      onPress={onPress}
      onLongPress={onLongPress}
      disabled={pasif}
      // A style function receives { pressed } so the button reacts while held.
      style={({ pressed }) => [
        styles.buton,
        { backgroundColor: renk },
        pressed && styles.basili,
        pasif && styles.pasif,
      ]}
    >
      <Text style={styles.butonYazi}>{baslik}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  buton: { paddingVertical: 16, borderRadius: 12, marginBottom: 12 },
  basili: { opacity: 0.7, transform: [{ scale: 0.97 }] },
  pasif: { opacity: 0.35 },
  butonYazi: { color: 'white', fontSize: 18, fontWeight: 'bold', textAlign: 'center' },
});
