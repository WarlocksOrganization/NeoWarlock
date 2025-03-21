using UnityEngine;

namespace UI
{
    public class PlayerCharacterUI : MonoBehaviour
    {
        [SerializeField] private QuickSlot[] quickSlots;

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
    
    }
}
