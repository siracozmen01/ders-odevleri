// Uygulama 2 — Envanter Filtreleme ve İstatistik
// Run: node js-alistirmalar/uygulama2-envanter.js
// Uses filter, map and reduce with arrow functions.

// Step 1 — define the inventory
const envanter = [
  { ad: "Kılıç", tur: "silah", deger: 150, nadir: true },
  { ad: "Kalkan", tur: "zirh", deger: 90, nadir: false },
  { ad: "İksir", tur: "sarf", deger: 20, nadir: false },
  { ad: "Asa", tur: "silah", deger: 300, nadir: true },
];

// Step 2 — keep only the weapons
console.log("--- Adım 2: silahlar ---");
const silahlar = envanter.filter((e) => e.tur === "silah");
console.log(silahlar.map((e) => e.ad));

// Step 3 — names of the rare items (filter + map chain)
console.log("--- Adım 3: nadir eşyalar ---");
const nadirAdlar = envanter
  .filter((e) => e.nadir)
  .map((e) => e.ad);
console.log(nadirAdlar);

// Step 4 — total inventory value (reduce, starting from 0)
console.log("--- Adım 4: toplam değer ---");
const toplamDeger = envanter.reduce((toplam, e) => toplam + e.deger, 0);
console.log(`Toplam değer: ${toplamDeger}`);

// Optional improvement — print items worth more than 100 as "ad (deger)"
console.log("--- Geliştirme: değeri 100'den büyük eşyalar ---");
envanter
  .filter((e) => e.deger > 100)
  .forEach((e) => console.log(`${e.ad} (${e.deger})`));
