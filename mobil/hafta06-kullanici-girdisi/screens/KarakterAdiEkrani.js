import { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';

const MAKS_UZUNLUK = 12;

// Uygulama 1 — Karakter Adı: Canlı Önizleme
export default function KarakterAdiEkrani() {
  const [isim, setIsim] = useState('');

  return (
    <View style={styles.ekran}>
      <Text style={styles.etiket}>Karakterinin adı</Text>
      <TextInput
        value={isim}
        onChangeText={setIsim}
        placeholder="Örn. Gölge Avcısı"
        maxLength={MAKS_UZUNLUK}
        style={styles.kutu}
      />

      {/* Step 2–3: character counter, turns orange from 10 characters on */}
      <Text style={[styles.sayac, isim.length >= 10 && { color: 'orange', fontWeight: 'bold' }]}>
        {isim.length} / {MAKS_UZUNLUK}
      </Text>

      {/* Kendin dene: a "Temizle" Pressable that empties the box */}
      <Pressable
        onPress={() => setIsim('')}
        style={({ pressed }) => [styles.temizle, pressed && { opacity: 0.6 }]}
      >
        <Text style={styles.temizleYazi}>Temizle</Text>
      </Pressable>

      {/* Step 4: live character card */}
      <View style={styles.kart}>
        <Text style={styles.kartIkon}>🛡️</Text>
        <Text style={styles.kartIsim}>
          {/* Kendin dene: show the name in upper case (Turkish locale: i -> İ) */}
          {isim.length > 0 ? isim.toLocaleUpperCase('tr-TR') : 'Adsız Kahraman'}
        </Text>
        <Text style={styles.kartAlt}>Seviye 1 · Çaylak</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  ekran: { flex: 1, padding: 24, paddingTop: 60, backgroundColor: '#f4f6fb' },
  etiket: { fontSize: 16, fontWeight: 'bold', marginBottom: 8 },
  kutu: {
    backgroundColor: 'white',
    borderWidth: 1,
    borderColor: '#ccd3e0',
    borderRadius: 10,
    padding: 14,
    fontSize: 18,
  },
  sayac: { textAlign: 'right', marginTop: 6, color: 'gray' },
  temizle: {
    alignSelf: 'flex-start',
    marginTop: 8,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
    backgroundColor: '#e3e7f0',
  },
  temizleYazi: { fontWeight: 'bold', color: '#333' },
  kart: { marginTop: 32, backgroundColor: '#1c2b45', borderRadius: 16, padding: 24, alignItems: 'center' },
  kartIkon: { fontSize: 48 },
  kartIsim: { fontSize: 26, fontWeight: 'bold', color: 'white', marginTop: 8 },
  kartAlt: { fontSize: 14, color: '#9fb6d4', marginTop: 4 },
});
