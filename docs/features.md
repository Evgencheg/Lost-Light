# Реестр фич

Все наши и портированные фичи: где лежат, кто отвечает, откуда взялись. Обновляется в том же PR, что и фича. Если владелец ушёл - фича уходит заместителю или получает статус «На поддержке».

Статусы - как в [диздоках](design/README.md#статусы).

| Фича | Где лежит | Диздок | Владелец | Заместитель | Происхождение | Статус |
|---|---|---|---|---|---|---|
| Пример: служебные планшеты СБ | `Content.*/_Corvax/SecApartment/` | [sec-apartments](design/sec-apartments.md) | @ник | @ник | Порт: Corvax, [PR](https://github.com/...) , MIT | Готов |
| Телефоны (дисковые, настенные) | `Content.*/_Trauma/Phones/`, `Content.Shared/_Trauma/PullBlocker/`, `Resources/Prototypes/_Trauma/`, `Resources/Prototypes/_RMC14/` | [phones](design/phones.md) | @Evgencheg | - | Порт: Trauma Station, PR 909 и последующие правки, AGPLv3 | Черновик |

## Правки в файлах Starlight и SS14

Наши изменения в чужих файлах помечаются `// LP edit start` / `// LP edit end` (в YAML - `# LP edit start` / `# LP edit end`; в `.ftl` и `using` пометки не ставятся). Полный список всегда можно получить поиском:

```bash
git grep -n "LP edit start"
```

Крупные правки, влияющие на поведение, описываются в диздоке соответствующей фичи.
