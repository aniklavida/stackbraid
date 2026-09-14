"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/shared/auth/AuthProvider";
import { ApiError } from "@/shared/http/api-error";
import { registerAccount, ValidationFailed } from "../application/use-cases";
import type { FieldErrors, RegisterFormValues } from "../domain/validation";

export function RegisterForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const auth = useAuth();

  const [values, setValues] = useState<RegisterFormValues>({ email: "", password: "", displayName: "" });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors<RegisterFormValues>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setSubmitting(true);
    try {
      await registerAccount(values, auth);
      router.push("/profile");
    } catch (error) {
      if (error instanceof ValidationFailed) {
        setFieldErrors(error.fieldErrors as FieldErrors<RegisterFormValues>);
      } else if (error instanceof ApiError) {
        if (Object.keys(error.fieldErrors).length > 0) {
          const mapped: FieldErrors<RegisterFormValues> = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            mapped[field as keyof RegisterFormValues] = messages[0];
          }
          setFieldErrors(mapped);
        }
        setFormError(t.has(`errors.${error.code}`) ? t(`errors.${error.code}`) : t("errors.generic"));
      } else {
        setFormError(t("errors.generic"));
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="w-full max-w-sm">
      <form onSubmit={handleSubmit} noValidate>
        <CardHeader>
          <CardTitle>
            <h1>{t("register.title")}</h1>
          </CardTitle>
          <CardDescription>{t("register.subtitle")}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {formError && (
            <p role="alert" className="text-sm text-destructive">
              {formError}
            </p>
          )}
          <div className="flex flex-col gap-2">
            <Label htmlFor="register-display-name">{t("register.displayName")}</Label>
            <Input
              id="register-display-name"
              autoComplete="name"
              value={values.displayName}
              onChange={(event) => setValues((prev) => ({ ...prev, displayName: event.target.value }))}
              aria-invalid={Boolean(fieldErrors.displayName)}
              aria-describedby={fieldErrors.displayName ? "register-display-name-error" : undefined}
            />
            {fieldErrors.displayName && (
              <p id="register-display-name-error" className="text-sm text-destructive">
                {fieldErrors.displayName}
              </p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="register-email">{t("register.email")}</Label>
            <Input
              id="register-email"
              type="email"
              autoComplete="email"
              value={values.email}
              onChange={(event) => setValues((prev) => ({ ...prev, email: event.target.value }))}
              aria-invalid={Boolean(fieldErrors.email)}
              aria-describedby={fieldErrors.email ? "register-email-error" : undefined}
            />
            {fieldErrors.email && (
              <p id="register-email-error" className="text-sm text-destructive">
                {fieldErrors.email}
              </p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="register-password">{t("register.password")}</Label>
            <Input
              id="register-password"
              type="password"
              autoComplete="new-password"
              value={values.password}
              onChange={(event) => setValues((prev) => ({ ...prev, password: event.target.value }))}
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby="register-password-hint"
            />
            <p id="register-password-hint" className="text-sm text-muted-foreground">
              {fieldErrors.password ?? t("register.passwordHint")}
            </p>
          </div>
        </CardContent>
        <CardFooter className="flex flex-col items-stretch gap-3">
          <Button type="submit" disabled={submitting}>
            {t("register.submit")}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            {t("register.hasAccount")}{" "}
            <Link href="/login" className="font-medium text-foreground underline underline-offset-4">
              {t("register.loginLink")}
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
