# 📚 English Vocabulary Memorization

### A simple vocabulary tool that helps you focus on remembering words.

A minimalist Windows vocabulary companion designed for CET-6 learners.

No complicated learning systems.\
No unnecessary tasks.\
Just read, recall, and verify.

[English](README.md) \| [简体中文](README_zh-CN.md)

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?style=flat-square)](#quick-start)
[![C#](https://img.shields.io/badge/C%23-WPF-512BD4?style=flat-square)](#for-developers)
[![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2%2B-512BD4?style=flat-square)](#quick-start)
[![License](https://img.shields.io/badge/Code-MIT-22C55E?style=flat-square)](LICENSE.txt)

[Get started](#quick-start) · [Explore features](#what-you-can-do) ·
[For developers](#for-developers)
:::

------------------------------------------------------------------------

## V2.0 Update

Version 2.0 introduces:

-   2,955 formal CET-6 vocabulary entries
-   US IPA pronunciation
-   Bilingual example sentences
-   20-word learning pages
-   English search
-   Three notebook entries
-   Improved data migration and backup support

Existing V1.7 profiles are backed up and migrated automatically.

------------------------------------------------------------------------

# Why This Project Exists

Most vocabulary applications keep adding more and more features:

-   complicated learning plans
-   multiple learning modes
-   achievement systems
-   forced review processes
-   endless settings

But vocabulary learning itself is already difficult.

The learning tool should not become another thing users need to learn.

English Vocabulary Memorization follows one principle:

> A vocabulary app should help you remember words, not manage your
> learning process.

------------------------------------------------------------------------

# Design Philosophy

## Minimalism: Return to the Core Task

The purpose of a vocabulary application is simple:

**Remember words.**

This project intentionally avoids unnecessary complexity.

Instead of complicated workflows, it focuses on two essential
experiences.

------------------------------------------------------------------------

# 📖 Memorization Page

A focused environment for learning vocabulary.

You can:

-   review words
-   understand meanings
-   read examples
-   listen to pronunciation

No distractions.

Just learning.

------------------------------------------------------------------------

# ✍️ Dictation Page

Recognition is not mastery.

A word that looks familiar is not necessarily a word you truly remember.

The dictation page provides the final verification:

> Can you actually recall and spell this word?

Only words that pass dictation verification are considered mastered.

------------------------------------------------------------------------

# Giving Control Back to Learners

Many vocabulary applications decide everything:

-   when a word is learned
-   how many reviews are required
-   when a word can be marked mastered

However, learning is personal.

Different learners have different memories and study habits.

This project gives control back to the learner:

> You decide when you understand a word.\
> The dictation system verifies whether you truly remember it.

Freedom during learning.\
Reliability during testing.

------------------------------------------------------------------------

# What You Can Do

  Feature               Description
  --------------------- ---------------------------------------------
  Vocabulary Learning   Focused CET-6 vocabulary learning workflow
  Progressive Reveal    Learn meaning and examples step by step
  Pronunciation         Word and sentence audio playback
  Dictation             Chinese-to-English and audio spelling tests
  Mistake Review        Review words that need another try
  Personal Notebooks    Manage favourites and custom collections
  Progress Tracking     Save learning records locally
  Import Vocabulary     Import your own CSV/XLSX word lists
  Backup                Export and restore learning data

------------------------------------------------------------------------

# A Study Loop You Control

1.  **Recall first**\
    Try to remember the meaning before revealing the answer.

2.  **Learn with context**\
    Read definitions, examples and pronunciation.

3.  **Verify by recall**\
    Use dictation to test whether you truly remember.

4.  **Return to mistakes**\
    Review words that still need practice.

------------------------------------------------------------------------

# Quick Start

## Requirements

-   Windows 10/11
-   .NET Framework 4.7.2 or later

For pronunciation:

Install an English (United States) Windows speech voice if unavailable.

## Run

1.  Download the portable package.
2.  Extract the ZIP file.
3.  Run:

```{=html}
<!-- -->
```
    六级词汇.exe

No Python, Node.js or development tools are required.

------------------------------------------------------------------------

# Bring Your Own Vocabulary

You can import your own vocabulary through:

    更多 → 词库管理 → 导入 CSV / XLSX

Example:

``` csv
word,meaning,example,example_translation
adapt,适应,We adapt to new situations.,我们适应新的情况。
```

Supported fields:

-   word
-   meaning
-   example
-   example_translation

------------------------------------------------------------------------

# Your Data Stays With You

This application is local-first.

Your:

-   vocabulary
-   learning progress
-   notebooks
-   mistakes
-   dictation history

remain on your computer.

No account required.

No server required for daily learning.

------------------------------------------------------------------------

# For Developers

Built with:

-   C#
-   WPF
-   .NET Framework
-   JSON persistence
-   System.Speech
-   CSV/XLSX import support

Build:

``` powershell
git clone https://github.com/klsdxf/English-Vocabulary-Memorization.git

cd English-Vocabulary-Memorization

.\build.ps1
```

------------------------------------------------------------------------

# Current Scope

The current version focuses on:

-   simple vocabulary learning
-   active recall verification
-   personal learning records

The project intentionally avoids unnecessary complexity.

Future improvements will follow one principle:

> Add value, not complexity.

------------------------------------------------------------------------

# Contributing

Suggestions and improvements are welcome.

You can:

-   report bugs
-   suggest ideas
-   submit pull requests

------------------------------------------------------------------------

# Support

If this project matches your learning philosophy:

⭐ Star this repository

Your support helps more learners discover it.

------------------------------------------------------------------------

# License

MIT License
