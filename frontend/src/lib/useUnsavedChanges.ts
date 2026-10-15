"use client";

import { useEffect } from "react";

/**
 * Warns before leaving a project with unsaved changes (requirements s24).
 *
 * Two routes out of a page have to be covered:
 *
 *  1. Leaving the site — closing the tab, reloading, typing a new address.
 *     `beforeunload` handles that, and the browser supplies its own wording.
 *
 *  2. Navigating inside the app — clicking "Projects" in the sidebar. The App
 *     Router gives no cancellable navigation event, so link clicks are
 *     intercepted during the capture phase and confirmed before Next.js sees
 *     them. Without this, a sidebar click silently discards an edited project,
 *     which for financial data is the worst of the two cases.
 */
export function useUnsavedChanges(dirty: boolean, message: string) {
  useEffect(() => {
    if (!dirty) return;

    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      // Browsers ignore custom text here and show their own prompt; returnValue
      // is still required for the prompt to appear at all.
      e.returnValue = message;
    };

    const onClickCapture = (e: MouseEvent) => {
      // Let modified clicks through: they open a new tab, so this page survives.
      if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

      const anchor = (e.target as HTMLElement | null)?.closest?.("a");
      if (!anchor) return;

      const href = anchor.getAttribute("href");
      if (!href || href.startsWith("#")) return;
      if (anchor.target && anchor.target !== "_self") return;
      if (anchor.hasAttribute("download")) return;

      // External links leave the site, so beforeunload covers them.
      const url = new URL(href, window.location.href);
      if (url.origin !== window.location.origin) return;
      if (url.pathname === window.location.pathname) return;

      if (!window.confirm(message)) {
        e.preventDefault();
        e.stopPropagation();
      }
    };

    window.addEventListener("beforeunload", onBeforeUnload);
    document.addEventListener("click", onClickCapture, true);

    return () => {
      window.removeEventListener("beforeunload", onBeforeUnload);
      document.removeEventListener("click", onClickCapture, true);
    };
  }, [dirty, message]);
}
