/**
 * Assembles the deployable site into site/dist:
 *   - copies everything from site/src
 *   - copies the SVG icons from the command-palette extension assets
 *   - copies the documentation Markdown (docs/**, command-palette/README.md)
 *     into dist/docs-src and generates a manifest.json describing them
 *
 * Uses only the Node.js standard library — there is nothing to npm install.
 */
import { cp, mkdir, readFile, rm, writeFile, copyFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const siteDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(siteDir, '..');
const distDir = path.join(siteDir, 'dist');

/** Branch used when rewriting relative Markdown links to github.com URLs. */
const BRANCH = 'main';

/** Documentation sources, in navigation order. */
const DOCS = [
  { id: 'powershell', label: 'PowerShell module', source: 'docs/powershell/Convert-UnixTimestamp.md' },
  { id: 'bash', label: 'Bash functions', source: 'docs/bash/Convert-UnixTimestamp.md' },
  { id: 'command-palette', label: 'Command Palette extension', source: 'command-palette/README.md' },
];

const ICONS = ['convert-ts.svg', 'convert-ts.wide.svg'];

function extractTitle(markdown, fallback) {
  const match = markdown.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : fallback;
}

function sourceDirectory(source) {
  return source.slice(0, source.lastIndexOf('/'));
}

const packageJson = JSON.parse(await readFile(path.join(siteDir, 'package.json'), 'utf8'));
const repoUrl = packageJson.repository.url.replace(/\.git$/, '');
const rawUrl = repoUrl.replace('https://github.com/', 'https://raw.githubusercontent.com/');

console.log('Building convert-ts documentation site…');

await rm(distDir, { recursive: true, force: true });
await cp(path.join(siteDir, 'src'), distDir, { recursive: true });

await mkdir(path.join(distDir, 'assets'), { recursive: true });
for (const icon of ICONS) {
  await copyFile(
    path.join(repoRoot, 'command-palette', 'ConvertTs', 'Assets', icon),
    path.join(distDir, 'assets', icon),
  );
}

const docsOutDir = path.join(distDir, 'docs-src');
await mkdir(docsOutDir, { recursive: true });

const manifestDocs = [];
for (const doc of DOCS) {
  const markdown = await readFile(path.join(repoRoot, doc.source), 'utf8');
  const file = `docs-src/${doc.id}.md`;
  await writeFile(path.join(distDir, file), markdown);
  manifestDocs.push({
    id: doc.id,
    label: doc.label,
    title: extractTitle(markdown, doc.label),
    file,
    sourceDir: sourceDirectory(doc.source),
  });
  console.log(`  + ${doc.source} -> ${file}`);
}

const manifest = { repoUrl, rawUrl, branch: BRANCH, docs: manifestDocs };
await writeFile(path.join(docsOutDir, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);

console.log(`Done. Output: ${path.relative(repoRoot, distDir)}${path.sep}`);
