from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from copy import deepcopy
import hashlib
import json
import shutil
from lxml import etree as E
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
REF = Path(r'C:\Users\iggie\.codex\plugins\cache\openai-curated-remote\openai-templates\0.1.1\skills\artifact-template-system-design\assets\reference.docx')
IMAGE = Path(r'C:\Users\iggie\.codex\generated_images\01a06cfb-bf7e-7143-a698-c0800621fe8a\exec-65eacfd3-933d-4f7e-abfa-45ba896185d9.png')
NS = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main', 'wp': 'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing', 'a': 'http://schemas.openxmlformats.org/drawingml/2006/main'}
def w(n): return '{'+NS['w']+'}'+n
def el(n, **attrs):
    x = E.Element(w(n))
    for k,v in attrs.items(): x.set(w(k),str(v))
    return x
def xml(x): return E.tostring(x,xml_declaration=True,encoding='UTF-8',standalone=True)

with ZipFile(REF) as z: parts = {n:z.read(n) for n in z.namelist()}
baseline = {n:hashlib.sha256(b).hexdigest() for n,b in parts.items()}
doc = E.fromstring(parts['word/document.xml'])
body = doc.find('w:body',NS)
paras = body.findall('w:p',NS)
tables = body.findall('w:tbl',NS)
refhash=hashlib.sha256(REF.read_bytes()).hexdigest()

contract = f'''# Reports proposal template contract

Reference: {REF}
SHA256: {refhash}
Reference render: qa/reference/page-1.png through page-7.png. All seven inspected.
Renderer: packaged renderer unavailable because soffice.exe is absent. Native Microsoft Word PDF export is the verified fallback; rasterization uses bundled Python pdf2image and Poppler.

One portrait US Letter section; 8.5 by 11 inches. Margins top/left/right 0.7 inch, bottom 0.62014. Header and footer distance 0.5 inch. Different first page enabled. Preserve section XML exactly.
Cover pattern: eight spacer paragraphs, two 36 pt Helvetica Neue titles (light then bold), lower third metadata in a five-cell borderless table and four-row pale-blue label/value table. Preserve source title spacing and sizes. Editable cover slots are document.xml top-level p[8], p[9], tables[0..1].
Body pattern: Helvetica Neue text (10 pt in lists, source body defaults preserved), navy 13.5 pt Heading 1, slate Heading 3, 1.25 line body spacing with 5.5 pt after. Preserve source font, run properties, paragraph rhythm, numbering, and visual hierarchy. Headings can be renamed and numbered with descriptive words. Paragraphs 21..88 are semantic content slots; empty spacing may be removed only to prevent stranded headings/blank pages. Page breaks may be added at major content groups to preserve usable pagination.
Nine source tables: metadata, cover facts, scope matrix, components, data contract, scenarios, launch checks, alternatives, milestones. Fill in place, preserve grids/fills/borders/cell padding, clone source body rows for extra comparable records, retain repeat headers. Source tables 3..8 are slightly wider than body (7.25 inch) and centered, as rendered. Text must fit without reducing font. Source pale blue first columns and white/pale alternating rows remain.
Figure at paragraph 32 is an editable image slot. Replace image1.png with requested Reports UI concept, preserve source width 6.7 inches and change height only to preserve image aspect ratio. Update caption and alt text. No external relationships required.
Footer slot footer1.xml changes organization and artifact name only. Other header/footer structures remain. Footnote 1 is an editable definition slot; preserve its marker and location. No TOC or PAGE fields present. The hyperlink in paragraph 59 is an optional source placeholder and is removed with that unsupported slot. No content controls or text boxes found.
Content map: 1 recommendation; 2 scope; 3 project findings; 4 sample design and report catalog; 5 workflow; 6 metrics; 7 consistency; 8 implementation boundaries; 9 acceptance; 10 alternatives; 11 proposed defaults; 12 implementation milestones.
Package preservation: patch only document.xml, footer1.xml, footnotes.xml, media/image1.png. Preserve all other parts byte-for-byte, including styles, numbering, fonts, theme, settings, all relationships and content types. Baseline hashes in package-baseline.json. Existing unused hyperlink relationship may remain.
Fidelity gates: retained reference unchanged; section properties exactly unchanged; all preserve-only parts identical; no template placeholders remain; image ratio correct; inspect every final page for clipping and pagination. Template colors override generic defaults because the selected reference is the visual authority.
'''
(ROOT/'qa'/'artifact.md').write_text(contract,encoding='utf-8')
(ROOT/'qa'/'package-baseline.json').write_text(json.dumps(baseline,indent=2),encoding='utf-8')

def set_text(p,text):
    ts=p.xpath('.//w:t',namespaces=NS)
    if ts:
        ts[0].text=text
        ts[0].set('{http://www.w3.org/XML/1998/namespace}space','preserve')
        for t in ts[1:]: t.text=''
    else:
        r=el('r');t=el('t');t.text=text;r.append(t);p.append(r)

texts={
8:'RJTech',9:'Reports Page Proposal',
21:'1  Recommendation',
22:'Build Reports as a place to investigate activity, reconcile totals, and export a chosen period. The Dashboard remains a quick overview; Reports adds detailed filters, explicit metric definitions, transaction drilldowns, and downloadable results. Start with Sales Orders, then extend the same layout to five related report areas.',
23:'Keep order value, money received, and outstanding balances separate. Each measures a different event or state. Use purchase dates for orders, payment dates for recorded receipts, and a clearly labelled current snapshot for inventory and outstanding balances. This is a design proposal only; no application implementation is included.',
25:'2  Scope',
27:'3  Findings from the Current Project',
28:'The project already stores checkouts and their item prices, installment contracts and payments, delivery quantities, products, categories, and customers. These records support useful operational reports. However, a completed installment changes its checkout to Paid while retaining the original purchase date, so the existing paid-only Dashboard total is not cash collected during that period.',
29:'There is no complete payment ledger for full payments, down payments, and refund payouts. Products hold selling prices, not purchase costs, and inventory has no complete historical movement ledger. Start with reports these records can support and clearly state when a result reflects current status.',
30:'4  Sample Design and Report Areas',
33:'Figure 1. Reports page concept with fictional sample data. Open the accompanying PNG for full-size inspection; chart values illustrate layout.',
35:'Recommended report tabs',
39:'5  How the Page Works',
40:'Choose a report tab. Sales Orders opens with the current month and Paid plus Ongoing orders selected.',
41:'Choose a date preset or custom range, then relevant category, payment type, payment method, status, or customer filters. Only show filters supported by that report.',
42:'Select Generate Report to apply edits. Reset restores that tab’s defaults. Show applied filters, date basis, PHP currency, and the generation time above the results.',
43:'Read four summary cards, useful charts, and a detailed table. Search and sort the result; paginate the table while keeping totals over all matching records.',
44:'Open an existing checkout, product, delivery, or installment details page from its formatted code or View action. Preserve filters when returning.',
45:'Export all matching rows to CSV, or Print a clean report that can be saved as PDF. Export and print use the same applied filters and metric rules as the screen.',
46:'Use existing light and dark theme tokens, blue actions, rounded cards, and table spacing. Keep status labels without dots, omit checkboxes and trend columns, and provide keyboard focus, loading, empty, and error states.',
48:'6  Metric and Date Definitions',
49:'Rules shared by cards charts tables and exports',
52:'Calculation safeguards',
54:'A checkout containing several items counts as one order. Sum item quantities for units and stored CheckoutItem.SubTotal for item revenue; never multiply header totals through an item join.',
55:'A category filter includes only matching item value and units. Count distinct orders containing those items and relabel value columns as Matching Item Value; do not show the whole checkout total as filtered category revenue.',
56:'Keep merchandise order value separate from installment interest and payment receipts. Example: a PHP 10,000 order counts once; a PHP 1,000 later receipt belongs to its payment date. Remaining balance comes from the installment contract.',
57:'Use current product categories as classifications and stored checkout item prices for historical values. Category edits may regroup past sales; preserve category snapshots later if historical classification is required.',
58:'Date ranges include the entire selected final day. Confirm how existing DateTime.Now records map to Asia/Manila before adding timezone conversions.',
61:'7  Consistency and Historical Limits',
63:'Every result must state its date basis and status scope. Current checkout statuses can restate earlier purchase periods after a cancellation or refund. Current inventory and balances are snapshots at generation time; they cannot be labelled month-end historical balances without additional records.',
65:'8  Implementation Boundaries',
67:'Keep report requests read-only. Some installment display services update stored statuses; use dedicated queries and pure overdue calculations instead of invoking those write paths.',
68:'Reuse the project’s details routes, code formatters, navigation, theme, and table patterns. Support small screens with stacked cards, scrollable tabs, and tables that retain readable columns.',
69:'Use a shared report filter model and query service. Project only needed fields, aggregate in SQL, use AsNoTracking, and page detailed rows on the server.',
70:'Apply the application’s access rules to both previews and exports. Customer contact details are optional columns; ordinary activity reports need only the customer name and reference.',
71:'Add XLSX, saved presets, audited refund events, payment reconciliation, cost-based profit, and historical inventory only in later phases with the required records and agreed definitions.',
74:'9  Acceptance Checks',
76:'10  Design Choices',
80:'11  Proposed Defaults for Review',
81:'Sales Orders uses purchase date and Paid plus Ongoing status. Offer Paid only as a labelled filter; explain the difference from the current Dashboard.',
82:'Collections initially means Recorded Installment Payments. Expand to all collections only after recording full payments, down payments, and refund reversals in one ledger.',
83:'Inventory and Installments show current snapshots. Add a plan start-date filter separately when reviewing an installment cohort.',
84:'Begin with CSV and Print. Add native XLSX or generated PDF if the team needs formatted files beyond browser printing.',
86:'12  Implementation Plan',
87:'Implement in four reviewable milestones after the design is accepted. Each milestone should reconcile to a small known dataset before adding the next report area. The accompanying implementation plan lists proposed files, detailed rules, and validation cases. No schema migration is needed for the initial read-only scope.',
}
for i,t in texts.items():set_text(paras[i],t)
# Remove an optional template link rather than invent a schema URL.
body.remove(paras[59])

def fill_table(idx,rows):
    table=tables[idx]
    trs=table.findall('w:tr',NS)
    while len(trs)<len(rows):
        c=deepcopy(trs[-1]);table.append(c);trs.append(c)
    for tr in trs[len(rows):]:table.remove(tr)
    for tr,vals in zip(trs,rows):
        cells=tr.findall('w:tc',NS)
        for cell,value in zip(cells,vals):
            ps=cell.findall('w:p',NS)
            if '\n' in value and len(ps)>1:
                for p,t in zip(ps,value.split('\n')):set_text(p,t)
            else:
                set_text(ps[0],value)
                for p in ps[1:]:set_text(p,'')

fill_table(0,[['STATUS\nProposed','','OWNER\nRJTech project','','LAST UPDATED\nSeptember 6, 2026']])
fill_table(1,[['Prepared for','RJTech project team'],['Prepared by','Codex'],['Related files','Reports Page Concept.png; Reports Implementation Plan.md'],['Scope','Report functionality, sample interface, data rules, and build milestones']])
fill_table(2,[['Initial release','Later data improvements'],['Sales Orders, with purchase-date totals and item details','Complete collections ledger including refunds'],['Recorded installment receipts and current balances','Historical balance snapshots and precise overdue aging'],['Current inventory, delivery history, customer activity','Stock movement ledger and purchase-cost valuation'],['Consistent filters, CSV, print, and light or dark mode','Saved presets, native XLSX, and generated PDF']])
fill_table(3,[['Report area','Main question','Date basis','Key output'],['Sales Orders','What was purchased?','Purchase date','Value, orders, units'],['Collections','Which receipts were recorded?','Payment date','Installment receipts'],['Installments','What is still owed?','Current snapshot','Open and overdue plans'],['Inventory','What needs restocking?','Current snapshot','Stock and reorder list'],['Deliveries','What stock arrived?','Delivery date','Batches and quantities'],['Customers','Who purchased in this period?','Purchase date','Customer activity']])
fill_table(4,[['Metric','Source','Scope','Calculation or rule'],['Order Value','Checkout','Purchase period','Sum eligible order totals once; Paid + Ongoing by default'],['Orders and Units','Checkout items','Same filters','Distinct checkout IDs; sum ItemQuantity'],['Average Order','Order totals','Same filters','Order Value / Orders; show zero when there are no orders'],['Recorded Receipts','Installment payments','Payment period','Sum Paid payment amounts; excludes down payments'],['Outstanding','Installment balance','Current open plans','Sum Balance on eligible open plans; includes contract interest'],['Inventory Units','Product quantity','Current products','Sum quantities; classify low stock with existing reorder rules'],['Customer Activity','Checkout customer','Purchase period','Distinct transacting customers; no registration growth metric']])
fill_table(5,[['Scenario','Required behavior','Why it matters'],['Installment completes later','Order stays in its purchase period; receipt uses payment date','Separates sales activity from receipts'],['Checkout later refunded','Show current status scope; retain recorded receipt history','No refund payout date or reversal ledger exists'],['Category filter applied','Aggregate matching lines; count each matching order once','Avoids overstated totals'],['Data changes before export','Requery using applied filters and show a new generation time','Do not imply an immutable report snapshot']])
fill_table(6,[['Check','Expected result','Evidence','Gate'],['Totals','Cards, charts, table, export reconcile','Known sample dataset','Required'],['Dates','Final-day and month boundaries included','Boundary records','Required'],['Payments','No duplicate or missing receipt totals','Partial and completed plans','Required'],['UI','Readable in both themes and on mobile','Visual review','Required'],['Read-only','Preview and export change no business rows','Database comparison','Required'],['Release after the detailed validation cases in the accompanying plan pass. Performance targets should be set against the team’s representative dataset.']])
fill_table(7,[['Choice','Benefit','Decision'],['Extend the Dashboard','Small initial UI change','Use a dedicated Reports page for detailed filters and exports'],['Copy paid-only sales logic','Matches the current Dashboard','Offer Paid only as an option; default to explicit order activity'],['Consolidate all cash now','One collections figure','Defer until receipts and reversals have complete dated records'],['Separate query per export','Quick first implementation','Reuse shared metric and filter logic to prevent drift']])
fill_table(8,[['Milestone','Deliverable','Exit criteria'],['M1  Definitions and shell','Filter rules, Reports navigation, responsive theme-aware layout','Known examples agree with the definitions'],['M2  Sales and export','Sales Orders, charts, drilldown, CSV and print','All filtered totals and exports reconcile'],['M3  Related reports','Recorded receipts, balances, inventory, deliveries, customers','Date basis and current-state labels verified'],['M4  Validation and release','Edge cases, access, performance, mobile and theme review','Acceptance cases pass; proposal scope remains read-only']])

# Replace the editable figure without distorting the generated concept.
iw,ih=Image.open(IMAGE).size
cx=round(6.7*914400);cy=round(cx*ih/iw)
for extent in doc.xpath('//wp:extent | //a:xfrm/a:ext',namespaces=NS):
    extent.set('cx',str(cx));extent.set('cy',str(cy))
for x in doc.xpath('//wp:docPr',namespaces=NS):x.set('descr','Sample RJTech Reports interface with report tabs, filters, KPI cards, charts, and an orders table. Fictional data.')
parts['word/media/image1.png']=IMAGE.read_bytes()

footer=E.fromstring(parts['word/footer1.xml'])
for t in footer.xpath('//w:t',namespaces=NS):
    if '[Organization Name]' in (t.text or ''):t.text='RJTech | Reports Page Proposal'
parts['word/footer1.xml']=xml(footer)
foot=E.fromstring(parts['word/footnotes.xml'])
for f in foot.findall('w:footnote',NS):
    if 'Use footnotes' in ''.join(f.xpath('.//w:t/text()',namespaces=NS)):
        p=f.find('w:p',NS);set_text(p,' Current snapshot means the stored state when the report is generated, not the state at a past month end.')
parts['word/footnotes.xml']=xml(foot)

# Group implementation behavior with the user workflow. Remove the redundant
# defaults section; those decisions are already stated in the metric sections.
for idx in range(65,74):
    body.remove(paras[idx]);body.insert(body.index(paras[48]),paras[idx])
for idx in range(80,86):body.remove(paras[idx])
set_text(paras[65],'6  Implementation Boundaries')
set_text(paras[48],'7  Metric and Date Definitions')
set_text(paras[61],'8  Consistency and Historical Limits')
set_text(paras[86],'11  Implementation Plan')
# Remove empty template spacing immediately between headings and their text.
for idx in [62,64,66,72,73,75,77,78,79,85]:
    if paras[idx].getparent() is body:body.remove(paras[idx])

# Keep the source layout while pairing major content groups with page starts.
for idx in [39,48,61,76]:
    p=paras[idx];pr=p.find('w:pPr',NS)
    if pr is None:pr=el('pPr');p.insert(0,pr)
    for x in pr.findall('w:pageBreakBefore',NS):pr.remove(x)
    pr.append(el('pageBreakBefore',val='1'))

# Pair each populated heading with its following content, overriding template
# direct properties which can otherwise strand a heading at the foot of a page.
for p in body.findall('w:p',NS):
    styles=p.xpath('./w:pPr/w:pStyle/@w:val',namespaces=NS)
    if any(s.startswith('Heading') for s in styles) and ''.join(p.xpath('.//w:t/text()',namespaces=NS)):
        pr=p.find('w:pPr',NS)
        for x in pr.findall('w:keepNext',NS):pr.remove(x)
        pr.append(el('keepNext',val='1'))

for idx in [74,86]:
    pr=paras[idx].find('w:pPr',NS)
    spacing=pr.find('w:spacing',NS)
    if spacing is None:spacing=el('spacing');pr.append(spacing)
    spacing.set(w('before'),'200')

(ROOT/'qa'/'artifact.md').write_text(contract+'\nPagination refinement: move the implementation-boundaries block after workflow; remove redundant defaults block (covered by metric definitions and standalone plan); renumber headings. Group pages as cover, overview, concept, workflow, metrics, consistency and checks, choices and milestones. Remove empty source paragraphs only where they strand headings. Preserve type sizes and table grids.\n',encoding='utf-8')

parts['word/document.xml']=xml(doc)
out=ROOT/'RJTech Reports Proposal.docx'
with ZipFile(out,'w',ZIP_DEFLATED) as z:
    for name,b in parts.items():z.writestr(name,b)
shutil.copyfile(IMAGE,ROOT/'Reports Page Concept.png')
mutable={'word/document.xml','word/footer1.xml','word/footnotes.xml','word/media/image1.png'}
assert all(hashlib.sha256(b).hexdigest()==baseline[n] for n,b in parts.items() if n not in mutable)
with ZipFile(REF) as z:original_doc=E.fromstring(z.read('word/document.xml'))
assert E.tostring(original_doc.find('w:body/w:sectPr',NS))==E.tostring(doc.find('w:body/w:sectPr',NS))
assert hashlib.sha256(REF.read_bytes()).hexdigest()==refhash
bodytext=' '.join(doc.xpath('//w:t/text()',namespaces=NS))
assert not any(x in bodytext for x in ['[Describe','[Goal','[Open question','[Type]','[Link','[Name'])
print(out)
print('Template geometry and preserve-only package parts verified; retained reference unchanged.')
