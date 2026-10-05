import { lazy, Suspense } from 'react'
import { usePreviewStore } from '../previewStore'

const FilePreviewDrawer = lazy(() => import('./FilePreviewDrawer').then((module) => ({ default: module.FilePreviewDrawer })))

export function LazyFilePreview() {
  const open = usePreviewStore((store) => store.path !== null)
  return open ? (
    <Suspense fallback={null}>
      <FilePreviewDrawer />
    </Suspense>
  ) : null
}
