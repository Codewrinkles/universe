/**
 * Shared type definitions for the Codewrinkles application
 */

export type Theme = "dark" | "light";

export type AuthMode = "password" | "magic";

export interface App {
  id: string;
  name: string;
  accent: "sky" | "violet" | "brand" | "amber" | "emerald";
  description: string;
}

export interface Post {
  id: number;
  timeAgo: string;
  content: string;
  type: "default" | "quote" | "thread" | "image";
  quoted?: {
    author: string;
    text: string;
  };
  repliesPreview?: number;
}

export interface Module {
  id: number;
  title: string;
  duration: string;
  status: "done" | "in-progress" | "todo";
}

export interface ChatMessage {
  id: number;
  from: "system" | "twin" | "you";
  text: string;
}

export interface SettingsSection {
  id: "profile" | "account" | "apps" | "notifications";
  label: string;
}

export interface OnboardingStep {
  id: number;
  title: string;
  description: string;
  chips: string[];
}
