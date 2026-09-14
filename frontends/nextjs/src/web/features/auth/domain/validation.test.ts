import { describe, expect, it } from "vitest";

import { validateLoginForm, validateRegisterForm } from "./validation";

describe("validateRegisterForm", () => {
  it("accepts a genuinely valid submission", () => {
    const errors = validateRegisterForm({
      email: "fatima.rahman@example.com",
      password: "correct-horse-battery",
      displayName: "Fatima Rahman",
    });
    expect(errors).toEqual({});
  });

  it("rejects a password under 8 characters, matching the contract's own RegisterRequest.password minLength", () => {
    const errors = validateRegisterForm({ email: "a@example.com", password: "short", displayName: "A" });
    expect(errors.password).toBeDefined();
  });

  it("rejects a malformed email", () => {
    const errors = validateRegisterForm({ email: "not-an-email", password: "correct-horse-battery", displayName: "A" });
    expect(errors.email).toBeDefined();
  });

  it("rejects an empty display name", () => {
    const errors = validateRegisterForm({ email: "a@example.com", password: "correct-horse-battery", displayName: "   " });
    expect(errors.displayName).toBeDefined();
  });

  it("rejects a display name over 200 characters, matching the contract's own maxLength", () => {
    const errors = validateRegisterForm({ email: "a@example.com", password: "correct-horse-battery", displayName: "x".repeat(201) });
    expect(errors.displayName).toBeDefined();
  });
});

describe("validateLoginForm", () => {
  it("accepts a valid submission", () => {
    expect(validateLoginForm({ email: "a@example.com", password: "anything" })).toEqual({});
  });

  it("rejects an empty password without guessing a minimum length — the backend, not this form, owns that rule for login", () => {
    const errors = validateLoginForm({ email: "a@example.com", password: "" });
    expect(errors.password).toBeDefined();
  });
});
