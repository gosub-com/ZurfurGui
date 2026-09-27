# Copilot Instructions

## Project Guidelines
- Prefer avoiding heuristic-based type inference in the ZUI generator; favor explicit concrete type names in JSON
  and deterministic rules (e.g., deriving interface name by prefixing `I`).
- Do not add horizontal rules between sections in Markdown documentation files in this repo. Follow the style of
  existing docs (e.g., architecture.md) which do not use them.

## Markdown Formatting Guidelines
- Wrap Markdown prose and list items at 115 characters or fewer.
- Tables and lines containing inline backtick code may exceed 115 characters when splitting would reduce readability
  or break formatting.

## Naming Conventions
- For sample control naming, use descriptive button field names such as `buttonOffsetPlus` and `buttonOffsetMinus`,
with matching PascalCase event handler names like `ButtonOffsetPlus_Click` and `ButtonOffsetMinus_Click`.

## Coding Style Guidelines
- Organize code files with the following order:
  1. Using directives
  2. Namespace declaration
  3. Class declaration
  4. Fields
  5. Simple Properties (complex or big properties can go down in the methods section)
  6. Constructors (followed closely by the functions it directly calls and other initialization functions like
     LoadContent, etc.)
  7. Methods (public closer to the top, private more towards the bottom, caller functions above callee functions
     so the general call flow is downwards)
- Use spaces everywhere, no tabs anywhere. Use 4 spaces for indentation.
