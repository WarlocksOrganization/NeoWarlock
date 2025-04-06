using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class PlayerCharacterUI : MonoBehaviour
    {
        [SerializeField] private QuickSlot[] quickSlots;
        [SerializeField] private Image DamageImage;
        [SerializeField] private GameObject QuickSlotUI;
        [SerializeField] private GameObject ghostQuickUI;
        [SerializeField] private QuickSlot ghostQuickSlots;
        
        [SerializeField] private GameObject resurrectButton;
        
        [SerializeField] private Transform buffContainer; // ✅ 버프 아이콘이 들어갈 부모
        [SerializeField] private BuffSlot buffSlotPrefab;
        
        private PlayerCharacter localPlayer;
        
        private void OnEnable()
        {
            UpdateQuickSlotKeyLabels(); // 키 표시 반영
            
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene.Contains("GameRoom"))
            {
                resurrectButton.SetActive(true);
            }
            else
            {
                resurrectButton.SetActive(false);
            }
        }

        public void SetQuickSlotData(int index, Sprite icon, float cooldown, string name, string description, Sprite upgradeIcon = null)
        {
            if (index > quickSlots.Length)
            {
                return;
            }
            quickSlots[index].SetQuickSlotData(icon, cooldown, name, description, upgradeIcon);
        }
        
        public void UpdateQuickSlotKeyLabels()
        {
            string[] classicKeys = { "Space", "1", "2", "3", "4" };
            string[] aosKeys =     { "Space", "Q", "W", "E", "R" };

            string[] keysToUse = PlayerSetting.PlayerKeyType == Constants.KeyType.Classic ? classicKeys : aosKeys;

            for (int i = 1; i < quickSlots.Length; i++)
            {
                quickSlots[i].skillNumText.text = keysToUse[i];
            }
        }

    
        public void UseSkill(int index, float cooldown)
        {
            if (index == 4)
            {
                quickSlots[index].DelSkill();
                return;
            }
            
            quickSlots[index].UseSkill(cooldown);
        }
        
        public void UseGhostSkill(float cooldown)
        {
            ghostQuickSlots.UseSkill(cooldown);
        }

        public void SelectSkill(int index, bool selected)
        {
            for (int i = 1; i < quickSlots.Length; i++)
            {
                quickSlots[i].SelectSkill(false);
            }
            if (index > 0)
            {
                quickSlots[index].SelectSkill(selected);
            }
        }

        public void SetDamageEffect(float hpPercent)
        {
            if (hpPercent > 0.5f)
            {
                DamageImage.color = new Color(1, 1, 1, (hpPercent - 0.5f)*2f);
            }
            else
            {
                DamageImage.color = new Color(1, 1, 1, 0);
            }
            if (hpPercent == 1)
            {
                DamageImage.color = new Color(0, 0, 0, 1);
                QuickSlotUI.SetActive(false);
                ghostQuickUI.SetActive(true);
            }
        }
        
        public void OnResurrectButtonClicked()
        {
            localPlayer = GetLocalPlayer();
            
            if (localPlayer != null)
            {
                localPlayer.CmdResurrect();
            }

            SetDamageEffect(0f);
            
            QuickSlotUI.SetActive(true);
            ghostQuickUI.SetActive(false);
        }

        
        PlayerCharacter GetLocalPlayer()
        {
            foreach (var player in FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player.isOwned) // 혹은 player.isLocalPlayer
                {
                    return player;
                }
            }
            return null;
        }

        private Dictionary<string, BuffSlot> activeBuffSlots = new();

        public void ShowBuff(string buffName, Sprite icon, float duration, string description)
        {
            if (activeBuffSlots.ContainsKey(buffName))
            {
                // 🔒 이미 Destroy된 객체에 접근할 가능성이 있음
                if (activeBuffSlots[buffName] != null)
                {
                    Destroy(activeBuffSlots[buffName].gameObject);
                }

                activeBuffSlots.Remove(buffName);
            }

            var slot = Instantiate(buffSlotPrefab, buffContainer);
            slot.Initialize(icon, duration, description);
            activeBuffSlots[buffName] = slot;
        }
        
        public void ClearAllBuffs()
        {
            foreach (var kv in activeBuffSlots)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }
            activeBuffSlots.Clear();
        }
    }
}
