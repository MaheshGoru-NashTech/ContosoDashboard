# ContosoDashboard Constitution

<!-- Sync Impact Report
Version change: 0.0.0 -> 1.0.0
Modified principles: n/a (new constitution)
Added sections: Core Principles, Project Constraints, Development Workflow, Governance
Removed sections: none
Deferred items: none
-->

## Core Principles

### I. Security and Authorization by Default
All features and data access paths MUST enforce authentication, authorization, and least-privilege behavior before any user-facing capability is considered complete. This project is a training system and MUST model safe access patterns: protected pages require explicit authorization, service-layer checks MUST validate identity and scope, and every new route or data operation MUST protect against IDOR and privilege escalation.

Rationale: the repository is explicitly designed for learning secure application patterns; security is part of the product, not a late-stage add-on.

### II. User-Centered Collaboration and Clarity
The dashboard MUST serve understandable, role-appropriate workflows for administrators, project managers, team leads, and employees. User-visible features MUST be clear, consistent, and accessible, with naming, status, and permissions aligned to the business model.

Rationale: a dashboard is only valuable when people can safely interpret ownership, priorities, and system state without ambiguity.

### III. Test-First and Verifiable Changes
Any behavior change MUST be backed by the smallest relevant automated or manual verification before merge. New features, security fixes, and bug regressions MUST have a clear test or validation step that demonstrates the intended behavior and catches the failure mode.

Rationale: in a training project, correctness must be visible, reproducible, and easy to review.

### IV. Architecture Must Remain Simple and Maintainable
Core logic MUST remain separated by responsibility: models, data access, services, and UI concerns MUST NOT be mixed into single files or layers. Reuse MUST be deliberate, dependencies MUST be explicit, and new abstractions require a clear reason.

Rationale: this codebase is intentionally structured as a teaching example; maintainability is a first-class requirement.

### V. Change Control and Operational Transparency
Changes to configuration, data models, authentication flow, or user-visible behavior MUST be documented in the repository and communicated in a way that makes impact, rollback, and validation clear. Every significant change MUST preserve the offline-training default behavior and document any migration path to production-like infrastructure.

Rationale: the project is a training artifact with explicit offline scope and migration guidance.

## Project Constraints
This repository is for training use only and MUST NOT be treated as production guidance without explicit review. The application may use mock authentication, local data storage, and sample seed data; any production-oriented change MUST preserve the training-safe defaults and clearly call out the required security and operational controls.

The project MUST remain offline-friendly and self-contained. External service dependencies are not required for the default developer workflow, and new features MUST NOT silently introduce cloud-only assumptions or security shortcuts. Code may demonstrate simplified architecture, but it MUST not bypass security, validation, or auditability.

## Development Workflow
All work in this repository MUST follow a simple reviewable lifecycle: define the user need, implement the minimal change, verify the change against the intended behavior, and document any operational impact.

Feature work MUST be broken into small, reviewable increments. Security-sensitive changes, authorization updates, and data-access changes require explicit validation before merge. Database and service-layer changes MUST remain compatible with the local training setup and document any required seed or migration adjustments.

Documentation is part of the implementation. Any change that alters user flows, authorization behavior, data shape, or architecture MUST include the corresponding adjustment to the repository guidance and examples.

## Governance
This constitution defines the non-negotiable standards for the repository. Any deviation requires documented rationale, explicit approval from the maintainers or reviewers, and a plan for remediation or migration. This document supersedes ad hoc practices when they conflict.

All pull requests and review activities MUST verify compliance with the applicable principles before merge. Reviewers MUST check for correctness, security, clarity, and alignment with the repository’s training purpose. Complexity MUST be justified, and risk MUST be explained when a design departs from the default simple patterns.

Amendments to this constitution MUST follow these rules:
1. Propose the amendment with a clear rationale and impact assessment.
2. Update the version according to the semantic versioning policy below.
3. Record the amendment in the constitution and include the dated governance summary.
4. Ensure the updated principles remain consistent with the project’s training purpose and security posture.

Versioning policy:
- MAJOR: backward-incompatible governance or principle changes that remove or redefine core requirements.
- MINOR: new principles, major expansions, or materially stronger governance requirements.
- PATCH: clarifications, wording fixes, and non-semantic improvements.

Compliance review expectations:
- Reviewers MUST verify security and authorization patterns before approving changes.
- Architecture and maintainability decisions MUST remain explainable and consistent with repository responsibilities.
- User-visible workflow changes MUST be evaluated for clarity, accessibility, and role alignment.
- Any documentation update that changes required behavior MUST be reviewed as part of the same change.

**Version**: 1.0.0 | **Ratified**: 2026-09-18 | **Last Amended**: 2026-09-18
