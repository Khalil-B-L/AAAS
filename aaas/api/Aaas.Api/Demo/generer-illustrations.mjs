// Illustrations des 8 animaux de démonstration (entrée 7 « Photos de démonstration »).
// Silhouettes dessinées ici, sans aucune image tierce ni dépendance : PNG RGB 8 bits encodés avec zlib (Node intégré).
// Fonds pris dans la palette de DESIGN.md (ux-aaas-adoption). Sortie : ./Photos/<nom>.png (400 × 300), déterministe.
//
// Régénérer (depuis la racine du dépôt) :
//   node aaas/api/Aaas.Api/Demo/generer-illustrations.mjs

import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { deflateSync } from 'node:zlib';

const LARGEUR = 400;
const HAUTEUR = 300;
const SOL = 268; // ordonnée du sol (pattes)
const SUR_ECHANTILLONNAGE = 4; // 4 × 4 échantillons par pixel : bords lissés

// Palette DESIGN.md.
const palette = {
  primary: '#0F5A2A',
  text: '#0B0F0C',
  accentText: '#B45100',
  surfaceSoft: '#F1F3F2',
  successSoft: '#E8F4EC',
  warningSoft: '#FFF1E5',
  dangerSoft: '#FCEBEA',
  // Teintes sur blanc (jamais photo-placeholder-start/end, réservées à « photo absente ou en chargement »).
  accentTint: '#FFD7B3', // accent #FF7A00 à 30 %
  primaryTint: '#C3D6CA', // primary #0F5A2A à 25 %
  logoGreen: '#3FA535',
  logoOrange: '#F08A1C',
};

// Un animal par modèle de DemoAnimaux.cs (nom → fichier en minuscules).
const animaux = [
  { nom: 'Caramel', espece: 'chat', haut: 'accentTint', bas: 'warningSoft', silhouette: 'primary', echelle: 1.0, miroir: false, variante: 0 },
  { nom: 'Rex', espece: 'chien', haut: 'primaryTint', bas: 'successSoft', silhouette: 'text', echelle: 1.0, miroir: false, variante: 0 },
  { nom: 'Mimi', espece: 'chat', haut: 'successSoft', bas: 'primaryTint', silhouette: 'text', echelle: 0.8, miroir: true, variante: 1 },
  { nom: 'Filou', espece: 'chien', haut: 'warningSoft', bas: 'dangerSoft', silhouette: 'primary', echelle: 0.9, miroir: true, variante: 1 },
  { nom: 'Pacha', espece: 'chat', haut: 'surfaceSoft', bas: 'warningSoft', silhouette: 'accentText', echelle: 1.05, miroir: false, variante: 2 },
  { nom: 'Bella', espece: 'chien', haut: 'dangerSoft', bas: 'warningSoft', silhouette: 'text', echelle: 0.78, miroir: false, variante: 2 },
  { nom: 'Simba', espece: 'chat', haut: 'logoOrange', bas: 'accentTint', silhouette: 'text', echelle: 0.95, miroir: true, variante: 3 },
  { nom: 'Kira', espece: 'chien', haut: 'logoGreen', bas: 'primaryTint', silhouette: 'text', echelle: 1.0, miroir: true, variante: 3 },
];

// --- Formes (repère local : origine au sol, x vers la droite, y vers le haut) -----------------------------------

const cercle = (cx, cy, r) => (x, y) => (x - cx) ** 2 + (y - cy) ** 2 <= r * r;

const ellipse = (cx, cy, rx, ry, angle = 0) => {
  const c = Math.cos(angle);
  const s = Math.sin(angle);
  return (x, y) => {
    const dx = x - cx;
    const dy = y - cy;
    const u = dx * c + dy * s;
    const v = -dx * s + dy * c;
    return (u / rx) ** 2 + (v / ry) ** 2 <= 1;
  };
};

// Segment épais à bouts ronds.
const capsule = (x1, y1, x2, y2, r) => (x, y) => {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const t = Math.max(0, Math.min(1, ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy)));
  return (x - x1 - t * dx) ** 2 + (y - y1 - t * dy) ** 2 <= r * r;
};

const triangle = (ax, ay, bx, by, cx, cy) => (x, y) => {
  const d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
  const d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
  const d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
  return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
};

// Courbe de Bézier quadratique épaisse (queue), approchée par des capsules ; rayon qui s'affine vers le bout.
const queue = (p0, p1, p2, r0, r1, segments = 16) => {
  const point = (t) => [
    (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1],
  ];
  const morceaux = [];
  for (let i = 0; i < segments; i++) {
    const [ax, ay] = point(i / segments);
    const [bx, by] = point((i + 1) / segments);
    morceaux.push(capsule(ax, ay, bx, by, r0 + ((r1 - r0) * (i + 0.5)) / segments));
  }
  return union(...morceaux);
};

const union = (...formes) => (x, y) => formes.some((f) => f(x, y));

// --- Silhouettes ----------------------------------------------------------------------------------------------

// Chat assis, de profil (tête à droite). Variantes : queue enroulée devant, dressée, posée derrière ; oreilles plus ou moins hautes.
function chat(variante) {
  const oreille = [0, 6, -4, 10][variante];
  const queues = [
    queue([-40, 14], [-95, 0], [-60, 70], 10, 6),
    queue([-42, 20], [-100, 60], [-80, 150], 10, 6),
    queue([-40, 12], [-110, 10], [-115, 45], 10, 7),
    queue([-38, 10], [-20, -18], [45, 6], 10, 7),
  ];
  return union(
    ellipse(0, 66, 52, 68), // corps
    ellipse(-12, 30, 48, 32), // cuisse
    ellipse(14, 118, 30, 32), // cou
    cercle(20, 150, 36), // tête
    ellipse(48, 140, 14, 11), // museau
    triangle(-12, 168, -6 - oreille / 2, 205 + oreille, 16, 182), // oreille arrière
    triangle(22, 184, 44, 205 + oreille, 50, 166), // oreille avant
    capsule(22, 70, 26, 6, 11), // patte avant
    capsule(42, 70, 44, 6, 11), // patte avant
    ellipse(30, 4, 14, 6), // patte
    ellipse(48, 4, 12, 6), // patte
    queues[variante],
  );
}

// Chien assis, de profil (tête à droite). Variantes : oreilles tombantes ou dressées, queue haute ou basse, museau long ou court.
function chien(variante) {
  const dressees = variante === 1 || variante === 3;
  const museau = variante === 2 ? 22 : 36;
  const oreilles = dressees
    ? union(triangle(10, 178, 14, 226, 38, 194), triangle(32, 194, 50, 230, 62, 188))
    : ellipse(-2, 148, 12, 30, 0.35);
  const queues = [
    queue([-60, 26], [-100, 40], [-110, 92], 9, 6),
    queue([-60, 24], [-95, 10], [-120, 18], 9, 6),
    queue([-58, 30], [-80, 80], [-120, 110], 9, 5),
    queue([-60, 22], [-110, 30], [-118, 80], 10, 6),
  ];
  return union(
    ellipse(-6, 70, 55, 64, -0.2), // corps
    ellipse(-28, 32, 46, 32), // cuisse
    ellipse(-14, 6, 34, 8), // patte arrière au sol
    ellipse(24, 96, 34, 50, 0.2), // poitrail
    capsule(30, 132, 18, 92, 26), // cou
    ellipse(38, 166, 38, 33), // tête
    capsule(58, 160, 58 + museau, 152, 16), // museau
    cercle(60 + museau, 157, 9), // truffe
    oreilles,
    capsule(30, 74, 34, 6, 12), // patte avant
    capsule(54, 74, 56, 6, 12), // patte avant
    ellipse(40, 4, 15, 6), // patte
    ellipse(62, 4, 14, 6), // patte
    queues[variante],
  );
}

// --- Rendu ----------------------------------------------------------------------------------------------------

const rgb = (hex) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));
const melange = (a, b, t) => a.map((v, i) => v + (b[i] - v) * t);

function dessiner(animal) {
  const forme = animal.espece === 'chat' ? chat(animal.variante) : chien(animal.variante);
  const haut = rgb(palette[animal.haut]);
  const bas = rgb(palette[animal.bas]);
  const encre = rgb(palette[animal.silhouette]);
  const ombre = melange(bas, encre, 0.18);
  const sol = (x, y) => ((x - LARGEUR / 2) / 130) ** 2 + ((y - SOL - 4) / 12) ** 2 <= 1;

  const pixels = Buffer.alloc((LARGEUR * 3 + 1) * HAUTEUR); // 1 octet de filtre (0) par ligne
  const pas = 1 / SUR_ECHANTILLONNAGE;
  for (let py = 0; py < HAUTEUR; py++) {
    const ligne = py * (LARGEUR * 3 + 1);
    const fond = melange(haut, bas, py / (HAUTEUR - 1));
    for (let px = 0; px < LARGEUR; px++) {
      let dedans = 0;
      let auSol = 0;
      for (let sy = 0; sy < SUR_ECHANTILLONNAGE; sy++) {
        for (let sx = 0; sx < SUR_ECHANTILLONNAGE; sx++) {
          const x = px + (sx + 0.5) * pas;
          const y = py + (sy + 0.5) * pas;
          // Repère image → repère de la silhouette (centrée, à l'échelle, éventuellement retournée).
          let lx = (x - LARGEUR / 2) / animal.echelle;
          if (animal.miroir) lx = -lx;
          const ly = (SOL - y) / animal.echelle;
          if (forme(lx, ly)) dedans++;
          else if (sol(x, y)) auSol++;
        }
      }
      const n = SUR_ECHANTILLONNAGE * SUR_ECHANTILLONNAGE;
      let couleur = melange(fond, ombre, auSol / n);
      couleur = melange(couleur, encre, dedans / n);
      const i = ligne + 1 + px * 3;
      pixels[i] = Math.round(couleur[0]);
      pixels[i + 1] = Math.round(couleur[1]);
      pixels[i + 2] = Math.round(couleur[2]);
    }
  }
  return png(pixels);
}

// --- Encodeur PNG minimal -------------------------------------------------------------------------------------

const tableCrc = Array.from({ length: 256 }, (_, n) => {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});

function crc32(octets) {
  let c = 0xffffffff;
  for (const o of octets) c = tableCrc[(c ^ o) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function bloc(type, donnees) {
  const longueur = Buffer.alloc(4);
  longueur.writeUInt32BE(donnees.length);
  const corps = Buffer.concat([Buffer.from(type, 'ascii'), donnees]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(corps));
  return Buffer.concat([longueur, corps, crc]);
}

function png(lignesFiltrees) {
  const entete = Buffer.alloc(13);
  entete.writeUInt32BE(LARGEUR, 0);
  entete.writeUInt32BE(HAUTEUR, 4);
  entete[8] = 8; // 8 bits par canal
  entete[9] = 2; // RGB
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    bloc('IHDR', entete),
    bloc('IDAT', deflateSync(lignesFiltrees, { level: 9 })),
    bloc('IEND', Buffer.alloc(0)),
  ]);
}

// --- Écriture -------------------------------------------------------------------------------------------------

const dossier = join(dirname(fileURLToPath(import.meta.url)), 'Photos');
mkdirSync(dossier, { recursive: true });
for (const animal of animaux) {
  const fichier = join(dossier, `${animal.nom.toLowerCase()}.png`);
  const contenu = dessiner(animal);
  writeFileSync(fichier, contenu);
  console.log(`${fichier} (${contenu.length} octets)`);
}
