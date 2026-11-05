# ASCE 7 Wind Load Calculator: notes for assistants

- **Read first:** `..\DesignHub\docs\SUITE.md`: all seven repos, how they fit, the branches, the build, and **the
  owner's rules** (local commits only, no installs, stay in the project folders, never click the owner's windows).
- **Branch:** `dev/ui-pass-2`.
- **Structure:**
  - `ASCE7Wind.Core`: the engine, no UI. It is edition-agnostic: everything that differs between ASCE 7-16 and 7-22 is
    in `Editions/Asce7Edition.cs`, plus the per-edition figures in `Coefficients/Chapter30/ASCE7_16|ASCE7_22` and the
    zone layouts in `Zoning/CC/...`.
  - `ASCE7Wind.ModelAdapter`: the building model to the engine, and the `Wind` section of the project file.
  - `ASCE7Wind.App`: `WindLoadCalculator.exe`.
  - The WPF controls are at the repo root (`UserControls`, `Helpers`): `ASCE7WindLoadCalculator.csproj`.
- **Test:**
  - `ASCE7Wind.Tests`: 353 pass, 2 skipped.
  - `ASCE7Wind.UI.Tests`: 71 pass, offscreen.
- **The code itself.** The ASCE 7-16 and 7-22 PDFs are in `C:\AllenEngineering\Codes` (**read only**).
  - PDF page = printed page + 55 for 7-16, + 61 for 7-22.
  - Both editions were audited against them on 2026-11-05 (Ch. 26, 27, 28, 30; commit `62b3570`).
  - **Do not change the 7-22 code unless it is verified wrong against the 7-22 PDF.**
  - A C&C curve holds only its printed breakpoints, straight on log(area) between them.
- **Worked examples** (`audit/examples`, `audit/TEST_AUDIT.md`): Quimby (7-05), SkyCiv plant (7-10), Medeek (7-10).
  - They run on 7-16, the closest edition built.
  - **Never change an example's published numbers.** Test each example on the edition it was written to.
  - Three examples each for 7-16 and 7-22 are still wanted (see the roadmap).
- **The suite's TODO list** is `..\ShearWallCalculator\ROADMAP.md`. Its Wind Load Calculator section lists the open
  items: open buildings, flexible buildings, Ch. 28 parapets, Ri, C&C eave height, and others.
- **Line endings:** some files are mixed LF/CRLF. Scripted multi-line edits must allow `\r?\n`.
