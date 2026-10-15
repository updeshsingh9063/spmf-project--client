"use client";

import Link from "next/link";
import { use, useEffect, useState } from "react";
import { ProjectEditor } from "@/components/ProjectEditor";
import { getProject } from "@/lib/projectStore";
import type { Project } from "@/lib/types";

export default function ProjectPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [project, setProject] = useState<Project | null | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getProject(id).then(setProject, (e: Error) => setError(e.message));
  }, [id]);

  if (error)
    return (
      <div role="alert" className="mx-auto max-w-6xl rounded-md border border-bad/30 bg-bad-wash px-4 py-3 text-sm text-bad">
        Couldn&apos;t open this project. {error}
      </div>
    );

  if (project === undefined) return <p className="text-sm text-muted">Opening project…</p>;
  if (project === null) {
    return (
      <div className="mx-auto max-w-md py-16 text-center">
        <h1 className="display text-xl">Project not found</h1>
        <p className="mt-2 text-sm text-ink-2">It may have been removed, or the link is incomplete.</p>
        <Link href="/projects" className="btn btn-quiet mt-6">Back to projects</Link>
      </div>
    );
  }
  // key forces a fresh editor when switching between projects
  return <ProjectEditor key={project.id} project={project} />;
}
