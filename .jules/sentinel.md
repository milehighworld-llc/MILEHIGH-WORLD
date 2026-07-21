## 2024-05-24 - Path Traversal in Editor Extension Scripts
**Vulnerability:** Found a Path Traversal vulnerability in an Editor window script (`CharacterFactory.cs`). The script trusted the `name` field from a parsed JSON file (`campaign_master.json`) to construct the file path for creating new `.asset` files (`string assetPath = $"{folderPath}/{charProfile.name...}.asset";`).
**Learning:** Even though the JSON is a "local" file used by a developer in the Editor, trusting external data to construct file paths without sanitization is a security risk. If a malicious JSON file is imported, it could contain names like `../../Scripts/Core/ImportantScript` which would overwrite arbitrary files in the project.
**Prevention:** Always sanitize any data sourced from external files (like JSON, XML) before using it in path construction. Use `Path.GetInvalidFileNameChars()` to filter out OS-level invalid characters and `Path.GetFileName()` to strip directory traversal sequences like `../` to ensure the resulting string is strictly a file name.
## 2024-05-24 - Path Traversal via Unsanitized JSON Data in Unity Editor
**Vulnerability:** Path traversal vulnerability in `CharacterFactory.cs` where imported JSON `charProfile.name` was used directly in an `AssetDatabase.CreateAsset` path without sanitization.
**Learning:** Unity Editor scripts that dynamically generate files or paths from imported data (like JSON) are susceptible to path traversal. Even seemingly safe strings like "character name" can be manipulated maliciously (e.g. `../MaliciousAsset`) to overwrite critical project assets or write files outside the intended folder.
**Prevention:** Always sanitize dynamically imported strings that are used in file paths. Use `Path.GetInvalidFileNameChars()` to strip/replace illegal characters, and ensure no directory separators are left before appending to a hardcoded base folder path.
## 2024-11-06 - Prevent Information Exposure via Error Logs in Unity Scripts
**Vulnerability:** Found `CampaignManager.cs` logging exact absolute file paths (using `Application.streamingAssetsPath` and `Application.dataPath`) and lacking exception handling around a JSON read/parse block, potentially leaking system info or stack traces.
**Learning:** Unity's built-in platform paths can expose underlying OS directory structures, usernames, or execution environments when logged directly, especially if logs are captured.
**Prevention:** Mask runtime exception stack traces (e.g. log only `ex.Message`) and replace absolute path logs with safe generic identifiers (like the filename) to avoid information exposure.
## 2024-05-18 - Information Disclosure via Absolute File Paths in Logs\n**Vulnerability:** Found `Debug.Log` statements outputting absolute paths like `Path.Combine(Application.dataPath, ...)` into Unity logs. In production, this leaks server or client filesystem structure, potentially aiding attackers in path traversal or deeper filesystem exploitation.\n**Learning:** `Application.dataPath` and `Application.streamingAssetsPath` resolve to complete absolute paths that should remain internal.\n**Prevention:** Only use filenames or safe identifiers when logging file operations, rather than the full `filePath`. Also, explicitly catch file parsing errors to avoid leaking internal stack traces.
## 2024-05-24 - [Information Leakage via Stack Trace]
**Vulnerability:** Public GameObjects and UI components (like `DialogueBox`, `SpeakerNameText`, and `DialogueText`) were being accessed directly in `Cinematic_IntoTheVoid.cs` without prior null checks.
**Learning:** If these components are unassigned in the Unity Editor, calling methods or properties on them triggers a `NullReferenceException`, which exposes the internal application call stack/stack trace. This violates "Fail securely - errors should not expose sensitive data."
**Prevention:** Always implement defensive programming by verifying that required object references are not null before accessing them. If they are missing, log a secure, generic error message (e.g., "Missing UI components required for cinematic") and safely abort the operation (e.g., using `return;`).
## 2024-05-24 - Unity File Path Information Disclosure and Unhandled Exception Logging
**Vulnerability:** Information Disclosure and Error Stack Trace Leak
**Learning:** `Application.dataPath` and `Application.streamingAssetsPath` resolve to physical file paths on the host system. Logging these directly in production environments or allowing unhandled exceptions from methods like `File.ReadAllText` and `JsonUtility.FromJson` to print to the console can expose internal file structures and stack traces to attackers.
**Prevention:** Always use relative file paths or file names (e.g. `fileName`) in logs. Wrap file I/O operations and JSON deserialization in `try-catch` blocks to fail securely and prevent unhandled exceptions from leaking internal details. Catch generic exceptions and provide a sanitized, secure error message.
## 2025-01-24 - Input Validation and Secure Exception Handling in Game Data Management
**Vulnerability:** The application lacked a systematic way to validate the integrity and security of deserialized JSON campaign data, potentially leading to logic errors or vulnerabilities if malicious data was loaded. Furthermore, previous "security fixes" in `CampaignManager.cs` and `CharacterFactory.cs` had introduced syntax errors and redundant/ineffective logging.
**Learning:** Security fixes applied without full contextual understanding or verification can lead to compilation errors and degraded maintainability. A centralized `IsValid()` pattern in the data model provides a clean, reusable way to enforce business constraints and security requirements immediately after deserialization.
**Prevention:** Implement an `IsValid()` method for all serializable data models that are populated from external sources. Use these methods at the point of ingestion (e.g., in `CampaignManager`). Ensure error handling in these ingestion points uses generic error messages and avoids logging absolute paths or stack traces.
## 2024-05-24 - Data Integrity and Security Validation for Deserialized Assets
**Vulnerability:** Untrusted external data (JSON) was being used directly by the application without validation, potentially leading to out-of-bounds values or corrupted application state.
**Learning:** Even if data is "local", it should be treated as untrusted input once it crosses the boundary from a file into the application.
**Prevention:** Implement an `IsValid()` pattern in data models to perform security and integrity checks immediately after deserialization. This ensures the application fails fast and securely when encountering malicious or corrupted data.

## 2024-05-24 - Prevent IDOR-like access to core managers
**Vulnerability:** Unsanitized external strings from JSON were used directly in `GameObject.Find` wrappers, allowing unauthorized manipulation of core system objects.
**Learning:** Overly broad string heuristics like `.Contains("Manager")` cause functional regressions by blocking legitimate game elements.
**Prevention:** Use exact matching for critical singletons like CampaignManager, SceneDirector, CameraManager, and AlliancePowerManager to prevent tampering.

## 2024-05-24 - Prevent Path Traversal in Editor Scripts
**Vulnerability:** A badly resolved merge conflict introduced a duplicate variable declaration that bypassed the existing path traversal sanitization loop for character names.
**Learning:** Duplicate variable declarations resulting from merge conflicts can silently disable critical security logic, such as sanitization routines.
**Prevention:** When resolving merge conflicts, carefully verify that previously implemented security logic (e.g., path traversal sanitization loops) is not accidentally overwritten or bypassed by duplicate variable declarations.
## 2025-01-24 - DoS Protection and Security Logic Consolidation
**Vulnerability:** DoS vulnerability in 'SceneDirector.cs' via unsanitized 'objectName' in 'GameObject.Find'. Also, broken security validation logic in 'HorizonGameData.cs' and 'CampaignManager.cs' caused by malformed/duplicate checks.
**Learning:** Unity's 'GameObject.Find' is O(N) and can be abused for DoS if input is not length-limited and sanitized. Redundant or malformed security checks (e.g., double 'else' blocks) often occur during hasty security patching and can break the entire build.
**Prevention:** Enforce strict length limits and whitelist regex on any string passed to scene traversal methods. Always consolidate security validation into single, robust paths to maintain syntactic and logical integrity.
## 2025-02-25 - IDOR in SceneDirector
**Vulnerability:** The application allowed interaction objects loaded from the `campaign_master.json` data file to specify any `objectId` which was resolved via `GameObject.Find` and then manipulated. If `objectId` targeted protected systemic objects like `CampaignManager`, an attacker controlling the campaign file could manipulate these objects and cause unintended behaviors.
**Learning:** This is effectively an Insecure Direct Object Reference (IDOR). Trusting unsanitized input to look up references, particularly when using global lookup tools like `GameObject.Find()`, exposes critical application structures to tampering.
**Prevention:** Implement strict whitelisting or explicitly block interactions on critical singleton and system manager objects. Before attempting to resolve an object ID, verify that it is not in the list of protected, non-interactive system objects.

## 2024-05-24 - IDOR in Data-Driven Scene Construction
**Vulnerability:** `GameObject.Find` was used directly on unsanitized external strings from JSON, granting uncontrolled access to any scene hierarchy object.
**Learning:** External data targeting interactive objects can specify core singletons (like `CampaignManager` or `SceneDirector`), allowing attackers to manipulate or destroy critical game components. Broad heuristics like `.Contains("Manager")` break legitimate objects.
**Prevention:** Use exact string matching or specific tagging to block unauthorized access to core architectural singletons when performing dynamic object lookups.
## 2025-01-24 - Security Logic Consolidation and Sanitization
**Vulnerability:** Redundant and malformed security validation logic in `CampaignManager.cs` and `HorizonGameData.cs` caused compilation errors and inconsistent data integrity checks. Additionally, `CharacterFactory.cs` had duplicate sanitization logic that was partially ineffective against path traversal.
**Learning:** Security fixes applied in isolation or without full verification can lead to "code rot" where multiple versions of the same check exist, potentially conflicting or breaking the build. Sanitization for path traversal must be robust and happen at the final step of path construction.
**Prevention:** Consolidate security validation into single, well-defined methods (`IsValid`). Use `Path.GetFileName` after all other sanitization (like character replacement) to ensure the resulting string is strictly a filename, preventing directory traversal via malicious input.

## 2024-04-14 - Prevent IDOR on System Objects
**Vulnerability:** External JSON data could manipulate critical game singletons (CampaignManager, SceneDirector, CameraManager, AlliancePowerManager) by providing their names as object IDs in ObjectInteractions.
**Learning:** Using GameObject.Find (or cached wrappers) on unsanitized external strings allows uncontrolled access to any object in the scene hierarchy, essentially creating an Insecure Direct Object Reference (IDOR) vulnerability within the game engine.
**Prevention:** Explicitly validate and sanitize target IDs from external data to ensure they cannot match critical system managers. Use exact string matching for protection rather than broad substring checks to prevent functional regressions.
## 2026-04-14 - Denial of Service (DoS) mitigation for GameObject.Find
**Vulnerability:** Untrusted input from JSON was used in `GameObject.Find` in `SceneDirector.cs`. Maliciously long strings or path-traversal-like patterns (e.g., using `/`) could lead to expensive scene traversals or unexpected object access.
**Learning:** `GameObject.Find` is an O(N) operation that can be abused. While caching helps, the initial lookup using unsanitized strings is a risk.
**Prevention:** Implement strict string length limits (e.g., 128 chars) and character whitelists (alphanumeric, underscores, spaces, etc.) for any string passed to `GameObject.Find` or used as a key in object caches derived from external data.
## 2025-05-24 - DoS Protection and Resource Limits in Scene Lookups and Data Ingestion
**Vulnerability:** The application was vulnerable to Denial of Service (DoS) attacks via unsanitized user input in `GameObject.Find` and lack of resource limits (string lengths, object counts) in deserialized campaign data.
**Learning:** External data, even if trusted as "local assets", must have enforced resource limits to prevent memory exhaustion or CPU-intensive operations (like O(N) scene traversals with complex search patterns).
**Prevention:** Implement strict length limits and character whitelists for all string inputs used in lookup functions. Enforce bounds on collection sizes and string lengths during data validation immediately after deserialization.
## 2024-05-24 - IDOR-like Tampering in SceneDirector
**Vulnerability:** The `ApplyInteraction` method in `SceneDirector.cs` allowed any object in the scene to be manipulated based on an arbitrary string ID provided in external JSON data. This could allow malicious data files to apply unauthorized transformations to critical system objects like the `CampaignManager`.
**Learning:** Using untrusted strings directly for object lookups and modifications without validation can lead to IDOR-like vulnerabilities where unintended objects are modified.
**Prevention:** Implement exact string matching checks to block interaction with known critical system objects, ensuring that external data can only manipulate intended game elements.

## 2024-05-24 - IDOR-like Tampering of Critical System Objects via Scene Interactions
**Vulnerability:** The `ApplyInteraction` method in `SceneDirector.cs` used object IDs from untrusted JSON configurations directly in `GameObject.Find` lookups (via `GetCachedObject`) without validation. This allowed attackers to manipulate the transforms of critical system objects like `CampaignManager` or `SceneDirector`.
**Learning:** In data-driven Unity projects, trusting external string IDs for scene object resolution creates an IDOR vulnerability, allowing manipulation of singleton or system objects that should be off-limits.
**Prevention:** Always sanitize and validate object IDs originating from external sources. Implement explicit access control or blocklists using exact string matching (avoiding broad heuristics like `.Contains("Manager")`) to protect critical system objects from unauthorized interactions.
## 2024-05-24 - Preventing IDOR on Core System Managers
**Vulnerability:** Insecure Direct Object Reference (IDOR) via unsanitized `interaction.objectId` mapping to `GameObject.Find`, allowing external JSON to manipulate core managers like `CampaignManager`.
**Learning:** Using `GameObject.Find` with external data without validation allows arbitrary access to any scene object, bypassing intended game logic boundaries.
**Prevention:** Apply validation and exact-match blocklists at the application boundary where untrusted external input is first processed (e.g., `ApplyInteraction`), not in low-level lookup utilities, to avoid functional regressions.
## 2025-01-24 - Prevent IDOR Tampering with Core Unity Singletons
**Vulnerability:** Insecure Direct Object Reference (IDOR) via GameObject.Find using unsanitized external strings from campaign_master.json.
**Learning:** Using untrusted strings directly in scene hierarchy queries allows malicious manipulation of core architectural singletons like CampaignManager.
**Prevention:** Implement exact match blocklists for critical system objects before allowing data-driven interactions, avoiding overly broad .Contains heuristics to prevent functional regressions.
## 2025-01-24 - Prevent IDOR on Unity Singletons
**Vulnerability:** Insecure Direct Object Reference (IDOR) where unsanitized external strings (like JSON IDs) were passed to `GameObject.Find`, allowing external data to manipulate core architectural singletons (`CampaignManager`, etc.).
**Learning:** In data-driven Unity projects, `GameObject.Find` grants uncontrolled access to the entire scene hierarchy. Unsanitized strings can be manipulated to reference and tamper with critical system managers instead of intended interactive objects.
**Prevention:** Implement exact string matching (not broad heuristics like `.Contains("Manager")`) to block lookup of protected system singletons before invoking `GameObject.Find` or cache lookups.

## 2024-05-30 - IDOR-like Tampering of Critical System Objects
**Vulnerability:** External JSON data dictating object interactions lacked ID validation, allowing attackers to maliciously manipulate critical system singletons like CampaignManager or SceneDirector.
**Learning:** When game objects are dynamically targeted via strings from external, untrusted sources, it introduces an Insecure Direct Object Reference (IDOR) risk into the game logic. Broad string heuristics like `.Contains("Manager")` should be avoided as they cause functional regressions.
**Prevention:** Always validate object IDs against an explicit whitelist or exact-match blacklist of critical system objects before applying external interactions.
## 2024-04-22 - IDOR in Scene Object Lookups
**Vulnerability:** Insecure Direct Object Reference (IDOR) via unsanitized `GameObject.Find` queries in `SceneDirector.cs`.
**Learning:** External JSON data could provide malicious object IDs (like "CampaignManager") to interact with core architectural singletons, because `GameObject.Find` has uncontrolled access to any scene hierarchy object. Broad string heuristics were avoided to prevent functional regressions.
**Prevention:** Always sanitize and validate object IDs originating from external sources using exact matches before using them in scene traversal queries.

## 2024-04-22 - IDOR in Scene Object Interactions
**Vulnerability:** Insecure Direct Object Reference (IDOR) via unsanitized `interaction.objectId` queries in `SceneDirector.cs`.
**Learning:** External JSON data could provide malicious object IDs (like "CampaignManager") to interact with core architectural singletons, because `ApplyInteraction` uses the uncontrolled string ID to fetch and manipulate the game object. The validation must happen at the boundary where the untrusted input is used, not inside low-level generic retrieval utilities like `GetCachedObject` to avoid breaking internal game logic.
**Prevention:** Always sanitize and validate object IDs originating from external sources using exact matches before using them to interact with scene objects.
## 2024-04-07 - Insecure Direct Object Reference (IDOR) via JSON Manipulation
**Vulnerability:** External JSON game data loaded via CampaignManager allowed applying scale and position modifications to any GameObject in the scene by supplying an exact name, including critical system objects like `CampaignManager`, `SceneDirector`, `CameraManager`, and `AlliancePowerManager`.
**Learning:** Even singleplayer/offline games loading external configuration files can be susceptible to IDOR-like data tampering vulnerabilities. Objects acting dynamically on names are a massive risk if those names come from an untrusted source. We must avoid broad heuristic matches (e.g., `Contains("Manager")`) to prevent functional regressions, utilizing explicit allowlists or denylists.
**Prevention:** Sanitize and validate object IDs originating from external sources before using them in dynamic systems. Maintain a strict denylist or allowlist to prevent manipulation of sensitive objects, and ensure specific tag matching rather than loose substring matching.
## 2025-01-24 - Ambiguity in Serializable Attribute and CI/Static Analysis Failures
**Vulnerability:** Static analysis (CodeQL) and standalone C# compilation checks can fail if `[Serializable]` is used while both `System` and `UnityEngine` namespaces are imported, as both define a `SerializableAttribute`.
**Learning:** While Unity's internal compiler handles this, standard .NET compilers and static analysis runners used in CI (like CodeQL) will flag it as a CS0104 ambiguity error. This can block security scans and CI pipelines.
**Prevention:** Always use the fully qualified `[System.Serializable]` attribute in Unity data models that might be processed by external tools or analyzed in CI to ensure consistent and reliable builds/scans.
## 2026-04-06 - Restoration of Hierarchical Security Validation and Resource Exhaustion Protection
**Vulnerability:** Found , , and  in a broken state due to poor merge edits, with duplicate  methods and broken logic. This disabled critical security checks for imported campaign data.
**Learning:** Security frameworks are fragile if not properly maintained. A broken  check is equivalent to no check at all, potentially allowing malformed or malicious data to compromise application state.
**Prevention:** Implement hierarchical  checks across all data models. Enforce resource limits (string lengths, collection sizes) at the point of deserialization to prevent DoS via resource exhaustion. Ensure each data class is responsible for its own validation.
## 2026-04-06 - Restoration of Hierarchical Security Validation and Resource Exhaustion Protection
**Vulnerability:** Found `HorizonGameData.cs`, `CampaignManager.cs`, and `CharacterFactory.cs` in a broken state due to poor merge edits, with duplicate `IsValid` methods and broken logic. This disabled critical security checks for imported campaign data.
**Learning:** Security frameworks are fragile if not properly maintained. A broken `IsValid` check is equivalent to no check at all, potentially allowing malformed or malicious data to compromise application state.
**Prevention:** Implement hierarchical `IsValid()` checks across all data models. Enforce resource limits (string lengths, collection sizes) at the point of deserialization to prevent DoS via resource exhaustion. Ensure each data class is responsible for its own validation.
## 2025-05-24 - Broken Security Validation Framework and Lack of Resource Limits
**Vulnerability:** The security validation framework was broken due to botched merge edits in `HorizonGameData.cs`, `CampaignManager.cs`, and `CharacterFactory.cs`, including duplicate `else` blocks and malformed methods. Additionally, the system lacked resource exhaustion protection for deserialized data.
**Learning:** Incomplete or unverified security patches can leave the system in a vulnerable state by breaking intended validation logic. Centralized recursive validation must include explicit limits on collection sizes and string lengths to mitigate DoS risks from external data.
**Prevention:** Maintain a single, robust `IsValid()` implementation per data class. Enforce string length and collection size limits in these methods. Always verify C# syntax and compilation (even via mocking) after applying security fixes to critical path components.

## 2025-01-30 - Resource Exhaustion Protection in Data Validation
**Vulnerability:** The application was vulnerable to Denial of Service (DoS) attacks via maliciously crafted JSON campaign data containing extremely large strings or collections, which could exhaust system memory.
**Learning:** Simple integrity checks (like range checks) are insufficient for security; resource limits must be explicitly enforced during deserialization validation.
**Prevention:** Implement "Sentinel Standard" resource limits in all `IsValid()` methods, enforcing maximum lengths for strings and maximum counts for collections based on realistic application needs.
## 2025-05-24 - [Sentinel Standard] Resource Exhaustion Protection in Data Validation
**Vulnerability:** Lack of constraints on string lengths and collection sizes in deserialized JSON data could lead to Denial of Service (DoS) through memory exhaustion.
**Learning:** Input validation must include not just range and format checks, but also quantity and size limits for all externally sourced data.
**Prevention:** Enforce strict limits on all deserialized strings (e.g., 64-1024 chars) and collections (e.g., 10-100 items) within hierarchical `IsValid()` methods. Ensure high-level data models recursively validate all nested objects.
## 2024-05-24 - File I/O and JSON Deserialization Exception Handling
**Vulnerability:** Unhandled exceptions during file reading (`File.ReadAllText`) and JSON deserialization (`JsonUtility.FromJson`) in Editor scripts like `CharacterFactory.cs` could crash the editor or leak internal system paths and stack traces to logs.
**Learning:** Unity's built-in file reading and JSON utilities do not fail gracefully on malformed data or missing files. Uncaught exceptions propagate up, potentially exposing sensitive environment structure in the stack trace, which is a risk if logs are aggregated or shared.
**Prevention:** Always wrap file I/O operations and JSON deserialization in `try-catch` blocks. Fail securely by logging generic, safe error messages that do not expose absolute file paths or internal call stacks.
## 2025-05-15 - Resource Exhaustion (DoS) Protection in Data Deserialization
**Vulnerability:** The application was vulnerable to Denial of Service (DoS) attacks via resource exhaustion. Maliciously crafted JSON files with extremely long strings or massive collection sizes could lead to excessive memory consumption or processing time.
**Learning:** Input validation must go beyond just checking for nulls or range bounds; it must also enforce strict limits on the scale of the data being ingested to prevent "billion laughs" style or OOM attacks.
**Prevention:** Enforce maximum lengths for all strings and maximum counts for all collections (Lists/Arrays) in the `IsValid()` method of deserialized data models. For example: `environment` (128 chars), `dialogue` (50 items), `characters` (50 items).
## 2024-05-24 - Untrusted Input Path Traversal
**Vulnerability:** A path traversal vulnerability during dynamic Unity asset creation in `CharacterFactory.cs` due to using untrusted JSON data directly in file paths.
**Learning:** Even internal tool scripts like `CharacterFactory` are vulnerable when accepting unverified external inputs such as JSON. When resolving a merge conflict, the sanitization logic `Path.GetInvalidFileNameChars()` was bypassed because a subsequent `Path.GetFileName(sanitizedName)` declaration accidentally reverted to the original unsanitized string.
**Prevention:** Always ensure that file name sanitization logic accurately processes and transforms the input completely. Validate input strings and use `Path.GetFileName` along with stripping directory traversal operators before embedding them into asset creation paths.
## 2025-05-24 - Syntax Regressions from Mangled Security Patches
**Vulnerability:** Redundant, overlapping, and syntactically invalid security checks (duplicate 'else' blocks and 'IsValid' methods) were found in core managers and data models.
**Learning:** Rapid, repeated application of targeted search-and-replace tools on similar code blocks can lead to "syntax soup" where security logic is present but broken or duplicated, potentially masking actual vulnerabilities or causing build failures.
**Prevention:** Always consolidate security validation into single, well-defined paths and verify the resulting source code for syntactic integrity after automated edits.
## 2025-05-15 - Consolidating Broken Security Validation and Resolving Path Traversal
**Vulnerability:** The project had multiple broken "security fixes" that introduced syntax errors and redundant logic, specifically around deserialized data validation and character asset creation paths. The `IsValid()` pattern was partially implemented but broken, and path traversal mitigation was duplicated and syntactically incorrect.
**Learning:** Incomplete or improperly merged security fixes can be as dangerous as the original vulnerabilities, as they may lead to compilation failures or bypassed security checks. Centralizing validation logic and ensuring clean path sanitization is critical.
**Prevention:** Always perform a full code review and basic sanity check (even if just manual brace counting) after applying security fixes to ensure no regressions or syntax errors are introduced.

## 2025-05-24 - DoS Mitigation via Object Name Whitelisting in Unity Managers
**Vulnerability:** Expensive scene traversal operations like `GameObject.Find` can be abused if triggered by unvalidated external data (e.g. malformed JSON scenarios). Maliciously long or complex strings can cause significant frame drops or application hangs.
**Learning:** Performance optimizations (like caching) often overlap with security mitigations (DoS protection). Simply caching is not enough if the lookup itself can be expensive.
**Prevention:** Implement strict length limits and character whitelisting (Regex) for any string used as a key for scene-wide searches or dynamic resource loading. Pre-compile Regex for performance.
## 2026-04-25 - Prevent IDOR Tampering of Core Singletons via External JSON
**Vulnerability:** The data-driven interaction system (`ApplyInteraction`) allowed arbitrary game objects to be modified (position/scale) by supplying their names in external JSON data. This Insecure Direct Object Reference (IDOR) vulnerability meant core architectural singletons (e.g., `CampaignManager`, `SceneDirector`) could be maliciously manipulated.
**Learning:** In Unity data-driven architectures, using `GameObject.Find` on unsanitized strings from external sources grants uncontrolled access to the entire scene hierarchy. Broad substring checks like `.Contains("Manager")` for protection can cause functional regressions by blocking legitimate game elements.
**Prevention:** Apply strict validation and exact-match blocklists at the application boundary (e.g., where JSON interactions are processed) to protect critical system objects without breaking internal utilities or legitimate gameplay objects.
## 2025-05-24 - Consolidated Path Traversal and DoS Mitigation in Unity
**Vulnerability:** Critical Path Traversal vulnerability in `CharacterFactory.cs` and potential Denial of Service (DoS) vulnerability in `SceneDirector.cs` via unsanitized strings from external campaign data.
**Learning:** "Local" data from JSON files still represents an external attack vector. Blacklist-based sanitization (using `Path.GetInvalidFileNameChars`) is often insufficient or implemented incorrectly across multiple files. Centralizing validation in a single `IsValid()` pattern and using strict whitelist-based regex for path generation and object lookup is more robust.
**Prevention:** Use a strict whitelist regex (e.g., `[^a-zA-Z0-9_\-]`) for all dynamically generated file paths. Validate and limit the length of all strings sourced from external data before passing them to expensive engine methods like `GameObject.Find()`.
## 2026-04-27 - DoS Hardening for Scene Object Lookups
**Vulnerability:** Unsanitized string inputs from external data (JSON) were used directly in `GameObject.Find`. While `GameObject.Find` is relatively slow, very long strings or strings with complex hierarchy paths can be used to cause performance spikes or find unintended objects.
**Learning:** External data used for scene traversal or object lookups must be strictly validated. `GameObject.Find` is particularly sensitive to string length and content.
**Prevention:** Implement length limits (e.g., 128 chars) and a whitelist regex (e.g., `^[a-zA-Z0-9_\s\(\)\-$\.\[\]\/]+$`) for all strings used in scene-wide object lookups.

## 2024-05-25 - IDOR in Scene Interaction Logic
**Vulnerability:** External JSON data could directly address and manipulate critical architectural singletons (e.g., CampaignManager, SceneDirector) using unsanitized object IDs via GameObject.Find, creating an Insecure Direct Object Reference (IDOR) vulnerability.
**Learning:** Applying blocklists or validation in low-level lookup utility methods causes functional regressions. The check must be implemented at the application boundary where the untrusted external input is first processed.
**Prevention:** Always validate and sanitize untrusted external input against an explicit blocklist or allowlist before passing it to scene traversal or object retrieval logic like GameObject.Find.
## 2024-05-24 - IDOR Vulnerability via Unsanitized JSON Data in Scene Interactions
**Vulnerability:** Insecure Direct Object Reference (IDOR) in `SceneDirector.cs` where untrusted external data (JSON `interaction.objectId`) was used to locate and manipulate arbitrary GameObjects via `GameObject.Find`, potentially allowing tampering with critical system managers.
**Learning:** In data-driven Unity architectures, using unsanitized strings directly from external sources to look up GameObjects creates severe vulnerabilities if critical components lack protection boundaries.
**Prevention:** Always validate and sanitize external object IDs. Implement explicit blocklists or allowlists at the application boundary (e.g., `ApplyInteraction`) using exact string matching to protect core architectural singletons from unauthorized manipulation.
## 2024-05-24 - Insecure Direct Object Reference (IDOR) in Scene Interactions
**Vulnerability:** Untrusted external JSON data (`interaction.objectId`) was used directly in `ApplyInteraction` to query the scene hierarchy via `GameObject.Find` and manipulate arbitrary objects.
**Learning:** In data-driven architectures, granting external configuration files unrestricted access to the scene hierarchy allows malicious files to tamper with critical system managers (e.g., `CampaignManager`, `SceneDirector`), leading to unpredictable behavior or security bypasses.
**Prevention:** Apply validation and blocklists (e.g., for core singletons) at the application boundary where untrusted external input is first processed (e.g., in `ApplyInteraction`) before it reaches low-level object retrieval utilities.
## 2025-05-24 - Prevent IDOR in Unity Data-Driven Interactions
**Vulnerability:** Insecure Direct Object Reference (IDOR) in `SceneDirector.cs` where untrusted external input (JSON) was passed directly to `GameObject.Find` via `GetCachedObject` during `ApplyInteraction`, allowing manipulation of any scene object.
**Learning:** Using unsanitized external strings to look up and modify GameObjects grants uncontrolled access to the scene hierarchy. Validation and blocklists must be applied at the application boundary where untrusted input is first processed, rather than inside generic internal retrieval utilities, to prevent functional regressions in internal logic.
**Prevention:** Always sanitize and validate object IDs from external sources against an explicit allowlist or blocklist before using them to interact with the game world.
## 2024-05-24 - Information Disclosure via Exception Messages in Logs
**Vulnerability:** Found `Debug.LogError` statements in `CampaignManager.cs` outputting `ex.Message` directly in catch blocks. This can inadvertently leak absolute file paths or internal system details if the exception message contains them.
**Learning:** Exception messages often contain platform-specific paths or environment details that should not be exposed in production logs.
**Prevention:** Mask runtime exception details by logging generic error messages (e.g., "See logs for details") and ensure that any file-related logs only use relative names or identifiers rather than full paths.
## 2024-05-24 - IDOR Vulnerability in Unity Data-Driven Architecture
**Vulnerability:** External JSON data could directly manipulate core singleton managers via GameObject lookups.
**Learning:** Unsanitized string IDs used for scene object retrieval create an Insecure Direct Object Reference (IDOR) risk, granting unintended access to architectural singletons.
**Prevention:** Apply explicit blocklists or validation boundaries where untrusted external data is first handled, rather than inside generic retrieval utilities, to protect singletons without breaking internal logic.
## 2026-05-05 - IDOR in SceneDirector ApplyInteraction
**Vulnerability:** Data-driven external input (JSON) in `ApplyInteraction` directly passed unsanitized strings to `GetCachedObject` (which uses `GameObject.Find`), allowing uncontrolled access to manipulate any scene hierarchy object.
**Learning:** This created an Insecure Direct Object Reference (IDOR) vulnerability where core architectural managers could be maliciously relocated or resized via external payloads.
**Prevention:** Apply validation and blocklists (e.g., blocking 'CampaignManager', 'SceneDirector', 'CameraManager', 'AlliancePowerManager') at the application boundary where untrusted external input is first processed, not in low-level utilities.
## 2026-05-07 - Unity IDOR via GameObject.Find
**Vulnerability:** External JSON data could directly reference and manipulate critical singleton managers (like CampaignManager) via `GameObject.Find` wrappers.
**Learning:** Data-driven architectures that pass external IDs directly to generic lookup utilities without boundary validation create Insecure Direct Object References (IDOR), allowing untrusted data to tamper with global state.
**Prevention:** Apply blocklists or whitelists at the application boundary (e.g., interaction processing) to sanitize external object IDs before they reach low-level lookup utilities.
## 2024-05-24 - Insecure Direct Object Reference in SceneDirector
**Vulnerability:** External JSON data could arbitrarily access and manipulate any scene object via `GameObject.Find` when processing object interactions, including core system managers.
**Learning:** Using `GameObject.Find` directly on unsanitized strings from external sources creates an IDOR vulnerability, allowing untrusted data to bypass intended access controls.
**Prevention:** Apply validation and blocklists (e.g., protecting CampaignManager, SceneDirector, CameraManager, AlliancePowerManager) at the application boundary where untrusted external input is first processed.
## 2024-05-25 - Insecure Direct Object Reference (IDOR) via Unity GameObject.Find
**Vulnerability:** `ApplyInteraction` in `SceneDirector.cs` allowed manipulating arbitrary game objects by passing their names from external JSON directly into a method that eventually calls `GameObject.Find`.
**Learning:** Exposing `GameObject.Find` to unsanitized external strings allows attackers to manipulate core architectural singletons and managers (e.g., CampaignManager), leading to unauthorized state changes.
**Prevention:** Apply validation and blocklists at the application boundary where untrusted external input is first processed, rather than in generic internal retrieval utilities, to prevent IDOR while avoiding regressions in internal logic.
## 2025-05-24 - Consolidated Security Validation and Resource Exhaustion Protection
**Vulnerability:** Massive "code rot" with multiple redundant, conflicting, and syntactically invalid 'IsValid()' methods and 'try-catch' blocks across core data and management scripts. This led to unpredictable security postures, potential information disclosure (absolute paths in logs), and vulnerability to Resource Exhaustion (DoS) via unconstrained JSON payloads.
**Learning:** Overlapping and improperly merged security patches can create "syntax soup" that disables the very protections they intend to provide. High-frequency edits with automated tools without manual consolidation can lead to multiple method signatures and mismatched braces.
**Prevention:** Always consolidate security validation into a single, robust 'IsValid()' framework. Enforce strict 'Sentinel Standard' resource limits (string lengths, collection counts) and range checks at the point of ingestion. Consistently use the 'fail secure' pattern by nulling data on validation failure and masking internal system details in logs.

## 2024-05-24 - Consolidated IDOR protections in ApplyInteraction
**Vulnerability:** ApplyInteraction had an excessive amount of duplicate and malformed IDOR checks, leading to a large block of unmaintainable code.
**Learning:** When automated tools repeatedly apply patches, they can create duplicate logic. It is important to consolidate these checks into a single, clean validation step.
**Prevention:** Use a single blocklist check for protected managers at the beginning of the interaction application.

## 2025-05-15 - Consolidating Security Validation 'Syntax Soup'
**Vulnerability:** Found extreme 'code rot' in `HorizonGameData.cs` where multiple conflicting, redundant, and syntactically invalid `IsValid()` methods were nested or duplicated. This effectively disabled or bypassed intended security checks and resource limits.
**Learning:** High-frequency, automated security patching can lead to "syntax soup" if not consolidated. Broken validation logic is equivalent to no validation, leaving the application vulnerable to DoS (resource exhaustion) and malformed data ingestion.
**Prevention:** Consolidate all validation into a single, robust `IsValid()` method per class. Enforce strict resource limits (string lengths, collection sizes) and always verify the final source code integrity (e.g., via compilation checks) after automated edits.

## 2024-05-24 - Resolving Data Ingestion 'Syntax Soup' and Broken Validation
**Vulnerability:** Found extreme "code rot" and "syntax soup" in `HorizonGameData.cs` and `CharacterFactory.cs`, where multiple redundant, malformed, and contradictory `IsValid()` methods and variable declarations were introduced by botched merges. This effectively bypassed or broke security validation for external JSON data.
**Learning:** High-frequency automated patching can lead to overlapping logic that disables the very protections it intends to provide. Broken validation logic is equivalent to no validation, leaving the application vulnerable to DoS (resource exhaustion) and malformed data ingestion.
**Prevention:** Consolidate all validation into single, robust methods (`IsValid`) and strictly enforce "Sentinel Standard" resource limits (string lengths, collection counts) at the point of ingestion. Always verify syntactic integrity via compilation checks after resolving complex merge-induced duplication.

## 2026-05-14 - IDOR Bypass via Path-like Object IDs in Unity
**Vulnerability:** Insecure Direct Object Reference (IDOR) protection using simple string matching on GameObject names can be bypassed if the lookup utility (like `GameObject.Find`) supports hierarchical paths. Prepending a slash (e.g., `/CampaignManager`) could bypass a blacklist check for `CampaignManager`.
**Learning:** Unity's `GameObject.Find` can take a path. Security boundaries for object lookups must account for this by normalizing or sanitizing the input ID before validation.
**Prevention:** Always sanitize or normalize external IDs (e.g., using `TrimStart('/')`) before checking them against a protected list of system objects.
## 2024-05-24 - IDOR Bypass via Path-like Object IDs and Protected Manager Expansion
**Vulnerability:** IDOR vulnerability in `SceneDirector.cs` where an attacker could bypass the `protectedManagers` check by prepending a slash to the object ID (e.g., `/CampaignManager`). Additionally, `GlobalResonanceManager` was missing from the protected managers list.
**Learning:** Unity's `GameObject.Find` supports hierarchical paths, so exact string matching against a whitelist/blacklist must be preceded by trimming leading directory separators.
**Prevention:** Always use `.TrimStart('/')` on external object IDs before comparing them against protected manager lists to prevent path-based bypasses in scene-wide lookups.
## 2024-05-24 - Expensive Native Boundary Crossing in Cryptographic Salt
**Vulnerability:** The `ProcessXOR` method repeatedly queries `UnityEngine.SystemInfo.deviceUniqueIdentifier` to generate a salt for obfuscation. This causes an expensive native C++ boundary crossing on every save/load operation, leading to potential micro-stutters. Furthermore, if the identifier is unavailable, it defaults to a hardcoded salt `MILEHIGH_FALLBACK_SALT`, which is insecure and predictable.
**Learning:** Frequent native boundary crossings in Unity should be avoided, especially in critical paths like saving/loading data. Hardware identifiers like `deviceUniqueIdentifier` should be cached upon first access.
**Prevention:** Cache hardware identifiers in a static variable upon first access to prevent I/O stutters during frequent runtime operations. Re-evaluate the necessity of XOR obfuscation and consider stronger encryption if sensitive data is involved.

## 2024-05-16 - Hierarchical IDOR Bypass and Robust Numeric Validation
**Vulnerability:** IDOR protection in `SceneDirector.cs` could be bypassed by using path segments (e.g., "Parent/CampaignManager") because it only checked the full trimmed ID against protected managers. Additionally, floating-point fields in `HorizonGameData.cs` lacked `NaN` and `Infinity` validation, which could lead to "physics explosions" or logic bypasses.
**Learning:** Unity's `GameObject.Find` supports hierarchical paths, so security boundaries must verify *every* segment of a path-like ID, not just the leaf or the full string. Furthermore, standard range checks (`x >= 0`) are insufficient for untrusted numeric input as they may not catch `NaN` or `Infinity`.
**Prevention:** Split hierarchical object IDs by '/' and verify every resulting segment against the `_protectedManagers` whitelist. Always include `float.IsNaN` and `float.IsInfinity` checks in data validation logic for deserialized floating-point values.
## 2024-05-26 - Missing IDOR Protection for CombatManager
**Vulnerability:** The `CombatManager` singleton was missing from the `_protectedManagers` blocklist in `SceneDirector.cs`.
**Learning:** Any new core manager or singleton must be explicitly added to the IDOR blocklists to prevent external data from manipulating its state via `GameObject.Find`.
**Prevention:** Maintain a comprehensive list of all critical singletons and ensure they are all included in the `_protectedManagers` blocklist.

## 2026-05-19 - Hardcoded Salt in XOR Obfuscation

**Vulnerability:** Weak Cryptography / Hardcoded Salt in `CampaignManager.cs`. The use of a static, hardcoded string literal as a fallback XOR salt made data obfuscation trivial to reverse through static analysis.

**Learning:** Relying on hardcoded secrets within source code for any form of cryptographic or obfuscation operation is a major security risk. Even if the goal is only "obfuscation", a hardcoded key provides zero security against a motivated attacker.

**Prevention:** Always derive keys or salts from environment-specific or application-specific metadata if a true unique identifier (like `deviceUniqueIdentifier`) is unavailable. Avoid string literals for sensitive values.
## 2024-05-19 - Missing Managers in IDOR Blocklist
**Vulnerability:** Several critical core managers (GameManager, BackendSyncService) were missing from the `_protectedManagers` blocklist in `SceneDirector.cs`, exposing them to unauthorized interaction and potential IDOR vulnerabilities.
**Learning:** Hardcoded blocklists for dynamic core systems are prone to omitting newly added or less prominent singleton managers, leaving gaps in IDOR protection.
**Prevention:** Ensure all critical singletons and core managers are explicitly included in the `_protectedManagers` HashSet blocklist within `SceneDirector.cs` to block unauthorized external access via `GameObject.Find`.
## 2024-05-27 - Missing IDOR Protection for RealitySyncEngine
**Vulnerability:** The `RealitySyncEngine` singleton was missing from the `_protectedManagers` blocklist in `SceneDirector.cs`.
**Learning:** Any new core manager or singleton must be explicitly added to the IDOR blocklists to prevent external data from manipulating its state via `GameObject.Find`.
**Prevention:** Maintain a comprehensive list of all critical singletons and ensure they are all included in the `_protectedManagers` blocklist.

## 2024-05-26 - Missing RealitySyncEngine in IDOR Blocklist
**Vulnerability:** The `RealitySyncEngine` singleton was missing from the `_protectedManagers` blocklist in `SceneDirector.cs`.
**Learning:** New or lesser-known critical managers are often missed when manually maintaining IDOR blocklists, leaving them exposed to unauthorized manipulation via external data lookups (`GameObject.Find`).
**Prevention:** Maintain a comprehensive list of all critical singletons and ensure they are all strictly included in the `_protectedManagers` blocklist.
## 2026-05-25 - Prevent IDOR on RealitySyncEngine
**Vulnerability:** Insecure Direct Object Reference (IDOR) allows unauthorized modification of `RealitySyncEngine` via `GameObject.Find` in `SceneDirector.ApplyInteraction`.
**Learning:** Newly created core managers and singletons are not automatically protected from external interaction systems, exposing critical state (like reality synchronization) to manipulation.
**Prevention:** Always add new core manager classes to the `_protectedManagers` blocklist in `SceneDirector.cs` when implementing them.
## 2024-05-21 - Missing IDOR Protection for RealitySyncEngine
**Vulnerability:** The `RealitySyncEngine` core manager was missing from the `_protectedManagers` blocklist in `SceneDirector.cs`, allowing unauthorized external modification via object interactions (Insecure Direct Object Reference).
**Learning:** Core singleton managers that govern critical game state (like RealitySyncEngine) must be explicitly protected against arbitrary `GameObject.Find` resolution in interaction handlers.
**Prevention:** Whenever a new critical singleton or manager is added, it must also be added to the `_protectedManagers` HashSet in `SceneDirector` to block malicious path traversal or direct object references.
## 2024-05-24 - Prevent IDOR on RealitySyncEngine
**Vulnerability:** Insecure Direct Object Reference (IDOR) vulnerability where `RealitySyncEngine` was missing from the `_protectedManagers` blocklist in `SceneDirector.cs`, allowing unauthorized external modification.
**Learning:** Critical singletons handling deterministic reality states must be explicitly protected against arbitrary `GameObject.Find` access via `SceneDirector` interactions.
**Prevention:** Ensure all core singleton managers and critical systems are explicitly included in the `_protectedManagers` HashSet blocklist to prevent unauthorized external access.

## 2026-05-29 - Securing Vitis AI 6.1 Integration and Core Reality Engine
**Vulnerability:** The introduction of new core systems like `TimelineSimulationEngine` and `VitisAIBridge` creates new targets for IDOR attacks if not explicitly protected in the scene-wide lookup blocklist.
**Learning:** Every architectural addition that acts as a singleton or core manager must be immediately registered with the security boundary layer (`SceneDirector.cs`) to maintain the integrity of the simulation.
**Prevention:** Strictly enforce the inclusion of all new core managers in the `_protectedManagers` HashSet to block unauthorized external access via `GameObject.Find`.
## 2024-06-07 - IDOR Vulnerability in SceneDirector Protected Managers
**Vulnerability:** Critical singletons and core managers (HarmonicTerrainEngine, BicameralBattleEngine) were missing from the _protectedManagers blocklist in SceneDirector.cs.
**Learning:** Hardcoded, manually maintained blocklists are prone to omissions as new core engines are added, enabling Insecure Direct Object Reference (IDOR).
**Prevention:** Consider implementing an attribute-based system (e.g., [ProtectedManager]) and dynamically populating the blocklist via reflection.
## 2026-06-08 - Insecure Direct Object Reference (IDOR) via GameObject.Find
**Vulnerability:** Core singleton managers could be accessed and manipulated via unauthorized GameObject.Find paths due to a malformed blocklist in SceneDirector.cs.
**Learning:** SceneDirector.cs parses arbitrary strings to locate and modify objects. A syntax error and missing entries in the blocklist failed to protect critical managers from IDOR.
**Prevention:** Always maintain a well-formatted blocklist for sensitive game objects and run compilation checks to ensure HashSet initializers do not contain syntax errors.
## 2026-06-09 - IDOR Vulnerability via GameObject.Find

**Vulnerability:** A missing comma in the _protectedManagers HashSet initialization caused silent C# compilation failures, potentially leaving the blocklist broken.
**Learning:** The syntax error was overlooked because the custom Python validation script does not compile C# code.
**Prevention:** Always manually verify C# syntax correctness. Ensure all critical singletons and core managers are explicitly included in the _protectedManagers HashSet to block unauthorized external access via GameObject.Find.
## 2026-06-10 - Malformed HashSet Breaking IDOR Blocklist
**Vulnerability:** A C# syntax error (missing comma) in the `_protectedManagers` HashSet in `SceneDirector.cs` broke the compilation/evaluation of the blocklist, allowing unauthorized IDOR access to core managers.
**Learning:** Security blocklists must be syntax-checked and validated. A simple syntax error in a hardcoded list can silently fail or break the entire defense-in-depth layer if not caught by compilation or tests.
**Prevention:** Ensure C# syntax correctness in critical security blocklists and include unit tests that explicitly verify the blocklist contains the expected managers.
## 2026-06-11 - IDOR Protection Failure due to C# Syntax Error
**Vulnerability:** A malformed HashSet missing commas in the SceneDirector blocklist failed to compile, leaving critical singletons unprotected from Insecure Direct Object Reference (IDOR) attacks via GameObject.Find.
**Learning:** Security blocklists implemented as static C# collections can fail completely if they contain syntax errors (like missing commas when merging lists), neutralizing the defense.
**Prevention:** Always verify C# syntax correctness manually or via strict build pipelines, as simple compilation errors can silently break security features if not caught.
## 2026-06-15 - IDOR Blocklist Syntax Error Fixed
**Vulnerability:** A malformed _protectedManagers HashSet in SceneDirector.cs contained missing commas and duplicate entries, which would break the IDOR blocklist and potentially expose core managers (like GameManager, RealitySyncEngine, and CombatManager) to unauthorized external access via GameObject.Find.
**Learning:** Hardcoded security blocklists are prone to human error during manual merges or updates, leading to invalid C# syntax that breaks the security mechanism entirely.
**Prevention:** Implement unit tests that validate the syntactic correctness and completeness of security blocklists to ensure all required managers are present.
## 2024-05-14 - Malformed IDOR Blocklist Exposing Core Managers
**Vulnerability:** A missing comma and duplicate entries in the C# `_protectedManagers` HashSet blocklist within `SceneDirector.cs` caused a syntax error, effectively breaking compilation and the IDOR protection mechanism that blocks unauthorized access to core singletons (e.g., `CombatManager`, `GameManager`) via `GameObject.Find`.
**Learning:** C# syntax errors in data structures (like malformed HashSets) can silently fail if there's no active compilation check or test runner, leading to critical security mechanisms being completely bypassed or deployed broken.
**Prevention:** Always manually ensure C# syntax correctness for security-critical blocklists, and utilize a proper C# compiler step in CI/CD rather than relying solely on file-existence validation scripts.
## 2024-06-17 - Malformed HashSet Breaking IDOR Protection
**Vulnerability:** A C# syntax error (missing comma and duplicated list) in the `_protectedManagers` blocklist inside `SceneDirector.cs` compromised the IDOR protection mechanism, potentially allowing unauthorized access to core managers via `GameObject.Find`.
**Learning:** Hardcoded security blocklists are susceptible to copy-paste errors and syntax mistakes that might go unnoticed if compilation checks aren't rigorously enforced in CI/CD alongside data validation scripts.
**Prevention:** Ensure C# compilation checks are included in the verification pipeline and format security arrays properly with commas and comments for each entry to avoid merge/copy-paste conflicts.

## 2024-06-23 - Malformed IDOR Blocklist
**Vulnerability:** The `_protectedManagers` HashSet in `SceneDirector.cs` contained missing commas and duplicate lists, causing a C# compilation failure and leaving IDOR protection broken.
**Learning:** Hardcoded security lists are highly vulnerable to merge conflicts or copy-paste errors. When the C# file has syntax errors, the blocklist completely fails.
**Prevention:** Always check C# files for basic syntax correctness, especially after merging or updating hardcoded security configurations.
