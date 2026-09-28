const fs = require('fs');

const markdown = fs.readFileSync('specifications-terminal.md', 'utf8').replace(/\r/g, '');
const oldHtml = fs.readFileSync('specifications-terminal.html', 'utf8');
let head = oldHtml.slice(0, oldHtml.indexOf('</style>') + '</style>'.length);
head = head.replace('Terminal & workspaces — Spécifications fonctionnelles', 'Dock — Spécifications complètes');
head += '<style>nav ol{columns:2;padding-left:22px;font-size:14px}nav li{break-inside:avoid}td:first-child{min-width:85px}.table-wrap{margin:18px 0}header a{color:#c5dfce}section h2{scroll-margin-top:20px}@media(max-width:700px){nav ol{columns:1}}</style></head>';

const escape = value => value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const inline = value => escape(value)
  .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>')
  .replace(/`([^`]+)`/g, '<code>$1</code>')
  .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

const lines = markdown.split('\n');
const sections = [];
let content = '';
let inSection = false;
let sectionIndex = 0;

function closeSection() {
  if (inSection) content += '</section>';
  inSection = false;
}

for (let i = 0; i < lines.length; i += 1) {
  const raw = lines[i].trim();
  if (!raw || raw.startsWith('# ')) continue;
  if (raw.startsWith('## ')) {
    closeSection();
    const id = `section-${++sectionIndex}`;
    const title = raw.slice(3);
    sections.push({ id, title });
    content += `<section id="${id}"><h2>${inline(title)}</h2>`;
    inSection = true;
    continue;
  }
  if (raw.startsWith('### ')) {
    content += `<h3>${inline(raw.slice(4))}</h3>`;
    continue;
  }
  if (raw.startsWith('|')) {
    const rows = [];
    while (i < lines.length && lines[i].trim().startsWith('|')) {
      rows.push(lines[i].trim().slice(1, -1).split('|').map(cell => cell.trim()));
      i += 1;
    }
    i -= 1;
    const header = rows[0].map(cell => `<th>${inline(cell)}</th>`).join('');
    const body = rows.slice(2).map(row => `<tr>${row.map(cell => `<td>${inline(cell)}</td>`).join('')}</tr>`).join('');
    content += `<div class="table-wrap"><table><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table></div>`;
    continue;
  }
  const ordered = /^\d+\. /.test(raw);
  const unordered = raw.startsWith('- ');
  if (ordered || unordered) {
    const pattern = ordered ? /^\d+\. / : /^- /;
    const tag = ordered ? 'ol' : 'ul';
    let list = `<${tag}>`;
    while (i < lines.length && pattern.test(lines[i].trim())) {
      list += `<li>${inline(lines[i].trim().replace(pattern, ''))}</li>`;
      i += 1;
    }
    i -= 1;
    content += `${list}</${tag}>`;
    continue;
  }
  let paragraph = raw;
  while (i + 1 < lines.length && lines[i + 1].trim() && !/^(#|\||- |\d+\. )/.test(lines[i + 1].trim())) {
    paragraph += ` ${lines[++i].trim()}`;
  }
  content += `<p>${inline(paragraph)}</p>`;
}
closeSection();

const header = `<body><header><div class="eyebrow">Référence pour l’implémentation · Version 2.0</div><h1>Dock — Spécifications complètes</h1><p>Terminal Windows, workspaces libres, interface compacte verte et navigation en arborescence.</p><div class="meta"><span class="pill">24 septembre 2026</span><span class="pill">18 sections</span><span class="pill">30 scénarios de recette</span></div><p><a href="specifications-terminal.md">Source Markdown versionnable</a> &nbsp; · &nbsp; <a href="docs/inspection-environnement.md">Inspection de l’environnement</a></p><button type="button" onclick="window.print()">Imprimer / Enregistrer en PDF</button></header>`;
const toc = `<nav aria-label="Sommaire"><h2>Sommaire</h2><ol>${sections.map(section => `<li><a href="#${section.id}">${inline(section.title.replace(/^\d+\. /, ''))}</a></li>`).join('')}</ol></nav>`;
const footer = '<footer>Document autonome, sans dépendance externe. Version Markdown et version HTML issues du même contenu.</footer>';

if (sections.length !== 18) throw new Error(`Nombre de sections inattendu : ${sections.length}`);
fs.writeFileSync('specifications-terminal.html', `${head}${header}<main>${toc}${content}${footer}</main></body></html>`);
console.log(`Rendered ${sections.length} sections from specifications-terminal.md.`);
