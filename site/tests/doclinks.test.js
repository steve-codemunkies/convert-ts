import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import {
  normalizeRepoPath,
  resolveDocHref,
  resolveDocImageSrc,
  slugifyHeading,
} from '../src/js/doclinks.js';

const REPO = 'https://github.com/steve-codemunkies/convert-ts';
const RAW = 'https://raw.githubusercontent.com/steve-codemunkies/convert-ts';

describe('normalizeRepoPath', () => {
  it('resolves dot segments', () => {
    assert.equal(normalizeRepoPath('command-palette/../docs/x.md'), 'docs/x.md');
    assert.equal(normalizeRepoPath('docs/bash/./Convert-UnixTimestamp.md'), 'docs/bash/Convert-UnixTimestamp.md');
    assert.equal(normalizeRepoPath('docs/a/../../b.md'), 'b.md');
  });
});

describe('resolveDocHref', () => {
  it('leaves absolute URLs and fragments unchanged', () => {
    assert.equal(resolveDocHref('https://example.com/x', 'command-palette', REPO), 'https://example.com/x');
    assert.equal(resolveDocHref('http://example.com', 'docs/bash', REPO), 'http://example.com');
    assert.equal(resolveDocHref('mailto:a@b.c', 'docs/bash', REPO), 'mailto:a@b.c');
    assert.equal(resolveDocHref('#settings', 'command-palette', REPO), '#settings');
  });

  it('maps relative file links to blob URLs', () => {
    assert.equal(
      resolveDocHref('../.github/workflows/command-palette-build.yml', 'command-palette', REPO),
      `${REPO}/blob/main/.github/workflows/command-palette-build.yml`,
    );
  });

  it('maps relative directory links to tree URLs', () => {
    assert.equal(
      resolveDocHref('../.github/scripts', 'command-palette', REPO),
      `${REPO}/tree/main/.github/scripts`,
    );
  });

  it('preserves fragments on relative links', () => {
    assert.equal(
      resolveDocHref('../docs/guide.md#install', 'command-palette', REPO),
      `${REPO}/blob/main/docs/guide.md#install`,
    );
  });

  it('resolves same-folder links', () => {
    assert.equal(
      resolveDocHref('Convert-UnixTimestamp.md', 'docs/bash', REPO),
      `${REPO}/blob/main/docs/bash/Convert-UnixTimestamp.md`,
    );
  });
});

describe('resolveDocImageSrc', () => {
  it('maps relative image sources to raw URLs', () => {
    assert.equal(
      resolveDocImageSrc('../assets/screenshot1.png', 'command-palette', RAW),
      `${RAW}/main/assets/screenshot1.png`,
    );
  });

  it('leaves absolute image sources unchanged', () => {
    const src = 'https://get.microsoft.com/images/en-us%20dark.svg';
    assert.equal(resolveDocImageSrc(src, 'command-palette', RAW), src);
  });
});

describe('slugifyHeading', () => {
  it('produces GitHub-style anchors', () => {
    assert.equal(slugifyHeading('Settings'), 'settings');
    assert.equal(slugifyHeading('Build and Install from Source'), 'build-and-install-from-source');
    assert.equal(slugifyHeading('`convertfrom_unixtimestamp`'), 'convertfrom_unixtimestamp');
    assert.equal(slugifyHeading('Convert-UnixTimestamp — Bash Functions'), 'convert-unixtimestamp--bash-functions');
  });
});
