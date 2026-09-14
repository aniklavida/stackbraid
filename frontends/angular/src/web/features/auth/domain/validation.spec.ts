import { describe, expect, it } from "vitest";

import { validateLoginForm, validateRegisterForm } from "./validation";

describe("validateRegisterForm", () => {
  it("accepts a valid submission", () => {
    expect(validateRegisterForm({ email: "sadia@example.com", password: "correct-horse", displayName: "Sadia" })).toEqual({});
  });

  it("rejects a malformed email", () => {
    expect(validateRegisterForm({ email: "not-an-email", password: "correct-horse", displayName: "Sadia" }).email).toBeDefined();
  });

  it("rejects a short password", () => {
    expect(validateRegisterForm({ email: "sadia@example.com", password: "short", displayName: "Sadia" }).password).toBeDefined();
  });

  it("rejects a blank display name", () => {
    expect(validateRegisterForm({ email: "sadia@example.com", password: "correct-horse", displayName: "  " }).displayName).toBeDefined();
  });

  it("rejects a display name over 200 characters", () => {
    const displayName = "a".repeat(201);
    expect(validateRegisterForm({ email: "sadia@example.com", password: "correct-horse", displayName }).displayName).toBeDefined();
  });
});

describe("validateLoginForm", () => {
  it("accepts a valid submission", () => {
    expect(validateLoginForm({ email: "sadia@example.com", password: "correct-horse" })).toEqual({});
  });

  it("rejects a malformed email", () => {
    expect(validateLoginForm({ email: "not-an-email", password: "correct-horse" }).email).toBeDefined();
  });

  it("rejects an empty password", () => {
    expect(validateLoginForm({ email: "sadia@example.com", password: "" }).password).toBeDefined();
  });
});
