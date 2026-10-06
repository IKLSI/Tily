import { useExplorerStore } from '../features/explorer/explorerStore'
import { useFilePickerStore } from '../features/explorer/filePickerStore'
import { useCommitPickerStore } from '../features/git/commitPickerStore'
import { useGitStore } from '../features/git/gitStore'
import { answerConsent, ConsentAnswer } from '../features/mcp/mcpConsent'
import { useMcpConsentStore } from '../features/mcp/mcpConsentStore'
import { usePreviewStore } from '../features/preview/previewStore'
import { cancelClose } from '../features/terminal/closeGuard'
import { usePasteStore } from '../features/terminal/pasteStore'
import { useWorktreeStore } from '../features/worktrees/worktreeStore'
import { StatusLevel, useHostStore } from '../stores/hostStore'
import { useUiStore } from '../stores/uiStore'

const DIALOG_ERROR_STATUS = 'Le dialogue a rencontré une erreur et a été fermé.'

export const closeOpenDialogs = (): void => {
  const ui = useUiStore.getState()
  ui.closePalette()
  ui.closeProjectPicker()
  ui.closeSettings()
  if (ui.closeConfirmation) {
    cancelClose()
  }
  useExplorerStore.getState().cancelDelete()
  useGitStore.getState().confirm(null)
  usePasteStore.getState().clear()
  useMcpConsentStore.getState().queue.forEach((request) => answerConsent(request.id, ConsentAnswer.Refused))
  const worktrees = useWorktreeStore.getState()
  worktrees.setPicker(null)
  worktrees.setDraft(null)
  worktrees.setRemoval(null)
  useFilePickerStore.getState().close()
  useCommitPickerStore.getState().setOpen(false)
  usePreviewStore.getState().clearPending()
}

export const recoverFromDialogError = (): void => {
  closeOpenDialogs()
  useHostStore.getState().setStatus(DIALOG_ERROR_STATUS, StatusLevel.Error)
}
