/**
 * DOM wiring for the converter front page. All conversion logic lives in
 * converter.js (unit tested); this module only renders the view model and
 * handles clipboard interaction.
 */
import {
  DEFAULT_BOUNDARY,
  buildViewModel,
  parseBoundary,
} from './converter.js';

const queryInput = document.getElementById('query');
const timeZoneSelect = document.getElementById('timezone');
const boundaryInput = document.getElementById('boundary');
const hintElement = document.getElementById('hint');
const resultsBody = document.getElementById('results-body');

const STORAGE_KEYS = {
  timeZone: 'convert-ts.timeZone',
  boundary: 'convert-ts.boundary',
};

const NOW_TICK_MS = 1000;
let nowTimer = null;

function populateTimeZones() {
  const localZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const allZones = typeof Intl.supportedValuesOf === 'function'
    ? Intl.supportedValuesOf('timeZone')
    : [];

  const fragment = document.createDocumentFragment();
  fragment.append(new Option('UTC', 'UTC'));

  if (localZone && localZone !== 'UTC') {
    fragment.append(new Option(`Local (${localZone})`, localZone));
  }

  for (const zone of allZones) {
    if (zone !== 'UTC' && zone !== localZone) {
      fragment.append(new Option(zone, zone));
    }
  }

  timeZoneSelect.append(fragment);

  const savedZone = localStorage.getItem(STORAGE_KEYS.timeZone);
  if (savedZone && [...timeZoneSelect.options].some((option) => option.value === savedZone)) {
    timeZoneSelect.value = savedZone;
  }
}

function restoreSettings() {
  boundaryInput.value = localStorage.getItem(STORAGE_KEYS.boundary) ?? String(DEFAULT_BOUNDARY);
}

async function copyText(text, button) {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text);
    } else {
      // Fallback for non-secure contexts (e.g. http on a LAN host).
      const textarea = document.createElement('textarea');
      textarea.value = text;
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.append(textarea);
      textarea.select();
      document.execCommand('copy');
      textarea.remove();
    }
    button.textContent = 'Copied!';
    button.classList.add('copied');
  } catch {
    button.textContent = 'Failed';
  }

  setTimeout(() => {
    button.textContent = 'Copy';
    button.classList.remove('copied');
  }, 1500);
}

function createRow({ label, text }) {
  const row = document.createElement('tr');

  const labelCell = document.createElement('td');
  labelCell.textContent = label;

  const valueCell = document.createElement('td');
  valueCell.className = 'value';
  valueCell.textContent = text;

  const actionCell = document.createElement('td');
  const button = document.createElement('button');
  button.type = 'button';
  button.className = 'copy-button';
  button.textContent = 'Copy';
  button.setAttribute('aria-label', `Copy ${label}`);
  button.addEventListener('click', () => copyText(text, button));
  actionCell.append(button);

  row.append(labelCell, valueCell, actionCell);
  return row;
}

function render() {
  const viewModel = buildViewModel(queryInput.value, {
    boundary: parseBoundary(boundaryInput.value),
    timeZone: timeZoneSelect.value,
  });

  resultsBody.replaceChildren(...viewModel.rows.map(createRow));

  if (viewModel.hint) {
    hintElement.textContent = viewModel.hint;
    hintElement.hidden = false;
  } else {
    hintElement.hidden = true;
  }

  // When showing live "now" values, refresh once per second.
  if (viewModel.showingNow && nowTimer === null) {
    nowTimer = setInterval(render, NOW_TICK_MS);
  } else if (!viewModel.showingNow && nowTimer !== null) {
    clearInterval(nowTimer);
    nowTimer = null;
  }
}

function saveSettings() {
  localStorage.setItem(STORAGE_KEYS.timeZone, timeZoneSelect.value);
  localStorage.setItem(STORAGE_KEYS.boundary, boundaryInput.value);
}

populateTimeZones();
restoreSettings();

queryInput.addEventListener('input', render);
timeZoneSelect.addEventListener('change', () => {
  saveSettings();
  render();
});
boundaryInput.addEventListener('input', () => {
  saveSettings();
  render();
});

for (const example of document.querySelectorAll('[data-example]')) {
  example.addEventListener('click', () => {
    queryInput.value = example.dataset.example;
    queryInput.focus();
    render();
  });
}

render();
