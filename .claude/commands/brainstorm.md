# Brainstorm Feature Spec

You are helping the user create and iteratively refine a **new** feature spec for the InfinityCodex web app. This command is designed to be run multiple times — each run incorporates prior answers and deepens the spec.

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

Goal: Turn a rough feature idea into a well-structured spec document by asking targeted questions one at a time and encoding every answer directly into the spec file.

Execution steps:

1. **Resolve the spec file.**
   - Derive a `kebab-case-feature-name` from the user's input or the existing feature topic.
   - Check whether `docs/features/[kebab-case-feature-name].md` already exists.
   - If it exists, read it and treat this as a refinement run — note which sections still have placeholder text or `[NEEDS CLARIFICATION]` markers.
   - If it does not exist, create a first draft now using `docs/templates/FEATURE_SPEC.md` as the structure:
     - Fill in the header fields (feature name, date, status, user description).
     - Write as many User Stories as you can confidently derive from the input, each with priority, plain-language description, independent test description, and at least one Given/When/Then acceptance scenario.
     - Write Functional Requirements using FR-### IDs for everything you can infer. Mark anything unclear with `[NEEDS CLARIFICATION: ...]`.
     - Fill in Key Entities if the feature clearly involves data.
     - Leave Success Criteria and Assumptions sections populated with your best inferences; mark unknowns explicitly.
     - Never invent facts about the project. If unsure, use `[NEEDS CLARIFICATION: ...]`.
   - Save the file before asking any questions.

2. **Scan for gaps.**
   Perform an internal coverage scan across these categories. Mark each: **Clear** / **Partial** / **Missing**.

   - User stories: are the core journeys captured with correct priority order?
   - Acceptance scenarios: are Given/When/Then scenarios specific and independently testable?
   - Role & permission model: which roles can do what?
   - Functional requirements: are there `[NEEDS CLARIFICATION]` markers or vague FRs?
   - Key entities: are the data shapes and relationships clear?
   - Edge cases: are boundary conditions and error scenarios addressed?
   - Success criteria: are SC-### outcomes measurable and concrete?
   - Assumptions: are scope boundaries and dependencies made explicit?

   For each Partial or Missing category, generate a candidate question — but only if the answer would materially change what gets written in the spec.

3. **Build a prioritized question queue (max 5).**
   - Rank by impact: user story priority order and role/permission decisions first, then data shape, then edge cases and success criteria.
   - Skip questions already answered in a previous run.
   - Do not reveal the queue — ask one question at a time.
   - If no meaningful gaps exist, report that and suggest proceeding to `/clarify`.

4. **Sequential questioning loop.**
   Present **exactly one question at a time**. For each:

   - Analyse all realistic options and determine the best one based on: consistency with existing features in this project, risk reduction, and simplicity.
   - State your recommendation prominently before showing options.
   - Format as:

   **Recommended:** Option [X] — [1–2 sentence reasoning tied to this specific feature and project context]

   Then render options as a Markdown table:

   | Option | Description |
   |--------|-------------|
   | A | ... |
   | B | ... |
   | C | ... |
   | D | Something else — I'll describe it |

   After the table: `Reply with a letter to choose, say "yes" or "recommended" to accept the suggestion, or describe your own answer.`

   - After the user replies:
     - "yes" or "recommended" → use your stated recommendation.
     - Letter match → record that option.
     - Free text → validate it resolves the question; if ambiguous, ask a quick follow-up (does not count as a new question).
   - Once accepted, immediately integrate the answer into the spec and save the file. Then move to the next question.
   - Stop early if: all critical gaps are resolved, the user says "done" / "good" / "stop", or you reach 5 questions.

5. **Per-answer integration rules.**
   Apply each accepted answer to the most relevant spec section:
   - User journey or priority decision → add or reorder a User Story block; update its priority label and "Why this priority" field
   - Role/permission decision → update the relevant User Story descriptions and acceptance scenarios; add or sharpen FR-### entries in Functional Requirements
   - Data shape or entity decision → update Key Entities
   - Edge case resolution → add a bullet under Edge Cases; add or update a Given/When/Then scenario in the relevant User Story
   - Success metric → add or sharpen an SC-### entry
   - Scope or dependency clarification → update Assumptions; remove any `[NEEDS CLARIFICATION]` marker the answer resolves
   - If the answer contradicts a placeholder or earlier assumption, replace it — leave no contradictory text.
   - Save after every integration.

6. **Completion report** (after the loop ends or early termination):
   Output exactly this structure:

   ---

   **Spec written:** `docs/features/[filename].md`

   **Questions asked:** [N] / 5

   **Sections updated:** [list]

   **Coverage summary:**

   | Category | Status |
   |----------|--------|
   | User stories & priority order | Clear / Partial / Missing |
   | Acceptance scenarios (Given/When/Then) | Clear / Partial / Missing |
   | Role & permission model | Clear / Partial / Missing |
   | Functional requirements | Clear / Partial / Missing |
   | Key entities | Clear / Partial / Missing |
   | Edge cases | Clear / Partial / Missing |
   | Success criteria | Clear / Partial / Missing |
   | Assumptions | Clear / Partial / Missing |

   **Next step:** [One of: "Run `/brainstorm` again with more detail to fill remaining gaps." / "Run `/clarify` to sharpen edge cases and `[NEEDS CLARIFICATION]` markers before planning." / "Spec looks complete — ready to plan implementation."]

   ---

## Behavior rules

- Never ask about all 5 questions at once — always one at a time.
- Never invent facts about the project. If unsure, mark with `[NEEDS CLARIFICATION: ...]`.
- Do not create a spec for a feature that already has a mature spec — redirect to `/clarify`.
- Respect early termination: "stop", "done", "skip" ends the loop immediately and triggers the completion report.
- Keep inserted text minimal and testable. Avoid narrative drift.
- Preserve existing spec formatting and heading hierarchy when editing.
- When writing acceptance scenarios, each Given/When/Then must describe observable system behavior — not implementation details.
