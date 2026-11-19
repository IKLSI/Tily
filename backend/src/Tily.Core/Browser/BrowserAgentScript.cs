namespace Tily.Core.Browser;

public static class BrowserAgentScript
{
    public const int MaxElements = 300;
    public const int MaxNameChars = 80;

    private const string Install = """
        (() => {
          if (window.__tilyAgent) {
            return;
          }
          const refs = new Map();
          let counter = 0;
          const selector = 'a[href],button,input:not([type=hidden]),select,textarea,summary,[role],[tabindex],[contenteditable=""],[contenteditable="true"],[onclick]';
          const clip = (text) => {
            const flat = (text || '').replace(/\s+/g, ' ').trim();
            return flat.length > __NAME__ ? flat.slice(0, __NAME__) + '…' : flat;
          };
          const visible = (element) => {
            const rect = element.getBoundingClientRect();
            if (rect.width <= 0 || rect.height <= 0) {
              return false;
            }
            const style = getComputedStyle(element);
            return style.visibility !== 'hidden' && style.display !== 'none';
          };
          const roleOf = (element) => {
            const explicit = element.getAttribute('role');
            if (explicit) {
              return explicit;
            }
            const tag = element.tagName.toLowerCase();
            if (tag === 'a') {
              return 'link';
            }
            if (tag === 'button' || tag === 'summary') {
              return 'button';
            }
            if (tag === 'select') {
              return 'combobox';
            }
            if (tag === 'textarea' || element.isContentEditable) {
              return 'textbox';
            }
            if (tag === 'input') {
              const type = (element.type || 'text').toLowerCase();
              if (type === 'checkbox' || type === 'radio') {
                return type;
              }
              if (type === 'button' || type === 'submit' || type === 'reset' || type === 'image') {
                return 'button';
              }
              return type === 'range' ? 'slider' : 'textbox';
            }
            return 'generic';
          };
          const nameOf = (element) => {
            const label = element.getAttribute('aria-label');
            if (label) {
              return clip(label);
            }
            const by = element.getAttribute('aria-labelledby');
            if (by) {
              const text = by.split(/\s+/).map((id) => document.getElementById(id)?.innerText || '').join(' ');
              if (clip(text)) {
                return clip(text);
              }
            }
            const labels = element.labels && element.labels.length ? Array.from(element.labels).map((item) => item.innerText).join(' ') : '';
            if (clip(labels)) {
              return clip(labels);
            }
            const tag = element.tagName.toLowerCase();
            if (tag === 'input' && ['button', 'submit', 'reset'].includes((element.type || '').toLowerCase()) && element.value) {
              return clip(element.value);
            }
            const text = clip(element.innerText || element.textContent);
            return text || clip(element.getAttribute('placeholder') || element.getAttribute('alt') || element.getAttribute('title') || element.getAttribute('name') || '');
          };
          const collect = (root, found) => {
            for (const element of root.querySelectorAll('*')) {
              if (element.matches(selector)) {
                found.push(element);
              }
              if (element.shadowRoot) {
                collect(element.shadowRoot, found);
              }
            }
            return found;
          };
          const describe = (element) => {
            const parts = [];
            const tag = element.tagName.toLowerCase();
            const type = (element.type || '').toLowerCase();
            if (element.disabled || element.getAttribute('aria-disabled') === 'true') {
              parts.push('désactivé');
            }
            if (type === 'checkbox' || type === 'radio' || element.hasAttribute('aria-checked')) {
              parts.push((element.checked || element.getAttribute('aria-checked') === 'true') ? 'coché' : 'non coché');
            }
            if (element.hasAttribute('aria-expanded')) {
              parts.push(element.getAttribute('aria-expanded') === 'true' ? 'déplié' : 'replié');
            }
            if (tag === 'select') {
              parts.push('valeur=' + JSON.stringify(clip(element.selectedOptions[0]?.text || '')));
            } else if (type === 'password' && element.value) {
              parts.push('valeur=' + JSON.stringify('•'.repeat(Math.min(element.value.length, 12))));
            } else if ((tag === 'input' || tag === 'textarea') && type !== 'checkbox' && type !== 'radio' && element.value) {
              parts.push('valeur=' + JSON.stringify(clip(element.value)));
            }
            if (tag === 'input' && type && !['text', 'button', 'submit', 'checkbox', 'radio'].includes(type)) {
              parts.push('type=' + type);
            }
            if (tag === 'a' && element.getAttribute('href')) {
              parts.push('href=' + JSON.stringify(clip(element.getAttribute('href'))));
            }
            return parts;
          };
          const resolve = (ref) => {
            const element = refs.get(ref)?.deref();
            if (!element || !element.isConnected) {
              throw new Error('Référence ' + ref + ' inconnue ou périmée : refaites un snapshot (tily_browser_snapshot).');
            }
            return element;
          };
          const deepElementAt = (x, y) => {
            let hit = document.elementFromPoint(x, y);
            while (hit && hit.shadowRoot) {
              const inner = hit.shadowRoot.elementFromPoint(x, y);
              if (!inner || inner === hit) {
                break;
              }
              hit = inner;
            }
            return hit;
          };
          window.__tilyAgent = {
            snapshot() {
              refs.clear();
              const lines = [];
              let shown = 0;
              let total = 0;
              for (const element of collect(document, [])) {
                if (!visible(element)) {
                  continue;
                }
                total++;
                if (shown >= __MAX__) {
                  continue;
                }
                shown++;
                const ref = 'e' + (++counter);
                refs.set(ref, new WeakRef(element));
                const name = nameOf(element);
                const extra = describe(element);
                lines.push(ref + ' ' + roleOf(element) + (name ? ' ' + JSON.stringify(name) : '') + (extra.length ? ' ' + extra.join(' ') : ''));
              }
              return { url: location.href, title: document.title, count: shown, total, truncated: total > shown, text: lines.join('\n') };
            },
            target(ref) {
              const element = resolve(ref);
              if (element.disabled || element.getAttribute('aria-disabled') === 'true') {
                throw new Error('L’élément ' + ref + ' est désactivé.');
              }
              element.scrollIntoView({ block: 'center', inline: 'center' });
              const rect = element.getBoundingClientRect();
              const x = Math.round(rect.left + rect.width / 2);
              const y = Math.round(rect.top + rect.height / 2);
              if (rect.width <= 0 || rect.height <= 0 || x < 0 || y < 0 || x > innerWidth || y > innerHeight) {
                throw new Error('L’élément ' + ref + ' n’est pas visible dans la page.');
              }
              const hit = deepElementAt(x, y);
              if (hit && hit !== element && !element.contains(hit) && !hit.contains(element)) {
                throw new Error('L’élément ' + ref + ' est masqué par <' + hit.tagName.toLowerCase() + '> ' + JSON.stringify(nameOf(hit)) + '.');
              }
              return { x, y, name: nameOf(element), role: roleOf(element) };
            },
            focus(ref, clear) {
              const element = resolve(ref);
              const tag = element.tagName.toLowerCase();
              if (!(tag === 'input' || tag === 'textarea' || element.isContentEditable)) {
                throw new Error('L’élément ' + ref + ' n’est pas un champ de saisie.');
              }
              if (element.disabled || element.readOnly) {
                throw new Error('Le champ ' + ref + ' est désactivé ou en lecture seule.');
              }
              element.scrollIntoView({ block: 'center', inline: 'center' });
              element.focus();
              if (clear) {
                if (typeof element.select === 'function') {
                  element.select();
                } else {
                  const range = document.createRange();
                  range.selectNodeContents(element);
                  const selection = getSelection();
                  selection.removeAllRanges();
                  selection.addRange(range);
                }
              }
              return { name: nameOf(element), role: roleOf(element) };
            },
            select(ref, wanted) {
              const element = resolve(ref);
              if (element.tagName.toLowerCase() !== 'select') {
                throw new Error('L’élément ' + ref + ' n’est pas une liste déroulante native : utilisez tily_browser_click.');
              }
              const options = Array.from(element.options);
              const match = options.find((option) => option.value === wanted) || options.find((option) => clip(option.text) === clip(wanted));
              if (!match) {
                throw new Error('Option « ' + wanted + ' » introuvable. Options : ' + options.map((option) => JSON.stringify(clip(option.text))).join(', ') + '.');
              }
              element.value = match.value;
              element.dispatchEvent(new Event('input', { bubbles: true }));
              element.dispatchEvent(new Event('change', { bubbles: true }));
              return { selected: clip(match.text), value: match.value };
            },
            check(condition) {
              if (condition.url) {
                return location.href.includes(condition.url);
              }
              if (condition.text) {
                return (document.body?.innerText || '').includes(condition.text);
              }
              const element = refs.get(condition.ref)?.deref();
              return !!element && element.isConnected && visible(element);
            }
          };
        })();
        """;

    public static string Prepare(string call) =>
        Install.Replace("__MAX__", MaxElements.ToString(), StringComparison.Ordinal).Replace("__NAME__", MaxNameChars.ToString(), StringComparison.Ordinal)
        + "\n" + call;
}
