# Example 3: Gable building, MWFRS envelope procedure (ASCE 7-10 Ch. 28)

**Source:** a wind load report produced by the Medeek Design online wind load calculator and issued by an engineering firm,
report no. 2026D75, ASCE 7-10, Chapter 28 Part 1 (envelope procedure, low-rise building).

Only the technical content is imported here. The report's title block names a private client and a project address, and both are left out on purpose. The figures were not imported: the report is a PDF, and rendering its pages needs a PDF tool (poppler) that is not installed here and may not be installed without asking. The values below are transcribed from the report's text.

**Why this one:** it is the only one of the three examples for **Chapter 28**. It covers:
- Load Case A (transverse) and Case B (longitudinal);
- GCpf interpolated at 18.43°;
- the end zones (1E to 6E);
- the torsional zones (1T to 6T);
- the "roof pressure = 0" rule;
- the diaphragm base shears.

ASCE 7-10 Fig. 28.4-1 is ASCE 7-16 Fig. 28.3-1, with the same coefficients. The tests run it as ASCE 7-16.

---

## Design data

| | |
|---|---|
| Building | Gable roof, enclosed, low-rise |
| Width W (across the ridge) | 36 ft |
| Length L (along the ridge) | 48 ft |
| Eave height | 12 ft |
| Roof pitch | 4/12 (θ = 18.43°) |
| Ridge height | 18 ft |
| Mean roof height h | 15 ft |
| Overhangs | 2 ft at eaves and gables |
| Basic wind speed V | 115 mph |
| Exposure | C |
| Kzt / Kd | 1.0 / 0.85 |
| Kh | 0.85 |
| qh | **24.43 psf** |
| GCpi | ±0.18 |
| End zone width a | **3.00 ft** (as printed) |
| Zone 2/2E distance | 18 ft |

## Load Case A: transverse (wind across the ridge)

GCpf, interpolated at 18.43°. For example, zone 1: 0.40 + (13.43/15)(0.13) = 0.516.

| Zone | GCpf | p, +GCpi (psf) | p, −GCpi (psf) |
|---|---|---|---|
| 1 | 0.52 | 8.22 | 17.01 |
| 2 | −0.69 | −21.25 | −12.46 |
| 3 | −0.47 | −15.84 | −7.05 |
| 4 | −0.42 | −14.54 | −5.75 |
| 1E | 0.78 | 14.66 | 23.46 |
| 2E | −1.07 | −30.54 | −21.74 |
| 3E | −0.67 | −20.85 | −12.05 |
| 4E | −0.62 | −19.50 | −10.70 |

Overhangs (psf):

| Zone | p |
|---|---|
| 2OH | −16.86 |
| 2EOH | −26.14 |
| 3OH | −11.45 |
| 3EOH | −16.45 |
| 2OH + W | −33.96 |
| 2EOH + W | −43.24 |

## Load Case B: longitudinal (wind along the ridge)

| Zone | GCpf | p, +GCpi (psf) | p, −GCpi (psf) |
|---|---|---|---|
| 1 | −0.45 | −15.39 | −6.60 |
| 2 | −0.69 | −21.25 | −12.46 |
| 3 | −0.37 | −13.44 | −4.64 |
| 4 | −0.45 | −15.39 | −6.60 |
| 5 | 0.40 | 5.37 | 14.17 |
| 6 | −0.29 | −11.48 | −2.69 |
| 1E | −0.48 | −16.12 | −7.33 |
| 2E | −1.07 | −30.54 | −21.74 |
| 3E | −0.53 | −17.34 | −8.55 |
| 4E | −0.48 | −16.12 | −7.33 |
| 5E | 0.61 | 10.50 | 19.30 |
| 6E | −0.43 | −14.90 | −6.11 |

## Torsional load cases (25% of the full pressure)

| Zone | +GCpi (psf) | −GCpi (psf) |
|---|---|---|
| 1T | 2.05 | 4.25 |
| 2T | −5.31 | −3.11 |
| 3T | −3.96 | −1.76 |
| 4T | −3.64 | −1.44 |
| 5T | 1.34 | 3.54 |
| 6T | −2.87 | −0.67 |

## Diaphragm design loads (ASD, × 0.6; the bottom half of the walls is neglected, as it goes straight to the foundation)

**Case A, transverse:**

| | lb |
|---|---|
| Walls | 4180 |
| Roof | −1027 |
| Overhangs | −591 |
| Total | 2562 (R1 = 1326, R2 = 1236) |
| Roof pressure = 0 case | **4180** |
| Minimum (16 psf walls, 8 psf roof) | **4429** |

**Case B, longitudinal:**

| | lb |
|---|---|
| Walls | 2277 |
| Gable ends | 1100 |
| Total | **3377** (RA = 1734, RB = 1643) |
| Minimum | 3110 |

---

## What the tests check (`ASCE7Wind.Tests/PublishedExampleTests.cs`, `Ex3_*`)

Model: 48 ft (x) × 36 ft (y), one level at 12 ft, gable 4/12 with the ridge W-E, 2 ft overhang, enclosed, V 115, Exposure C, ASCE 7-16, lateral procedure = envelope (Ch. 28).

| Test | Published | Result |
|---|---|---|
| Ch. 28 applies; h; qh; θ | 15; 24.43; 18.43° | **pass** |
| End zone width a | 3.00 printed; the standard gives 3.60 | **pass** (checks 3.60; the report's 3.00 is an apparent error) |
| Case A GCpf (8 zones) and p with +GCpi | table above | **pass** |
| Case B GCpf (12 zones) | table above | **pass** |
| Torsional zones at 25% (1T to 4T in A, 5T and 6T in B) | 25% | **pass** |
| Base shear, walls only, strength, whole walls: A 4180/0.6×2 = 13.93 k; B (2277×2 + 1100)/0.6 = 9.42 k | 13.93; 9.42 | **pass** |
| Case A roof horizontal force, no overhangs: −1027/0.6 = −1.712 k | −1.712 | **pass**: −1.712 reproduced with the report's a = 3.0; the program gives −1.743 with a = 3.6 |

**End zone width a.** Fig. 28.3-1 note 9 (7-10 Fig. 28.4-1) gives a = 10% of the least horizontal dimension or 0.4h, whichever is smaller, but not less than 4% of the least dimension or 3 ft. Here that is min(3.6, 6.0) = 3.6, which is at least max(1.44, 3) = 3, so **a = 3.6 ft**. The report prints 3.00 ft, the 3 ft floor. That looks like a slip in the report. The program follows the standard.

**Roof horizontal force.** By hand, qh × 6 ft rise × [(−0.69 + 0.47)(48 − 2a) + (−1.07 + 0.67)(2a)] gives −1.706 k with a = 3.0 (the report's −1.712 k) and −1.738 k with a = 3.6. The program gives −1.743 k. The difference from the report is the end-zone width above. Neither number changes the design, because the roof's horizontal force is negative and the "roof pressure = 0" case governs.

**Overhangs:** the program's Chapter 28 forces leave out the overhang pressures (2OH, 3OH...). In Case A they only reduce the total, which is then set aside for the roof = 0 case. They would matter for uplift, which this test doesn't cover.
