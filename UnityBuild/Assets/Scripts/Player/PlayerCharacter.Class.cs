using System.Collections.Generic;
using Cinemachine;
using DataSystem;
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
        [SerializeField] private Constants.CharacterClass characterClass; // ✅ 직업 동기화

        [Header("Character Models")]
        public GameObject[] mageModel;
        public GameObject[] archerModel;
        public GameObject[] warriorModel;

        private Dictionary<Constants.CharacterClass, GameObject[]> characterModels;

        private void InitializeCharacterModels()
        {
            // ✅ 직업별 모델을 Dictionary에 저장
            characterModels = new Dictionary<Constants.CharacterClass, GameObject[]>
            {
                { Constants.CharacterClass.Mage, mageModel },
                { Constants.CharacterClass.Archer, archerModel },
                { Constants.CharacterClass.Warrior, warriorModel }
            };
        }

        public void SetCharacterClass(Constants.CharacterClass newClass)
        {
            animator.SetFloat("Blend", (int)newClass);
            if (!isServer)
            {
                CmdSetCharacterClass(newClass);
            }
            else
            {
                ApplyCharacterClass(newClass);
            }
        }

        [Command]
        private void CmdSetCharacterClass(Constants.CharacterClass newClass)
        {
            ApplyCharacterClass(newClass);
        }

        private void ApplyCharacterClass(Constants.CharacterClass newClass)
        {
            characterClass = newClass;
            SetClassSkills();
            ActivateCharacterModel(newClass);
        }

        private void OnCharacterClassChanged(Constants.CharacterClass oldClass, Constants.CharacterClass newClass)
        {
            SetClassSkills();
            ActivateCharacterModel(newClass);
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

        private void SetClassSkills()
        {
            switch (characterClass)
            {
                case Constants.CharacterClass.Mage:
                    SetMovementSkill(new TeleportSkill());
                    SetAvailableAttack(1, 1); // 파이어볼
                    SetAvailableAttack(2, 2); // 번개
                    SetAvailableAttack(3, 4); // 얼음
                    break;

                case Constants.CharacterClass.Archer:
                    SetMovementSkill(new RollSkill());
                    SetAvailableAttack(1, 11); // 원거리 화살 공격
                    SetAvailableAttack(2, 12); // 독화살
                    SetAvailableAttack(3, 13); // 폭발 화살
                    break;
                /*
                case Constants.CharacterClass.Warrior:
                    SetMovementSkill(new ChargeSkill());
                    SetAvailableAttack(1, 7); // 돌진 공격
                    SetAvailableAttack(2, 8); // 광역 베기
                    SetAvailableAttack(3, 9); // 방패 치기
                    break;*/
            }

            //Debug.Log($"[{characterClass}] 직업에 맞게 스킬과 공격을 설정했습니다!");
        }
    }
}