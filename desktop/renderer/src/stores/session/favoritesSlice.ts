import { mutateSession } from './helpers'
import type { SessionSlice, FavoritesSlice } from './types'

export const createFavoritesSlice: SessionSlice<FavoritesSlice> = (set) => ({
  toggleFavorite: (commandId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        draft.favorites = draft.favorites.includes(commandId) ? draft.favorites.filter((candidate) => candidate !== commandId) : [...draft.favorites, commandId]
      }),
    })),

  setFavorites: (commandIds) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        draft.favorites = commandIds
      }),
    })),
})
