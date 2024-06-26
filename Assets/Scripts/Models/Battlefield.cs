﻿using System.Collections.Generic;
using Scorewarrior.Test.Configs;
using Scorewarrior.Test.Views;
using UnityEngine;
using UnityEngine.Events;

namespace Scorewarrior.Test.Models
{
    public class Battlefield
    {
        private readonly Dictionary<Faction, List<Vector3>> _spawnPositionsByTeam;
        private readonly Dictionary<Faction, List<Character>> _charactersByTeam;
        private readonly Dictionary<Faction, int> _deadCharactersByTeam;
        private readonly CharacterModifierConfig[] _characterModifiers;
        private readonly WeaponModifierConfig[] _weaponModifiers;

        private bool _paused;

        public Battlefield(
            Dictionary<Faction, List<Vector3>> spawnPositionsByTeam,
            CharacterModifierConfig[] characterModifiers,
            WeaponModifierConfig[] weaponModifiers
        )
        {
            _spawnPositionsByTeam = spawnPositionsByTeam;
            _charactersByTeam = new();
            _deadCharactersByTeam = new();
            _characterModifiers = characterModifiers;
            _weaponModifiers = weaponModifiers;
        }

        public void Start(CharacterPrefab[] prefabs, UnityEvent teamLostEvent)
        {
            _paused = false;
            _charactersByTeam.Clear();

            List<CharacterPrefab> availablePrefabs = new List<CharacterPrefab>(prefabs);
            foreach (var positionsPair in _spawnPositionsByTeam)
            {
                List<Vector3> positions = positionsPair.Value;
                List<Character> characters = new List<Character>();
                _charactersByTeam.Add(positionsPair.Key, characters);
                _deadCharactersByTeam.Add(positionsPair.Key, 0);
                int i = 0;
                while (i < positions.Count && availablePrefabs.Count > 0)
                {
                    int index = Random.Range(0, availablePrefabs.Count);
                    var characterFaction = positionsPair.Key;
                    var newCharacter = CreateCharacterAt(availablePrefabs[index], this, positions[i], characterFaction);
                    newCharacter.GetDeathEvent().AddListener(() => {
                        _deadCharactersByTeam[characterFaction]++;
                        if (_deadCharactersByTeam[characterFaction] >= _spawnPositionsByTeam[characterFaction].Count)
                        {
                            teamLostEvent.Invoke();
                            _paused = true;
                        }
                    });
                    characters.Add(newCharacter);
                    availablePrefabs.RemoveAt(index);
                    i++;
                }
            }
        }

        public bool TryGetNearestAliveEnemy(Character character, out Character target)
        {
            Character nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            var enemies = character.GetFaction() == Faction.Ally ? _charactersByTeam[Faction.Enemy] : _charactersByTeam[Faction.Ally];
            foreach (Character enemy in enemies)
            {
                if (enemy.IsAlive)
                {
                    float distance = Vector3.Distance(character.Position, enemy.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }
            target = nearestEnemy;
            return target != null;
        }

        public void Update(float deltaTime)
        {
            if (_paused)
                return;

            foreach (var charactersPair in _charactersByTeam)
            {
                List<Character> characters = charactersPair.Value;
                foreach (Character character in characters)
                {
                    character.Update(deltaTime);
                }
            }
        }

        public void Destroy()
        {
            foreach (var characterPair in _charactersByTeam)
            {
                foreach (var character in characterPair.Value)
                {
                    MonoBehaviour.Destroy(character.Prefab.gameObject);
                }
            }
        }

        private Character CreateCharacterAt(CharacterPrefab prefab, Battlefield battlefield, Vector3 position, Faction faction)
        {
            CharacterPrefab character = Object.Instantiate(prefab, position, Quaternion.identity);
            return new Character(character, new Weapon(character.GetWeapon(), _weaponModifiers), battlefield, faction, _characterModifiers);
        }
    }
}