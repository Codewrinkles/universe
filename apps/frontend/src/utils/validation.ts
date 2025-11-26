/**
 * Form validation utilities
 * Client-side validation matching backend rules
 */

import type { FormErrors } from "../types";

/**
 * Validate email format
 */
export function validateEmail(email: string): string | undefined {
  if (!email || !email.trim()) {
    return "Email is required";
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email.trim())) {
    return "Invalid email format";
  }

  return undefined;
}

/**
 * Password strength requirements
 */
export interface PasswordStrengthResult {
  isValid: boolean;
  hasMinLength: boolean;
  hasUpperCase: boolean;
  hasLowerCase: boolean;
  hasNumber: boolean;
  hasSpecialChar: boolean;
  strength: "weak" | "medium" | "strong";
  metCount: number;
}

/**
 * Check password strength and requirements
 */
export function checkPasswordStrength(password: string): PasswordStrengthResult {
  const hasMinLength = password.length >= 8;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasLowerCase = /[a-z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>\-_=+\[\]\\;'/`~]/.test(password);

  const requirements = [hasMinLength, hasUpperCase, hasLowerCase, hasNumber, hasSpecialChar];
  const metCount = requirements.filter(Boolean).length;

  const isValid = metCount === 5;
  const strength: "weak" | "medium" | "strong" =
    metCount === 5 ? "strong" : metCount >= 3 ? "medium" : "weak";

  return {
    isValid,
    hasMinLength,
    hasUpperCase,
    hasLowerCase,
    hasNumber,
    hasSpecialChar,
    strength,
    metCount,
  };
}

/**
 * Validate password with detailed error message
 */
export function validatePassword(password: string): string | undefined {
  if (!password) {
    return "Password is required";
  }

  const strength = checkPasswordStrength(password);

  if (!strength.hasMinLength) {
    return "Password must be at least 8 characters";
  }

  if (!strength.isValid) {
    return "Password must contain uppercase, lowercase, number, and special character";
  }

  return undefined;
}

/**
 * Validate name
 */
export function validateName(name: string): string | undefined {
  if (!name || !name.trim()) {
    return "Name is required";
  }

  const trimmed = name.trim();
  if (trimmed.length < 2) {
    return "Name must be at least 2 characters";
  }

  if (trimmed.length > 100) {
    return "Name must be 100 characters or less";
  }

  return undefined;
}

/**
 * Validate handle (optional field)
 */
export function validateHandle(handle: string | undefined): string | undefined {
  if (!handle || !handle.trim()) {
    return undefined; // Handle is optional
  }

  const trimmed = handle.trim();

  if (trimmed.length < 3) {
    return "Handle must be at least 3 characters";
  }

  if (trimmed.length > 50) {
    return "Handle must be 50 characters or less";
  }

  const handleRegex = /^[a-zA-Z0-9_-]+$/;
  if (!handleRegex.test(trimmed)) {
    return "Handle can only contain letters, numbers, underscores, and hyphens";
  }

  return undefined;
}

/**
 * Validate the entire registration form
 */
export function validateRegisterForm(
  email: string,
  password: string,
  name: string,
  handle?: string
): FormErrors {
  const errors: FormErrors = {};

  const emailError = validateEmail(email);
  if (emailError) errors.email = emailError;

  const passwordError = validatePassword(password);
  if (passwordError) errors.password = passwordError;

  const nameError = validateName(name);
  if (nameError) errors.name = nameError;

  const handleError = validateHandle(handle);
  if (handleError) errors.handle = handleError;

  return errors;
}

/**
 * Check if form has any errors
 */
export function hasErrors(errors: FormErrors): boolean {
  return Object.values(errors).some((error) => error !== undefined);
}
