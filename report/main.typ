// report/report.typ
// DevOps, Software Evolution and Software Maintenance — Final Report
// IT University of Copenhagen, Spring 2026
// Build: `typst compile report.typ build/BSc_group_X.pdf`

#set document(
  title: "ITU-MiniTwit — Group X Report",
  author: ("Member A", "Member B", "Member C", "Member D", "Member E"),
)

#set page(
  paper: "a4",
  margin: (x: 2.5cm, y: 2.5cm),
  numbering: "1 / 1",
  number-align: center,
)

#set text(font: "New Computer Modern", size: 11pt, lang: "en")
#set par(justify: true, leading: 0.65em)
#set heading(numbering: "1.1")

// Slightly smaller, italic figure captions
#show figure.caption: set text(size: 9.5pt, style: "italic")

// Code listings
#show raw.where(block: true): set block(
  fill: luma(245),
  inset: 8pt,
  radius: 4pt,
  width: 100%,
)
#show raw: set text(font: "DejaVu Sans Mono", size: 9.5pt)

// Required by exam: per-section authorship callout
#let author-tag(name) = box(
  inset: (x: 6pt, y: 2pt),
  fill: luma(230),
  radius: 3pt,
  text(size: 9pt, style: "italic", [Author: #name]),
)

// =========================
// Title page
// =========================
#align(center + horizon)[
  #text(size: 22pt, weight: "bold")[ITU-MiniTwit] \
  #v(0.4em)
  #text(size: 14pt)[A Report on Design, Operation, and Evolution] \
  #v(2em)
  #text(size: 13pt, weight: "bold")[Group X] \
  #v(1em)
  #text(size: 11pt)[
    Member A \<id1\@itu.dk\> \
    Member B \<id2\@itu.dk\> \
    Member C \<id3\@itu.dk\> \
    Member D \<id4\@itu.dk\> \
    Member E \<id5\@itu.dk\>
  ] \
  #v(2em)
  #text(size: 11pt)[
    BSc DevOps, Software Evolution and Software Maintenance \
    IT University of Copenhagen --- Spring 2026
  ] \
  #v(0.5em)
  #text(size: 11pt)[#datetime.today().display("[day]/[month]/[year]")]
]

#pagebreak()

#outline(title: [Contents], indent: auto)
#pagebreak()

// =========================
// 1. Introduction
// =========================
= Introduction
#author-tag("Member A")

One paragraph framing the project, the team's high-level goals, and pointers to:
the main repository, issue tracker, monitoring dashboard, and logging dashboard.
~100 words.

// =========================
// 2. System's Perspective
// =========================
= System's Perspective

== Architecture and Design
#author-tag("Member A")

Describe the architecture using three viewpoints. If you invent a notation, include
a legend.

// #figure(
//   image("images/deployment.png", width: 90%),
//   caption: [Allocation viewpoint --- UML deployment diagram of the production system.],
// ) <fig:deployment>

// #figure(
//   image("images/components.png", width: 80%),
//   caption: [Module viewpoint --- component/package decomposition.],
// ) <fig:components>

// #figure(
//   image("images/sequence.png", width: 85%),
//   caption: [Component & Connector viewpoint --- sequence diagram for a representative request.],
// ) <fig:sequence>

== Dependencies
#author-tag("Member B")

#figure(
  table(
    columns: (auto, 1fr, 1fr),
    align: (left, left, left),
    table.header[*Layer*][*Tool / Technology*][*Purpose*],
    [Runtime], [Bun + Elysia], [HTTP server, request handling],
    [Data], [PostgreSQL @ PlanetScale], [Primary persistence],
    [Infra], [Docker Swarm on Hetzner+OCI], [Hosting & orchestration],
    [CI/CD], [GitHub Actions], [Build, test, deploy],
    [Observability], [Grafana / Loki / Prometheus], [Metrics & logs],
    [Secrets], [Doppler], [Secret distribution],
  ),
  caption: [Key dependencies, by layer.],
) <tab:deps>

== Current State
#author-tag("Member B")

Static analysis findings (Semgrep / ESLint / Sonar), test coverage, technical-debt
hotspots, and quality gates enforced in CI. Cite concrete numbers.

// =========================
// 3. Process Perspective
// =========================
= Process Perspective

== CI/CD Pipeline
#author-tag("Member C")

// #figure(
//   image("images/cicd.png", width: 95%),
//   caption: [CI/CD pipeline from commit to production.],
// ) <fig:cicd>

End-to-end stages and tools, including triggers, gates, and artifacts produced.

== Deployment and Release
#author-tag("Member C")

Versioning scheme, environments, rollout strategy, rollback. Link a representative
release.

== Availability and Scaling
#author-tag("Member C")

Replicas, autoscaling, multi-region setup, failure modes, recovery procedures.

== Monitoring
#author-tag("Member D")

What is monitored (golden signals + business metrics), collection mechanism,
dashboards. Link the dashboards.

== Logging
#author-tag("Member D")

What is logged, structured-log conventions, aggregation, retention. Link the logging UI.

== Security Hardening
#author-tag("Member D")

Threat-model summary and concrete mitigations: TLS, secrets management, dependency
scanning, container hardening, branch protection, SAST/DAST. Reference any incidents
handled.

// =========================
// 4. Reflection Perspective
// =========================
= Reflection Perspective
#author-tag("Member E")

Biggest issues with respect to *evolution & refactoring*, *operation*, and
*maintenance* --- each anchored to specific commits, PRs, and issues. Close with a
paragraph on the team's "DevOps style".

// =========================
// 5. Use of Generative AI
// =========================
= Use of Generative AI
#author-tag("Member E")

Tools used, tasks they were applied to, how they were used, and a brief reflection
on whether they helped or hindered. Follow ITU's GenAI guidelines.

// =========================
// References (optional)
// =========================
// #bibliography("references.bib", style: "ieee")
