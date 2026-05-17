// report/report.typ
// DevOps, Software Evolution and Software Maintenance — Final Report
// IT University of Copenhagen, Spring 2026
// Build: `typst compile report.typ build/BSc_group_X.pdf`

#set document(
  title: "ITU-MiniTwit — Group X Report",
  author: ("Alexander Rossau", "Member B", "Member C", "Member D", "Phillip Nikolai Rasmussen"),
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
    Alexander Rossau \<ross\@itu.dk\> \
    Member B \<id2\@itu.dk\> \
    Member C \<id3\@itu.dk\> \
    Member D \<id4\@itu.dk\> \
    Phillip Nikolai Rasmussen \<phir\@itu.dk\>
  ] \
  #v(2em)
  #text(size: 11pt)[
    BSc DevOps, Software Evolution and Software Maintenance \
    IT University of Copenhagen - Spring 2026
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
#author-tag("Alexander Rossau")

This report documents the design, operation, and evolution of _ITU-MiniTwit_, a Twitter-like microblogging platform built as part of the DevOps, Software Evolution and Software Maintenance course at ITU Copenhagen, Spring 2026. Users can register, post short messages ("cheeps"), follow other authors, and comment on posts. The system was re-implemented from a legacy Flask application into a C\#/ASP.NET stack and deployed on cloud infrastructure using Docker Swarm. The source code is hosted at #link("https://github.com/Anders0106/Devops2026")[GitHub], with work tracked via the #link("https://github.com/Anders0106/Devops2026/issues")[issue tracker]. Operational metrics are available in Grafana and logs are aggregated through Grafana/Loki.

// =========================
// 2. System's Perspective
// =========================
= System's Perspective

== Architecture and Design
#author-tag("Alexander Rossau")

*Allocation viewpoint.* @fig:deployment shows the production deployment. The
system runs as a Docker Swarm stack on a DigitalOcean droplet. This can be deployed in one command, using the included Terraform files in the `infrastructure` directory.

Caddy acts as a reverse proxy with IP-hash load balancing, forwarding HTTP traffic to the `chirp-web` application container. The application connects to a PostgreSQL~17 database over the internal Docker overlay network. Observability is co-located: Prometheus scrapes application metrics (exposed via `/metrics`) and a `postgres-exporter` sidecar; Promtail ships container logs to Loki; and Grafana queries both Prometheus and Loki for dashboards and alerting. Shepherd polls the GitHub Container Registry and performs rolling updates when a new image tagged `latest` is published. We've used this rolling-update pattern since the start of the project to enable zero-downtime deployments, with manual redeploys and database migrations being the exceptions where downtime did occur.

On our main deployment used in this course, we use Tailscale Funnel to expose the service to the internet on ports 80 and 443. This allows access to the service from anywhere, without exposing the server's public IP address directly. The tradeoff with this is that all requests take a latency penalty, as they are routed through Tailscale's network - which is not meant for production traffic. In a real production environment, we would use a custom domain and configure it in Caddy to handle the traffic.

#figure(
  image("images/deployment.png", width: 90%),
  caption: [Allocation viewpoint - UML deployment diagram of the production system.],
) <fig:deployment>

*Module viewpoint.* @fig:components illustrates the package decomposition. The codebase follows an onion architecture across four .NET projects. `Chirp.Core` defines the domain model (`Author`, `Cheep`, `Follow`, `Comment`) and DTOs with no outward dependencies. `Chirp.Repositories` provides Entity Framework Core access to PostgreSQL through `ChirpDBContext` and depends only on `Core`. `Chirp.Services` contains business logic (`CheepService`) and depends on repository interfaces. `Chirp.Razor` is the outermost layer, hosting ASP.NET Razor Pages for the web UI and an API controller that implements the simulator endpoints (`/msgs`, `/fllws`, `/register`). Prometheus counters (`ChirpMetrics`) are recorded in this layer. This separation allows the CI pipeline to run integration tests against a temporary database instance, without the web layer.

#figure(
  image("images/components.png", width: 80%),
  caption: [Module viewpoint - component/package decomposition.],
) <fig:components>

*Component & Connector viewpoint.* @fig:sequence traces the "post a cheep" flow through the system. A simulator `POST /msgs/{username}` request is received by Caddy, forwarded to the API controller, which validates the authorization header, resolves the author through `CheepService`, and delegates persistence to `CheepRepository`. After the cheep is inserted into PostgreSQL, the controller increments the `chirp_cheeps_created_total` Prometheus counter and returns `204 No Content`.

#figure(
  image("images/sequence.png", width: 85%),
  caption: [Component & Connector viewpoint - sequence diagram for posting a cheep.],
) <fig:sequence>

== Dependencies
#author-tag("Member B")

// #figure(
//   table(
//     columns: (auto, 1fr, 1fr),
//     align: (left, left, left),
//     table.header[*Layer*][*Tool / Technology*][*Purpose*],
//     [Runtime], [Bun + Elysia], [HTTP server, request handling],
//     [Data], [PostgreSQL @ PlanetScale], [Primary persistence],
//     [Infra], [Docker Swarm on Hetzner+OCI], [Hosting & orchestration],
//     [CI/CD], [GitHub Actions], [Build, test, deploy],
//     [Observability], [Grafana / Loki / Prometheus], [Metrics & logs],
//     [Secrets], [Doppler], [Secret distribution],
//   ),
//   caption: [Key dependencies, by layer.],
// ) <tab:deps>
// TODO: Add dependencies table (above is an example)

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
#author-tag("Anders Hansen")

What is monitored (golden signals + business metrics), collection mechanism,
dashboards. Link the dashboards.

When managing a website, Availability is key. Monitoring facilitates availability and is an important aid in noticing and diagnosing a problem with a web application. 

In this project we use Grafana for visualization and Prometheus to collect and provide the Data that is displayed. Prometheus scrapes the data from a frontend-, and a backend source.  The frontend data comes from an exposed endpoint on the website, "/metrics". This endpoint primarily provides business relevant metrics, such as how many cheeps or comments are created by users. Prometheus scrapes the backend data through postgres-exporter. This data can be split up intro reactive- and proactive Monitoring. We use reactive monitoring to see wether the database is active. Furthermore, we also use proactive monitoring to be able to identify problems in advance. Examples include monitoring the size of the database and also how high the cache hit rate is. E.g. when we monitor the database size we would be to set an alarm that notifies on 10% available space left and act on this - proactive monitoring.

#figure(
  image("images/PostgresMonitoring.png", width: 90%),
  caption: [Monitoring overview of postgres database],
) <fig:monitoring>

== Logging
#author-tag("Anders Hansen")

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
*maintenance* - each anchored to specific commits, PRs, and issues. Close with a
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
