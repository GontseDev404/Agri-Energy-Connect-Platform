# Agri-Energy Connect

Agri-Energy Connect is an individual academic ASP.NET Core MVC project developed as part of a Bachelor of Computer and Information Sciences programme. It models how farmers and employees could manage products, community activity and accountable system records in one application.

The project demonstrates role-aware workflows, relational persistence, application validation, localisation, audit-related behaviour and automated testing. It is a local academic prototype, not a hosted commercial product or a claim of enterprise production readiness.

## Project context

- **Ownership:** Individual academic project
- **Technology:** ASP.NET Core MVC on .NET 9.0, Entity Framework Core, SQLite and ASP.NET Core Identity
- **Interface:** Razor views with Bootstrap, jQuery, AJAX and Chart.js
- **Roles:** Farmer, Employee and Admin paths are implemented in the application
- **Languages:** English (South Africa), Afrikaans and isiZulu resources
- **Public repository:** [GontseDev404/Agri-Energy-Connect-Platform](https://github.com/GontseDev404/Agri-Energy-Connect-Platform)

## What the project demonstrates

### Product and farmer workflows

- Farmers can create and manage product records through role-scoped routes.
- Employees can review and manage relevant farmer and product records.
- The application validates ownership and input before persisting changes.

### Community and dashboard features

- Forum posts and replies provide a community discussion flow.
- Role-aware dashboards expose product and activity information.
- Chart.js is used for the dashboard visualisations present in the project.

### Identity and audit-related behaviour

- ASP.NET Core Identity provides authentication and role-based access paths.
- Product and related actions are connected to application audit activity.
- The audit trail is an implemented project feature; it is not presented as a compliance certification or a production security guarantee.

### Localisation and interface work

- Resource files support English (South Africa), Afrikaans and isiZulu.
- The interface includes responsive styling, labels and selected semantic/ARIA affordances.
- No independent WCAG conformance audit is claimed by this repository.

## Architecture

The application uses an MVC structure with EF Core persistence and dedicated service boundaries for identity, notifications and audit-related behaviour.

~~~
Identity and role paths
        |
MVC controllers and Razor views
        |
Application services and validation
        |
EF Core DbContext and SQLite
        |
Farmers, products, forum records, notifications and audit records
~~~

The repository layout separates controllers, data, models, services, views, localisation resources and Identity pages.

### Technologies used

- ASP.NET Core MVC and .NET 9.0
- Entity Framework Core with SQLite
- ASP.NET Core Identity
- Bootstrap 5, jQuery and AJAX
- Chart.js
- Custom CSS and responsive layout work

## Run locally

The default development path uses a local SQLite database. No hosted demo environment is claimed.

~~~
dotnet restore .\ST10038937_prog7311_poe1.sln
dotnet run --project .\ST10038937_prog7311_poe1\ST10038937_prog7311_poe1.csproj
~~~

Open the local URL printed by ASP.NET Core. The exact port can vary by the local launch profile.

If the local database needs to be recreated, stop the application and remove the local app.db file. EF Core will recreate it when the application starts and the seed routine runs.

### Local-only seed accounts

The seed routine creates disposable development accounts so the role-scoped workflows can be exercised in an isolated checkout. The public README does not publish their passwords. Do not reuse any seeded development credential outside local testing, and change the seed values before using the code in any non-local environment.

## Verification evidence

An isolated local validation run passed **18/18 xUnit and WebApplicationFactory tests on 28 May 2026** after repairing the MVC cookie/JWT authentication split, Identity Razor Page tag-helper imports and source-quality warnings.

That result is dated local evidence. It does not prove a current deployment, public hosting, load capacity, high availability, formal security compliance or accessibility conformance.

For a fresh local check:

~~~
dotnet test .\ST10038937_prog7311_poe1.Tests\ST10038937_prog7311_poe1.Tests.csproj
~~~

## Known boundaries

- The project is an academic prototype and has not been presented as a live commercial system.
- The checked-in development configuration uses SQLite rather than a remotely managed database.
- No AWS, Azure or other cloud deployment is claimed by this repository.
- Reliability, scalability, penetration testing and formal accessibility compliance have not been independently established here.
- The seeded accounts are for isolated local development only.

## Project structure

- Controllers — MVC and API controllers
- Data — DbContext, seed logic and migrations
- Models — ApplicationUser, Product, Farmer, ForumPost, PostReply and AuditLog
- Services — identity, notification, audit and supporting services
- Views — Razor views
- Resources — localisation resources
- Areas/Identity — customised Identity pages
- wwwroot — CSS, JavaScript, images and client libraries

## Development approach

The project was developed iteratively as an academic prototype, with feature work covering product management, forum flows, audit activity, localisation and responsive interface changes. This describes the development approach, not a claim of a formal production delivery process.

## Troubleshooting

- If the local database needs to be recreated, stop the application and remove app.db before starting it again.
- If dependencies need to be restored, run dotnet restore from the repository root.
- If the application port is already in use, stop the previous local process or use the port printed by the active launch profile.

## License

This project is licensed under the MIT License. See the LICENSE file for details.

---

Agri-Energy Connect — academic project, 2025
