/**
 * Pure helpers for the documentation pages: resolving relative Markdown links
 * against the repository and generating GitHub-style heading anchors.
 * Dependency-free so it can be unit tested with `node --test`.
 */

/**
 * Normalizes a relative repo path, resolving '.' and '..' segments.
 * Returns the path relative to the repository root.
 */
export function normalizeRepoPath(path) {
  const segments = [];
  for (const segment of path.split('/')) {
    if (segment === '' || segment === '.') {
      continue;
    }
    if (segment === '..') {
      segments.pop();
    } else {
      segments.push(segment);
    }
  }
  return segments.join('/');
}

/**
 * Resolves a Markdown link from a document against the repository.
 * - Absolute URLs (any scheme) and pure '#fragment' links are returned unchanged.
 * - Relative links are resolved against `sourceDir` (the document's folder in the
 *   repo) and mapped to a github.com blob (file) or tree (directory) URL.
 */
export function resolveDocHref(href, sourceDir, repoUrl, branch = 'main') {
  if (/^([a-z][a-z0-9+.-]*:|#)/i.test(href)) {
    return href;
  }

  const [pathPart, hashPart] = href.split('#');
  const resolved = normalizeRepoPath(`${sourceDir}/${pathPart}`);
  const hash = hashPart ? `#${hashPart}` : '';
  const lastSegment = resolved.split('/').pop() ?? '';
  const kind = lastSegment.includes('.') ? 'blob' : 'tree';
  return `${repoUrl}/${kind}/${branch}/${resolved}${hash}`;
}

/** Resolves a Markdown image source to a raw.githubusercontent.com URL. */
export function resolveDocImageSrc(src, sourceDir, rawUrl, branch = 'main') {
  if (/^([a-z][a-z0-9+.-]*:|#)/i.test(src)) {
    return src;
  }
  const resolved = normalizeRepoPath(`${sourceDir}/${src}`);
  return `${rawUrl}/${branch}/${resolved}`;
}

/**
 * GitHub-style heading slug: lowercase, punctuation removed (keeps letters,
 * digits, underscores and hyphens), whitespace collapsed to single hyphens.
 */
export function slugifyHeading(text) {
  return text
    .trim()
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .replace(/\s/g, '-'); // GitHub replaces each whitespace char individually
}
