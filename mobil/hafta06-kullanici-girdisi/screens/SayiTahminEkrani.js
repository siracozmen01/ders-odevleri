import { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

const ALT_SINIR = 1;
const UST_SINIR = 100;

// Homework — Sayı Tahmin Oyunu
export default function SayiTahminEkrani() {
  // Secret number kept in state, generated with the line given in the assignment.
  const [gizli, setGizli] = useState(Math.floor(Math.random() * 100) + 1);
  const [tahmin, setTahmin] = useState('');
  const [mesaj, setMesaj] = useState(`${ALT_SINIR} ile ${UST_SINIR} arasında bir sayı tuttum.`);
  const [uyari, setUyari] = useState('');
  const [tahminSayisi, setTahminSayisi] = useState(0);
  const [sonTahmin, setSonTahmin] = useState(null);
  const [bildi, setBildi] = useState(false);

  function tahminEt() {
    const sayi = Number(tahmin);
    setTahmin(''); // clear the box after every guess

    // Invalid input: empty, not a whole number or outside 1–100. Not counted.
    if (tahmin.trim() === '' || !Number.isInteger(sayi) || sayi < ALT_SINIR || sayi > UST_SINIR) {
      setUyari(`Geçersiz giriş: ${ALT_SINIR} ile ${UST_SINIR} arasında bir sayı yaz.`);
      return;
    }

    setUyari('');
    const yeniSayi = tahminSayisi + 1;
    setTahminSayisi(yeniSayi);
    setSonTahmin(sayi);

    if (sayi < gizli) {
      setMesaj('Daha büyük!');
    } else if (sayi > gizli) {
      setMesaj('Daha küçük!');
    } else {
      setMesaj('Bildin! 🎉');
      setBildi(true);
    }
  }

  // Bonus: start a new round with a fresh secret number.
  function yenidenOyna() {
    setGizli(Math.floor(Math.random() * 100) + 1);
    setTahmin('');
    setMesaj(`Yeni bir sayı tuttum (${ALT_SINIR}–${UST_SINIR}).`);
    setUyari('');
    setTahminSayisi(0);
    setSonTahmin(null);
    setBildi(false);
  }

  return (
    <View style={styles.ekran}>
      <Text style={styles.baslik}>Sayı Tahmin Oyunu</Text>

      <View style={[styles.mesajKutu, bildi && styles.mesajKutuBildi]}>
        {sonTahmin !== null && <Text style={styles.sonTahmin}>Tahminin: {sonTahmin}</Text>}
        <Text style={styles.mesaj}>{mesaj}</Text>
      </View>

      <Text style={styles.sayac}>
        {bildi ? `${tahminSayisi}. tahminde bildin!` : `Tahmin sayısı: ${tahminSayisi}`}
      </Text>

      {bildi ? (
        <Pressable
          onPress={yenidenOyna}
          style={({ pressed }) => [styles.buton, styles.yenidenButon, pressed && styles.basili]}
        >
          <Text style={styles.butonYazi}>YENİDEN OYNA</Text>
        </Pressable>
      ) : (
        <View>
          <TextInput
            value={tahmin}
            onChangeText={setTahmin}
            onSubmitEditing={tahminEt}
            placeholder={`${ALT_SINIR}–${UST_SINIR}`}
            placeholderTextColor="#9aa5b1"
            keyboardType="numeric"
            maxLength={3}
            style={styles.kutu}
          />
          {uyari !== '' && <Text style={styles.uyari}>⚠️ {uyari}</Text>}
          <Pressable
            onPress={tahminEt}
            style={({ pressed }) => [styles.buton, pressed && styles.basili]}
          >
            <Text style={styles.butonYazi}>Tahmin Et</Text>
          </Pressable>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  ekran: { flex: 1, padding: 24, paddingTop: 60, backgroundColor: '#f4f6fb' },
  baslik: { fontSize: 28, fontWeight: 'bold', marginBottom: 24 },
  mesajKutu: {
    backgroundColor: '#1c2b45',
    borderRadius: 16,
    padding: 24,
    alignItems: 'center',
    minHeight: 120,
    justifyContent: 'center',
  },
  mesajKutuBildi: { backgroundColor: '#22a06b' },
  sonTahmin: { color: 'rgba(255, 255, 255, 0.75)', fontSize: 16, marginBottom: 6 },
  mesaj: { color: 'white', fontSize: 24, fontWeight: 'bold', textAlign: 'center' },
  sayac: { fontSize: 16, color: 'gray', textAlign: 'center', marginVertical: 16 },
  kutu: {
    backgroundColor: 'white',
    borderWidth: 1,
    borderColor: '#ccd3e0',
    borderRadius: 10,
    padding: 14,
    fontSize: 22,
    textAlign: 'center',
  },
  uyari: { color: '#c9372c', fontWeight: 'bold', marginTop: 10 },
  buton: { backgroundColor: '#2f6fed', padding: 16, borderRadius: 12, marginTop: 16 },
  yenidenButon: { backgroundColor: '#22a06b' },
  basili: { opacity: 0.7, transform: [{ scale: 0.97 }] },
  butonYazi: { color: 'white', fontSize: 18, fontWeight: 'bold', textAlign: 'center' },
});
