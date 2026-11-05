# Wind calculator test audit (2026-11-04)

Two parts:
1. An audit of the existing tests: does each one check a number taken from ASCE 7, or only what the code already does?
2. A check against three **published worked examples** by other authors (in `examples/`). Each builds its own building model and runs the whole chain: model → WindAdapter → WindAnalysis → lateral loads. The tests are in `ASCE7Wind.Tests/PublishedExampleTests.cs`.

Nothing in the program was changed for this audit.

## 1. Published examples: results

16 tests: 11 pass, 5 fail. No failure points to a calculation error in the program. Each comes from the example itself, either rounding or a slip, and each is explained in its README.

| Example | Procedure | Pass | Fail | Why it fails |
|---|---|---|---|---|
| [01 Quimby, two-story office](examples/01-quimby-two-story/README.md) | Ch. 27 (as 7-05 Method 2) | 4 | 2 | The author uses the 15° Cp (−0.70) for a 14.04° roof. Fig. 27.3-1 interpolated gives −0.74, and Case I uplift follows (94.4 vs 92.1 k). |
| [02 SkyCiv, plant structure](examples/02-skyciv-plant/README.md) | Ch. 27 (7-10), partially enclosed | 3 | 1 | Kz rounded to 2 places in the article (qh 31.33 vs 31.40). 3 of 14 pressures are off by 0.06 to 0.08 psf. |
| [03 Medeek report, gable](examples/03-medeek-envelope/README.md) | Ch. 28 envelope (7-10) | 4 | 2 | The report prints a = 3.00 ft; the standard's rule gives 3.6 ft. The roof horizontal force follows (−1.743 vs −1.712 k). |

**Confirmed against independent published numbers:**
- **Ch. 27:**
  - qz at several heights;
  - wall Cp, including the L/B interpolation;
  - roof Cp normal to the ridge (interpolated in θ and h/L) and parallel to it (distance bands), both values;
  - band plan areas;
  - design pressures with ±GCpi, the windward wall using qz at height;
  - horizontal totals in both directions;
  - roof uplift (all of E/W, and N/S Case II).
- **Ch. 28:**
  - qh;
  - GCpf for all 8 Case A zones and all 12 Case B zones at 18.43°;
  - T zones at 25%;
  - wall base shears in both directions;
  - the "roof = 0" rule in Case A.

**Not covered by any of the three examples:**
- C&C, apart from the SkyCiv wall zones, which are still to be added;
- parapets;
- hip roofs;
- Ch. 28 overhangs;
- the 7-22 edition differences;
- the Fig. 27.3-8 Cases 2–4 (no example gives them).

## 2. Existing tests: what they really check

The 124 tests were audited for whether each expected value comes from the standard (or a hand calculation from it) or from the code. The findings, most important first:

| # | Finding | Where | Confidence |
|---|---|---|---|
| 1 | **Ch. 28 zone 2/2E extent.** Fig. 28.3-1 limits zone 2 to the smaller of 0.5 × the dimension parallel to the wind and 2.5 he; the rest of the roof is zone 3. This isn't implemented, and a test enforces the unlimited extent. Only matters when a windward slope is wider than 2.5 he. (In example 3 it doesn't come into play: 18 ft < 37.5 ft.) | `EnvelopeLateralLoads.cs` 300–310; `EnvelopeTests.cs` 191 | ~75% |
| 2 | **Cases 2–4 built on the 27.1.5 minimum.** `DesignKips` = max(calculated, minimum) is used to build Fig. 27.3-8 Cases 2–4. The 16/8 psf minimum is a separate load case in the standard, not a base for the eccentric cases. | `WindLateralLoads.cs` 32, 131; `StoryWindLoads.cs` 111 | high |
| 3 | **Fig. 30.3-1 note 5 not implemented:** wall GCp may be reduced 10% when θ ≤ 10°. The parapet tests lock in the unreduced values. | `ParapetCladdingTests.cs` 38, 54, 55 | high |
| 4 | **5E/6E width = a** was "confirmed by the user" but has no published source behind the test. | `EnvelopeTests` | open |
| 5 | **Test bug:** a test loops over editions but hard-codes `ASCE_VER_7_22` and ignores the loop variable `v`. | `Chapter30_CladdingTests.cs` 215 | certain |
| 6 | **No value tests** for:<br>• Fig. 27.3-1 normal-to-ridge rows at h/L 0.5 and 1.0 (the published examples now cover h/L ≈ 0.5);<br>• the area reductions 0.9 / 0.8;<br>• Figs. 30.3-2B to I;<br>• the 7-22 overhang roof + wall rule, which is marked "TO VERIFY" in the code;<br>• the 0.6h / 0.2h zone widths. | `WindLoadCalculator_CC.cs` 112–124 and others | — |
| 7 | **Parapets:**<br>• 16 psf minimum applied per parapet face;<br>• MWFRS parapets ignored on sloped roofs. | `WindLoadCalculator_CC.cs` 156 | medium |
| 8 | **Ke fixed at 1** (7-16 / 7-22 ground elevation factor); **Ri not implemented.** | — | certain |
| 9 | **Self-referential tests:** the GoldenMaster tests, most ModelAdapter tests and many structural tests compare the code against itself (snapshots, sums of its own parts). They catch changes, not misreadings of the standard. | `GoldenMaster*`, `ModelAdapterTests` | — |
| 10 | **Ch. 27 windward wall.** The audit asked whether `AddWallPressures` uses qh for the whole windward wall. The published examples settle it: the windward wall uses qz at each height (`WindwardWallExternalPressureAt`, integrated in `WallsKips`), as both examples do. | `WindLoadCalculator_MWFRS.cs` 145 | resolved |

**Checked and consistent with the standard:**
- Kz tables and constants;
- Kd placement by edition;
- GCpi;
- the Fig. 27.3-1 values as tested;
- the Fig. 27.3-8 factors;
- the 27.1.5 and 30.2.2 minimums;
- Fig. 28.3-1 Case A/B GCpf;
- the 25% T zones.

## 3. Fixed (2026-11-04, after this audit)

| # | Fix | Tests |
|---|---|---|
| 1 | **Ch. 28 Case A:** a negative zone 2/2E GCpf now applies only for min(0.5 D, 2.5 he) from the windward eave. The rest of the windward slope to the ridge takes zone 3/3E (`EnvelopeDirection.Zone2Reach`). The plan diagram shows the cut. Case B is unchanged: the note's "distance from the edge, to the ridge" describes the Case A layout. | `EnvelopeTests.Zone2_LimitedTo2_5he_BothEditions` (replaces the eave-to-ridge test), `Zone2_ReachesRidge_WhenHalfDepthIsLess` |
| 2 | **Superseded (the user's later ruling: "Take the code and commentary as true").** C27.1.5, and C28.3.4 (7-16) / C28.3.6 (7-22), say the minimum is "applied as a separate load case in addition to the normal load cases". It is now its own case (`WindLateralLoads.MinimumCaseNumber` = 0), one per wind, with no torsion and no internal pressure. Cases 1–4 are built on the calculated force. The earlier "raises the force" rule is withdrawn. | `LateralLoadTests.Minimum_IsASeparateLoadCase_C27_1_5`, `WindCaseTests.MinimumCase_PerWind_Separate`, `EnvelopeTests` (minimum as a separate case) |
| 3 | **Fig. 30.3-1 note 5:** wall GCp × 0.9 when θ ≤ 10° (a flat roof counts as 0°), in both editions. The reduction reaches the wall pressures and the parapet faces taken from the wall figure. Fig. 30.3-2A note 5 (positive roof zones 2/3 equal to wall zones 4/5) still uses the figure's unreduced wall values, because note 5 of 30.3-1 is for walls. | `WallReductionTests` (new); parapet hand calculations redone with 0.9 |
| 4 | **Test bug:** the gable-end test now runs the edition it is given. | `Chapter30_CladdingTests.WallZones_GableEnd_IncludesTriangleWithApexAtMidSpan` |

## 4. Still open

- **Every wind case keeps its own zone values.** No zone gets a larger value from another case, and the Load Manager analyzes each case separately. Places that break this today:
  - `WindLateralLoads.DirectionalForce` keeps the "governing" of the two Fig. 27.3-1 roof values. The two values, roof Case A and roof Case B, should each be their own case, carrying their own roof pressures (`WindCases` does this; `DirectionalForce` still picks one). It no longer feeds SWC or the Load Manager (§ 6). It is left only in this calculator's own summary screens (`WindSummary`, the setup and details pages) and in `StoryWindLoads.Compute`'s per-floor direction forces.
  - ~~The Load Manager makes its own roof wind and pairs it with the shear wall cases.~~ Fixed (§ 6).
  - SWC's foundation export keeps the larger couple of the rigid and flexible runs at each wall end (`MainWindow.Lateral.cs`, `Govern`). Rigid/flexible is not a wind case (the user's ruling), so the wind case stays one case; the choice is between two ways of sharing it among the walls.
- Cases 3 and 4: settled. Each quartering wind takes each pair of roof values (A/A, A/B, B/A, B/B), resolved area by area.
- Ch. 28 overhangs, Ke, Ri, and the self-referential tests (items 7–9).

## 5. Wind cases, each with its own zones (2026-11-04)

- `WindCases.Build` makes every Fig. 27.3-8 case: 4 winds × 2 roof values (Fig. 27.3-1 roof Case A and Case B: on a flat roof, −0.9 / −0.5 / −0.3 by distance, and −0.18 over the whole roof; both are suction) × ±GCpi (Case 1), each with ± torsion (Case 2), the 4 quartering winds × 4 roof pairs × ±GCpi (Case 3), each with 4 torsion signs (Case 4). That gives 208 cases. Each case keeps its own wall and roof zone pressures.
- A quartering case is resolved area by area. The two winds' zone maps are overlaid surface by surface, and each piece takes 0.75 (Case 4: 0.563) of each wind's external pressure, plus the case's internal pressure once.
- `WindCaseTests.EveryCase_CoversEachSurfaceOnce`: in every case, each surface is covered exactly once (no double counting).
- `WindCaseEnvelope.Build` gives each piece its largest and its smallest net pressure, each with the case that gives it. The two are kept apart.
- In the 3D viewer, **Wind cases** opens a list: the envelope first, then every case. A filter narrows the list, and the arrow keys step through it. A Max / Min toggle switches the envelope between pressure and suction.
- Tests: `WindCaseEnvelopeTests` (coverage; the SW roof corner at −36.4 psf from Case 3 SW, worked by hand) and `Wind3DViewerRenderTests.WindCases_ListEnvelopeToggleAndOneCase`.

**Region definitions, checked against secondary sources** (the standard itself was not available: it needs a login, and the PDFs could not be rendered without installing poppler):
- **Ch. 27, Fig. 27.3-8:** the figure is a plan with one uniform pressure per windward and leeward face (PWX, 0.75 PWX, 0.563 PWX). Torsion is a separate moment MT = 0.75(PWX + PLX)·BX·eX with eX = ±0.15 BX. It is not a split of the pressure across a face. Here a quartering wall carries the superposed pressures of both winds (e.g. the west wall is windward for the W wind and a side wall for the S wind), so it differs from a single wind only through the windward qz height bands. The roof is resolved piece by piece, since the zone maps of the two winds differ.
- **Ch. 28, Figs. 28.3-1 / 28.3-2 (7-22: Load Cases 1–4):** this is where walls are subdivided: an end strip 2a wide at the reference corner (1E/4E, 5E/6E) and the interior zones. In the torsional cases (7-22 LC 3 and 4), the half away from the reference corner takes the T zones at 25 %. Every corner is taken in turn as the reference corner. These are not built as wind cases yet.
- Still unconfirmed: the verbatim 7-16 / 7-22 figure notes, the exact layout of the Case 4 MT formula, the 7-22 revision of Fig. 27.3-8, and the width and position of 5E / 6E.

### Chapter 28 wind cases (2026-11-04)

- `WindCases.BuildEnvelope` makes the 7-22 Load Cases 1 to 4 (Figs. 28.3-1 / 28.3-2), each with its own zones:
  - LC1: transverse, Case A;
  - LC2: longitudinal, Case B;
  - LC3 and LC4: their torsional cases, with the T zones at 25 % beyond the plan center.
- Each building corner in turn is the reference corner, and each sign of GCpi is its own case: 2 axes × 4 corners × 2 × 2 = 32 cases (16 when torsion is exempt). p = qh Kd [(GCpf) − (GCpi)].
- The zones are the rows of `EnvelopeLateralLoads.Direction`, moved onto the building's surfaces for each corner. `ZoneForces_AddUpToVxVyMt_AtEveryCorner` checks that their forces add up to the case's Vx, Vy and Mt at every corner of both axes. This confirms that the mapping and the existing corner rule agree.
- Hand values come from `EnvelopeTests` (flat 60 × 40 × 15 ft):
  - zone 1E at the reference corner is 0.61 q ∓ 0.18 q;
  - zone 1T on the far half is 0.25 × 0.40 q − 0.18 q;
  - the envelope's largest push at the SW wall corner is 1E with −GCpi.
- In Case A the walls parallel to the wind take GCpf = 0 (the user's ruling). They are still zones, with the internal pressure alone: −0.18 q with +GCpi, +0.18 q with −GCpi (`CaseA_WallsParallelToTheWind_GCpfZero_InternalPressureOnly`). Every case covers every surface exactly once.
- The Chapter 28 minimum (28.3.4 in 7-16, 28.3.6 in 7-22) is a separate load case, one per wind (C28.3.4 / C28.3.6), as in Chapter 27.
- The viewer's case list has a Ch. 27 / Ch. 28 choice. Each procedure has its own envelope; the two are never mixed.
- The envelope grid now overlays every distinct zone map among the cases, starting from the whole surface.

**Still open (Chapter 28):**
- ~~A flat roof takes Case A in both directions.~~ Settled: a flat roof is run as though it had a ridge at mid-building, once each way (Case A and Case B in each direction).
- ~~`EnvelopeLateralLoads.Compute` makes the minimum a separate load case.~~ That is now the rule everywhere (C28.3.4 / C28.3.6).

### Fig. 27.3-1 checks and Ch. 28 flat roofs (2026-11-04)

- **Ch. 28 flat roof:** the same tables and zones as a sloped roof, with Case A read at the 0-5° row. With no ridge, it takes Case A and Case B in each direction (`EnvelopeLateralLoads.CoefficientCases`). This applies both in `Compute` (SWC) and in the zone cases: 64 Ch. 28 cases for a flat roof, 32 for a gable.
- **Distance bands** (0-h/2, h/2-h, h-2h, >2h from the windward edge) are cut to the building's depth along the wind. A narrow building has only the bands that fit, and only those zones get pressures. That is all that other programs read (`NarrowBuildingZoneTests`).
- **Where the bands apply:** wind normal to the ridge with θ < 10°, or parallel to the ridge at any θ. Otherwise the slopes take the windward / leeward Cp (`DistanceBands_NormalBelow10Degrees_OrParallelAtAnyAngle`).
- **Normal-to-ridge table (θ ≥ 10°):** every listed value matches the figure, as read from my memory of it (the standard was not available), in all three h/L rows, windward (both values) and leeward.
  - **Open:** where the figure gives one windward value (h/L ≤ 0.25 at 45°, and 0.01θ at ≥ 60° for every h/L), the code still carries a Case A of 0.0. That is a second roof case the figure does not give.
  - **Open:** the −1.3 (h/L ≥ 1.0) is never reduced for area (factor fixed at 1.0, which is conservative).

## 6. One wind source for every app (2026-11-04)

The user: "The source of truth for wind is the Wind Calculator." What changed:
- `WindCases.For(analysis, onlyCases1And3)` returns this calculator's own case list for the procedure its setup chose: Ch. 27 `Build` (212 cases: 208 + 4 minimum), or Ch. 28 `BuildEnvelope` when it is chosen and applies.
- `StoryWindLoads.ComputeCases` gives every one of those cases on every floor, under its own name:
  - the wall part goes by the wall force on each floor's strip (qz up the windward wall);
  - the minimum's 16 psf goes by wall area;
  - the roof and parapet part goes to the roof;
  - Ch. 28 goes in the proportion of that direction's forces.
  - Each case keeps internal `Parts` so the floors add up exactly.
- Tests: `StoryWindTests.ComputeCases_EveryCalculatorCase_FloorsAddUp_BothChapters`, and `ComputeCases_TwoStories_ByHand` (Case 1, Case 2 torsion 0.75 × 0.15 B × the floor's force, and the minimum's 16 psf × B × 15 ft).
- `WindCase.SoffitPsfAlongLength / AlongWidth` is the windward overhang soffit pressure: Ch. 27 Cp 0.8 (27.3.3) at the case's factor, Ch. 28 GCp 0.7 (28.3.3).
- SWC designs the walls for exactly these cases (`MainWindow.Levels.SetWindLoadsPerLevel`). It saves only its hold-down couples, under these names; its own "From" direction guess is removed. The manual-loads mode stays at the user's choice ("Keep it"). It unlinks the wind, and SWC then writes no Lateral section, so manual loads never reach the Load Manager.
- The Load Manager's own 8 roof cases (`RoofWind.Mwfrs`) and the direction pairing (`FoundationAnalysis.PairWind`) are removed.
  - `RoofWind.Cases` gives one roof case per calculator case, with that case's own zones and GCpi sign.
  - The foundation runs each case once, with the shear walls' forces of the same name.
  - A saved shear wall case the calculator no longer has is left out, with a note.
- Tests: `WindUpliftTests.Cases_TheCalculatorsOwn_ZonesOnThePlan_HandCalc` (−23.09 / −14.29 / −35.30 psf by hand), `FoundationAnalysisTests.WindCases_TheCalculatorsOwn_ShearWallsByName` and `WindCases_FullChain_EveryCalculatorCase`.
- Run time on lbltwo: the Load Manager's 212-case uplift took 2.9 s per edit; it now takes about 0.39 s, with the results identical to the last digit (4,452 values compared).
  - About 87 % of the time was finding the roof zone at each sample point for every case. `RoofWind.Locator` now finds each point once per zone layout and is shared by every case with the same outlines: 1.1 s.
  - The cases then run in parallel (`WindUplift.Run`, `Parallel.For`, kept in order): 0.39 s on 16 cores.
  - `FoundationAnalysisTests.WindCases_FullChain_EveryCalculatorCase` checks that the parallel run matches each case run alone.
  - Still open: SWC's time for designing 212 cases per wall.
