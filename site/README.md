# convert-ts documentation site

A static documentation + converter site for this repository, published to GitHub Pages at
<https://steve-codemunkies.github.io/convert-ts/>.

The front page is a browser-based Unix epoch timestamp converter with the same behavior as the
PowerToys Command Palette extension (same boundary rule, same output values and formats). The
documentation pages render the Markdown from [`docs/`](../docs) and
[`command-palette/README.md`](../command-palette/README.md) in the browser.

## How it works

- `src/` — the site source (HTML, CSS, JavaScript). No framework, no bundler.
- `src/js/converter.js` — browser port of the extension's converter logic (see
  `command-palette/ConvertTs/Utilities/UnixTimestampConverter.cs`).
- `src/js/doclinks.js` — resolves relative Markdown links to GitHub URLs and generates
  GitHub-style heading anchors.
- `src/lib/marked.esm.js` — vendored copy of [marked](https://marked.js.org) (MIT license) used to
  render Markdown in the browser. Committed to the repo so the site works offline with no
  `npm install`.
- `build.mjs` — assembles `dist/` from `src/`, the extension's SVG icons, and the documentation
  Markdown sources. Pure Node.js standard library.
- `serve.mjs` — minimal static file server for local testing.
- `tests/` — unit tests run with the built-in Node.js test runner. No dependencies.

## Local testing

### Prerequisites

- [Node.js](https://nodejs.org/) 20 or later (LTS recommended). On Windows:

  ```powershell
  winget install -e --id OpenJS.NodeJS.LTS
  ```

There is **nothing to install** — the project has no npm dependencies.

### Run the unit tests

```powershell
cd site
npm test
```

(Equivalent to `node --test "tests/**/*.test.js"`.)

### Build and view the site locally

```powershell
cd site
npm start
```

`npm start` builds the site into `site/dist/` and serves it at <http://localhost:8080/>.
To run the steps separately: `npm run build`, then `npm run serve`. Set the `PORT` environment
variable to use a different port.

Any change to `src/`, the Markdown docs, or the icons requires re-running `npm run build`.

## Deployment

Deployment is handled by [`.github/workflows/pages.yml`](../.github/workflows/pages.yml), which runs
the unit tests, builds the site, and publishes `site/dist` to GitHub Pages on every push that
touches the site sources or documentation.

One-time repository setup: **Settings → Pages → Build and deployment → Source: GitHub Actions**.

Site JavaScript is also tested on every pull request that touches `site/` by
[`.github/workflows/site-test.yml`](../.github/workflows/site-test.yml).
