# FollowerAppService

Legacy multi-project C# solution demonstrating background services,
automation workflows, and data processing for social media–related systems.

> **Status:** Legacy / archived project  
> **Note:** This repository does not contain production secrets. Configuration is expected to be provided via local settings or environment variables.

---

## Overview

`FollowerAppService` is a Visual Studio solution composed of multiple services and utilities
designed to explore automation, background processing, and data persistence patterns
commonly used in long-running backend systems.

The project focuses on service orchestration, separation of concerns, and interaction
with external systems using both API-based and browser-driven approaches.

---

## Solution Structure

The solution consists of multiple projects, each responsible for a specific area:

- **FlwService** – Core service layer and orchestration logic  
- **FlwSaver** – Data persistence and storage of collected information  
- **FlwMetricService** – Processing and aggregation of metrics  
- **FlwDatabase** – Database access layer and data models  
- **InstaWebJob** – Background job / worker-style execution  
- **Instagram.Api** – External system integration layer  
- **InstagramSelenium** – Browser automation using Selenium  
- **InstagramModels** – Shared domain models  
- **FollowerForm** – Windows Forms–based administrative UI  
- **InstagramTest** – Test and experimental components  

---

## Architecture Highlights

- Multi-project Visual Studio solution
- Background job and worker-style execution
- Separation of concerns across services and layers
- External system integration (API and automation)
- Shared domain models across multiple components

---

## Technology Stack

- **Language:** C#  
- **Framework:** .NET Framework  
- **IDE:** Visual Studio  
- **UI:** Windows Forms  
- **Automation:** Selenium  
- **Data Access:** ADO.NET / project-specific persistence  
- **Solution Type:** Monolithic solution with service-oriented components  

---

## Configuration & Secrets

- No secrets are stored in the repository
- Runtime configuration is expected to be provided externally
- Sensitive files are excluded via `.gitignore`

---

## Notes for Reviewers

This is a **legacy and experimental project**, preserved to demonstrate
real-world service decomposition, automation workflows, and backend architecture
beyond simple demo applications.

The codebase is not actively modernized and is intended for architectural reference.

---
