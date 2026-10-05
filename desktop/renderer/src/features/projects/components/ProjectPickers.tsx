import { useShallow } from 'zustand/react/shallow'
import { closeProjectPicker, selectProject } from '../projectOpenActions'
import { useHostStore } from '../../../stores/hostStore'
import { useProjectOpenStore } from '../projectOpenStore'
import { ProjectPicker } from './ProjectPicker'
import { ProjectRepositoryPicker } from './ProjectRepositoryPicker'

export function ProjectPickers() {
  const choice = useProjectOpenStore((state) => state.choice)
  const { projects, projectsRoot, projectsError } = useHostStore(useShallow((state) => ({ projects: state.projects, projectsRoot: state.projectsRoot, projectsError: state.projectsError })))

  if (choice?.sources) {
    return <ProjectRepositoryPicker choice={choice} repositories={choice.sources.repositories} defaultRepository={choice.sources.defaultRepository} />
  }
  return <ProjectPicker projects={projects} root={projectsRoot} error={projectsError} onClose={closeProjectPicker} onSelect={selectProject} />
}
