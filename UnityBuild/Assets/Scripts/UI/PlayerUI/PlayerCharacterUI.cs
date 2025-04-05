using DataSystem;
using GameManagement;
using Player;
using UnityEngine;
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
        
        private PlayerCharacter localPlayer;
        
        private void OnEnable()
        {
            UpdateQuickSlotKeyLabels(); // 키 표시 반영
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

    }
}
