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
- `Use-PortableNode.ps1` — downloads a portable Node.js build and puts it on PATH (for Windows
  machines without Node.js installed).
- `tests/` — unit tests run with the built-in Node.js test runner. No dependencies.

## Local testing

### Prerequisites

- [Node.js](https://nodejs.org/) 20 or later (LTS recommended).

There is **nothing to install** for the site itself — the project has no npm dependencies. You only
need `node` (and `npm`) executables, via either of the options below.

#### Option A — install Node.js (recommended)

On Windows:

```powershell
winget install -e --id OpenJS.NodeJS.LTS
```

Or download the LTS installer from <https://nodejs.org/>.

#### Option B — portable Node.js on Windows (no install, no admin rights)

If you don't want to install Node.js system-wide, use the official zip build via the helper script
[`Use-PortableNode.ps1`](Use-PortableNode.ps1). Run it **dot-sourced** from the repository root so
the PATH change applies to your current terminal:

```powershell
. .\site\Use-PortableNode.ps1
```

Then verify:

```powershell
node --version
```

The script downloads Node 22 LTS to `$env:TEMP\node-portable` (pass `-Version` or `-Destination`
to change this), skips the download if it is already present, and prepends the binaries to
`$env:PATH`. It is equivalent to running these steps manually:

```powershell
# Download and extract (any v20+ version works)
Invoke-WebRequest -Uri 'https://nodejs.org/dist/v22.11.0/node-v22.11.0-win-x64.zip' -OutFile "$env:TEMP\node-portable.zip"
Expand-Archive -Path "$env:TEMP\node-portable.zip" -DestinationPath "$env:TEMP\node-portable" -Force

# Put the portable binaries on PATH for this terminal session only
$env:PATH = "$env:TEMP\node-portable\node-v22.11.0-win-x64;$env:PATH"

node --version
```

Notes:

- The PATH change lasts only for the current terminal session — re-run the script (dot-sourced) in
  each new terminal, or invoke `node.exe` by its full path.
- `node` must be on PATH when using the npm scripts (`npm test`, `npm start`), because npm spawns
  `node` as a child process. Without PATH changes, call node directly instead, e.g.
  `& "$env:TEMP\node-portable\node-v22.11.0-win-x64\node.exe" build.mjs`.

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

Deployment is handled by [`.github/workflows/site-publish.yml`](../.github/workflows/site-publish.yml), which runs
the unit tests, builds the site, and publishes `site/dist` to GitHub Pages on every push that
touches the site sources or documentation.

One-time repository setup: **Settings → Pages → Build and deployment → Source: GitHub Actions**.

Site JavaScript is also tested on every pull request that touches `site/` by
[`.github/workflows/site-test.yml`](../.github/workflows/site-test.yml).
