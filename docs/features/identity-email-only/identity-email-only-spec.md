Here is the spec in English:

---

## Spec: Restrict Authentication Identity to Email Only

### Context

Currently, the sign-in flow accepts either `email` or `phoneNumber` as the `identity` field. This needs to change — `phoneNumber` should no longer be a valid login identity.

### Required Changes

**Authentication (`POST /api/v1/auth/signin`):**
- The `identity` field must only accept **email** — remove support for `phoneNumber` as a login identity
- If a non-email value is submitted (e.g. a phone number), return `400 Bad Request` with a clear validation error
- Update the request validation logic accordingly

**Phone Number — demoted to profile field only:**
- `phoneNumber` remains a field on the user entity and is still collected at sign-up
- It is stored and returned in the user profile (`UserProfile.phoneNumber`) as informational data only
- It has **no role** in authentication, identity resolution, or account lookup

**Sign Up (`POST /api/v1/auth/signup`):**
- `phoneNumber` remains an optional or required field in the sign-up request — but purely for profile purposes
- Remove any logic that indexes or resolves users by `phoneNumber` for auth purposes

### Scope of Changes

- Remove `phoneNumber` lookup from the sign-in command handler and any identity resolution service
- Update input validation on the sign-in endpoint to reject non-email values
- Update API documentation in `docs/api/auth-api-structure.md` to reflect that `identity` only accepts email
- Ensure no other auth flows (refresh token, forgot password, reset password) rely on `phoneNumber` as an identity

### Notes

- `phoneNumber` uniqueness constraint can remain if already in place — it is still a valid profile field
- This is a **breaking change** for any client currently signing in with a phone number — document it clearly
- After the change, the `SignInRequest` interface should be updated to reflect the new constraint:
