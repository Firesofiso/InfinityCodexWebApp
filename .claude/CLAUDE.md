# Claude Code Guidelines

## Codebase Navigation

Before searching for files with grep or glob, read **`docs/file-map.md`** in the repo root.
It lists every controller, model, service, Angular component, route, guard, and import script
with the file path, route prefix / URL, and a one-line description of what each does.

---

## Core Philosophy

Write code for the next person who has to read it — not for the machine that runs it.
Prefer simple and boring solutions. If a clever approach and a straightforward approach
both solve the problem, choose the straightforward one every time.

---

## Clean Code

- Each function or method does one thing and does it well.
- Name variables, functions, and classes after what they represent, not how they work.
- Avoid abbreviations unless they are universally understood (e.g. `id`, `url`, `err`).
- Delete dead code — don't comment it out and leave it behind.
- Keep functions short. If a function needs a comment to explain what a section does,
  that section probably belongs in its own named function.
- Avoid deep nesting. Use early returns to flatten conditional logic.

```js
// ✗ Avoid
function process(data) {
  if (data) {
    if (data.items) {
      return data.items.map(i => i.value * 2);
    }
  }
}

// ✓ Prefer
function processItems(data) {
  if (!data || !data.items) {
    return [];
  }

  return data.items.map(item => item.value * 2);
}
```

---

## Concise Code

- Don't repeat yourself, but don't over-abstract either. Wait until you have three
  instances of duplication before extracting a shared abstraction.
- Remove code that doesn't carry its weight. Every line is a line someone has to read.
- Prefer standard library functions over hand-rolled equivalents when they're clear
  and well-known.
- Keep files focused. A file that does too many things should be split up.

---

## Avoid One-Liners

- Avoid collapsing logic into a single dense expression just because the language allows it.
- Intermediate variables with good names make the intent clear and the output debuggable.
- Chained method calls are fine when each step is obvious; break the chain when it isn't.

```js
// ✗ Avoid
const result = data.filter(x => x.active && x.score > 10).map(x => ({ ...x, rank: x.score / total * 100 })).sort((a, b) => b.rank - a.rank);

// ✓ Prefer
const activeHighScorers = data.filter(item => item.active && item.score > 10);

const ranked = activeHighScorers.map(item => ({
  ...item,
  rank: (item.score / total) * 100,
}));

const sortedByRank = ranked.sort((a, b) => b.rank - a.rank);
```

---

## Readable Code

- Write code that reads like a sequence of clear decisions, not a puzzle to decode.
- Prefer explicit over implicit. A little verbosity is fine if it aids understanding.
- Use comments to explain **why**, not **what**. The code already says what it does.
- Group related logic together and leave a blank line between conceptually separate steps.
- Booleans and conditions should read naturally.

```python
# ✗ Avoid
if not not user and user.status != 2 and not user.archived:

# ✓ Prefer
is_active_user = user is not None and user.status == STATUS_ACTIVE and not user.archived

if is_active_user:
```

---

## Git Diffs That Humans Can Read

The goal is a diff where a reviewer can follow what changed and why without holding
the entire codebase in their head.

- **One concern per commit.** Don't mix a refactor with a bug fix with a new feature.
- **Write useful commit messages.** The subject line states what changed. The body
  explains why if it isn't obvious.
- **Don't reformat code you aren't changing.** Mixing style fixes with logic changes
  buries the real diff.
- **Avoid moving and modifying in the same commit.** If a file needs to be moved and
  its contents need to change, do it in two commits.
- **Keep pull requests small.** A PR that touches 20 files is hard to review well.
  Break large changes into a sequence of smaller, independently reviewable steps.
- **Prefer additive changes.** Add the new thing, migrate call sites, then remove the
  old thing — rather than doing all three at once.

---

## Simple and Boring Solutions

- Reach for the well-understood tool before the clever one.
- Don't introduce a new dependency when the standard library is sufficient.
- Avoid premature optimization. Solve for correctness first; profile before optimizing.
- When in doubt, write the naive solution and ship it. Complexity can be added later
  when there is evidence it is actually needed.
- Prefer established patterns from the existing codebase over importing new patterns.
- If you find yourself writing an elaborate system, stop and ask: what is the simplest
  thing that would actually work here?

---

## Checklist Before Submitting Code

- [ ] Could a teammate understand this without asking me to explain it?
- [ ] Is every name honest about what the thing actually is or does?
- [ ] Is there any logic that could be replaced with a named variable or function?
- [ ] Is there any code that exists just in case — and could be deleted?
- [ ] Does the diff show only the change it claims to show?
- [ ] Is there a simpler way to accomplish the same thing?