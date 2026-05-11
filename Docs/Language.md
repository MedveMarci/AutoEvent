# Language & Translations

AutoEvent supports multiple languages. The language is loaded from the server's locale by default, but can be changed
manually.

---

## Available Languages

| Language   | Contributors            |
|------------|-------------------------|
| English    | SnivyFilms, RedForce    |
| Russian    | RisottoMan              |
| Polish     | Vretu, Tksemdem         |
| French     | Robocnop, Antoniofo     |
| Hungarian  | Öcsi, MedveMarci        |
| Italian    | NotZer0Two              |
| German     | SeekEDstroy             |
| Thai       | karorogunso             |
| Chinese    | kldhsh123               |
| Portuguese | FireThing               |
| Turkish    | zurna_sever_58, Rooster |
| Spanish    | EnderZ024, ZrNoxb       |

---

## How to Change Language

1. List available translations:
   ```
   ev language list
   ```

2. Load a translation:
   ```
   ev language load english
   ev language load russian
   ev language load polish
   ```

3. Restart the server for the change to take full effect.

The loaded translation replaces `translation.yml` in the AutoEvent config folder.

---

## Adding a New Language

If your language is not listed, you can create a translation and contribute it to the repository:

1. Copy an existing translation file from `AutoEvent/Translations/`
2. Translate all string values
3. Submit a pull request to the [repository](https://github.com/MedveMarci/AutoEvent)

---

## Fixing a Translation Error

1. Find the translation file in `AutoEvent/Translations/`
2. Correct the relevant line
3. Submit a pull request with the fix — it will be included in the next release
