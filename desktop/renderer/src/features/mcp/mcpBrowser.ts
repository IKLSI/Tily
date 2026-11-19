import type { BrowserAction, BrowserCapture, BrowserConsoleEntry, BrowserEvaluation, BrowserSnapshot, BrowserWait, BrowserEntries, BrowserLogLevel, BrowserNavigation, BrowserNetworkEntry, BrowserState } from '../../bridge/browserMessages'
import { isBrowserStarted } from '../browser/browserLayer'
import { requestBrowser } from '../browser/browserRequests'
import { BLANK_PAGE, BrowserViewport } from '../../model/browser'
import { createOwnedBrowserPane, isBrowserPane, splitLeaf, SplitAxis, tabOfPane } from '../../model/session'
import { useBrowserStore } from '../browser/browserStore'
import { useSessionStore } from '../../stores/sessionStore'
import { flagArgument, numberArgument, textArgument, type McpArguments } from './mcpArguments'
import { paneLabel, requireConsent } from './mcpConsent'
import { requireCaller } from './mcpOrganize'
import { locationOf, placeOf, placesOf, requirePlace, requireSession, type PanePlace } from './mcpPanes'

enum Placement {
  Right = 'right',
  Down = 'down',
  Tab = 'tab',
}

const NAVIGATION_TIMEOUT_MS = 40_000
const CAPTURE_TIMEOUT_MS = 40_000
const START_TIMEOUT_MS = 15_000
const START_POLL_MS = 100
const ACTION_TIMEOUT_MS = 40_000
const WAIT_MARGIN_MS = 10_000
const DETAIL_MAX_CHARS = 500
const DEFAULT_LIMIT = 50

const lastBrowserOf = new Map<string, string>()

const viewportOf = (value: string | undefined): BrowserViewport | undefined => (value === BrowserViewport.Mobile || value === BrowserViewport.Desktop ? value : undefined)

const remember = (caller: string | undefined, place: PanePlace): PanePlace => {
  if (caller) {
    lastBrowserOf.set(caller, place.pane.id)
  }
  return place
}

const browserPlace = (paneId: string | undefined, caller: string | undefined): PanePlace => {
  if (paneId) {
    const place = requirePlace(paneId)
    if (!isBrowserPane(place.pane)) {
      throw new Error(`Le pane ${paneId} (${locationOf(place)}) est un terminal, pas un navigateur : les panes navigateur portent kind: "browser" dans tily_layout.`)
    }
    return remember(caller, place)
  }
  const session = requireSession()
  const browsers = placesOf(session).filter((place) => isBrowserPane(place.pane))
  const callerTab = placeOf(session, caller)?.tab.id
  const inCallerTab = browsers.filter((place) => place.tab.id === callerTab)
  const lastUsed = browsers.find((place) => caller && place.pane.id === lastBrowserOf.get(caller))
  const target = lastUsed ?? browsers.filter((place) => caller && place.pane.owner === caller).at(-1) ?? (inCallerTab.length === 1 ? inCallerTab[0] : undefined) ?? (browsers.length === 1 ? browsers[0] : undefined)
  if (target) {
    return target
  }
  throw new Error(browsers.length === 0 ? 'Aucun pane navigateur dans Tily : ouvrez-en un avec tily_browser_open.' : 'Plusieurs panes navigateur : indiquez pane (identifiants dans tily_layout, kind: "browser").')
}

const waitForStart = async (paneId: string): Promise<void> => {
  const deadline = Date.now() + START_TIMEOUT_MS
  while (!useBrowserStore.getState().states[paneId]) {
    if (Date.now() > deadline) {
      throw new Error('Le navigateur n’a pas démarré à temps : réessayez dans un instant.')
    }
    await new Promise((resolve) => setTimeout(resolve, START_POLL_MS))
  }
}

const startedBrowser = async (values: McpArguments, caller: string | undefined): Promise<PanePlace> => {
  const place = browserPlace(textArgument(values, 'pane'), caller)
  if (!isBrowserStarted(place.pane.id)) {
    throw new Error(`Le pane navigateur ${place.pane.id} (${locationOf(place)}) n’a pas démarré : il n’a jamais été affiché depuis le lancement de Tily. Affichez-le avec tily_focus.`)
  }
  await waitForStart(place.pane.id)
  return place
}

const confirmInteraction = async (place: PanePlace, caller: string | undefined, action: string, detail?: string): Promise<void> => {
  if (place.pane.owner) {
    return
  }
  await requireConsent({ caller, action, target: paneLabel(place), detail: detail && detail.length > DETAIL_MAX_CHARS ? `${detail.slice(0, DETAIL_MAX_CHARS)}…` : detail })
}

const requiredText = (values: McpArguments, name: string, message: string): string => {
  const value = textArgument(values, name)
  if (!value) {
    throw new Error(message)
  }
  return value
}

const referenceOf = (values: McpArguments): string => requiredText(values, 'reference', 'Indiquez la référence de l’élément (tily_browser_snapshot).')

const describe = (place: PanePlace) => ({ pane: place.pane.id, location: locationOf(place) })

export const openBrowserPane = async (values: McpArguments, caller: string | undefined) => {
  const owner = requireCaller(caller)
  const source = requirePlace(textArgument(values, 'pane') ?? caller)
  const placement = (textArgument(values, 'placement') as Placement | undefined) ?? Placement.Right
  const viewport = viewportOf(textArgument(values, 'viewport')) ?? BrowserViewport.Desktop
  const focus = flagArgument(values, 'focus')
  const url = textArgument(values, 'url') ?? BLANK_PAGE
  const pane = createOwnedBrowserPane(source.pane.path, source.pane.shell, BLANK_PAGE, viewport, owner)
  useSessionStore.getState().change((draft) => {
    const workspace = draft.workspaces.find((candidate) => candidate.id === source.workspace.id)
    const tab = workspace?.tabs.find((candidate) => candidate.id === source.tab.id)
    if (!workspace || !tab) {
      return
    }
    if (placement === Placement.Tab) {
      const created = { ...tabOfPane(pane), owner }
      workspace.tabs.push(created)
      workspace.active = created.id
    } else {
      tab.tree = splitLeaf(tab.tree, source.pane.id, placement === Placement.Down ? SplitAxis.Vertical : SplitAxis.Horizontal, pane)
      workspace.active = tab.id
      if (focus) {
        tab.active = pane.id
      }
    }
    draft.active = workspace.id
  })
  await waitForStart(pane.id)
  const navigation = url === BLANK_PAGE ? undefined : await requestBrowser<BrowserNavigation>({ type: 'browser.navigate', pane: pane.id, url }, NAVIGATION_TIMEOUT_MS)
  const place = remember(caller, requirePlace(pane.id))
  return { ...describe(place), placement, viewport, navigation, message: `Navigateur ouvert dans « ${locationOf(place)} »${navigation ? ` : ${navigation.title || navigation.url}` : ' sur une page vide'}.` }
}

export const navigateBrowserPane = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const url = textArgument(values, 'url')
  if (!url) {
    throw new Error('Indiquez l’adresse à charger.')
  }
  return { ...describe(place), ...(await requestBrowser<BrowserNavigation>({ type: 'browser.navigate', pane: place.pane.id, url }, NAVIGATION_TIMEOUT_MS)) }
}

export const reloadBrowserPane = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  return { ...describe(place), ...(await requestBrowser<BrowserNavigation>({ type: 'browser.reload', pane: place.pane.id }, NAVIGATION_TIMEOUT_MS)) }
}

export const resizeBrowserPane = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const viewport = viewportOf(textArgument(values, 'viewport'))
  if (!viewport) {
    throw new Error('Indiquez la largeur : desktop ou mobile.')
  }
  useSessionStore.getState().setPaneViewport(place.pane.id, viewport)
  const state = await requestBrowser<BrowserState>({ type: 'browser.viewport', pane: place.pane.id, viewport })
  return { ...describe(place), viewport: state.viewport, url: state.url, title: state.title }
}

export const browserConsole = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const level = (textArgument(values, 'level') ?? 'log') as BrowserLogLevel
  const entries = await requestBrowser<BrowserEntries<BrowserConsoleEntry>>({
    type: 'browser.console',
    pane: place.pane.id,
    level,
    sinceLoad: flagArgument(values, 'sinceLoad'),
    limit: numberArgument(values, 'limit') ?? DEFAULT_LIMIT,
  })
  return { ...describe(place), url: useBrowserStore.getState().states[place.pane.id]?.url, ...entries }
}

export const browserNetwork = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const entries = await requestBrowser<BrowserEntries<BrowserNetworkEntry>>({
    type: 'browser.network',
    pane: place.pane.id,
    failedOnly: flagArgument(values, 'failedOnly'),
    sinceLoad: flagArgument(values, 'sinceLoad'),
    limit: numberArgument(values, 'limit') ?? DEFAULT_LIMIT,
  })
  return { ...describe(place), url: useBrowserStore.getState().states[place.pane.id]?.url, ...entries }
}

export const browserScreenshot = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const viewport = viewportOf(textArgument(values, 'viewport')) ?? place.pane.viewport ?? BrowserViewport.Desktop
  const capture = await requestBrowser<BrowserCapture>({ type: 'browser.screenshot', pane: place.pane.id, viewport, fullPage: flagArgument(values, 'fullPage') }, CAPTURE_TIMEOUT_MS)
  return { ...describe(place), ...capture }
}

export const browserSnapshot = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const snapshot = await requestBrowser<BrowserSnapshot>({ type: 'browser.pageSnapshot', pane: place.pane.id }, ACTION_TIMEOUT_MS)
  return { ...describe(place), ...snapshot }
}

export const browserClick = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const ref = referenceOf(values)
  await confirmInteraction(place, caller, 'Cliquer sur un élément de la page', `Élément ${ref}`)
  return { ...describe(place), ...(await requestBrowser<BrowserAction>({ type: 'browser.click', pane: place.pane.id, ref }, ACTION_TIMEOUT_MS)) }
}

export const browserType = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const ref = referenceOf(values)
  const text = requiredText(values, 'text', 'Indiquez le texte à saisir.')
  await confirmInteraction(place, caller, 'Saisir du texte dans la page', `Élément ${ref} : ${text}`)
  const action = await requestBrowser<BrowserAction>({ type: 'browser.type', pane: place.pane.id, ref, text, clear: flagArgument(values, 'clear'), submit: flagArgument(values, 'submit') }, ACTION_TIMEOUT_MS)
  return { ...describe(place), ...action }
}

export const browserPress = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const chord = requiredText(values, 'chord', 'Indiquez la touche à presser.')
  await confirmInteraction(place, caller, 'Presser une touche dans la page', chord)
  return { ...describe(place), ...(await requestBrowser<BrowserAction>({ type: 'browser.press', pane: place.pane.id, chord }, ACTION_TIMEOUT_MS)) }
}

export const browserSelect = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const ref = referenceOf(values)
  const option = requiredText(values, 'option', 'Indiquez l’option à choisir.')
  await confirmInteraction(place, caller, 'Choisir une option dans la page', `Élément ${ref} : ${option}`)
  return { ...describe(place), ...(await requestBrowser<BrowserAction>({ type: 'browser.select', pane: place.pane.id, ref, option }, ACTION_TIMEOUT_MS)) }
}

export const browserWaitFor = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const timeoutMs = numberArgument(values, 'timeoutMs') ?? 10_000
  const wait = await requestBrowser<BrowserWait>(
    { type: 'browser.waitFor', pane: place.pane.id, text: textArgument(values, 'text'), ref: textArgument(values, 'reference'), url: textArgument(values, 'url'), gone: flagArgument(values, 'gone'), timeoutMs },
    timeoutMs + WAIT_MARGIN_MS,
  )
  return { ...describe(place), ...wait }
}

export const browserEvaluate = async (values: McpArguments, caller: string | undefined) => {
  const place = await startedBrowser(values, caller)
  const expression = requiredText(values, 'expression', 'Indiquez l’expression JavaScript à évaluer.')
  await confirmInteraction(place, caller, 'Exécuter du JavaScript dans la page', expression)
  return { ...describe(place), ...(await requestBrowser<BrowserEvaluation>({ type: 'browser.evaluate', pane: place.pane.id, expression }, ACTION_TIMEOUT_MS)) }
}
