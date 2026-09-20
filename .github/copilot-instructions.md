# Copilot Instructions

## Project Guidelines
- Prefer avoiding heuristic-based type inference in the ZUI generator; favor explicit concrete type names in JSON and deterministic rules (e.g., deriving interface name by prefixing `I`).
- Do not add horizontal rules between sections in Markdown documentation files in this repo. Follow the style of
  existing docs (e.g., architecture.md) which do not use them.

## JSON Conversion Guidelines
- When converting JSON object keys to JSON5, remove quotes from keys that are valid unquoted identifiers, including keys beginning with `$`, such as `$namespace`.

## Markdown Formatting Guidelines
- Wrap Markdown prose and list items at 115 characters or fewer.
- Tables and lines containing inline backtick code may exceed 115 characters when splitting would reduce
  readability or break formatting.
