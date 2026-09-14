import { describe, expect, it } from "vitest";

import { ApiError, unwrap } from "./api-error";

describe("unwrap", () => {
  it("returns data on success", async () => {
    await expect(unwrap(Promise.resolve({ data: { id: "1" }, error: undefined }))).resolves.toEqual({ id: "1" });
  });

  it("returns undefined for a 204 response, without treating absence of data as failure", async () => {
    await expect(unwrap(Promise.resolve({ data: undefined, error: undefined, response: { status: 204 } as Response }))).resolves.toBeUndefined();
  });

  it("throws an ApiError carrying the Problem's code and field errors on failure", async () => {
    const problem = {
      type: "about:blank",
      title: "Validation failed",
      status: 400,
      code: "IDENTITY.VALIDATION_FAILED",
      errors: { email: ["must be a valid email address"] },
    };
    const promise = unwrap(Promise.resolve({ data: undefined, error: problem, response: { status: 400 } as Response }));
    await expect(promise).rejects.toBeInstanceOf(ApiError);
    await promise.catch((error: ApiError) => {
      expect(error.code).toBe("IDENTITY.VALIDATION_FAILED");
      expect(error.status).toBe(400);
      expect(error.fieldErrors.email).toEqual(["must be a valid email address"]);
    });
  });
});
