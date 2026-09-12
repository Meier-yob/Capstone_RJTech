# Copilot Instructions

## Project Guidelines
- For Capstone_RJTech, System Developer authentication must use Clerk only, remain separate from Owner/Staff authentication, require server-side verification and the SystemDeveloper role, and never store the Clerk password in the RJTech database. Review architecture before implementation.
- Centralize owner identity and ownership data in the Owner model instead of ApplicationUser; avoid duplicating user fields across models. Authentication should use only necessary Owner fields, with no ApplicationUser model used for owner accounts.