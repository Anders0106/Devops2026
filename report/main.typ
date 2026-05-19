// report/report.typ
// DevOps, Software Evolution and Software Maintenance — Final Report
// IT University of Copenhagen, Spring 2026
// Build: `typst compile report.typ build/BSc_group_X.pdf`

#set document(
  title: "ITU-MiniTwit — Group C Report",
  author: ("Alexander Rossau", "Alexander Frederiksen", "David Nicholas Nielsen", "Anders Georg Frølich Hansen", "Phillip Nikolai Rasmussen"),
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
  #text(size: 13pt, weight: "bold")[Group C - youCanCUs] \
  //#text(size: 13pt, weight: "bold")[youCanCUs] \
  #v(1em)
  #text(size: 11pt)[
    Alexander Rossau \<ross\@itu.dk\> \
    Alexander Frederiksen \<alefr\@itu.dk\> \
    David Nicholas Nielsen \<davn\@itu.dk\> \
    Anders Georg Frølich Hansen \<ageh\@itu.dk\> \
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

This report documents the design, operation, and evolution of _ITU-MiniTwit_, a Twitter-like microblogging platform built as part of the DevOps, Software Evolution and Software Maintenance course at ITU Copenhagen, Spring 2026. Users can register, post short messages ("cheeps"), follow other authors, and comment on posts. The system was re-implemented from a legacy Flask application into a C\#/ASP.NET stack and deployed on cloud infrastructure using Docker Swarm. The source code is hosted at #link("https://github.com/Anders0106/Devops2026")[GitHub]. Operational metrics are available in Grafana and logs are aggregated through Grafana/Loki.

// =========================
// 2. System's Perspective
// =========================
= System's Perspective

== Architecture and Design
#author-tag("Alexander Rossau")

*Allocation viewpoint.* @fig:deployment shows the production deployment. The
system runs as a Docker Swarm stack on a DigitalOcean droplet. This can be deployed in one command, using the included Terraform files in the `infrastructure` directory.

Caddy acts as a reverse proxy with IP-hash load balancing, forwarding HTTP traffic to the `chirp-web` application container. The application connects to a PostgreSQL~17 database over the internal Docker overlay network. Observability is co-located: Prometheus scrapes application metrics (exposed via `/metrics`) and a `postgres-exporter` sidecar. Promtail ships container logs to Loki and Grafana queries both Prometheus and Loki for dashboards and alerting. Shepherd polls the GitHub Container Registry and performs rolling updates when a new image tagged `latest` is published. We have used this rolling-update pattern since the start of the project to enable zero-downtime deployments, with manual redeploys and database migrations being the exceptions where downtime did occur.

On our main deployment used in this course, we use Tailscale Funnel to expose the service to the internet on ports 80 and 443. This allows access to the service from anywhere, without exposing the server's public IP address directly. The tradeoff with this is that all requests take a latency penalty, as they are routed through the Tailscale network, which is not meant for production traffic. In a real production environment, we would use a custom domain and configure it in Caddy to handle the traffic.

#figure(
  image("images/deployment.png", width: 90%),
  caption: [Allocation viewpoint - UML deployment diagram of the production system.],
) <fig:deployment>

*Module viewpoint.* @fig:components illustrates the package decomposition. The codebase follows an onion architecture across four .NET projects. `Chirp.Core` defines the domain model (`Author`, `Cheep`, `Follow`, `Comment`) and DTOs with no outward dependencies. `Chirp.Repositories` provides Entity Framework Core access to PostgreSQL through `ChirpDBContext` and depends only on `Core`. `Chirp.Services` contains business logic (`CheepService`) and depends on repository interfaces. `Chirp.Razor` is the outermost layer, hosting ASP.NET Razor Pages for the web UI and an API controller that implements the simulator endpoints (`/msgs`, `/fllws`, `/register`). Prometheus counters (`ChirpMetrics`) are recorded in this layer. This separation allows the CI pipeline to run integration tests against a temporary database instance, without the web layer.

#figure(
  image("images/components.png", width: 100%),
  caption: [Module viewpoint - component/package decomposition.],
) <fig:components>

*Component & Connector viewpoint.* @fig:sequence traces the "post a cheep" flow through the system. A simulator `POST /msgs/{username}` request is received by Caddy, forwarded to the API controller, which validates the authorization header, resolves the author through `CheepService`, and delegates persistence to `CheepRepository`. After the cheep is inserted into PostgreSQL, the controller increments the `chirp_cheeps_created_total` Prometheus counter and returns `204 No Content`.

#figure(
  image("images/sequence.png", width: 100%),
  caption: [Component & Connector viewpoint - sequence diagram for posting a cheep.],
) <fig:sequence>

#pagebreak()

== Dependencies
#author-tag("Alexander F")

#figure(
   table(
     columns: (auto, 1fr, 1fr),
     align: (left, left, left),
     table.header[*Layer*][*Tool / Technology*][*Purpose*],
     [Runtime], [ASP.NET Core (.NET 8)], [HTTP server, request handling],
     [ORM], [Entity Framework Core], [Database access],
     [Data], [PostgreSQL (Docker container, accessed via Npgsql and EF Core)], [Primary persistence],
     [Containerization], [Docker], [Service isolation & deployment],
     [Infra], [Docker Swarm + Terraform (DigitalOcean)], [Hosting & orchestration],
     [CI/CD], [GitHub Actions], [Build, test, deploy],
     [Observability (Monitoring)], [Prometheus], [Metrics collection],
     [Observability (Logging)], [Loki + Promtail], [Log aggregation],
     [Observability (Visualization)], [Grafana], [Dashboard visualization],
     
     //[Secrets], [Doppler], [Secret distribution],
   ),
   caption: [Key dependencies, by layer.],
 ) <tab:deps>
// TODO: Add dependencies table (above is an example)


Our MiniTwit system is built in C\# and runs with ASP.NET Core via .NET as our web framework. The data is stored in PostgreSQL database running in a Docker container using the Npgsql provider. Our program is containerized using Docker.

The system is deployed using Docker Swarm, and the infrastructure is from using Terraform.

For observability, we are using Prometheus to collect metrics from the program, while Loki and Promtail handle the log aggregation and Grafana is used to visualize both the metrics and the logs through its dashboards.

== Current State
#author-tag("Alexande F")

Our system uses GitHub Actions for continuous integration and automated validation. The CI pipeline makes static analysis, automated builds, database-backed testing, and browser-based integration testing with every commit.

The project contains four different kinds of automated tests: unit tests, integration tests, UI tests, and end-to-end tests. These tests are done in the CI pipeline using `make test`. 

Static analysis and security scanning are done using Semgrep with rulesets targeting C\#, Dockerfiles, and the OWASP Top 10. The CI workflow also provisions a PostgreSQL container during testing to support the integration tests against a database environment. The end-to-end testing includes a Playwright browser automation test for the web UI.

The system as it is right now contains 34 build warnings like 'nullable-reference warnings', 'logging-analysis warnings', 'unused-variable warnings' and 'test-analyzer warnings'. In addition to 1 known package warning (`NU1903` related to the `Microsoft.Build 17.8.3`). These warnings mostly concern nullable-reference handling and package dependency issues. Even though these warnings are present, our system builds fine and runs successfully.

Our repository has 3 GitHub Actions workflows (`ci.yml`, `devskim.yml`, and `release-docker.yml`) supporting the automated testing, the security scanning, and also the deployment processes.


#figure(caption: [Warnings part 1], 
  image("images/warnings1.png")
)
#figure(caption: [Warnings part 2], 
  image("images/warnings2.png")
)

// =========================
// 3. Process Perspective
// =========================
= Process Perspective

== CI/CD Pipeline
#author-tag("David Nicholas Nielsen")

The CI/CD pipeline is implemented using the following tools:
- We use Git and GitHub for version control and code hosting.
- We use GitHub Actions for automated testing and building the application.
- We use Docker for containerization, GitHub Container Registry for container image hosting, and Docker Swarm for orchestration and deployment.
- We use Terraform for infrastructure-as-code (IaC), provisioning our DigitalOcean droplet and Docker Swarm cluster.

Regarding GitHub Actions, we have set up the following workflows:
1. *CI*: runs on every push and pull request, executing tests, as well as static analysis with Semgrep. It enforces quality gates by failing if tests do not pass or if Semgrep finds critical issues.
2. *DevSkim Security Scan*: runs on every push to the `main` branch, scanning for security issues with DevSkim and failing if any are found.
3. *Build and Push Docker Image on Release*: runs whenever a new release is published, building a new Docker image, pushing it to GitHub Container Registry.

The pipeline is illustrated in the figure below.
#figure(caption: [CI/CD Pipeline. This diagram shows the flow from local code changes to deployment. Note that other containers are also running on the server, such as those used for the database and the monitoring stack.], 
  image("images/CICD.png")
)

== Deployment and Release
#author-tag("David Nicholas Nielsen")

Docker Swarm is configured to automatically update to use the newest available release of ITU-MiniTwit. We use a tool called _Shepherd_ that periodically checks for new releases. If a new release is found, it performs a rolling update of the stack, meaning that the new version is deployed to one replica at a time. In case of a failed deployment, the stack will automatically roll back to the previous version.

== Availability and Scaling
#author-tag("David Nicholas Nielsen")

Currently, the system always creates exactly one replica of each service, and if a replica fails, the container is restarted. To increase availability, we could scale horizontally by increasing the number of replicas and use Caddy for load-balancing. The number of replicas could be set to a higher number, or it could be adjusted dynamically based on the load. This would allow the system to handle more traffic and provide tolerance in case of failures. Of course, in a real-world scenario, simply increasing the number of replicas may not be enough to ensure high availability, as we would need to consider other possible bottlenecks, such as the single-replica database.

//Replicas, autoscaling, multi-region setup, failure modes, recovery procedures.

== Monitoring
#author-tag("Anders Georg Frølich Hansen")

When managing a website, availability is key. Monitoring facilitates availability and is an important aid in noticing and diagnosing a problem with a web application. 

In this project we use Grafana for visualization and Prometheus to collect and provide the data that is displayed. Prometheus scrapes the data from a frontend-, and a backend source. The frontend data comes from an exposed endpoint on the website, "/metrics". This endpoint primarily provides business relevant metrics, such as how many cheeps or comments are created by users. Prometheus also scrapes the backend data through postgres-exporter. This data can be split up into reactive- and proactive monitoring. We use reactive monitoring to see whether the database is active. Furthermore, we also use proactive monitoring to be able to identify problems in advance. Examples include monitoring the size of the database and also how high the cache hit rate is. For an example of proactive monitoring, when we monitor the database size we could set an alarm that notifies on 10% available disk space.

#figure(
  image("images/PostgresMonitoring.png"),
  caption: [Monitoring overview of postgres database],
) <fig:monitoring>

== Logging
#author-tag("Anders Georg Frølich Hansen")

Just as monitoring is key for availability, logging is key for diagnosing a problem. For this project we use the built-in logging functionality in the .NET library. Promtail collects these logs from the docker containers together with OS logs. They are then all sent to Loki, which is responsible for aggregating and storing them. At last, the logs are displayed chronologically in Grafana.

The logs are sent as structured JSON objects and include information that can help us debug/recall what has happened, such as: containerid, POST/GET, endpoint, response time, IP-address, and a description of what was logged. 

We log many different things. Below are some grouped examples:

*Security events* #linebreak()
A user fails/succeeds to register #linebreak()
A user fails/succeeds to login #linebreak()

*business-critical operations* #linebreak()
A users timeline is accessed #linebreak()
A user follows/unfollows another user #linebreak()
A user creates/deletes a cheep #linebreak()

*Errors* #linebreak()
Exceptions #linebreak()

*System events* #linebreak()
System logs #linebreak()

Performance can also be tracked through responstime time, if this is too slow, something could be wrong and a request is instead logged as the type - warning.


== Security Hardening
#author-tag("Anders Georg Frølich Hansen")

Security is important, especially since our application contains sensetive information such as passwords. The following sections describes how we try to implement the defense in depth model.

*TLS* #linebreak()
All communication between client and server is secured using HTTPS, ensuring that data is encrypted in transit.

*Hashing* #linebreak()
User passwords are never stored in plain text. Instead, they are stored using salted hashing.

*Secret scanning and vulnerability checks* #linebreak()
As part of the CI/CD pipeline, we automatically scan the codebase for accidentally committed secrets such as passwords. This is done using DevSkim and Semgrep with rules based on OWASP. These tools help identify common vulnerabilities such as SQL injection.

*Network* #linebreak()
All communication between clients and the server happens through a reverse proxy. This adds an additional security layer and the opportunity to filter various malicioius requests before they ever reach the server. E.g. ddos attacks by maximising amount of requests from one IP-adress.

We keep as few ports open as possible. In figure @fig:firewallRules our firewall rules can be seen.

#figure(
  image("images/FirewallRules.png"),
  caption: [Overview of what user each container from docker swarm is run as],
) <fig:firewallRules>

*Least privileges* #linebreak()
Every container is run with least possible privileges. As seen in @fig:ContainerUser, most containers are not run as root.

#figure(
  image("images/ContainerUser.png"),
  caption: [Overview of what user each container from docker swarm is run as],
) <fig:ContainerUser>

// =========================
// 4. Reflection Perspective
// =========================
= Reflection Perspective
#author-tag("Phillip Nikolai Rasmussen")

We had some issues figuring out why a lot of follow and cheep API requests were failing. It ended up being a culmination of many small issues not considered when the functions were first created, such as case-sensitive usernames and whitespace handling. This can be seen in commits #link("https://github.com/Anders0106/Devops2026/commit/3428504")[`3428504`], #link("https://github.com/Anders0106/Devops2026/commit/4b511f6")[`4b511f6`], and #link("https://github.com/Anders0106/Devops2026/commit/f05129f")[`f05129f`]. The issue still persisted, so we added logging in commit #link("https://github.com/Anders0106/Devops2026/commit/5d61e5d")[`5d61e5d`] to highlight exactly where things were going wrong.

Another big issue was the migration from SQLite to PostgreSQL (commit #link("https://github.com/Anders0106/Devops2026/commit/315f361")[`315f361`]). SQLite had been handling certain values case-insensitively, which PostgreSQL does not, so we needed a conversion script to fix the existing data (commit #link("https://github.com/Anders0106/Devops2026/commit/be71171")[`be71171`]). On top of that, a lot of tests broke, leading to a string of quick fixes from commits #link("https://github.com/Anders0106/Devops2026/commit/65de226")[`65de226`] to #link("https://github.com/Anders0106/Devops2026/commit/970e37c")[`970e37c`], not fully resolved until #link("https://github.com/Anders0106/Devops2026/commit/8c816e2")[`8c816e2`] two weeks later. This caused downtime for our application.

During the maintenance phase, we went through a round of security hardening after realizing the system had some gaps. In a series of commits we added Caddy as a reverse proxy (#link("https://github.com/Anders0106/Devops2026/commit/9adb05c")[`9adb05c`]), bound all service ports to localhost (#link("https://github.com/Anders0106/Devops2026/commit/356062b")[`356062b`]), removed root as the running user from our containers (#link("https://github.com/Anders0106/Devops2026/commit/4113645")[`4113645`]), and added Semgrep for static security analysis (#link("https://github.com/Anders0106/Devops2026/commit/ee46c2e")[`ee46c2e`]). Going through this as a dedicated step made it clear how many small attack surfaces a running system can accumulate without much notice.

The DevOps style of our work was focused on improving the CI/CD flow constantly throughout development. The focus was on fixing issues and making sure that if they reappear, we know right away. Also trying to automate as many things as possible, which made the codebase much easier to work with as the project progressed.

// =========================
// 5. Use of Generative AI
// =========================
= Use of Generative AI
#author-tag("Phillip Nikolai Rasmussen")

During the project we used Claude (Sonnet 4.6 and Opus 4.6) and GPT-5.4 as generative AI assistants. The two providers were used interchangeably, choosing whichever gave cleaner output for a given task.

*Learning new tooling* Terraform and Docker Swarm were new to the team. Rather than reading documentation from scratch, we used AI to get targeted explanations and working examples we could adapt to our setup. This significantly reduced the time needed to become productive with each tool.

*Boilerplate generation* Integrating Prometheus, Loki, and Grafana involves a lot of repetitive configuration. AI generated base templates that we then reviewed and adjusted, avoiding the most mechanical parts of the work.

*Test fixes* Approximately 15 existing tests broke during the initial migration. AI diagnosed the failures and proposed fixes quickly. Not a hard task, but one that would have consumed more time without assistance.

*Logging instrumentation.* Once we agreed on a logging convention, AI applied it consistently across the codebase, turning a tedious but low-value task into a matter of minutes.

AI improved our velocity noticeably. The clearest benefit was lowering the barrier to unfamiliar tooling, because getting a working starting point is often the hardest step. The main drawback was overconfident output, particularly with Terraform provider-specific syntax, which occasionally introduced subtle errors we only caught through testing. This taught us to treat AI output as a draft to verify rather than a finished answer, and to always cross-reference official documentation for infrastructure-critical configuration.


// =========================
// References (optional)
// =========================
// #bibliography("references.bib", style: "ieee")
