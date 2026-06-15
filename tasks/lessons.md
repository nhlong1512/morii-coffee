# Lessons

- When a user invokes a Spec Kit phase, execute only that phase. Requirements mentioning later implementation, tests, build verification, or handoff documents belong in the generated artifacts until the corresponding implementation phase is explicitly invoked.
- When the git branch name and `.specify/feature.json` disagree, treat `.specify/feature.json` as the active feature for downstream Spec Kit phases and correct any setup-script output before writing artifacts.
