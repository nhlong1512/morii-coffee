# Feature Specification: Remove AWS SES Email Provider Support

**Feature Branch**: `002-remove-aws-ses`
**Created**: 2026-03-27
**Status**: Draft
**Input**: User description: "Refer the 001-email-social-auth, please remove the logic of AWS SES, I just only integrate with Sendgrid"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Simplified Email Configuration (Priority: P1)

As a developer deploying the Morii Coffee application, I need to configure only SendGrid credentials without dealing with AWS SES options, so that the configuration process is simpler and there are no unused settings cluttering the configuration files.

**Why this priority**: This is the core simplification effort. Removing AWS SES reduces configuration complexity, eliminates confusion about which provider to use, and ensures the codebase only contains code that's actually being used. This directly impacts developer experience during deployment and maintenance.

**Independent Test**: Can be fully tested by deploying the application with only SendGrid configuration (no AWS SES credentials), starting the application successfully, sending test emails via the welcome and password reset flows, and confirming that emails are delivered without any AWS-related errors or warnings in the logs.

**Acceptance Scenarios**:

1. **Given** the application is configured with only SendGrid settings, **When** a new user signs up, **Then** a welcome email is sent successfully via SendGrid without any AWS SES fallback logic being triggered
2. **Given** the EmailSettings configuration section is examined, **When** a developer reviews appsettings.json, **Then** only SendGrid-related configuration properties are present (no AwsSes section)
3. **Given** the application starts up, **When** the dependency injection container resolves IEmailService, **Then** SendGridEmailService is registered directly without any provider switching logic

---

### User Story 2 - Cleaner Codebase (Priority: P2)

As a developer maintaining the Morii Coffee codebase, I want AWS SES-related code removed entirely, so that the codebase is leaner, easier to understand, and contains only actively used components.

**Why this priority**: Code cleanliness and maintainability are important but secondary to functional simplification. Removing unused code reduces cognitive load for future developers, eliminates dead code that could confuse AI code assistants or new team members, and reduces the surface area for potential bugs.

**Independent Test**: Can be fully tested by performing a global code search for "SES", "AwsSes", and AWS email-related terms across the entire solution and confirming that no AWS SES implementation files, configuration classes, or provider-switching logic remain in the codebase.

**Acceptance Scenarios**:

1. **Given** the codebase is searched for "AwsSes", **When** the search completes, **Then** no references to AWS SES configuration classes, service implementations, or NuGet packages are found
2. **Given** the EmailSettings class is reviewed, **When** a developer examines the source code, **Then** only SendGrid-related properties and no AwsSes options class are present
3. **Given** the DependencyInjection.cs file is reviewed, **When** the IEmailService registration is examined, **Then** only SendGridEmailService is registered without any provider-switching switch statement

---

### User Story 3 - No Email Service Disruption (Priority: P1)

As an end user of the Morii Coffee application, I expect to continue receiving welcome emails and password reset emails exactly as before, so that my user experience is not impacted by internal refactoring.

**Why this priority**: User-facing functionality must never regress. While this is technically a refactoring task (removing unused code), it's critical that existing email delivery continues to work identically to how it worked before. Any disruption would be a production issue.

**Independent Test**: Can be fully tested by executing the same email test scenarios from feature 001-email-social-auth (welcome email on signup, password reset email on forgot-password) and verifying that all emails are delivered with identical content, timing, and branding as before the AWS SES removal.

**Acceptance Scenarios**:

1. **Given** a user completes sign-up, **When** their account is created, **Then** they receive the same branded welcome email via SendGrid as they did before the AWS SES removal
2. **Given** a user requests a password reset, **When** the request is submitted, **Then** they receive the same branded password reset email via SendGrid as they did before the AWS SES removal
3. **Given** the email service encounters an error, **When** SendGrid fails to send, **Then** the application logs the error and continues functioning exactly as it did before the AWS SES removal (no new error types introduced)

---

### Edge Cases

- **SendGrid API unavailable**: What happens when SendGrid service is down? The application continues to function normally, email send operations fail gracefully with logged errors, and no AWS SES fallback is attempted (behavior identical to current implementation).

- **Missing SendGrid configuration**: What happens when SendGrid API key is not configured in appsettings.json? The application fails to start with a clear error message indicating that SendGrid configuration is required.

- **Migration from existing deployment**: What happens to a currently running production instance using SendGrid? No changes are required to existing SendGrid configuration; the application continues to work identically since AWS SES was never actually used in production.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST continue sending welcome emails via SendGrid with no change in delivery behavior, timing, or email content
- **FR-002**: System MUST continue sending password reset emails via SendGrid with no change in delivery behavior, timing, or email content
- **FR-003**: System MUST register SendGridEmailService directly as the IEmailService implementation without any provider-switching logic
- **FR-004**: System MUST remove all AWS SES configuration classes, options, and properties from the EmailSettings model
- **FR-005**: System MUST remove any AWS SES NuGet packages or SDK dependencies from the Infrastructure project
- **FR-006**: Configuration files (appsettings.json) MUST contain only SendGrid-related settings under the EmailSettings section
- **FR-007**: System MUST fail to start with a clear error message if required SendGrid configuration is missing
- **FR-008**: All existing email functionality from feature 001-email-social-auth MUST continue to work identically after AWS SES removal

### Key Entities

- **EmailSettings**: Simplified configuration model containing only SendGrid-related properties (ApiKey), global email properties (FromEmail, FromName, StorefrontUrl, ResetPasswordBaseUrl), and no AWS SES-related properties.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero AWS SES-related code remains in the codebase (verified by global search returning no results for "AwsSes", "SES", or AWS email SDK namespaces)
- **SC-002**: EmailSettings configuration model is simplified to contain only SendGrid options and global settings (no AwsSes property)
- **SC-003**: Application starts successfully with only SendGrid configuration present in appsettings.json
- **SC-004**: Welcome emails and password reset emails continue to be delivered via SendGrid with 100% functional parity to pre-refactoring behavior
- **SC-005**: No new errors, warnings, or exceptions are introduced during application startup or email sending operations
- **SC-006**: Configuration file size is reduced by eliminating unused AWS SES configuration sections

## Assumptions

- AWS SES was planned as an alternative provider but was never actually implemented or used in production deployments
- All current production deployments are using SendGrid exclusively (no migration from AWS SES to SendGrid is required)
- The SendGrid implementation is fully functional and meets all current email delivery requirements
- No future requirement exists to support multiple email providers or to switch to AWS SES
- The EmailSettings.Provider property currently exists but is either always set to "SendGrid" or defaults to SendGrid

## Out of Scope

- Adding any new email functionality or templates
- Changing SendGrid email delivery behavior or configuration structure
- Implementing alternative email providers (e.g., SMTP, Mailgun, Postmark)
- Email retry logic or queue management improvements
- Email analytics, tracking, or monitoring enhancements
- Changes to email template HTML/CSS or branding
