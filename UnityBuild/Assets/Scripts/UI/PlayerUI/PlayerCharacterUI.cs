using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerCharacterUI : MonoBehaviour
    {
        [SerializeField] private QuickSlot[] quickSlots;
        [SerializeField] private Image DamageImage;
        [SerializeField] private GameObject QuickSlotUI;

        public void SetQuickSlotData(int index, Sprite icon, float cooldown, string name, string description)
        {
            if (index > quickSlots.Length)
            {
                return;
            }
            quickSlots[index].SetQuickSlotData(icon, cooldown, name, description);
        }
    
        public void UseSkill(int index, float cooldown)
        {
            quickSlots[index].UseSkill(cooldown);
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
            DamageImage.color = new Color(1, 1, 1, hpPercent);
            if (hpPercent == 1)
            {
                DamageImage.color = new Color(0, 0, 0, 1);
                QuickSlotUI.SetActive(false);
            }
        }
    }
}
