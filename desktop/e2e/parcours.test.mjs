import assert from 'node:assert/strict'
import { access, mkdir, mkdtemp, realpath, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { dirname, join } from 'node:path'
import { after, before, test } from 'node:test'
import { fileURLToPath } from 'node:url'
import { _electron } from 'playwright-core'

const DESKTOP_DIRECTORY = join(dirname(fileURLToPath(import.meta.url)), '..')
const STARTUP_TIMEOUT_MS = 60_000
const POLL_INTERVAL_MS = 200
const POLL_TIMEOUT_MS = 20_000
const PANE_SELECTOR = '[data-pane-id]'

let sandbox
let application
let window

const waitUntil = async (description, condition) => {
  const deadline = Date.now() + POLL_TIMEOUT_MS
  while (Date.now() < deadline) {
    if (await condition()) {
      return
    }
    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS))
  }
  throw new Error(`Délai dépassé : ${description}`)
}

const exists = (path) =>
  access(path).then(
    () => true,
    () => false,
  )

before(async () => {
  sandbox = await realpath(await mkdtemp(join(tmpdir(), 'tily-e2e-')))
  const home = join(sandbox, 'home')
  await mkdir(home)
  application = await _electron.launch({
    args: [DESKTOP_DIRECTORY, `--user-data-dir=${join(sandbox, 'profile')}`],
    env: { ...process.env, HOME: home, TILY_DATA_DIR: join(sandbox, 'data') },
    timeout: STARTUP_TIMEOUT_MS,
  })
  window = await application.firstWindow()
  await window.locator(PANE_SELECTOR).first().waitFor({ timeout: STARTUP_TIMEOUT_MS })
})

after(async () => {
  await application?.close()
  if (sandbox) {
    await rm(sandbox, { recursive: true, force: true })
  }
})

test('le workspace de départ affiche un pane', async () => {
  assert.equal(await window.locator(PANE_SELECTOR).count(), 1)
})

test('le split côte à côte ajoute un second pane', async () => {
  await window.getByRole('button', { name: 'Split côte à côte' }).first().click()
  await waitUntil('deux panes affichés', async () => (await window.locator(PANE_SELECTOR).count()) === 2)
})

test('une commande saisie dans le pane actif s’exécute dans le vrai shell', async () => {
  const marker = join(sandbox, 'marqueur')
  await window.locator(`${PANE_SELECTOR} .xterm`).last().click()
  await window.keyboard.type(`touch ${marker}`)
  await window.keyboard.press('Enter')
  await waitUntil('fichier créé par la commande', () => exists(marker))
})

test('Cmd + F ouvre la recherche du terminal et Échap la ferme', async () => {
  await window.keyboard.press('Meta+f')
  const searchBar = window.getByRole('search')
  await searchBar.waitFor({ timeout: POLL_TIMEOUT_MS })
  await window.keyboard.press('Escape')
  await searchBar.waitFor({ state: 'detached', timeout: POLL_TIMEOUT_MS })
})
