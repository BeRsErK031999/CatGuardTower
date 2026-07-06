# Prompt Workflow

Use this workflow when asking Codex to continue the project.

## Core Rules

- Work phase by phase.
- Before each phase, read `AGENTS.md` and `docs/planning/`.
- Before file changes, write a file-level plan.
- Do not perform several phases in one prompt.
- Do not move to the next phase without explicit owner approval.
- Stop at review gates and ask the owner to send the required report to ChatGPT.
- Do not connect SDKs without a separate task.
- Do not do a large refactor without a clear reason.
- Do not create fake Unity assets or fake Unity project files.

## Before Each Task

Codex should:

- check `git status`;
- identify the current phase;
- read relevant planning docs;
- state what files or systems it expects to touch;
- state what it will not touch.

## During Each Task

Codex should:

- keep changes small;
- preserve existing scope guardrails;
- avoid gameplay work unless the current phase asks for it;
- avoid server, iOS, Firebase, ads, and IAP unless the current phase asks for them;
- update docs when decisions or workflows change.

## After Each Task

Codex should report:

- changed files;
- what was implemented;
- manual test steps;
- validation commands and results;
- risks/TODO;
- commit hash;
- git status.

## Review Gate Behavior

When a phase reaches a review gate, Codex should stop and ask the owner to collect the gate report. The next phase starts only after the owner explicitly asks for it.

## Good Prompt Shape

```text
Работаем только над Phase N.
Сначала прочитай AGENTS.md и docs/planning.
Перед изменениями дай file-level plan.
Сделай только задачи из Phase N.
Не переходи к следующей фазе.
После изменений запусти доступные проверки и сделай commit.
```

## Bad Prompt Shape

```text
Сделай сразу gameplay, аналитику, рекламу, магазин и релиз.
```

This is too broad and should be split into phase-specific prompts.
