namespace Tily.Core.Shell;

public static class ZshIntegrationScripts
{
    public const string Environment = """
        if [[ -f "$TILY_USER_ZDOTDIR/.zshenv" ]]; then
          TILY_ZDOTDIR=$ZDOTDIR
          ZDOTDIR=$TILY_USER_ZDOTDIR
          . "$TILY_USER_ZDOTDIR/.zshenv"
          TILY_USER_ZDOTDIR=$ZDOTDIR
          ZDOTDIR=$TILY_ZDOTDIR
        fi
        """;

    public const string Profile = """
        if [[ -o login && -f "$TILY_USER_ZDOTDIR/.zprofile" ]]; then
          TILY_ZDOTDIR=$ZDOTDIR
          ZDOTDIR=$TILY_USER_ZDOTDIR
          . "$TILY_USER_ZDOTDIR/.zprofile"
          ZDOTDIR=$TILY_ZDOTDIR
        fi
        """;

    public const string Startup = """
        if [[ "$HISTFILE" == "$ZDOTDIR/.zsh_history" ]]; then
          HISTFILE="$TILY_USER_ZDOTDIR/.zsh_history"
        fi
        TILY_ZDOTDIR=$ZDOTDIR
        ZDOTDIR=$TILY_USER_ZDOTDIR
        if [[ -f "$ZDOTDIR/.zshrc" ]]; then
          . "$ZDOTDIR/.zshrc"
        fi
        . "$TILY_ZDOTDIR/tily-integration.zsh"
        if [[ "$ZDOTDIR" == "$HOME" ]]; then
          unset ZDOTDIR
        fi
        unset TILY_ZDOTDIR TILY_USER_ZDOTDIR
        """;

    public const string Integration = """
        zmodload zsh/datetime 2>/dev/null
        typeset -g __tily_command="" __tily_started=0 __tily_running=0

        __tily_base64() {
          print -rn -- "$1" | /usr/bin/base64 | /usr/bin/tr -d '\n'
        }

        __tily_report_directory() {
          local url_path="" i ch hexch LC_CTYPE=C LC_COLLATE=C LC_ALL= LANG=
          for (( i = 1; i <= ${#PWD}; ++i )); do
            ch="$PWD[i]"
            if [[ "$ch" =~ [/._~A-Za-z0-9-] ]]; then
              url_path+="$ch"
            else
              printf -v hexch "%02X" "'$ch"
              url_path+="%${hexch: -2:2}"
            fi
          done
          printf '\e]7;file://%s\e\\' "$url_path"
        }

        __tily_precmd() {
          local -i tily_status=$?
          __tily_report_directory
          if (( __tily_running )); then
            __tily_running=0
            local -i tily_elapsed=$(( (${EPOCHREALTIME:-0} - __tily_started) * 1000 ))
            printf '\e]6973;done;%d;%d;%s\e\\' $tily_elapsed $(( tily_status == 0 )) "$(__tily_base64 "$__tily_command")"
          fi
          return $tily_status
        }

        __tily_preexec() {
          __tily_command=$1
          __tily_started=${EPOCHREALTIME:-0}
          __tily_running=1
          printf '\e]6973;exec;%s\e\\' "$(__tily_base64 "$1")"
        }

        __tily_prompt_rows() {
          emulate -L zsh
          setopt extendedglob
          local tily_prompt=${(%)PS1} tily_line
          local -i tily_rows=0 tily_width=$(( COLUMNS > 0 ? COLUMNS : 80 ))
          tily_prompt=${tily_prompt//$'\e'\[[0-9;?]#[ -\/]#[@-~]/}
          tily_prompt=${tily_prompt//$'\e'\][^$'\a'$'\e']#($'\a'|$'\e'\\)/}
          for tily_line in "${(@f)tily_prompt}"; do
            tily_rows+=$(( ${#tily_line} > 0 ? (${#tily_line} + tily_width - 1) / tily_width : 1 ))
          done
          (( tily_rows > 0 )) || tily_rows=1
          printf '\e]6973;prompt;%d\e\\' $tily_rows
        }

        typeset -ga precmd_functions preexec_functions
        precmd_functions=(__tily_precmd ${precmd_functions:#__tily_*} __tily_prompt_rows)
        preexec_functions=(${preexec_functions:#__tily_*} __tily_preexec)
        """;
}
