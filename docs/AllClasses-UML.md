# All Classes Combined — the full RJTech architecture in one diagram

Everything in one connected image: **ViewModels → Controllers → Services →
Models → Data**, using the same standard **three-section UML compartments**
(name / attributes / operations) as the rest of the set, this time hand-laid-out
as SVG so every class and every relation has an exact, reproducible position.

- Image: [`AllClasses-UML.png`](AllClasses-UML.png) (6700 × 4945)
- Editable source: [`AllClasses-UML.svg`](AllClasses-UML.svg)
- Generator: [`combined-uml.js`](../../build/combined-uml.js)
  (see _Re-rendering_ below)

---

## What is shown

| Band | Colour | Layer | Contents |
| --- | --- | --- | --- |
| 1 | green `#1F6F54` | **View Models / DTOs** | 27 classes — per-module VMs incl. dashboard &
  reports/view-models in a 4-across grid |
| 2 | blue `#1F4E79` | **Controllers (ASP.NET MVC)** | 12 controllers + framework `Controller` base |
| 3 | purple `#5B3E77` | **Services** | 9 real services + supporting helpers/interfaces/enums |
| 4 | teal `#2F6472` | **Models (EF entities + dataset records)** | 13 entities + 16 report dataset records |
| 5 | slate `#333F50` | **Data / Persistence** | `ApplicationDbContext` |

Columns group classes by module: **auth · settings · dashboard ·
products&scan · sales · installments · delivery · notifications · reports ·
report-data grid · excel** (the report-data grid is the wide column whose cells
are the 16 dataset record types `ReportSnapshot` aggregates).

**165 edges** are shown: composition (diamond, solid), aggregation (diamond,
open), generalisation (hollow arrow), dependency, implements (dashed), and the
dashed **persists** relations that every controller and entity has with the
`ApplicationDbContext`.

## Honest quality note

This is a _dense_ diagram by design — 108 boxes and 165 relations at
all-modules scale. The layout is produced with a constrained orthogonal router:

- **Zero box overlaps and zero connector-through-box crossings.** Every
  connector enters and leaves only at its own two boxes (verified geometrically,
  all 165 endpoints land on their boxes).
- Connector-connector crossings are confined to the **open gutters/seams**
  between bands and the gaps between columns/cells (the only free corridors a
  cross-module edge can use), never through a box.
- At this scale a modest number of such crossings is structurally forced; they
  are intentionally routed through empty corridor space where they read as
  normal metro-map congestion rather than clutter.

The two real clutter zones are honest products of the architecture itself:
the **report-data grid fan** (16 edges from `ReportSnapshot` out to its 16
aggregated record types) and the **persistence fan** (23 verticals dropping
from 10 controllers + 13 entities into the top of `ApplicationDbContext`).

---

## Re-rendering

```powershell
node build/combined-uml.js combined-uml.svg
& 'C:\Program Files\Google\Chrome\Application\chrome.exe' `
  --headless=new --disable-gpu --hide-scrollbars `
  --screenshot='combined-uml.png' --window-size=6700,4945 `
  'file:///C:/path/to/combined-uml.svg'
```

Run `node combined-uml.js combined-uml.svg` then the Chrome screenshot above
from `build/`, and check geometry with
`node verify-layout.js combined-uml.svg` (reports box overlaps,
connector-through-box crossings and connector-connector crossing count).