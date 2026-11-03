/**
 * DOM wiring for the documentation page. Fetches the Markdown sources that
 * build.mjs copied into dist/docs-src, renders them with the vendored marked
 * library, and rewrites relative links/images to point at the GitHub repo.
 * Link-resolution logic lives in doclinks.js (unit tested).
 */
import { marked } from '../lib/marked.esm.js';
import { resolveDocHref, resolveDocImageSrc, slugifyHeading } from './doclinks.js';

const docsList = document.getElementById('docs-list');
const contentElement = document.getElementById('doc-content');

async function loadManifest() {
  const response = await fetch('./docs-src/manifest.json');
  if (!response.ok) {
    throw new Error(`Failed to load docs manifest (HTTP ${response.status})`);
  }
  return response.json();
}

function renderNav(manifest, activeId) {
  docsList.replaceChildren(...manifest.docs.map((doc) => {
    const item = document.createElement('li');
    const link = document.createElement('a');
    link.href = `#${doc.id}`;
    link.textContent = doc.label;
    link.title = doc.title;
    if (doc.id === activeId) {
      link.classList.add('active');
    }
    item.append(link);
    return item;
  }));
}

function addHeadingAnchors() {
  const seen = new Set();
  for (const heading of contentElement.querySelectorAll('h1, h2, h3, h4, h5, h6')) {
    if (heading.id) {
      continue;
    }
    let slug = slugifyHeading(heading.textContent ?? '');
    let suffix = 0;
    while (seen.has(slug)) {
      suffix += 1;
      slug = `${slugifyHeading(heading.textContent ?? '')}-${suffix}`;
    }
    seen.add(slug);
    heading.id = slug;
  }
}

function rewriteLinks(doc, manifest) {
  for (const link of contentElement.querySelectorAll('a[href]')) {
    const href = link.getAttribute('href');
    const resolved = resolveDocHref(href, doc.sourceDir, manifest.repoUrl, manifest.branch);
    link.setAttribute('href', resolved);
    if (/^https?:\/\//i.test(resolved)) {
      link.setAttribute('target', '_blank');
      link.setAttribute('rel', 'noopener noreferrer');
    }
  }

  for (const image of contentElement.querySelectorAll('img[src]')) {
    const src = image.getAttribute('src');
    image.setAttribute('src', resolveDocImageSrc(src, doc.sourceDir, manifest.rawUrl, manifest.branch));
  }
}

async function showDoc(manifest, requestedId) {
  const doc = manifest.docs.find((entry) => entry.id === requestedId) ?? manifest.docs[0];
  renderNav(manifest, doc.id);

  try {
    const response = await fetch(`./${doc.file}`);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    const markdown = await response.text();
    contentElement.innerHTML = marked.parse(markdown);
    addHeadingAnchors();
    rewriteLinks(doc, manifest);
    document.title = `${doc.title} — convert-ts`;
  } catch (error) {
    contentElement.innerHTML = '';
    const paragraph = document.createElement('p');
    paragraph.textContent = `Failed to load ${doc.label} documentation: ${error.message}`;
    contentElement.append(paragraph);
  }
}

async function main() {
  try {
    const manifest = await loadManifest();
    const currentId = () => window.location.hash.replace(/^#/, '');
    await showDoc(manifest, currentId());
    window.addEventListener('hashchange', () => showDoc(manifest, currentId()));
  } catch (error) {
    docsList.replaceChildren();
    contentElement.innerHTML = '';
    const paragraph = document.createElement('p');
    paragraph.textContent = String(error);
    contentElement.append(paragraph);
  }
}

main();
