# UML Diagrams — RJTech Capstone

Complete set of UML class diagrams rendered as images. Every class uses the standard
**three-section UML compartment layout**:

| Section | Contents |
| --- | --- |
| **Name** | Class identifier (header) + stereotype / table (`<<entity>>`, `<<dto>>`, `<<sealed>>`, `<<service>>`) |
| **Attributes** | Properties with types — `+` public, `-` private, `$` static |
| **Operations** | Constructor first, then methods (`+ name(params) : return type`) |

Relationships:

- `<|--` inheritance · `..|>` implements · `*--` composition · `o--` aggregation · `-->` dependency
- Generics use tilde notation (e.g. `List~int~`) for Mermaid/Markdown safety.

All Mermaid diagrams were rendered from the sources in this folder. To re-render or tweak,
open the `.mmd` file at [mermaid.live](https://mermaid.live) or run
`npx @mermaid-js/mermaid-cli -i <file>.mmd -o <file>.png`.

---

## 0. Formal Class Diagram (Hierarchical — master view)

The whole RJTech model recreated as one hierarchy: a single `User` **God Class** at
top-center, three large system columns (**User Management · Content Services · Sales &
Database Connector**), and the `DatabaseContext` data layer at the bottom. Hand-laid-out SVG
for exact positioning.

![Formal UML Class Diagram](FormalClassDiagram.png)

Full write-up + per-system breakdown: [Formal-ClassDiagram.md](Formal-ClassDiagram.md)
Editable source: [`FormalClassDiagram.svg`](FormalClassDiagram.svg)

---

## 1. System Overview (all 4 layers)

ViewModels → Controllers → Services → Models + Data, in one diagram.
> Note: large image (29825 × 4542) — zoom in for detail.

![System Overview UML](SystemOverview-UML.png)

Source: [`SystemOverview-UML.mmd`](SystemOverview-UML.mmd)

---

## 2. Class Model (Data Layer)

The EF Core entities (database tables) — 13 classes, the heart of the system.

![Class Model UML](ClassModel-UML.png)

Source diagram: [UML.md](UML.md) · Details table format: [ClassModel-Tables.md](ClassModel-Tables.md)

---

## 3. ViewModels

All view models / DTOs grouped by module: Auth, Settings, Dashboard, Product, Sales,
Installments, Delivery, and Reports.

![ViewModels UML](ViewModels-UML.png)

Source: [`ViewModels-UML.mmd`](ViewModels-UML.mmd)

---

## 4. Controllers

All 12 MVC controllers with their injected service dependencies and action methods.

![Controllers UML](Controllers-UML.png)

Source: [`Controllers-UML.mmd`](Controllers-UML.mmd)

---

## 5. All Classes Combined (everything connected, one image)

The complete architecture in a **single connected diagram**: ViewModels,
Controllers, Services, Models/entities and the `DatabaseContext`, all five
layers linked together — 108 classes, 165 relations. Hand-laid-out orthogonal
SVG (image 6700 × 4945), so there are no box overlaps and no connector runs
through boxes.

![All Classes Combined UML](AllClasses-UML.png)

Write-up + layout truth-notes: [AllClasses-UML.md](AllClasses-UML.md)
Editable source: [`AllClasses-UML.svg`](AllClasses-UML.svg)