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
import { signIn } from "../application/use-cases";
import { ValidationFailed } from "../application/use-cases";
import type { FieldErrors, LoginFormValues } from "../domain/validation";

export function LoginForm() {
  const t = useTranslations("auth");
  const router = useRouter();
  const auth = useAuth();

  const [values, setValues] = useState<LoginFormValues>({ email: "", password: "" });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors<LoginFormValues>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setSubmitting(true);
    try {
      await signIn(values, auth);
      router.push("/profile");
    } catch (error) {
      if (error instanceof ValidationFailed) {
        setFieldErrors(error.fieldErrors as FieldErrors<LoginFormValues>);
      } else if (error instanceof ApiError) {
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
            <h1>{t("login.title")}</h1>
          </CardTitle>
          <CardDescription>{t("login.subtitle")}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {formError && (
            <p role="alert" className="text-sm text-destructive">
              {formError}
            </p>
          )}
          <div className="flex flex-col gap-2">
            <Label htmlFor="login-email">{t("login.email")}</Label>
            <Input
              id="login-email"
              type="email"
              autoComplete="email"
              value={values.email}
              onChange={(event) => setValues((prev) => ({ ...prev, email: event.target.value }))}
              aria-invalid={Boolean(fieldErrors.email)}
              aria-describedby={fieldErrors.email ? "login-email-error" : undefined}
            />
            {fieldErrors.email && (
              <p id="login-email-error" className="text-sm text-destructive">
                {fieldErrors.email}
              </p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="login-password">{t("login.password")}</Label>
            <Input
              id="login-password"
              type="password"
              autoComplete="current-password"
              value={values.password}
              onChange={(event) => setValues((prev) => ({ ...prev, password: event.target.value }))}
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby={fieldErrors.password ? "login-password-error" : undefined}
            />
            {fieldErrors.password && (
              <p id="login-password-error" className="text-sm text-destructive">
                {fieldErrors.password}
              </p>
            )}
          </div>
        </CardContent>
        <CardFooter className="flex flex-col items-stretch gap-3">
          <Button type="submit" disabled={submitting}>
            {t("login.submit")}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            {t("login.noAccount")}{" "}
            <Link href="/register" className="font-medium text-foreground underline underline-offset-4">
              {t("login.registerLink")}
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
