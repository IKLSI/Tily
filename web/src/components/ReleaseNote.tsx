interface ReleaseNoteProps {
  text: string
}

const CODE_DELIMITER = '`'

export function ReleaseNote({ text }: ReleaseNoteProps) {
  return (
    <>
      {text.split(CODE_DELIMITER).map((part, index) =>
        index % 2 === 1 ? (
          <code key={index} className="rounded bg-dock-paper px-1 font-mono text-[11px] text-dock-green-deep">
            {part}
          </code>
        ) : (
          <span key={index}>{part}</span>
        ),
      )}
    </>
  )
}
