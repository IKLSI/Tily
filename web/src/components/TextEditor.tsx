import { EditorState } from '@codemirror/state'
import { EditorView } from '@codemirror/view'
import { useEffect, useRef } from 'react'
import { editorExtensions } from '../preview/editorSetup'
import { savePreview } from '../preview/previewEdit'
import { usePreviewStore } from '../store/previewStore'

interface TextEditorProps {
  name: string
  language?: string
}

const handleChange = (text: string) => usePreviewStore.getState().setDraft(text)

const handleSave = () => savePreview()

export function TextEditor({ name, language }: TextEditorProps) {
  const hostRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const parent = hostRef.current
    if (!parent) {
      return
    }
    const view = new EditorView({
      parent,
      state: EditorState.create({ doc: usePreviewStore.getState().edit?.draft ?? '', extensions: [editorExtensions(language, { onChange: handleChange, onSave: handleSave }), EditorView.contentAttributes.of({ 'aria-label': `Édition de ${name}` })] }),
    })
    view.focus()
    return () => view.destroy()
  }, [name, language])

  return <div ref={hostRef} className="min-h-0 flex-1 overflow-hidden" />
}
