# Installation

# Exiled setup

## 1. Download and Setup :moyai:

### *You need to download the latest release:*

- [``AutoEvent-Exiled.dll``](https://github.com/RisottoMan/AutoEvent/releases/latest) move to => ``EXILED/Plugins``

- [
  ``ProjectMER.dll``](https://github.com/RisottoMan/AutoEvent/tree/beta14.1-mer/releases/download/v.9.11.1/MapEditorReborn.dll)
  move to => ``EXILED/Plugins``

- [``Music.tar.gz``](https://github.com/RisottoMan/AutoEvent/releases/latest) unzip files to =>
  ``EXILED/Configs/AutoEvent/Music``

- [``Schematics.tar.gz``](https://github.com/RisottoMan/AutoEvent/releases/latest) unzip files to =>
  ``EXILED/Configs/AutoEvent/Schematics``

- Ensure that the Config has the following directories, and that they can be accessed to the server.

```yml
# Where the schematics directory is located. By default it is located in the AutoEvent folder.
schematics_directory_path: /home/container/.config/EXILED/Configs/AutoEvent/Schematics
# Where the music directory is located. By default it is located in the AutoEvent folder.
music_directory_path: /home/container/.config/EXILED/Configs/AutoEvent/Music
```

- ***Sometimes these settings fail to auto-generate in the config properly, so please double check they are valid before
  reaching out to us.***

## 2. Permission :gem:

### *Give permission to your role in ``EXILED/Configs/permissions.yml``:*

```
owner:
  inheritance: [ ]
  permissions:
    - ev.*
```

Available Permission Tress:

```
ev.*           - Main Permission for all AutoEvent commands.
  - ev.list    - View the available events.
  - ev.run      - Run an event.
  - ev.stop     - Stop an event.
  - ev.volume   - Change the volume of all events.
  - ev.language - Change language for translations.
```

# LabApi setup

## 1. Download and Setup :moyai:

### *You need to download the latest release:*

- [``AutoEvent-LabApi.dll``](https://github.com/RisottoMan/AutoEvent/releases/latest) move to => ``LabApi/plugins/global/``

- [
  ``ProjectMER.dll``](https://github.com/RisottoMan/AutoEvent/tree/beta14.1-mer/releases/download/v.9.11.1/MapEditorReborn.dll)
  move to => ``LabApi/plugins/global/``

- [``Music.tar.gz``](https://github.com/RisottoMan/AutoEvent/releases/latest) unzip files to =>
  ``LabApi/configs/AutoEvent/Music``

- [``Schematics.tar.gz``](https://github.com/RisottoMan/AutoEvent/releases/latest) unzip files to =>
  ``LabApi/configs/AutoEvent/Schematics``

- Ensure that the Config has the following directories, and that they can be accessed to the server.

```yml
# Where the schematics directory is located. By default it is located in the AutoEvent folder.
schematics_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Schematics
# Where the music directory is located. By default it is located in the AutoEvent folder.
music_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Music
```

- ***Sometimes these settings fail to auto-generate in the config properly, so please double check they are valid before
  reaching out to us.***

## 2. Permission :gem:

### *Give permission to your role in ``LabApi/configs/permissions.yml``:*

```
owner:
  inheritance: [ ]
  permissions:
    - ev.*
```

Available Permission Tress:

```
ev.*           - Main Permission for all AutoEvent commands.
  - ev.list    - View the available events.
  - ev.run      - Run an event.
  - ev.stop     - Stop an event.
  - ev.volume   - Change the volume of all events.
  - ev.language - Change language for translations.
```
