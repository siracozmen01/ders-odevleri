// Uygulama 1 — Oyuncu Skor Listesi
// Run: node js-alistirmalar/uygulama1-skor-listesi.js
// Uses const/let, arrays, map, forEach and template literals.

// Step 1 — define the data
const oyuncular = [
  { isim: "Ayşe", puan: 40 },
  { isim: "Veli", puan: 15 },
  { isim: "Zeynep", puan: 70 },
  { isim: "Mehmet", puan: 55 },
];

// Step 2 — build one line of text per player (map + template literal)
console.log("--- Adım 2: map ---");
const satirlar = oyuncular.map((o) => `${o.isim}: ${o.puan} puan`);
console.log(satirlar);

// Step 3 — print every player one by one (forEach)
console.log("--- Adım 3: forEach ---");
oyuncular.forEach((o) => console.log(`${o.isim} -> ${o.puan}`));

// Step 4 — find the highest score
console.log("--- Adım 4: en yüksek puan ---");
let enYuksek = oyuncular[0];
oyuncular.forEach((o) => {
  if (o.puan > enYuksek.puan) {
    enYuksek = o;
  }
});
console.log(`Lider: ${enYuksek.isim} (${enYuksek.puan})`);

// Optional improvement — sort by score (descending) and print the top 3.
// The spread copy keeps the original array untouched (sort mutates in place).
console.log("--- Geliştirme: ilk 3 ---");
const sirali = [...oyuncular].sort((a, b) => b.puan - a.puan);
sirali.slice(0, 3).forEach((o, i) => console.log(`${i + 1}. ${o.isim}`));
