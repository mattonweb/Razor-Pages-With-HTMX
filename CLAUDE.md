# CLAUDE.md — Razor Pages With HTMX

## Recent changes (attended edits to this file — read, then carry on)

- **2026-11-08 (attended session with Matt): the adapter is now `HTMX.Adapter.Customers`**, named for the customer
  database it owns: organizations and their contacts, and the internal users who manage them. The layout and the
  solution-architecture paragraph say so. The entries below that name `HTMX.Adapter.Contacts` predate the rename.
- **2026-11-07, late (attended session with Matt): the database moved into `HTMX.Adapter.Contacts`.** The layout
  and the solution-architecture paragraph name it; the web project now only registers it. No entity exists yet.
- **2026-11-07, night (attended session with Matt): the `dotnet-solution-architecture` skill was adopted**, this
  repository being its first adopter. A fifth junction, a row in the skills table, and an adoption state of
  `conforming` below. The two existing projects already fit its naming; anything new follows it.
- **2026-11-07, evening (attended session with Matt): the solution exists.** The layout section names it and its
  projects; the stack section records the persistence choice. No entity exists yet; the first is the user.
- **2026-11-07, later (attended session with Matt): the `brand-htmlx-pages` skill was started.** It is a real
  folder under `.claude/skills/`, the one skill whose home is this repository, and it is a draft: read its status
  table before building any UI. The skills section, the edit rule and the `skills` scope below now cover it.
- **2026-11-07 (attended session with Matt): this file was written and the repository set up.** Nothing here has
  run yet. The solution does not exist until the first session creates it; the layout section below says where.

Orientation for Claude Code sessions in this repository. It is **public** and exists for one purpose: a prototype
Razor Pages application whose create, read, update, delete and list (CRUDL) operations are made interactive with
[HTMX](https://htmx.org/), built as the worked example for a presentation on how Razor Pages can be enhanced with
HTMX. Everything in the tree may be shown on a screen to an audience. Build it so that it reads well that way:
small, conventional, and explained by its own code and comments rather than by anything outside the repository.

## Working agreement — Matt commits; you never do

All work happens on `main`. There are no feature branches, no pull requests and no pipeline. You change files in the
working tree, build and test, and stop. **Matt reviews the tree and makes every commit himself, under his own
identity.** A hook (`.claude/hooks/git-guardrail`, `handback` profile) denies `git commit`, `git push`, `git merge`,
`git rebase`, `git reset`, forced or destructive forms of the other git commands, and anything that rewrites history.
If it denies a command you were about to run, that is the agreement working, not a defect: do not look for another
way to make the same change.

- **End every session with a drafted commit message** in the form the `devops-git-conventions` skill gives
  (`.claude/skills/devops-git-conventions/references/commit-messages.md`): a Conventional Commits subject of at most
  72 characters, a body that says why, and a last line that names the verification you ran. Put it in your final
  message so Matt can paste it. A drafted message with no verification line means the work was not verified, and
  say so plainly if that is the case.
- **Leave the tree in a state that builds.** If you must stop midway, say exactly which files are half done.
- **Never touch files you did not change.** No reformatting passes, no `dotnet format` across the solution unless the
  session asks for it.
- **Read-only git is always fine:** `status`, `diff`, `log`, `show`, `branch`.

**This repository is public: nothing private goes into it.** No keys, no credentials, no connection strings, no
personal data, no reference to any other person's or company's internal systems, and nothing copied from a private
repository. Sample data is invented and obviously so. If a request would put anything of that kind into the tree,
decline it and say why.

## Stack

- .NET 10 SDK (10.0.x is installed on this machine) and C# 14, the versions the house C# skill is written for.
- ASP.NET Core Razor Pages. No Blazor, no MVC controllers, no minimal-API endpoints except where a Razor Page
  handler genuinely cannot do the job, and then the file says why.
- HTMX for the interactive CRUDL work: partial page updates from page handlers that return partial views, with the
  page still working as plain Razor Pages when HTMX is absent (progressive enhancement). Pin HTMX to one exact
  version and record it in `README.md`.
- Persistence is whatever the presentation needs and nothing more. Chosen 2026-11-07: Entity Framework Core over an
  in-memory SQLite database, created when the application starts and seeded from code; see `README.md`.
- Tests: MSTest, written as the `code-csharp-mstest` skill describes.

## Layout (created 2026-11-07)

```
src/
  HTMX Pages Solution.slnx          the one solution file
  Directory.Build.props             house build settings for every project (code-csharp-standards, section 1)
  HTMX.Adapter.Customers/           the in-memory customer database, its entities and their data access
  HTMX.Application.Web/             the Razor Pages application
  HTMX.Test.Unit/                   the unit tests
  .editorconfig                     house copy, see below
```

## Skills: owned by Matt

`.claude/skills/` holds 5 Windows junctions, never copies, each gitignored. They point into the public
[Signalbox](https://github.com/mattonweb/Signalbox) repository, cloned at `C:\Source\Signalbox`; its `REPO-SETUP.md`
explains the mechanism. It also holds one real folder, `brand-htmlx-pages`, tracked in this repository because the
brand belongs to this application and to no other.

| Skill | Home | What it governs |
|---|---|---|
| `code-software-standards` | Signalbox | The language-neutral half of the house code standards, read before the language skill |
| `code-csharp-standards` | Signalbox | House C# rules: file layout, type taxonomy, method shape, the 500-line ceiling, allowed language features. Its enforceable half is `src/.editorconfig` plus the analyzers |
| `code-csharp-mstest` | Signalbox | How tests are written: naming, assertion patterns, structure |
| `dotnet-solution-architecture` | Signalbox | Which projects exist and what each may reference: `{{solution}}.{{kind}}.{{name}}` naming, the four project kinds, the dependency rules, and the public surface of a class library. Read it before creating a project or adding a project reference |
| `devops-git-conventions` | Signalbox | The commit-message form you draft for Matt. Its branch, pull-request and tag sections do not apply here; this repository has no branches and no pipeline |
| `brand-htmlx-pages` | this repository | The HTMLX Pages brand: tokens, CSS, icons and the markup of every UI component, with specimen pages. A draft; its status table says which groups are settled |

⛔ **Matt owns every skill. Never edit a file inside one of the five junctions**, however wrong it looks; report the
defect in your final message instead. An edit through a junction lands in Signalbox's working tree.
`brand-htmlx-pages` is Matt's too, and changes only in a session he attends; an unattended session that finds it
wrong reports the defect and leaves it.

**Commit scopes (`devops-git-conventions` section 3): one scope per drafted commit, from this list, or none when the
change spans the repository.** `web` (the Razor Pages application), `htmx` (the client-side interaction layer and
its partials), `data` (the store and its models), `tests` (the unit tests), `deps` (package updates), `docs`
(`README.md` and presentation notes), `skills` (the `brand-htmlx-pages` skill, the junctions and their gitignore
lines), `charter` (this file). A scope
that is not here is added to this list in its own commit before the commit that uses it; never invented in the
message.

**The 500-line ceiling (`code-csharp-standards` section 8): the named-exception list for this repository is
EMPTY**, and the repository starts with no files over it. A file that needs to exceed the ceiling gets its entry
here, with its line count and a reason, in the same session that creates it.

**Solution architecture (`dotnet-solution-architecture` section 7): adoption state `conforming`.** The solution holds
one Application (`HTMX.Application.Web`), one Adapter (`HTMX.Adapter.Customers`) and one optional test project
(`HTMX.Test.Unit`). **One Adapter per database:** `HTMX.Adapter.Customers` owns the in-memory `Customers` database
and every entity in it (organizations, their contacts, and the internal users who manage them), with each entity's
provider (its Entity Framework queries) and service (its rules) beside it; a new entity type joins this Adapter,
never a new one. A data source or an external system that earns its own
project is an `HTMX.Adapter.<Name>` exposing only `Configuration/`, `Contracts/` and
`ServiceCollectionExtensions`; a workflow across capabilities is an
`HTMX.Orchestrator.<Name>`; page models stay presentation. A change that would break a dependency rule or make an
internal type public is declined with the reason. No architecture test exists yet.

The house `.editorconfig` (`src/.editorconfig`) and `.gitattributes` are verbatim copies of Signalbox's root files,
applied 2026-11-07. A copy that differs from Signalbox is drift: it is fixed in Signalbox first, never here.

## Session start

1. `git status` and `git log -1` in this repository, so you know what Matt last committed and whether the tree
   already holds uncommitted work from an earlier session. **Uncommitted work is Matt's to commit, not yours to
   discard.** Build on it or ask; never revert it.
2. Read this file in full. If a tool's behavior disagrees with it, stop that thread and report the surprise in your
   final message rather than working around it.
3. If the junctioned skills under `.claude/skills/` look stale or missing, say so; Matt pulls Signalbox. Do not pull
   it yourself.
