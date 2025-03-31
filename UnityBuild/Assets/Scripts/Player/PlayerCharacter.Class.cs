using System.Collections.Generic;
using Cinemachine;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public partial class PlayerCharacter
    {
        [SyncVar(hook = nameof(OnCharacterClassChanged))]
        public Constants.CharacterClass PLayerCharacterClass = Constants.CharacterClass.None; // ✅ 직업 동기화

        [SyncVar(hook = nameof(OnMoveSkillChanged))]
        private Constants.SkillType MoveSkill = Constants.SkillType.None; // ✅ 이동 스킬 동기화

        [SyncVar(hook = nameof(OnAttackSkillsChanged))]
        private int[] AttackSkills = null; // ✅ 공격 스킬 동기화

        [Header("Character Models")]
        public GameObject[] mageModel;
        public GameObject[] archerModel;
        public GameObject[] warriorModel;
        public GameObject[] necromancerModel;
        public GameObject[] priestModel;

        private Dictionary<Constants.CharacterClass, GameObject[]> characterModels;

        private void InitializeCharacterModels()
        {
            // ✅ 직업별 모델을 Dictionary에 저장
            characterModels = new Dictionary<Constants.CharacterClass, GameObject[]>
            {
                { Constants.CharacterClass.Mage, mageModel },
                { Constants.CharacterClass.Archer, archerModel },
                { Constants.CharacterClass.Warrior, warriorModel },
                { Constants.CharacterClass.Necromancer, necromancerModel },
                { Constants.CharacterClass.Priest, priestModel }
            };
        }

        // 서버에서 동기화된 데이터를 설정
        [Command]
        public void CmdSetCharacterData(Constants.CharacterClass newClass, Constants.SkillType newMoveSkill, int[] newAttackSkills)
        {
            if (!NetworkClient.active)
            {
                OnCharacterClassChanged(PLayerCharacterClass, newClass);
                OnMoveSkillChanged(MoveSkill, newMoveSkill);
                OnAttackSkillsChanged(AttackSkills, newAttackSkills);
            }
            PLayerCharacterClass = newClass;
            MoveSkill = newMoveSkill;
            AttackSkills = newAttackSkills;

            if (NetworkClient.active)
            {
                buffSystem?.CmdClearAllBuffs();
            }
            else
            {
                buffSystem?.ServerClearAllBuffs();
            }
        }

        // 캐릭터 클래스 변경 시 호출될 동기화 함수
        private void OnCharacterClassChanged(Constants.CharacterClass oldClass, Constants.CharacterClass newClass)
        {
            if (newClass == Constants.CharacterClass.None)
            {
                return;
            }
            animator.SetFloat("Blend", (int)newClass);
            ApplyCharacterClass(newClass);
            UpdateCount();
        }

        private void ApplyCharacterClass(Constants.CharacterClass newClass)
        {
            if (newClass == Constants.CharacterClass.None)
            {
                return;
            }
            ActivateCharacterModel(newClass);
        }

        // 이동 스킬 변경 시 호출될 동기화 함수
        private void OnMoveSkillChanged(Constants.SkillType oldSkill, Constants.SkillType newSkill)
        {
            if (newSkill == Constants.SkillType.None)
            {
                return;
            }
            SetMovementSkill(newSkill);
        }

        // 공격 스킬 변경 시 호출될 동기화 함수
        private void OnAttackSkillsChanged(int[] oldSkills, int[] newSkills)
        {
            if (newSkills == null)
                return;

            for (int i = 1; i <= 3; i++)
            {
                SetAvailableAttack(i, newSkills[i]);
            }

            // ✅ 스킬 바뀐 이후 카드 적용
            if (PlayerSetting.PlayerCards != null && PlayerSetting.PlayerCards.Count > 0)
            {
                ApplyCardBonuses(PlayerSetting.PlayerCards);
            }

            if (isOwned && playerUI == null)
            {
                playerUI = FindFirstObjectByType<PlayerCharacterUI>();
                lastMovementSkillTime = -Mathf.Infinity;
            }
        }
        public void SetCharacterClass(Constants.CharacterClass cls)
        {
            if (characterModels == null)
                InitializeCharacterModels();

            PLayerCharacterClass = cls;
            ApplyCharacterClass(cls);
        }
        private void ActivateCharacterModel(Constants.CharacterClass newClass)
        {
            foreach (var model in characterModels.Values)
            {
                foreach (var mGameObject in model)
                {
                    if (mGameObject != null)
                        mGameObject.SetActive(false); // ✅ 모든 모델 비활성화
                }
            }

            if (characterModels.ContainsKey(newClass) && characterModels[newClass] != null)
            {
                
                foreach (var model in characterModels[newClass])
                {
                    model.SetActive(true); // ✅ 해당 직업 모델만 활성화
                }
                
            }
        }
    }
}