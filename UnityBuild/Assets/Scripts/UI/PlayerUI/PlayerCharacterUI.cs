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

        public void SetQuickSlotData(int index, Sprite icon, float cooldown, string name, string description, Sprite upgradeIcon = null)
        {
            if (index > quickSlots.Length)
            {
                return;
            }
            quickSlots[index].SetQuickSlotData(icon, cooldown, name, description, upgradeIcon);
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
                DamageImage.color = new Color(1, 1, 1, hpPercent - 0.5f);
            }
            if (hpPercent == 1)
            {
                DamageImage.color = new Color(0, 0, 0, 1);
                QuickSlotUI.SetActive(false);
                ghostQuickUI.SetActive(true);
            }
        }
    }
}
