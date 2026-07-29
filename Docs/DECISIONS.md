# Architectural Decision Log

Record important decisions here.
Do not log every small coding choice.

Use this format:

## ADR-XXX — Title

**Status:** Proposed | Accepted | Replaced

**Context:**  
What problem or uncertainty required a decision?

**Decision:**  
What was chosen?

**Reasoning:**  
Why is this preferable to the alternatives?

**Consequences:**  
What becomes easier, harder, or constrained?

---

## ADR-001 — Fixed Logical Board Across Devices

**Status:** Accepted

**Context:**  
Changing playable board dimensions by device aspect ratio changes threat travel distance, barrier completion time, and level difficulty.

**Decision:**  
Use the same logical board dimensions on supported phones and tablets. Fit that board into the gameplay viewport. Wider devices may show non-playable decorative space.

**Reasoning:**  
A level should behave consistently regardless of device.

**Consequences:**  
Tablet layouts require intentional framing outside the board, but level balance remains stable.

---

## ADR-002 — Gameplay and Presentation Separation

**Status:** Accepted

**Context:**  
The first build may use placeholders, while later art must be replaceable through Unity without rewriting gameplay.

**Decision:**  
Keep board, barrier, capture, threat, and scoring rules independent from sprites, materials, audio, VFX, and haptics.

**Reasoning:**  
This supports fast prototyping, safe reskinning, testing, and multiple themes.

**Consequences:**  
Additional presentation adapters/components are required, but gameplay logic becomes testable and reusable.
