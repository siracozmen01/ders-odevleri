import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import OyunButonu from '../components/OyunButonu';

const MAKS_MANA = 100;
const BUYU_MALIYETI = 30;
const DUSMAN_MAKS_CAN = 100;
const SALDIRI_HASARI = 10;
const BUYU_HASARI = 30;

// Uygulama 2 — OyunButonu Bileşeni ve Savaş Ekranı
export default function SavasEkrani() {
  const [mesaj, setMesaj] = useState('Bir hamle seç');
  const [mana, setMana] = useState(50);
  // Kendin dene: enemy health
  const [dusmanCan, setDusmanCan] = useState(DUSMAN_MAKS_CAN);

  const hasarVer = (hasar) => setDusmanCan(Math.max(0, dusmanCan - hasar));

  function yenidenBasla() {
    setMesaj('Bir hamle seç');
    setMana(50);
    setDusmanCan(DUSMAN_MAKS_CAN);
  }

  return (
    <View style={styles.ekran}>
      {/* Enemy health bar (like week 5's health bar exercise) */}
      <Text style={styles.dusman}>👹 Düşman Canı: {dusmanCan} / {DUSMAN_MAKS_CAN}</Text>
      <View style={styles.cubukDis}>
        <View style={[styles.cubukIc, { width: `${(dusmanCan / DUSMAN_MAKS_CAN) * 100}%` }]} />
      </View>

      <Text style={styles.mesaj}>{mesaj}</Text>
      <Text style={styles.mana}>Mana: {mana} / {MAKS_MANA}</Text>

      {/* Kendin dene: when the enemy is defeated, show "Zafer!" instead of the buttons */}
      {dusmanCan <= 0 ? (
        <View>
          <Text style={styles.zafer}>🏆 Zafer!</Text>
          <OyunButonu baslik="🔁 Yeniden Savaş" renk="#22a06b" onPress={yenidenBasla} />
        </View>
      ) : (
        <View>
          <OyunButonu
            baslik="⚔️ Saldır"
            renk="#c9372c"
            onPress={() => {
              hasarVer(SALDIRI_HASARI);
              setMesaj(`Düşmana saldırdın! (-${SALDIRI_HASARI})`);
            }}
          />
          <OyunButonu
            baslik="🛡️ Savun"
            renk="#2f6fed"
            onPress={() => setMesaj('Savunmaya geçtin.')}
            onLongPress={() => setMesaj('🛡️ Kalkanı kaldırdın! (uzun basma)')}
          />
          <OyunButonu
            baslik={`🔮 Büyü (${BUYU_MALIYETI} mana)`}
            renk="#7b4fd6"
            // Disabled while there is not enough mana, so mana never goes negative.
            pasif={mana < BUYU_MALIYETI}
            onPress={() => {
              setMana(mana - BUYU_MALIYETI);
              hasarVer(BUYU_HASARI);
              setMesaj(`Ateş topu fırlattın! (-${BUYU_HASARI})`);
            }}
          />
          <OyunButonu
            baslik="💤 Dinlen (+20 mana)"
            renk="#22a06b"
            onPress={() => {
              setMana(mana + 20 > MAKS_MANA ? MAKS_MANA : mana + 20);
              setMesaj('Biraz dinlendin.');
            }}
          />
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  ekran: { flex: 1, justifyContent: 'center', padding: 24, backgroundColor: '#14213d' },
  dusman: { color: 'white', fontSize: 16, fontWeight: 'bold', marginBottom: 8 },
  cubukDis: { height: 12, backgroundColor: '#2c3a5c', borderRadius: 6, marginBottom: 32 },
  cubukIc: { height: 12, backgroundColor: '#c9372c', borderRadius: 6 },
  mesaj: { color: 'white', fontSize: 22, textAlign: 'center', marginBottom: 32 },
  mana: { color: '#9fb6d4', fontSize: 16, textAlign: 'center', marginBottom: 16 },
  zafer: { color: '#e2a400', fontSize: 40, fontWeight: 'bold', textAlign: 'center', marginBottom: 24 },
});
