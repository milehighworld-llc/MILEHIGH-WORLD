// Copyright 2026 MILEHIGH-WORLD LLC. All Rights Reserved.
// PROPRIETARY AND CONFIDENTIAL: DO NOT DISTRIBUTE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using MilehighWorld.Data;
using MilehighWorld.Characters;

namespace MilehighWorld.Core
{
    public class SceneDirector : MonoBehaviour
    {
        public List<GameObject> characterPrefabs = new List<GameObject>();
        public Transform characterSpawnRoot = null!;

        private readonly Dictionary<string, GameObject?> _objectCache = new Dictionary<string, GameObject?>();
        private readonly Dictionary<string, GameObject?> _prefabCache = new Dictionary<string, GameObject?>();
        private readonly Dictionary<int, CharacterControllerBase?> _controllerCache = new Dictionary<int, CharacterControllerBase?>();

        // 🛡️ Sentinel: Prevent IDOR (Insecure Direct Object Reference) by blocking GameObject.Find access to core singletons.
        // 🛡️ Sentinel: Protected core singletons from Insecure Direct Object Reference (IDOR) access via GameObject.Find.
        private static readonly HashSet<string> _protectedManagers = new HashSet<string>
        {
            "CampaignManager", "SceneDirector", "CameraManager", "AlliancePowerManager", "GlobalResonanceManager", "CombatManager", "EncounterDirector", "NarrativeActionResolver", "GameManager", "BackendSyncService", "RealitySyncEngine",
            "RealityAnchor", "BicameralBattleEngine", "FoxParadeDirector", "CinematicController", "TimelineSimulationEngine", "VitisAIBridge", "IXNodeController", "HarmonicTerrainEngine"
        };

        private static readonly Regex _nameValidator = new Regex(@"^[a-zA-Z0-9_\s\(\)\-$\.\/\[\]]+$", RegexOptions.Compiled);

        private void Awake()
        {
            InitializePrefabCache();
        }

        private void Start()
        {
            var campaignData = CampaignManager.Instance.currentCampaignData;
            if (campaignData != null && campaignData.scenarios != null && campaignData.scenarios.Count > 0)
            {
                SetupScene(campaignData.scenarios[0]);
            }
        }

        private void OnDestroy()
        {
            _objectCache.Clear();
            _prefabCache.Clear();
            _controllerCache.Clear();
        }

        private void InitializePrefabCache()
        {
            _prefabCache.Clear();
            if (characterPrefabs != null)
            {
                foreach (var prefab in characterPrefabs)
                {
                    if (prefab != null && !string.IsNullOrEmpty(prefab.name))
                    {
                        _prefabCache[prefab.name] = prefab;
                    }
                }
            }
        }

        public void SetupScene(SceneScenario scenario)
        {
            if (scenario == null || !scenario.IsValid())
            {
                return;
            }
            Debug.Log($"Setting up scenario: {scenario.scenarioId}");

            _objectCache.Clear();
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && !string.IsNullOrEmpty(go.name) && !_objectCache.ContainsKey(go.name))
                {
                    _objectCache[go.name] = go;
                }
            }

            var campaignData = CampaignManager.Instance.currentCampaignData;
            if (campaignData != null && campaignData.characters != null)
            {
                foreach (var charProfile in campaignData.characters)
                {
                    if (charProfile != null)
                    {
                        SpawnOrUpdateCharacter(charProfile);
                    }
                }
            }

            if (scenario.interactiveObjects != null)
            {
                foreach (var interaction in scenario.interactiveObjects)
                {
                    if (interaction != null)
                    {
                        ApplyInteraction(interaction);
                    }
                }
            }
        }

        public void SpawnOrUpdateCharacter(CharacterProfile profile)
        {
            if (profile == null || !profile.IsValid())
            {
                return;
            }

            GameObject? characterObj = GetCachedObject(profile.name);

            if (characterObj == null)
            {
                GameObject? prefab = GetPrefab(profile.name);
                if (prefab != null)
                {
                    characterObj = Instantiate<GameObject>(prefab, characterSpawnRoot);
                    characterObj.name = profile.name;
                    _objectCache[profile.name] = characterObj;
                }
            }

            if (characterObj != null)
            {
                var controller = GetCharacterController(characterObj);
                if (controller != null)
                {
                    CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
                    data.characterName = profile.name;
                    data.role = profile.role;
                    data.traits = profile.traits;
                    data.behaviorScript = profile.behaviorScript;

                    controller.Initialize(data);
                }
            }
        }

        private void ApplyInteraction(ObjectInteraction interaction)
        {
            if (interaction == null || string.IsNullOrEmpty(interaction.objectId))
            {
                return;
            }

            string sanitizedId = interaction.objectId.TrimStart('/');
            string[] segments = sanitizedId.Split('/');
            foreach (string segment in segments)
            {
                if (_protectedManagers.Contains(segment))
                {
                    Debug.LogWarning($"[Security] Blocked unauthorized interaction attempt on core system: {sanitizedId}");
                    return;
                }
            }

            GameObject? target = GetCachedObject(interaction.objectId);

            if (target != null)
            {
                if (interaction.isVector)
                {
                    target.transform.position = interaction.GetVectorValue();
                }
                else
                {
                    target.transform.localScale = UnityEngine.Vector3.one * interaction.floatValue;
                }
            }
        }

        private GameObject? GetCachedObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            if (objectName.Length > 128 || !_nameValidator.IsMatch(objectName))
            {
                Debug.LogWarning($"[Security] GetCachedObject blocked potentially malicious input: {objectName}");
                return null;
            }

            if (_objectCache.TryGetValue(objectName, out GameObject? obj))
            {
                // ⚡ Bolt: Check for valid object. Re-fetch if the object was destroyed.
                if (obj != null) return obj;
                if (obj != null)
                {
                    return obj;
                }
                // ⚡ Bolt: Use ReferenceEquals for robust negative caching, skipping O(N) GameObject.Find for truly missing objects.
                if (System.Object.ReferenceEquals(obj, null) || obj != null) return obj;
            }

            obj = GameObject.Find(objectName);
            // ⚡ Bolt Optimization: Only cache actual objects for GetCachedObject because scene objects are dynamic
            // Caching null here breaks late-instantiated objects.
            if (obj != null) _objectCache[objectName] = obj;
            return obj;
        }

        private GameObject? GetPrefab(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                return null;
            }

            if (_prefabCache.TryGetValue(profileName, out GameObject? prefab))
            {
                // ⚡ Bolt: Robust negative caching for static prefabs
                if (System.Object.ReferenceEquals(prefab, null)) return null;
                if (prefab != null) return prefab;
                if (prefab != null)
                {
                    return prefab;
                }
            }

            // ⚡ Bolt: Cache missing static prefabs as null to prevent repeated O(P) list searches
            prefab = characterPrefabs?.Find(p => p != null && (p.name == profileName || p.name.Contains(profileName)));
            _prefabCache[profileName] = prefab;
            if (prefab != null)
            {
                _prefabCache[profileName] = prefab;
            }
            return prefab;
        }

        private CharacterControllerBase? GetCharacterController(GameObject characterObj)
        {
            int id = characterObj.GetInstanceID();
            // ⚡ Bolt: Implement negative caching using ReferenceEquals to safely skip redundant GetComponent calls on GameObjects that definitively lack the component.
            if (_controllerCache.TryGetValue(id, out CharacterControllerBase? controller))
            {
                // ⚡ Bolt: Robust negative caching for components
                if (System.Object.ReferenceEquals(controller, null)) return null;
                if (controller != null) return controller;
                if (System.Object.ReferenceEquals(controller, null) || controller != null) return controller;
            }

            // ⚡ Bolt: Cache missing components as null to prevent repeated GetComponent overhead
            controller = characterObj.GetComponent<CharacterControllerBase>();
            _controllerCache[id] = controller;

            controller = characterObj.GetComponent<CharacterControllerBase>();
            if (controller != null)
            {
                _controllerCache[id] = controller;
            }
            return controller;
        }
    }
}
