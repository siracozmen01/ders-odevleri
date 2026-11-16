import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, TextInput } from 'react-native';

// Sections 1–4 of the note: controlled TextInput, useful TextInput props,
// events (onSubmitEditing) and a custom Pressable with a pressed style.
export default function TemellerEkrani() {
  // 1. Controlled input
  const [isim, setIsim] = useState('');
  // 2. Login boxes + numeric keyboard
  const [kullaniciAdi, setKullaniciAdi] = useState('');
  const [sifre, setSifre] = useState('');
  const [yas, setYas] = useState('');
  // 3. Submit with Enter
  const [mesaj, setMesaj] = useState('');
  const [gonderilen, setGonderilen] = useState('');
  // 4. Pressable
  const [basmaSayisi, setBasmaSayisi] = useState(0);

  return (
    <ScrollView contentContainerStyle={styles.ekran}>
      <Text style={styles.baslik}>Temeller</Text>

      <Text style={styles.bolum}>1. TextInput</Text>
      <TextInput value={isim} onChangeText={setIsim} placeholder="Adını yaz" style={styles.kutu} />
      <Text style={styles.selam}>Merhaba, {isim}!</Text>

      <Text style={styles.bolum}>2. Kullanışlı özellikler</Text>
      <TextInput
        value={kullaniciAdi}
        onChangeText={setKullaniciAdi}
        placeholder="Kullanıcı adı"
        autoCapitalize="none"
        maxLength={16}
        style={styles.kutu}
      />
      <Text style={styles.not}>{kullaniciAdi.length} / 16</Text>
      <TextInput
        value={sifre}
        onChangeText={setSifre}
        placeholder="Şifre"
        secureTextEntry
        style={styles.kutu}
      />
      <TextInput
        value={yas}
        onChangeText={setYas}
        placeholder="Yaş (sadece rakam)"
        keyboardType="numeric"
        maxLength={3}
        style={[styles.kutu, styles.bosluk]}
      />
      {/* TextInput always gives a string: "25" + 1 is "251" */}
      {yas !== '' && (
        <Text style={styles.not}>
          {`yas + 1 → "${yas + 1}"   ·   Number(yas) + 1 → ${Number(yas) + 1}`}
          {isNaN(Number(yas)) ? '  (NaN: sayı değil)' : ''}
        </Text>
      )}

      <Text style={styles.bolum}>3. Olaylar: onSubmitEditing</Text>
      <TextInput
        value={mesaj}
        onChangeText={setMesaj}
        onSubmitEditing={() => {
          console.log('Gönderildi:', mesaj);
          setGonderilen(mesaj);
          setMesaj(''); // controlled input: clear the box from code
        }}
        placeholder="Mesaj yaz ve Enter'a bas"
        style={styles.kutu}
      />
      {gonderilen !== '' && <Text style={styles.not}>Gönderildi: {gonderilen}</Text>}

      <Text style={styles.bolum}>4. Pressable</Text>
      <Pressable
        onPress={() => {
          console.log('Başla!');
          setBasmaSayisi(basmaSayisi + 1);
        }}
        style={({ pressed }) => [styles.buton, pressed && { opacity: 0.6, transform: [{ scale: 0.95 }] }]}
      >
        <Text style={styles.butonYazi}>OYUNA BAŞLA</Text>
      </Pressable>
      <Text style={styles.not}>Basma sayısı: {basmaSayisi}</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  ekran: { padding: 24, paddingTop: 60, backgroundColor: '#f4f6fb', flexGrow: 1 },
  baslik: { fontSize: 28, fontWeight: 'bold', marginBottom: 8 },
  bolum: { fontSize: 16, fontWeight: 'bold', marginTop: 20, marginBottom: 8 },
  kutu: {
    backgroundColor: 'white',
    borderWidth: 1,
    borderColor: '#999',
    padding: 12,
    borderRadius: 8,
    marginBottom: 6,
  },
  bosluk: { marginTop: 6 },
  selam: { fontSize: 22, marginTop: 10 },
  not: { color: 'gray', marginBottom: 6 },
  buton: { backgroundColor: '#2f6fed', paddingVertical: 14, paddingHorizontal: 32, borderRadius: 12, marginBottom: 8 },
  butonYazi: { color: 'white', fontSize: 18, fontWeight: 'bold', textAlign: 'center' },
});
