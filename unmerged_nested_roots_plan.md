# Implementation Plan: Unmerged Nested Root Folders (Approach B)

## Overview
This plan outlines the changes needed to display child root folders as separate, independent root nodes in the `TreeView` while preserving the parent's full tree hierarchy (Inclusive / Mirrored model). It details how to achieve this with zero redundant disk I/O and negligible CPU/memory overhead by scanning minimal covering roots in a single pass and memoizing child root subtrees.

> [!IMPORTANT]
> **Code Documentation Requirement:** Any new or updated functions, methods, records, or classes must include a single-line, information-dense `<summary>` XML doc comment describing its (updated) function, matching the established codebase convention.

---

## Proposed Changes

### 1. `FolderTreeBuilder.cs`

#### A. Refactor Root Partitioning
Replace `RemoveNestedRoots` with a method that deduplicates exact paths while distinguishing between covering roots and nested roots:
- Input: `IEnumerable<string> roots`
- Deduplicate exact paths (case-insensitive), preserving original order.
- Identify **Covering Roots** (roots not under any other root in the set).
- Identify **Nested Roots** as a `HashSet<string>` (roots where `IsUnder(child, parent)` is true for some other root).
- Retain the original ordered list of distinct roots for the final projection.

#### B. Update `Scanner`
- Pass `HashSet<string> nestedRoots` into `Scanner`; pre-size the cache to `nestedRoots.Count`:
  ```csharp
  private readonly Dictionary<string, DirectoryScan> _nestedRootScans = new(nestedRoots.Count, StringComparer.OrdinalIgnoreCase);
  ```
- In `ScanDirectory(DirectoryInfo directory)`:
  - **After `ScanDirectory` returns** (i.e., after the full subtree has been built and the `DirectoryScan` is complete), check if `nestedRoots.Contains(directory.FullName)`.
  - If so, record `_nestedRootScans[directory.FullName] = scan;`. The check must not be placed before recursion; doing so would capture an empty or partial scan.
  - Provide a method or property `TryGetNestedScan(string path, out DirectoryScan? scan)` to retrieve captured scans.

#### C. Handle Empty Child Roots
- When a folder is a designated child root, ensure it is retained in the cache even if `child.Files.Count == 0` so it can still be displayed as an empty root node (consistent with top-level root behavior tested in `Scan_rootWithoutMatches_stillShownButNotCounted`).

#### D. Project Ordered Roots in `FolderTreeBuilder.Scan`
- Execute `scanner.ScanRoot(root)` for each covering root.
- Reconstruct the final list of `FolderNode` items in the exact order requested by the user:
  - If a path is a covering root: use the result of `scanner.ScanRoot(path)`.
  - If a path is a nested child root: retrieve the captured `DirectoryScan` via `TryGetNestedScan` and project it using a new **pure** `BuildNode` method (see Section E). If the cache has no entry for a child root (because its ancestor was inaccessible or missing and so the child was never visited), that path is silently omitted from `result.Roots` — the same behavior as a covering root that returns `null` from `ScanRoot`.
- Set `MergedRootCount` to 0 (or remove/repurpose it to count exact duplicates).

#### E. Introduce a Pure `BuildNode` Helper
Split the current `Wrap` method into two layers to avoid double-counting `MatchingFolderCount` and redundant re-sorting when the same `DirectoryScan` is projected a second time for a child root:

- **`BuildNode(string fullPath, string displayName, DirectoryScan scan)`** — pure construction only: relabels children with `Path.GetRelativePath`, sorts children and files, computes `SubtreeFileCount`, and returns a `FolderNode`. Does **not** touch `MatchingFolderCount`.
- **`Wrap(string fullPath, string displayName, DirectoryScan scan)`** — calls `BuildNode` and additionally increments `MatchingFolderCount` when `scan.Files.Count > 0`. Used during the recursive scan pass.
- When projecting a child root as a top-level root node, call `BuildNode` directly (not `Wrap`), since `MatchingFolderCount` was already incremented when the directory was first wrapped during the recursive scan.

---

### 2. `FolderScanResult` record & `FolderTreeStatusFormatter.cs`

- Rename `RootsScanned` → `CoveringRootsScanned` in `FolderScanResult` and all usages. The field counts only the covering roots that were physically scanned from disk, not the total number of displayed root nodes.
- Update `FormatCompleted`:
  - Use `CoveringRootsScanned` in the "N roots scanned" phrase (zero-match branch).
  - Remove `nested roots merged` phrasing when `MergedRootCount == 0`.
- Update `FormatTooltip`:
  - Relabel the `Roots scanned:` line to `Covering roots scanned:` (or equivalent) to match the renamed field.
  - Omit or update the `Nested roots merged` line.

---

### 3. `FoldersTreeController.cs`

- Verify `Populate(FolderScanResult result)`:
  - The controller already iterates over `result.Roots` and creates a top-level `TreeNode` for each item.
  - Because `result.Roots` now contains all user-requested roots (both covering and child roots), each will automatically become a separate top-level node in the `TreeView`.
- Double-clicking or selecting any node (parent or child root) will continue to work seamlessly because each `TreeNode.Tag` carries its corresponding `FolderNode`.

---

### 4. Tests (`tests/Random File Select.Tests/FoldersTreeTests.cs`)

- Update existing tests:
  - `RemoveNestedRoots_dropsNested_keepsOrder_countsMerged` ➔ Update to verify that exact duplicates are removed while nested roots are preserved in the ordered output.
  - `Scan_nestedRoot_scannedOnce_countsNotInflated` ➔ Update to verify:
    1. Both the parent root and the nested child root exist as separate items in `result.Roots`.
    2. `ProcessedCount` equals the number of physical directories (confirming the child tree was visited on disk only once).
    3. The child root node has the expected `SubtreeFileCount` and files.
- Add new tests:
  - Verify deterministic ordering of roots when child roots precede parent roots in the input list.
  - Verify that an empty nested root folder is still represented as a root node in `result.Roots`.
  - Verify that a child root node's `DisplayName` equals its full path and that its direct children's `DisplayName` values are correct relative paths (not paths relative to the covering root).
  - Update `FormatCompleted_mergedRoots` to reflect the removed/updated "nested roots merged" phrasing.
  - Update `FormatTooltip_containsBreakdown` to reflect the renamed `CoveringRootsScanned` field and updated tooltip label.

---

## Edge Cases Handled

1. **Root Order Independence:** A child root can appear before or after its parent in the list. By scanning covering roots first and projecting the final roots in input order, the user's root ordering in the CheckedListBox is respected.
2. **Deeply Nested Roots (Grandchildren):** If `A`, `A\B`, and `A\B\C` are all roots, only `A` is scanned from disk. `A\B` and `A\B\C` are both captured and wrapped as separate root nodes.
3. **Missing/Inaccessible Child Roots:** If a configured child root's ancestor covering root is inaccessible or missing, `ScanRoot` returns `null` and the recursive scan never reaches the child folder, so no cache entry is recorded. The projection step calls `TryGetNestedScan` for the child path, finds nothing, and silently omits it from `result.Roots` — identical to how a covering root that returns `null` is omitted. No extra error-handling code is required.
4. **Immutable Sharing:** Because `FolderNode` and `RetainedFile` are immutable records, sharing subtree structures between the parent node and the child root node avoids race conditions and memory mutation bugs.
