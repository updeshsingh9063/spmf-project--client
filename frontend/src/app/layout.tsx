import type { Metadata } from "next";
import { AuthProvider } from "@/lib/auth";
import "./globals.css";

export const metadata: Metadata = {
  title: "SPFM · Shield Security Doors",
  description: "Shield project financial management",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    // suppressHydrationWarning covers attributes added to <html> and <body> before
    // React hydrates. Browser extensions routinely do this (password managers,
    // theme switchers, dev tools), and the resulting console error looks alarming
    // while saying nothing about this application.
    //
    // It applies only to these two elements' own attributes, not to their
    // children, so a genuine mismatch anywhere in the tree is still reported.
    <html lang="en" suppressHydrationWarning>
      <body className="antialiased" suppressHydrationWarning>
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
