// End-of-week exercise (Küçük alıştırma)
// Run: node js-alistirmalar/alistirma.js

const oyuncular = [
  { isim: "Ayşe", puan: 40 },
  { isim: "Veli", puan: 15 },
  { isim: "Zeynep", puan: 70 },
];

// 1) An array of names only (map)
const isimler = oyuncular.map((o) => o.isim);
console.log("1) İsimler:", isimler);

// 2) Players with more than 30 points (filter)
const otuzUstu = oyuncular.filter((o) => o.puan > 30);
console.log("2) Puanı 30'dan büyük olanlar:", otuzUstu);

// 3) One line per player (forEach + template literal);
//    destructuring reads the fields straight from each object.
console.log("3) Oyuncu satırları:");
oyuncular.forEach(({ isim, puan }) => console.log(`${isim}: ${puan} puan`));
