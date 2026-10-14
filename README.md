<div align="center">

# 📚 English Vocabulary Memorization

### See the word. Recall the meaning. Get the spelling right.

A focused Windows vocabulary companion for CET-6 preparation.<br>
Study with context, practise by dictation, and return to the words that need another try.

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?style=flat-square)](#quick-start)
[![C%23](https://img.shields.io/badge/C%23-WPF-512BD4?style=flat-square)](#for-developers)
[![Runtime](https://img.shields.io/badge/.NET%20Framework-4.7.2%2B-512BD4?style=flat-square)](#quick-start)
[![License](https://img.shields.io/badge/Code-MIT-22C55E?style=flat-square)](LICENSE.txt)

**English · [简体中文](README_zh-CN.md)**

[Get started](#quick-start) · [Explore features](#what-you-can-do) · [Import your words](#bring-your-own-vocabulary) · [Build from source](#for-developers)

</div>

---

**V2.0 update:** 2,955 formal CET-6 entries with US IPA and bilingual examples; 20-word pages, English search and three notebook entries. Existing V1.7 profiles are backed up and merged automatically. Read the [upgrade and rollback guide](docs/V2.0升级与回退.md), [quality report](docs/V2.0词库质量报告.md) and [acceptance results](docs/V2.0验收报告.md). Code remains MIT; the new wordbook content is CC BY-SA 4.0 with a separate CMU pronunciation notice.

## Make each study session count

Recognising a word is a start. Recalling its meaning and spelling it from a prompt give you something concrete to practise.

**English Vocabulary Memorization**, also named **CET6Word** in the source, brings those steps into one desktop workflow: reveal a definition when you need it, read an example, listen to the word, then test yourself. Incorrect answers become a review list you can work through later.

Your vocabulary, notebooks and learning records stay on your computer. Everyday study requires no account or server; local pronunciation uses an installed Windows American English voice.

## What you can do

| Feature | What it gives you |
| --- | --- |
| 🧭 Pages and English search | Default 20 words per page; page jumps, arrow keys and a read-only search panel across the full formal wordbook. |
| 📚 Three book entries | Original wordbook, favourites and dynamically recited words (current mastered status or last reliable correct dictation). |
| 📖 Progressive reveal | Start with English only. Click to reveal the Chinese meaning, click again for the example, then collapse it. |
| 🔊 Word and sentence audio | Play either independently; optionally enable automatic reading when you actively reveal a word. |
| ✍️ Two dictation modes | Type the English spelling from a Chinese meaning or an audio prompt, with immediate feedback. |
| 🎯 Focused mistake review | Retry the current round's mistakes or revisit saved incorrect words. The default review threshold is two consecutive correct answers and can be adjusted. |
| ⭐ Personal notebooks | Use one-click favourites and create multiple notebooks for your own topics or study goals. |
| ✅ Mastered-word controls | Remove familiar words from the study list and restore them whenever you want. |
| 🔀 Pick up where you left off | Switch between fixed and random order; the app saves your order and reading position. |
| 📥 Your own vocabulary | Import CSV or XLSX with a preview of valid entries, duplicates, invalid rows and missing examples. |
| 💾 Persistent study records | Automatically save progress, resume unfinished dictation rounds, and export a complete backup for another computer. |

### A study loop you control

1. **Recall first.** Browse the English-only list and try to remember the meaning.
2. **Reveal with purpose.** Open the meaning and example; listen to the word or sentence.
3. **Practise the spelling.** Choose recently studied words, a random selection, or a notebook for dictation.
4. **Return to mistakes.** Review incorrect words and track consecutive correct answers.

“Recently studied” records words whose meanings or examples you actively reveal. Scrolling, favouriting, audio playback and the global translation switch do not count as active study.

## Quick start

### Run the included desktop app

**Requirements:** Windows 10/11 and .NET Framework 4.7.2 or later. For local pronunciation, install an **English (United States)** speech voice in Windows if one is unavailable.

1. Use the **CET6Word-V2.0-Windows.zip** portable package delivered with this version. A source package needs rebuilding first.
2. Extract the ZIP into a folder.
3. Double-click **`六级词汇.exe`**. Keep `六级词汇.exe.config` beside it.

You do not need Python, Node.js, Microsoft Excel or a .NET SDK to run the included app. The current application interface is in Simplified Chinese; this README provides English guidance.

### Your first session

- Click a word's main text to reveal its meaning and example.
- Use **发音** for the word and **朗读例句** for its example sentence.
- Click **☆ / ★** to manage favourites, or **熟词** to mark a word as mastered.
- Open **默写** to choose your word range, question count and Chinese or listening prompt.
- Find mastered words, mistakes, vocabulary imports and settings under **更多**.

Automatic reading is off by default. Dictation ignores letter case and leading/trailing whitespace, but requires the correct spelling; it does not accept approximate spellings or alternative word forms.

## Bring your own vocabulary

The formal collection contains **2,955 unique CET-6 entries** with part of speech, US IPA, Chinese definitions and bilingual examples. It draws primarily on the licensed OpenEtymology CET6 wordbook. It is not an official exhaustive exam syllabus. An upgrade also retains 47 unmatched older entries and their records outside the formal count.

To study your own material, open **更多 → 词库管理 → 导入 CSV / XLSX**.

```csv
word,meaning,example,example_translation
adapt,适应,We adapt to new situations.,我们适应新的情况。
```

| Column | Required | Content |
| --- | --- | --- |
| `word` | Yes | English word |
| `meaning` | Yes | Chinese meaning |
| `example` | No | English example sentence |
| `example_translation` | No | Chinese translation of the example |

Chinese headers such as `单词`, `释义`, `例句` and `例句翻译` are also supported.

- **CSV:** UTF-8, including BOM, or GB18030.
- **XLSX:** the first worksheet; Microsoft Excel does not need to be installed. Convert older `.xls` files to `.xlsx` or `.csv` first.
- **Duplicates:** compared without case sensitivity; the first valid entry is retained.
- **Separate progress:** each import creates a new vocabulary collection. Identical spellings in different collections keep independent learning records.
- **Limits:** 50 MB per import file and 100,000 entries across all collections.

Start with the [CSV template](data/词库导入模板.csv), or explore the [included vocabulary CSV](data/六级原词书.csv). Import material you have permission to use. Details about the data are in the [vocabulary and licensing notes](docs/词库与授权说明.md).

## Pronunciation, offline use and optional Azure Speech

Word and example playback use **American English at normal speed**. Without an online service, the app uses Windows `en-US` speech and caches the generated audio for reuse.

If you have an Azure Speech resource, enter your **region and service key** in **更多 → 设置与数据**. The implemented adapter uses `en-US-JennyNeural`, caches successful online audio, and falls back to Windows speech if the online request fails.

Online speech requires your own valid credentials and may incur provider charges. The repository's acceptance report documents adapter and fallback tests; it does not claim a live Azure account was verified. Missing local English speech voices can prevent playback until a suitable voice is installed.

## Your learning data stays with you

The default data folder is **`%LOCALAPPDATA%\CET6Word`**. Learning records are stored separately from the executable, so moving or replacing the application folder does not remove them.

Use **更多 → 设置与数据** to export or restore a **`.cet6backup`** file.

- **Included:** imported vocabulary, order and position, study records, mastered words, notebooks, dictation sessions and answers, mistake history, and preferences.
- **Excluded:** regenerable audio caches and the online service key. Configure the key again on a new computer.
- **Restore behaviour:** the backup replaces the current profile after validation; the app keeps a copy of the profile from before restoration.

The app also maintains a previous saved profile for recovery and prevents simultaneous writers to one data folder. Export regular backups when you want an independent copy or need to move computers.

Azure keys are protected with Windows DPAPI for the current Windows user. Online speech sends the English text requested for playback, rather than uploading the complete learning profile.

## For developers

The application uses **C# + WPF + .NET Framework**, with `System.Speech` for local audio, JSON for persistence, and ZIP/XML handling for XLSX imports. The build script compiles with the Roslyn compiler supplied by an installed .NET SDK.

Rebuilding requires a Windows development environment with a **.NET SDK** and the **.NET Framework/WPF assemblies** used by `build.ps1`. Run the following from PowerShell in the repository root:

```powershell
git clone https://github.com/klsdxf/English-Vocabulary-Memorization.git
cd English-Vocabulary-Memorization

# Build the desktop executable
.\build.ps1

# Run the project's automated tests
.\build.ps1 -Test

# Build and package portable/source archives into a chosen folder
.\package.ps1 -Destination .\dist
```

For a separate study or test profile:

```powershell
& '.\六级词汇.exe' --data-dir 'C:\CET6Word-Profile'
```

| Location | Purpose |
| --- | --- |
| `src/Core.cs` | Study state, notebooks, dictation and mistake-review rules |
| `src/UI.cs` / `src/MainWindow.xaml` | WPF interface and user interactions |
| `src/Storage.cs` | Persistence, backup validation and encrypted key storage |
| `src/Imports.cs` | CSV/XLSX vocabulary import |
| `src/Speech.cs` | Windows speech, optional Azure adapter and audio caching |
| `data/` | Original starter vocabulary and import template |
| `tests/` | Learning, storage, import, speech and UI tests |
| `docs/` | Architecture, acceptance evidence and data licensing |

## Current scope

The implemented review system tracks mistakes and consecutive correct answers. Scheduled spaced repetition (SRS), AI tutoring, cloud sync and mobile/web clients are not implemented. The documentation describes the existing Windows app rather than promising those features.

For design choices and validation boundaries, read the [architecture notes](docs/V2.0实施清单.md) and [acceptance report](docs/V2.0验收报告.md), currently in Chinese.

## Contribute and support

Have a reproducible bug, an import edge case, or a way to make the study flow smoother? [Open an issue](https://github.com/klsdxf/English-Vocabulary-Memorization/issues) with the steps, expected behaviour and Windows/runtime details. Pull requests are welcome; run the project tests when changing application behaviour.

If this app helps you build a vocabulary habit, a **⭐ Star** helps others discover it.

## License

The project source code and its original vocabulary data are covered by the [MIT License](LICENSE.txt). Imported third-party vocabulary, Windows/.NET components and speech services remain subject to their own rights and terms.
