namespace Tily.Core.Shell;

public static class BashIntegrationScript
{
    public const string Content = """
        if [ -r /etc/profile ]; then
          . /etc/profile
        fi
        if [ -r "$HOME/.bash_profile" ]; then
          . "$HOME/.bash_profile"
        elif [ -r "$HOME/.bash_login" ]; then
          . "$HOME/.bash_login"
        elif [ -r "$HOME/.profile" ]; then
          . "$HOME/.profile"
        fi

        __tily_started=$SECONDS
        __tily_armed=0
        __tily_ran=0

        __tily_report_directory() {
          local url_path="" i ch hexch LC_CTYPE=C LC_ALL=
          for ((i = 0; i < ${#PWD}; ++i)); do
            ch="${PWD:i:1}"
            if [[ "$ch" =~ [/._~A-Za-z0-9-] ]]; then
              url_path+="$ch"
            else
              printf -v hexch "%02X" "'$ch"
              url_path+="%${hexch: -2:2}"
            fi
          done
          printf '\e]7;file://%s\e\\' "$url_path"
        }

        __tily_prompt_command() {
          local tily_status=$? tily_command
          __tily_report_directory
          if [ "$__tily_ran" = 1 ]; then
            __tily_ran=0
            tily_command=$(HISTTIMEFORMAT= builtin history 1)
            tily_command="${tily_command#*[0-9]  }"
            printf '\e]6973;done;%d;%d;%s\e\\' $(( (SECONDS - __tily_started) * 1000 )) $(( tily_status == 0 )) "$(printf '%s' "$tily_command" | /usr/bin/base64 | /usr/bin/tr -d '\n')"
          fi
          return $tily_status
        }

        __tily_arm() {
          __tily_started=$SECONDS
          __tily_armed=1
        }

        __tily_debug() {
          if [ "$__tily_armed" = 1 ]; then
            __tily_armed=0
            __tily_ran=1
            __tily_started=$SECONDS
          fi
        }

        if [ -z "$(trap -p DEBUG)" ]; then
          trap '__tily_debug' DEBUG
        fi
        PROMPT_COMMAND="__tily_prompt_command${PROMPT_COMMAND:+; $PROMPT_COMMAND}; __tily_arm"
        """;
}
